using System;
using System.Collections.Generic;
using OutlandHaven.UIToolkit;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    public sealed class TutorialRuntimeController
    {
        private const string GameplayInputLockId = "Tutorial";
        private const int MaxAnchorResolveAttempts = 8;
        private const int AnchorResolveRetryMilliseconds = 50;
#if UNITY_EDITOR
        private const bool EnableEditorDiagnostics = true;
#endif

        private readonly VisualElement _host;
        private readonly UIEventsSO _uiEvents;
        private readonly GameSessionSO _gameSession;
        private readonly TutorialCatalogSO _catalog;
        private readonly TutorialOverlayView _overlayView;
        private readonly List<TutorialStepDefinition> _activeSequence = new List<TutorialStepDefinition>();
        private readonly HashSet<ScreenType> _queuedScreens = new HashSet<ScreenType>();
        private VisualElement _highlightClickAnchor;

        private int _activeSequenceIndex;
        private int _anchorResolveAttempts;
        private ScreenType _activeScreen = ScreenType.None;
        private bool _eventsBound;
        private bool _isShowing;
        private bool _timeScalePaused;
        private float _previousTimeScale = 1f;

        public TutorialRuntimeController(
            VisualElement host,
            UIEventsSO uiEvents,
            GameSessionSO gameSession,
            TutorialCatalogSO catalog)
        {
            _host = host;
            _uiEvents = uiEvents;
            _gameSession = gameSession;
            _catalog = catalog;
            _overlayView = host != null ? new TutorialOverlayView(host) : null;

            if (_overlayView != null)
            {
                _overlayView.NextRequested += HandleNextRequested;
                _overlayView.DismissRequested += HandleDismissRequested;
                _overlayView.HighlightClicked += HandleHighlightedClicked;
            }
        }

        public void Bind()
        {
            if (_eventsBound || _uiEvents == null)
                return;

            _uiEvents.OnRequestOpen += HandleRequestOpen;
            _uiEvents.OnScreenOpen += HandleScreenOpen;
            _uiEvents.OnScreenClose += HandleScreenClose;
            _eventsBound = true;
        }

        public void Unbind()
        {
            if (!_eventsBound || _uiEvents == null)
                return;

            _uiEvents.OnRequestOpen -= HandleRequestOpen;
            _uiEvents.OnScreenOpen -= HandleScreenOpen;
            _uiEvents.OnScreenClose -= HandleScreenClose;
            _eventsBound = false;
            HideActiveTutorial(markCurrentStepComplete: false);
        }

        private void HandleRequestOpen(ScreenType screenType, object payload)
        {
            // Request-open is an early signal used only as a fallback; visible anchors are still validated later.
            if (_isShowing)
                return;

            QueueScreenTrigger(screenType);
        }

        private void HandleScreenOpen(ScreenType screenType)
        {
            if (_isShowing)
                return;

            QueueScreenTrigger(screenType);
        }

        private void QueueScreenTrigger(ScreenType screenType)
        {
            TutorialTrigger trigger = ResolveScreenTrigger(screenType);
            if (trigger == TutorialTrigger.Custom)
                return;

            if (_host == null)
            {
                LogEditorDiagnostic(screenType, "No UI root was available for the tutorial overlay.");
                return;
            }

            if (!_queuedScreens.Add(screenType))
                return;

            _anchorResolveAttempts = 0;
            _host.schedule.Execute(() =>
            {
                _queuedScreens.Remove(screenType);
                TryStartTutorial(trigger, screenType);
            }).ExecuteLater(0);
        }

        private void HandleScreenClose(ScreenType screenType)
        {
            if (!_isShowing || screenType != _activeScreen)
                return;

            HideActiveTutorial(markCurrentStepComplete: false);
        }

        private void TryStartTutorial(TutorialTrigger trigger, ScreenType screenType)
        {
            if (_isShowing)
                return;

            if (_catalog == null)
            {
                LogEditorDiagnostic(screenType, "No TutorialCatalogSO was loaded.");
                return;
            }

            if (_gameSession == null)
            {
                LogEditorDiagnostic(screenType, "No GameSessionSO was loaded.");
                return;
            }

            if (!_gameSession.TutorialsEnabled)
            {
                LogEditorDiagnostic(screenType, "Tutorials are disabled for the active session.");
                return;
            }

            BuildEligibleSequence(trigger, screenType);
            if (_activeSequence.Count == 0)
            {
                LogEditorDiagnostic(screenType, $"No eligible tutorial steps for trigger '{trigger}' on screen '{screenType}'.");
                return;
            }

            _activeSequenceIndex = 0;
            _activeScreen = screenType;
            TryShowCurrentStep(trigger, screenType);
        }

        private void TryShowCurrentStep(TutorialTrigger trigger, ScreenType screenType)
        {
            if (_activeSequenceIndex < 0 || _activeSequenceIndex >= _activeSequence.Count)
            {
                HideActiveTutorial(markCurrentStepComplete: false);
                return;
            }

            TutorialStepDefinition step = _activeSequence[_activeSequenceIndex];
            if (!TutorialAnchorRegistry.TryGetVisibleBounds(step.AnchorId, out Rect anchorBounds))
            {
                _anchorResolveAttempts++;
                if (_anchorResolveAttempts <= MaxAnchorResolveAttempts)
                    _host?.schedule.Execute(() => TryShowCurrentStep(trigger, screenType)).ExecuteLater(AnchorResolveRetryMilliseconds);
                else
                {
                    LogEditorDiagnostic(screenType, $"Could not resolve visible tutorial anchor '{step.AnchorId}' for step '{step.StepId}'.");
                    HideActiveTutorial(markCurrentStepComplete: false);
                }
                return;
            }

            _anchorResolveAttempts = 0;
            _isShowing = true;
            ApplyPauseAndInputLock(step);
            BindHighlightedClickIfNeeded(step);
            LogEditorDiagnostic(screenType, $"Showing tutorial step '{step.StepId}' on anchor '{step.AnchorId}'.");
            _overlayView?.Show(step, anchorBounds, _activeSequenceIndex < _activeSequence.Count - 1);
        }

        private void BuildEligibleSequence(TutorialTrigger trigger, ScreenType screenType)
        {
            _activeSequence.Clear();

            IReadOnlyList<TutorialStepDefinition> steps = _catalog.Steps;
            if (steps == null)
                return;

            List<TutorialStepDefinition> eligibleSteps = new List<TutorialStepDefinition>();
            for (int i = 0; i < steps.Count; i++)
            {
                TutorialStepDefinition step = steps[i];
                if (!IsStepEligible(step, trigger, screenType))
                    continue;

                eligibleSteps.Add(step);
            }

            if (eligibleSteps.Count == 0)
                return;

            eligibleSteps.Sort(CompareSteps);
            string sequenceId = eligibleSteps[0].SequenceId;

            for (int i = 0; i < eligibleSteps.Count; i++)
            {
                TutorialStepDefinition step = eligibleSteps[i];
                if (string.Equals(step.SequenceId, sequenceId, StringComparison.Ordinal))
                {
                    _activeSequence.Add(step);
                }
            }

            _activeSequence.Sort(CompareSteps);
        }

        private bool IsStepEligible(TutorialStepDefinition step, TutorialTrigger trigger, ScreenType screenType)
        {
            if (step == null)
                return false;

            if (step.Trigger != trigger && step.Trigger != TutorialTrigger.ScreenOpened)
                return false;

            if (step.RequiredScreen != ScreenType.None && step.RequiredScreen != screenType)
                return false;

            if (string.IsNullOrWhiteSpace(step.StepId) || string.IsNullOrWhiteSpace(step.AnchorId))
                return false;

            if (step.OneShot && _gameSession.IsTutorialStepCompleted(step.StepId))
                return false;

            IReadOnlyList<string> prerequisites = step.PrerequisiteStepIds;
            if (prerequisites == null)
                return true;

            for (int i = 0; i < prerequisites.Count; i++)
            {
                string prerequisiteId = prerequisites[i];
                if (!string.IsNullOrWhiteSpace(prerequisiteId)
                    && !_gameSession.IsTutorialStepCompleted(prerequisiteId))
                {
                    return false;
                }
            }

            return true;
        }

        private static int CompareSteps(TutorialStepDefinition left, TutorialStepDefinition right)
        {
            int priorityCompare = left.Priority.CompareTo(right.Priority);
            if (priorityCompare != 0)
                return priorityCompare;

            int sequenceCompare = string.Compare(left.SequenceId, right.SequenceId, StringComparison.Ordinal);
            if (sequenceCompare != 0)
                return sequenceCompare;

            return left.SequenceIndex.CompareTo(right.SequenceIndex);
        }

        private void HandleNextRequested()
        {
            UnbindHighlightedClickAnchor();
            CompleteCurrentStep();
            _activeSequenceIndex++;

            if (_activeSequenceIndex >= _activeSequence.Count)
            {
                HideActiveTutorial(markCurrentStepComplete: false);
                return;
            }

            TutorialStepDefinition step = _activeSequence[_activeSequenceIndex];
            ReleasePauseAndInputLock();
            ApplyPauseAndInputLock(step);
            TryShowCurrentStep(step.Trigger, step.RequiredScreen);
        }

        private void HandleDismissRequested()
        {
            HideActiveTutorial(markCurrentStepComplete: true);
        }

        private void HandleHighlightedClicked()
        {
            HideActiveTutorial(markCurrentStepComplete: true);
        }

        private void CompleteCurrentStep()
        {
            if (_gameSession == null || _activeSequenceIndex < 0 || _activeSequenceIndex >= _activeSequence.Count)
                return;

            _gameSession.MarkTutorialStepCompleted(_activeSequence[_activeSequenceIndex].StepId);
        }

        private void HideActiveTutorial(bool markCurrentStepComplete)
        {
            if (markCurrentStepComplete)
                CompleteCurrentStep();

            UnbindHighlightedClickAnchor();
            _overlayView?.Hide();
            ReleasePauseAndInputLock();
            _activeSequence.Clear();
            _activeSequenceIndex = 0;
            _activeScreen = ScreenType.None;
            _isShowing = false;
        }

        private void BindHighlightedClickIfNeeded(TutorialStepDefinition step)
        {
            UnbindHighlightedClickAnchor();

            if (step == null || step.DismissMode != TutorialDismissMode.ClickHighlighted)
                return;

            if (!TutorialAnchorRegistry.TryGetElement(step.AnchorId, out VisualElement anchor))
                return;

            _highlightClickAnchor = anchor;
            _highlightClickAnchor.RegisterCallback<ClickEvent>(HandleAnchorClicked);
        }

        private void UnbindHighlightedClickAnchor()
        {
            if (_highlightClickAnchor == null)
                return;

            _highlightClickAnchor.UnregisterCallback<ClickEvent>(HandleAnchorClicked);
            _highlightClickAnchor = null;
        }

        private void HandleAnchorClicked(ClickEvent evt)
        {
            HandleHighlightedClicked();
        }

        private void ApplyPauseAndInputLock(TutorialStepDefinition step)
        {
            if (step == null)
                return;

            if (step.BlocksInput)
                _uiEvents?.OnGameplayInputLockRequested?.Invoke(GameplayInputLockId);

            if (!step.PauseGameplay || _timeScalePaused)
                return;

            _previousTimeScale = Time.timeScale;
            Time.timeScale = 0f;
            _timeScalePaused = true;
        }

        private void ReleasePauseAndInputLock()
        {
            _uiEvents?.OnGameplayInputUnlockRequested?.Invoke(GameplayInputLockId);

            if (!_timeScalePaused)
                return;

            Time.timeScale = _previousTimeScale;
            _timeScalePaused = false;
        }

        private static TutorialTrigger ResolveScreenTrigger(ScreenType screenType)
        {
            switch (screenType)
            {
                case ScreenType.Smith:
                    return TutorialTrigger.SmithOpened;
                case ScreenType.Inventory:
                    return TutorialTrigger.InventoryOpened;
                case ScreenType.HUD:
                    return TutorialTrigger.HudReady;
                default:
                    return TutorialTrigger.ScreenOpened;
            }
        }

        private static void LogEditorDiagnostic(ScreenType screenType, string message)
        {
#if UNITY_EDITOR
            if (EnableEditorDiagnostics && screenType == ScreenType.Smith)
            {
                Debug.Log($"[TutorialRuntimeController] {message}");
            }
#endif
        }
    }
}
