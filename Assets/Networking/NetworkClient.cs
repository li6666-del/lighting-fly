// ============================================================
// NetworkClient.cs  -  M1: Unity 侧的 UDP 回声客户端
// ------------------------------------------------------------
// 功能:挂在场景任意 GameObject 上。Start 时与 GameServer
//      建立 UDP socket,开一个后台收包线程;主线程 Update
//      里检测 Space 按键发送 "Hello from Unity",收到回包
//      后 Debug.Log 到 Console。
//
// 核心模式(本项目的基石,M2-M6 都会沿用):
//      ┌─────────────┐           ┌──────────────────────┐
//      │ 子线程       │ Receive  │ 阻塞等待 UDP 包到达   │
//      │ ReceiveLoop │────────▶│ 解析后丢进线程安全队列 │
//      └─────────────┘           └──────────┬───────────┘
//                                           │
//      ┌─────────────┐           ┌──────────▼───────────┐
//      │ 主线程       │ Update  │ 从队列消费、分发事件   │
//      │ MonoBehavior│◀────────│ (只有这里能碰 Unity API)│
//      └─────────────┘           └──────────────────────┘
//
// 为什么非要这么搞?
//   - UdpClient.Receive 是阻塞 API,放在主线程会直接卡住
//     Unity 的 60fps 渲染循环,游戏会冻住。
//   - 子线程又不能直接访问 transform/Debug.Log 之类的
//     Unity API(Unity 绝大多数 API 非线程安全,必须在
//     主线程调用)。
//   - 所以中间夹一个 ConcurrentQueue 做"线程间邮箱",
//     子线程塞进去、主线程取出来,双方解耦。
// ============================================================

using System;
using System.Collections.Concurrent; // ConcurrentQueue<T>:线程安全队列
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>
/// M1 阶段的 UDP 客户端。挂到场景里任一 GameObject 上即可工作。
/// Inspector 可配置 Server 地址和端口。
/// </summary>
public class NetworkClient : MonoBehaviour
{
    // ---------- Inspector 可配置字段 ----------

    [Header("Server 连接参数")]
    [Tooltip("GameServer 所在的 IP。本机调试用 127.0.0.1。")]
    [SerializeField] private string serverIP = "127.0.0.1";

    [Tooltip("GameServer 监听的 UDP 端口,必须和 Server 端一致(默认 7777)。")]
    [SerializeField] private int serverPort = 7777;

    // ---------- 运行期字段(私有) ----------

    // .NET 的 UDP socket 封装。一个 UdpClient 既能收也能发。
    private UdpClient udpClient;

    // Server 的完整地址(IP + Port),发送时作为目的地。
    private IPEndPoint serverEndPoint;

    // 后台收包线程。由 Start 启动,OnDestroy 停止。
    private Thread receiveThread;

    // 线程间通信"邮箱":子线程 Enqueue,主线程 Dequeue。
    // ConcurrentQueue 是 .NET 内置的无锁线程安全队列,
    // 无需我们自己加 lock —— 用它就对了。
    private readonly ConcurrentQueue<string> receivedMessages = new ConcurrentQueue<string>();

    // 控制收包线程是否继续循环的标志位。
    // volatile 关键字告诉编译器/CPU:这个变量可能被别的线程
    // 改,不要把它的读取缓存到寄存器里 —— 每次都要从内存读。
    // 不加 volatile 可能出现"主线程设 false 但子线程看不到"的
    // 诡异 bug(虽然现代 x86 上大概率不会,但语义上必须加)。
    private volatile bool isRunning = false;

    // ============================================================
    // Unity 生命周期
    // ============================================================

