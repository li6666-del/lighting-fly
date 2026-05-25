using TMPro;
using Photon.Pun;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class BossController : MonoBehaviour
{
    [Header("Health")]
    public int maxHp = 90;
    public int scoreReward = 0;
    [HideInInspector] public float extraHealthMultiplier = 1f;

    [Header("Movement")]
    public float moveSpeed = 45f;
    public float moveRange = 150f;

    [Header("Attack")]
    public GameObject bulletPrefab;
    public float fireInterval = 0.72f;
    public int fanBulletCount = 13;
    public float fanAngle = 82f;
    public float muzzleOffset = 38f;
    public float attackOriginHeight = 12f;

    [Header("Ultimate Laser")]
    public bool enableUltimateLaser = true;
    public float ultimateInitialDelay = 5f;
    public float ultimateInterval = 9f;
    public float ultimateChargeDuration = 2.2f;
    public float ultimateLaserDuration = 1.45f;
    public float ultimateLaserRange = 360f;
    public float ultimateLaserRadius = 11f;
    public int ultimateLaserDamage = 14;
    public float ultimateDamageInterval = 0.28f;

    [Header("Collision")]
    public float colliderSizeMultiplier = 0.72f;

    private int hp;
    private float nextFireTime;
    private Vector3 centerPosition;
    private Transform player;
    private EnemySpawner owner;
    private Slider hpSlider;
    private TextMeshProUGUI hpText;
    private bool dead;
    private int uiSlot;
    private BossChargeWarning ultimateChargeFx;
    private LaserBeam ultimateLaser;
    private Coroutine ultimateRoutine;
    private bool isUsingUltimate;
    private float nextUltimateTime;

    public void Initialize(EnemySpawner spawner, GameObject fallbackBulletPrefab, int healthUiSlot)
    {
        owner = spawner;
        uiSlot = Mathf.Max(0, healthUiSlot);
        if (bulletPrefab == null)
        {
            bulletPrefab = fallbackBulletPrefab;
        }
    }

    void Start()
    {
        ApplyDifficulty();
        hp = Mathf.Max(1, maxHp);
        centerPosition = transform.position;

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject != null)
        {
            player = playerObject.transform;
        }

        EnsureCollisionSetup();
        CombatEffects.ApplyBossGlow(gameObject);
        EnsureUltimateComponents();
        nextUltimateTime = Time.time + Mathf.Max(1f, ultimateInitialDelay) + uiSlot * 1.25f;
        CreateHealthUi();
        UpdateHealthUi();
    }

    void ApplyDifficulty()
    {
        maxHp = Mathf.RoundToInt(maxHp * DifficultyManager.BossHealthMultiplier * Mathf.Max(1f, extraHealthMultiplier));
        moveSpeed *= DifficultyManager.BossMoveMultiplier;
        fireInterval *= DifficultyManager.BossFireIntervalMultiplier;
        fanBulletCount += DifficultyManager.BossFanBulletBonus;
    }

    void Update()
    {
        if (dead)
            return;

        bool networkFollower = NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient;

        if (!isUsingUltimate && !networkFollower)
        {
            MoveBoss();
        }
        TryUltimate();
        if (!isUsingUltimate && !networkFollower)
        {
            TryShoot();
        }
    }

    public void TakeDamage(int damage, Vector3 hitPosition)
    {
        if (dead)
            return;

        NetworkCoopBossIdentity networkBoss = GetComponent<NetworkCoopBossIdentity>();
        if (NetworkCoopGameRuntime.ReportBossDamaged(networkBoss, damage, hitPosition))
        {
            return;
        }

        hp -= Mathf.Max(1, damage);
        CombatEffects.SpawnHit(hitPosition, (hitPosition - transform.position).normalized);
        UpdateHealthUi();

        if (hp <= 0)
        {
            Die();
        }
    }

    public void ApplyNetworkHealth(int currentHp, int maxHpValue)
    {
        maxHp = Mathf.Max(1, maxHpValue);
        hp = Mathf.Clamp(currentHp, 0, maxHp);

        if (hpSlider != null)
        {
            hpSlider.maxValue = maxHp;
        }

        UpdateHealthUi();
    }

    public void DestroyNetworkBoss(Vector3 hitPosition)
    {
        if (dead)
            return;

        dead = true;
        CancelUltimate();
        hp = 0;
        UpdateHealthUi();
        SpawnDeathEffects(hitPosition);
        DestroyHealthUi();
        Destroy(gameObject);
    }

    void MoveBoss()
    {
        float x = centerPosition.x + Mathf.Sin(Time.time * moveSpeed * 0.01f) * moveRange;
        transform.position = new Vector3(x, centerPosition.y, centerPosition.z);
    }

    void TryShoot()
    {
        if (bulletPrefab == null || player == null || Time.time < nextFireTime)
            return;

        Vector3 origin = transform.position + Vector3.up * attackOriginHeight + Vector3.back * muzzleOffset;
        Vector3 direction = (player.position - origin).normalized;
        Quaternion centerRotation = Quaternion.LookRotation(direction);
        int count = Mathf.Max(1, fanBulletCount);
        float startAngle = -fanAngle * 0.5f;
        float step = count == 1 ? 0f : fanAngle / (count - 1);

        for (int i = 0; i < count; i++)
        {
            Quaternion rotation = centerRotation * Quaternion.Euler(0f, startAngle + step * i, 0f);
            Vector3 position = origin + rotation * Vector3.forward * 8f;
            if (NetworkCoopGameRuntime.SpawnEnemyBullet(position, rotation, bulletPrefab))
                continue;

            Instantiate(bulletPrefab, position, rotation);
            CombatEffects.SpawnEnemyMuzzleFlash(position, rotation);
        }

        nextFireTime = Time.time + fireInterval;
    }

    void TryUltimate()
    {
        if (NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient)
            return;

        if (!enableUltimateLaser || player == null || isUsingUltimate || Time.time < nextUltimateTime)
            return;

        if (ultimateRoutine != null)
            return;

        ultimateRoutine = StartCoroutine(UltimateLaserRoutine());
    }

    IEnumerator UltimateLaserRoutine()
    {
        isUsingUltimate = true;
        nextFireTime = Time.time + ultimateChargeDuration + ultimateLaserDuration + fireInterval;

        EnsureUltimateComponents();

        Vector3 releasePosition = GetUltimateOrigin();
        bool chargeComplete = false;
        if (ultimateChargeFx != null)
        {
            ultimateChargeFx.orbLocalOffset = transform.InverseTransformPoint(releasePosition);
            ultimateChargeFx.StartCharge(Mathf.Max(0.2f, ultimateChargeDuration), pos =>
            {
                releasePosition = pos;
                chargeComplete = true;
            });
        }
        else
        {
            yield return new WaitForSeconds(Mathf.Max(0.2f, ultimateChargeDuration));
            chargeComplete = true;
        }

        while (!dead && !chargeComplete)
        {
            yield return null;
        }

        if (!dead)
        {
            yield return FireUltimateLaser(releasePosition);
        }

        nextUltimateTime = Time.time + Mathf.Max(2f, ultimateInterval);
        isUsingUltimate = false;
        ultimateRoutine = null;
    }

    IEnumerator FireUltimateLaser(Vector3 origin)
    {
        EnsureUltimateComponents();
        if (ultimateLaser == null)
            yield break;

        Vector3 direction = player != null
            ? (player.position - origin).normalized
            : transform.forward;
        if (direction.sqrMagnitude < 0.001f)
        {
            direction = Vector3.back;
        }

        ultimateLaser.transform.SetPositionAndRotation(origin, Quaternion.LookRotation(direction));
        ultimateLaser.BeginFire();

        float endTime = Time.time + Mathf.Max(0.1f, ultimateLaserDuration);
        float nextDamageTime = 0f;

        while (!dead && Time.time < endTime)
        {
            if (Time.time >= nextDamageTime)
            {
                DamagePlayerWithUltimate(origin, direction);
                nextDamageTime = Time.time + Mathf.Max(0.05f, ultimateDamageInterval);
            }

            yield return null;
        }

        ultimateLaser.EndFire();
    }

    void DamagePlayerWithUltimate(Vector3 origin, Vector3 direction)
    {
        if (player == null)
            return;

        Vector3 toPlayer = player.position - origin;
        float alongBeam = Vector3.Dot(toPlayer, direction);
        if (alongBeam < 0f || alongBeam > ultimateLaserRange)
            return;

        Vector3 closestPoint = origin + direction * alongBeam;
        float distance = Vector3.Distance(player.position, closestPoint);
        if (distance > ultimateLaserRadius)
            return;

        Vector3 hitNormal = (player.position - closestPoint).sqrMagnitude > 0.001f
            ? (player.position - closestPoint).normalized
            : -direction;

        if (NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient)
            return;

        NetworkPlayerController networkPlayer = player.GetComponentInParent<NetworkPlayerController>();
        bool blockedByShield = networkPlayer != null
            ? networkPlayer.IsShieldActive
            : PlayerSkill.Instance != null && PlayerSkill.Instance.IsShieldActive;

        if (blockedByShield)
        {
            CombatEffects.SpawnHit(closestPoint, hitNormal);
            return;
        }

        if (NetworkCoopGameRuntime.ReportPlayerDamaged(ultimateLaserDamage, closestPoint, hitNormal, false))
            return;

        CombatEffects.SpawnHit(closestPoint, hitNormal);
        BloodManager.blood = Mathf.Max(0, BloodManager.blood - Mathf.Max(1, ultimateLaserDamage));
    }

    Vector3 GetUltimateOrigin()
    {
        return transform.position + Vector3.up * attackOriginHeight + Vector3.back * muzzleOffset;
    }

    void EnsureUltimateComponents()
    {
        if (ultimateChargeFx == null)
        {
            ultimateChargeFx = GetComponent<BossChargeWarning>();
            if (ultimateChargeFx == null)
            {
                ultimateChargeFx = gameObject.AddComponent<BossChargeWarning>();
            }

            ultimateChargeFx.warningColor = new Color(1f, 0.08f, 0.03f, 1f);
            ultimateChargeFx.colorIntensity = 2.6f;
            ultimateChargeFx.maxRingRadius = 32f;
            ultimateChargeFx.ringWidth = 0.75f;
            ultimateChargeFx.orbMaxScale = 7f;
            ultimateChargeFx.orbLocalOffset = transform.InverseTransformPoint(GetUltimateOrigin());
        }

        if (ultimateLaser == null)
        {
            GameObject laserObject = new GameObject("Boss Ultimate Laser");
            ultimateLaser = laserObject.AddComponent<LaserBeam>();
            ultimateLaser.beamColor = new Color(1f, 0.12f, 0.04f, 1f);
            ultimateLaser.colorIntensity = 3.6f;
            ultimateLaser.startWidth = 9f;
            ultimateLaser.endWidth = 5f;
            ultimateLaser.widthJitter = 0.28f;
            ultimateLaser.maxDistance = ultimateLaserRange;
            ultimateLaser.hitMask = 0;
            ultimateLaser.spawnHitSparks = false;
            ultimateLaser.RefreshAppearance();
        }
    }

    void EnsureCollisionSetup()
    {
        Bounds bounds = CalculateBounds();
        BoxCollider rootBox = GetComponent<BoxCollider>();
        if (rootBox == null && GetComponentInChildren<Collider>() == null)
        {
            rootBox = gameObject.AddComponent<BoxCollider>();
        }

        if (rootBox != null)
        {
            rootBox.center = transform.InverseTransformPoint(bounds.center);
            rootBox.size = GetLocalColliderSize(bounds);
            rootBox.isTrigger = true;
        }

        Rigidbody rigidbody = GetComponent<Rigidbody>();
        if (rigidbody == null)
        {
            rigidbody = gameObject.AddComponent<Rigidbody>();
        }

        rigidbody.useGravity = false;
        rigidbody.isKinematic = true;
    }

    Vector3 GetLocalColliderSize(Bounds bounds)
    {
        Vector3 scale = transform.lossyScale;
        float x = Mathf.Abs(scale.x) > 0.001f ? bounds.size.x / Mathf.Abs(scale.x) : bounds.size.x;
        float y = Mathf.Abs(scale.y) > 0.001f ? bounds.size.y / Mathf.Abs(scale.y) : bounds.size.y;
        float z = Mathf.Abs(scale.z) > 0.001f ? bounds.size.z / Mathf.Abs(scale.z) : bounds.size.z;

        return new Vector3(
            Mathf.Max(x * colliderSizeMultiplier, 1f),
            Mathf.Max(y * colliderSizeMultiplier, 1f),
            Mathf.Max(z * colliderSizeMultiplier, 1f)
        );
    }

    Bounds CalculateBounds()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
        {
            return new Bounds(transform.position, Vector3.one * 12f);
        }

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        return bounds;
    }

    void CreateHealthUi()
    {
        Canvas canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        GameObject root = new GameObject("Boss Health UI");
        root.transform.SetParent(canvas.transform, false);

        RectTransform rootRect = root.AddComponent<RectTransform>();
        rootRect.anchorMin = new Vector2(0.5f, 1f);
        rootRect.anchorMax = new Vector2(0.5f, 1f);
        rootRect.pivot = new Vector2(0.5f, 1f);
        rootRect.anchoredPosition = new Vector2(0f, -18f - uiSlot * 58f);
        rootRect.sizeDelta = new Vector2(520f, 54f);

        hpSlider = root.AddComponent<Slider>();
        hpSlider.minValue = 0f;
        hpSlider.maxValue = maxHp;
        hpSlider.value = hp;
        hpSlider.transition = Selectable.Transition.None;

        Image background = CreateUiImage(root.transform, "Background", new Color(0.06f, 0.02f, 0.02f, 0.75f), Vector2.zero, Vector2.one);
        Image fill = CreateUiImage(root.transform, "Fill", new Color(1f, 0.12f, 0.03f, 0.92f), Vector2.zero, Vector2.one);
        hpSlider.targetGraphic = fill;
        hpSlider.fillRect = fill.rectTransform;

        GameObject textObject = new GameObject("Boss HP Text");
        textObject.transform.SetParent(root.transform, false);
        hpText = textObject.AddComponent<TextMeshProUGUI>();
        hpText.alignment = TextAlignmentOptions.Center;
        hpText.fontSize = 28f;
        hpText.color = new Color(1f, 0.82f, 0.62f);
        hpText.raycastTarget = false;

        RectTransform textRect = hpText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        background.raycastTarget = false;
        fill.raycastTarget = false;
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

    Image CreateUiImage(Transform parent, string objectName, Color color, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject imageObject = new GameObject(objectName);
        imageObject.transform.SetParent(parent, false);
        Image image = imageObject.AddComponent<Image>();
        image.color = color;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        return image;
    }

    void UpdateHealthUi()
    {
        if (hpSlider != null)
        {
            hpSlider.value = Mathf.Max(0, hp);
        }

        if (hpText != null)
        {
            hpText.text = $"BOSS HP {Mathf.Max(0, hp)}/{maxHp}";
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (NetworkCoopGameRuntime.ReportPlayerCrashed(100, other.transform.position))
            {
                return;
            }

            CombatEffects.SpawnExplosion(other.transform.position);
            BloodManager.blood = 0;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            if (NetworkCoopGameRuntime.IsActive && !PhotonNetwork.IsMasterClient)
            {
                return;
            }

            if (NetworkCoopGameRuntime.ReportPlayerCrashed(100, collision.transform.position))
            {
                return;
            }

            CombatEffects.SpawnExplosion(collision.transform.position);
            BloodManager.blood = 0;
        }
    }

    void Die()
    {
        dead = true;
        CancelUltimate();
        if (scoreReward > 0)
        {
            ScoreManager.score += scoreReward;
        }

        SpawnDeathEffects(transform.position);
        DestroyHealthUi();

        if (owner != null)
        {
            owner.OnBossDefeated();
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        CancelUltimate();
        DestroyHealthUi();
    }

    void CancelUltimate()
    {
        if (ultimateRoutine != null)
        {
            StopCoroutine(ultimateRoutine);
            ultimateRoutine = null;
        }

        isUsingUltimate = false;

        if (ultimateChargeFx != null && ultimateChargeFx.IsCharging)
        {
            ultimateChargeFx.CancelCharge();
        }

        if (ultimateLaser != null)
        {
            ultimateLaser.EndFire();
            Destroy(ultimateLaser.gameObject);
            ultimateLaser = null;
        }
    }

    void SpawnDeathEffects(Vector3 center)
    {
        CombatEffects.SpawnBossExplosion(center);
        CombatEffects.SpawnExplosion(center + transform.right * 35f);
        CombatEffects.SpawnExplosion(center - transform.right * 35f);
        CombatEffects.SpawnExplosion(center + transform.forward * 24f);
        CombatEffects.SpawnExplosion(center - transform.forward * 20f);
    }

    void DestroyHealthUi()
    {
        if (hpSlider != null)
        {
            Destroy(hpSlider.gameObject);
            hpSlider = null;
            hpText = null;
        }
    }
}
