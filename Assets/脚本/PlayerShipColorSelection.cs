using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public enum PlayerShipColorChoice
{
    Blue,
    Green
}

public struct PlayerShipVisualTheme
{
    public PlayerShipColorChoice Choice;
    public Color ShipTint;
    public Color ShipGlow;
    public Color HeroEnergy;
    public Color EngineLight;
    public Color EngineWhiteCoreMid;
    public Color EngineWhiteCoreEnd;
    public Color EngineOuterStart;
    public Color EngineOuterMid;
    public Color EngineOuterEnd;
    public Color BulletTrail;
    public Color MuzzleLight;
    public Color MuzzleFlash;
    public Color HitLight;
    public Color HitSparks;
    public Color Laser;
    public Color ShieldLight;
    public Color ShieldShell;
    public Color ShieldEmission;
    public Color ShieldRing;
    public Color ShieldParticleStart;
    public Color ShieldParticleEnd;
    public Color SkillStatusText;
    public Color VoidInnerRing;
    public Color VoidTear;
    public Color VoidLight;
    public Color VoidCollapseRing;
    public Color VoidCollapseFlash;
    public Color VoidCrushStart;
}

public static class PlayerShipColorSelection
{
    private static PlayerShipColorChoice currentChoice = PlayerShipColorChoice.Blue;
    private static bool hasSelection;

    public static PlayerShipVisualTheme CurrentTheme
    {
        get { return GetTheme(hasSelection ? currentChoice : PlayerShipColorChoice.Blue); }
    }

    public static PlayerShipVisualTheme BlueTheme
    {
        get { return GetTheme(PlayerShipColorChoice.Blue); }
    }

    public static void ResetToDefault()
    {
        currentChoice = PlayerShipColorChoice.Blue;
        hasSelection = false;
    }

    public static void Select(PlayerShipColorChoice choice)
    {
        currentChoice = choice;
        hasSelection = true;
    }

    public static bool IsSinglePlayerScene(string sceneName)
    {
        return sceneName == "GameScene1" || sceneName == "GameScene2";
    }

    public static PlayerShipVisualTheme GetTheme(PlayerShipColorChoice choice)
    {
        return choice == PlayerShipColorChoice.Green ? CreateGreenTheme() : CreateBlueTheme();
    }

    private static PlayerShipVisualTheme CreateBlueTheme()
    {
        return new PlayerShipVisualTheme
        {
            Choice = PlayerShipColorChoice.Blue,
            ShipTint = new Color(0.04f, 0.18f, 0.22f),
            ShipGlow = new Color(0.08f, 0.92f, 1f),
            HeroEnergy = new Color(0.05f, 0.95f, 1f, 1f),
            EngineLight = new Color(0.18f, 0.82f, 1f),
            EngineWhiteCoreMid = new Color(0.62f, 1.45f, 2.6f, 1f),
            EngineWhiteCoreEnd = new Color(0.05f, 0.58f, 1.7f, 0f),
            EngineOuterStart = new Color(0.18f, 1.0f, 2.4f, 0.86f),
            EngineOuterMid = new Color(0.0f, 0.72f, 2.8f, 0.42f),
            EngineOuterEnd = new Color(0.0f, 0.18f, 0.9f, 0f),
            BulletTrail = new Color(0.15f, 0.95f, 1f, 1f),
            MuzzleLight = new Color(0.25f, 0.85f, 1f),
            MuzzleFlash = new Color(0.2f, 0.9f, 1f, 1f),
            HitLight = new Color(0.25f, 0.9f, 1f),
            HitSparks = new Color(0.25f, 0.9f, 1f, 1f),
            Laser = new Color(0.2f, 1f, 0.85f, 1f),
            ShieldLight = new Color(0.08f, 0.95f, 1f),
            ShieldShell = new Color(0.12f, 0.85f, 1f, 0.22f),
            ShieldEmission = new Color(0.05f, 0.75f, 1f),
            ShieldRing = new Color(0.15f, 0.95f, 1f, 0.72f),
            ShieldParticleStart = new Color(0.08f, 0.95f, 1f),
            ShieldParticleEnd = new Color(0.08f, 0.55f, 1f),
            SkillStatusText = new Color(0.16f, 1f, 1f, 1f),
            VoidInnerRing = new Color(0.46f, 0.78f, 1.05f, 0.55f),
            VoidTear = new Color(0.58f, 0.76f, 1.05f, 0.36f),
            VoidLight = new Color(0.38f, 0.72f, 1f),
            VoidCollapseRing = new Color(0.45f, 0.62f, 0.9f, 0.58f),
            VoidCollapseFlash = new Color(0.55f, 0.78f, 1f),
            VoidCrushStart = new Color(0.78f, 0.9f, 1f)
        };
    }