    /// <summary>
    /// Start:场景加载后、第一帧 Update 前调用。
    /// 在这里初始化 socket 并启动收包线程。
    /// </summary>
    private void Start()
    {
        try
        {
            // 传 0 表示"让 OS 随机挑一个可用端口并立即绑定"。
            // 相比无参构造,这样可以保证 socket 在 Start 返回前
            // 就已经在内核里绑好监听状态 —— 子线程的 Receive
            // 一开始就有一个确定的 socket 在收包,不会因为 Send
            // 还没执行而处于"悬空"状态导致回包丢失。
            udpClient = new UdpClient(0);

            // 打印实际绑定到的本地端口,方便排查"Server 回发到哪
            // 个端口"、"Client 在哪个端口监听"是否一致。
            int localPort = ((IPEndPoint)udpClient.Client.LocalEndPoint).Port;
            Debug.Log($"[NetworkClient] Local UDP socket bound to port {localPort}");

            // 把 Server IP + Port 打包成 IPEndPoint,后面 Send 要用。
            serverEndPoint = new IPEndPoint(IPAddress.Parse(serverIP), serverPort);

            // 启动收包线程(下面 ReceiveLoop 方法)。
            isRunning = true;
            receiveThread = new Thread(ReceiveLoop)
            {
                // IsBackground=true:前台线程会阻止进程退出,
                // 后台线程则随进程一起死。Unity Editor 停止播放
                // 时,如果这是前台线程,Editor 会假死 —— 所以必须
                // 设成后台线程。
                IsBackground = true,
                Name = "UDP-ReceiveLoop"
            };
            receiveThread.Start();

            Debug.Log($"[NetworkClient] Started. Target = {serverIP}:{serverPort}. Press SPACE to send.");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkClient] Start failed: {ex}");
        }
    }

    /// <summary>
    /// Update:每帧调用(60Hz 左右)。做两件事:
    ///   1. 检测 Space 按键 → 发送 Hello 包;
    ///   2. 把子线程塞进队列的收包内容 Debug.Log 出来。
    /// </summary>
    private void Update()
    {
        // ---- 1. 发送 ----
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SendMessageToServer("Hello from Unity");
        }

        // ---- 2. 消费收包队列 ----
        // while 而不是 if:一帧内可能累积多个包,一次全处理完,
        // 避免延迟累积。TryDequeue 失败说明队列空了就退出。
        while (receivedMessages.TryDequeue(out string msg))
        {
            Debug.Log($"[NetworkClient] <- {msg}");
        }
    }

    /// <summary>
    /// OnDestroy:对象销毁或场景切换或 Editor 停止播放时调用。
    /// 必须在这里关闭 socket 和线程,否则:
    ///   - socket 不关会一直占用端口;
    ///   - 线程不停会在 Editor 里泄漏,下次播放叠加一份。
    /// </summary>
    private void OnDestroy()
    {
        // 先通知子线程退出循环。
        isRunning = false;

        // 关闭 socket。这会让子线程里阻塞的 Receive 抛出
        // SocketException,从而跳出循环(见 ReceiveLoop)。
        if (udpClient != null)
        {
            udpClient.Close();
            udpClient = null;
        }

        // 等子线程真正退出(最多等 200ms)。
        // Join 不带超时的话,万一子线程卡住会把主线程也卡死。
        if (receiveThread != null && receiveThread.IsAlive)
        {
            receiveThread.Join(200);
            receiveThread = null;
        }

        Debug.Log("[NetworkClient] Shutdown complete.");
    }

    // ============================================================
    // 发送与接收
    // ============================================================

    /// <summary>
    /// 把字符串以 UTF-8 编码发给 Server。主线程调用即可
    /// (Send 不阻塞,底层是 OS 级的"塞进发送缓冲区就返回")。
    /// </summary>
    public void SendMessageToServer(string text)
    {
        if (udpClient == null) return;

        try
        {
            byte[] data = Encoding.UTF8.GetBytes(text);
            udpClient.Send(data, data.Length, serverEndPoint);
            Debug.Log($"[NetworkClient] -> \"{text}\" ({data.Length} bytes)");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[NetworkClient] Send failed: {ex.Message}");
        }
    }

    /// <summary>
    /// 后台收包线程主函数。在循环里阻塞 Receive,收到包就
    /// 塞进 receivedMessages 队列给主线程消费。
    ///
    /// 注意:这个方法里绝对不能调用任何 Unity API
    ///       (Debug.Log 在子线程里调用虽然不会崩,但任何
    ///        transform/GameObject 访问都会抛异常)。
    ///        上面我用的 Debug.Log 其实 Unity 是允许的,
    ///        但作为纪律,全部信息都走队列更安全。
    /// </summary>
    private void ReceiveLoop()
    {
        // 接收方 IPEndPoint。Receive 会把实际发送方的地址填进来。
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);

        while (isRunning)
        {
            try
            {
                // 阻塞等包。OnDestroy 里的 Close 会让这里抛出
                // SocketException / ObjectDisposedException,
                // 从而走到下面 catch 退出循环。
                byte[] data = udpClient.Receive(ref remoteEndPoint);

                string text = Encoding.UTF8.GetString(data);

                // 塞进队列,等主线程 Update 取。
                receivedMessages.Enqueue(
                    $"from {remoteEndPoint.Address}:{remoteEndPoint.Port}  \"{text}\"");
            }
            catch (SocketException)
            {
                // 关闭 socket 触发,正常退出路径,不打印错误。
                break;
            }
            catch (ObjectDisposedException)
            {
                // udpClient 已被 Close/Dispose,同上。
                break;
            }
            catch (Exception ex)
            {
                // 其他未知异常,塞进队列让主线程打印出来。
                receivedMessages.Enqueue($"[ReceiveLoop Error] {ex.Message}");
            }
        }
    }
}
