using TMPro;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSkill : MonoBehaviour
{
    public static PlayerSkill Instance { get; private set; }

    [Header("Charge")]
    public int killsPerCharge = 10;

    [Header("Shield")]
    public float shieldDuration = 4f;

    [Header("Barrage")]
    public int barrageBulletCount = 17;
    public int barrageWaves = 5;
    public float barrageWaveInterval = 0.06f;
    public float barrageSpreadAngle = 100f;
    public float barrageSpawnOffset = 10f;

    [Header("Laser Skill")]
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
    public bool laserStopsAtFirstHit = false;

    [Header("Void Collapse Skill")]
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

    public bool IsShieldActive => shieldTimeRemaining > 0f;

    private int killProgress;
    private int charges;
    private float shieldTimeRemaining;
    private FireLogic fireLogic;
    private GameObject shieldVisual;
    private Material shieldShellMaterial;
    private Light shieldLight;
    private Transform shieldRingA;
    private Transform shieldRingB;
    private ParticleSystem shieldParticles;
    private TextMeshProUGUI statusText;
    private LaserBeam skillLaser;
    private Coroutine laserRoutine;
    private Coroutine voidCollapseRoutine;
    private readonly HashSet<GameObject> laserKilledEnemies = new HashSet<GameObject>();
    private readonly HashSet<BossController> laserDamagedBosses = new HashSet<BossController>();
    private readonly HashSet<GameObject> voidCollapseKilledEnemies = new HashSet<GameObject>();
    private readonly HashSet<BossController> voidCollapseDamagedBosses = new HashSet<BossController>();
    private readonly RaycastHit[] laserHitBuffer = new RaycastHit[64];
    private readonly Collider[] voidCollapseHitBuffer = new Collider[96];
    private PlayerShipVisualTheme visualTheme;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    void Start()
    {
        visualTheme = PlayerShipColorSelection.CurrentTheme;
        laserColor = visualTheme.Laser;
        fireLogic = GetComponent<FireLogic>();
        CreateShieldVisual();
        CreateStatusText();
        UpdateStatusText();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.X))
        {
            TryUseSkill();
        }
        else if (Input.GetKeyDown(KeyCode.C))
        {
            TryUseLaserSkill();
        }
        else if (Input.GetKeyDown(KeyCode.V))
        {
            TryUseVoidCollapseSkill();
        }

        if (shieldTimeRemaining > 0f)
        {
            shieldTimeRemaining -= Time.deltaTime;
            if (shieldTimeRemaining <= 0f)
            {
                shieldTimeRemaining = 0f;
                SetShieldVisible(false);
            }
            else if (shieldVisual != null)
            {
                shieldVisual.transform.Rotate(Vector3.up, 140f * Time.deltaTime, Space.Self);
                AnimateShieldVisual();
            }

            UpdateStatusText();
        }
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

    public bool TryUseSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        shieldTimeRemaining = shieldDuration;
        SetShieldVisible(true);
        StartCoroutine(FireBarrage());
        UpdateStatusText();
        return true;
    }

    public bool TryUseLaserSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        if (laserRoutine != null)
        {
            StopCoroutine(laserRoutine);
            if (skillLaser != null)
            {
                skillLaser.EndFire();
            }
        }
        laserRoutine = StartCoroutine(FireLaserSkill());
        UpdateStatusText();
        return true;
    }

    public bool TryUseForwardBurstSkill()
    {
        return TryUseLaserSkill();
    }

    public bool TryUseVoidCollapseSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        if (voidCollapseRoutine != null)
        {
            StopCoroutine(voidCollapseRoutine);
            voidCollapseRoutine = null;
        }

        Transform origin = fireLogic != null && fireLogic.firePoint != null ? fireLogic.firePoint : transform;
        Vector3 center = VoidSingularitySkill.FindBestCenter(
            transform,
            origin,
            voidCollapseScanRange,
            voidCollapseClusterRadius,
            voidCollapseFallbackDistance);

        NetworkPlayerSkillFx.PlayVoidSingularity(center, voidCollapseDuration, voidCollapsePullRadius, visualTheme);
        voidCollapseRoutine = StartCoroutine(VoidCollapseRoutine(center));
        UpdateStatusText();
        return true;
    }

    private IEnumerator FireBarrage()
    {
        if (fireLogic == null || fireLogic.bulletPrefab == null)
            yield break;

        Transform origin = fireLogic.firePoint != null ? fireLogic.firePoint : transform;
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
                GameObject bullet = RuntimeObjectPool.Spawn(fireLogic.bulletPrefab, position, rotation);
                MarkSkillBullet(bullet);
                CombatEffects.SpawnMuzzleFlash(position, rotation, visualTheme);
            }

            yield return new WaitForSeconds(barrageWaveInterval);
        }
    }

    private IEnumerator FireLaserSkill()
    {
        EnsureSkillLaser();
        if (skillLaser == null)
            yield break;

        Transform origin = fireLogic != null && fireLogic.firePoint != null ? fireLogic.firePoint : transform;
        skillLaser.transform.SetParent(origin, false);
        skillLaser.transform.localPosition = Vector3.zero;
        skillLaser.transform.localRotation = Quaternion.identity;

        laserKilledEnemies.Clear();
        skillLaser.BeginFire();

        float endTime = Time.time + Mathf.Max(0.1f, laserDuration);
        float nextBossDamageTime = 0f;

        while (Time.time < endTime)
        {
            bool canDamageBoss = Time.time >= nextBossDamageTime;
            if (DamageLaserTargets(origin, canDamageBoss))
            {
                nextBossDamageTime = Time.time + Mathf.Max(0.03f, laserBossDamageInterval);
            }
            yield return null;
        }

        skillLaser.EndFire();
        laserKilledEnemies.Clear();
        laserRoutine = null;
    }

    private void EnsureSkillLaser()
    {
        if (skillLaser != null)
            return;

        Transform origin = fireLogic != null && fireLogic.firePoint != null ? fireLogic.firePoint : transform;
        GameObject laserObject = new GameObject("Player Skill Laser");
        laserObject.transform.SetParent(origin, false);
        laserObject.transform.localPosition = Vector3.zero;
        laserObject.transform.localRotation = Quaternion.identity;

        skillLaser = laserObject.AddComponent<LaserBeam>();
        skillLaser.beamColor = visualTheme.Laser;
        skillLaser.colorIntensity = laserColorIntensity;
        skillLaser.startWidth = laserStartWidth;
        skillLaser.endWidth = laserEndWidth;
        skillLaser.widthJitter = 0.22f;
        skillLaser.maxDistance = laserRange;
        skillLaser.hitMask = laserStopsAtFirstHit ? laserHitMask : 0;
        skillLaser.spawnHitSparks = true;
        skillLaser.sparkColor = Color.white;
        skillLaser.RefreshAppearance();
    }

    private bool DamageLaserTargets(Transform origin, bool canDamageBoss)
    {
        if (origin == null)
            return false;

        Vector3 start = origin.position;
        Vector3 direction = origin.forward;
        float range = Mathf.Max(1f, laserRange);
        float radius = Mathf.Max(0.1f, laserRadius);
        bool damagedBoss = false;

        int hitCount = Physics.SphereCastNonAlloc(start, radius, direction, laserHitBuffer, range, laserHitMask, QueryTriggerInteraction.Collide);
        if (canDamageBoss)
        {
            laserDamagedBosses.Clear();
        }

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
                    Vector3 hitPosition = targetCollider.ClosestPoint(start);
                    boss.TakeDamage(laserBossDamage, hitPosition);
                    damagedBoss = true;
                }
                continue;
            }

            Enemy enemy = targetCollider.GetComponentInParent<Enemy>();
            if (enemy == null || laserKilledEnemies.Contains(enemy.gameObject))
                continue;

            laserKilledEnemies.Add(enemy.gameObject);
            Vector3 enemyHitPosition = targetCollider.ClosestPoint(start);
            CombatEffects.SpawnHit(enemyHitPosition, -direction, visualTheme);
            CombatEffects.SpawnExplosion(enemy.transform.position);

            Destroy(enemy.gameObject);
            ScoreManager.score += laserEnemyScoreValue;
            BloodManager.blood = Mathf.Min(100, BloodManager.blood + DifficultyManager.GetKillHealAmount(laserEnemyHealValue));
        }

        return damagedBoss;
    }

    private IEnumerator VoidCollapseRoutine(Vector3 center)
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

    private void DamageVoidCollapseTargets(Vector3 center, float radius, bool finalBurst)
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
                    boss.TakeDamage(Mathf.Max(1, damage), targetCollider.ClosestPoint(center));
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
            NetworkPlayerSkillFx.PlayVoidCrush(hitPosition, visualTheme);
            Destroy(enemy.gameObject);
            ScoreManager.score += Mathf.Max(0, voidCollapseEnemyScoreValue);
            BloodManager.blood = Mathf.Min(100, BloodManager.blood + DifficultyManager.GetKillHealAmount(voidCollapseEnemyHealValue));
        }
    }

    private void CreateShieldVisual()
    {
        shieldVisual = new GameObject("Skill Shield Visual");
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localPosition = Vector3.zero;

        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "Shield Energy Shell";
        shell.transform.SetParent(shieldVisual.transform, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localScale = new Vector3(82f, 42f, 82f);

        Collider collider = shell.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = shell.GetComponent<Renderer>();
        shieldShellMaterial = CreateShieldMaterial();
        renderer.material = shieldShellMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        shieldRingA = CreateShieldRing("Shield Outer Ring", 46f, 1.5f, Quaternion.Euler(0f, 0f, 0f)).transform;
        shieldRingB = CreateShieldRing("Shield Tilt Ring", 39f, 1.1f, Quaternion.Euler(62f, 0f, 0f)).transform;
        shieldParticles = CreateShieldParticles();

        GameObject lightObject = new GameObject("Shield Pulse Light");
        lightObject.transform.SetParent(shieldVisual.transform, false);
        lightObject.transform.localPosition = Vector3.zero;
        shieldLight = lightObject.AddComponent<Light>();
        shieldLight.type = LightType.Point;
        shieldLight.color = visualTheme.ShieldLight;
        shieldLight.intensity = 1.7f;
        shieldLight.range = 48f;

        SetShieldVisible(false);
    }

    private Material CreateShieldMaterial()
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader)
        {
            name = "Runtime_Skill_Shield"
        };

        Color color = visualTheme.ShieldShell;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", visualTheme.ShieldEmission * 1.1f);
        }

        material.SetFloat("_Mode", 3f);
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private GameObject CreateShieldRing(string objectName, float radius, float width, Quaternion localRotation)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.SetParent(shieldVisual.transform, false);
        ringObject.transform.localPosition = Vector3.zero;
        ringObject.transform.localRotation = localRotation;

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 96;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 3;
        line.numCornerVertices = 3;
        line.material = CreateShieldAdditiveMaterial(visualTheme.ShieldRing);

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        return ringObject;
    }

    private ParticleSystem CreateShieldParticles()
    {
        GameObject node = new GameObject("Shield Energy Particles");
        node.SetActive(false);
        node.transform.SetParent(shieldVisual.transform, false);
        node.transform.localPosition = Vector3.zero;

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.85f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 1.05f);
        Color particleStart = visualTheme.ShieldParticleStart;
        particleStart.a = 0.35f;
        main.startColor = new ParticleSystem.MinMaxGradient(particleStart, Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 55f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 32f;
        shape.randomDirectionAmount = 0.45f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(visualTheme.ShieldParticleStart, 0f),
                new GradientColorKey(Color.white, 0.28f),
                new GradientColorKey(visualTheme.ShieldParticleEnd, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.48f, 0.28f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer particleRenderer = particles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.material = CreateShieldAdditiveMaterial(Color.white);

        node.SetActive(true);
        return particles;
    }

    private Material CreateShieldAdditiveMaterial(Color color)
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader)
        {
            name = "Runtime_Skill_Shield_Additive"
        };
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", color);
        }
        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }
        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        }
        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }
        material.renderQueue = 3000;
        return material;
    }

    private void AnimateShieldVisual()
    {
        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;
        if (shieldRingA != null)
        {
            shieldRingA.Rotate(Vector3.up, 210f * Time.deltaTime, Space.Self);
        }
        if (shieldRingB != null)
        {
            shieldRingB.Rotate(Vector3.forward, -155f * Time.deltaTime, Space.Self);
        }
        if (shieldLight != null)
        {
            shieldLight.intensity = Mathf.Lerp(1.15f, 2.35f, pulse);
            shieldLight.range = Mathf.Lerp(42f, 56f, pulse);
        }
        if (shieldShellMaterial != null && shieldShellMaterial.HasProperty("_Color"))
        {
            Color color = visualTheme.ShieldShell;
            color.a = Mathf.Lerp(0.14f, 0.26f, pulse);
            shieldShellMaterial.SetColor("_Color", color);
        }
        if (shieldShellMaterial != null && shieldShellMaterial.HasProperty("_EmissionColor"))
        {
            shieldShellMaterial.SetColor("_EmissionColor", visualTheme.ShieldEmission * Mathf.Lerp(0.75f, 1.45f, pulse));
        }
    }

    private void SetShieldVisible(bool visible)
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(visible);
        }
        if (shieldParticles != null)
        {
            if (visible)
            {
                shieldParticles.Play();
            }
            else
            {
                shieldParticles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
        }
    }

    private void CreateStatusText()
    {
        Canvas canvas = FindSceneCanvas();
        if (canvas == null)
            return;

        GameObject textObject = new GameObject("Skill Status Text");
        textObject.transform.SetParent(canvas.transform, false);
        statusText = textObject.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 25f;
        statusText.color = visualTheme.SkillStatusText;
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

    private Canvas FindSceneCanvas()
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

    private void UpdateStatusText()
    {
        if (statusText == null)
            return;

        string shieldText = IsShieldActive ? $"\nShield: {shieldTimeRemaining:0.0}s" : string.Empty;
        statusText.text = $"Skill X/C/V: {charges}\nCharge: {killProgress}/{GetCurrentKillsPerCharge()}{shieldText}";
    }

    private int GetCurrentKillsPerCharge()
    {
        return DifficultyManager.GetRequiredKillsForCharge(killsPerCharge);
    }

    private void MarkSkillBullet(GameObject bullet)
    {
        if (bullet == null)
            return;

        BulletLogic bulletLogic = bullet.GetComponent<BulletLogic>();
        if (bulletLogic != null)
        {
            bulletLogic.grantsSkillCharge = false;
        }
    }

    void OnDisable()
    {
        CleanupRuntimeState(false);
    }

    void OnDestroy()
    {
        CleanupRuntimeState(true);

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void CleanupRuntimeState(bool destroyUi)
    {
        if (laserRoutine != null)
        {
            StopCoroutine(laserRoutine);
            laserRoutine = null;
        }

        if (voidCollapseRoutine != null)
        {
            StopCoroutine(voidCollapseRoutine);
            voidCollapseRoutine = null;
        }

        StopAllCoroutines();

        if (skillLaser != null)
        {
            skillLaser.EndFire();
            Destroy(skillLaser.gameObject);
            skillLaser = null;
        }

        shieldTimeRemaining = 0f;
        SetShieldVisible(false);
        laserKilledEnemies.Clear();
        laserDamagedBosses.Clear();
        voidCollapseKilledEnemies.Clear();
        voidCollapseDamagedBosses.Clear();

        if (destroyUi && statusText != null)
        {
            Destroy(statusText.gameObject);
            statusText = null;
        }
    }
}
