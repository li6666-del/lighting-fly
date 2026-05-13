using TMPro;
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

        MoveBoss();
        TryShoot();
    }

    public void TakeDamage(int damage, Vector3 hitPosition)
    {
        if (dead)
            return;

        hp -= Mathf.Max(1, damage);
        CombatEffects.SpawnHit(hitPosition, (hitPosition - transform.position).normalized);
        UpdateHealthUi();

        if (hp <= 0)
        {
            Die();
        }
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
            Instantiate(bulletPrefab, position, rotation);
            CombatEffects.SpawnMuzzleFlash(position, rotation);
        }

        nextFireTime = Time.time + fireInterval;
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
            BloodManager.blood = 0;
            CombatEffects.SpawnExplosion(other.transform.position);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            BloodManager.blood = 0;
            CombatEffects.SpawnExplosion(collision.transform.position);
        }
    }

    void Die()
    {
        dead = true;
        if (scoreReward > 0)
        {
            ScoreManager.score += scoreReward;
        }

        Vector3 center = transform.position;
        CombatEffects.SpawnExplosion(center);
        CombatEffects.SpawnExplosion(center + transform.right * 35f);
        CombatEffects.SpawnExplosion(center - transform.right * 35f);
        CombatEffects.SpawnExplosion(center + transform.forward * 24f);

        if (hpSlider != null)
        {
            Destroy(hpSlider.gameObject);
        }

        if (owner != null)
        {
            owner.OnBossDefeated();
        }

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        if (hpSlider != null)
        {
            Destroy(hpSlider.gameObject);
        }
    }
}
