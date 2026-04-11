using UnityEngine;
using UnityEngine.UIElements;
using System;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Skills
{
    public class PlayerSkillView : GameView, IDisposable
    {
        public override ScreenType ID => ScreenType.Skills;

        // Data dependencies
        private SkillData[] _allSkills;
        private SkillData _currentlySelectedSkill;

        // UI References - Info Panel
        private Label _infoName;
        private Label _infoDesc;
        private Label _infoCost;
        private Label _infoState;
        private Button _unlockButton;

        public PlayerSkillView(VisualElement topElement, UIEventsSO uiEvents, SkillData[] allSkills)
            : base(topElement, uiEvents)
        {
            _allSkills = allSkills;
        }

        protected override void SetVisualElements()
        {
            // Query the Info Panel elements
            _infoName = m_TopElement.Q<Label>("info-skill-name");
            _infoDesc = m_TopElement.Q<Label>("info-skill-desc");
            _infoCost = m_TopElement.Q<Label>("info-skill-cost");
            _infoState = m_TopElement.Q<Label>("info-skill-state");
            _unlockButton = m_TopElement.Q<Button>("btn-unlock-skill");
        }

        protected override void RegisterButtonCallbacks()
        {
            // For the MVP, we bind specific UXML button names to their SkillData IDs
            BindNodeToData(m_TopElement.Q<Button>("node_double_jump"), "SKILL_DOUBLE_JUMP");
            BindNodeToData(m_TopElement.Q<Button>("node_dash"), "SKILL_DASH");
            // Add more nodes here as you expand the UXML

            if (_unlockButton != null)
            {
                _unlockButton.clicked += OnUnlockClicked;
            }
        }

        private void BindNodeToData(Button node, string targetSkillID)
        {
            if (node == null) return;

            // Find the matching ScriptableObject from our database
            SkillData data = Array.Find(_allSkills, s => s.skillID == targetSkillID);

            if (data != null)
            {
                // When the visual node is clicked, update the info panel
                node.clicked += () => SelectSkill(data);
            }
            else
            {
                Debug.LogWarning($"PlayerSkillView: Could not find SkillData for ID {targetSkillID}");
            }
        }

        private void SelectSkill(SkillData skill)
        {
            _currentlySelectedSkill = skill;

            // Inject the ScriptableObject data into the UXML labels
            _infoName.text = skill.skillName;
            _infoDesc.text = skill.description;
            _infoCost.text = $"Cost: {skill.costSP} SP";

            // TODO: Hook into GameSessionSO to check if the player actually has this skill unlocked
            // bool isUnlocked = _gameSession.PlayerInventory.HasSkill(skill.skillID);
            // _infoState.text = isUnlocked ? "Status: Unlocked" : "Status: Locked";
        }

        private void OnUnlockClicked()
        {
            if (_currentlySelectedSkill == null) return;

            Debug.Log($"Attempting to unlock: {_currentlySelectedSkill.skillName}");
            // TODO: Tell the GameSessionSO/SkillManager to spend SP and unlock the skill
        }

        public override void Setup(object payload)
        {
            base.Setup(payload);

            // 1. Reset the Info Panel 
            _currentlySelectedSkill = null;
            _infoName.text = "Select a Skill";
            _infoDesc.text = "Hover or click a skill node to see its details here.";
            _infoCost.text = "Cost: -";
            _infoState.text = "Status: -";

            // 2. Unpack the payload from the Controller
            if (payload is SkillsPayload stats)
            {
                // Example: Update stat labels in your UXML if you add them later
                // _strengthLabel.text = $"Strength: {stats.Strength}";
                // _agilityLabel.text = $"Agility: {stats.Agility}";
            }
        }

        public void Dispose()
        {
            if (_unlockButton != null)
            {
                _unlockButton.clicked -= OnUnlockClicked;
            }
            // Note: UI Toolkit automatically unregisters clicked events when VisualElements are destroyed,
            // but it's good practice to clean up global event subscriptions here if you add any.
        }
    }
}