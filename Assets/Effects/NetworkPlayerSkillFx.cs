using UnityEngine;

public static class NetworkPlayerSkillFx
{
    public static void PlayShield(Transform parent, float duration)
    {
        PlayShield(parent, duration, PlayerShipColorSelection.BlueTheme);
    }

    public static void PlayShield(Transform parent, float duration, PlayerShipVisualTheme theme)
    {
        if (parent == null)
            return;

        GameObject shieldObject = new GameObject("Network Skill Shield Visual");
        shieldObject.transform.SetParent(parent, false);
        shieldObject.transform.localPosition = Vector3.zero;
        NetworkSkillShieldFx shieldFx = shieldObject.AddComponent<NetworkSkillShieldFx>();
        shieldFx.Initialize(duration, theme);
    }

    public static LaserBeam CreateLaser(Transform origin, Color color, float intensity, float range, float startWidth, float endWidth)
    {
        if (origin == null)
            return null;

        GameObject laserObject = new GameObject("Network Player Skill Laser");
        laserObject.transform.SetParent(origin, false);
        laserObject.transform.localPosition = Vector3.zero;
        laserObject.transform.localRotation = Quaternion.identity;

        LaserBeam laser = laserObject.AddComponent<LaserBeam>();
        laser.beamColor = color;
        laser.colorIntensity = intensity;
        laser.startWidth = startWidth;
        laser.endWidth = endWidth;
        laser.widthJitter = 0.22f;
        laser.maxDistance = range;
        laser.hitMask = 0;
        laser.spawnHitSparks = true;
        laser.sparkColor = Color.white;
        laser.RefreshAppearance();
        return laser;
    }

    public static void PlayVoidSingularity(Vector3 position, float duration, float radius)
    {
        PlayVoidSingularity(position, duration, radius, PlayerShipColorSelection.BlueTheme);
    }

    public static void PlayVoidSingularity(Vector3 position, float duration, float radius, PlayerShipVisualTheme theme)
    {
        GameObject singularityObject = new GameObject("Void Singularity Skill Visual");
        singularityObject.transform.position = position;
        VoidSingularityFx singularity = singularityObject.AddComponent<VoidSingularityFx>();
        singularity.Initialize(duration, radius, theme);
    }

    public static void PlayVoidCrush(Vector3 position)
    {
        PlayVoidCrush(position, PlayerShipColorSelection.BlueTheme);
    }

    public static void PlayVoidCrush(Vector3 position, PlayerShipVisualTheme theme)
    {
        GameObject crushObject = new GameObject("Void Crush Hit");
        crushObject.transform.position = position;
        VoidCrushFx crush = crushObject.AddComponent<VoidCrushFx>();
        crush.Initialize(theme);
    }
}

public static class VoidSingularitySkill
{
    public static Vector3 FindBestCenter(Transform owner, Transform firePoint, float scanRange, float clusterRadius, float fallbackDistance)
    {
        Transform origin = firePoint != null ? firePoint : owner;
        if (origin == null)
            return Vector3.forward * fallbackDistance;

        Vector3 originPosition = origin.position;
        Vector3 forward = origin.forward.sqrMagnitude > 0.001f ? origin.forward.normalized : Vector3.forward;
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>();
        Enemy bestEnemy = null;
        int bestCount = 0;
        float bestDistanceScore = float.MaxValue;
        float scanRangeSqr = scanRange * scanRange;
        float clusterRadiusSqr = clusterRadius * clusterRadius;

        foreach (Enemy candidate in enemies)
        {
            if (candidate == null || !candidate.gameObject.activeInHierarchy)
                continue;

            Vector3 toCandidate = candidate.transform.position - originPosition;
            if (toCandidate.sqrMagnitude > scanRangeSqr || Vector3.Dot(forward, toCandidate.normalized) <= 0.08f)
                continue;

            int count = 0;
            Vector3 clusterCenter = Vector3.zero;
            foreach (Enemy other in enemies)
            {
                if (other == null || !other.gameObject.activeInHierarchy)
                    continue;

                Vector3 toOther = other.transform.position - candidate.transform.position;
                if (toOther.sqrMagnitude <= clusterRadiusSqr)
                {
                    count++;
                    clusterCenter += other.transform.position;
                }
            }

            float distanceScore = toCandidate.sqrMagnitude;
            if (count > bestCount || (count == bestCount && distanceScore < bestDistanceScore))
            {
                bestCount = count;
                bestEnemy = candidate;
                bestDistanceScore = distanceScore;
            }
        }

        Vector3 center = bestEnemy != null ? GetClusterAverage(bestEnemy, enemies, clusterRadiusSqr) : originPosition + forward * fallbackDistance;
        return ClampCenterInFront(originPosition, forward, center, fallbackDistance);
    }

