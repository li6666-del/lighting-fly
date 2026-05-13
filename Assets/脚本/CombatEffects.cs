using UnityEngine;

public static class CombatEffects
{
    private static Material additiveParticleMaterial;
    private static Material engineFlameMaterial;
    private static Material beamMetalMaterial;
    private static Material playerAccentMaterial;
    private static Material enemyAccentMaterial;
    private static Texture2D softParticleTexture;

    public static void ApplyPlayerShipVisuals(GameObject ship)
    {
        // Keep the player ship's original prefab materials intact.
    }

    public static void AttachPlayerEngineJet(GameObject ship, Transform firePoint)
    {
        if (ship == null || ship.transform.Find("Runtime Player Engine Jet") != null)
            return;

        Transform root = ship.transform;
        Vector3 localPosition = EstimatePlayerEngineLocalPosition(root, firePoint);
        Vector3 localBackDirection = EstimatePlayerBackDirection(root, firePoint);

        GameObject node = new GameObject("Runtime Player Engine Jet");
        node.transform.SetParent(root, false);
        node.transform.localPosition = localPosition;
        node.transform.localRotation = Quaternion.LookRotation(localBackDirection, Vector3.up);

        Light light = node.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.15f, 0.85f, 1f);
        light.intensity = 3.4f;
        light.range = 42f;

