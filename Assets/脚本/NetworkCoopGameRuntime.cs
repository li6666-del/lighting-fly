using System.Collections;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class NetworkCoopGameRuntime : MonoBehaviourPunCallbacks, IOnEventCallback
{
    private const byte SpawnEnemyEventCode = 41;
    private const byte DestroyEnemyEventCode = 42;
    private const byte RequestDestroyEnemyEventCode = 43;
    private const byte SharedHealthEventCode = 44;
    private const byte SpawnBossEventCode = 45;
    private const byte RequestBossDamageEventCode = 46;
    private const byte BossStateEventCode = 47;
    private const byte RequestSharedDamageEventCode = 48;
    private const byte EnemyTransformStateEventCode = 49;
    private const byte BossTransformStateEventCode = 50;
    private const byte SpawnEnemyBulletEventCode = 51;
    private const int MaxRememberedDestroyedEnemies = 1024;
    private const int MaxSharedHealth = 100;
    private const int MaxBossDamagePerRequest = 25;
    private const int MaxSharedDamagePerRequest = 100;
    private const float StateSyncInterval = 0.08f;

    public static NetworkCoopGameRuntime Instance { get; private set; }
    public static bool IsActive => Instance != null && NetworkCoopSession.IsCoopGameActive && PhotonNetwork.InRoom;
    public bool HasSpawnConfiguration => enemyPrefab != null;

    private readonly Dictionary<int, GameObject> enemiesById = new Dictionary<int, GameObject>();
    private readonly HashSet<int> destroyedEnemyIds = new HashSet<int>();
    private readonly Queue<int> destroyedEnemyOrder = new Queue<int>();
    private readonly Dictionary<int, GameObject> bossesById = new Dictionary<int, GameObject>();
    private readonly Dictionary<int, int> bossHpById = new Dictionary<int, int>();
    private readonly Dictionary<int, int> bossMaxHpById = new Dictionary<int, int>();
    private readonly HashSet<int> destroyedBossIds = new HashSet<int>();

    private GameObject enemyPrefab;
    private GameObject enemyBulletPrefab;
    private float spawnInterval = 0.55f;
    private int enemiesPerWave = 2;
    private float extraEnemyChance = 0.35f;
    private float spawnZ = 600f;
    private float minX = -250f;
    private float maxX = 150f;
    private float minSpacing = 45f;
    private GameObject bossPrefab;
    private int bossTriggerScore = 50;
    private int bossScoreInterval = 200;
    private Vector3 bossSpawnPosition = new Vector3(-50f, 0f, 520f);
    private float bossScaleMultiplier = 12f;
    private float bossHorizontalSpacing = 140f;
    private int maxBossCount = 3;

    private float nextSpawnTime;
    private int nextSpawnId = 1;
    private int nextBossId = 1;
    private int nextBossScore = 50;
    private int activeBossCount;
    private int sharedHealth = MaxSharedHealth;
    private bool sharedGameOver;
    private float nextStateSyncTime;
    private float nextConfigRecoveryTime;
    private bool warnedMissingEnemyPrefab;
    private bool warnedMissingEnemyBulletPrefab;
    private bool loggedFirstEnemyWave;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[NetworkCoopGameRuntime] Duplicate runtime detected; destroying the newer instance.");
            enabled = false;
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public override void OnEnable()
    {
        if (Instance != null && Instance != this)
            return;

        base.OnEnable();
        PhotonNetwork.AddCallbackTarget(this);
    }

    public override void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        base.OnDisable();
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public override void OnMasterClientSwitched(Player newMasterClient)
    {
        base.OnMasterClientSwitched(newMasterClient);

        if (!PhotonNetwork.IsMasterClient)
            return;

        RemoveRemoteSmoothersForMaster();
        nextSpawnTime = Time.time + 0.4f;
        nextStateSyncTime = Time.time;
        activeBossCount = 0;
        foreach (KeyValuePair<int, GameObject> bossEntry in bossesById)
        {
            if (bossEntry.Value != null && !destroyedBossIds.Contains(bossEntry.Key))
            {
                activeBossCount++;
            }
        }

        nextSpawnId = Mathf.Max(nextSpawnId, GetNextAvailableId(enemiesById, destroyedEnemyIds));
        nextBossId = Mathf.Max(nextBossId, GetNextAvailableId(bossesById, destroyedBossIds));
        nextBossScore = GetNextBossScoreAfterCurrentScore();
        Debug.Log($"[NetworkCoopGameRuntime] Local client became MasterClient. nextSpawnId={nextSpawnId}, nextBossId={nextBossId}, nextBossScore={nextBossScore}, activeBossCount={activeBossCount}");
    }

    public void ConfigureFromSpawner(EnemySpawner spawner)
    {
        sharedHealth = MaxSharedHealth;
        sharedGameOver = false;
        ScoreManager.score = 0;
        BloodManager.blood = MaxSharedHealth;
        enemiesById.Clear();
        destroyedEnemyIds.Clear();
        destroyedEnemyOrder.Clear();
        bossesById.Clear();
        bossHpById.Clear();
        bossMaxHpById.Clear();
        destroyedBossIds.Clear();
        nextSpawnId = 1;
        nextBossId = 1;
        activeBossCount = 0;

        if (spawner == null)
        {
            Debug.LogWarning("[NetworkCoopGameRuntime] No EnemySpawner found, co-op enemies cannot spawn.");
            return;
        }

        enemyPrefab = spawner.enemyPrefab;
        enemyBulletPrefab = GetEnemyBulletPrefab(spawner.enemyPrefab);
        spawnInterval = spawner.spawnInterval;
        enemiesPerWave = spawner.enemiesPerWave;
        extraEnemyChance = spawner.extraEnemyChance;
        spawnZ = spawner.spawnZ;
        minX = spawner.minX;
        maxX = spawner.maxX;
        minSpacing = spawner.minSpacing;
        bossPrefab = spawner.bossPrefab;
        bossTriggerScore = spawner.bossTriggerScore;
        bossScoreInterval = spawner.bossScoreInterval;
        bossSpawnPosition = spawner.bossSpawnPosition;
        bossScaleMultiplier = spawner.bossScaleMultiplier;
        bossHorizontalSpacing = spawner.bossHorizontalSpacing;
        maxBossCount = spawner.maxBossCount;
        nextBossScore = Mathf.Max(1, bossTriggerScore);
        nextSpawnTime = Time.time + 0.4f;
        warnedMissingEnemyPrefab = false;
        warnedMissingEnemyBulletPrefab = false;
        loggedFirstEnemyWave = false;
        nextConfigRecoveryTime = 0f;

        Debug.Log($"[NetworkCoopGameRuntime] Configured. enemyPrefab={(enemyPrefab != null ? enemyPrefab.name : "NULL")}, spawnInterval={spawnInterval}, isMaster={PhotonNetwork.IsMasterClient}");
    }

    void Update()
    {
        if (sharedGameOver || !PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            return;

        if (enemyPrefab == null && Time.time >= nextConfigRecoveryTime)
        {
            TryRecoverSpawnConfiguration();
            nextConfigRecoveryTime = Time.time + 1f;
        }

        if (Time.time >= nextStateSyncTime)
        {
            RaiseTransformStates();
            nextStateSyncTime = Time.time + StateSyncInterval;
        }

        if (bossPrefab != null && ScoreManager.score >= nextBossScore)
        {
            SpawnNetworkBossWave(GetBossCountForScore(nextBossScore));
            AdvanceBossScore();
        }

        if (enemyPrefab != null && Time.time >= nextSpawnTime)
        {
            SpawnNetworkWave();
            nextSpawnTime = Time.time + GetCurrentSpawnInterval();
        }
        else if (enemyPrefab == null && !warnedMissingEnemyPrefab)
        {
            warnedMissingEnemyPrefab = true;
            Debug.LogWarning("[NetworkCoopGameRuntime] enemyPrefab is missing on MasterClient, no co-op enemies will spawn.");
        }
    }

    private void SpawnNetworkWave()
    {
        if (!loggedFirstEnemyWave)
        {
            loggedFirstEnemyWave = true;
            Debug.Log($"[NetworkCoopGameRuntime] Enemy spawn loop started. interval={GetCurrentSpawnInterval():0.00}, waveBase={enemiesPerWave}, isMaster={PhotonNetwork.IsMasterClient}");
        }

        int count = Mathf.Max(1, enemiesPerWave + DifficultyManager.EnemyWaveBonus);
        float bonusChance = Mathf.Clamp01(extraEnemyChance + DifficultyManager.ExtraEnemyChanceBonus);
        if (Random.value < bonusChance)
        {
            count++;
        }

        float lastX = float.NaN;
        for (int i = 0; i < count; i++)
        {
            float x = PickSpawnX(lastX);
            lastX = x;

            float zOffset = Random.Range(-35f, 35f);
            int spawnId = nextSpawnId++;
            RaiseSpawnEnemy(spawnId, new Vector3(x, 0f, spawnZ + zOffset));
        }
    }

    private float GetCurrentSpawnInterval()
    {
        return Mathf.Max(0.18f, spawnInterval * DifficultyManager.SpawnIntervalMultiplier);
    }

    private float PickSpawnX(float lastX)
    {
        for (int attempt = 0; attempt < 8; attempt++)
        {
            float x = Random.Range(minX, maxX);
            if (float.IsNaN(lastX) || Mathf.Abs(x - lastX) >= minSpacing)
            {
                return x;
            }
        }

        return Random.Range(minX, maxX);
    }

    private void RaiseSpawnEnemy(int spawnId, Vector3 position)
    {
        object[] content =
        {
            spawnId,
            position.x,
            position.y,
            position.z
        };

        PhotonNetwork.RaiseEvent(
            SpawnEnemyEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    private int GetBossCountForScore(int scoreThreshold)
    {
        int interval = Mathf.Max(1, bossScoreInterval);
        if (scoreThreshold < interval)
            return 1;

        return Mathf.Max(2, scoreThreshold / interval + 1);
    }

    private void AdvanceBossScore()
    {
        int interval = Mathf.Max(1, bossScoreInterval);
        if (nextBossScore < interval)
        {
            nextBossScore = interval;
        }
        else
        {
            nextBossScore += interval;
        }
    }

    private void SpawnNetworkBossWave(int requestedBossCount)
    {
        if (bossPrefab == null)
            return;

        int count = Mathf.Clamp(requestedBossCount, 1, Mathf.Max(1, maxBossCount));
        float overflowHealthMultiplier = DifficultyManager.GetBossOverflowHealthMultiplier(requestedBossCount, maxBossCount);
        float centerIndex = (count - 1) * 0.5f;

        for (int i = 0; i < count; i++)
        {
            int bossId = nextBossId++;
            int maxHp = CalculateBossMaxHp(overflowHealthMultiplier);
            int uiSlot = activeBossCount;
            Vector3 position = bossSpawnPosition + Vector3.right * ((i - centerIndex) * bossHorizontalSpacing);

            activeBossCount++;
            bossMaxHpById[bossId] = maxHp;
            bossHpById[bossId] = maxHp;
            RaiseSpawnBoss(bossId, position, overflowHealthMultiplier, uiSlot, maxHp);
        }
    }

    private int CalculateBossMaxHp(float extraHealthMultiplier)
    {
        BossController prefabBoss = bossPrefab != null ? bossPrefab.GetComponent<BossController>() : null;
        int baseHp = prefabBoss != null ? prefabBoss.maxHp : 90;
        float difficultyMultiplier = DifficultyManager.BossHealthMultiplier * Mathf.Max(1f, extraHealthMultiplier);
        return Mathf.Max(1, Mathf.RoundToInt(baseHp * difficultyMultiplier));
    }

    private void RaiseSpawnBoss(int bossId, Vector3 position, float extraHealthMultiplier, int uiSlot, int maxHp)
    {
        object[] content =
        {
            bossId,
            position.x,
            position.y,
            position.z,
            Mathf.Max(1f, bossScaleMultiplier),
            Mathf.Max(1f, extraHealthMultiplier),
            Mathf.Max(0, uiSlot),
            Mathf.Max(1, maxHp)
        };

        PhotonNetwork.RaiseEvent(
            SpawnBossEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    public static bool ReportEnemyDestroyed(
        NetworkCoopEnemyIdentity enemy,
        Vector3 hitPosition,
        Vector3 hitNormal,
        int scoreValue,
        int healValue,
        bool grantsSkillCharge
    )
    {
        if (!IsActive || enemy == null)
            return false;

        int killerActorNumber = PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;

        if (PhotonNetwork.IsMasterClient)
        {
            Instance.TryConfirmEnemyDestroyed(enemy.spawnId, hitPosition, hitNormal, scoreValue, healValue, grantsSkillCharge, killerActorNumber);
        }
        else
        {
            Instance.RaiseDestroyEnemyRequest(enemy.spawnId, hitPosition, hitNormal, scoreValue, healValue, grantsSkillCharge, killerActorNumber);
        }

        return true;
    }

    private void RaiseDestroyEnemyRequest(
        int spawnId,
        Vector3 hitPosition,
        Vector3 hitNormal,
        int scoreValue,
        int healValue,
        bool grantsSkillCharge,
        int killerActorNumber
    )
    {
        object[] content =
        {
            spawnId,
            hitPosition.x,
            hitPosition.y,
            hitPosition.z,
            hitNormal.x,
            hitNormal.y,
            hitNormal.z,
            scoreValue,
            healValue,
            grantsSkillCharge,
            killerActorNumber
        };

        PhotonNetwork.RaiseEvent(
            RequestDestroyEnemyEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    private void TryConfirmEnemyDestroyed(
        int spawnId,
        Vector3 hitPosition,
        Vector3 hitNormal,
        int scoreValue,
        int healValue,
        bool grantsSkillCharge,
        int killerActorNumber
    )
    {
        if (destroyedEnemyIds.Contains(spawnId))
            return;

        if (!enemiesById.ContainsKey(spawnId))
            return;

        RaiseDestroyEnemy(spawnId, hitPosition, hitNormal, scoreValue, healValue, grantsSkillCharge, killerActorNumber);
    }

    private void RaiseDestroyEnemy(
        int spawnId,
        Vector3 hitPosition,
        Vector3 hitNormal,
        int scoreValue,
        int healValue,
        bool grantsSkillCharge,
        int killerActorNumber
    )
    {
        object[] content =
        {
            spawnId,
            hitPosition.x,
            hitPosition.y,
            hitPosition.z,
            hitNormal.x,
            hitNormal.y,
            hitNormal.z,
            scoreValue,
            healValue,
            grantsSkillCharge,
            killerActorNumber
        };

        PhotonNetwork.RaiseEvent(
            DestroyEnemyEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    public static bool ReportBossDamaged(NetworkCoopBossIdentity boss, int damage, Vector3 hitPosition)
    {
        if (!IsActive || boss == null || damage <= 0)
            return false;

        if (PhotonNetwork.IsMasterClient)
        {
            Instance.TryConfirmBossDamage(boss.bossId, damage, hitPosition);
        }
        else
        {
            Instance.RaiseBossDamageRequest(boss.bossId, damage, hitPosition);
        }

        return true;
    }

    public static bool SpawnEnemyBullet(Vector3 position, Quaternion rotation, GameObject fallbackPrefab)
    {
        if (!IsActive)
            return false;

        if (!PhotonNetwork.IsMasterClient)
            return true;

        if (Instance.enemyBulletPrefab == null && fallbackPrefab != null)
        {
            Instance.enemyBulletPrefab = fallbackPrefab;
        }

        if (Instance.enemyBulletPrefab == null)
        {
            if (!Instance.warnedMissingEnemyBulletPrefab)
            {
                Instance.warnedMissingEnemyBulletPrefab = true;
                Debug.LogWarning("[NetworkCoopGameRuntime] enemyBulletPrefab is missing, enemy bullets cannot be spawned in co-op.");
            }
            return false;
        }

        Instance.SpawnEnemyBulletLocal(position, rotation);
        Instance.RaiseEnemyBullet(position, rotation);
        return true;
    }

    private void RaiseEnemyBullet(Vector3 position, Quaternion rotation)
    {
        object[] content =
        {
            position.x,
            position.y,
            position.z,
            rotation.x,
            rotation.y,
            rotation.z,
            rotation.w
        };

        PhotonNetwork.RaiseEvent(
            SpawnEnemyBulletEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendUnreliable
        );
    }

    private void SpawnEnemyBulletLocal(Vector3 position, Quaternion rotation)
    {
        if (enemyBulletPrefab == null)
            return;

        Instantiate(enemyBulletPrefab, position, rotation);
        CombatEffects.SpawnEnemyMuzzleFlash(position, rotation);
    }

    private void RaiseBossDamageRequest(int bossId, int damage, Vector3 hitPosition)
    {
        object[] content =
        {
            bossId,
            Mathf.Max(1, damage),
            hitPosition.x,
            hitPosition.y,
            hitPosition.z
        };

        PhotonNetwork.RaiseEvent(
            RequestBossDamageEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    private void TryConfirmBossDamage(int bossId, int damage, Vector3 hitPosition)
    {
        if (destroyedBossIds.Contains(bossId))
            return;

        if (!bossesById.ContainsKey(bossId))
            return;

        if (!bossMaxHpById.TryGetValue(bossId, out int maxHp))
            return;

        int currentHp = bossHpById.TryGetValue(bossId, out int hp) ? hp : maxHp;
        currentHp = Mathf.Max(0, currentHp - Mathf.Max(1, damage));
        bossHpById[bossId] = currentHp;

        bool dead = currentHp <= 0;
        RaiseBossState(bossId, currentHp, maxHp, hitPosition, dead);
    }

    private void RaiseBossState(int bossId, int currentHp, int maxHp, Vector3 hitPosition, bool dead)
    {
        object[] content =
        {
            bossId,
            Mathf.Max(0, currentHp),
            Mathf.Max(1, maxHp),
            hitPosition.x,
            hitPosition.y,
            hitPosition.z,
            dead
        };

        PhotonNetwork.RaiseEvent(
            BossStateEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    public static bool ReportPlayerDamaged(int damage, Vector3 hitPosition, Vector3 hitNormal, bool blockedByShield = false)
    {
        if (!IsActive || damage <= 0)
            return false;

        CombatEffects.SpawnHit(hitPosition, hitNormal);

        if (blockedByShield)
            return true;

        if (PhotonNetwork.IsMasterClient)
        {
            Instance.ApplySharedDamage(damage);
        }
        else
        {
            Instance.RaiseSharedDamageRequest(damage);
        }

        return true;
    }

    public static bool ReportPlayerCrashed(int damage, Vector3 crashPoint)
    {
        if (!IsActive)
            return false;

        CombatEffects.SpawnExplosion(crashPoint);

        if (PhotonNetwork.IsMasterClient)
        {
            Instance.ApplySharedDamage(Mathf.Max(1, damage));
        }
        else
        {
            Instance.RaiseSharedDamageRequest(damage);
        }

        return true;
    }

    private void RaiseSharedDamageRequest(int damage)
    {
        object[] content =
        {
            Mathf.Max(1, damage)
        };

        PhotonNetwork.RaiseEvent(
            RequestSharedDamageEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.MasterClient },
            SendOptions.SendReliable
        );
    }

    private void ApplySharedDamage(int damage)
    {
        if (sharedGameOver)
            return;

        sharedHealth = Mathf.Max(0, sharedHealth - Mathf.Max(1, damage));
        sharedGameOver = sharedHealth <= 0;
        RaiseSharedHealth(sharedHealth, sharedGameOver);
    }

    private void RaiseSharedHealth(int health, bool gameOver)
    {
        object[] content =
        {
            health,
            gameOver
        };

        PhotonNetwork.RaiseEvent(
            SharedHealthEventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.All },
            SendOptions.SendReliable
        );
    }

    private void RaiseTransformStates()
    {
        foreach (KeyValuePair<int, GameObject> pair in enemiesById)
        {
            if (pair.Value != null)
            {
                RaiseTransformState(EnemyTransformStateEventCode, pair.Key, pair.Value.transform);
            }
        }

        foreach (KeyValuePair<int, GameObject> pair in bossesById)
        {
            if (pair.Value != null)
            {
                RaiseTransformState(BossTransformStateEventCode, pair.Key, pair.Value.transform);
            }
        }
    }

    private void RaiseTransformState(byte eventCode, int id, Transform target)
    {
        Vector3 position = target.position;
        Quaternion rotation = target.rotation;
        object[] content =
        {
            id,
            position.x,
            position.y,
            position.z,
            rotation.x,
            rotation.y,
            rotation.z,
            rotation.w
        };

        PhotonNetwork.RaiseEvent(
            eventCode,
            content,
            new RaiseEventOptions { Receivers = ReceiverGroup.Others },
            SendOptions.SendUnreliable
        );
    }

    public void OnEvent(EventData photonEvent)
    {
        object[] data = photonEvent.CustomData as object[];
        if (data == null)
            return;

        if (photonEvent.Code == SpawnEnemyEventCode)
        {
            HandleSpawnEnemy(data);
        }
        else if (photonEvent.Code == RequestDestroyEnemyEventCode)
        {
            HandleDestroyEnemyRequest(data);
        }
        else if (photonEvent.Code == DestroyEnemyEventCode)
        {
            HandleDestroyEnemy(data);
        }
        else if (photonEvent.Code == SharedHealthEventCode)
        {
            HandleSharedHealth(data);
        }
        else if (photonEvent.Code == SpawnBossEventCode)
        {
            HandleSpawnBoss(data);
        }
        else if (photonEvent.Code == RequestBossDamageEventCode)
        {
            HandleBossDamageRequest(data);
        }
        else if (photonEvent.Code == BossStateEventCode)
        {
            HandleBossState(data);
        }
        else if (photonEvent.Code == RequestSharedDamageEventCode)
        {
            HandleSharedDamageRequest(data);
        }
        else if (photonEvent.Code == EnemyTransformStateEventCode)
        {
            HandleTransformState(data, enemiesById);
        }
        else if (photonEvent.Code == BossTransformStateEventCode)
        {
            HandleTransformState(data, bossesById);
        }
        else if (photonEvent.Code == SpawnEnemyBulletEventCode)
        {
            HandleSpawnEnemyBullet(data);
        }
    }

    private void HandleDestroyEnemyRequest(object[] data)
    {
        if (!PhotonNetwork.IsMasterClient
            || !TryGetInt(data, 0, out int spawnId)
            || !TryGetVector3(data, 1, out Vector3 hitPosition)
            || !TryGetVector3(data, 4, out Vector3 hitNormal)
            || !TryGetInt(data, 7, out int requestedScoreValue)
            || !TryGetInt(data, 8, out int requestedHealValue)
            || !TryGetBool(data, 9, out bool requestedSkillCharge)
            || spawnId <= 0)
        {
            return;
        }

        int scoreValue = Mathf.Clamp(requestedScoreValue, 0, 1);
        int healValue = Mathf.Clamp(requestedHealValue, 0, 5);
        bool grantsSkillCharge = requestedSkillCharge && scoreValue > 0;
        int killerActorNumber = TryGetInt(data, 10, out int parsedActorNumber) ? parsedActorNumber : -1;

        TryConfirmEnemyDestroyed(spawnId, hitPosition, hitNormal, scoreValue, healValue, grantsSkillCharge, killerActorNumber);
    }

    private void HandleBossDamageRequest(object[] data)
    {
        if (!PhotonNetwork.IsMasterClient
            || !TryGetInt(data, 0, out int bossId)
            || !TryGetInt(data, 1, out int requestedDamage)
            || !TryGetVector3(data, 2, out Vector3 hitPosition)
            || bossId <= 0
            || requestedDamage <= 0)
        {
            return;
        }

        int damage = Mathf.Clamp(requestedDamage, 1, MaxBossDamagePerRequest);
        TryConfirmBossDamage(bossId, damage, hitPosition);
    }

    private void HandleSharedDamageRequest(object[] data)
    {
        if (!PhotonNetwork.IsMasterClient || !TryGetInt(data, 0, out int requestedDamage) || requestedDamage <= 0)
            return;

        ApplySharedDamage(Mathf.Clamp(requestedDamage, 1, MaxSharedDamagePerRequest));
    }

    private void HandleTransformState(object[] data, Dictionary<int, GameObject> objectsById)
    {
        if (PhotonNetwork.IsMasterClient
            || !TryGetInt(data, 0, out int id)
            || !TryGetVector3(data, 1, out Vector3 position)
            || !TryGetQuaternion(data, 4, out Quaternion rotation))
        {
            return;
        }

        if (!objectsById.TryGetValue(id, out GameObject target) || target == null)
            return;

        NetworkCoopTransformSmoother smoother = target.GetComponent<NetworkCoopTransformSmoother>();
        if (smoother == null)
        {
            smoother = target.AddComponent<NetworkCoopTransformSmoother>();
            smoother.ResetTo(target.transform.position, target.transform.rotation);
        }

        smoother.SetTarget(position, rotation);
    }

    private void AddRemoteSmootherIfNeeded(GameObject target, Vector3 position, Quaternion rotation)
    {
        if (PhotonNetwork.IsMasterClient || target == null)
            return;

        NetworkCoopTransformSmoother smoother = target.GetComponent<NetworkCoopTransformSmoother>();
        if (smoother == null)
        {
            smoother = target.AddComponent<NetworkCoopTransformSmoother>();
        }

        smoother.ResetTo(position, rotation);
    }

    private void RemoveRemoteSmoothersForMaster()
    {
        foreach (KeyValuePair<int, GameObject> pair in enemiesById)
        {
            RemoveRemoteSmoother(pair.Value);
        }

        foreach (KeyValuePair<int, GameObject> pair in bossesById)
        {
            RemoveRemoteSmoother(pair.Value);
        }
    }

    private void RemoveRemoteSmoother(GameObject target)
    {
        if (target == null)
            return;

        NetworkCoopTransformSmoother smoother = target.GetComponent<NetworkCoopTransformSmoother>();
        if (smoother != null)
        {
            Destroy(smoother);
        }
    }

    private void HandleSpawnEnemyBullet(object[] data)
    {
        if (PhotonNetwork.IsMasterClient
            || !TryGetVector3(data, 0, out Vector3 position)
            || !TryGetQuaternion(data, 3, out Quaternion rotation))
        {
            return;
        }

        SpawnEnemyBulletLocal(position, rotation);
    }

    private void HandleSpawnEnemy(object[] data)
    {
        if (enemyPrefab == null)
        {
            TryRecoverSpawnConfiguration();
        }

        if (enemyPrefab == null
            || !TryGetInt(data, 0, out int spawnId)
            || !TryGetVector3(data, 1, out Vector3 position)
            || spawnId <= 0)
        {
            return;
        }

        nextSpawnId = Mathf.Max(nextSpawnId, spawnId + 1);
        if (enemiesById.ContainsKey(spawnId) || destroyedEnemyIds.Contains(spawnId))
            return;

        GameObject enemyObject = Instantiate(enemyPrefab, position, Quaternion.Euler(0f, 180f, 0f));
        NetworkCoopEnemyIdentity identity = enemyObject.GetComponent<NetworkCoopEnemyIdentity>();
        if (identity == null)
        {
            identity = enemyObject.AddComponent<NetworkCoopEnemyIdentity>();
        }

        identity.spawnId = spawnId;
        enemiesById[spawnId] = enemyObject;
        AddRemoteSmootherIfNeeded(enemyObject, position, Quaternion.Euler(0f, 180f, 0f));
    }

    private bool TryRecoverSpawnConfiguration()
    {
        EnemySpawner[] spawners = FindObjectsOfType<EnemySpawner>(true);
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
                ConfigureFromSpawner(spawner);
                Debug.Log($"[NetworkCoopGameRuntime] Recovered co-op spawn config from {spawner.name}.");
                return true;
            }
        }

        if (fallbackSpawner != null)
        {
            ConfigureFromSpawner(fallbackSpawner);
            return enemyPrefab != null;
        }

        return false;
    }

    private void HandleSpawnBoss(object[] data)
    {
        if (bossPrefab == null
            || !TryGetInt(data, 0, out int bossId)
            || !TryGetVector3(data, 1, out Vector3 position)
            || !TryGetFloat(data, 4, out float scaleMultiplier)
            || !TryGetFloat(data, 5, out float extraHealthMultiplier)
            || !TryGetInt(data, 6, out int uiSlot)
            || !TryGetInt(data, 7, out int maxHp)
            || bossId <= 0)
        {
            return;
        }

        nextBossId = Mathf.Max(nextBossId, bossId + 1);
        if (bossesById.ContainsKey(bossId) || destroyedBossIds.Contains(bossId))
            return;

        scaleMultiplier = Mathf.Clamp(scaleMultiplier, 1f, 30f);
        extraHealthMultiplier = Mathf.Clamp(extraHealthMultiplier, 1f, 20f);
        maxHp = Mathf.Max(1, maxHp);

        GameObject bossObject = Instantiate(bossPrefab, position, Quaternion.Euler(0f, 180f, 0f));
        bossObject.transform.localScale *= Mathf.Max(1f, scaleMultiplier);

        NetworkCoopBossIdentity identity = bossObject.GetComponent<NetworkCoopBossIdentity>();
        if (identity == null)
        {
            identity = bossObject.AddComponent<NetworkCoopBossIdentity>();
        }

        BossController boss = bossObject.GetComponent<BossController>();
        if (boss == null)
        {
            boss = bossObject.AddComponent<BossController>();
        }

        identity.bossId = bossId;
        boss.extraHealthMultiplier = Mathf.Max(1f, extraHealthMultiplier);
        boss.Initialize(null, enemyBulletPrefab, Mathf.Max(0, uiSlot));

        bossesById[bossId] = bossObject;
        AddRemoteSmootherIfNeeded(bossObject, position, Quaternion.Euler(0f, 180f, 0f));
        bossMaxHpById[bossId] = Mathf.Max(1, maxHp);
        if (!bossHpById.ContainsKey(bossId))
        {
            bossHpById[bossId] = Mathf.Max(1, maxHp);
        }

        StartCoroutine(ApplyBossHealthNextFrame(boss, bossHpById[bossId], bossMaxHpById[bossId]));
    }

    private IEnumerator ApplyBossHealthNextFrame(BossController boss, int hp, int maxHp)
    {
        yield return null;

        if (boss != null)
        {
            boss.ApplyNetworkHealth(hp, maxHp);
        }
    }

    private void HandleDestroyEnemy(object[] data)
    {
        if (!TryGetInt(data, 0, out int spawnId)
            || !TryGetVector3(data, 1, out Vector3 hitPosition)
            || !TryGetVector3(data, 4, out Vector3 hitNormal)
            || !TryGetInt(data, 7, out int requestedScoreValue)
            || !TryGetInt(data, 8, out int requestedHealValue)
            || !TryGetBool(data, 9, out bool requestedSkillCharge)
            || spawnId <= 0)
        {
            return;
        }

        int scoreValue = Mathf.Clamp(requestedScoreValue, 0, 1);
        int healValue = Mathf.Clamp(requestedHealValue, 0, 5);
        bool grantsSkillCharge = requestedSkillCharge && scoreValue > 0;
        int killerActorNumber = TryGetInt(data, 10, out int parsedActorNumber) ? parsedActorNumber : -1;

        if (!RememberDestroyedEnemy(spawnId))
            return;

        if (enemiesById.TryGetValue(spawnId, out GameObject enemyObject) && enemyObject != null)
        {
            CombatEffects.SpawnHit(hitPosition, hitNormal);
            CombatEffects.SpawnExplosion(enemyObject.transform.position);
            Destroy(enemyObject);
        }

        enemiesById.Remove(spawnId);
        ScoreManager.score += scoreValue;
        sharedHealth = Mathf.Min(MaxSharedHealth, sharedHealth + DifficultyManager.GetKillHealAmount(healValue));
        BloodManager.blood = sharedHealth;

        bool isLocalKiller = killerActorNumber <= 0
            || PhotonNetwork.LocalPlayer == null
            || PhotonNetwork.LocalPlayer.ActorNumber == killerActorNumber;

        if (grantsSkillCharge && isLocalKiller && NetworkPlayerController.LocalPlayer != null)
        {
            NetworkPlayerController.LocalPlayer.RegisterEnemyKill();
        }
        else if (grantsSkillCharge && isLocalKiller && PlayerSkill.Instance != null)
        {
            PlayerSkill.Instance.RegisterEnemyKill();
        }
    }

    private void HandleBossState(object[] data)
    {
        if (!TryGetInt(data, 0, out int bossId)
            || !TryGetInt(data, 1, out int reportedCurrentHp)
            || !TryGetInt(data, 2, out int reportedMaxHp)
            || !TryGetVector3(data, 3, out Vector3 hitPosition)
            || !TryGetBool(data, 6, out bool dead)
            || bossId <= 0)
        {
            return;
        }

        int currentHp = Mathf.Max(0, reportedCurrentHp);
        int maxHp = Mathf.Max(1, reportedMaxHp);

        bossHpById[bossId] = currentHp;
        bossMaxHpById[bossId] = maxHp;

        if (!bossesById.TryGetValue(bossId, out GameObject bossObject) || bossObject == null)
        {
            if (dead)
            {
                destroyedBossIds.Add(bossId);
            }

            return;
        }

        BossController boss = bossObject.GetComponent<BossController>();
        Vector3 hitNormal = (hitPosition - bossObject.transform.position).normalized;
        CombatEffects.SpawnHit(hitPosition, hitNormal);

        if (dead)
        {
            if (!destroyedBossIds.Add(bossId))
                return;

            bossesById.Remove(bossId);
            bossHpById.Remove(bossId);
            bossMaxHpById.Remove(bossId);

            if (PhotonNetwork.IsMasterClient)
            {
                activeBossCount = Mathf.Max(0, activeBossCount - 1);
            }

            if (boss != null)
            {
                boss.ApplyNetworkHealth(0, maxHp);
                boss.DestroyNetworkBoss(hitPosition);
            }
            else
            {
                CombatEffects.SpawnExplosion(bossObject.transform.position);
                Destroy(bossObject);
            }

            return;
        }

        if (boss != null)
        {
            boss.ApplyNetworkHealth(currentHp, maxHp);
        }
    }

    private bool RememberDestroyedEnemy(int spawnId)
    {
        if (!destroyedEnemyIds.Add(spawnId))
            return false;

        destroyedEnemyOrder.Enqueue(spawnId);
        while (destroyedEnemyOrder.Count > MaxRememberedDestroyedEnemies)
        {
            int expiredId = destroyedEnemyOrder.Dequeue();
            destroyedEnemyIds.Remove(expiredId);
        }

        return true;
    }

    private void HandleSharedHealth(object[] data)
    {
        if (!TryGetInt(data, 0, out int reportedHealth) || !TryGetBool(data, 1, out bool reportedGameOver))
            return;

        sharedHealth = Mathf.Clamp(reportedHealth, 0, MaxSharedHealth);
        sharedGameOver = reportedGameOver;
        BloodManager.blood = sharedHealth;
    }

    private static int GetNextAvailableId(Dictionary<int, GameObject> objectsById, HashSet<int> destroyedIds)
    {
        int nextId = 1;
        foreach (int id in objectsById.Keys)
        {
            nextId = Mathf.Max(nextId, id + 1);
        }

        foreach (int id in destroyedIds)
        {
            nextId = Mathf.Max(nextId, id + 1);
        }

        return nextId;
    }

    private int GetNextBossScoreAfterCurrentScore()
    {
        int threshold = Mathf.Max(1, bossTriggerScore);
        int interval = Mathf.Max(1, bossScoreInterval);
        while (threshold <= ScoreManager.score)
        {
            threshold = threshold < interval ? interval : threshold + interval;
        }

        return threshold;
    }

    private static bool TryGetInt(object[] data, int index, out int value)
    {
        value = 0;
        if (data == null || index < 0 || index >= data.Length || data[index] == null)
            return false;

        if (data[index] is int intValue)
        {
            value = intValue;
            return true;
        }

        if (data[index] is byte byteValue)
        {
            value = byteValue;
            return true;
        }

        if (data[index] is short shortValue)
        {
            value = shortValue;
            return true;
        }

        return false;
    }

    private static bool TryGetFloat(object[] data, int index, out float value)
    {
        value = 0f;
        if (data == null || index < 0 || index >= data.Length || data[index] == null)
            return false;

        if (data[index] is float floatValue)
        {
            value = floatValue;
            return true;
        }

        if (data[index] is double doubleValue)
        {
            value = (float)doubleValue;
            return true;
        }

        if (data[index] is int intValue)
        {
            value = intValue;
            return true;
        }

        return false;
    }

    private static bool TryGetBool(object[] data, int index, out bool value)
    {
        value = false;
        if (data == null || index < 0 || index >= data.Length || !(data[index] is bool boolValue))
            return false;

        value = boolValue;
        return true;
    }

    private static bool TryGetVector3(object[] data, int startIndex, out Vector3 value)
    {
        value = Vector3.zero;
        if (!TryGetFloat(data, startIndex, out float x)
            || !TryGetFloat(data, startIndex + 1, out float y)
            || !TryGetFloat(data, startIndex + 2, out float z))
        {
            return false;
        }

        value = new Vector3(x, y, z);
        return true;
    }

    private static bool TryGetQuaternion(object[] data, int startIndex, out Quaternion value)
    {
        value = Quaternion.identity;
        if (!TryGetFloat(data, startIndex, out float x)
            || !TryGetFloat(data, startIndex + 1, out float y)
            || !TryGetFloat(data, startIndex + 2, out float z)
            || !TryGetFloat(data, startIndex + 3, out float w))
        {
            return false;
        }

        value = new Quaternion(x, y, z, w);
        return true;
    }

    private GameObject GetEnemyBulletPrefab(GameObject sourceEnemyPrefab)
    {
        if (sourceEnemyPrefab == null)
            return null;

        Enemy enemy = sourceEnemyPrefab.GetComponentInChildren<Enemy>();
        return enemy != null ? enemy.bulletPrefab : null;
    }
}