    private static PlayerShipVisualTheme CreateGreenTheme()
    {
        return new PlayerShipVisualTheme
        {
            Choice = PlayerShipColorChoice.Green,
            ShipTint = new Color(0.04f, 0.22f, 0.1f),
            ShipGlow = new Color(0.08f, 1f, 0.36f),
            HeroEnergy = new Color(0.05f, 1f, 0.32f, 1f),
            EngineLight = new Color(0.18f, 1f, 0.42f),
            EngineWhiteCoreMid = new Color(0.62f, 2.6f, 1.05f, 1f),
            EngineWhiteCoreEnd = new Color(0.05f, 1.7f, 0.58f, 0f),
            EngineOuterStart = new Color(0.18f, 2.4f, 0.85f, 0.86f),
            EngineOuterMid = new Color(0.0f, 2.8f, 0.72f, 0.42f),
            EngineOuterEnd = new Color(0.0f, 0.9f, 0.18f, 0f),
            BulletTrail = new Color(0.15f, 1f, 0.32f, 1f),
            MuzzleLight = new Color(0.25f, 1f, 0.38f),
            MuzzleFlash = new Color(0.2f, 1f, 0.34f, 1f),
            HitLight = new Color(0.25f, 1f, 0.38f),
            HitSparks = new Color(0.25f, 1f, 0.38f, 1f),
            Laser = new Color(0.35f, 1f, 0.28f, 1f),
            ShieldLight = new Color(0.08f, 1f, 0.32f),
            ShieldShell = new Color(0.12f, 1f, 0.36f, 0.22f),
            ShieldEmission = new Color(0.05f, 1f, 0.28f),
            ShieldRing = new Color(0.15f, 1f, 0.32f, 0.72f),
            ShieldParticleStart = new Color(0.08f, 1f, 0.32f),
            ShieldParticleEnd = new Color(0.08f, 0.9f, 0.2f),
            SkillStatusText = new Color(0.2f, 1f, 0.42f, 1f),
            VoidInnerRing = new Color(0.46f, 1.05f, 0.58f, 0.55f),
            VoidTear = new Color(0.58f, 1.05f, 0.64f, 0.36f),
            VoidLight = new Color(0.38f, 1f, 0.46f),
            VoidCollapseRing = new Color(0.45f, 0.9f, 0.48f, 0.58f),
            VoidCollapseFlash = new Color(0.55f, 1f, 0.58f),
            VoidCrushStart = new Color(0.78f, 1f, 0.78f)
        };
    }
}

public static class TmpChineseFontFallback
{
    private static readonly string[] PreferredChineseFonts =
    {
        "Source Han Sans SC",
        "Microsoft YaHei UI",
        "Microsoft YaHei",
        "SimHei",
        "SimSun",
        "Noto Sans CJK SC"
    };