    public static void PullEnemies(Vector3 center, float radius, float strength)
    {
        Enemy[] enemies = Object.FindObjectsOfType<Enemy>();
        float radiusSqr = radius * radius;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            Vector3 offset = center - enemy.transform.position;
            float distanceSqr = offset.sqrMagnitude;
            if (distanceSqr > radiusSqr || distanceSqr < 0.01f)
                continue;

            float distance = Mathf.Sqrt(distanceSqr);
            float pull = Mathf.Lerp(strength * 1.25f, strength * 0.35f, Mathf.Clamp01(distance / radius));
            enemy.transform.position = Vector3.MoveTowards(enemy.transform.position, center, pull * Time.deltaTime);
        }
    }

    private static Vector3 GetClusterAverage(Enemy bestEnemy, Enemy[] enemies, float clusterRadiusSqr)
    {
        Vector3 center = Vector3.zero;
        int count = 0;
        foreach (Enemy enemy in enemies)
        {
            if (enemy == null || !enemy.gameObject.activeInHierarchy)
                continue;

            if ((enemy.transform.position - bestEnemy.transform.position).sqrMagnitude <= clusterRadiusSqr)
            {
                center += enemy.transform.position;
                count++;
            }
        }

        return count > 0 ? center / count : bestEnemy.transform.position;
    }

    private static Vector3 ClampCenterInFront(Vector3 origin, Vector3 forward, Vector3 center, float fallbackDistance)
    {
        Vector3 toCenter = center - origin;
        float forwardDistance = Vector3.Dot(toCenter, forward);
        float minDistance = Mathf.Max(120f, fallbackDistance * 0.42f);
        if (forwardDistance < minDistance)
        {
            center += forward * (minDistance - forwardDistance);
        }

        return center;
    }
}

public class VoidSingularityFx : MonoBehaviour
{
    private float duration;
    private float radius;
    private float elapsed;
    private readonly System.Collections.Generic.List<Transform> rings = new System.Collections.Generic.List<Transform>();
    private readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();
    private Transform core;
    private Transform eventHorizonHalo;
    private Transform lensingBand;
    private Transform shadowField;
    private Light pulseLight;
    private bool collapsed;
    private PlayerShipVisualTheme visualTheme;

    public void Initialize(float duration, float radius)
    {
        Initialize(duration, radius, PlayerShipColorSelection.BlueTheme);
    }

