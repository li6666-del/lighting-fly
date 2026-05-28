using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LaserBeam : MonoBehaviour
{
    [Header("Appearance")]
    public Color beamColor = new Color(0.2f, 1f, 0.85f, 1f);
    public float colorIntensity = 4f;
    public float startWidth = 1.2f;
    public float endWidth = 0.6f;
    [Range(0f, 1f)] public float widthJitter = 0.18f;
    public float textureScrollSpeed = -8f;

    [Header("Behavior")]
    public float maxDistance = 200f;
    public LayerMask hitMask = ~0;
    public bool fireOnStart = false;

    [Header("Hit Sparks")]
    public bool spawnHitSparks = true;
    public Color sparkColor = new Color(1f, 0.95f, 0.7f, 1f);

    private LineRenderer line;
    private Material runtimeMaterial;
    private ParticleSystem hitSparks;
    private bool isFiring;
    private Coroutine fireForCoroutine;
    private float jitterSeed;

    void Awake()
    {
        EnsureRenderer();
        line.enabled = false;
        jitterSeed = Random.value * 100f;

        if (spawnHitSparks)
        {
            hitSparks = CreateHitSparkSystem();
        }
    }

    void Start()
    {
        if (fireOnStart)
        {
            BeginFire();
        }
    }

    void Update()
    {
        if (!isFiring)
            return;

        Vector3 origin = transform.position;
        Vector3 direction = transform.forward;
        bool didHit = Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance, hitMask, QueryTriggerInteraction.Collide);
        Vector3 endPoint = didHit ? hit.point : origin + direction * maxDistance;

        line.SetPosition(0, origin);
        line.SetPosition(1, endPoint);

        float jitter = (Mathf.PerlinNoise(jitterSeed, Time.time * 18f) - 0.5f) * 2f * widthJitter;
        line.startWidth = Mathf.Max(0.05f, startWidth * (1f + jitter));
        line.endWidth = Mathf.Max(0.05f, endWidth * (1f + jitter * 0.6f));

        if (textureScrollSpeed != 0f && runtimeMaterial != null && runtimeMaterial.HasProperty("_MainTex"))
        {
            Vector2 offset = runtimeMaterial.mainTextureOffset;
            offset.x += textureScrollSpeed * Time.deltaTime;
            runtimeMaterial.mainTextureOffset = offset;
        }

        UpdateHitSparks(didHit, hit);
    }

    void OnDestroy()
    {
        if (fireForCoroutine != null)
        {
            StopCoroutine(fireForCoroutine);
        }
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }
        if (hitSparks != null)
        {
            Destroy(hitSparks.gameObject);
        }
    }

    public void BeginFire()
    {
        EnsureRenderer();
        isFiring = true;
        line.enabled = true;
    }

    public void EndFire()
    {
        isFiring = false;
        if (line != null)
        {
            line.enabled = false;
        }
        if (hitSparks != null && hitSparks.isPlaying)
        {
            hitSparks.Stop();
        }
    }

    public void FireFor(float duration)
    {
        if (fireForCoroutine != null)
        {
            StopCoroutine(fireForCoroutine);
        }

        fireForCoroutine = StartCoroutine(FireForRoutine(duration));
    }

    public void RefreshAppearance()
    {
        EnsureRenderer();
        if (runtimeMaterial != null)
        {
            Destroy(runtimeMaterial);
        }

        runtimeMaterial = CreateBeamMaterial();
        line.material = runtimeMaterial;

        if (hitSparks != null)
        {
            ParticleSystemRenderer renderer = hitSparks.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.material = runtimeMaterial;
            }
        }
    }

    private IEnumerator FireForRoutine(float duration)
    {
        BeginFire();
        yield return new WaitForSeconds(Mathf.Max(0.01f, duration));
        EndFire();
        fireForCoroutine = null;
    }

    private void EnsureRenderer()
    {
        if (line == null)
        {
            line = GetComponent<LineRenderer>();
        }

        line.positionCount = 2;
        line.useWorldSpace = true;
        line.numCapVertices = 4;
        line.numCornerVertices = 4;
        line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        line.receiveShadows = false;

        if (runtimeMaterial == null)
        {
            runtimeMaterial = CreateBeamMaterial();
            line.material = runtimeMaterial;
        }
    }

    private Material CreateBeamMaterial()
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader))
        {
            shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");
        }

        Material material = new Material(shader) { name = "Runtime_LaserBeam" };
        Color hdr = beamColor * colorIntensity;
        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", hdr);
        }
        if (material.HasProperty("_TintColor"))
        {
            material.SetColor("_TintColor", hdr);
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", hdr);
        }
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 4f);
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

    private void UpdateHitSparks(bool didHit, RaycastHit hit)
    {
        if (hitSparks == null)
            return;

        if (didHit)
        {
            hitSparks.transform.position = hit.point;
            hitSparks.transform.rotation = Quaternion.LookRotation(hit.normal);
            if (!hitSparks.isPlaying)
            {
                hitSparks.Play();
            }
        }
        else if (hitSparks.isPlaying)
        {
            hitSparks.Stop();
        }
    }

    private ParticleSystem CreateHitSparkSystem()
    {
        GameObject node = new GameObject("LaserBeam_HitSparks");
        node.transform.SetParent(transform, false);
        node.SetActive(false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(8f, 22f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startColor = new ParticleSystem.MinMaxGradient(sparkColor, Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 80;
        main.gravityModifier = 0.4f;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 70f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.1f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = runtimeMaterial;

        node.SetActive(true);
        particles.Stop();
        return particles;
    }
}
