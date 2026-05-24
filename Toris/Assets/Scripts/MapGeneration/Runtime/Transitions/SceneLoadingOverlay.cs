using UnityEngine;
using UnityEngine.UIElements;

internal struct SceneLoadingOverlaySettings
{
    public Sprite Background;
    public Color FallbackColor;
    public float BackgroundOverscan;
    public float DimAlpha;
    public string LoadingMessage;
    public float DotIntervalSeconds;
    public int ActivationDotCount;
}

internal sealed class SceneLoadingOverlay
{
    private const string DefaultLoadingMessage = "Loading";
    private const float MinimumDotIntervalSeconds = 0.05f;
    private const float MinimumBackgroundOverscan = 1f;
    private const float MaximumBackgroundOverscan = 1.08f;
    private const string RootName = "LoadingScreenRoot";
    private const string ContentName = "LoadingContent";
    private const string BackgroundBackingName = "LoadingBackgroundBacking";
    private const string BackgroundName = "LoadingBackground";
    private const string DimmerName = "LoadingDimmer";
    private const string LabelRootName = "LoadingLabelRoot";
    private const string LabelName = "LoadingLabel";
    private const string CoverName = "TransitionCover";

    private readonly GameObject hostObject;
    private readonly UIDocument document;
    private readonly VisualElement root;
    private readonly VisualElement content;
    private readonly VisualElement backgroundBacking;
    private readonly VisualElement background;
    private readonly VisualElement dimmer;
    private readonly VisualElement labelRoot;
    private readonly Label loadingLabel;
    private readonly VisualElement cover;

    private string message = DefaultLoadingMessage;
    private float dotIntervalSeconds = 0.35f;
    private int maxDotCount = 3;
    private int currentDotCount;
    private float nextDotTime;
    private bool animateDots;
    private float alpha;
    private float contentAlpha;
    private float coverAlpha;

    public UIDocument Document => document;
    public float Alpha => alpha;
    public float ContentAlpha => contentAlpha;
    public float CoverAlpha => coverAlpha;

    public SceneLoadingOverlay(VisualTreeAsset template, PanelSettings panelSettings)
    {
        hostObject = new GameObject("SceneTransition_LoadingScreen");
        Object.DontDestroyOnLoad(hostObject);

        document = hostObject.AddComponent<UIDocument>();
        document.panelSettings = panelSettings;

        VisualElement documentRoot = document.rootVisualElement;
        if (template != null)
        {
            template.CloneTree(documentRoot);
        }

        root = documentRoot.Q<VisualElement>(RootName);
        if (root == null)
        {
            root = BuildFallbackLayout(documentRoot);
        }

        content = root.Q<VisualElement>(ContentName);
        backgroundBacking = root.Q<VisualElement>(BackgroundBackingName);
        background = root.Q<VisualElement>(BackgroundName);
        dimmer = root.Q<VisualElement>(DimmerName);
        labelRoot = root.Q<VisualElement>(LabelRootName);
        loadingLabel = root.Q<Label>(LabelName);
        cover = root.Q<VisualElement>(CoverName);

        Hide();
    }

    public void Show(SceneLoadingOverlaySettings settings)
    {
        Color opaqueFallbackColor = settings.FallbackColor;
        opaqueFallbackColor.a = 1f;

        message = string.IsNullOrWhiteSpace(settings.LoadingMessage)
            ? DefaultLoadingMessage
            : settings.LoadingMessage.Trim();

        dotIntervalSeconds = Mathf.Max(MinimumDotIntervalSeconds, settings.DotIntervalSeconds);
        maxDotCount = Mathf.Max(1, settings.ActivationDotCount);
        currentDotCount = Mathf.Min(1, maxDotCount);
        animateDots = false;
        nextDotTime = Time.realtimeSinceStartup;

        backgroundBacking.style.backgroundColor = opaqueFallbackColor;
        ConfigureBackground(settings.Background, settings.BackgroundOverscan, opaqueFallbackColor);
        dimmer.style.backgroundColor = new Color(0f, 0f, 0f, Mathf.Clamp01(settings.DimAlpha));
        cover.style.backgroundColor = new Color(opaqueFallbackColor.r, opaqueFallbackColor.g, opaqueFallbackColor.b, 0f);

        ApplyText();

        root.style.display = DisplayStyle.Flex;
        root.pickingMode = PickingMode.Position;
        labelRoot.style.display = DisplayStyle.Flex;
        loadingLabel.style.display = DisplayStyle.Flex;
        SetAlpha(1f);
        SetContentAlpha(0f);
        SetCoverAlpha(0f);
    }

    public void ResetLoadingContent()
    {
        currentDotCount = Mathf.Min(1, maxDotCount);
        animateDots = false;
        nextDotTime = Time.realtimeSinceStartup;
        labelRoot.style.display = DisplayStyle.Flex;
        loadingLabel.style.display = DisplayStyle.Flex;
        ApplyText();
        SetContentAlpha(0f);
    }

