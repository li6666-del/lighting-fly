using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private readonly Dictionary<Renderer, Material[]> styledRenderers = new Dictionary<Renderer, Material[]>();
    private Material engineFlameMaterial;
    private float nextStyleRefreshTime;

    public void Startgame()
    {
        Time.timeScale = 1f;
        if (!StartIntroVideoTransition.PlayThenLoad("SetMenu1"))
        {
            SceneManager.LoadScene("SetMenu1");
        }
    }

    public void Quitgame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Start()
    {
        Application.targetFrameRate = 60;
        QualitySettings.vSyncCount = 0;
        BindStartMenuQuitButton();

        if (!IsGameplayScene())
            return;

        ConfigureSceneLighting();
        RefreshCombatVisuals();
    }

    private void BindStartMenuQuitButton()
    {
        if (SceneManager.GetActiveScene().name != "StartMenu")
            return;

        Button[] buttons = FindObjectsOfType<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            string labelText = label != null ? label.text : string.Empty;
            bool isQuitButton = button.name.Contains("结束") || labelText.Contains("Quit") || labelText.Contains("退出");
            if (!isQuitButton)
                continue;

            button.onClick.RemoveListener(Quitgame);
            button.onClick.AddListener(Quitgame);
        }
    }

    void Update()
    {
        if (!IsGameplayScene())
            return;

        if (Time.time < nextStyleRefreshTime)
            return;

        nextStyleRefreshTime = Time.time + 0.5f;
        RefreshCombatVisuals();
    }

    private bool IsGameplayScene()
    {
        return SceneManager.GetActiveScene().name.StartsWith("GameScene");
    }

    private void ConfigureSceneLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.52f, 0.58f, 0.7f);
        RenderSettings.ambientIntensity = 1.6f;
        RenderSettings.reflectionIntensity = 1.2f;

        EnsureDirectionalLight(
            "Visual Key Light",
            Quaternion.Euler(38f, -28f, 0f),
            new Color(1f, 0.9f, 0.78f),
            2.6f
        );

        EnsureDirectionalLight(
            "Visual Rim Light",
            Quaternion.Euler(-18f, 145f, 0f),
            new Color(0.3f, 0.85f, 1f),
            1.7f
        );
    }

    private void EnsureDirectionalLight(string lightName, Quaternion rotation, Color color, float intensity)
    {
        GameObject existing = GameObject.Find(lightName);
        Light light = existing != null ? existing.GetComponent<Light>() : null;

        if (light == null)
        {
            GameObject node = existing != null ? existing : new GameObject(lightName);
            light = node.AddComponent<Light>();
        }

        light.gameObject.transform.rotation = rotation;
        light.type = LightType.Directional;
        light.color = color;
        light.intensity = intensity;
    }

    private void RefreshCombatVisuals()
    {
        ApplyStyleToTaggedObjects(
            "Enemy",
            new Color(0.75f, 0.03f, 0.01f),
            new Color(1f, 0.18f, 0.04f),
            0.55f,
            18f
        );
    }

    private void ApplyStyleToTaggedObjects(string tagName, Color bodyColor, Color glowColor, float lightIntensity, float lightRange)
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
            if (obj == null)
                continue;

            AddObjectFillLight(obj.transform, glowColor, lightIntensity, lightRange);
            TintRenderers(obj, bodyColor, glowColor);
            AddEngineFlame(obj.transform, glowColor, tagName == "Player");
        }
    }

    private void AddObjectFillLight(Transform root, Color color, float intensity, float range)
    {
        Transform existing = root.Find("Runtime Visual Glow");
        Light light;

        if (existing == null)
        {
            GameObject node = new GameObject("Runtime Visual Glow");
            node.transform.SetParent(root, false);
            node.transform.localPosition = new Vector3(0f, 8f, -18f);
            light = node.AddComponent<Light>();
            light.type = LightType.Point;
        }
        else
        {
            light = existing.GetComponent<Light>();
            if (light == null)
            {
                light = existing.gameObject.AddComponent<Light>();
            }
        }

        light.color = color;
        light.intensity = intensity;
        light.range = range;
    }

    private void TintRenderers(GameObject root, Color bodyColor, Color glowColor)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer || styledRenderers.ContainsKey(renderer))
                continue;

            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                if (material == null)
                    continue;

                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", glowColor * 0.55f);
                }

                if (material.HasProperty("_Color"))
                {
                    material.color = Color.Lerp(bodyColor, Color.white, 0.22f);
                }

                if (material.HasProperty("_Metallic"))
                {
                    material.SetFloat("_Metallic", 0.85f);
                }

                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", 0.78f);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.78f);
                }
            }

            styledRenderers.Add(renderer, materials);
        }
    }

    private void AddEngineFlame(Transform root, Color color, bool isPlayer)
    {
        if (root.Find("Runtime Engine Flame") != null)
            return;

        GameObject node = new GameObject("Runtime Engine Flame");
        node.transform.SetParent(root, false);
        node.transform.localPosition = EstimateEngineLocalPosition(root, isPlayer);
        node.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);

        Light light = node.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = isPlayer ? 3.5f : 0.65f;
        light.range = isPlayer ? 65f : 16f;

        ParticleSystem flame = CreateEngineParticle(node.transform, color, isPlayer);
        ParticleSystem smoke = CreateEngineSmoke(node.transform, isPlayer);

        flame.Play();
        smoke.Play();
    }

    private Vector3 EstimateEngineLocalPosition(Transform root, bool isPlayer)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Vector3(0f, 0f, isPlayer ? -18f : -12f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = root.InverseTransformPoint(bounds.center);
        float depth = Mathf.Max(
            Vector3.Distance(
                root.InverseTransformPoint(bounds.min),
                root.InverseTransformPoint(bounds.max)
            ) * 0.18f,
            isPlayer ? 14f : 9f
        );

        return new Vector3(localCenter.x, localCenter.y, localCenter.z - depth);
    }

    private ParticleSystem CreateEngineParticle(Transform parent, Color color, bool isPlayer)
    {
        GameObject node = new GameObject("Flame Core");
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.6f;
        main.startLifetime = isPlayer ? 0.28f : 0.18f;
        main.startSpeed = isPlayer ? new ParticleSystem.MinMaxCurve(35f, 70f) : new ParticleSystem.MinMaxCurve(18f, 38f);
        main.startSize = isPlayer ? new ParticleSystem.MinMaxCurve(6f, 13f) : new ParticleSystem.MinMaxCurve(1.2f, 2.8f);
        main.startColor = isPlayer
            ? new ParticleSystem.MinMaxGradient(Color.white, color)
            : new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, 0.12f), color * 0.32f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = isPlayer ? 95f : 18f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = isPlayer ? 3.2f : 0.65f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetEngineFlameMaterial();

        node.SetActive(true);
        return particles;
    }

    private ParticleSystem CreateEngineSmoke(Transform parent, bool isPlayer)
    {
        GameObject node = new GameObject("Heat Haze");
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.8f;
        main.startLifetime = 0.45f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(12f, 24f);
        main.startSize = isPlayer ? new ParticleSystem.MinMaxCurve(8f, 18f) : new ParticleSystem.MinMaxCurve(6f, 13f);
        main.startColor = new Color(0.18f, 0.22f, 0.26f, 0.28f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = isPlayer ? 22f : 5f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = isPlayer ? 4f : 0.75f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetEngineFlameMaterial();

        node.SetActive(true);
        return particles;
    }

    private Material GetEngineFlameMaterial()
    {
        if (engineFlameMaterial != null)
            return engineFlameMaterial;

        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader))
        {
            shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");
        }

        engineFlameMaterial = new Material(shader)
        {
            name = "Runtime_Engine_Flame"
        };
        engineFlameMaterial.SetColor("_Color", Color.white);
        engineFlameMaterial.SetColor("_TintColor", Color.white);
        return engineFlameMaterial;
    }
}
