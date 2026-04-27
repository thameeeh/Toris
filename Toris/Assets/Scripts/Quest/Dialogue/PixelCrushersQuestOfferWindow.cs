using System;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens a simple available-job window from Pixel Crushers dialogue.
/// Pixel Crushers still owns quest state; this component only presents configured offer groups
/// and starts the selected quest through the Dialogue System quest API.
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersQuestOfferWindow : MonoBehaviour
{
    [Tooltip("Named groups of available jobs. Dialogue opens a group with TorisOpenQuestOffers(\"GroupId\").")]
    [SerializeField] private PixelCrushersQuestOfferGroup[] _offerGroups = Array.Empty<PixelCrushersQuestOfferGroup>();
    [Tooltip("Fallback title if the selected offer group has no title.")]
    [SerializeField] private string _defaultTitle = "Available Jobs";
    [Tooltip("Runtime panel size. This is a temporary functional UI; visuals can be replaced later.")]
    [SerializeField] private Vector2 _panelSize = new Vector2(620f, 420f);
    [Tooltip("Fullscreen dim color behind the available jobs panel.")]
    [SerializeField] private Color _backdropColor = new Color(0f, 0f, 0f, 0.65f);
    [Tooltip("Panel background color.")]
    [SerializeField] private Color _panelColor = new Color(0.08f, 0.07f, 0.06f, 0.95f);
    [Tooltip("Quest offer button color.")]
    [SerializeField] private Color _buttonColor = new Color(0.26f, 0.2f, 0.12f, 1f);
    [Tooltip("Text color used by the generated offer window.")]
    [SerializeField] private Color _textColor = Color.white;
    [Header("Gameplay Input Lock")]
    [Tooltip("Project UI event channel used to freeze Toris gameplay input while this offer window is open.")]
    [SerializeField] private UIEventsSO _uiEvents;
    [Tooltip("Named gameplay input lock used while this offer window is open.")]
    [SerializeField] private string _gameplayInputLockId = "PixelCrushersQuestOffers";

#if UNITY_EDITOR
    [Tooltip("Logs opened groups and accepted quest offers. Editor only.")]
    [SerializeField] private bool _debugOffers = true;
#endif

    private const int CanvasSortingOrder = 5000;
    private const int DefaultFontSize = 20;
    private const int SmallFontSize = 16;

    private GameObject _root;
    private RectTransform _content;
    private Text _titleText;
    private Text _emptyText;
    private Font _font;
    private PixelCrushersQuestOfferGroup _activeGroup;
    private bool _gameplayInputLocked;

    public void Open(string offerGroupId)
    {
        if (string.IsNullOrWhiteSpace(offerGroupId))
        {
            LogWarning("Cannot open quest offers without an offer group id.");
            return;
        }

        if (!TryFindOfferGroup(offerGroupId, out PixelCrushersQuestOfferGroup group))
        {
            LogWarning($"Quest offer group '{offerGroupId}' was not found.");
            return;
        }

        EnsureWindow();
        _activeGroup = group;
        Populate(group);
        _root.SetActive(true);
        RequestGameplayInputLock();
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        LogDebug($"Opened offer group '{offerGroupId}'.");
    }

    public void Close()
    {
        if (_root != null)
            _root.SetActive(false);

        ReleaseGameplayInputLock();
    }

    private void OnDisable()
    {
        Close();
    }

    private void OnDestroy()
    {
        ReleaseGameplayInputLock();

        if (_root != null)
            Destroy(_root);
    }

    private bool TryFindOfferGroup(string offerGroupId, out PixelCrushersQuestOfferGroup group)
    {
        group = null;

        if (_offerGroups == null)
            return false;

        for (int i = 0; i < _offerGroups.Length; i++)
        {
            PixelCrushersQuestOfferGroup candidate = _offerGroups[i];
            if (candidate == null || !candidate.Matches(offerGroupId))
                continue;

            group = candidate;
            return true;
        }

        return false;
    }

    private void Populate(PixelCrushersQuestOfferGroup group)
    {
        ClearContent();

        _titleText.text = string.IsNullOrWhiteSpace(group.Title) ? _defaultTitle : group.Title;
        bool addedAnyOffer = false;

        if (group.Offers != null)
        {
            for (int i = 0; i < group.Offers.Length; i++)
            {
                PixelCrushersQuestOfferDefinition offer = group.Offers[i];
                if (offer == null || !offer.CanShow())
                    continue;

                CreateOfferButton(offer);
                addedAnyOffer = true;
            }
        }

        _emptyText.text = string.IsNullOrWhiteSpace(group.EmptyText) ? "No jobs available right now." : group.EmptyText;
        _emptyText.gameObject.SetActive(!addedAnyOffer);
        LayoutRebuilder.ForceRebuildLayoutImmediate(_content);
    }

    private void CreateOfferButton(PixelCrushersQuestOfferDefinition offer)
    {
        GameObject buttonObject = new GameObject($"Quest Offer - {offer.QuestName}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObject.transform.SetParent(_content, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = _buttonColor;

        LayoutElement layout = buttonObject.GetComponent<LayoutElement>();
        layout.minHeight = 92f;

        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(() => AcceptOffer(offer));

        GameObject textObject = new GameObject("Text", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(16f, 8f);
        textRect.offsetMax = new Vector2(-16f, -8f);

        Text text = textObject.GetComponent<Text>();
        text.font = _font;
        text.fontSize = SmallFontSize;
        text.color = _textColor;
        text.alignment = TextAnchor.MiddleLeft;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        text.text = BuildOfferText(offer);
    }

    private string BuildOfferText(PixelCrushersQuestOfferDefinition offer)
    {
        string title = QuestLog.GetQuestTitle(offer.QuestName);
        if (string.IsNullOrWhiteSpace(title))
            title = offer.QuestName;

        string description = QuestLog.GetQuestDescription(offer.QuestName);
        if (string.IsNullOrWhiteSpace(description) && offer.EntryNumber > 0)
            description = QuestLog.GetQuestEntry(offer.QuestName, offer.EntryNumber);

        return string.IsNullOrWhiteSpace(description)
            ? title
            : $"{title}\n{description}";
    }

    private void AcceptOffer(PixelCrushersQuestOfferDefinition offer)
    {
        if (offer == null || string.IsNullOrWhiteSpace(offer.QuestName))
            return;

        QuestState currentState = PixelCrushersQuestBridge.GetQuestState(offer.QuestName);
        if (!offer.CanAccept(currentState))
        {
            LogWarning($"Quest '{offer.QuestName}' cannot be accepted from state '{currentState}'.");
            PopulateCurrentGroup();
            return;
        }

        PixelCrushersQuestBridge.SetQuestState(offer.QuestName, QuestState.Active);

        if (offer.EntryNumber > 0)
            PixelCrushersQuestBridge.SetQuestEntryState(offer.QuestName, offer.EntryNumber, QuestState.Active);

        if (!string.IsNullOrWhiteSpace(offer.ProgressVariableName))
            PixelCrushersQuestBridge.SetIntVariable(offer.ProgressVariableName, 0);

        LogDebug($"Accepted quest offer '{offer.QuestName}'.");
        Close();
    }

    private void PopulateCurrentGroup()
    {
        if (_activeGroup != null)
            Populate(_activeGroup);
    }

    private void EnsureWindow()
    {
        if (_root != null)
            return;

        UITools.RequireEventSystem();
        _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        _root = new GameObject("Toris Quest Offer Window", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));

        Canvas canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = CanvasSortingOrder;

        CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        RectTransform rootRect = _root.GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        GameObject backdrop = CreateRect("Backdrop", rootRect, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backdropImage = backdrop.AddComponent<Image>();
        backdropImage.color = _backdropColor;

        GameObject panel = CreateRect("Panel", rootRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), -_panelSize * 0.5f, _panelSize * 0.5f);
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = _panelColor;

        CreateHeader(panel.transform);
        CreateScrollArea(panel.transform);
        CreateCloseButton(panel.transform);

        _root.SetActive(false);
    }

    private void CreateHeader(Transform parent)
    {
        GameObject titleObject = CreateRect("Title", parent, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -72f), new Vector2(-72f, -16f));
        _titleText = titleObject.AddComponent<Text>();
        _titleText.font = _font;
        _titleText.fontSize = DefaultFontSize;
        _titleText.fontStyle = FontStyle.Bold;
        _titleText.color = _textColor;
        _titleText.alignment = TextAnchor.MiddleLeft;
    }

    private void CreateScrollArea(Transform parent)
    {
        GameObject scrollObject = CreateRect("Offer Scroll View", parent, Vector2.zero, Vector2.one, new Vector2(24f, 24f), new Vector2(-24f, -84f));
        ScrollRect scrollRect = scrollObject.AddComponent<ScrollRect>();

        GameObject viewportObject = CreateRect("Viewport", scrollObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image viewportImage = viewportObject.AddComponent<Image>();
        viewportImage.color = new Color(0f, 0f, 0f, 0.08f);
        Mask mask = viewportObject.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        GameObject contentObject = CreateRect("Content", viewportObject.transform, new Vector2(0f, 1f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);
        _content = contentObject.GetComponent<RectTransform>();
        _content.pivot = new Vector2(0.5f, 1f);

        VerticalLayoutGroup layoutGroup = contentObject.AddComponent<VerticalLayoutGroup>();
        layoutGroup.padding = new RectOffset(0, 0, 0, 0);
        layoutGroup.spacing = 10f;
        layoutGroup.childAlignment = TextAnchor.UpperCenter;
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        ContentSizeFitter fitter = contentObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        scrollRect.viewport = viewportObject.GetComponent<RectTransform>();
        scrollRect.content = _content;
        scrollRect.horizontal = false;

        GameObject emptyObject = CreateRect("Empty Text", viewportObject.transform, Vector2.zero, Vector2.one, new Vector2(12f, 12f), new Vector2(-12f, -12f));
        _emptyText = emptyObject.AddComponent<Text>();
        _emptyText.font = _font;
        _emptyText.fontSize = SmallFontSize;
        _emptyText.color = _textColor;
        _emptyText.alignment = TextAnchor.MiddleCenter;
    }

    private void CreateCloseButton(Transform parent)
    {
        GameObject buttonObject = CreateRect("Close Button", parent, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-64f, -64f), new Vector2(-16f, -16f));
        Image image = buttonObject.AddComponent<Image>();
        image.color = _buttonColor;

        Button button = buttonObject.AddComponent<Button>();
        button.onClick.AddListener(Close);

        GameObject labelObject = CreateRect("Label", buttonObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Text label = labelObject.AddComponent<Text>();
        label.font = _font;
        label.fontSize = SmallFontSize;
        label.color = _textColor;
        label.alignment = TextAnchor.MiddleCenter;
        label.text = "X";
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        return gameObject;
    }

    private void ClearContent()
    {
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);
    }

    private void LogDebug(string message)
    {
#if UNITY_EDITOR
        if (_debugOffers)
            Debug.Log($"[PixelCrushersQuestOfferWindow] {message}", this);
#endif
    }

    private void LogWarning(string message)
    {
#if UNITY_EDITOR
        if (_debugOffers)
            Debug.LogWarning($"[PixelCrushersQuestOfferWindow] {message}", this);
#endif
    }

    private void RequestGameplayInputLock()
    {
        if (_uiEvents == null || _gameplayInputLocked || string.IsNullOrWhiteSpace(_gameplayInputLockId))
            return;

        _uiEvents.OnGameplayInputLockRequested?.Invoke(_gameplayInputLockId);
        _gameplayInputLocked = true;
    }

    private void ReleaseGameplayInputLock()
    {
        if (_uiEvents == null || !_gameplayInputLocked || string.IsNullOrWhiteSpace(_gameplayInputLockId))
            return;

        _uiEvents.OnGameplayInputUnlockRequested?.Invoke(_gameplayInputLockId);
        _gameplayInputLocked = false;
    }
}

[Serializable]
public class PixelCrushersQuestOfferGroup
{
    [Tooltip("Dialogue command group id. Example: TorisOpenQuestOffers(\"GuideJobs\").")]
    public string GroupId = "GuideJobs";
    [Tooltip("Title shown at the top of the available jobs window.")]
    public string Title = "Available Jobs";
    [Tooltip("Text shown when this group has no currently available jobs.")]
    public string EmptyText = "No jobs available right now.";
    [Tooltip("Pixel Crushers quests that can be offered in this group.")]
    public PixelCrushersQuestOfferDefinition[] Offers = Array.Empty<PixelCrushersQuestOfferDefinition>();

    public bool Matches(string groupId)
    {
        return !string.IsNullOrWhiteSpace(GroupId)
               && string.Equals(GroupId, groupId, StringComparison.Ordinal);
    }
}

[Serializable]
public class PixelCrushersQuestOfferDefinition
{
    [Tooltip("Pixel Crushers quest name to start when this offer is selected.")]
    public string QuestName = string.Empty;
    [Tooltip("Quest entry/objective number to mark active when this offer is accepted. Use 0 for quests without entries.")]
    public int EntryNumber = 1;
    [Tooltip("Optional Dialogue System variable reset to 0 when the quest is accepted, such as a kill counter.")]
    public string ProgressVariableName = string.Empty;
    [Tooltip("Show this offer while the Pixel Crushers quest is unassigned.")]
    public bool ShowWhenUnassigned = true;
    [Tooltip("Show this offer while the Pixel Crushers quest is grantable.")]
    public bool ShowWhenGrantable = true;
    [Tooltip("Show this offer while already active. Usually false for job boards.")]
    public bool ShowWhenActive = false;
    [Tooltip("Show this offer while waiting for turn-in. Usually false because the NPC route handles turn-in dialogue.")]
    public bool ShowWhenReturnToNpc = false;

    public bool CanShow()
    {
        if (string.IsNullOrWhiteSpace(QuestName))
            return false;

        QuestState state = PixelCrushersQuestBridge.GetQuestState(QuestName);
        return IsStateVisible(state);
    }

    public bool CanAccept(QuestState state)
    {
        return state == QuestState.Unassigned || state == QuestState.Grantable;
    }

    private bool IsStateVisible(QuestState state)
    {
        switch (state)
        {
            case QuestState.Unassigned:
                return ShowWhenUnassigned;
            case QuestState.Grantable:
                return ShowWhenGrantable;
            case QuestState.Active:
                return ShowWhenActive;
            case QuestState.ReturnToNPC:
                return ShowWhenReturnToNpc;
            default:
                return false;
        }
    }
}