    private static TMP_FontAsset chineseFallback;
    private static bool runnerCreated;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        EnsureInstalled();
        CreateRunner();
    }

    public static void EnsureInstalled()
    {
        TMP_FontAsset fallback = GetOrCreateFallback();
        if (fallback == null)
            return;

        AddFallback(TMP_Settings.defaultFontAsset, fallback);

        if (TMP_Settings.fallbackFontAssets != null && !TMP_Settings.fallbackFontAssets.Contains(fallback))
        {
            TMP_Settings.fallbackFontAssets.Add(fallback);
        }

        PatchLoadedTexts(fallback);
    }

    private static TMP_FontAsset GetOrCreateFallback()
    {
        if (chineseFallback != null)
            return chineseFallback;

        Font font = Font.CreateDynamicFontFromOSFont(PreferredChineseFonts, 90);
        if (font == null)
        {
            Debug.LogWarning("TmpChineseFontFallback: no Chinese OS font was found, TMP Chinese text may still miss glyphs.");
            return null;
        }

        chineseFallback = TMP_FontAsset.CreateFontAsset(
            font,
            90,
            9,
            GlyphRenderMode.SDFAA,
            2048,
            2048,
            AtlasPopulationMode.Dynamic,
            true);

        if (chineseFallback != null)
        {
            chineseFallback.name = "Runtime Chinese TMP Fallback";
            chineseFallback.hideFlags = HideFlags.HideAndDontSave;
            chineseFallback.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            chineseFallback.isMultiAtlasTexturesEnabled = true;
        }

        return chineseFallback;
    }

    private static void PatchLoadedTexts(TMP_FontAsset fallback)
    {
        TMP_Text[] texts = UnityEngine.Object.FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text == null)
                continue;

            if (text.font == null)
            {
                text.font = TMP_Settings.defaultFontAsset;
            }

            AddFallback(text.font, fallback);
            text.SetVerticesDirty();
            text.SetLayoutDirty();
        }
    }

    private static void AddFallback(TMP_FontAsset font, TMP_FontAsset fallback)
    {
        if (font == null || fallback == null || font == fallback)
            return;

        if (font.fallbackFontAssetTable == null)
        {
            font.fallbackFontAssetTable = new System.Collections.Generic.List<TMP_FontAsset>();
        }

        if (!font.fallbackFontAssetTable.Contains(fallback))
        {
            font.fallbackFontAssetTable.Add(fallback);
        }
    }

    private static void CreateRunner()
    {
        if (runnerCreated)
            return;

        runnerCreated = true;
        GameObject runner = new GameObject("TMP Chinese Font Fallback Bootstrap");
        runner.hideFlags = HideFlags.HideAndDontSave;
        UnityEngine.Object.DontDestroyOnLoad(runner);
        runner.AddComponent<TmpChineseFontFallbackRunner>();
    }
}

public class TmpChineseFontFallbackRunner : MonoBehaviour
{
    private int framesToPatch;

    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
        framesToPatch = 4;
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void LateUpdate()
    {
        if (framesToPatch <= 0)
            return;

        framesToPatch--;
        TmpChineseFontFallback.EnsureInstalled();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        framesToPatch = 4;
        TmpChineseFontFallback.EnsureInstalled();
    }
}

public static class PlayerShipColorSelectionPrompt
{
    private static Font uiFont;
    private static Sprite softCircleSprite;
    private static Sprite shipSilhouetteSprite;
    private static Sprite panelGradientSprite;

