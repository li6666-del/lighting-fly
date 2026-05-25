using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BloodManager : MonoBehaviour
{
    private const int MaxBlood = 100;

    public TextMeshProUGUI bloodText;
    public GameObject gameOverPanel;
    public Button restartButton;
    public Button menuButton;
    public string menuSceneName = "StartMenu";

    public static int blood = MaxBlood;

    private bool isGameOver = false;

    void Start()
    {
        Time.timeScale = 1f;
        isGameOver = false;
        ApplyHpStyle();

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(false);
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(false);
            restartButton.onClick.AddListener(OnRestartClick);
        }

        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(false);
            menuButton.onClick.AddListener(OnMenuClick);
        }
    }

    void Update()
    {
        if (isGameOver || bloodText == null)
            return;

        if (blood > 0)
        {
            ApplyHpStyle();
            bloodText.SetText("HP:{0}", blood);
        }
        else
        {
            GameOver();
        }
    }

    void ApplyHpStyle()
    {
        if (bloodText == null)
            return;

        bloodText.color = new Color(1f, 0.42f, 0.36f, 1f);
        bloodText.fontStyle = FontStyles.Bold;
    }

    void GameOver()
    {
        isGameOver = true;

        if (bloodText != null)
        {
            bloodText.SetText("HP:0");
        }

        Time.timeScale = 0f;

        if (AudioManager.Instance != null)
            AudioManager.Instance.PauseBGM();

        if (HighScoreManager.Instance != null)
        {
            HighScoreManager.Instance.TryUpdateHighScore(ScoreManager.score);
        }

        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
        }

        if (restartButton != null)
        {
            restartButton.gameObject.SetActive(true);
        }

        if (menuButton != null)
        {
            menuButton.gameObject.SetActive(true);
        }

        if (NetworkCoopSession.ShouldReturnToLobby())
        {
            if (restartButton != null)
            {
                restartButton.gameObject.SetActive(false);
            }

            if (menuButton != null)
            {
                menuButton.gameObject.SetActive(false);
            }

            StartCoroutine(ReturnToCoopLobbyAfterDelay());
        }
    }

    public void OnRestartClick()
    {
        if (NetworkCoopSession.ShouldReturnToLobby())
        {
            ReturnToCoopLobby();
            return;
        }

        ResetGameState();
        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeBGM();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void OnMenuClick()
    {
        if (NetworkCoopSession.ShouldReturnToLobby())
        {
            ReturnToCoopLobby();
            return;
        }

        ResetGameState();
        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeBGM();
        SceneManager.LoadScene(menuSceneName);
    }

    private void ReturnToCoopLobby()
    {
        NetworkCoopSession.PrepareReturnToLobby();
        ResetGameState();
        Time.timeScale = 1f;
        if (AudioManager.Instance != null)
            AudioManager.Instance.ResumeBGM();
        SceneManager.LoadScene(NetworkCoopSession.LobbySceneName);
    }

    private IEnumerator ReturnToCoopLobbyAfterDelay()
    {
        yield return new WaitForSecondsRealtime(2f);
        ReturnToCoopLobby();
    }

    public static void ResetGameState()
    {
        ScoreManager.score = 0;
        blood = MaxBlood;
    }

    void OnDestroy()
    {
        if (restartButton != null)
        {
            restartButton.onClick.RemoveListener(OnRestartClick);
        }

        if (menuButton != null)
        {
            menuButton.onClick.RemoveListener(OnMenuClick);
        }
    }
}