    public void Initialize(float duration, float radius, PlayerShipVisualTheme theme)
    {
        visualTheme = theme;
        this.duration = Mathf.Max(0.2f, duration);
        this.radius = Mathf.Max(300f, radius * 1.25f);
        CreateCore();
        CreateEventHorizonHalo();
        CreateShadowField();
        CreateRings();
        CreateAccretionStreams();
        CreateInwardParticles();
        CreateTearLines();
        CreateLight();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float life = Mathf.Clamp01(elapsed / duration);
        float pulse = (Mathf.Sin(Time.time * 18f) + 1f) * 0.5f;
        float collapseEase = life > 0.82f ? Mathf.InverseLerp(0.82f, 1f, life) : 0f;

        if (core != null)
        {
            float coreScale = Mathf.Lerp(radius * 0.26f, radius * 0.15f, collapseEase) * Mathf.Lerp(0.9f, 1.1f, pulse);
            core.localScale = Vector3.one * coreScale;
        }

        if (eventHorizonHalo != null)
        {
            eventHorizonHalo.Rotate(Vector3.up, 440f * Time.deltaTime, Space.Self);
            float haloScale = Mathf.Lerp(1f, 0.62f, collapseEase) * Mathf.Lerp(0.92f, 1.08f, pulse);
            eventHorizonHalo.localScale = Vector3.one * haloScale;
        }

        if (lensingBand != null)
        {
            lensingBand.Rotate(Vector3.up, -185f * Time.deltaTime, Space.Self);
            float bandScale = Mathf.Lerp(1f, 0.7f, collapseEase) * Mathf.Lerp(0.96f, 1.04f, pulse);
            lensingBand.localScale = Vector3.one * bandScale;
        }

        if (shadowField != null)
        {
            float fieldPulse = Mathf.Lerp(0.96f, 1.05f, pulse) * Mathf.Lerp(1f, 0.78f, collapseEase);
            shadowField.localScale = new Vector3(radius * 2.25f * fieldPulse, radius * 0.08f, radius * 1.18f * fieldPulse);
        }

        for (int i = 0; i < rings.Count; i++)
        {
            if (rings[i] == null)
                continue;

            float direction = i % 2 == 0 ? 1f : -1f;
            rings[i].Rotate(Vector3.up, direction * (220f + i * 80f) * Time.deltaTime, Space.Self);
            float ringScale = Mathf.Lerp(1f, 0.72f, collapseEase) * Mathf.Lerp(0.96f, 1.04f, pulse);
            rings[i].localScale = Vector3.one * ringScale;
        }

        if (pulseLight != null)
        {
            pulseLight.intensity = Mathf.Lerp(5.2f, 13f, pulse) * Mathf.Lerp(1f, 1.8f, collapseEase);
            pulseLight.range = Mathf.Lerp(radius * 0.9f, radius * 1.25f, pulse);
        }

        if (elapsed >= duration && !collapsed)
        {
            collapsed = true;
            VoidCollapseWave.Spawn(transform.position, radius * 1.08f, visualTheme);
            Camera camera = Camera.main;
            if (camera != null)
            {
                BossCameraShake shake = camera.GetComponent<BossCameraShake>();
                if (shake == null)
                {
                    shake = camera.gameObject.AddComponent<BossCameraShake>();
                }
                shake.Play(0.32f, 0.22f);
            }
            Destroy(gameObject, 0.04f);
        }
    }

    void OnDestroy()
    {
        foreach (Material material in materials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }

    private void CreateCore()
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Void Event Core";
        sphere.transform.SetParent(transform, false);
        sphere.transform.localPosition = Vector3.zero;
        core = sphere.transform;

        Collider collider = sphere.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Material material = CreateCoreMaterial();
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        materials.Add(material);
    }

    private void CreateEventHorizonHalo()
    {
        eventHorizonHalo = CreateOrbitLine(
            "Void Photon Ring",
            radius * 0.32f,
            0.58f,
            radius * 0.018f,
            Quaternion.Euler(8f, 0f, 0f),
            WithAlpha(visualTheme.VoidInnerRing, 0.86f),
            192).transform;

        lensingBand = CreateOrbitLine(
            "Void Gravitational Lensing Band",
            radius * 0.42f,
            0.36f,
            radius * 0.01f,
            Quaternion.Euler(-12f, 0f, 19f),
            WithAlpha(visualTheme.VoidTear, 0.42f),
            224).transform;
    }

    private void CreateRings()
    {
        CreateRing("Void Inner Event Horizon", radius * 0.35f, radius * 0.015f, Quaternion.Euler(8f, 0f, 0f), visualTheme.VoidInnerRing, 192);
        CreateRing("Void Deep Accretion Ring", radius * 0.58f, radius * 0.032f, Quaternion.Euler(18f, 0f, 34f), new Color(0.17f, 0.04f, 0.35f, 0.58f), 224);
        CreateBrokenAccretionArcs();
    }