    public static GameObject Create(Action<PlayerShipColorChoice> onSelected, Action onClosed)
    {
        TmpChineseFontFallback.EnsureInstalled();

        GameObject overlay = new GameObject("Ship Color Selection Overlay");
        Canvas canvas = overlay.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = overlay.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        overlay.AddComponent<GraphicRaycaster>();

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image dim = overlay.AddComponent<Image>();
        dim.color = new Color(0.005f, 0.014f, 0.026f, 0.94f);

        CreateBackdropLines(overlay.transform);

        GameObject glow = CreateRect("Selection Panel Ambient Glow", overlay.transform, new Vector2(1040f, 640f), Vector2.zero);
        Image glowImage = glow.AddComponent<Image>();
        glowImage.sprite = GetSoftCircleSprite();
        glowImage.color = new Color(0.04f, 0.65f, 1f, 0.12f);
        glowImage.raycastTarget = false;

        GameObject panel = CreateRect("Color Selection Panel", overlay.transform, new Vector2(900f, 560f), Vector2.zero);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.sprite = GetPanelGradientSprite();
        panelImage.color = Color.white;

        CreateBorder(panel.transform, new Vector2(900f, 560f), new Color(0.14f, 0.85f, 1f, 0.38f), 2f);
        CreateCornerBrackets(panel.transform, new Vector2(900f, 560f), new Color(0.25f, 1f, 0.95f, 0.86f), 44f, 4f);
        CreateLine(panel.transform, "Header Energy Line", new Vector2(730f, 2f), new Vector2(0f, 139f), new Color(0.16f, 0.92f, 1f, 0.55f));

        Text title = CreateText(panel.transform, "选择战机能量颜色", 46, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, 198f), new Vector2(760f, 60f), Color.white);
        AddTextShadow(title, new Color(0f, 0.65f, 1f, 0.42f), new Vector2(0f, -3f));
        CreateText(panel.transform, "本次出击生效，机身、射击与技能光效同步换色", 22, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, 153f), new Vector2(780f, 34f), new Color(0.72f, 0.88f, 0.96f, 1f));

        CreateChoiceButton(panel.transform, "蓝色", "经典蓝色能量", PlayerShipColorChoice.Blue, new Color(0.1f, 0.82f, 1f, 1f), new Vector2(-215f, -26f), onSelected);
        CreateChoiceButton(panel.transform, "绿色", "绿色复刻能量", PlayerShipColorChoice.Green, new Color(0.16f, 1f, 0.36f, 1f), new Vector2(215f, -26f), onSelected);
        CreateCloseButton(panel.transform, onClosed);

        return overlay;
    }

    private static void CreateChoiceButton(Transform parent, string title, string subtitle, PlayerShipColorChoice choice, Color accentColor, Vector2 anchoredPosition, Action<PlayerShipColorChoice> onSelected)
    {
        GameObject glow = CreateRect(title + " Energy Glow", parent, new Vector2(390f, 310f), anchoredPosition);
        Image glowImage = glow.AddComponent<Image>();
        glowImage.sprite = GetSoftCircleSprite();
        glowImage.color = WithAlpha(accentColor, 0.11f);
        glowImage.raycastTarget = false;

        GameObject buttonObject = CreateRect(title + " Energy Bay", parent, new Vector2(340f, 258f), anchoredPosition);
        RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
        Image background = buttonObject.AddComponent<Image>();
        Color baseColor = Color.Lerp(new Color(0.018f, 0.026f, 0.044f, 0.98f), accentColor, 0.08f);
        background.color = baseColor;

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() => onSelected?.Invoke(choice));

        Image tint = CreateRect(title + " Card Tint", buttonObject.transform, new Vector2(340f, 258f), Vector2.zero).AddComponent<Image>();
        tint.color = WithAlpha(accentColor, 0.045f);
        tint.raycastTarget = false;

        CreateBorder(buttonObject.transform, new Vector2(340f, 258f), WithAlpha(accentColor, 0.58f), 2f);
        CreateCornerBrackets(buttonObject.transform, new Vector2(340f, 258f), WithAlpha(accentColor, 0.9f), 26f, 3f);

        Image topBar = CreateRect(title + " Energy Bar", buttonObject.transform, new Vector2(186f, 6f), new Vector2(0f, 100f)).AddComponent<Image>();
        topBar.color = accentColor;
        topBar.raycastTarget = false;
        CreateLine(buttonObject.transform, title + " Energy Bar Underline", new Vector2(248f, 1.5f), new Vector2(0f, 88f), WithAlpha(accentColor, 0.35f));

        GameObject markGlow = CreateRect(title + " Ship Mark Glow", buttonObject.transform, new Vector2(122f, 122f), new Vector2(0f, 32f));
        Image markGlowImage = markGlow.AddComponent<Image>();
        markGlowImage.sprite = GetSoftCircleSprite();
        markGlowImage.color = WithAlpha(accentColor, 0.26f);
        markGlowImage.raycastTarget = false;

        GameObject shipMark = CreateRect(title + " Ship Mark", buttonObject.transform, new Vector2(86f, 86f), new Vector2(0f, 32f));
        Image shipImage = shipMark.AddComponent<Image>();
        shipImage.sprite = GetShipSilhouetteSprite();
        shipImage.color = accentColor;
        shipImage.raycastTarget = false;

        Text titleText = CreateText(buttonObject.transform, title, 38, FontStyle.Bold, TextAnchor.MiddleCenter, new Vector2(0f, -50f), new Vector2(260f, 46f), Color.white);
        AddTextShadow(titleText, WithAlpha(accentColor, 0.55f), new Vector2(0f, -2f));
        CreateText(buttonObject.transform, subtitle, 20, FontStyle.Normal, TextAnchor.MiddleCenter, new Vector2(0f, -88f), new Vector2(260f, 30f), new Color(0.76f, 0.9f, 0.94f, 1f));

        EnergyChoiceButtonFx feedback = buttonObject.AddComponent<EnergyChoiceButtonFx>();
        feedback.Initialize(buttonRect, background, glowImage, topBar, baseColor, accentColor);
    }

    private static void CreateCloseButton(Transform parent, Action onClosed)
    {
        GameObject buttonObject = CreateRect("Close Color Selection", parent, new Vector2(138f, 44f), new Vector2(0f, -236f));
        Image background = buttonObject.AddComponent<Image>();
        background.color = new Color(0.06f, 0.1f, 0.14f, 0.96f);
        CreateBorder(buttonObject.transform, new Vector2(138f, 44f), new Color(0.58f, 0.78f, 0.88f, 0.35f), 1.5f);

        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = background.color;
        colors.highlightedColor = new Color(0.1f, 0.18f, 0.24f, 0.98f);
        colors.pressedColor = new Color(0.025f, 0.05f, 0.08f, 0.98f);
        colors.selectedColor = colors.highlightedColor;
        colors.colorMultiplier = 1f;
        button.colors = colors;
        button.onClick.AddListener(() => onClosed?.Invoke());
        CreateText(buttonObject.transform, "返回", 20, FontStyle.Bold, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(122f, 38f), new Color(0.86f, 0.94f, 0.98f, 1f));
    }

    private static GameObject CreateRect(string objectName, Transform parent, Vector2 size, Vector2 anchoredPosition)
    {
        GameObject node = new GameObject(objectName);
        node.transform.SetParent(parent, false);

        RectTransform rect = node.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = anchoredPosition;
        return node;
    }

    private static void CreateBackdropLines(Transform parent)
    {
        for (int i = 0; i < 7; i++)
        {
            float y = -420f + i * 140f;
            CreateLine(parent, "Backdrop Scan Line " + i, new Vector2(1500f, 1.2f), new Vector2(0f, y), new Color(0.08f, 0.78f, 1f, 0.045f));
        }

        CreateLine(parent, "Backdrop Left Rail", new Vector2(2f, 760f), new Vector2(-650f, 0f), new Color(0.08f, 0.78f, 1f, 0.08f));
        CreateLine(parent, "Backdrop Right Rail", new Vector2(2f, 760f), new Vector2(650f, 0f), new Color(0.08f, 1f, 0.55f, 0.07f));
    }

    private static Image CreateLine(Transform parent, string objectName, Vector2 size, Vector2 anchoredPosition, Color color)
    {
        GameObject lineObject = CreateRect(objectName, parent, size, anchoredPosition);
        Image line = lineObject.AddComponent<Image>();
        line.color = color;
        line.raycastTarget = false;
        return line;
    }

    private static void CreateBorder(Transform parent, Vector2 size, Color color, float thickness)
    {
        CreateLine(parent, "Border Top", new Vector2(size.x, thickness), new Vector2(0f, size.y * 0.5f), color);
        CreateLine(parent, "Border Bottom", new Vector2(size.x, thickness), new Vector2(0f, -size.y * 0.5f), color);
        CreateLine(parent, "Border Left", new Vector2(thickness, size.y), new Vector2(-size.x * 0.5f, 0f), color);
        CreateLine(parent, "Border Right", new Vector2(thickness, size.y), new Vector2(size.x * 0.5f, 0f), color);
    }

    private static void CreateCornerBrackets(Transform parent, Vector2 size, Color color, float length, float thickness)
    {
        float x = size.x * 0.5f;
        float y = size.y * 0.5f;
        float half = length * 0.5f;

        CreateLine(parent, "Corner TL Horizontal", new Vector2(length, thickness), new Vector2(-x + half, y), color);
        CreateLine(parent, "Corner TL Vertical", new Vector2(thickness, length), new Vector2(-x, y - half), color);
        CreateLine(parent, "Corner TR Horizontal", new Vector2(length, thickness), new Vector2(x - half, y), color);
        CreateLine(parent, "Corner TR Vertical", new Vector2(thickness, length), new Vector2(x, y - half), color);
        CreateLine(parent, "Corner BL Horizontal", new Vector2(length, thickness), new Vector2(-x + half, -y), color);
        CreateLine(parent, "Corner BL Vertical", new Vector2(thickness, length), new Vector2(-x, -y + half), color);
        CreateLine(parent, "Corner BR Horizontal", new Vector2(length, thickness), new Vector2(x - half, -y), color);
        CreateLine(parent, "Corner BR Vertical", new Vector2(thickness, length), new Vector2(x, -y + half), color);
    }

    private static Text CreateText(Transform parent, string text, int fontSize, FontStyle fontStyle, TextAnchor alignment, Vector2 anchoredPosition, Vector2 size, Color color)
    {
        GameObject textObject = CreateRect(text + " Text", parent, size, anchoredPosition);
        Text label = textObject.AddComponent<Text>();
        label.text = text;
        label.font = GetUiFont();
        label.fontSize = fontSize;
        label.fontStyle = fontStyle;
        label.alignment = alignment;
        label.color = color;
        label.raycastTarget = false;
        return label;
    }

    private static void AddTextShadow(Text label, Color color, Vector2 distance)
    {
        if (label == null)
            return;

        Shadow shadow = label.gameObject.AddComponent<Shadow>();
        shadow.effectColor = color;
        shadow.effectDistance = distance;
    }

    private static Color WithAlpha(Color color, float alpha)
    {
        color.a = alpha;
        return color;
    }

    private static Sprite GetSoftCircleSprite()
    {
        if (softCircleSprite != null)
            return softCircleSprite;

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime_UI_Soft_Circle",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
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
        softCircleSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        softCircleSprite.hideFlags = HideFlags.HideAndDontSave;
        return softCircleSprite;
    }

    private static Sprite GetShipSilhouetteSprite()
    {
        if (shipSilhouetteSprite != null)
            return shipSilhouetteSprite;

        const int size = 96;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "Runtime_UI_Ship_Mark",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Point,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color clear = new Color(1f, 1f, 1f, 0f);
        Color ink = Color.white;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f) / size * 2f - 1f;
                float ny = (y + 0.5f) / size;
                bool nose = ny > 0.58f && Mathf.Abs(nx) < Mathf.Lerp(0.08f, 0.02f, Mathf.InverseLerp(0.58f, 0.95f, ny));
                bool body = ny > 0.22f && ny <= 0.72f && Mathf.Abs(nx) < 0.09f;
                bool wing = ny > 0.27f && ny < 0.52f && Mathf.Abs(nx) > 0.09f && Mathf.Abs(nx) < Mathf.Lerp(0.58f, 0.16f, Mathf.InverseLerp(0.27f, 0.52f, ny));
                bool tail = ny > 0.13f && ny < 0.28f && Mathf.Abs(nx) < Mathf.Lerp(0.34f, 0.1f, Mathf.InverseLerp(0.13f, 0.28f, ny));
                texture.SetPixel(x, y, nose || body || wing || tail ? ink : clear);
            }
        }

        texture.Apply(false, true);
        shipSilhouetteSprite = Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
        shipSilhouetteSprite.hideFlags = HideFlags.HideAndDontSave;
        return shipSilhouetteSprite;
    }

    private static Sprite GetPanelGradientSprite()
    {
        if (panelGradientSprite != null)
            return panelGradientSprite;

        const int width = 8;
        const int height = 96;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = "Runtime_UI_Panel_Gradient",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        Color top = new Color(0.018f, 0.055f, 0.076f, 0.985f);
        Color bottom = new Color(0.008f, 0.021f, 0.034f, 0.985f);
        for (int y = 0; y < height; y++)
        {
            float t = y / (float)(height - 1);
            Color color = Color.Lerp(bottom, top, t);
            for (int x = 0; x < width; x++)
            {
                texture.SetPixel(x, y, color);
            }
        }

        texture.Apply(false, true);
        panelGradientSprite = Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f);
        panelGradientSprite.hideFlags = HideFlags.HideAndDontSave;
        return panelGradientSprite;
    }

    private static Font GetUiFont()
    {
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        return uiFont;
    }
}