        ParticleSystem core = CreatePlayerEngineJetParticle(node.transform, new Color(0.15f, 0.92f, 1f, 0.95f), 0.28f, 0.9f, 42f, 82f, 150f, 0.34f, 4f);
        ParticleSystem glow = CreatePlayerEngineJetParticle(node.transform, new Color(0.05f, 0.48f, 1f, 0.32f), 0.75f, 1.9f, 18f, 42f, 70f, 0.42f, 6f);
        core.Play();
        glow.Play();
    }

    public static void ApplyEnemyShipVisuals(GameObject ship)
    {
        ApplyShipVisuals(ship, new Color(0.42f, 0.04f, 0.02f), new Color(1f, 0.12f, 0.03f), 1.4f, 38f, false);
    }

    public static void ApplyBossGlow(GameObject boss)
    {
        if (boss == null || boss.transform.Find("Runtime Boss Glow Rig") != null)
            return;

        Transform root = boss.transform;
        Bounds bounds;
        if (!TryGetLocalBounds(root, out bounds))
            return;

        GameObject rig = new GameObject("Runtime Boss Glow Rig");
        rig.transform.SetParent(root, false);

        Color energyBlue = new Color(0.08f, 0.95f, 1f, 1f);
        Color warningRed = new Color(1f, 0.12f, 0.04f, 1f);
        Material blueMaterial = GetAccentMaterial(energyBlue, false);
        Material redMaterial = GetAccentMaterial(warningRed, false);

        float width = Mathf.Max(bounds.size.x, 6f);
        float height = Mathf.Max(bounds.size.y, 6f);
        float depth = Mathf.Max(bounds.size.z, 6f);
        Vector3 center = bounds.center;

        AddAccentCube(rig.transform, blueMaterial, center + new Vector3(0f, height * 0.32f, -depth * 0.08f), new Vector3(width * 0.7f, height * 0.035f, depth * 0.08f));
        AddAccentCube(rig.transform, blueMaterial, center + new Vector3(-width * 0.32f, 0f, 0f), new Vector3(width * 0.035f, height * 0.68f, depth * 0.08f));
        AddAccentCube(rig.transform, blueMaterial, center + new Vector3(width * 0.32f, 0f, 0f), new Vector3(width * 0.035f, height * 0.68f, depth * 0.08f));

        AddAccentBeacon(rig.transform, redMaterial, warningRed, center + new Vector3(0f, height * 0.08f, -depth * 0.46f), Mathf.Max(width * 0.07f, 1.4f));
        AddBossAuraLight(rig.transform, energyBlue, center, Mathf.Max(width, height) * 1.7f);
        AddBossEnergyParticles(rig.transform, center, Mathf.Max(width, height, depth) * 0.45f, energyBlue);
    }

    public static void SpawnExplosion(Vector3 position)
    {
        GameObject effect = CreateEffectRoot("ExplosionEffect", position, Quaternion.identity, 2.5f);
        AddPointLight(effect, new Color(1f, 0.42f, 0.05f), 12f, 110f, 0.22f);

        ParticleSystem flash = AddParticleSystem(effect, new Color(1f, 0.72f, 0.08f, 0.92f), 2.8f, 7.5f, 0.24f, 115, 5f);
        ParticleSystem.MainModule flashMain = flash.main;
        flashMain.startSpeed = new ParticleSystem.MinMaxCurve(45f, 105f);
        flashMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        ParticleSystem core = AddParticleSystem(effect, new Color(0.15f, 0.92f, 1f, 0.9f), 1.8f, 4.6f, 0.28f, 56, 1.4f);
        ParticleSystem.MainModule coreMain = core.main;
        coreMain.startSpeed = new ParticleSystem.MinMaxCurve(10f, 28f);

        ParticleSystem sparks = AddParticleSystem(effect, new Color(1f, 0.18f, 0.02f, 0.95f), 0.9f, 2.4f, 0.75f, 180, 4f);
        ParticleSystem.MainModule sparksMain = sparks.main;
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(72f, 170f);
        sparksMain.gravityModifier = 0.15f;

        ParticleSystem smoke = AddParticleSystem(effect, new Color(0.26f, 0.25f, 0.28f, 0.34f), 4.5f, 12f, 1.25f, 95, 8f);
        ParticleSystem.MainModule smokeMain = smoke.main;
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(9f, 22f);
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    private static Vector3 EstimatePlayerEngineLocalPosition(Transform root, Transform firePoint)
    {
        if (firePoint != null)
        {
            Vector3 localFirePoint = root.InverseTransformPoint(firePoint.position);
            Vector3 localDirection = localFirePoint.sqrMagnitude > 0.001f ? -localFirePoint.normalized : Vector3.back;
            float distance = Mathf.Max(localFirePoint.magnitude * 0.65f, 6f);
            return localDirection * distance;
        }

        Bounds bounds;
        if (TryGetLocalBounds(root, out bounds))
        {
            return new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - bounds.size.z * 0.08f);
        }

        return new Vector3(0f, 0f, -8f);
    }

    private static Vector3 EstimatePlayerBackDirection(Transform root, Transform firePoint)
    {
        if (firePoint != null)
        {
            Vector3 localForward = root.InverseTransformDirection(firePoint.forward);
            if (localForward.sqrMagnitude > 0.001f)
            {
                return -localForward.normalized;
            }
        }

        return Vector3.back;
    }

    private static ParticleSystem CreatePlayerEngineJetParticle(Transform parent, Color color, float minSize, float maxSize, float minSpeed, float maxSpeed, float rate, float lifetime, float angle)
    {
        GameObject node = new GameObject("Jet Particles");
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.7f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.75f, lifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = 0.32f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetAdditiveParticleMaterial();

        node.SetActive(true);
        return particles;
    }

    public static void SpawnHit(Vector3 position, Vector3 normal)
    {
        Quaternion rotation = normal.sqrMagnitude > 0.001f
            ? Quaternion.LookRotation(normal)
            : Quaternion.identity;

        GameObject effect = CreateEffectRoot("HitEffect", position, rotation, 0.9f);
        AddPointLight(effect, new Color(0.25f, 0.9f, 1f), 4f, 55f, 0.14f);

        ParticleSystem sparks = AddParticleSystem(effect, new Color(0.25f, 0.9f, 1f, 1f), 2.2f, 6f, 0.32f, 52, 2f);
        ParticleSystem.MainModule main = sparks.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(52f, 120f);

        ParticleSystem.ShapeModule shape = sparks.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 26f;
        shape.radius = 2.2f;
    }

    public static void SpawnMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        GameObject effect = CreateEffectRoot("MuzzleFlash", position, rotation, 0.38f);
        AddPointLight(effect, new Color(0.25f, 0.85f, 1f), 4f, 50f, 0.1f);

        ParticleSystem flash = AddParticleSystem(effect, new Color(0.2f, 0.9f, 1f, 1f), 3f, 9f, 0.14f, 28, 1.8f);
        ParticleSystem.MainModule main = flash.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(45f, 95f);

        ParticleSystem.ShapeModule shape = flash.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 16f;
        shape.radius = 2.2f;
    }

    public static void AttachBulletTrail(GameObject bullet, Color color, float width, float lifetime)
    {
        if (bullet == null || bullet.GetComponent<TrailRenderer>() != null)
            return;

        TrailRenderer trail = bullet.AddComponent<TrailRenderer>();
        trail.time = lifetime;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.2f;
        trail.numCornerVertices = 2;
        trail.numCapVertices = 2;
        trail.alignment = LineAlignment.View;
        trail.material = GetAdditiveParticleMaterial();

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.35f),
                new GradientColorKey(color * 0.4f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
    }

    public static void SpawnBeamPulse(Vector3 start, Vector3 direction, float length, float radius, float duration)
    {
        direction = direction.sqrMagnitude > 0.001f ? direction.normalized : Vector3.forward;

        GameObject beam = new GameObject("Skill Beam Pulse");
        Object.Destroy(beam, duration);

        LineRenderer core = beam.AddComponent<LineRenderer>();
        core.positionCount = 2;
        core.SetPosition(0, start);
        core.SetPosition(1, start + direction * length);
        core.startWidth = radius * 0.34f;
        core.endWidth = radius * 0.22f;
        core.numCapVertices = 8;
        core.numCornerVertices = 4;
        core.material = GetAdditiveParticleMaterial();
        core.colorGradient = CreateBeamGradient(new Color(0.86f, 0.96f, 1f, 1f), new Color(0.55f, 0.78f, 0.92f, 0f));

        GameObject metalObject = new GameObject("Skill Beam Metallic Core");
        metalObject.transform.SetParent(beam.transform, false);
        LineRenderer metal = metalObject.AddComponent<LineRenderer>();
        metal.positionCount = 2;
        metal.SetPosition(0, start);
        metal.SetPosition(1, start + direction * length);
        metal.startWidth = radius * 0.12f;
        metal.endWidth = radius * 0.08f;
        metal.numCapVertices = 4;
        metal.numCornerVertices = 2;
        metal.material = GetBeamMetalMaterial();
        metal.colorGradient = CreateBeamGradient(new Color(0.92f, 0.96f, 1f, 1f), new Color(0.55f, 0.65f, 0.72f, 0.1f));

        GameObject haloObject = new GameObject("Skill Beam Halo");
        haloObject.transform.SetParent(beam.transform, false);
        LineRenderer halo = haloObject.AddComponent<LineRenderer>();
        halo.positionCount = 2;
        halo.SetPosition(0, start);
        halo.SetPosition(1, start + direction * length);
        halo.startWidth = radius * 0.95f;
        halo.endWidth = radius * 0.5f;
        halo.numCapVertices = 8;
        halo.numCornerVertices = 4;
        halo.material = GetAdditiveParticleMaterial();
        halo.colorGradient = CreateBeamGradient(new Color(0.08f, 0.55f, 1f, 0.28f), new Color(0.08f, 0.55f, 1f, 0f));

        AddPointLight(beam, new Color(0.55f, 0.9f, 1f), 5f, radius * 4f, duration);
    }

    private static Gradient CreateBeamGradient(Color startColor, Color endColor)
    {
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(Color.white, 0.18f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(Mathf.Max(startColor.a, 0.9f), 0.18f),
                new GradientAlphaKey(endColor.a, 1f)
            }
        );
        return gradient;
    }

    private static void ApplyShipVisuals(GameObject ship, Color tintColor, Color glowColor, float lightIntensity, float lightRange, bool isPlayer)
    {
        if (ship == null)
            return;

        Renderer[] renderers = ship.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                continue;

            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                TuneExistingShipMaterial(material, tintColor, glowColor);
            }
        }

        AddPersistentGlowLight(ship.transform, glowColor, lightIntensity, lightRange);
        AddEngineFlame(ship.transform, glowColor, isPlayer);
        AddTechAccents(ship.transform, glowColor, isPlayer);
    }

    private static void TuneExistingShipMaterial(Material material, Color tintColor, Color glowColor)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Color"))
        {
            material.color = Color.Lerp(material.color, tintColor, 0.18f);
        }
        if (material.HasProperty("_BaseColor"))
        {
            Color baseColor = material.GetColor("_BaseColor");
            material.SetColor("_BaseColor", Color.Lerp(baseColor, tintColor, 0.18f));
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", glowColor * 0.18f);
        }
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", Mathf.Max(material.GetFloat("_Metallic"), 0.78f));
        }
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", Mathf.Max(material.GetFloat("_Glossiness"), 0.82f));
        }
        if (material.HasProperty("_Smoothness"))
        {
            material.SetFloat("_Smoothness", Mathf.Max(material.GetFloat("_Smoothness"), 0.82f));
        }
    }

    private static void AddPersistentGlowLight(Transform root, Color color, float intensity, float range)
    {
        Transform existing = root.Find("Runtime Ship Glow");
        Light light;

        if (existing == null)
        {
            GameObject node = new GameObject("Runtime Ship Glow");
            node.transform.SetParent(root, false);
            node.transform.localPosition = new Vector3(0f, 3f, -4f);
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

    private static void AddBossAuraLight(Transform parent, Color color, Vector3 localPosition, float range)
    {
        GameObject node = new GameObject("Boss Aura Light");
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;

        Light light = node.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 3.8f;
        light.range = range;
    }

    private static void AddBossEnergyParticles(Transform parent, Vector3 localPosition, float radius, Color color)
    {
        GameObject node = new GameObject("Boss Energy Particles");
        node.SetActive(false);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1.2f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 8f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.45f, 1.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 45f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.randomDirectionAmount = 0.18f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(color, 0.35f),
                new GradientColorKey(color * 0.45f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.72f, 0.18f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetAdditiveParticleMaterial();

        node.SetActive(true);
        particles.Play();
    }

    private static void AddEngineFlame(Transform root, Color color, bool isPlayer)
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
        light.intensity = isPlayer ? 1.4f : 1.2f;
        light.range = isPlayer ? 26f : 22f;

        ParticleSystem flame = CreateEngineParticle(node.transform, color, isPlayer);
        ParticleSystem smoke = CreateEngineSmoke(node.transform, isPlayer);
        flame.Play();
        smoke.Play();
    }

    private static Vector3 EstimateEngineLocalPosition(Transform root, bool isPlayer)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Vector3(0f, 0f, isPlayer ? 10f : 8f);

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 localCenter = root.InverseTransformPoint(bounds.center);
        Vector3 localMin = root.InverseTransformPoint(bounds.min);
        Vector3 localMax = root.InverseTransformPoint(bounds.max);
        float localDepth = Mathf.Abs(localMax.z - localMin.z);
        float offset = Mathf.Max(localDepth * 0.35f, isPlayer ? 8f : 6f);

        return new Vector3(localCenter.x, localCenter.y, localCenter.z + offset);
    }

    private static ParticleSystem CreateEngineParticle(Transform parent, Color color, bool isPlayer)
    {
        GameObject node = new GameObject("Flame Core");
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.6f;
        main.startLifetime = isPlayer ? 0.16f : 0.13f;
        main.startSpeed = isPlayer ? new ParticleSystem.MinMaxCurve(12f, 26f) : new ParticleSystem.MinMaxCurve(10f, 22f);
        main.startSize = isPlayer ? new ParticleSystem.MinMaxCurve(0.55f, 1.7f) : new ParticleSystem.MinMaxCurve(0.45f, 1.3f);
        main.startColor = new ParticleSystem.MinMaxGradient(Color.white, color);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = isPlayer ? 28f : 22f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = isPlayer ? 0.45f : 0.35f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetEngineFlameMaterial();

        node.SetActive(true);
        return particles;
    }

    private static ParticleSystem CreateEngineSmoke(Transform parent, bool isPlayer)
    {
        GameObject node = new GameObject("Heat Haze");
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.8f;
        main.startLifetime = 0.28f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
        main.startSize = isPlayer ? new ParticleSystem.MinMaxCurve(0.8f, 2.2f) : new ParticleSystem.MinMaxCurve(0.6f, 1.8f);
        main.startColor = new Color(0.18f, 0.22f, 0.26f, 0.14f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = isPlayer ? 5f : 4f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 18f;
        shape.radius = isPlayer ? 0.55f : 0.45f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetEngineFlameMaterial();

        node.SetActive(true);
        return particles;
    }

    private static void AddTechAccents(Transform root, Color glowColor, bool isPlayer)
    {
        if (root.Find("Runtime Tech Accents") != null)
            return;

        Bounds bounds;
        if (!TryGetLocalBounds(root, out bounds))
            return;

        GameObject group = new GameObject("Runtime Tech Accents");
        group.transform.SetParent(root, false);

        float width = Mathf.Max(bounds.size.x, 8f);
        float depth = Mathf.Max(bounds.size.z, 10f);
        float height = Mathf.Max(bounds.size.y, 4f);
        float y = bounds.center.y + height * 0.38f;

        Material material = GetAccentMaterial(glowColor, isPlayer);
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x, y, bounds.center.z + depth * 0.08f), new Vector3(width * 0.08f, height * 0.035f, depth * 0.55f));
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x - width * 0.27f, y, bounds.center.z - depth * 0.03f), new Vector3(width * 0.18f, height * 0.03f, depth * 0.08f));
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x + width * 0.27f, y, bounds.center.z - depth * 0.03f), new Vector3(width * 0.18f, height * 0.03f, depth * 0.08f));

        AddAccentBeacon(group.transform, material, glowColor, new Vector3(bounds.center.x - width * 0.42f, y, bounds.center.z + depth * 0.12f), Mathf.Max(width * 0.035f, 0.8f));
        AddAccentBeacon(group.transform, material, glowColor, new Vector3(bounds.center.x + width * 0.42f, y, bounds.center.z + depth * 0.12f), Mathf.Max(width * 0.035f, 0.8f));
    }

    private static bool TryGetLocalBounds(Transform root, out Bounds localBounds)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        bool hasBounds = false;
        localBounds = new Bounds(Vector3.zero, Vector3.zero);

        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                continue;

            Bounds worldBounds = renderer.bounds;
            Vector3 localCenter = root.InverseTransformPoint(worldBounds.center);
            Vector3 localSize = root.InverseTransformVector(worldBounds.size);
            localSize = new Vector3(Mathf.Abs(localSize.x), Mathf.Abs(localSize.y), Mathf.Abs(localSize.z));

            if (!hasBounds)
            {
                localBounds = new Bounds(localCenter, localSize);
                hasBounds = true;
            }
            else
            {
                localBounds.Encapsulate(new Bounds(localCenter, localSize));
            }
        }

        return hasBounds;
    }

    private static void AddAccentCube(Transform parent, Material material, Vector3 localPosition, Vector3 localScale)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Glow Strip";
        Object.Destroy(cube.GetComponent<Collider>());
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = localPosition;
        cube.transform.localScale = localScale;

        Renderer renderer = cube.GetComponent<Renderer>();
        renderer.material = material;
    }

    private static void AddAccentBeacon(Transform parent, Material material, Color color, Vector3 localPosition, float size)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Wing Beacon";
        Object.Destroy(sphere.GetComponent<Collider>());
        sphere.transform.SetParent(parent, false);
        sphere.transform.localPosition = localPosition;
        sphere.transform.localScale = Vector3.one * size;

        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material = material;

        Light light = sphere.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = 0.9f;
        light.range = size * 8f;
    }

    private static GameObject CreateEffectRoot(string name, Vector3 position, Quaternion rotation, float destroyDelay)
    {
        GameObject effect = new GameObject(name);
        effect.transform.SetPositionAndRotation(position, rotation);
        Object.Destroy(effect, destroyDelay);
        return effect;
    }

    private static ParticleSystem AddParticleSystem(GameObject parent, Color color, float minSize, float maxSize, float lifetime, int burstCount, float radius)
    {
        GameObject node = new GameObject("Particles");
        node.SetActive(false);
        node.transform.SetParent(parent.transform, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = false;
        main.duration = lifetime;
        main.startLifetime = lifetime;
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = color;
        main.playOnAwake = true;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetAdditiveParticleMaterial();

        node.SetActive(true);
        particles.Play();
        return particles;
    }

    private static Material GetAdditiveParticleMaterial()
    {
        if (additiveParticleMaterial != null)
            return additiveParticleMaterial;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        additiveParticleMaterial = new Material(shader)
        {
            name = "Runtime_Additive_CombatParticles"
        };
        additiveParticleMaterial.SetColor("_Color", Color.white);
        additiveParticleMaterial.SetColor("_TintColor", Color.white);
        if (additiveParticleMaterial.HasProperty("_MainTex"))
        {
            additiveParticleMaterial.SetTexture("_MainTex", GetSoftParticleTexture());
        }
        return additiveParticleMaterial;
    }

    private static Texture2D GetSoftParticleTexture()
    {
        if (softParticleTexture != null)
            return softParticleTexture;

        const int size = 64;
        softParticleTexture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime_Soft_Round_Particle",
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
                alpha = alpha * alpha * (3f - 2f * alpha);
                softParticleTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        softParticleTexture.Apply(false, true);
        return softParticleTexture;
    }

    private static Material GetBeamMetalMaterial()
    {
        if (beamMetalMaterial != null)
            return beamMetalMaterial;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        beamMetalMaterial = new Material(shader)
        {
            name = "Runtime_Beam_Metal_Core"
        };

        if (beamMetalMaterial.HasProperty("_Color"))
        {
            beamMetalMaterial.SetColor("_Color", new Color(0.86f, 0.92f, 1f, 1f));
        }

        return beamMetalMaterial;
    }

    private static Material GetEngineFlameMaterial()
    {
        if (engineFlameMaterial != null)
            return engineFlameMaterial;

        Shader shader = Shader.Find("Particles/Standard Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Legacy Shaders/Particles/Additive");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        engineFlameMaterial = new Material(shader)
        {
            name = "Runtime_Engine_Flame"
        };
        engineFlameMaterial.SetColor("_Color", Color.white);
        engineFlameMaterial.SetColor("_TintColor", Color.white);
        return engineFlameMaterial;
    }

    private static Material GetAccentMaterial(Color color, bool isPlayer)
    {
        Material cached = isPlayer ? playerAccentMaterial : enemyAccentMaterial;
        if (cached != null)
            return cached;

        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Unlit/Color");
        }

        Material material = new Material(shader)
        {
            name = isPlayer ? "Runtime_Player_Tech_Accents" : "Runtime_Enemy_Tech_Accents"
        };

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color * 0.75f);
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * 1.8f);
        }
        if (material.HasProperty("_Metallic"))
        {
            material.SetFloat("_Metallic", 0.25f);
        }
        if (material.HasProperty("_Glossiness"))
        {
            material.SetFloat("_Glossiness", 0.9f);
        }

        if (isPlayer)
            playerAccentMaterial = material;
        else
            enemyAccentMaterial = material;

        return material;
    }

    private static void AddPointLight(GameObject parent, Color color, float intensity, float range, float lifetime)
    {
        Light light = parent.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        Object.Destroy(light, lifetime);
    }
}