    private GameObject CreateOrbitLine(string name, float ringRadius, float ellipse, float width, Quaternion localRotation, Color color, int segments)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localRotation = localRotation;

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 5;
        line.numCornerVertices = 4;
        Material material = CreateAdditiveMaterial(color, "Runtime_Void_Orbit_Line");
        line.material = material;
        materials.Add(material);

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float ripple = 1f + Mathf.Sin(angle * 6f) * 0.018f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius * ripple, 0f, Mathf.Sin(angle) * ringRadius * ellipse * ripple));
        }

        return ringObject;
    }

    private void CreateRing(string name, float ringRadius, float width, Quaternion localRotation, Color color, int segments)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(transform, false);
        ringObject.transform.localRotation = localRotation;
        rings.Add(ringObject.transform);

        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = segments;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 4;
        line.numCornerVertices = 3;
        Material material = CreateAdditiveMaterial(color, "Runtime_Void_Ring");
        line.material = material;
        materials.Add(material);

        for (int i = 0; i < segments; i++)
        {
            float angle = i / (float)segments * Mathf.PI * 2f;
            float ripple = 1f + Mathf.Sin(angle * 7f) * 0.035f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius * ripple, 0f, Mathf.Sin(angle) * ringRadius * 0.42f * ripple));
        }
    }

    private void CreateBrokenAccretionArcs()
    {
        GameObject arcRoot = new GameObject("Void Broken Accretion Arcs");
        arcRoot.transform.SetParent(transform, false);
        arcRoot.transform.localRotation = Quaternion.Euler(-18f, 0f, -12f);
        rings.Add(arcRoot.transform);

        Material material = CreateAdditiveMaterial(new Color(0.12f, 0.09f, 0.22f, 0.32f), "Runtime_Void_Broken_Accretion");
        materials.Add(material);

        const int arcCount = 12;
        for (int i = 0; i < arcCount; i++)
        {
            GameObject arc = new GameObject("Void Accretion Fragment");
            arc.transform.SetParent(arcRoot.transform, false);
            arc.transform.localRotation = Quaternion.Euler(Random.Range(-10f, 10f), Random.Range(0f, 360f), Random.Range(-6f, 6f));

            LineRenderer line = arc.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            int segments = Random.Range(18, 34);
            line.positionCount = segments;
            float width = radius * Random.Range(0.006f, 0.014f);
            line.startWidth = width;
            line.endWidth = width * Random.Range(0.35f, 0.7f);
            line.numCapVertices = 3;
            line.numCornerVertices = 2;
            line.material = material;

            float arcRadius = radius * Random.Range(0.64f, 0.98f);
            float ellipse = Random.Range(0.36f, 0.48f);
            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            float span = Random.Range(16f, 44f) * Mathf.Deg2Rad;
            for (int j = 0; j < segments; j++)
            {
                float t = j / Mathf.Max(1f, segments - 1f);
                float angle = startAngle + span * t;
                float ragged = 1f + Mathf.Sin(angle * 9f + i) * 0.018f + Random.Range(-0.012f, 0.012f);
                line.SetPosition(j, new Vector3(Mathf.Cos(angle) * arcRadius * ragged, 0f, Mathf.Sin(angle) * arcRadius * ellipse * ragged));
            }
        }
    }

    private void CreateAccretionStreams()
    {
        GameObject streamRoot = new GameObject("Void Inward Accretion Streams");
        streamRoot.transform.SetParent(transform, false);
        streamRoot.transform.localRotation = Quaternion.Euler(14f, 0f, -10f);
        rings.Add(streamRoot.transform);

        const int streamCount = 18;
        for (int i = 0; i < streamCount; i++)
        {
            GameObject stream = new GameObject("Void Inward Stream");
            stream.transform.SetParent(streamRoot.transform, false);
            stream.transform.localRotation = Quaternion.Euler(Random.Range(-6f, 6f), Random.Range(0f, 360f), Random.Range(-4f, 4f));

            LineRenderer line = stream.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.loop = false;
            line.positionCount = 14;
            line.startWidth = radius * Random.Range(0.006f, 0.012f);
            line.endWidth = radius * Random.Range(0.0015f, 0.0035f);
            line.numCapVertices = 3;
            line.numCornerVertices = 2;

            Color streamColor = Color.Lerp(visualTheme.VoidTear, visualTheme.VoidInnerRing, Random.Range(0.25f, 0.75f));
            streamColor.a = Random.Range(0.22f, 0.48f);
            Material material = CreateAdditiveMaterial(streamColor, "Runtime_Void_Inward_Stream");
            line.material = material;
            materials.Add(material);

            float startAngle = Random.Range(0f, Mathf.PI * 2f);
            float spiral = Random.Range(0.55f, 1.25f) * Mathf.PI;
            float startRadius = radius * Random.Range(0.7f, 1.08f);
            float endRadius = radius * Random.Range(0.18f, 0.32f);
            float verticalOffset = Random.Range(-radius * 0.035f, radius * 0.035f);

            for (int j = 0; j < line.positionCount; j++)
            {
                float t = j / Mathf.Max(1f, line.positionCount - 1f);
                float eased = 1f - (1f - t) * (1f - t);
                float angle = startAngle + spiral * eased;
                float streamRadius = Mathf.Lerp(startRadius, endRadius, eased);
                float wobble = 1f + Mathf.Sin(t * Mathf.PI * 4f + i) * 0.025f;
                line.SetPosition(j, new Vector3(
                    Mathf.Cos(angle) * streamRadius * wobble,
                    verticalOffset * (1f - t),
                    Mathf.Sin(angle) * streamRadius * 0.42f * wobble));
            }
        }
    }

    private void CreateInwardParticles()
    {
        GameObject node = new GameObject("Void Fine Inward Dust");
        node.SetActive(false);
        node.transform.SetParent(transform, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.85f, 1.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 1.8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.22f, 0.72f);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 1500;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 780f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.radiusThickness = 0.78f;
        shape.randomDirectionAmount = 0.02f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.radial = new ParticleSystem.MinMaxCurve(-radius * 1.12f, -radius * 0.52f);
        velocity.orbitalX = new ParticleSystem.MinMaxCurve(0f, 0f);
        velocity.orbitalY = new ParticleSystem.MinMaxCurve(-2.4f, 2.4f);
        velocity.orbitalZ = new ParticleSystem.MinMaxCurve(0f, 0f);

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.04f, 0.06f, 0.14f), 0f),
                new GradientColorKey(new Color(0.22f, 0.08f, 0.42f), 0.46f),
                new GradientColorKey(visualTheme.VoidCrushStart, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.46f, 0.28f),
                new GradientAlphaKey(0.72f, 0.82f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.15f),
                new Keyframe(0.35f, 0.75f),
                new Keyframe(0.82f, 1f),
                new Keyframe(1f, 0f)));

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 0.42f;
        noise.frequency = 1.7f;
        noise.scrollSpeed = 0.95f;
        noise.damping = true;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        Material material = CreateAdditiveMaterial(Color.white, "Runtime_Void_Fine_Dust");
        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", GetSoftParticleTexture());
        }
        renderer.material = material;
        renderer.sortingOrder = 8;
        materials.Add(material);
        node.SetActive(true);
        particles.Play();
    }

    private void CreateTearLines()
    {
        Material material = CreateAdditiveMaterial(visualTheme.VoidTear, "Runtime_Void_Gravity_Tears");
        materials.Add(material);
        for (int i = 0; i < 24; i++)
        {
            GameObject tear = new GameObject("Void Gravity Tear");
            tear.transform.SetParent(transform, false);
            tear.transform.localRotation = Quaternion.Euler(Random.Range(-24f, 24f), Random.Range(0f, 360f), Random.Range(-10f, 10f));
            rings.Add(tear.transform);

            LineRenderer line = tear.AddComponent<LineRenderer>();
            line.useWorldSpace = false;
            line.positionCount = 2;
            line.startWidth = radius * Random.Range(0.0025f, 0.006f);
            line.endWidth = 0f;
            line.numCapVertices = 2;
            line.material = material;
            float start = Random.Range(radius * 0.28f, radius * 0.92f);
            float length = Random.Range(radius * 0.08f, radius * 0.22f);
            line.SetPosition(0, new Vector3(start, Random.Range(-radius * 0.08f, radius * 0.08f), 0f));
            line.SetPosition(1, new Vector3(Mathf.Max(radius * 0.08f, start - length), 0f, 0f));
        }
    }

    private void CreateLight()
    {
        GameObject lightObject = new GameObject("Void Event Horizon Light");
        lightObject.transform.SetParent(transform, false);
        pulseLight = lightObject.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = visualTheme.VoidLight;
        pulseLight.intensity = 7.5f;
        pulseLight.range = radius * 0.95f;
    }

    private void CreateShadowField()
    {
        GameObject field = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        field.name = "Void Shadow Pressure Field";
        field.transform.SetParent(transform, false);
        field.transform.localPosition = Vector3.zero;
        field.transform.localRotation = Quaternion.Euler(10f, 0f, 18f);
        shadowField = field.transform;

        Collider collider = field.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }

        Material material = CreateShadowMaterial();
        Renderer renderer = field.GetComponent<Renderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        materials.Add(material);
    }

    private Material CreateCoreMaterial()
    {
        Shader shader = CombatEffects.FindRuntimeShader("Standard", "Unlit/Color");

        Material material = new Material(shader) { name = "Runtime_Void_Black_Core" };
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(0.001f, 0.001f, 0.004f, 1f));
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", Color.black);
        }
        if (material.HasProperty("_Glossiness"))
            material.SetFloat("_Glossiness", 0.2f);
        return material;
    }

    private Material CreateShadowMaterial()
    {
        Shader shader = CombatEffects.FindRuntimeShader("Standard", "Unlit/Color");

        Material material = new Material(shader) { name = "Runtime_Void_Shadow_Field" };
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", new Color(0f, 0f, 0.01f, 0.42f));
        if (material.HasProperty("_Mode"))
            material.SetFloat("_Mode", 3f);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 2990;
        return material;
    }

    private static Material CreateAdditiveMaterial(Color color, string name)
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader))
            shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_TintColor"))
            material.SetColor("_TintColor", color);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Texture2D GetSoftParticleTexture()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime_Void_Soft_Dot",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };
        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        float radius = size * 0.5f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - distance);
                alpha = alpha * alpha * alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply(false, true);
        return texture;
    }
}