public class EnergyChoiceButtonFx : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    private RectTransform rect;
    private Image background;
    private Image glow;
    private Image bar;
    private Color baseColor;
    private Color accentColor;
    private bool hovered;
    private bool pressed;

    public void Initialize(RectTransform targetRect, Image backgroundImage, Image glowImage, Image barImage, Color normalColor, Color accent)
    {
        rect = targetRect;
        background = backgroundImage;
        glow = glowImage;
        bar = barImage;
        baseColor = normalColor;
        accentColor = accent;
    }

    void Update()
    {
        if (rect == null || background == null)
            return;

        float targetScale = pressed ? 0.985f : hovered ? 1.035f : 1f;
        rect.localScale = Vector3.Lerp(rect.localScale, Vector3.one * targetScale, Time.unscaledDeltaTime * 12f);

        Color targetBackground = pressed
            ? Color.Lerp(baseColor, Color.black, 0.18f)
            : hovered ? Color.Lerp(baseColor, accentColor, 0.14f) : baseColor;
        background.color = Color.Lerp(background.color, targetBackground, Time.unscaledDeltaTime * 10f);

        if (glow != null)
        {
            Color glowColor = accentColor;
            glowColor.a = pressed ? 0.24f : hovered ? 0.19f : 0.11f;
            glow.color = Color.Lerp(glow.color, glowColor, Time.unscaledDeltaTime * 9f);
        }

        if (bar != null)
        {
            Color barColor = accentColor;
            barColor.a = pressed ? 0.78f : hovered ? 1f : 0.82f;
            bar.color = Color.Lerp(bar.color, barColor, Time.unscaledDeltaTime * 10f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovered = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovered = false;
        pressed = false;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressed = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        pressed = false;
    }
}
