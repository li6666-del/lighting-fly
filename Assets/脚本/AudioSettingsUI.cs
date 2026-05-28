using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    private GameObject panel;
    private Button togglePanelButton;
    private Button muteButton;
    private Button prevButton;
    private Button nextButton;
    private Button volumeDownButton;
    private Button volumeUpButton;
    private Button restoreButton;
    private Slider volumeSlider;
    private TextMeshProUGUI trackNameText;
    private TextMeshProUGUI muteText;
    private RectTransform toggleButtonRect;
    private RectTransform panelRect;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
        BuildUI();
    }

    void OnEnable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SettingsChanged += Refresh;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        Refresh();
        ApplyScenePosition();
    }

    void OnDisable()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SettingsChanged -= Refresh;
        }

        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.B))
        {
            TogglePanel();
        }

        if (Input.GetKeyDown(KeyCode.M))
        {
            AudioManager.Instance?.ToggleBGM();
        }

        if (Input.GetKeyDown(KeyCode.LeftBracket))
        {
            AudioManager.Instance?.PlayPrevBGM();
        }

        if (Input.GetKeyDown(KeyCode.RightBracket))
        {
            AudioManager.Instance?.PlayNextBGM();
        }

        if (Input.GetKeyDown(KeyCode.Minus) || Input.GetKeyDown(KeyCode.KeypadMinus))
        {
            AudioManager.Instance?.AdjustBGMVolume(-0.1f);
        }

        if (Input.GetKeyDown(KeyCode.Equals) || Input.GetKeyDown(KeyCode.Plus) || Input.GetKeyDown(KeyCode.KeypadPlus))
        {
            AudioManager.Instance?.AdjustBGMVolume(0.1f);
        }

        if (Input.GetKeyDown(KeyCode.Escape) && panel != null && panel.activeSelf)
        {
            panel.SetActive(false);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplyScenePosition();
    }

    void BuildUI()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 200;
        gameObject.AddComponent<CanvasScaler>();
        gameObject.AddComponent<GraphicRaycaster>();

        togglePanelButton = CreateButton(transform, "BGM", new Vector2(66f, 36f), new Vector2(64f, -42f), new Color(0.08f, 0.13f, 0.18f, 0.82f));
        toggleButtonRect = togglePanelButton.GetComponent<RectTransform>();
        togglePanelButton.onClick.AddListener(TogglePanel);

        panel = new GameObject("BGM Panel");
        panel.transform.SetParent(transform, false);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.04f, 0.06f, 0.08f, 0.84f);

        panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(16f, -86f);
        panelRect.sizeDelta = new Vector2(360f, 206f);

        muteButton = CreateButton(panel.transform, "On", new Vector2(70f, 34f), new Vector2(45f, -32f), new Color(0.1f, 0.45f, 0.5f, 0.9f));
        muteText = muteButton.GetComponentInChildren<TextMeshProUGUI>();
        muteButton.onClick.AddListener(() =>
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.ToggleBGM();
            }
        });

        prevButton = CreateButton(panel.transform, "<", new Vector2(42f, 34f), new Vector2(99f, -32f), new Color(0.13f, 0.17f, 0.21f, 0.9f));
        prevButton.onClick.AddListener(() => AudioManager.Instance?.PlayPrevBGM());

        nextButton = CreateButton(panel.transform, ">", new Vector2(42f, 34f), new Vector2(281f, -32f), new Color(0.13f, 0.17f, 0.21f, 0.9f));
        nextButton.onClick.AddListener(() => AudioManager.Instance?.PlayNextBGM());

        trackNameText = CreateLabel(panel.transform, "", new Vector2(190f, -32f), 16f, TextAlignmentOptions.Center);
        trackNameText.rectTransform.sizeDelta = new Vector2(166f, 34f);
        trackNameText.enableWordWrapping = false;
        trackNameText.overflowMode = TextOverflowModes.Ellipsis;

        CreateLabel(panel.transform, "Volume", new Vector2(61f, -88f), 20f, TextAlignmentOptions.Left);
        volumeDownButton = CreateButton(panel.transform, "-", new Vector2(36f, 30f), new Vector2(132f, -90f), new Color(0.13f, 0.17f, 0.21f, 0.9f));
        volumeDownButton.onClick.AddListener(() => AudioManager.Instance?.AdjustBGMVolume(-0.1f));

        volumeSlider = CreateSlider(panel.transform, new Vector2(228f, -90f), new Vector2(150f, 24f));
        volumeSlider.onValueChanged.AddListener(value => AudioManager.Instance?.SetBGMVolume(value));

        volumeUpButton = CreateButton(panel.transform, "+", new Vector2(36f, 30f), new Vector2(320f, -90f), new Color(0.13f, 0.17f, 0.21f, 0.9f));
        volumeUpButton.onClick.AddListener(() => AudioManager.Instance?.AdjustBGMVolume(0.1f));

        restoreButton = CreateButton(panel.transform, "Reset", new Vector2(84f, 30f), new Vector2(73f, -142f), new Color(0.28f, 0.12f, 0.1f, 0.9f));
        restoreButton.onClick.AddListener(() => AudioManager.Instance?.RestoreAudibleDefaults());

        CreateLabel(panel.transform, "B:Menu  M:Mute  +/-:Volume  []:Track", new Vector2(227f, -142f), 15f, TextAlignmentOptions.Center)
            .rectTransform.sizeDelta = new Vector2(240f, 30f);

        panel.SetActive(false);
    }

    void ApplyScenePosition()
    {
        bool isGameplayScene = SceneManager.GetActiveScene().name.StartsWith("GameScene");
        float yOffset = isGameplayScene ? -82f : -42f;
        float panelYOffset = isGameplayScene ? -126f : -86f;

        if (toggleButtonRect != null)
        {
            toggleButtonRect.anchoredPosition = new Vector2(64f, yOffset);
        }

        if (panelRect != null)
        {
            panelRect.anchoredPosition = new Vector2(16f, panelYOffset);
        }
    }

    void TogglePanel()
    {
        if (panel != null)
        {
            panel.SetActive(!panel.activeSelf);
        }
    }

    void Refresh()
    {
        if (AudioManager.Instance == null)
            return;

        if (muteText != null)
        {
            muteText.text = AudioManager.Instance.IsMuted ? "Off" : "On";
        }

        if (volumeSlider != null)
        {
            volumeSlider.SetValueWithoutNotify(AudioManager.Instance.BGMVolume);
        }

        if (trackNameText != null)
        {
            trackNameText.text = AudioManager.Instance.CurrentTrackName;
        }
    }

    Button CreateButton(Transform parent, string text, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject buttonObject = new GameObject(text + " Button");
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.AddComponent<Image>();
        image.color = color;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        DisableNavigation(button);
        button.onClick.AddListener(ClearSelectedUi);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = CreateLabel(buttonObject.transform, text, Vector2.zero, 24f, TextAlignmentOptions.Center);
        label.rectTransform.anchorMin = Vector2.zero;
        label.rectTransform.anchorMax = Vector2.one;
        label.rectTransform.offsetMin = Vector2.zero;
        label.rectTransform.offsetMax = Vector2.zero;
        return button;
    }

    TextMeshProUGUI CreateLabel(Transform parent, string text, Vector2 anchoredPosition, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject labelObject = new GameObject(text + " Label");
        labelObject.transform.SetParent(parent, false);

        TextMeshProUGUI label = labelObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = new Color(0.8f, 0.94f, 1f);
        label.alignment = alignment;
        label.raycastTarget = false;

        RectTransform rect = label.rectTransform;
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(110f, 28f);
        return label;
    }

    Slider CreateSlider(Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject sliderObject = new GameObject("BGM Volume Slider");
        sliderObject.transform.SetParent(parent, false);

        RectTransform rect = sliderObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Slider slider = sliderObject.AddComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        DisableNavigation(slider);
        ClearSelectionOnPointerUp(sliderObject);

        Image background = CreateImage(sliderObject.transform, "Background", new Color(0.15f, 0.18f, 0.22f, 0.95f));
        Stretch(background.rectTransform);

        Image fill = CreateImage(sliderObject.transform, "Fill", new Color(0.08f, 0.88f, 1f, 0.95f));
        RectTransform fillRect = fill.rectTransform;
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = new Vector2(4f, 5f);
        fillRect.offsetMax = new Vector2(-4f, -5f);

        Image handle = CreateImage(sliderObject.transform, "Handle", new Color(0.92f, 0.98f, 1f, 1f));
        RectTransform handleRect = handle.rectTransform;
        handleRect.sizeDelta = new Vector2(16f, 24f);

        slider.targetGraphic = handle;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        return slider;
    }

    Image CreateImage(Transform parent, string objectName, Color color)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;
        return image;
    }

    void DisableNavigation(Selectable selectable)
    {
        Navigation navigation = selectable.navigation;
        navigation.mode = Navigation.Mode.None;
        selectable.navigation = navigation;
    }

    void ClearSelectedUi()
    {
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(null);
        }
    }

    void ClearSelectionOnPointerUp(GameObject target)
    {
        EventTrigger trigger = target.AddComponent<EventTrigger>();
        EventTrigger.Entry entry = new EventTrigger.Entry
        {
            eventID = EventTriggerType.PointerUp
        };
        entry.callback.AddListener(_ => ClearSelectedUi());
        trigger.triggers.Add(entry);
    }

    void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