public class VoidCollapseWave : MonoBehaviour
{
    private float radius;
    private float elapsed;
    private float duration;
    private LineRenderer coldRing;
    private LineRenderer darkRing;
    private PlayerShipVisualTheme visualTheme;
    private readonly System.Collections.Generic.List<Material> materials = new System.Collections.Generic.List<Material>();

    public static void Spawn(Vector3 position, float radius)
    {
        Spawn(position, radius, PlayerShipColorSelection.BlueTheme);
    }

    public static void Spawn(Vector3 position, float radius, PlayerShipVisualTheme theme)
    {
        GameObject waveObject = new GameObject("Void Collapse Wave");
        waveObject.transform.position = position;
        VoidCollapseWave wave = waveObject.AddComponent<VoidCollapseWave>();
        wave.Initialize(radius, theme);
    }

    public void Initialize(float radius)
    {
        Initialize(radius, PlayerShipColorSelection.BlueTheme);
    }

    public void Initialize(float radius, PlayerShipVisualTheme theme)
    {
        visualTheme = theme;
        this.radius = Mathf.Max(10f, radius);
        duration = 0.46f;
        coldRing = CreateRing("Void Cold Collapse Ring", visualTheme.VoidCollapseRing, this.radius * 0.011f);
        darkRing = CreateRing("Void Deep Collapse Ring", new Color(0.08f, 0.015f, 0.22f, 0.64f), this.radius * 0.035f);

        Light flash = gameObject.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = visualTheme.VoidCollapseFlash;
        flash.intensity = 3.2f;
        flash.range = this.radius * 0.62f;
        Destroy(flash, 0.12f);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - (1f - t) * (1f - t);
        float coldRadius = Mathf.Lerp(radius * 0.12f, radius * 0.95f, eased);
        float darkRadius = Mathf.Lerp(radius * 0.05f, radius * 0.82f, eased);
        SetRing(coldRing, coldRadius, 0.18f * (1f - t));
        SetRing(darkRing, darkRadius, 0.42f * (1f - t));

        if (t >= 1f)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        foreach (Material material in materials)
        {
            if (material != null)
            {
                Destroy(material);
            }
        }
    }

