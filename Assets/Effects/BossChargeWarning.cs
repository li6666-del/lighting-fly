// ============================================================
// BossChargeWarning.cs  -  Boss 大招充能 + 警告特效
// ------------------------------------------------------------
// 功能:Boss 释放大招前的"预警 + 充能"特效,让玩家有反应时间。
//      包含:
//        1. 地面警告圈(LineRenderer 画的圆环,从小扩到大);
//        2. Boss 身前充能球(发光球体,从小变大,带脉动);
//        3. 收缩粒子云(从外向中心汇聚的粒子,营造"吸能"感);
//        4. 脉动光源(随充能进度增强 + 高频跳动);
//        5. 临近完成时圆环闪烁(0.3 秒频率)警告即将释放;
//        6. 充能完成 → 一次性大闪光 + 触发回调,你可以在回调里
//           真正生成大招激光/弹幕。
//
// 使用方法:
//   void Update() {
//     if (希望Boss放大招) {
//       chargeEffect.StartCharge(2.5f, OnChargeComplete);
//     }
//   }
//   void OnChargeComplete() {
//     // 这里真正释放大招(spawn 激光、弹幕等)
//     // 比如调一下 LaserBeam.FireFor(1.5f)
//   }
//
// 依赖:无。完全自包含,独立于 BossController。
//
// 设计要点:
//   - StartCharge 返回前会先 cancel 上一次充能,不会叠加;
//   - 充能中 Boss 被打死,调 CancelCharge() 可以安全终止;
//   - onComplete 是 Action<Vector3>,把充能完成时的世界坐标传出去,
//     方便你后续在那个点生成大招本体。
// ============================================================

using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Boss 大招充能 + 警告特效。挂在 Boss 身上即可。
/// </summary>
public class BossChargeWarning : MonoBehaviour
{
    // ---------- 配置:外观 ----------

    [Header("外观")]
    [Tooltip("警告主色,一般红 / 橙 / 紫表示危险。")]
    public Color warningColor = new Color(1f, 0.15f, 0.05f, 1f);

    [Tooltip("HDR 亮度倍率,配合 Bloom 用。")]
    public float colorIntensity = 4f;

    // ---------- 配置:警告圈 ----------

    [Header("地面警告圈")]
    [Tooltip("是否在 Boss 脚下/正下方生成警告圈。空中飞行的 Boss 也可以,圈会画在 Boss 同高度的水平面。")]
    public bool spawnGroundRing = true;

    [Tooltip("警告圈最终扩到的半径(世界单位)。")]
    public float maxRingRadius = 25f;

    [Tooltip("警告圈线条粗细。")]
    public float ringWidth = 1.2f;

    [Tooltip("警告圈分段数。值越大圆越圆,开销也越高。120 一般够用。")]
    [Range(24, 240)] public int ringSegments = 120;

    // ---------- 配置:充能球 ----------

    [Header("充能球(Boss 身前)")]
    [Tooltip("是否生成 Boss 身前的充能能量球。")]
    public bool spawnChargeOrb = true;

    [Tooltip("充能球相对 Boss 的偏移位置(本地坐标)。前方一点点比较合理。")]
    public Vector3 orbLocalOffset = new Vector3(0f, 0f, 8f);

    [Tooltip("充能球最大直径。")]
    public float orbMaxScale = 6f;

    // ---------- 运行期 ----------

    private Coroutine activeRoutine;       // 当前充能协程,用于 cancel
    private bool isCharging;                // 当前是否在充能,外部可以查询

    public bool IsCharging => isCharging;

    // ============================================================
    // 公共 API
    // ============================================================

    /// <summary>
    /// 开始一次充能。会自动取消上一次未完成的充能。
    /// </summary>
    /// <param name="duration">充能总时长(秒)。建议 1.5~3 秒,留给玩家反应。</param>
    /// <param name="onComplete">充能完成时的回调,参数 = 充能完成时充能球的世界坐标。
    /// 你可以在回调里生成激光、弹幕等大招本体。</param>
    public void StartCharge(float duration, Action<Vector3> onComplete = null)
    {
        if (activeRoutine != null)
        {
            CancelCharge();
        }
        activeRoutine = StartCoroutine(ChargeRoutine(duration, onComplete));
    }

