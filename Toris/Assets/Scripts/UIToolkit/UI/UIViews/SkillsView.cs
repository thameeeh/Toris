using UnityEngine.UIElements;

namespace OutlandHaven.UIToolkit
{
    public struct SkillsPayload
    {
        public int Strength;
        public float StrengthXpPercentage;
        public int Agility;
        public float AgilityXpPercentage;
        public int Intelligence;
        public float IntelligenceXpPercentage;
    }

    public class SkillsView : GameView
    {
        public override ScreenType ID => ScreenType.Skills;

        private Label _lblStrength;
        private Label _lblAgility;
        private Label _lblIntelligence;
        private ProgressBar _pbStrengthXp;
        private ProgressBar _pbAgilityXp;
        private ProgressBar _pbIntelligenceXp;
        private Button _btnClose;

        public SkillsView(VisualElement topElement, UIEventsSO uiEvents) : base(topElement, uiEvents)
        {
        }

        protected override void SetVisualElements()
        {
            _lblStrength = m_TopElement.Q<Label>("lbl-strength");
            _lblAgility = m_TopElement.Q<Label>("lbl-agility");
            _lblIntelligence = m_TopElement.Q<Label>("lbl-intelligence");

            _pbStrengthXp = m_TopElement.Q<ProgressBar>("pb-strength-xp");
            _pbAgilityXp = m_TopElement.Q<ProgressBar>("pb-agility-xp");
            _pbIntelligenceXp = m_TopElement.Q<ProgressBar>("pb-intelligence-xp");

            _btnClose = m_TopElement.Q<Button>("btn-close");
        }

        protected override void RegisterButtonCallbacks()
        {
            if (_btnClose != null)
            {
                _btnClose.RegisterCallback<ClickEvent>(OnCloseClicked);
            }
        }

        private void OnCloseClicked(ClickEvent evt)
        {
            UIEvents.OnRequestClose?.Invoke(ID);
        }

        public override void Setup(object payload)
        {
            base.Setup(payload);

            if (payload is SkillsPayload data)
            {
                if (_lblStrength != null) _lblStrength.text = $"Strength: {data.Strength}";
                if (_lblAgility != null) _lblAgility.text = $"Agility: {data.Agility}";
                if (_lblIntelligence != null) _lblIntelligence.text = $"Intelligence: {data.Intelligence}";

                if (_pbStrengthXp != null) _pbStrengthXp.value = data.StrengthXpPercentage;
                if (_pbAgilityXp != null) _pbAgilityXp.value = data.AgilityXpPercentage;
                if (_pbIntelligenceXp != null) _pbIntelligenceXp.value = data.IntelligenceXpPercentage;
            }
        }
    }
}