    private LineRenderer CreateRing(string name, Color color, float width)
    {
        GameObject ringObject = new GameObject(name);
        ringObject.transform.SetParent(transform, false);
        LineRenderer line = ringObject.AddComponent<LineRenderer>();
        line.useWorldSpace = false;
        line.loop = true;
        line.positionCount = 160;
        line.startWidth = width;
        line.endWidth = width;
        line.numCapVertices = 3;
        Material material = VoidSingularityFx_CreateAdditiveMaterial(color, "Runtime_Void_Collapse_Wave");
        line.material = material;
        materials.Add(material);
        return line;
    }

    private void SetRing(LineRenderer line, float ringRadius, float alpha)
    {
        if (line == null)
            return;

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * ringRadius, 0f, Mathf.Sin(angle) * ringRadius));
        }

        Gradient gradient = new Gradient();
        Color color = Color.white;
        if (line.material != null && line.material.HasProperty("_Color"))
        {
            color = line.material.GetColor("_Color");
        }
        color.a = alpha;
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(0f, 1f) }
        );
        line.colorGradient = gradient;
    }

    private static Material VoidSingularityFx_CreateAdditiveMaterial(Color color, string name)
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader))
            shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");

        Material material = new Material(shader) { name = name };
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_TintColor"))
            material.SetColor("_TintColor", color);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }
}