    public void StartLoadingAnimation()
    {
        currentDotCount = Mathf.Min(1, maxDotCount);
        animateDots = true;
        nextDotTime = Time.realtimeSinceStartup + dotIntervalSeconds;
        labelRoot.style.display = DisplayStyle.Flex;
        loadingLabel.style.display = DisplayStyle.Flex;
        ApplyText();
    }

    public void Tick()
    {
        if (root.style.display == DisplayStyle.None || !animateDots)
            return;

        while (Time.realtimeSinceStartup >= nextDotTime)
        {
            currentDotCount = currentDotCount >= maxDotCount ? 1 : currentDotCount + 1;
            nextDotTime += dotIntervalSeconds;
            ApplyText();
        }
    }

    public void CompleteLoading()
    {
        animateDots = false;
        currentDotCount = maxDotCount;
        ApplyText();
        labelRoot.style.display = DisplayStyle.None;
        loadingLabel.style.display = DisplayStyle.None;
    }

    public void SetAlpha(float value)
    {
        alpha = Mathf.Clamp01(value);
        root.style.opacity = alpha;
    }

    public void SetContentAlpha(float value)
    {
        contentAlpha = Mathf.Clamp01(value);
        content.style.opacity = contentAlpha;
    }

    public void SetCoverAlpha(float value)
    {
        coverAlpha = Mathf.Clamp01(value);
        Color coverColor = cover.resolvedStyle.backgroundColor;
        cover.style.backgroundColor = new Color(coverColor.r, coverColor.g, coverColor.b, coverAlpha);
    }

    public void Hide()
    {
        animateDots = false;
        labelRoot.style.display = DisplayStyle.None;
        loadingLabel.style.display = DisplayStyle.None;
        SetAlpha(0f);
        SetContentAlpha(0f);
        SetCoverAlpha(0f);
        root.pickingMode = PickingMode.Ignore;
        root.style.display = DisplayStyle.None;
    }

    public void Dispose()
    {
        if (hostObject != null)
        {
            Object.Destroy(hostObject);
        }
    }

    private void ConfigureBackground(Sprite sprite, float overscan, Color fallbackColor)
    {
        float resolvedOverscan = Mathf.Clamp(
            overscan,
            MinimumBackgroundOverscan,
            MaximumBackgroundOverscan);
        float insetPercent = (1f - resolvedOverscan) * 50f;

        background.style.left = Length.Percent(insetPercent);
        background.style.right = Length.Percent(insetPercent);
        background.style.top = Length.Percent(insetPercent);
        background.style.bottom = Length.Percent(insetPercent);

        if (sprite == null)
        {
            background.style.backgroundImage = null;
            background.style.backgroundColor = fallbackColor;
            return;
        }

        background.style.backgroundImage = new StyleBackground(sprite);
        background.style.backgroundColor = Color.clear;
    }

    private void ApplyText()
    {
        int visibleDotCount = Mathf.Clamp(currentDotCount, 0, maxDotCount);
        loadingLabel.text = $"{message}{new string('.', visibleDotCount)}";
    }

    private static VisualElement BuildFallbackLayout(VisualElement documentRoot)
    {
        VisualElement fallbackRoot = new VisualElement { name = RootName };
        fallbackRoot.AddToClassList("loading-screen");

        VisualElement fallbackContent = new VisualElement { name = ContentName };
        fallbackContent.AddToClassList("loading-screen__content");
        fallbackRoot.Add(fallbackContent);

        VisualElement fallbackBacking = new VisualElement { name = BackgroundBackingName };
        fallbackBacking.AddToClassList("loading-screen__background-backing");
        fallbackContent.Add(fallbackBacking);

        VisualElement fallbackBackground = new VisualElement { name = BackgroundName };
        fallbackBackground.AddToClassList("loading-screen__background");
        fallbackContent.Add(fallbackBackground);

        VisualElement fallbackDimmer = new VisualElement { name = DimmerName };
        fallbackDimmer.AddToClassList("loading-screen__dimmer");
        fallbackContent.Add(fallbackDimmer);

        VisualElement fallbackLabelRoot = new VisualElement { name = LabelRootName };
        fallbackLabelRoot.AddToClassList("loading-screen__label-root");
        fallbackContent.Add(fallbackLabelRoot);

        VisualElement fallbackTopAccent = new VisualElement();
        fallbackTopAccent.AddToClassList("loading-screen__accent");
        fallbackTopAccent.AddToClassList("loading-screen__accent--top");
        fallbackLabelRoot.Add(fallbackTopAccent);

        Label fallbackLabel = new Label { name = LabelName };
        fallbackLabel.AddToClassList("loading-screen__label");
        fallbackLabelRoot.Add(fallbackLabel);

        VisualElement fallbackBottomAccent = new VisualElement();
        fallbackBottomAccent.AddToClassList("loading-screen__accent");
        fallbackBottomAccent.AddToClassList("loading-screen__accent--bottom");
        fallbackLabelRoot.Add(fallbackBottomAccent);

        VisualElement fallbackCover = new VisualElement { name = CoverName };
        fallbackCover.AddToClassList("loading-screen__cover");
        fallbackRoot.Add(fallbackCover);

        documentRoot.Add(fallbackRoot);
        return fallbackRoot;
    }
}