    /// <summary>
    /// 强制取消当前充能(比如 Boss 在充能途中被打断)。
    /// 不会触发 onComplete。
    /// </summary>
    public void CancelCharge()
    {
        if (activeRoutine != null)
        {
            StopCoroutine(activeRoutine);
            activeRoutine = null;
        }
        isCharging = false;
        // 子物体在协程的 finally 块里清理,这里直接销毁所有 "ChargeFX_" 开头的子物体兜底
        CleanupAllFXChildren();
    }

    // ============================================================
    // 充能主流程
    // ============================================================

    /// <summary>
    /// 充能协程:阶段化驱动各个子效果,完成后清理 + 触发回调。
    /// </summary>
    private IEnumerator ChargeRoutine(float duration, Action<Vector3> onComplete)
    {
        isCharging = true;

        // ---- 构建子部件 ----
        Color hdrColor = warningColor * colorIntensity;

        LineRenderer ring = spawnGroundRing ? CreateGroundRing(hdrColor) : null;
        Material ringMat = ring != null ? ring.material : null;

        GameObject orb = spawnChargeOrb ? CreateChargeOrb(hdrColor) : null;
        Material orbMat = orb != null ? orb.GetComponent<Renderer>().material : null;

        ParticleSystem inwardSwirl = spawnChargeOrb ? CreateInwardSwirl(orb.transform, hdrColor) : null;
        Light pulseLight = CreatePulseLight(orb != null ? orb.transform : transform, hdrColor);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float progress = Mathf.Clamp01(t / duration);

            // ---- 警告圈:扩大 + 接近完成时闪烁 ----
            if (ring != null && ringMat != null)
            {
                float radius = Mathf.Lerp(1f, maxRingRadius, EaseOutQuad(progress));
                UpdateRingRadius(ring, radius);

                // 70% 之后开始闪烁警告,频率随进度加快
                if (progress > 0.7f)
                {
                    float blinkFreq = Mathf.Lerp(6f, 18f, (progress - 0.7f) / 0.3f);
                    float blink = (Mathf.Sin(t * blinkFreq * Mathf.PI * 2f) + 1f) * 0.5f;
                    Color c = hdrColor * (0.4f + 0.6f * blink);
                    ringMat.SetColor("_Color", c);
                    if (ringMat.HasProperty("_TintColor")) ringMat.SetColor("_TintColor", c);
                }
            }

            // ---- 充能球:逐渐变大 + 颜色变亮 ----
            if (orb != null && orbMat != null)
            {
                float orbScale = Mathf.Lerp(0.3f, orbMaxScale, EaseInQuad(progress));
                // 加一点高频"心跳"
                float pulse = 1f + 0.08f * Mathf.Sin(t * 12f);
                orb.transform.localScale = Vector3.one * orbScale * pulse;

                // 临近完成,颜色更白(过曝感)
                Color orbColor = Color.Lerp(hdrColor, Color.white * 5f, progress * progress);
                if (orbMat.HasProperty("_Color")) orbMat.SetColor("_Color", orbColor);
                if (orbMat.HasProperty("_EmissionColor")) orbMat.SetColor("_EmissionColor", orbColor);
            }

            // ---- 灯光:亮度随进度增强 + 高频抖动 ----
            if (pulseLight != null)
            {
                float baseIntensity = Mathf.Lerp(2f, 14f, progress);
                pulseLight.intensity = baseIntensity * (1f + 0.25f * Mathf.Sin(t * 22f));
                pulseLight.range = Mathf.Lerp(8f, 40f, progress);
            }

            yield return null;
        }

        // ---- 充能完成:最后的白光闪 + 触发回调 ----
        Vector3 completePosition = orb != null ? orb.transform.position : transform.position;
        StartCoroutine(FinalFlash(completePosition, hdrColor));
        onComplete?.Invoke(completePosition);

        // 等闪光播完再清理
        yield return new WaitForSeconds(0.25f);

        if (ring != null) DestroyFxObject(ring.gameObject);
        if (orb != null) DestroyFxObject(orb);
        if (inwardSwirl != null && orb == null) DestroyFxObject(inwardSwirl.gameObject);
        if (pulseLight != null) DestroyFxObject(pulseLight.gameObject);

