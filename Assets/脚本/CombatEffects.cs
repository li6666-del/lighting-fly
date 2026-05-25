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
        if (ship == null || ship.transform.Find("Runtime Player Hero FX") != null)
            return;

        Color tintColor = new Color(0.04f, 0.18f, 0.22f);
        Color glowColor = new Color(0.08f, 0.92f, 1f);
        Renderer[] renderers = ship.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer is ParticleSystemRenderer || renderer is TrailRenderer)
                continue;

            Material[] materials = renderer.materials;
            foreach (Material material in materials)
            {
                TuneExistingShipMaterial(material, tintColor, glowColor, 0.09f, 0.18f);
            }
        }

        AddPersistentGlowLight(ship.transform, glowColor, 1.05f, 26f);
        AddPlayerHeroFx(ship.transform);
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
        light.color = new Color(0.18f, 0.82f, 1f);
        light.intensity = 4.6f;
        light.range = 48f;

        PlayerEnginePulse pulse = node.AddComponent<PlayerEnginePulse>();
        pulse.Initialize(light, 3.4f, 5.8f, 10.5f, 38f, 56f);

        CreateRocketEnginePlume(
            node.transform,
            "Rocket White Core",
            Color.white * 2.8f,
            new Color(0.62f, 1.45f, 2.6f, 1f),
            new Color(0.05f, 0.58f, 1.7f, 0f),
            0.18f,
            0.55f,
            72f,
            132f,
            420f,
            0.28f,
            0.52f,
            2.8f,
            0.26f,
            420,
            false);

        CreateRocketEnginePlume(
            node.transform,
            "Rocket Blue Outer Flame",
            new Color(0.18f, 1.0f, 2.4f, 0.86f),
            new Color(0.0f, 0.72f, 2.8f, 0.42f),
            new Color(0.0f, 0.18f, 0.9f, 0f),
            0.48f,
            1.35f,
            38f,
            92f,
            280f,
            0.52f,
            0.92f,
            5.2f,
            0.95f,
            360,
            false);

        CreateRocketEnginePlume(
            node.transform,
            "Rocket Orange Heat",
            new Color(2.4f, 0.88f, 0.16f, 0.58f),
            new Color(1.35f, 0.24f, 0.04f, 0.26f),
            new Color(0.45f, 0.06f, 0.02f, 0f),
            0.58f,
            1.7f,
            24f,
            62f,
            120f,
            0.45f,
            0.78f,
            9f,
            1.25f,
            180,
            false);

        CreateRocketEngineSmoke(node.transform);
    }

    private static void AddPlayerHeroFx(Transform root)
    {
        Bounds bounds;
        if (!TryGetLocalBounds(root, out bounds))
            return;

        GameObject group = new GameObject("Runtime Player Hero FX");
        group.transform.SetParent(root, false);

        Color energy = new Color(0.05f, 0.95f, 1f, 1f);
        Material material = GetAccentMaterial(energy, true);

        float width = Mathf.Max(bounds.size.x, 10f);
        float depth = Mathf.Max(bounds.size.z, 12f);
        float height = Mathf.Max(bounds.size.y, 4f);
        float y = bounds.center.y + height * 0.48f;

        AddAccentCube(group.transform, material, new Vector3(bounds.center.x, y, bounds.center.z + depth * 0.08f), new Vector3(width * 0.06f, height * 0.035f, depth * 0.72f));
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x - width * 0.28f, y, bounds.center.z - depth * 0.02f), new Vector3(width * 0.26f, height * 0.035f, depth * 0.075f));
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x + width * 0.28f, y, bounds.center.z - depth * 0.02f), new Vector3(width * 0.26f, height * 0.035f, depth * 0.075f));
        AddAccentBeacon(group.transform, material, energy, new Vector3(bounds.center.x, y, bounds.center.z + depth * 0.43f), Mathf.Max(width * 0.045f, 0.9f));

        AddPlayerWingTrail(group.transform, new Vector3(bounds.center.x - width * 0.44f, y, bounds.center.z - depth * 0.08f), energy, Mathf.Max(width * 0.035f, 0.8f));
        AddPlayerWingTrail(group.transform, new Vector3(bounds.center.x + width * 0.44f, y, bounds.center.z - depth * 0.08f), energy, Mathf.Max(width * 0.035f, 0.8f));
        AddPlayerEnergyParticles(group.transform, bounds.center, Mathf.Max(width, depth) * 0.34f, energy);

        Light light = group.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = energy;
        light.intensity = 0.95f;
        light.range = Mathf.Max(width, depth) * 1.25f;

        PlayerHeroPulse pulse = group.AddComponent<PlayerHeroPulse>();
        pulse.Initialize(light, 0.78f, 1.25f, 4.8f);
    }

    private static void AddPlayerWingTrail(Transform parent, Vector3 localPosition, Color color, float width)
    {
        GameObject node = new GameObject("Player Wing Light Trail");
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;

        TrailRenderer trail = node.AddComponent<TrailRenderer>();
        trail.time = 0.26f;
        trail.startWidth = width;
        trail.endWidth = 0f;
        trail.minVertexDistance = 0.12f;
        trail.numCornerVertices = 3;
        trail.numCapVertices = 3;
        trail.alignment = LineAlignment.View;
        trail.material = GetAdditiveParticleMaterial();

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(Color.Lerp(Color.white, color, 0.35f), 0f),
                new GradientColorKey(color, 0.42f),
                new GradientColorKey(color * 0.25f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.62f, 0f),
                new GradientAlphaKey(0.34f, 0.42f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        trail.colorGradient = gradient;
    }

    private static void AddPlayerEnergyParticles(Transform parent, Vector3 localPosition, float radius, Color color)
    {
        GameObject node = new GameObject("Player Energy Field");
        node.SetActive(false);
        node.transform.SetParent(parent, false);
        node.transform.localPosition = localPosition;

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1.1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.75f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.18f, 0.58f);
        main.startColor = new ParticleSystem.MinMaxGradient(color * 0.55f, Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 80;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 34f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = radius;
        shape.randomDirectionAmount = 0.35f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(color * 0.6f, 0f),
                new GradientColorKey(Color.white, 0.22f),
                new GradientColorKey(color * 0.25f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.42f, 0.22f),
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

    public static void ApplyEnemyShipVisuals(GameObject ship)
    {
        ApplyShipVisuals(ship, new Color(0.5f, 0.025f, 0.01f), new Color(1f, 0.18f, 0.04f), 0.65f, 18f, false);
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
        AddPointLight(effect, new Color(1f, 0.35f, 0.04f), 4.5f, 45f, 0.16f);

        ParticleSystem flash = AddParticleSystem(effect, new Color(1f, 0.52f, 0.08f, 0.62f), 1.4f, 3.8f, 0.18f, 58, 2.8f);
        ParticleSystem.MainModule flashMain = flash.main;
        flashMain.startSpeed = new ParticleSystem.MinMaxCurve(32f, 72f);
        flashMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        ParticleSystem core = AddParticleSystem(effect, new Color(0.15f, 0.75f, 1f, 0.45f), 0.9f, 2.4f, 0.22f, 32, 0.9f);
        ParticleSystem.MainModule coreMain = core.main;
        coreMain.startSpeed = new ParticleSystem.MinMaxCurve(8f, 20f);

        ParticleSystem sparks = AddParticleSystem(effect, new Color(1f, 0.18f, 0.02f, 0.7f), 0.45f, 1.4f, 0.62f, 110, 2.6f);
        ParticleSystem.MainModule sparksMain = sparks.main;
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(52f, 125f);
        sparksMain.gravityModifier = 0.15f;

        ParticleSystem smoke = AddParticleSystem(effect, new Color(0.22f, 0.21f, 0.24f, 0.26f), 2.8f, 7.5f, 1.1f, 60, 5.5f);
        ParticleSystem.MainModule smokeMain = smoke.main;
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(7f, 16f);
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;
    }

    public static void SpawnBossExplosion(Vector3 position)
    {
        GameObject effect = CreateEffectRoot("BossExplosionEffect", position, Quaternion.identity, 3.4f);
        AddPointLight(effect, new Color(1f, 0.28f, 0.04f), 8f, 82f, 0.28f);

        ParticleSystem flash = AddParticleSystem(effect, new Color(1f, 0.36f, 0.05f, 0.78f), 3.2f, 8.5f, 0.28f, 120, 6f);
        ParticleSystem.MainModule flashMain = flash.main;
        flashMain.startSpeed = new ParticleSystem.MinMaxCurve(42f, 95f);
        flashMain.startRotation = new ParticleSystem.MinMaxCurve(0f, 6.28f);

        ParticleSystem core = AddParticleSystem(effect, new Color(1f, 0.85f, 0.32f, 0.55f), 1.8f, 4.6f, 0.22f, 65, 3.2f);
        ParticleSystem.MainModule coreMain = core.main;
        coreMain.startSpeed = new ParticleSystem.MinMaxCurve(14f, 34f);

        ParticleSystem sparks = AddParticleSystem(effect, new Color(1f, 0.12f, 0.02f, 0.8f), 0.65f, 2.2f, 0.9f, 230, 7f);
        ParticleSystem.MainModule sparksMain = sparks.main;
        sparksMain.startSpeed = new ParticleSystem.MinMaxCurve(85f, 190f);
        sparksMain.gravityModifier = 0.08f;

        ParticleSystem smoke = AddParticleSystem(effect, new Color(0.16f, 0.13f, 0.12f, 0.42f), 6f, 16f, 1.9f, 130, 11f);
        ParticleSystem.MainModule smokeMain = smoke.main;
        smokeMain.startSpeed = new ParticleSystem.MinMaxCurve(12f, 30f);
        smokeMain.simulationSpace = ParticleSystemSimulationSpace.World;

        SpawnShockwave(position, new Color(1f, 0.18f, 0.04f, 0.34f), 42f, 0.52f);
        ShakeMainCamera(0.42f, 0.32f);
    }

    private static Vector3 EstimatePlayerEngineLocalPosition(Transform root, Transform firePoint)
    {
        if (firePoint != null)
        {
            Vector3 localFirePoint = root.InverseTransformPoint(firePoint.position);
            Vector3 localDirection = localFirePoint.sqrMagnitude > 0.001f ? -localFirePoint.normalized : Vector3.back;
            float distance = Mathf.Max(localFirePoint.magnitude * 0.96f, 8.5f);
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

    private static ParticleSystem CreateRocketEnginePlume(
        Transform parent,
        string name,
        Color startColor,
        Color midColor,
        Color endColor,
        float minSize,
        float maxSize,
        float minSpeed,
        float maxSpeed,
        float rate,
        float minLifetime,
        float maxLifetime,
        float angle,
        float radius,
        int maxParticles,
        bool stretch)
    {
        GameObject node = new GameObject(name);
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 0.55f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(minLifetime, maxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(minSpeed, maxSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(minSize, maxSize);
        main.startColor = Color.white;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = rate;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = radius;
        shape.randomDirectionAmount = 0.035f;

        ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.Local;
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 16f);
        velocity.x = new ParticleSystem.MinMaxCurve(-0.55f, 0.55f);
        velocity.y = new ParticleSystem.MinMaxCurve(-0.25f, 0.25f);

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.38f),
                new Keyframe(0.18f, 0.95f),
                new Keyframe(0.64f, 0.72f),
                new Keyframe(1f, 0.06f)));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(startColor, 0f),
                new GradientColorKey(midColor, 0.28f),
                new GradientColorKey(endColor, 1f)
            },
            new[]
            {
                new GradientAlphaKey(startColor.a, 0f),
                new GradientAlphaKey(midColor.a, 0.42f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = stretch ? 0.18f : 0.42f;
        noise.frequency = 1.15f;
        noise.scrollSpeed = 1.25f;
        noise.damping = true;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = 1f;
        renderer.velocityScale = 0f;
        renderer.material = GetAdditiveParticleMaterial();
        renderer.sortingOrder = 5;

        node.SetActive(true);
        particles.Play();
        return particles;
    }

    private static ParticleSystem CreateRocketEngineSmoke(Transform parent)
    {
        GameObject node = new GameObject("Rocket Exhaust Smoke");
        node.SetActive(false);
        node.transform.SetParent(parent, false);

        ParticleSystem particles = node.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.duration = 1.4f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.55f, 1.05f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(10f, 24f);
        main.startSize = new ParticleSystem.MinMaxCurve(1.1f, 2.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.18f, 0.2f, 0.22f, 0.18f),
            new Color(0.42f, 0.34f, 0.28f, 0.11f));
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 90;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = 16f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 1.1f;
        shape.randomDirectionAmount = 0.12f;

        ParticleSystem.SizeOverLifetimeModule sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(
            1f,
            new AnimationCurve(
                new Keyframe(0f, 0.22f),
                new Keyframe(0.45f, 0.9f),
                new Keyframe(1f, 1.25f)));

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = particles.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new[]
            {
                new GradientColorKey(new Color(0.6f, 0.58f, 0.55f), 0f),
                new GradientColorKey(new Color(0.2f, 0.22f, 0.25f), 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.05f, 0f),
                new GradientAlphaKey(0.11f, 0.24f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = 1.4f;
        noise.frequency = 0.62f;
        noise.scrollSpeed = 0.55f;

        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = GetAdditiveParticleMaterial();
        renderer.sortingOrder = 3;

        node.SetActive(true);
        particles.Play();
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

    public static void SpawnEnemyMuzzleFlash(Vector3 position, Quaternion rotation)
    {
        GameObject effect = CreateEffectRoot("EnemyMuzzleFlash", position, rotation, 0.24f);
        AddPointLight(effect, new Color(0.95f, 0.12f, 0.04f), 0.9f, 16f, 0.06f);

        ParticleSystem flash = AddParticleSystem(effect, new Color(0.95f, 0.16f, 0.04f, 0.55f), 0.45f, 1.35f, 0.1f, 12, 0.8f);
        ParticleSystem.MainModule main = flash.main;
        main.startSpeed = new ParticleSystem.MinMaxCurve(24f, 52f);

        ParticleSystem.ShapeModule shape = flash.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = 0.75f;
    }

    public static void AttachBulletTrail(GameObject bullet, Color color, float width, float lifetime, float headColorBlend = 0.55f, float alphaMultiplier = 1f)
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
                new GradientColorKey(Color.Lerp(Color.white, color, Mathf.Clamp01(headColorBlend)), 0f),
                new GradientColorKey(color, 0.35f),
                new GradientColorKey(color * 0.25f, 1f)
            },
            new[]
            {
                new GradientAlphaKey(0.72f * alphaMultiplier, 0f),
                new GradientAlphaKey(0.45f * alphaMultiplier, 0.35f),
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
                float emissionStrength = isPlayer ? 0.09f : 0.24f;
                float tintBlend = isPlayer ? 0.18f : 0.24f;
                TuneExistingShipMaterial(material, tintColor, glowColor, emissionStrength, tintBlend);
            }
        }

        AddPersistentGlowLight(ship.transform, glowColor, lightIntensity, lightRange);
        AddEngineFlame(ship.transform, glowColor, isPlayer);
        AddTechAccents(ship.transform, glowColor, isPlayer);
    }

    private static void TuneExistingShipMaterial(Material material, Color tintColor, Color glowColor, float emissionStrength, float tintBlend)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Color"))
        {
            material.color = Color.Lerp(material.color, tintColor, tintBlend);
        }
        if (material.HasProperty("_BaseColor"))
        {
            Color baseColor = material.GetColor("_BaseColor");
            material.SetColor("_BaseColor", Color.Lerp(baseColor, tintColor, tintBlend));
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", glowColor * emissionStrength);
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
        light.intensity = isPlayer ? 1.4f : 0.55f;
        light.range = isPlayer ? 26f : 13f;

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
        main.startLifetime = isPlayer ? 0.16f : 0.19f;
        main.startSpeed = isPlayer ? new ParticleSystem.MinMaxCurve(12f, 26f) : new ParticleSystem.MinMaxCurve(12f, 28f);
        main.startSize = isPlayer ? new ParticleSystem.MinMaxCurve(0.55f, 1.7f) : new ParticleSystem.MinMaxCurve(0.38f, 1.05f);
        main.startColor = isPlayer
            ? new ParticleSystem.MinMaxGradient(Color.white, color)
            : new ParticleSystem.MinMaxGradient(Color.Lerp(color, Color.white, 0.18f), color * 0.32f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = isPlayer ? 28f : 16f;

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 11f;
        shape.radius = isPlayer ? 0.45f : 0.42f;

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
        main.startLifetime = isPlayer ? 0.28f : 0.34f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
        main.startSize = isPlayer ? new ParticleSystem.MinMaxCurve(0.8f, 2.2f) : new ParticleSystem.MinMaxCurve(0.7f, 2.05f);
        main.startColor = isPlayer ? new Color(0.18f, 0.22f, 0.26f, 0.14f) : new Color(0.24f, 0.08f, 0.04f, 0.1f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = isPlayer ? 5f : 5f;

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
        if (isPlayer)
        {
            AddAccentCube(group.transform, material, new Vector3(bounds.center.x, y, bounds.center.z + depth * 0.08f), new Vector3(width * 0.08f, height * 0.035f, depth * 0.55f));
            AddAccentCube(group.transform, material, new Vector3(bounds.center.x - width * 0.27f, y, bounds.center.z - depth * 0.03f), new Vector3(width * 0.18f, height * 0.03f, depth * 0.08f));
            AddAccentCube(group.transform, material, new Vector3(bounds.center.x + width * 0.27f, y, bounds.center.z - depth * 0.03f), new Vector3(width * 0.18f, height * 0.03f, depth * 0.08f));

            AddAccentBeacon(group.transform, material, glowColor, new Vector3(bounds.center.x - width * 0.42f, y, bounds.center.z + depth * 0.12f), Mathf.Max(width * 0.035f, 0.8f));
            AddAccentBeacon(group.transform, material, glowColor, new Vector3(bounds.center.x + width * 0.42f, y, bounds.center.z + depth * 0.12f), Mathf.Max(width * 0.035f, 0.8f));
            return;
        }

        AddAccentCube(group.transform, material, new Vector3(bounds.center.x, y, bounds.center.z + depth * 0.12f), new Vector3(width * 0.055f, height * 0.035f, depth * 0.48f));
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x - width * 0.26f, y, bounds.center.z - depth * 0.02f), new Vector3(width * 0.14f, height * 0.03f, depth * 0.07f));
        AddAccentCube(group.transform, material, new Vector3(bounds.center.x + width * 0.26f, y, bounds.center.z - depth * 0.02f), new Vector3(width * 0.14f, height * 0.03f, depth * 0.07f));

        AddAccentBeacon(group.transform, material, glowColor, new Vector3(bounds.center.x - width * 0.38f, y, bounds.center.z + depth * 0.18f), Mathf.Max(width * 0.026f, 0.42f));
        AddAccentBeacon(group.transform, material, glowColor, new Vector3(bounds.center.x + width * 0.38f, y, bounds.center.z + depth * 0.18f), Mathf.Max(width * 0.026f, 0.42f));
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
        bool isEnemyAccent = material == enemyAccentMaterial;
        light.intensity = isEnemyAccent ? 0.32f : 0.9f;
        light.range = size * (isEnemyAccent ? 4.2f : 8f);
    }

    private static GameObject CreateEffectRoot(string name, Vector3 position, Quaternion rotation, float destroyDelay)
    {
        GameObject effect = new GameObject(name);
        effect.transform.SetPositionAndRotation(position, rotation);
        Object.Destroy(effect, destroyDelay);
        return effect;
    }

    private static void SpawnShockwave(Vector3 position, Color color, float maxScale, float duration)
    {
        GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        sphere.name = "Boss Explosion Shockwave";
        sphere.transform.position = position;
        sphere.transform.localScale = Vector3.one * 0.1f;
        Object.Destroy(sphere.GetComponent<Collider>());

        Material material = CreateTransparentMaterial(color);
        Renderer renderer = sphere.GetComponent<Renderer>();
        renderer.material = material;
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        renderer.receiveShadows = false;

        BossShockwaveAnimator animator = sphere.AddComponent<BossShockwaveAnimator>();
        animator.Initialize(material, color, maxScale, duration);
    }

    private static Material CreateTransparentMaterial(Color color)
    {
        Shader shader = Shader.Find("Standard");
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        Material material = new Material(shader)
        {
            name = "Runtime_BossExplosion_Shockwave"
        };

        if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
        if (material.HasProperty("_Mode"))
        {
            material.SetFloat("_Mode", 3f);
        }
        if (material.HasProperty("_SrcBlend"))
        {
            material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        }
        if (material.HasProperty("_DstBlend"))
        {
            material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        }
        if (material.HasProperty("_ZWrite"))
        {
            material.SetInt("_ZWrite", 0);
        }
        material.DisableKeyword("_ALPHATEST_ON");
        material.EnableKeyword("_ALPHABLEND_ON");
        material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        material.renderQueue = 3000;
        return material;
    }

    private static void ShakeMainCamera(float intensity, float duration)
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        BossCameraShake shake = camera.GetComponent<BossCameraShake>();
        if (shake == null)
        {
            shake = camera.gameObject.AddComponent<BossCameraShake>();
        }
        shake.Play(intensity, duration);
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
            material.SetColor("_Color", color * (isPlayer ? 0.75f : 0.55f));
        }
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", color * (isPlayer ? 1.8f : 0.9f));
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

public class BossShockwaveAnimator : MonoBehaviour
{
    private Material material;
    private Color color;
    private float maxScale;
    private float duration;
    private float elapsed;

    public void Initialize(Material material, Color color, float maxScale, float duration)
    {
        this.material = material;
        this.color = color;
        this.maxScale = Mathf.Max(1f, maxScale);
        this.duration = Mathf.Max(0.05f, duration);
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float eased = 1f - (1f - progress) * (1f - progress);
        transform.localScale = Vector3.one * Mathf.Lerp(0.1f, maxScale, eased);

        if (material != null && material.HasProperty("_Color"))
        {
            Color faded = color;
            faded.a = color.a * (1f - progress);
            material.SetColor("_Color", faded);
        }

        if (progress >= 1f)
        {
            if (material != null)
            {
                Destroy(material);
            }
            Destroy(gameObject);
        }
    }
}

public class BossCameraShake : MonoBehaviour
{
    private Vector3 originalLocalPosition;
    private float intensity;
    private float duration;
    private float elapsed;
    private bool shaking;

    public void Play(float intensity, float duration)
    {
        if (!shaking)
        {
            originalLocalPosition = transform.localPosition;
        }

        this.intensity = Mathf.Max(this.intensity, intensity);
        this.duration = Mathf.Max(0.05f, duration);
        elapsed = 0f;
        shaking = true;
    }

    void LateUpdate()
    {
        if (!shaking)
            return;

        elapsed += Time.deltaTime;
        float progress = Mathf.Clamp01(elapsed / duration);
        float decay = 1f - progress;
        Vector3 offset = Random.insideUnitSphere * intensity * decay;
        offset.z = 0f;
        transform.localPosition = originalLocalPosition + offset;

        if (progress >= 1f)
        {
            transform.localPosition = originalLocalPosition;
            shaking = false;
            intensity = 0f;
        }
    }
}

public class PlayerHeroPulse : MonoBehaviour
{
    private Light pulseLight;
    private float minIntensity;
    private float maxIntensity;
    private float speed;
    private float phase;

    public void Initialize(Light light, float minIntensity, float maxIntensity, float speed)
    {
        pulseLight = light;
        this.minIntensity = Mathf.Max(0f, minIntensity);
        this.maxIntensity = Mathf.Max(this.minIntensity, maxIntensity);
        this.speed = Mathf.Max(0.1f, speed);
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (pulseLight != null)
        {
            float t = (Mathf.Sin(Time.time * speed + phase) + 1f) * 0.5f;
            pulseLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, t);
        }

    }
}

