using Photon.Pun;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkPlayerController : MonoBehaviourPun, IPunObservable
{
    public static NetworkPlayerController LocalPlayer { get; private set; }
    public static bool IsLocalShieldActive => LocalPlayer != null && LocalPlayer.IsShieldActive;
    public bool IsShieldActive => shieldTimeRemaining > 0f || networkShieldActive;

    [Header("Movement")]
    public float moveSpeed = 400f;
    public float leftLimit = -250f;
    public float rightLimit = 150f;

    [Header("Shooting")]
    public string networkBulletPrefabName = "NetworkPlayerBullet";
    public Transform firePoint;
    public float fireRate = 0.2f;

    [Header("Skill")]
    public int killsPerCharge = 10;
    public float shieldDuration = 4f;
    public float laserDuration = 2.8f;
    public float laserRange = 520f;
    public float laserRadius = 10f;
    public int laserBossDamage = 3;
    public float laserBossDamageInterval = 0.08f;
    public int laserEnemyScoreValue = 1;
    public int laserEnemyHealValue = 5;
    public Color laserColor = new Color(0.2f, 1f, 0.85f, 1f);
    public float laserColorIntensity = 5f;
    public float laserStartWidth = 6f;
    public float laserEndWidth = 3f;
    public LayerMask laserHitMask = ~0;
    public int barrageBulletCount = 17;
    public int barrageWaves = 5;
    public float barrageWaveInterval = 0.06f;
    public float barrageSpreadAngle = 100f;
    public float barrageSpawnOffset = 10f;
    public float voidCollapseDuration = 3f;
    public float voidCollapseScanRange = 650f;
    public float voidCollapseClusterRadius = 120f;
    public float voidCollapseFallbackDistance = 260f;
    public float voidCollapsePullRadius = 240f;
    public float voidCollapsePullStrength = 420f;
    public float voidCollapseExplosionRadius = 340f;
    public float voidCollapseCoreKillRadius = 52f;
    public float voidCollapseDamageInterval = 0.12f;
    public int voidCollapseBossDamagePerTick = 1;
    public int voidCollapseBossFinalDamage = 5;
    public int voidCollapseEnemyScoreValue = 1;
    public int voidCollapseEnemyHealValue = 5;

    private float nextFireTime;
    private Vector3 networkPosition;
    private Quaternion networkRotation;
    private int killProgress;
    private int charges;
    private float shieldTimeRemaining;
    private TextMeshProUGUI statusText;
    private bool networkShieldActive;
    private Coroutine networkShieldRoutine;
    private Coroutine laserVisualRoutine;
    private Coroutine laserDamageRoutine;
    private Coroutine barrageRoutine;
    private Coroutine voidCollapseRoutine;
    private LaserBeam activeLaserVisual;
    private readonly HashSet<GameObject> laserKilledEnemies = new HashSet<GameObject>();
    private readonly HashSet<BossController> laserDamagedBosses = new HashSet<BossController>();
    private readonly HashSet<GameObject> voidCollapseKilledEnemies = new HashSet<GameObject>();
    private readonly HashSet<BossController> voidCollapseDamagedBosses = new HashSet<BossController>();
    private readonly RaycastHit[] laserHitBuffer = new RaycastHit[64];
    private readonly Collider[] voidCollapseHitBuffer = new Collider[96];
    private GameObject cachedNetworkBulletPrefab;

    void Awake()
    {
        DisableLegacyInputComponents();
    }

    void Start()
    {
        networkPosition = transform.position;
        networkRotation = transform.rotation;

        CombatEffects.ApplyPlayerShipVisuals(gameObject);
        CombatEffects.AttachPlayerEngineJet(gameObject, firePoint);

        if (photonView.IsMine)
        {
            LocalPlayer = this;
            CreateStatusText();
            UpdateStatusText();
        }
    }

    void Update()
    {
        if (photonView.IsMine)
        {
            MoveLocalPlayer();
            TryShoot();
            TryUseSkills();
            UpdateShieldTimer();
        }
        else
        {
            transform.position = Vector3.Lerp(transform.position, networkPosition, Time.deltaTime * 12f);
            transform.rotation = Quaternion.Lerp(transform.rotation, networkRotation, Time.deltaTime * 12f);
        }
    }

    void MoveLocalPlayer()
    {
        float input = Input.GetAxis("Horizontal");
        float newX = transform.position.x + input * moveSpeed * Time.deltaTime;
        newX = Mathf.Clamp(newX, leftLimit, rightLimit);
        transform.position = new Vector3(newX, transform.position.y, transform.position.z);
    }

    void TryShoot()
    {
        if (!Input.GetKey(KeyCode.Space) || Time.time < nextFireTime)
            return;

        Transform origin = firePoint != null ? firePoint : transform;
        photonView.RPC(
            nameof(RpcSpawnPlayerBullet),
            RpcTarget.All,
            origin.position,
            origin.rotation,
            true,
            GetLocalActorNumber()
        );
        nextFireTime = Time.time + fireRate;
    }

    void TryUseSkills()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            TryUseShieldSkill();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            TryUseLaserSkill();
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            TryUseVoidCollapseSkill();
        }
    }

    bool TryUseShieldSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        shieldTimeRemaining = shieldDuration;
        photonView.RPC(nameof(RpcPlayShield), RpcTarget.All, shieldDuration);
        if (barrageRoutine != null)
        {
            StopCoroutine(barrageRoutine);
        }
        barrageRoutine = StartCoroutine(FireBarrageSkill());
        UpdateStatusText();
        return true;
    }

    bool TryUseLaserSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        if (laserDamageRoutine != null)
        {
            StopCoroutine(laserDamageRoutine);
            laserDamageRoutine = null;
        }

        photonView.RPC(nameof(RpcPlayLaser), RpcTarget.All, laserDuration);
        laserDamageRoutine = StartCoroutine(DamageLaserTargetsRoutine());
        UpdateStatusText();
        return true;
    }

    bool TryUseVoidCollapseSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        Transform origin = firePoint != null ? firePoint : transform;
        Vector3 center = VoidSingularitySkill.FindBestCenter(
            transform,
            origin,
            voidCollapseScanRange,
            voidCollapseClusterRadius,
            voidCollapseFallbackDistance);

        photonView.RPC(nameof(RpcPlayVoidCollapse), RpcTarget.All, center, voidCollapseDuration, voidCollapsePullRadius);
        UpdateStatusText();
        return true;
    }

    void UpdateShieldTimer()
    {
        if (shieldTimeRemaining <= 0f)
            return;

        shieldTimeRemaining = Mathf.Max(0f, shieldTimeRemaining - Time.deltaTime);
        UpdateStatusText();
    }

    public void RegisterEnemyKill()
    {
        killProgress++;
        if (killProgress >= GetCurrentKillsPerCharge())
        {
            killProgress = 0;
            charges++;
        }

        UpdateStatusText();
    }

    [PunRPC]
    void RpcPlayShield(float duration)
    {
        networkShieldActive = true;
        if (networkShieldRoutine != null)
        {
            StopCoroutine(networkShieldRoutine);
        }

        networkShieldRoutine = StartCoroutine(ClearNetworkShieldFlag(duration));
        NetworkPlayerSkillFx.PlayShield(transform, duration);
    }

    [PunRPC]
    void RpcPlayLaser(float duration)
    {
        if (laserVisualRoutine != null)
        {
            StopCoroutine(laserVisualRoutine);
            laserVisualRoutine = null;
        }

        CleanupActiveLaserVisual();
        laserVisualRoutine = StartCoroutine(PlayLaserVisual(duration));
    }

    [PunRPC]
    void RpcPlayVoidCollapse(Vector3 center, float duration, float pullRadius)
    {
        NetworkPlayerSkillFx.PlayVoidSingularity(center, duration, pullRadius);

        if (NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient)
            return;

        if (voidCollapseRoutine != null)
        {
            StopCoroutine(voidCollapseRoutine);
            voidCollapseRoutine = null;
        }

        voidCollapseRoutine = StartCoroutine(VoidCollapseRoutine(center));
    }

    IEnumerator ClearNetworkShieldFlag(float duration)
    {
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        networkShieldActive = false;
        networkShieldRoutine = null;
    }

    IEnumerator PlayLaserVisual(float duration)
    {
        Transform origin = firePoint != null ? firePoint : transform;
        LaserBeam laser = NetworkPlayerSkillFx.CreateLaser(origin, laserColor, laserColorIntensity, laserRange, laserStartWidth, laserEndWidth);
        if (laser == null)
            yield break;

        activeLaserVisual = laser;
        laser.BeginFire();
        yield return new WaitForSeconds(Mathf.Max(0.1f, duration));
        if (activeLaserVisual == laser)
        {
            CleanupActiveLaserVisual();
        }
        laserVisualRoutine = null;
    }

    void CleanupActiveLaserVisual()
    {
        if (activeLaserVisual == null)
            return;

        activeLaserVisual.EndFire();
        Destroy(activeLaserVisual.gameObject);
        activeLaserVisual = null;
    }

    IEnumerator DamageLaserTargetsRoutine()
    {
        laserKilledEnemies.Clear();
        float endTime = Time.time + Mathf.Max(0.1f, laserDuration);
        float nextBossDamageTime = 0f;

        while (Time.time < endTime)
        {
            bool canDamageBoss = Time.time >= nextBossDamageTime;
            if (DamageLaserTargets(canDamageBoss))
            {
                nextBossDamageTime = Time.time + Mathf.Max(0.03f, laserBossDamageInterval);
            }
            yield return null;
        }

        laserKilledEnemies.Clear();
        laserDamageRoutine = null;
    }

    IEnumerator FireBarrageSkill()
    {
        Transform origin = firePoint != null ? firePoint : transform;
        int count = Mathf.Max(1, barrageBulletCount);
        float startAngle = -barrageSpreadAngle * 0.5f;
        float step = count == 1 ? 0f : barrageSpreadAngle / (count - 1);

        for (int wave = 0; wave < Mathf.Max(1, barrageWaves); wave++)
        {
            float waveOffset = (wave - (barrageWaves - 1) * 0.5f) * (step * 0.35f);

            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + step * i + waveOffset;
                Quaternion rotation = origin.rotation * Quaternion.Euler(0f, angle, 0f);
                Vector3 sideOffset = origin.right * ((i - (count - 1) * 0.5f) * 0.9f);
                Vector3 position = origin.position + sideOffset + rotation * Vector3.forward * barrageSpawnOffset;
                photonView.RPC(
                    nameof(RpcSpawnPlayerBullet),
                    RpcTarget.All,
                    position,
                    rotation,
                    false,
                    GetLocalActorNumber()
                );
            }

            yield return new WaitForSeconds(barrageWaveInterval);
        }

        barrageRoutine = null;
    }

    bool DamageLaserTargets(bool canDamageBoss)
    {
        Transform origin = firePoint != null ? firePoint : transform;
        Vector3 start = origin.position;
        Vector3 direction = origin.forward;
        float range = Mathf.Max(1f, laserRange);
        float radius = Mathf.Max(0.1f, laserRadius);
        bool damagedBoss = false;
        if (canDamageBoss)
        {
            laserDamagedBosses.Clear();
        }

        int hitCount = Physics.SphereCastNonAlloc(start, radius, direction, laserHitBuffer, range, laserHitMask, QueryTriggerInteraction.Collide);
        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = laserHitBuffer[i];
            Collider targetCollider = hit.collider;
            if (targetCollider == null)
                continue;

            BossController boss = targetCollider.GetComponentInParent<BossController>();
            if (boss != null)
            {
                if (canDamageBoss && laserDamagedBosses.Add(boss))
                {
                    boss.TakeDamage(laserBossDamage, targetCollider.ClosestPoint(start));
                    damagedBoss = true;
                }
                continue;
            }

            Enemy enemy = targetCollider.GetComponentInParent<Enemy>();
            if (enemy == null || laserKilledEnemies.Contains(enemy.gameObject))
                continue;

            laserKilledEnemies.Add(enemy.gameObject);
            Vector3 enemyHitPosition = targetCollider.ClosestPoint(start);
            NetworkCoopEnemyIdentity identity = enemy.GetComponent<NetworkCoopEnemyIdentity>();
            bool handled = NetworkCoopGameRuntime.ReportEnemyDestroyed(
                identity,
                enemyHitPosition,
                -direction,
                laserEnemyScoreValue,
                laserEnemyHealValue,
                false
            );

            if (!handled)
            {
                CombatEffects.SpawnHit(enemyHitPosition, -direction);
                CombatEffects.SpawnExplosion(enemy.transform.position);
                Destroy(enemy.gameObject);
                ScoreManager.score += laserEnemyScoreValue;
                BloodManager.blood = Mathf.Min(100, BloodManager.blood + DifficultyManager.GetKillHealAmount(laserEnemyHealValue));
            }
        }

        return damagedBoss;
    }

    IEnumerator VoidCollapseRoutine(Vector3 center)
    {
        voidCollapseKilledEnemies.Clear();
        float endTime = Time.time + Mathf.Max(0.1f, voidCollapseDuration);
        float nextDamageTime = 0f;

        while (Time.time < endTime)
        {
            VoidSingularitySkill.PullEnemies(center, voidCollapsePullRadius, voidCollapsePullStrength);
            if (Time.time >= nextDamageTime)
            {
                DamageVoidCollapseTargets(center, voidCollapsePullRadius, false);
                nextDamageTime = Time.time + Mathf.Max(0.03f, voidCollapseDamageInterval);
            }
            yield return null;
        }

        DamageVoidCollapseTargets(center, voidCollapseExplosionRadius, true);
        voidCollapseKilledEnemies.Clear();
        voidCollapseDamagedBosses.Clear();
        voidCollapseRoutine = null;
    }

    void DamageVoidCollapseTargets(Vector3 center, float radius, bool finalBurst)
    {
        int hitCount = Physics.OverlapSphereNonAlloc(center, Mathf.Max(1f, radius), voidCollapseHitBuffer, ~0, QueryTriggerInteraction.Collide);
        voidCollapseDamagedBosses.Clear();

        for (int i = 0; i < hitCount; i++)
        {
            Collider targetCollider = voidCollapseHitBuffer[i];
            if (targetCollider == null)
                continue;

            BossController boss = targetCollider.GetComponentInParent<BossController>();
            if (boss != null)
            {
                if (voidCollapseDamagedBosses.Add(boss))
                {
                    int damage = finalBurst ? voidCollapseBossFinalDamage : voidCollapseBossDamagePerTick;
                    Vector3 bossHitPosition = targetCollider.ClosestPoint(center);
                    NetworkCoopBossIdentity bossIdentity = boss.GetComponent<NetworkCoopBossIdentity>();
                    if (!NetworkCoopGameRuntime.ReportBossDamaged(bossIdentity, Mathf.Max(1, damage), bossHitPosition))
                    {
                        boss.TakeDamage(Mathf.Max(1, damage), bossHitPosition);
                    }
                }
                continue;
            }

            Enemy enemy = targetCollider.GetComponentInParent<Enemy>();
            if (enemy == null || voidCollapseKilledEnemies.Contains(enemy.gameObject))
                continue;

            float distance = Vector3.Distance(enemy.transform.position, center);
            if (!finalBurst && distance > voidCollapseCoreKillRadius)
                continue;

            voidCollapseKilledEnemies.Add(enemy.gameObject);
            Vector3 hitPosition = targetCollider.ClosestPoint(center);
            NetworkPlayerSkillFx.PlayVoidCrush(hitPosition);

            NetworkCoopEnemyIdentity identity = enemy.GetComponent<NetworkCoopEnemyIdentity>();
            bool handled = NetworkCoopGameRuntime.ReportEnemyDestroyed(
                identity,
                hitPosition,
                (hitPosition - center).normalized,
                Mathf.Max(0, voidCollapseEnemyScoreValue),
                Mathf.Max(0, voidCollapseEnemyHealValue),
                false
            );

            if (!handled)
            {
                Destroy(enemy.gameObject);
                ScoreManager.score += Mathf.Max(0, voidCollapseEnemyScoreValue);
                BloodManager.blood = Mathf.Min(100, BloodManager.blood + DifficultyManager.GetKillHealAmount(voidCollapseEnemyHealValue));
            }
        }
    }


    [PunRPC]
    void RpcSpawnPlayerBullet(Vector3 position, Quaternion rotation, bool skillGrantsCharge, int ownerActorNumber)
    {
        GameObject bulletPrefab = GetNetworkBulletPrefab();
        if (bulletPrefab == null)
            return;

        GameObject bullet = Instantiate(bulletPrefab, position, rotation);
        NetworkBullet networkBullet = bullet.GetComponent<NetworkBullet>();
        if (networkBullet != null)
        {
            networkBullet.InitializeLocal(ownerActorNumber, skillGrantsCharge);
        }

        CombatEffects.SpawnMuzzleFlash(position, rotation);
    }

    GameObject GetNetworkBulletPrefab()
    {
        if (cachedNetworkBulletPrefab == null && !string.IsNullOrWhiteSpace(networkBulletPrefabName))
        {
            cachedNetworkBulletPrefab = Resources.Load<GameObject>(networkBulletPrefabName);
        }

        if (cachedNetworkBulletPrefab == null && photonView.IsMine)
        {
            Debug.LogWarning($"NetworkPlayerController: could not load Resources/{networkBulletPrefabName} for co-op bullets.");
        }

        return cachedNetworkBulletPrefab;
    }

    int GetLocalActorNumber()
    {
        return PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1;
    }

    void DisableLegacyInputComponents()
    {
        PlayerMovement playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        FireLogic fireLogic = GetComponent<FireLogic>();
        if (fireLogic != null)
        {
            fireLogic.enabled = false;
        }

        PlayerSkill playerSkill = GetComponent<PlayerSkill>();
        if (playerSkill != null)
        {
            playerSkill.enabled = false;
        }
    }

    void CreateStatusText()
    {
        Canvas canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        GameObject textObject = new GameObject("Network Skill Status Text");
        textObject.transform.SetParent(canvas.transform, false);
        statusText = textObject.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 25f;
        statusText.color = new Color(0.16f, 1f, 1f, 1f);
        statusText.alignment = TextAlignmentOptions.TopRight;
        statusText.fontStyle = FontStyles.Bold;
        statusText.lineSpacing = -10f;

        RectTransform rect = statusText.rectTransform;
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -52f);
        rect.sizeDelta = new Vector2(420f, 96f);
    }

    Canvas FindSceneCanvas()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases)
        {
            if (canvas != null && canvas.gameObject.scene == activeScene)
            {
                return canvas;
            }
        }

        return null;
    }

    void UpdateStatusText()
    {
        if (statusText == null)
            return;

        string shieldText = shieldTimeRemaining > 0f ? $"\nShield: {shieldTimeRemaining:0.0}s" : string.Empty;
        statusText.text = $"Skill X/C/V: {charges}\nCharge: {killProgress}/{GetCurrentKillsPerCharge()}{shieldText}";
    }

    int GetCurrentKillsPerCharge()
    {
        return DifficultyManager.GetRequiredKillsForCharge(killsPerCharge);
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
        }
        else
        {
            networkPosition = (Vector3)stream.ReceiveNext();
            networkRotation = (Quaternion)stream.ReceiveNext();
        }
    }

    void OnDestroy()
    {
        if (LocalPlayer == this)
        {
            LocalPlayer = null;
        }

        if (statusText != null)
        {
            Destroy(statusText.gameObject);
        }

        CleanupActiveLaserVisual();
        if (barrageRoutine != null)
        {
            StopCoroutine(barrageRoutine);
            barrageRoutine = null;
        }
        if (voidCollapseRoutine != null)
        {
            StopCoroutine(voidCollapseRoutine);
            voidCollapseRoutine = null;
        }
    }
}
