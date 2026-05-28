using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    private const int MinRoomCode = 100000;
    private const int MaxRoomCode = 1000000;
    private const int MaxCreateRoomRetries = 8;
    private const string RoomCodeUiName = "Network Room Code UI";
    private const string StatusTextName = "Network Status Text";

    private static NetworkLauncher activeLauncher;

    [Header("Connection")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 2;
    public string networkGameSceneName = "GameScene1";
    public string previousMenuSceneName = NetworkCoopSession.PreviousMenuSceneName;
    public bool waitForFullRoom = true;

    [Header("Optional UI")]
    public TextMeshProUGUI statusText;
    public bool autoBuildRoomCodeUi = true;

    [Header("Debug")]
    public bool allowRandomMatchmakingDebug;

    private bool isLoadingGame;
    private bool createRoomWhenReady;
    private bool joinRandomWhenReady;
    private bool joinRoomByCodeWhenReady;
    private string createdRoomCode;
    private string pendingJoinRoomCode;
    private int createRoomRetryCount;

    private GameObject roomCodeUiRoot;
    private GameObject createRoomPanel;
    private GameObject joinRoomPanel;
    private TextMeshProUGUI roomCodeText;
    private TextMeshProUGUI createRoomStatusText;
    private TextMeshProUGUI joinRoomStatusText;
    private TMP_InputField roomCodeInput;
    private bool ownsStatusText;

    void Awake()
    {
        if (!enabled)
            return;

        if (activeLauncher != null && activeLauncher != this)
        {
            Debug.LogWarning("[NetworkLauncher] Duplicate active launcher found. Disabling this instance.");
            enabled = false;
            return;
        }

        activeLauncher = this;
    }

    void OnDestroy()
    {
        DestroyLobbyRuntimeUi();

        if (activeLauncher == this)
        {
            activeLauncher = null;
        }
    }

    void Start()
    {
        if (activeLauncher != this)
            return;

        if (autoBuildRoomCodeUi)
        {
            EnsureRoomCodeUi();
        }

        if (NetworkCoopSession.IsReturningToLobby)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            if (PhotonNetwork.IsConnected)
            {
                SetStatus("Resetting previous co-op connection...");
                PhotonNetwork.Disconnect();
                return;
            }

            NetworkCoopSession.CompleteReturnToLobby();
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        Connect();
    }

    public void Connect()
    {
        SetStatus("Connecting...");

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            SetStatus("Leaving previous room...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.JoinLobby();
            return;
        }

        PhotonNetwork.GameVersion = gameVersion;
        PhotonNetwork.ConnectUsingSettings();
    }

    public void CreateRoom()
    {
        EnsureRoomCodeUi();
        ShowCreateRoomPanel();

        joinRandomWhenReady = false;
        joinRoomByCodeWhenReady = false;
        pendingJoinRoomCode = string.Empty;
        createRoomRetryCount = 0;
        createdRoomCode = GenerateRoomCode();
        SetRoomCodeLabel(createdRoomCode);

        if (!IsReadyForMatchmaking())
        {
            createRoomWhenReady = true;
            SetStatus($"Room Code: {createdRoomCode}. Connecting to Photon...");
            EnsureConnectedToLobby();
            return;
        }

        CreateRoomNow();
    }

    public void ShowJoinRoomPanel()
    {
        EnsureRoomCodeUi();
        HideRoomCodePanels();

        if (roomCodeUiRoot != null)
            roomCodeUiRoot.SetActive(true);

        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(true);

        if (roomCodeInput != null)
        {
            roomCodeInput.text = string.Empty;
            roomCodeInput.Select();
            roomCodeInput.ActivateInputField();
        }

        createRoomWhenReady = false;
        joinRoomByCodeWhenReady = false;
        pendingJoinRoomCode = string.Empty;
        SetStatus("Enter the 6-digit room code.");
    }

    public void JoinRoomWithInputCode()
    {
        EnsureRoomCodeUi();

        string code = NormalizeRoomCode(roomCodeInput != null ? roomCodeInput.text : pendingJoinRoomCode);
        if (!IsValidRoomCode(code))
        {
            SetStatus("Please enter a 6-digit room code.");
            return;
        }

        pendingJoinRoomCode = code;
        createRoomWhenReady = false;
        joinRandomWhenReady = false;

        if (!IsReadyForMatchmaking())
        {
            joinRoomByCodeWhenReady = true;
            SetStatus($"Room Code: {pendingJoinRoomCode}. Connecting to Photon...");
            EnsureConnectedToLobby();
            return;
        }

        JoinRoomByCodeNow();
    }

    public void CancelRoomCodePanel()
    {
        createRoomWhenReady = false;
        joinRoomByCodeWhenReady = false;
        joinRandomWhenReady = false;
        pendingJoinRoomCode = string.Empty;
        createdRoomCode = string.Empty;
        createRoomRetryCount = 0;

        HideRoomCodePanels();

        if (PhotonNetwork.InRoom && !isLoadingGame)
        {
            SetStatus("Leaving room...");
            PhotonNetwork.LeaveRoom();
            return;
        }

        SetStatus("Ready. Create a room or enter a room code.");
    }

    public void JoinRandomRoom()
    {
        if (!allowRandomMatchmakingDebug)
        {
            ShowJoinRoomPanel();
            return;
        }

        createRoomWhenReady = false;
        joinRoomByCodeWhenReady = false;

        if (!IsReadyForMatchmaking())
        {
            joinRandomWhenReady = true;
            SetStatus("Photon is connecting. Random room will be joined when ready...");
            EnsureConnectedToLobby();
            return;
        }

        JoinRandomRoomNow();
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public void BackToPreviousMenu()
    {
        NetworkCoopSession.EndCoopSession(true);
        BloodManager.ResetGameState();
        Time.timeScale = 1f;
        SceneManager.LoadScene(previousMenuSceneName);
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("Connected. Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        if (createRoomWhenReady)
        {
            CreateRoomNow();
            return;
        }

        if (joinRoomByCodeWhenReady)
        {
            JoinRoomByCodeNow();
            return;
        }

        if (joinRandomWhenReady)
        {
            JoinRandomRoomNow();
            return;
        }

        SetStatus("Ready. Create a room or enter a room code.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        if (returnCode == ErrorCode.GameIdAlreadyExists && !string.IsNullOrEmpty(createdRoomCode) && createRoomRetryCount < MaxCreateRoomRetries)
        {
            createRoomRetryCount++;
            createdRoomCode = GenerateRoomCode();
            SetRoomCodeLabel(createdRoomCode);
            SetStatus($"Room code already used. Retrying with {createdRoomCode}...");
            CreateRoomNow();
            return;
        }

        createRoomWhenReady = false;
        SetStatus($"Create room failed: {message}");
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        joinRoomByCodeWhenReady = false;
        SetStatus("Join failed. The room code is wrong, full, or already started.");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        joinRandomWhenReady = false;
        SetStatus("No random room found. Use a room code, or enable random debug mode.");
    }

    public override void OnJoinedRoom()
    {
        SetRoomCodeLabel(PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : createdRoomCode);
        SetStatus(GetRoomStatusMessage());
        TryStartGame();
    }

    public override void OnLeftRoom()
    {
        if (NetworkCoopSession.IsReturningToLobby)
        {
            PhotonNetwork.AutomaticallySyncScene = false;
            if (PhotonNetwork.IsConnected)
            {
                PhotonNetwork.Disconnect();
                return;
            }
        }

        PhotonNetwork.AutomaticallySyncScene = true;
        Connect();
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        if (NetworkCoopSession.IsReturningToLobby)
        {
            NetworkCoopSession.CompleteReturnToLobby();
            PhotonNetwork.AutomaticallySyncScene = true;
            Connect();
            return;
        }

        SetStatus($"Disconnected: {cause}");
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus(GetRoomStatusMessage());
        TryStartGame();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        if (isLoadingGame)
            return;

        SetStatus(GetRoomStatusMessage());
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null && !PhotonNetwork.CurrentRoom.IsOpen)
            return;

        TryStartGame();
    }

    private void TryStartGame()
    {
        if (isLoadingGame || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (waitForFullRoom && PhotonNetwork.CurrentRoom.PlayerCount < maxPlayersPerRoom)
            return;

        isLoadingGame = true;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        SetStatus("Room full. Starting co-op...");
        DestroyLobbyRuntimeUi();
        NetworkCoopSession.BeginCoopGame();
        PhotonNetwork.LoadLevel(networkGameSceneName);
    }

    private string GetRoomStatusMessage()
    {
        if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            return "Not in a room.";

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        string roomName = PhotonNetwork.CurrentRoom.Name;
        if (waitForFullRoom && playerCount < maxPlayersPerRoom)
        {
            return $"Room {roomName}. Waiting for player... {playerCount}/{maxPlayersPerRoom}";
        }

        return $"Joined room {roomName}. Players: {playerCount}/{maxPlayersPerRoom}";
    }

    private bool IsReadyForMatchmaking()
    {
        return PhotonNetwork.IsConnectedAndReady && PhotonNetwork.InLobby;
    }

    private void EnsureConnectedToLobby()
    {
        if (!PhotonNetwork.IsConnected)
        {
            Connect();
            return;
        }

        if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
        {
            PhotonNetwork.JoinLobby();
        }
    }

    private void CreateRoomNow()
    {
        createRoomWhenReady = false;

        if (string.IsNullOrEmpty(createdRoomCode))
        {
            createdRoomCode = GenerateRoomCode();
            SetRoomCodeLabel(createdRoomCode);
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsOpen = true,
            IsVisible = false,
            EmptyRoomTtl = 0,
            PlayerTtl = 0,
            CleanupCacheOnLeave = true
        };

        PhotonNetwork.CreateRoom(createdRoomCode, options);
        SetStatus($"Creating room {createdRoomCode}...");
    }

    private void JoinRoomByCodeNow()
    {
        joinRoomByCodeWhenReady = false;
        PhotonNetwork.JoinRoom(pendingJoinRoomCode);
        SetStatus($"Joining room {pendingJoinRoomCode}...");
    }

    private void JoinRandomRoomNow()
    {
        joinRandomWhenReady = false;
        PhotonNetwork.JoinRandomRoom();
        SetStatus("Joining random room...");
    }

    private static string GenerateRoomCode()
    {
        return Random.Range(MinRoomCode, MaxRoomCode).ToString();
    }

    private static string NormalizeRoomCode(string rawCode)
    {
        if (string.IsNullOrEmpty(rawCode))
            return string.Empty;

        string code = string.Empty;
        for (int i = 0; i < rawCode.Length; i++)
        {
            if (char.IsDigit(rawCode[i]))
            {
                code += rawCode[i];
            }
        }

        return code;
    }

    private static bool IsValidRoomCode(string code)
    {
        return !string.IsNullOrEmpty(code) && code.Length == 6;
    }

    private void EnsureRoomCodeUi()
    {
        if (!autoBuildRoomCodeUi)
            return;

        Canvas canvas = FindLobbyCanvas();
        if (canvas == null)
            return;

        EnsureEventSystem();
        EnsureStatusText(canvas);
        BuildRoomCodePanels(canvas);
        BindLobbyEntryButtons();
    }

    private static Canvas FindLobbyCanvas()
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        Canvas fallback = null;

        for (int i = 0; i < canvases.Length; i++)
        {
            if (fallback == null)
                fallback = canvases[i];

            if (canvases[i].renderMode == RenderMode.ScreenSpaceOverlay)
                return canvases[i];
        }

        if (fallback != null)
            return fallback;

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280f, 720f);
        scaler.matchWidthOrHeight = 0.5f;

        return canvas;
    }

    private static void EnsureEventSystem()
    {
        if (FindObjectOfType<EventSystem>() != null)
            return;

        new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
    }

    private void EnsureStatusText(Canvas canvas)
    {
        if (statusText != null)
            return;

        ownsStatusText = false;

        GameObject existing = GameObject.Find(StatusTextName);
        if (existing != null)
        {
            statusText = existing.GetComponent<TextMeshProUGUI>();
            if (statusText != null)
            {
                ownsStatusText = true;
                return;
            }
        }

        GameObject statusObject = new GameObject(StatusTextName, typeof(RectTransform));
        statusObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = statusObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.15f, 0f);
        rect.anchorMax = new Vector2(0.85f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = new Vector2(0f, 18f);
        rect.sizeDelta = new Vector2(0f, 48f);

        statusText = statusObject.AddComponent<TextMeshProUGUI>();
        ownsStatusText = true;
        statusText.fontSize = 22f;
        statusText.color = new Color(0.9f, 1f, 1f, 1f);
        statusText.alignment = TextAlignmentOptions.Bottom;
        statusText.fontStyle = FontStyles.Bold;
        statusText.raycastTarget = false;
    }

    private void BuildRoomCodePanels(Canvas canvas)
    {
        if (roomCodeUiRoot != null)
            return;

        GameObject existing = GameObject.Find(RoomCodeUiName);
        if (existing != null)
        {
            roomCodeUiRoot = existing;
            roomCodeUiRoot.SetActive(false);
            return;
        }

        roomCodeUiRoot = new GameObject(RoomCodeUiName, typeof(RectTransform));
        roomCodeUiRoot.transform.SetParent(canvas.transform, false);
        StretchToParent(roomCodeUiRoot.GetComponent<RectTransform>());

        createRoomPanel = CreateOverlayPanel(roomCodeUiRoot.transform, "Create Room Panel");
        joinRoomPanel = CreateOverlayPanel(roomCodeUiRoot.transform, "Join Room Panel");

        BuildCreateRoomPanel(createRoomPanel.transform);
        BuildJoinRoomPanel(joinRoomPanel.transform);

        HideRoomCodePanels();
    }

    private void BuildCreateRoomPanel(Transform parent)
    {
        GameObject card = CreateCard(parent, "Create Room Card", new Vector2(540f, 300f));

        CreateText(card.transform, "Create Title", "Create Room", 36f, TextAlignmentOptions.Center, new Vector2(0f, 108f), new Vector2(500f, 48f), new Color(0.95f, 0.98f, 1f, 1f));
        roomCodeText = CreateText(card.transform, "Room Code Text", "Room Code: ------", 34f, TextAlignmentOptions.Center, new Vector2(0f, 50f), new Vector2(500f, 48f), new Color(1f, 0.86f, 0.35f, 1f));
        createRoomStatusText = CreateText(card.transform, "Create Status Text", "Waiting for another player...", 22f, TextAlignmentOptions.Center, new Vector2(0f, -12f), new Vector2(500f, 64f), new Color(0.72f, 1f, 1f, 1f));

        CreateButton(card.transform, "Create Back Button", "Back", new Vector2(0f, -104f), new Vector2(160f, 46f), CancelRoomCodePanel);
    }

    private void BuildJoinRoomPanel(Transform parent)
    {
        GameObject card = CreateCard(parent, "Join Room Card", new Vector2(560f, 360f));

        CreateText(card.transform, "Join Title", "Join Room", 36f, TextAlignmentOptions.Center, new Vector2(0f, 134f), new Vector2(520f, 48f), new Color(0.95f, 0.98f, 1f, 1f));
        CreateText(card.transform, "Join Tip", "Enter the creator's 6-digit room code.", 22f, TextAlignmentOptions.Center, new Vector2(0f, 82f), new Vector2(520f, 38f), new Color(0.72f, 1f, 1f, 1f));
        roomCodeInput = CreateRoomCodeInput(card.transform, new Vector2(0f, 26f), new Vector2(260f, 48f));
        joinRoomStatusText = CreateText(card.transform, "Join Status Text", "Waiting for room code.", 20f, TextAlignmentOptions.Center, new Vector2(0f, -36f), new Vector2(520f, 48f), new Color(1f, 0.82f, 0.55f, 1f));

        CreateButton(card.transform, "Confirm Join Button", "Join", new Vector2(-95f, -126f), new Vector2(150f, 46f), JoinRoomWithInputCode);
        CreateButton(card.transform, "Join Back Button", "Back", new Vector2(95f, -126f), new Vector2(150f, 46f), CancelRoomCodePanel);
    }

    private GameObject CreateOverlayPanel(Transform parent, string name)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(parent, false);
        StretchToParent(panel.GetComponent<RectTransform>());

        Image image = panel.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.58f);

        return panel;
    }

    private static GameObject CreateCard(Transform parent, string name, Vector2 size)
    {
        GameObject card = new GameObject(name, typeof(RectTransform), typeof(Image));
        card.transform.SetParent(parent, false);

        RectTransform rect = card.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Image image = card.GetComponent<Image>();
        image.color = new Color(0.03f, 0.06f, 0.1f, 0.92f);

        return card;
    }

    private TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, TextAlignmentOptions alignment, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = fontSize;
        label.color = color;
        label.alignment = alignment;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        return label;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 anchoredPosition, Vector2 size, UnityAction action)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.16f, 0.5f, 0.76f, 0.95f);

        Button button = buttonObject.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.16f, 0.5f, 0.76f, 0.95f);
        colors.highlightedColor = new Color(0.25f, 0.72f, 1f, 1f);
        colors.pressedColor = new Color(0.08f, 0.32f, 0.52f, 1f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(action);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        StretchToParent(textObject.GetComponent<RectTransform>());

        TextMeshProUGUI text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 22f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.fontStyle = FontStyles.Bold;
        text.raycastTarget = false;

        return button;
    }

    private TMP_InputField CreateRoomCodeInput(Transform parent, Vector2 anchoredPosition, Vector2 size)
    {
        GameObject inputObject = new GameObject("Room Code Input", typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        inputObject.transform.SetParent(parent, false);

        RectTransform rect = inputObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = inputObject.GetComponent<Image>();
        image.color = new Color(0.95f, 0.98f, 1f, 0.96f);

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(inputObject.transform, false);
        RectTransform textAreaRect = textArea.GetComponent<RectTransform>();
        StretchToParent(textAreaRect);
        textAreaRect.offsetMin = new Vector2(12f, 0f);
        textAreaRect.offsetMax = new Vector2(-12f, 0f);

        TextMeshProUGUI text = CreateInputText(textArea.transform, "Text", string.Empty, new Color(0.02f, 0.04f, 0.08f, 1f));
        TextMeshProUGUI placeholder = CreateInputText(textArea.transform, "Placeholder", "123456", new Color(0.25f, 0.3f, 0.36f, 0.55f));

        TMP_InputField input = inputObject.GetComponent<TMP_InputField>();
        input.textViewport = textAreaRect;
        input.textComponent = text;
        input.placeholder = placeholder;
        input.characterLimit = 6;
        input.contentType = TMP_InputField.ContentType.IntegerNumber;
        input.characterValidation = TMP_InputField.CharacterValidation.Digit;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.onSubmit.AddListener(_ => JoinRoomWithInputCode());

        return input;
    }

    private static TextMeshProUGUI CreateInputText(Transform parent, string name, string text, Color color)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        StretchToParent(textObject.GetComponent<RectTransform>());

        TextMeshProUGUI label = textObject.AddComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = 28f;
        label.color = color;
        label.alignment = TextAlignmentOptions.Center;
        label.fontStyle = FontStyles.Bold;
        label.raycastTarget = false;

        return label;
    }

    private static void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
    }

    private void BindLobbyEntryButtons()
    {
        Button[] buttons = FindObjectsOfType<Button>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            string label = GetButtonLabel(button);

            if (label == "Create Room")
            {
                SetButtonLabel(button, "Create Room");
                AddRuntimeButtonListenerIfNeeded(button, CreateRoom);
                continue;
            }

            if (label == "Join Random" || label == "Join Room")
            {
                SetButtonLabel(button, "Join Room");
                AddRuntimeButtonListenerIfNeeded(button, ShowJoinRoomPanel);
            }
        }
    }

    private static void AddRuntimeButtonListenerIfNeeded(Button button, UnityAction action)
    {
        button.onClick.RemoveAllListeners();
        if (button.onClick.GetPersistentEventCount() == 0)
        {
            button.onClick.AddListener(action);
        }
    }

    private static string GetButtonLabel(Button button)
    {
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
            return tmpText.text.Trim();

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
            return uiText.text.Trim();

        return string.Empty;
    }

    private static void SetButtonLabel(Button button, string label)
    {
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.text = label;
            return;
        }

        Text uiText = button.GetComponentInChildren<Text>(true);
        if (uiText != null)
        {
            uiText.text = label;
        }
    }

    private void ShowCreateRoomPanel()
    {
        HideRoomCodePanels();

        if (roomCodeUiRoot != null)
            roomCodeUiRoot.SetActive(true);

        if (createRoomPanel != null)
            createRoomPanel.SetActive(true);

        if (createRoomStatusText != null)
            createRoomStatusText.text = "Creating private room...";
    }

    private void HideRoomCodePanels()
    {
        if (roomCodeUiRoot != null)
            roomCodeUiRoot.SetActive(false);

        if (createRoomPanel != null)
            createRoomPanel.SetActive(false);

        if (joinRoomPanel != null)
            joinRoomPanel.SetActive(false);
    }

    private void DestroyLobbyRuntimeUi()
    {
        if (roomCodeUiRoot != null)
        {
            Destroy(roomCodeUiRoot);
            roomCodeUiRoot = null;
        }

        createRoomPanel = null;
        joinRoomPanel = null;
        roomCodeText = null;
        createRoomStatusText = null;
        joinRoomStatusText = null;
        roomCodeInput = null;

        if (ownsStatusText && statusText != null)
        {
            Destroy(statusText.gameObject);
            statusText = null;
            ownsStatusText = false;
        }
    }

    private void SetRoomCodeLabel(string code)
    {
        if (roomCodeText == null)
            return;

        roomCodeText.text = string.IsNullOrEmpty(code) ? "Room Code: ------" : $"Room Code: {code}";
    }

    void SetStatus(string message)
    {
        Debug.Log($"[NetworkLauncher] {message}");
        if (statusText != null)
        {
            statusText.text = message;
        }

        if (createRoomPanel != null && createRoomPanel.activeSelf && createRoomStatusText != null)
        {
            createRoomStatusText.text = message;
        }

        if (joinRoomPanel != null && joinRoomPanel.activeSelf && joinRoomStatusText != null)
        {
            joinRoomStatusText.text = message;
        }
    }
}
