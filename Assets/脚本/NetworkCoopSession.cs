using Photon.Pun;

public static class NetworkCoopSession
{
    public const string LobbySceneName = "NetworkLobby";
    public const string PreviousMenuSceneName = "SetMenu1";

    public static bool IsCoopGameActive { get; private set; }
    public static bool IsReturningToLobby { get; private set; }
    public static bool HasCoopReturnTarget { get; private set; }

    public static void BeginCoopGame()
    {
        IsCoopGameActive = true;
        IsReturningToLobby = false;
        HasCoopReturnTarget = true;
    }

    public static void EndCoopSession(bool disconnectFromPhoton = false)
    {
        IsCoopGameActive = false;
        IsReturningToLobby = false;
        HasCoopReturnTarget = false;
        PhotonNetwork.AutomaticallySyncScene = false;

        if (PhotonNetwork.InRoom)
        {
            PhotonNetwork.LeaveRoom();
        }
        else if (disconnectFromPhoton && PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public static void PrepareReturnToLobby()
    {
        IsCoopGameActive = false;
        IsReturningToLobby = true;
        HasCoopReturnTarget = true;
        PhotonNetwork.AutomaticallySyncScene = false;

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }
    }

    public static void CompleteReturnToLobby()
    {
        IsReturningToLobby = false;
        HasCoopReturnTarget = false;
    }

    public static bool ShouldReturnToLobby()
    {
        return HasCoopReturnTarget
            || IsCoopGameActive
            || PhotonNetwork.InRoom
            || NetworkCoopGameRuntime.Instance != null
            || NetworkPlayerController.LocalPlayer != null;
    }
}
