using System;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    public enum PrologueStoryCompletionMode
    {
        RevealGameplay = 0,
        HoldBlack = 1
    }

    [Serializable]
    public sealed class PrologueStoryCard
    {
        [SerializeField] private string title;
        [SerializeField, TextArea(2, 5)] private string body;
        [SerializeField] private Sprite background;
        [SerializeField] private string continueText = "Continue";

        public PrologueStoryCard()
        {
        }

        public PrologueStoryCard(string title, string body, string continueText = "Continue")
        {
            this.title = title;
            this.body = body;
            this.continueText = continueText;
        }

        public string Title => title;
        public string Body => body;
        public Sprite Background => background;
        public string ContinueText => continueText;
    }

    [DefaultExecutionOrder(200)]
    public sealed class PrologueStorySequenceController : MonoBehaviour
    {
        private const string GameplayInputLockId = "PrologueStory";
        private const string DefaultUiEventsResourcePath = "GameData/SOForEvents/UI Events SO";

        private static readonly PrologueStoryCard[] DefaultCards =
        {
            new PrologueStoryCard(
                "The Road Behind",
                "The road behind you is gone now. Whatever life you had before the border, it belongs to someone else."),
            new PrologueStoryCard(
                "The Wilds Ahead",
                "Out here, the wilds are not empty. Things that should be dead still move, and things that still breathe learn to hide."),
            new PrologueStoryCard(
                "Safe Haven",
                "Somewhere ahead is Safe Haven, a place built by people who had nowhere else to go. Reach it, and you might last the night.",
                "Begin")
        };

        [Header("UI")]
        [SerializeField] private UIDocument uiDocument;
        [SerializeField] private UIEventsSO uiEvents;
        [SerializeField] private StyleSheet storyStyleSheet;

        [Header("Playback")]
        [SerializeField] private bool playOnStart = true;
        [SerializeField] private bool pauseGameplay;
        [SerializeField, Min(0f)] private float cardFadeSeconds = 0.35f;
        [SerializeField, Min(0f)] private float finalImageFadeSeconds = 1f;
        [SerializeField, Min(0f)] private float gameplayRevealFadeSeconds = 0.75f;
        [SerializeField, Min(0f)] private float initialGameplayFadeSeconds;
        [SerializeField] private PrologueStoryCompletionMode completionMode = PrologueStoryCompletionMode.RevealGameplay;
        [SerializeField] private PrologueStoryCard[] storyCards = Array.Empty<PrologueStoryCard>();
        [SerializeField] private UnityEvent onSequenceCompleted = new UnityEvent();

        private PrologueStoryView _view;
        private int _currentCardIndex;
        private bool _isPlaying;
        private bool _advanceQueued;
        private bool _timeScalePaused;
        private float _previousTimeScale = 1f;

        public event Action SequenceCompleted;

        private void Start()
        {
            ResolveDependencies();

            if (playOnStart)
                Begin();
        }

        private void OnDisable()
        {
            End(releaseOnly: true);
        }

        public void Begin()
        {
            TryBegin();
        }

        public bool TryBegin()
        {
            if (_isPlaying)
                return false;

            PrologueStoryCard[] cards = ResolveCards();
            if (cards.Length == 0)
                return false;

            VisualElement host = ResolveHost();
            if (host == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning("[PrologueStorySequenceController] No UI host was available for the story intro.", this);
#endif
                return false;
            }

            _view ??= new PrologueStoryView(
                host,
                storyStyleSheet,
                cardFadeSeconds,
                finalImageFadeSeconds,
                gameplayRevealFadeSeconds);
            _view.AdvanceRequested += HandleAdvanceRequested;

            _currentCardIndex = 0;
            _isPlaying = true;
            _advanceQueued = initialGameplayFadeSeconds > 0f;
            LockGameplay();

            if (_advanceQueued)
                _view.ShowFirstCardFromGameplay(
                    cards[_currentCardIndex],
                    _currentCardIndex,
                    cards.Length,
                    initialGameplayFadeSeconds,
                    HandleCardShown);
            else
                ShowCurrentCard(cards, instant: true);

            return true;
        }

        private void HandleAdvanceRequested()
        {
            if (_advanceQueued)
                return;

            _advanceQueued = true;
            PrologueStoryCard[] cards = ResolveCards();
            _currentCardIndex++;

            if (_currentCardIndex >= cards.Length)
            {
                if (_view != null)
                    _view.PlayOutro(
                        completionMode == PrologueStoryCompletionMode.RevealGameplay,
                        HandleOutroFinished);
                else
                    HandleOutroFinished();

                return;
            }

            ShowCurrentCard(cards, instant: false);
        }

        private void ShowCurrentCard(PrologueStoryCard[] cards, bool instant)
        {
            _view?.Show(cards[_currentCardIndex], _currentCardIndex, cards.Length, instant, HandleCardShown);
        }

        private void HandleCardShown()
        {
            _advanceQueued = false;
        }

        private void HandleOutroFinished()
        {
            CompleteSequence();
        }

        private void End(bool releaseOnly)
        {
            FinishPlayback(hideView: true, invokeCompletion: false, releaseOnly: releaseOnly);
        }

        private void CompleteSequence()
        {
            FinishPlayback(
                hideView: completionMode == PrologueStoryCompletionMode.RevealGameplay,
                invokeCompletion: true,
                releaseOnly: false);
        }

        private void FinishPlayback(bool hideView, bool invokeCompletion, bool releaseOnly)
        {
            if (_view != null)
            {
                _view.AdvanceRequested -= HandleAdvanceRequested;
                if (hideView)
                    _view.Hide();
            }

            if (_isPlaying || releaseOnly)
                UnlockGameplay();

            _isPlaying = false;
            _advanceQueued = false;
            _currentCardIndex = 0;

            if (!invokeCompletion)
                return;

            SequenceCompleted?.Invoke();
            onSequenceCompleted?.Invoke();
        }

        private PrologueStoryCard[] ResolveCards()
        {
            return storyCards != null && storyCards.Length > 0
                ? storyCards
                : DefaultCards;
        }

        private VisualElement ResolveHost()
        {
            if (uiDocument == null || uiDocument.rootVisualElement == null)
                return null;

            return uiDocument.rootVisualElement;
        }

        private void ResolveDependencies()
        {
            if (uiDocument == null)
            {
                UIManager manager = FindFirstObjectByType<UIManager>();
                if (manager != null)
                    manager.TryGetComponent(out uiDocument);
            }

            if (uiEvents == null)
                uiEvents = Resources.Load<UIEventsSO>(DefaultUiEventsResourcePath);
        }

        private void LockGameplay()
        {
            uiEvents?.OnGameplayInputLockRequested?.Invoke(GameplayInputLockId);

            if (!pauseGameplay || _timeScalePaused)
                return;

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _timeScalePaused = true;
        }

        private void UnlockGameplay()
        {
            uiEvents?.OnGameplayInputUnlockRequested?.Invoke(GameplayInputLockId);

            if (!_timeScalePaused)
                return;

            Time.timeScale = _previousTimeScale;
            _timeScalePaused = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (storyCards == null)
                storyCards = Array.Empty<PrologueStoryCard>();
        }
#endif
    }

    internal sealed class PrologueStoryView
    {
        private readonly VisualElement _root;
        private readonly VisualElement _cardLayer;
        private readonly VisualElement _background;
        private readonly Label _titleLabel;
        private readonly Label _bodyLabel;
        private readonly Label _advanceLabel;
        private readonly float _cardFadeSeconds;
        private readonly float _finalImageFadeSeconds;
        private readonly float _gameplayRevealFadeSeconds;
        private readonly int _fadeStepMilliseconds;
        private int _transitionVersion;

        public event Action AdvanceRequested;

        public PrologueStoryView(
            VisualElement host,
            StyleSheet styleSheet,
            float cardFadeSeconds,
            float finalImageFadeSeconds,
            float gameplayRevealFadeSeconds)
        {
            if (styleSheet != null && !host.styleSheets.Contains(styleSheet))
                host.styleSheets.Add(styleSheet);

            _cardFadeSeconds = Mathf.Max(0f, cardFadeSeconds);
            _finalImageFadeSeconds = Mathf.Max(0f, finalImageFadeSeconds);
            _gameplayRevealFadeSeconds = Mathf.Max(0f, gameplayRevealFadeSeconds);
            _fadeStepMilliseconds = 16;

            _root = new VisualElement { name = "PrologueStoryRoot", focusable = true };
            _root.AddToClassList("prologue-story");
            _root.RegisterCallback<KeyDownEvent>(HandleKeyDown);
            _root.RegisterCallback<ClickEvent>(HandleRootClicked);

            _cardLayer = new VisualElement { name = "PrologueStoryCardLayer" };
            _cardLayer.AddToClassList("prologue-story__card-layer");
            _root.Add(_cardLayer);

            _background = new VisualElement { name = "PrologueStoryBackground" };
            _background.AddToClassList("prologue-story__background");
            _cardLayer.Add(_background);

            VisualElement dimmer = new VisualElement { name = "PrologueStoryDimmer" };
            dimmer.AddToClassList("prologue-story__dimmer");
            _cardLayer.Add(dimmer);

            VisualElement content = new VisualElement { name = "PrologueStoryContent" };
            content.AddToClassList("prologue-story__content");

            _titleLabel = new Label { name = "PrologueStoryTitle" };
            _titleLabel.AddToClassList("prologue-story__title");
            content.Add(_titleLabel);

            _bodyLabel = new Label { name = "PrologueStoryBody" };
            _bodyLabel.AddToClassList("prologue-story__body");
            content.Add(_bodyLabel);

            VisualElement prompt = new VisualElement { name = "PrologueStoryPrompt" };
            prompt.AddToClassList("prologue-story__prompt");

            _advanceLabel = new Label { name = "PrologueStoryAdvanceLabel" };
            _advanceLabel.AddToClassList("prologue-story__prompt-text");
            prompt.Add(_advanceLabel);

            Label keycap = new Label { name = "PrologueStoryKeycap", text = "Space" };
            keycap.AddToClassList("prologue-story__keycap");
            prompt.Add(keycap);

            content.Add(prompt);

            _cardLayer.Add(content);
            host.Add(_root);
            Hide();
        }

        public void Show(PrologueStoryCard card, int index, int totalCount, bool instant, Action shownCallback)
        {
            if (card == null)
                return;

            if (instant || _cardFadeSeconds <= 0f || _root.resolvedStyle.display == DisplayStyle.None)
            {
                _transitionVersion++;
                ApplyCard(card, index, totalCount);
                _root.style.opacity = 1f;
                _cardLayer.style.opacity = 1f;
                _root.style.display = DisplayStyle.Flex;
                _root.BringToFront();
                _root.Focus();
                shownCallback?.Invoke();
                return;
            }

            int transitionVersion = ++_transitionVersion;
            FadeTo(_cardLayer, 0f, _cardFadeSeconds, transitionVersion, () =>
            {
                ApplyCard(card, index, totalCount);
                FadeTo(_cardLayer, 1f, _cardFadeSeconds, transitionVersion, shownCallback);
            });
        }

        public void ShowFirstCardFromGameplay(
            PrologueStoryCard card,
            int index,
            int totalCount,
            float gameplayFadeSeconds,
            Action shownCallback)
        {
            if (card == null)
                return;

            int transitionVersion = ++_transitionVersion;
            ApplyCard(card, index, totalCount);
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 0f;
            _cardLayer.style.opacity = 0f;
            _root.BringToFront();
            _root.Focus();

            FadeTo(_root, 1f, gameplayFadeSeconds, transitionVersion, () =>
            {
                FadeTo(_cardLayer, 1f, _cardFadeSeconds, transitionVersion, shownCallback);
            });
        }

        public void PlayOutro(bool revealGameplay, Action completed)
        {
            int transitionVersion = ++_transitionVersion;
            _root.style.display = DisplayStyle.Flex;
            _root.style.opacity = 1f;
            _root.BringToFront();
            _root.Focus();

            FadeTo(_cardLayer, 0f, _finalImageFadeSeconds, transitionVersion, () =>
            {
                if (revealGameplay)
                    FadeTo(_root, 0f, _gameplayRevealFadeSeconds, transitionVersion, completed);
                else
                    completed?.Invoke();
            });
        }

        public void Hide()
        {
            _transitionVersion++;
            _root.style.display = DisplayStyle.None;
            _root.style.opacity = 1f;
            _cardLayer.style.opacity = 0f;
        }

        private void ApplyCard(PrologueStoryCard card, int index, int totalCount)
        {
            _titleLabel.text = string.IsNullOrWhiteSpace(card.Title) ? "Prologue" : card.Title;
            _bodyLabel.text = card.Body ?? string.Empty;

            bool isLastCard = index >= totalCount - 1;
            _advanceLabel.text = string.IsNullOrWhiteSpace(card.ContinueText)
                ? (isLastCard ? "Begin" : "Continue")
                : card.ContinueText.Trim();

            if (card.Background != null)
                _background.style.backgroundImage = new StyleBackground(card.Background);
            else
                _background.style.backgroundImage = new StyleBackground(StyleKeyword.None);
        }

        private void HandleRootClicked(ClickEvent evt)
        {
            evt.StopPropagation();
            AdvanceRequested?.Invoke();
        }

        private void HandleKeyDown(KeyDownEvent evt)
        {
            if (evt.keyCode != KeyCode.Return
                && evt.keyCode != KeyCode.KeypadEnter
                && evt.keyCode != KeyCode.Space)
            {
                return;
            }

            evt.StopPropagation();
            AdvanceRequested?.Invoke();
        }

        private void FadeTo(
            VisualElement element,
            float targetOpacity,
            float durationSeconds,
            int transitionVersion,
            Action completed)
        {
            float startOpacity = element.resolvedStyle.opacity;
            float startTime = Time.realtimeSinceStartup;
            float duration = Mathf.Max(0.001f, durationSeconds);

            _root.style.display = DisplayStyle.Flex;
            _root.schedule.Execute(() => TickFade(element, startOpacity, targetOpacity, startTime, duration, transitionVersion, completed))
                .ExecuteLater(_fadeStepMilliseconds);
        }

        private void TickFade(
            VisualElement element,
            float startOpacity,
            float targetOpacity,
            float startTime,
            float duration,
            int transitionVersion,
            Action completed)
        {
            if (transitionVersion != _transitionVersion)
                return;

            float t = Mathf.Clamp01((Time.realtimeSinceStartup - startTime) / duration);
            float eased = Mathf.SmoothStep(0f, 1f, t);
            element.style.opacity = Mathf.Lerp(startOpacity, targetOpacity, eased);

            if (t >= 1f)
            {
                completed?.Invoke();
                return;
            }

            _root.schedule.Execute(() => TickFade(element, startOpacity, targetOpacity, startTime, duration, transitionVersion, completed))
                .ExecuteLater(_fadeStepMilliseconds);
        }
    }
}