public class VoidCrushFx : MonoBehaviour
{
    private float elapsed;
    private const float Duration = 0.28f;
    private ParticleSystem particles;
    private Material material;
    private PlayerShipVisualTheme visualTheme;

    public void Initialize()
    {
        Initialize(PlayerShipColorSelection.BlueTheme);
    }

    public void Initialize(PlayerShipVisualTheme theme)
    {
        visualTheme = theme;
        GameObject particleObject = new GameObject("Void Crush Fine Particles");
        particleObject.SetActive(false);
        particleObject.transform.SetParent(transform, false);
        particles = particleObject.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = Duration;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.18f, 0.34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4f, 14f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.46f);
        main.startColor = Color.white;
        main.maxParticles = 42;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)34) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 6f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(visualTheme.VoidCrushStart, 0f),
                new GradientColorKey(new Color(0.12f, 0.04f, 0.28f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.72f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        material = CreateMaterial();
        renderer.material = material;
        particleObject.SetActive(true);
        particles.Play();
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        if (elapsed >= Duration)
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (material != null)
        {
            Destroy(material);
        }
    }

    private Material CreateMaterial()
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader))
            shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");
        return new Material(shader) { name = "Runtime_Void_Crush" };
    }
}

public class NetworkSkillShieldFx : MonoBehaviour
{
    private float remainingTime;
    private Material shellMaterial;
    private Transform ringA;
    private Transform ringB;
    private Light pulseLight;
    private ParticleSystem particles;
    private Material ringMaterial;
    private Material particleMaterial;
    private PlayerShipVisualTheme visualTheme;

    public void Initialize(float duration)
    {
        Initialize(duration, PlayerShipColorSelection.BlueTheme);
    }

    public void Initialize(float duration, PlayerShipVisualTheme theme)
    {
        visualTheme = theme;
        remainingTime = Mathf.Max(0.1f, duration);
        CreateShell();
        ringA = CreateRing("Network Shield Outer Ring", 46f, 1.5f, Quaternion.identity).transform;
        ringB = CreateRing("Network Shield Tilt Ring", 39f, 1.1f, Quaternion.Euler(62f, 0f, 0f)).transform;
        particles = CreateParticles();
        CreateLight();
    }

