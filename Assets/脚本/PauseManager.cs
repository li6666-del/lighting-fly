using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    public GameObject pausePanel;
    public Button resumeButton;
    public Button menuButton;

    [Header("Scene")]
    public string menuSceneName = "StartMenu";

    private bool isPaused = false;

    void Start()
    {
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(ResumeGame);
        }

        if (menuButton != null)
        {
            menuButton.onClick.AddListener(ReturnToMenu);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused)
                ResumeGame();
            else
                PauseGame();
        }
    }

    void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(true);
        }
    }

    void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
        {
            pausePanel.SetActive(false);
        }
    }

    void ReturnToMenu()
    {
        Time.timeScale = 1f;

        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.TryUpdateHighScore(ScoreManager.score);
        }

        bool isCoopGame = NetworkCoopSession.ShouldReturnToLobby();
        if (isCoopGame)
        {
            NetworkCoopSession.PrepareReturnToLobby();
        }

        BloodManager.ResetGameState();
        SceneManager.LoadScene(isCoopGame ? NetworkCoopSession.LobbySceneName : menuSceneName);
    }

    void OnDestroy()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.RemoveListener(ResumeGame);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(ReturnToMenu);
        }
    }
}
