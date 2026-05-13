  using TMPro;
using UnityEngine;

/// <summary>
/// 最高分显示脚本（轻量版）
/// 专门用于菜单场景：从 PlayerPrefs 读取最高分并显示到 TMP 文本上
/// 不依赖 HighScoreManager 是否存在，直接读取存储的数据
/// 将此脚本挂到菜单场景中的 TMP 文本对象上即可
/// </summary>
public class HighScoreDisplay : MonoBehaviour
{
    // PlayerPrefs 存储键名，必须与 HighScoreManager 中的 HighScoreKey 一致
    private const string HighScoreKey = "HighScore";

    /// <summary>
    /// 场景加载时立即读取并显示最高分
    /// </summary>
    void Start()
    {
        // 获取 TMP 文本组件（脚本挂在 TMP 对象上，直接 GetComponent 即可）
        TextMeshProUGUI text = GetComponent<TextMeshProUGUI>();

        if (text != null)
        {
            // 从 PlayerPrefs 读取最高分，若从未保存过则默认为 0
            int highScore = PlayerPrefs.GetInt(HighScoreKey, 0);
            text.SetText("Best Score: {0}", highScore);
        }
        else
        {
            Debug.LogWarning("HighScoreDisplay:未找到 TextMeshProUGUI 组件，请将此脚本挂到 TMP 文本对象上");
        }  
    }
}
