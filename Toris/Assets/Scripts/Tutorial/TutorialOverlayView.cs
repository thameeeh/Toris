using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.Tutorial
{
    public sealed class TutorialOverlayView
    {
        private const float CutoutPadding = 8f;
        private const float ScreenPadding = 14f;
        // Includes the larger authored callout footprint so placement keeps it within narrow game views.
        private const float TooltipWidth = 420f;
        private const float TooltipFallbackHeight = 235f;
        private const string NextButtonText = "Next";
        private const string DoneButtonText = "Got it";

        private readonly VisualElement _host;
        private readonly VisualElement _root;
        private readonly VisualElement _topShade;
        private readonly VisualElement _bottomShade;
        private readonly VisualElement _leftShade;
        private readonly VisualElement _rightShade;
        private readonly VisualElement _highlight;
        private readonly VisualElement _tooltip;
        private readonly Label _titleLabel;
        private readonly Label _bodyLabel;
        private readonly Button _nextButton;

        private TutorialStepDefinition _currentStep;
        private bool _hasNextStep;

        public event Action NextRequested;
        public event Action HighlightClicked;
        public event Action DismissRequested;

        public TutorialOverlayView(VisualElement host)
        {
            _host = host;
            _root = new VisualElement { name = "TutorialOverlay" };
            _root.AddToClassList("tutorial-overlay");
            _root.pickingMode = PickingMode.Ignore;

            _topShade = CreateShade("TutorialShadeTop");
            _bottomShade = CreateShade("TutorialShadeBottom");
            _leftShade = CreateShade("TutorialShadeLeft");
            _rightShade = CreateShade("TutorialShadeRight");

            _highlight = new VisualElement { name = "TutorialHighlight" };
            _highlight.AddToClassList("tutorial-overlay__highlight");
            _highlight.pickingMode = PickingMode.Ignore;

            _tooltip = new VisualElement { name = "TutorialTooltip" };
            _tooltip.AddToClassList("tutorial-overlay__tooltip");
            _tooltip.pickingMode = PickingMode.Position;

            _titleLabel = new Label { name = "TutorialTitle" };
            _titleLabel.AddToClassList("tutorial-overlay__title");

            _bodyLabel = new Label { name = "TutorialBody" };
            _bodyLabel.AddToClassList("tutorial-overlay__body");

            _nextButton = new Button(HandleNextButtonClicked) { name = "TutorialNextButton" };
            _nextButton.AddToClassList("standard-button");
            _nextButton.AddToClassList("tutorial-overlay__button");

            _tooltip.Add(_titleLabel);
            _tooltip.Add(_bodyLabel);
            _tooltip.Add(_nextButton);

            _root.Add(_topShade);
            _root.Add(_bottomShade);
            _root.Add(_leftShade);
            _root.Add(_rightShade);
            _root.Add(_highlight);
            _root.Add(_tooltip);
            _host.Add(_root);

            _root.RegisterCallback<ClickEvent>(HandleRootClicked, TrickleDown.TrickleDown);
            Hide();
        }

        public void Show(TutorialStepDefinition step, Rect targetBounds, bool hasNextStep)
        {
            if (step == null)
            {
                Hide();
                return;
            }

            _currentStep = step;
            _hasNextStep = hasNextStep;

            _titleLabel.text = step.Title;
            _bodyLabel.text = step.Body;
            _nextButton.text = hasNextStep ? NextButtonText : DoneButtonText;
            _nextButton.style.display = step.DismissMode == TutorialDismissMode.NextButton
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _root.style.display = DisplayStyle.Flex;
            _root.BringToFront();
            UpdateTargetBounds(targetBounds);
            _root.schedule.Execute(() => UpdateTargetBounds(targetBounds)).ExecuteLater(0);
        }

        public void UpdateTargetBounds(Rect targetBounds)
        {
            if (_root.style.display == DisplayStyle.None)
                return;

            float hostWidth = ResolveDimension(_host.resolvedStyle.width, Screen.width);
            float hostHeight = ResolveDimension(_host.resolvedStyle.height, Screen.height);

            float left = Mathf.Clamp(targetBounds.xMin - CutoutPadding, 0f, hostWidth);
            float top = Mathf.Clamp(targetBounds.yMin - CutoutPadding, 0f, hostHeight);
            float right = Mathf.Clamp(targetBounds.xMax + CutoutPadding, 0f, hostWidth);
            float bottom = Mathf.Clamp(targetBounds.yMax + CutoutPadding, 0f, hostHeight);
            float width = Mathf.Max(0f, right - left);
            float height = Mathf.Max(0f, bottom - top);

            SetRect(_topShade, 0f, 0f, hostWidth, top);
            SetRect(_bottomShade, 0f, bottom, hostWidth, Mathf.Max(0f, hostHeight - bottom));
            SetRect(_leftShade, 0f, top, left, height);
            SetRect(_rightShade, right, top, Mathf.Max(0f, hostWidth - right), height);
            SetRect(_highlight, left, top, width, height);
            PositionTooltip(left, top, right, bottom, hostWidth, hostHeight);
        }

        public void Hide()
        {
            _root.style.display = DisplayStyle.None;
            _currentStep = null;
            _hasNextStep = false;
        }

        private static VisualElement CreateShade(string name)
        {
            VisualElement shade = new VisualElement { name = name };
            shade.AddToClassList("tutorial-overlay__shade");
            shade.pickingMode = PickingMode.Position;
            return shade;
        }

        private void PositionTooltip(float left, float top, float right, float bottom, float hostWidth, float hostHeight)
        {
            float tooltipHeight = ResolveDimension(_tooltip.resolvedStyle.height, TooltipFallbackHeight);
            float tooltipLeft;
            float tooltipTop;

            switch (_currentStep.Placement)
            {
                case TutorialTooltipPlacement.Left:
                    tooltipLeft = left - TooltipWidth - ScreenPadding;
                    tooltipTop = top;
                    break;
                case TutorialTooltipPlacement.Right:
                    tooltipLeft = right + ScreenPadding;
                    tooltipTop = top;
                    break;
                case TutorialTooltipPlacement.Above:
                    tooltipLeft = left;
                    tooltipTop = top - tooltipHeight - ScreenPadding;
                    break;
                case TutorialTooltipPlacement.Below:
                    tooltipLeft = left;
                    tooltipTop = bottom + ScreenPadding;
                    break;
                default:
                    tooltipLeft = right + ScreenPadding;
                    tooltipTop = top;
                    if (tooltipLeft + TooltipWidth + ScreenPadding > hostWidth)
                        tooltipLeft = left - TooltipWidth - ScreenPadding;
                    if (tooltipLeft < ScreenPadding)
                        tooltipLeft = ScreenPadding;
                    if (tooltipTop + tooltipHeight + ScreenPadding > hostHeight)
                        tooltipTop = Mathf.Max(ScreenPadding, hostHeight - tooltipHeight - ScreenPadding);
                    break;
            }

            tooltipLeft = Mathf.Clamp(tooltipLeft, ScreenPadding, Mathf.Max(ScreenPadding, hostWidth - TooltipWidth - ScreenPadding));
            tooltipTop = Mathf.Clamp(tooltipTop, ScreenPadding, Mathf.Max(ScreenPadding, hostHeight - tooltipHeight - ScreenPadding));
            _tooltip.style.left = tooltipLeft;
            _tooltip.style.top = tooltipTop;
        }

        private void HandleNextButtonClicked()
        {
            if (_hasNextStep)
                NextRequested?.Invoke();
            else
                DismissRequested?.Invoke();
        }

        private void HandleRootClicked(ClickEvent evt)
        {
            if (_currentStep == null)
                return;

            if (evt.target == _topShade
                || evt.target == _bottomShade
                || evt.target == _leftShade
                || evt.target == _rightShade)
            {
                if (_currentStep.DismissMode == TutorialDismissMode.ClickAnywhere)
                    DismissRequested?.Invoke();
                return;
            }

            if (_currentStep.DismissMode == TutorialDismissMode.ClickHighlighted)
                HighlightClicked?.Invoke();
        }

        private static void SetRect(VisualElement element, float left, float top, float width, float height)
        {
            element.style.left = left;
            element.style.top = top;
            element.style.width = width;
            element.style.height = height;
        }

        private static float ResolveDimension(float value, float fallback)
        {
            return float.IsNaN(value) || value <= 0f ? fallback : value;
        }
    }
}
