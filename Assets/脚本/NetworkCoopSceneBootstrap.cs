using Photon.Pun;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class NetworkCoopSceneBootstrap
{
    private const string CoopSceneName = "GameScene1";
    private const string NetworkPlayerPrefabName = "NetworkPlayer";

    private static readonly Vector3 FallbackPlayerOneSpawn = new Vector3(-80f, 0f, 180f);
    private static readonly Vector3 FallbackPlayerTwoSpawn = new Vector3(20f, 0f, 180f);
    private static readonly Vector3 PlayerOneOffset = new Vector3(-45f, 0f, 0f);
    private static readonly Vector3 PlayerTwoOffset = new Vector3(45f, 0f, 0f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        TryBootstrap(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        TryBootstrap(scene);
    }

    private static void TryBootstrap(Scene scene)
    {
        if (scene.name != CoopSceneName)
            return;

        if (!PhotonNetwork.InRoom)
            return;

        NetworkCoopSession.BeginCoopGame();

        if (!NetworkCoopSession.IsCoopGameActive)
            return;

        Vector3 singlePlayerAnchor = GetSinglePlayerAnchorPosition();
        EnemySpawner sourceSpawner = FindSourceEnemySpawner();
        bool runtimeAlreadyExists = Object.FindObjectOfType<NetworkCoopGameRuntime>() != null;

        CreateNetworkRuntime(sourceSpawner);
        if (!runtimeAlreadyExists)
        {
            DisableSinglePlayerCombat();
            ClearSinglePlayerEnemies();
            DisableSinglePlayerObjects();
        }

        if (!HasLocalNetworkPlayer())
        {
            SpawnNetworkPlayer(singlePlayerAnchor);
        }
    }

    private static EnemySpawner FindSourceEnemySpawner()
    {
        EnemySpawner[] spawners = Object.FindObjectsOfType<EnemySpawner>(true);
        EnemySpawner fallbackSpawner = null;
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner == null)
                continue;

            if (fallbackSpawner == null)
            {
                fallbackSpawner = spawner;
            }

            if (spawner.enemyPrefab != null)
            {
                return spawner;
            }
        }

        return fallbackSpawner;
    }

    private static void CreateNetworkRuntime(EnemySpawner sourceSpawner)
    {
        NetworkCoopGameRuntime existing = Object.FindObjectOfType<NetworkCoopGameRuntime>();
        if (existing != null)
        {
            if (!existing.HasSpawnConfiguration && sourceSpawner != null)
            {
                existing.ConfigureFromSpawner(sourceSpawner);
            }
            return;
        }

        GameObject runtimeObject = new GameObject("Network Coop Runtime");
        NetworkCoopGameRuntime runtime = runtimeObject.AddComponent<NetworkCoopGameRuntime>();
        runtime.ConfigureFromSpawner(sourceSpawner);
    }

    private static bool HasLocalNetworkPlayer()
    {
        NetworkPlayerController[] players = Object.FindObjectsOfType<NetworkPlayerController>(true);
        foreach (NetworkPlayerController player in players)
        {
            if (player == null)
                continue;

            PhotonView view = player.GetComponent<PhotonView>();
            if (view != null && view.IsMine)
                return true;
        }

        return false;
    }

    private static void DisableSinglePlayerCombat()
    {
        EnemySpawner[] spawners = Object.FindObjectsOfType<EnemySpawner>(true);
        foreach (EnemySpawner spawner in spawners)
        {
            if (spawner != null)
            {
                spawner.enabled = false;
            }
        }
    }

    private static void ClearSinglePlayerEnemies()
    {
        DestroyObjectsWithTag("Enemy");
        DestroyObjectsWithTag("EnemyBullet");
        DestroyObjectsWithTag("Boss");

        Enemy[] enemies = Object.FindObjectsOfType<Enemy>(true);
        foreach (Enemy enemy in enemies)
        {
            if (enemy != null)
            {
                Object.Destroy(enemy.gameObject);
            }
        }

        BossController[] bosses = Object.FindObjectsOfType<BossController>(true);
        foreach (BossController boss in bosses)
        {
            if (boss != null)
            {
                Object.Destroy(boss.gameObject);
            }
        }

        EnemyBulletLogic[] bullets = Object.FindObjectsOfType<EnemyBulletLogic>(true);
        foreach (EnemyBulletLogic bullet in bullets)
        {
            if (bullet != null)
            {
                Object.Destroy(bullet.gameObject);
            }
        }
    }

    private static void DestroyObjectsWithTag(string tagName)
    {
        GameObject[] objects;
        try
        {
            objects = GameObject.FindGameObjectsWithTag(tagName);
        }
        catch (UnityException)
        {
            return;
        }

        foreach (GameObject obj in objects)
        {
            if (obj != null)
            {
                Object.Destroy(obj);
            }
        }
    }

    private static void DisableSinglePlayerObjects()
    {
        GameObject[] players;
        try
        {
            players = GameObject.FindGameObjectsWithTag("Player");
        }
        catch (UnityException)
        {
            return;
        }

        foreach (GameObject player in players)
        {
            if (player == null)
                continue;

            if (player.GetComponent<PhotonView>() != null)
                continue;

            player.SetActive(false);
        }
    }

    private static Vector3 GetSinglePlayerAnchorPosition()
    {
        GameObject[] players;
        try
        {
            players = GameObject.FindGameObjectsWithTag("Player");
        }
        catch (UnityException)
        {
            return Vector3.zero;
        }

        foreach (GameObject player in players)
        {
            if (player == null)
                continue;

            if (player.GetComponent<PhotonView>() != null)
                continue;

            return player.transform.position;
        }

        return Vector3.zero;
    }

    private static void SpawnNetworkPlayer(Vector3 singlePlayerAnchor)
    {
        if (PhotonNetwork.LocalPlayer == null)
            return;

        bool hasAnchor = singlePlayerAnchor != Vector3.zero;
        bool isPlayerOne = PhotonNetwork.LocalPlayer.ActorNumber % 2 == 1;

        Vector3 spawnPosition = hasAnchor
            ? singlePlayerAnchor + (isPlayerOne ? PlayerOneOffset : PlayerTwoOffset)
            : (isPlayerOne ? FallbackPlayerOneSpawn : FallbackPlayerTwoSpawn);

        PhotonNetwork.Instantiate(NetworkPlayerPrefabName, spawnPosition, Quaternion.identity);
    }
}
