using Photon.Pun;
using UnityEngine;

public class NetworkPlayerSpawner : MonoBehaviour
{
    public string networkPlayerPrefabName = "NetworkPlayer";
    public Vector3 playerOneSpawn = new Vector3(-60f, 0f, -180f);
    public Vector3 playerTwoSpawn = new Vector3(60f, 0f, -180f);

    void Start()
    {
        if (!PhotonNetwork.InRoom)
        {
            Debug.LogWarning("NetworkPlayerSpawner: not in a Photon room.");
            return;
        }

        int playerIndex = GetLocalPlayerIndex();
        Vector3 spawnPosition = playerIndex == 0
            ? playerOneSpawn
            : playerTwoSpawn;

        PhotonNetwork.Instantiate(networkPlayerPrefabName, spawnPosition, Quaternion.identity);
    }

    private int GetLocalPlayerIndex()
    {
        int index = 0;
        foreach (int actorNumber in PhotonNetwork.CurrentRoom.Players.Keys)
        {
            if (actorNumber < PhotonNetwork.LocalPlayer.ActorNumber)
            {
                index++;
            }
        }

        return index;
    }
}