        isCharging = false;
        activeRoutine = null;
    }

    /// <summary>
    /// 充能完成的最终白光闪,复用 HitExplosionEffect 的思路但只要光不要碎片。
    /// </summary>
    private IEnumerator FinalFlash(Vector3 position, Color hdrColor)
    {
        GameObject flashNode = new GameObject("ChargeFX_FinalFlash");
        flashNode.transform.position = position;

        Light flash = flashNode.AddComponent<Light>();
        flash.type = LightType.Point;
        flash.color = Color.Lerp(hdrColor, Color.white * 5f, 0.7f);
        flash.intensity = 25f;
        flash.range = 60f;

        float t = 0f;
        float startIntensity = flash.intensity;
        while (t < 0.2f)
        {
            t += Time.deltaTime;
            flash.intensity = Mathf.Lerp(startIntensity, 0f, t / 0.2f);
            yield return null;
        }

        Destroy(flashNode);
    }

    // ============================================================
    // 部件构建
    // ============================================================

    /// <summary>
    /// 地面警告圈:LineRenderer 画一个圆环。
    /// 圆心 = Boss 当前位置(Y 高度 = Boss 当前 Y),所以飞行 Boss 的警告圈也跟着它的高度。
    /// </summary>
    private LineRenderer CreateGroundRing(Color hdrColor)
    {
        GameObject node = new GameObject("ChargeFX_GroundRing");
        node.transform.SetParent(transform, false);
        node.transform.localPosition = Vector3.zero;

        LineRenderer line = node.AddComponent<LineRenderer>();
        line.useWorldSpace = false;  // 跟着 Boss 走
        line.loop = true;
        line.positionCount = ringSegments;
        line.startWidth = ringWidth;
        line.endWidth = ringWidth;
        line.numCapVertices = 2;

        Material mat = CreateAdditiveMaterial(hdrColor);
        line.material = mat;

        UpdateRingRadius(line, 1f);
        return line;
    }

    /// <summary>
    /// 更新圆环的半径(逐顶点计算)。Time.time 加进相位,圆环看起来在缓慢旋转。
    /// </summary>
    private void UpdateRingRadius(LineRenderer line, float radius)
    {
        int n = line.positionCount;
        float phase = Time.time * 0.3f;  // 缓慢旋转
        for (int i = 0; i < n; i++)
        {
            float angle = (i / (float)n) * Mathf.PI * 2f + phase;
            float x = Mathf.Cos(angle) * radius;
            float z = Mathf.Sin(angle) * radius;
            line.SetPosition(i, new Vector3(x, 0f, z));
        }
    }

    /// <summary>
    /// 充能球:一个 Sphere primitive,自发光材质,在 Boss 身前指定偏移位置。
    /// </summary>
    private GameObject CreateChargeOrb(Color hdrColor)
    {
        GameObject orb = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        orb.name = "ChargeFX_Orb";
        orb.transform.SetParent(transform, false);
        orb.transform.localPosition = orbLocalOffset;
        orb.transform.localScale = Vector3.one * 0.3f;

        Collider col = orb.GetComponent<Collider>();
        if (col != null) Destroy(col);  // 充能球不参与物理碰撞

        Renderer rend = orb.GetComponent<Renderer>();
        Material mat = CreateOrbMaterial(hdrColor);
        rend.material = mat;
        rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        rend.receiveShadows = false;

        return orb;
    }

    /// <summary>
    /// 收缩粒子云:粒子从外围一个球壳生成,向中心移动,在中心消失。
    /// 用 velocityOverLifetime 的 radial = 负值实现"向心收缩"。
    /// </summary>
    private ParticleSystem CreateInwardSwirl(Transform attachTo, Color hdrColor)
    {
        GameObject node = new GameObject("ChargeFX_InwardSwirl");
        node.transform.SetParent(attachTo, false);
        node.transform.localPosition = Vector3.zero;
        node.SetActive(false);

        ParticleSystem ps = node.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.startLifetime = 0.6f;
        main.startSpeed = 0f;  // 速度交给 velocityOverLifetime 控
        main.startSize = new ParticleSystem.MinMaxCurve(0.6f, 1.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(hdrColor, Color.white);
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.maxParticles = 120;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 70f;

        // 在球壳上发射(粒子起点在 radius 处)
        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 8f;

        // 向心运动:radial 速度设为负,粒子会沿径向往中心冲
        ParticleSystem.VelocityOverLifetimeModule velOL = ps.velocityOverLifetime;
        velOL.enabled = true;
        velOL.radial = new ParticleSystem.MinMaxCurve(-14f);

        // 生命末尾大小快速衰减,模拟"被吸入"
        ParticleSystem.SizeOverLifetimeModule sizeOL = ps.sizeOverLifetime;
        sizeOL.enabled = true;
        sizeOL.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.3f, 1f, 1.2f));

        ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = CreateAdditiveMaterial(hdrColor);

        node.SetActive(true);
        return ps;
    }

    /// <summary>
    /// 充能脉动光源:点光源 + 颜色随进度变亮。
    /// </summary>
    private Light CreatePulseLight(Transform attachTo, Color hdrColor)
    {
        GameObject node = new GameObject("ChargeFX_PulseLight");
        node.transform.SetParent(attachTo, false);
        node.transform.localPosition = Vector3.zero;

        Light light = node.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = hdrColor;
        light.intensity = 2f;
        light.range = 10f;
        return light;
    }

    // ============================================================
    // 材质工具
    // ============================================================

    /// <summary>
    /// 充能球材质:Standard shader + Emission,这样球体既有立体感又会发光。
    /// </summary>
    private Material CreateOrbMaterial(Color hdrColor)
    {
        Shader shader = CombatEffects.FindRuntimeShader("Standard", "Sprites/Default", "Unlit/Color");

        Material mat = new Material(shader) { name = "Runtime_ChargeOrb" };
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdrColor);
        if (mat.HasProperty("_EmissionColor"))
        {
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", hdrColor);
        }
        // 半透明 Additive 设置,让中心看起来更亮
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 3f);
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        return mat;
    }

    /// <summary>
    /// 通用 Additive 材质(警告圈、粒子用)。
    /// </summary>
    private Material CreateAdditiveMaterial(Color hdrColor)
    {
        Shader shader = CombatEffects.GetAdditiveEffectShader();
        if (!CombatEffects.IsRuntimeShaderUsable(shader)) shader = CombatEffects.FindRuntimeShader("Sprites/Default", "Unlit/Color");

        Material mat = new Material(shader) { name = "Runtime_ChargeFX_Additive" };
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", hdrColor);
        if (mat.HasProperty("_TintColor")) mat.SetColor("_TintColor", hdrColor);
        if (mat.HasProperty("_Mode")) mat.SetFloat("_Mode", 4f);
        if (mat.HasProperty("_SrcBlend")) mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend")) mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.One);
        if (mat.HasProperty("_ZWrite")) mat.SetInt("_ZWrite", 0);
        mat.renderQueue = 3000;
        return mat;
    }

    /// <summary>
    /// 兜底清理:CancelCharge 后调一次,把所有 "ChargeFX_" 开头的子物体扫掉。
    /// </summary>
    private void CleanupAllFXChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name.StartsWith("ChargeFX_"))
            {
                DestroyFxObject(child.gameObject);
            }
        }
    }

    private void DestroyFxObject(GameObject fxObject)
    {
        if (fxObject == null)
            return;

        Renderer[] renderers = fxObject.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            Material[] materials = renderer.sharedMaterials;
            foreach (Material material in materials)
            {
                if (material != null && material.name.StartsWith("Runtime_Charge"))
                {
                    Destroy(material);
                }
            }
        }

        Destroy(fxObject);
    }

    // ============================================================
    // 数学工具:缓动函数
    // ============================================================

    private static float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);
    private static float EaseInQuad(float t) => t * t;
}

// ============================================================
// 用法速查
// ------------------------------------------------------------
// // 简单触发(默认 2.5 秒充能)
// chargeFx.StartCharge(2.5f, releasePos =>
// {
//     // 充能完成,在 releasePos 真正释放大招
//     Debug.Log("BOOM at " + releasePos);
//     // laser.transform.position = releasePos;
//     // laser.FireFor(1.5f);
// });
//
// // Boss 被打死,中断充能
// if (bossHp <= 0 && chargeFx.IsCharging) chargeFx.CancelCharge();
//
// // 想换颜色(紫色诡异感):
// chargeFx.warningColor = new Color(0.8f, 0.1f, 0.9f);
// chargeFx.colorIntensity = 5f;
// ============================================================