    void Update()
    {
        remainingTime -= Time.deltaTime;
        if (remainingTime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        transform.Rotate(Vector3.up, 140f * Time.deltaTime, Space.Self);
        float pulse = (Mathf.Sin(Time.time * 8f) + 1f) * 0.5f;

        if (ringA != null)
            ringA.Rotate(Vector3.up, 210f * Time.deltaTime, Space.Self);
        if (ringB != null)
            ringB.Rotate(Vector3.forward, -155f * Time.deltaTime, Space.Self);
        if (pulseLight != null)
        {
            pulseLight.intensity = Mathf.Lerp(1.15f, 2.35f, pulse);
            pulseLight.range = Mathf.Lerp(42f, 56f, pulse);
        }
        if (shellMaterial != null && shellMaterial.HasProperty("_Color"))
        {
            Color shellColor = visualTheme.ShieldShell;
            shellColor.a = Mathf.Lerp(0.14f, 0.26f, pulse);
            shellMaterial.SetColor("_Color", shellColor);
        }
        if (shellMaterial != null && shellMaterial.HasProperty("_EmissionColor"))
        {
            shellMaterial.SetColor("_EmissionColor", visualTheme.ShieldEmission * Mathf.Lerp(0.75f, 1.45f, pulse));
        }
    }

    void OnDestroy()
    {
        if (shellMaterial != null)
        {
            Destroy(shellMaterial);
        }
        if (ringMaterial != null)
        {
            Destroy(ringMaterial);
        }
        if (particleMaterial != null && particleMaterial != ringMaterial)
        {
            Destroy(particleMaterial);
        }
    }

    private void CreateShell()
    {
        GameObject shell = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        shell.name = "Network Shield Energy Shell";
        shell.transform.SetParent(transform, false);
        shell.transform.localPosition = Vector3.zero;
        shell.transform.localScale = new Vector3(82f, 42f, 82f);

        Collider shellCollider = shell.GetComponent<Collider>();
        if (shellCollider != null)
        {
            Destroy(shellCollider);
        }

        Renderer renderer = shell.GetComponent<Renderer>();
        shellMaterial = CreateTransparentMaterial();
        renderer.material = shellMaterial;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;
    }

    private void CreateLight()
    {
        GameObject lightObject = new GameObject("Network Shield Pulse Light");
        lightObject.transform.SetParent(transform, false);
        lightObject.transform.localPosition = Vector3.zero;
        pulseLight = lightObject.AddComponent<Light>();
        pulseLight.type = LightType.Point;
        pulseLight.color = visualTheme.ShieldLight;
        pulseLight.intensity = 1.7f;
        pulseLight.range = 48f;
    }

    private Material CreateTransparentMaterial()
    {
        Shader shader = CombatEffects.FindRuntimeShader("Standard", "Unlit/Color");

        Material material = new Material(shader) { name = "Runtime_Network_Skill_Shield" };
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", visualTheme.ShieldShell);
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

    private GameObject CreateRing(string objectName, float radius, float width, Quaternion localRotation)
    {
        GameObject ringObject = new GameObject(objectName);
        ringObject.transform.SetParent(transform, false);
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
        if (ringMaterial == null)
        {
            ringMaterial = CreateAdditiveMaterial(visualTheme.ShieldRing);
        }
        line.material = ringMaterial;

        for (int i = 0; i < line.positionCount; i++)
        {
            float angle = i / (float)line.positionCount * Mathf.PI * 2f;
            line.SetPosition(i, new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
        }

        return ringObject;
    }

    private ParticleSystem CreateParticles()
    {
        GameObject node = new GameObject("Network Shield Energy Particles");
        node.SetActive(false);
        node.transform.SetParent(transform, false);
        node.transform.localPosition = Vector3.zero;

        ParticleSystem shieldParticles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = shieldParticles.main;
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

        ParticleSystem.EmissionModule emission = shieldParticles.emission;
        emission.rateOverTime = 55f;

        ParticleSystem.ShapeModule shape = shieldParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 32f;
        shape.randomDirectionAmount = 0.45f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = shieldParticles.colorOverLifetime;
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

        ParticleSystemRenderer particleRenderer = shieldParticles.GetComponent<ParticleSystemRenderer>();
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleMaterial = CreateAdditiveMaterial(visualTheme.ShieldParticleStart);
        particleRenderer.material = particleMaterial;
        node.SetActive(true);
        shieldParticles.Play();
        return shieldParticles;
    }

    private Material CreateAdditiveMaterial(Color color)
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader))
            shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");

        Material material = new Material(shader) { name = "Runtime_Network_Skill_Shield_Additive" };
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_TintColor"))
            material.SetColor("_TintColor", color);
        if (material.HasProperty("_SrcBlend"))
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (material.HasProperty("_DstBlend"))
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (material.HasProperty("_ZWrite"))
            material.SetInt("_ZWrite", 0);
        material.renderQueue = 3000;
        return material;
    }
}
