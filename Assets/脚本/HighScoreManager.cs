using TMPro;
using UnityEngine;

/// <summary>
/// 最高分管理器
/// 负责记录、持久化保存最高分，并通过TMP文本显示最高分
/// 使用 PlayerPrefs 存储，关闭游戏后数据不丢失
/// </summary>
public class HighScoreManager : MonoBehaviour
{
    // 单例实例，方便其他脚本通过 HighScoreManager.Instance 访问
    public static HighScoreManager Instance { get; private set; }

    [Tooltip("用于显示最高分的TMP文本组件，在Inspector中拖入")]
    public TextMeshProUGUI highScoreText;

    // PlayerPrefs 存储用的键名，用常量避免拼写错误
    private const string HighScoreKey = "HighScore";

    /// <summary>
    /// 静态属性：从 PlayerPrefs 读取当前最高分
    /// 若从未存储过则默认返回 0
    /// </summary>
   //实际存储在 PlayerPrefs 里的键名
    public  static  int   HighScore=>PlayerPrefs.GetInt(HighScoreKey,0);//静态属性，不需要实例也能访问

    void Awake()
    {
        // 单例模式：若场景中已存在一个实例，则销毁重复的
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        // 游戏启动时立即刷新一次显示，确保TMP文本显示正确的历史最高分
        UpdateDisplay();
    }

    /// <summary>
    /// 尝试更新最高分，在玩家死亡时调用
    /// 只有 currentScore 超过历史最高分时才会写入并刷新显示
    /// </summary>
    /// <param name="currentScore">本局死亡时的得分</param>
    public void TryUpdateHighScore(int currentScore)
    {
        // 当前分数高于历史最高分才更新，否则不做任何操作
        if (currentScore > HighScore)
        {
            // 写入 PlayerPrefs 并立即落盘，防止意外退出导致数据丢失
            PlayerPrefs.SetInt(HighScoreKey, currentScore);
            PlayerPrefs.Save();

            // 刷新 TMP 文本显示新的最高分
            UpdateDisplay();
        }
    }

    /// <summary>
    /// 将最高分写入 TMP 文本组件
    /// </summary>
    void UpdateDisplay()
    {
        // 防止未绑定文本组件时报空引用错误
        if (highScoreText != null)
            highScoreText.SetText("最高分: {0}", HighScore);
    }
}