public class PlayerEnginePulse : MonoBehaviour
{
    private Light engineLight;
    private float minIntensity;
    private float maxIntensity;
    private float speed;
    private float minRange;
    private float maxRange;
    private float phase;

    public void Initialize(Light light, float minIntensity, float maxIntensity, float speed, float minRange, float maxRange)
    {
        engineLight = light;
        this.minIntensity = Mathf.Max(0f, minIntensity);
        this.maxIntensity = Mathf.Max(this.minIntensity, maxIntensity);
        this.speed = Mathf.Max(0.1f, speed);
        this.minRange = Mathf.Max(0f, minRange);
        this.maxRange = Mathf.Max(this.minRange, maxRange);
        phase = Random.value * Mathf.PI * 2f;
    }

    void Update()
    {
        if (engineLight == null)
            return;

        float basePulse = (Mathf.Sin(Time.time * speed + phase) + 1f) * 0.5f;
        float microPulse = (Mathf.Sin(Time.time * speed * 2.7f + phase * 0.37f) + 1f) * 0.5f;
        float pulse = Mathf.Clamp01(basePulse * 0.72f + microPulse * 0.28f);
        engineLight.intensity = Mathf.Lerp(minIntensity, maxIntensity, pulse);
        engineLight.range = Mathf.Lerp(minRange, maxRange, pulse);
    }
}
