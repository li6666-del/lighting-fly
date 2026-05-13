using Photon.Pun;
using Photon.Realtime;
using TMPro;
using UnityEngine;

public class NetworkLauncher : MonoBehaviourPunCallbacks
{
    [Header("Connection")]
    public string gameVersion = "1.0";
    public byte maxPlayersPerRoom = 2;
    public string networkGameSceneName = "NetworkGameScene";

    [Header("Optional UI")]
    public TextMeshProUGUI statusText;

    void Start()
    {
        PhotonNetwork.AutomaticallySyncScene = true;
        Connect();
    }

    public void Connect()
    {
        SetStatus("Connecting...");

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
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            SetStatus("Photon is not ready.");
            return;
        }

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom
        };

        PhotonNetwork.CreateRoom(null, options);
        SetStatus("Creating room...");
    }

    public void JoinRandomRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            SetStatus("Photon is not ready.");
            return;
        }

        PhotonNetwork.JoinRandomRoom();
        SetStatus("Joining random room...");
    }

    public void LeaveRoom()
    {
        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
    }

    public override void OnConnectedToMaster()
    {
        SetStatus("Connected. Joining lobby...");
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        SetStatus("Ready. Create or join a room.");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        SetStatus($"Create room failed: {message}");
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        SetStatus("No room found. Creating a new room...");
        CreateRoom();
    }

    public override void OnJoinedRoom()
    {
        SetStatus($"Joined room. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");

        if (PhotonNetwork.IsMasterClient)
        {
            PhotonNetwork.LoadLevel(networkGameSceneName);
        }
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        SetStatus($"Player joined. Players: {PhotonNetwork.CurrentRoom.PlayerCount}/{maxPlayersPerRoom}");
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
