using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SetMenuOnlineTurn : MonoBehaviour
{
    public string onlineLobbySceneName = "NetworkLobby";
    public Button onlineButton;

    private bool isBound;

    void Awake()
    {
        if (onlineButton == null)
        {
            onlineButton = FindButtonByLabel("Online");
        }
    }

    void OnEnable()
    {
        BindButton();
    }

    void Start()
    {
        BindButton();
    }

    void OnDisable()
    {
        UnbindButton();
    }

    public void OpenOnlineLobby()
    {
        if (string.IsNullOrWhiteSpace(onlineLobbySceneName))
        {
            Debug.LogWarning("Online lobby scene name is empty.");
            return;
        }

        BloodManager.ResetGameState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(onlineLobbySceneName);
    }

    private void BindButton()
    {
        if (isBound || onlineButton == null)
            return;

        onlineButton.onClick.AddListener(OpenOnlineLobby);
        isBound = true;
    }

    private void UnbindButton()
    {
        if (!isBound || onlineButton == null)
            return;

        onlineButton.onClick.RemoveListener(OpenOnlineLobby);
        isBound = false;
    }

    private Button FindButtonByLabel(string labelText)
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            TextMeshProUGUI tmpLabel = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (tmpLabel != null && tmpLabel.text.Trim() == labelText)
                return button;

            Text legacyLabel = button.GetComponentInChildren<Text>(true);
            if (legacyLabel != null && legacyLabel.text.Trim() == labelText)
                return button;
        }

        return null;
    }
}
