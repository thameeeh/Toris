using System;
using System.Collections.Generic;
using OutlandHaven.UIToolkit;
using UnityEngine;

namespace OutlandHaven.Tutorial
{
    public enum TutorialTrigger
    {
        ScreenOpened = 0,
        SmithOpened = 1,
        InventoryOpened = 2,
        HudReady = 3,
        SkillUnlocked = 4,
        AbilitySlotsUpdated = 5,
        Custom = 100
    }

    public enum TutorialTooltipPlacement
    {
        Auto = 0,
        Left = 1,
        Right = 2,
        Above = 3,
        Below = 4
    }

    public enum TutorialDismissMode
    {
        NextButton = 0,
        ClickHighlighted = 1,
        ClickAnywhere = 2
    }

    [Serializable]
    public class TutorialStepDefinition
    {
        [SerializeField] private string stepId;
        [SerializeField] private TutorialTrigger trigger = TutorialTrigger.ScreenOpened;
        [SerializeField] private ScreenType requiredScreen = ScreenType.None;
        [SerializeField] private string anchorId;
        [SerializeField] private string title;
        [SerializeField, TextArea] private string body;
        [SerializeField] private TutorialTooltipPlacement placement = TutorialTooltipPlacement.Auto;
        [SerializeField] private bool blocksInput = true;
        [SerializeField] private bool pauseGameplay = true;
        [SerializeField] private bool allowHighlightedClick = true;
        [SerializeField] private TutorialDismissMode dismissMode = TutorialDismissMode.NextButton;
        [SerializeField] private string sequenceId;
        [SerializeField] private int sequenceIndex;
        [SerializeField] private int priority;
        [SerializeField] private bool oneShot = true;
        [SerializeField] private string[] prerequisiteStepIds = Array.Empty<string>();

        public string StepId => stepId;
        public TutorialTrigger Trigger => trigger;
        public ScreenType RequiredScreen => requiredScreen;
        public string AnchorId => anchorId;
        public string Title => title;
        public string Body => body;
        public TutorialTooltipPlacement Placement => placement;
        public bool BlocksInput => blocksInput;
        public bool PauseGameplay => pauseGameplay;
        public bool AllowHighlightedClick => allowHighlightedClick;
        public TutorialDismissMode DismissMode => dismissMode;
        public string SequenceId => sequenceId;
        public int SequenceIndex => sequenceIndex;
        public int Priority => priority;
        public bool OneShot => oneShot;
        public IReadOnlyList<string> PrerequisiteStepIds => prerequisiteStepIds;
    }

    [CreateAssetMenu(fileName = "TutorialCatalog", menuName = "Tutorial/Tutorial Catalog")]
    public class TutorialCatalogSO : ScriptableObject
    {
        private const string DefaultResourcePath = "GameData/Tutorial/DefaultTutorialCatalog";

        [SerializeField] private List<TutorialStepDefinition> steps = new List<TutorialStepDefinition>();

        public IReadOnlyList<TutorialStepDefinition> Steps => steps;

        public static TutorialCatalogSO LoadDefault() => Resources.Load<TutorialCatalogSO>(DefaultResourcePath);
    }
}
