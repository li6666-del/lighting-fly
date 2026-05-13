using TMPro;
using System.Collections;
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

    [Header("Forward Burst")]
    public int burstRows = 3;
    public int burstBulletsPerRow = 9;
    public float burstRowInterval = 0.08f;
    public float burstColumnSpacing = 8f;
    public float burstSpawnOffset = 12f;

    public bool IsShieldActive => shieldTimeRemaining > 0f;

    private int killProgress;
    private int charges;
    private float shieldTimeRemaining;
    private FireLogic fireLogic;
    private GameObject shieldVisual;
    private TextMeshProUGUI statusText;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
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
            TryUseForwardBurstSkill();
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

    public bool TryUseForwardBurstSkill()
    {
        if (charges <= 0)
            return false;

        charges--;
        StartCoroutine(FireForwardBurst());
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
                GameObject bullet = Instantiate(fireLogic.bulletPrefab, position, rotation);
                MarkSkillBullet(bullet);
                CombatEffects.SpawnMuzzleFlash(position, rotation);
            }

            yield return new WaitForSeconds(barrageWaveInterval);
        }
    }

    private IEnumerator FireForwardBurst()
    {
        if (fireLogic == null || fireLogic.bulletPrefab == null)
            yield break;

        Transform origin = fireLogic.firePoint != null ? fireLogic.firePoint : transform;
        int rows = Mathf.Max(1, burstRows);
        int columns = Mathf.Max(1, burstBulletsPerRow);
        float centerIndex = (columns - 1) * 0.5f;

        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                Vector3 sideOffset = origin.right * ((column - centerIndex) * burstColumnSpacing);
                Vector3 forwardOffset = origin.forward * (burstSpawnOffset + row * 5f);
                Vector3 position = origin.position + sideOffset + forwardOffset;
                Quaternion rotation = origin.rotation;

                GameObject bullet = Instantiate(fireLogic.bulletPrefab, position, rotation);
                MarkSkillBullet(bullet);
                CombatEffects.SpawnMuzzleFlash(position, rotation);
            }

            yield return new WaitForSeconds(burstRowInterval);
        }
    }

    private void CreateShieldVisual()
    {
        shieldVisual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shieldVisual.name = "Skill Shield Visual";
        shieldVisual.transform.SetParent(transform, false);
        shieldVisual.transform.localPosition = Vector3.zero;
        shieldVisual.transform.localScale = new Vector3(82f, 42f, 82f);

        Collider collider = shieldVisual.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Renderer renderer = shieldVisual.GetComponent<Renderer>();
        renderer.material = CreateShieldMaterial();
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

        Color color = new Color(0.15f, 0.8f, 1f, 0.16f);
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", new Color(0.05f, 0.75f, 1f) * 0.65f);
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

    private void SetShieldVisible(bool visible)
    {
        if (shieldVisual != null)
        {
            shieldVisual.SetActive(visible);
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
        statusText.text = $"Skill X/C: {charges}\nCharge: {killProgress}/{GetCurrentKillsPerCharge()}{shieldText}";
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

    void OnDestroy()
    {
        if (statusText != null)
        {
            Destroy(statusText.gameObject);
        }
    }
}
