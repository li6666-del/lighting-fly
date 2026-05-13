using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Settwoturn : MonoBehaviour
{
    public string targetSceneName1 = "GameScene1";
    public string targetSceneName2 = "GameScene2";
    public string targetSceneName3 = "SetMenu1";

    public Button jumpButton1;
    public Button jumpButton2;
    public Button jumpButton3;

    void Start()
    {
        if (jumpButton1 != null)
        {
            jumpButton1.onClick.AddListener(OnJumpButton1Click);
        }

        if (jumpButton2 != null)
        {
            jumpButton2.onClick.AddListener(OnJumpButton2Click);
        }

        if (jumpButton3 != null)
        {
            jumpButton3.onClick.AddListener(OnJumpButton3Click);
        }
    }

    public void OnJumpButton1Click()
    {
        LoadTargetScene(targetSceneName1);
    }

    public void OnJumpButton2Click()
    {
        LoadTargetScene(targetSceneName2);
    }

    public void OnJumpButton3Click()
    {
        LoadTargetScene(targetSceneName3);
    }

    void LoadTargetScene(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
            return;

        BloodManager.ResetGameState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    void OnDestroy()
    {
        if (jumpButton1 != null)
        {
            jumpButton1.onClick.RemoveListener(OnJumpButton1Click);
        }

        if (jumpButton2 != null)
        {
            jumpButton2.onClick.RemoveListener(OnJumpButton2Click);
        }

        if (jumpButton3 != null)
        {
            jumpButton3.onClick.RemoveListener(OnJumpButton3Click);
        }
    }
}
