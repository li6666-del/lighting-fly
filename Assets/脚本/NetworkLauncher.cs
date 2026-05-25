using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    [Header("Connection")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 2;
    public string networkGameSceneName = "GameScene1";
    public string previousMenuSceneName = NetworkCoopSession.PreviousMenuSceneName;
    public bool waitForFullRoom = true;

    [Header("Optional UI")]
    public TextMeshProUGUI statusText;

    private bool isLoadingGame;
    private bool createRoomWhenReady;
    private bool joinRandomWhenReady;

    void Start()
    {
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
        joinRandomWhenReady = false;

        if (!IsReadyForMatchmaking())
        {
            createRoomWhenReady = true;
            SetStatus("Photon is connecting. Room will be created when ready...");
            EnsureConnectedToLobby();
            return;
        }

        CreateRoomNow();
    }

    public void JoinRandomRoom()
    {
        createRoomWhenReady = false;

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

        if (joinRandomWhenReady)
        {
            JoinRandomRoomNow();
            return;
        }

        SetStatus("Ready. Create or join a room.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        createRoomWhenReady = false;
        SetStatus($"Create room failed: {message}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        joinRandomWhenReady = false;
        SetStatus("No room found. Creating a new room...");
        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
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
        NetworkCoopSession.BeginCoopGame();
        PhotonNetwork.LoadLevel(networkGameSceneName);
    }

    private string GetRoomStatusMessage()
    {
        if (!PhotonNetwork.InRoom)
            return "Not in a room.";

        int playerCount = PhotonNetwork.CurrentRoom.PlayerCount;
        if (waitForFullRoom && playerCount < maxPlayersPerRoom)
        {
            return $"Waiting for player... {playerCount}/{maxPlayersPerRoom}";
        }

        return $"Joined room. Players: {playerCount}/{maxPlayersPerRoom}";
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

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsOpen = true,
            IsVisible = true
        };

        PhotonNetwork.CreateRoom(null, options);
        SetStatus("Creating room...");
    }

    private void JoinRandomRoomNow()
    {
        joinRandomWhenReady = false;
        PhotonNetwork.JoinRandomRoom();
        SetStatus("Joining random room...");
    }

    void SetStatus(string message)
    {
        Debug.Log($"[NetworkLauncher] {message}");
        if (statusText != null)
        {
            statusText.text = message;
        }
    }
}
