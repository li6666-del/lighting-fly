# 雷霆战机最终版

一款基于 Unity 的 3D 飞行射击无尽生存游戏。玩家驾驶战机横向闪避、持续射击、积攒技能能量，在越来越密集的敌机、Boss 弹幕和动态难度中尽可能获得更高分数。

## 项目亮点

- 单机无尽模式：支持 `GameScene1`、`GameScene2` 两个单机场景。
- 战机能量选色：进入单机场景前可选择蓝色或绿色，局内战机辉光、子弹、尾焰和技能 UI 同步换色。
- 三段核心技能：`X` 护盾齐射、`C` 激光爆发、`V` 虚空坍缩。
- 动态难度系统：分数越高，敌人生成越快、波次更密、Boss 压力更强、击杀恢复收益降低。
- Boss 战表现：包含血条、蓄力警告、激光压迫、多 Boss 与弹幕反馈。
- Photon 联机模式：支持创建/加入房间，房主战机渲染为蓝色，加入者战机渲染为绿色。
- 运行时特效系统：尾焰、辉光、爆炸、护盾、激光、黑洞和命中特效均由脚本动态生成。

## 运行环境

- Unity：`2022.3.62f3c1`
- 渲染管线：Built-in Render Pipeline
- 主要依赖：
  - Photon PUN 2
  - TextMeshPro
  - Post Processing
  - UnityGLTF

## 场景结构

Build Settings 当前包含以下场景：

| 场景 | 作用 |
| --- | --- |
| `Assets/Scenes/StartMenu.unity` | 开始菜单与开场入口 |
| `Assets/Scenes/SetMenu1.unity` | 模式选择菜单 |
| `Assets/Scenes/SetMenu2.unity` | 单机场景选择菜单 |
| `Assets/Scenes/GameScene1.unity` | 单机无尽场景 1 |
| `Assets/Scenes/GameScene2.unity` | 单机无尽场景 2 |
| `Assets/Scenes/NetworkLobby.unity` | Photon 联机大厅 |
| `Assets/Scenes/NetworkGameScene.unity` | 联机游戏场景 |

## 操作方式

| 操作 | 按键 |
| --- | --- |
| 左右移动 | `A / D` 或方向键 |
| 普通射击 | `Space` |
| 护盾齐射 | `X` |
| 激光爆发 | `C` |
| 虚空坍缩 | `V` |

技能需要通过击杀敌人积攒充能，右上角会显示技能次数、充能进度和护盾剩余时间。

## 联机说明

联机部分基于 Photon PUN 2：

- 玩家在 `NetworkLobby` 中创建或加入房间。
- 房间内由 MasterClient 负责敌人与 Boss 的权威生成。
- 战机、子弹、技能、血量、分数和游戏结束状态通过 Photon 同步。
- 创建房间的玩家使用蓝色主题，加入房间的玩家使用绿色主题。

Photon 配置文件位于：

```text
Assets/Photon/PhotonUnityNetworking/Resources/PhotonServerSettings.asset
```

出于隐私与安全考虑，仓库中不保存 Photon AppId。需要运行联机模式时，请在 Unity 中打开 Photon 设置面板，填入自己的 Realtime AppId 后再本地运行。

## 关键脚本

| 脚本 | 作用 |
| --- | --- |
| `Assets/脚本/PlayerMovement.cs` | 玩家移动与单机玩家初始化 |
| `Assets/脚本/FireLogic.cs` | 普通射击逻辑 |
| `Assets/脚本/PlayerSkill.cs` | 单机 X/C/V 技能逻辑 |
| `Assets/脚本/PlayerShipColorSelection.cs` | 单机战机选色 UI 与蓝/绿主题 |
| `Assets/脚本/DifficultyManager.cs` | 动态难度参数 |
| `Assets/脚本/EnemySpawner.cs` | 敌人波次生成 |
| `Assets/脚本/BossController.cs` | Boss 血量、攻击与表现 |
| `Assets/脚本/CombatEffects.cs` | 通用运行时战斗特效 |
| `Assets/Effects/LaserBeam.cs` | 激光技能表现 |
| `Assets/Effects/NetworkPlayerSkillFx.cs` | 联机/技能特效表现 |
| `Assets/Effects/BossChargeWarning.cs` | Boss 蓄力警告特效 |
| `Assets/脚本/NetworkLauncher.cs` | Photon 连接、建房、加房 UI |
| `Assets/脚本/NetworkPlayerController.cs` | 联机玩家移动、射击、技能同步 |
| `Assets/脚本/NetworkCoopGameRuntime.cs` | 联机敌人、Boss、状态同步 |

## 打开与验证

1. 使用 Unity Hub 打开项目根目录：

```text
D:\unity\雷霆战机最终版
```

2. 确认 Unity 版本为 `2022.3.62f3c1` 或兼容的 2022.3 LTS。
3. 从 `StartMenu` 或 Build Settings 中的场景开始运行。
4. 可选命令行编译检查：

```powershell
dotnet build Assembly-CSharp.csproj
```

## 目录维护建议

- `Library/`、`Temp/`、`Logs/`、`UserSettings/` 属于 Unity 本地生成目录，不应提交。
- 大型视频、模型、音频资源会明显增加仓库体积，后续如果资源继续增多，建议考虑 Git LFS。
- 功能完成后及时 `commit`，保持 `main` 分支为可运行的稳定版本。
