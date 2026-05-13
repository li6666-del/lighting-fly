using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartMenuTurn : MonoBehaviour
{
    public string targetSceneName = "SetMenu1";
    public Button startButton;
    public Button quitButton;

    void Start()
    {
        Time.timeScale = 1f;

        if (startButton != null)
        {
            startButton.onClick.AddListener(OnStartButtonClick);
        }

        if (quitButton != null)
        {
            quitButton.onClick.AddListener(OnQuitButtonClick);
        }
    }

    public void OnStartButtonClick()
    {
        if (string.IsNullOrWhiteSpace(targetSceneName))
            return;

        BloodManager.ResetGameState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(targetSceneName);
    }

    public void OnQuitButtonClick()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void OnDestroy()
    {
        if (startButton != null)
        {
            startButton.onClick.RemoveListener(OnStartButtonClick);
        }

        if (quitButton != null)
        {
            quitButton.onClick.RemoveListener(OnQuitButtonClick);
        }
    }
}
