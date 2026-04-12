using OutlandHaven.UIToolkit;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace OutlandHaven.Skills
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

    public class PlayerSkillView : GameView, IDisposable
    {
        public override ScreenType ID => ScreenType.Skills;

        // Data dependencies
        private SkillData[] _allSkills;
        private SkillData _currentlySelectedSkill;
        private Dictionary<string, Button> _nodeMap = new Dictionary<string, Button>();

        // UI References - Info Panel
        private Label _infoName;
        private Label _infoDesc;
        private Label _infoCost;
        private Label _infoState;
        private Button _unlockButton;

        private GameSessionSO _gameSession;
        private UISkillEventsSO _uiSkillEvents;

        public PlayerSkillView(VisualElement topElement, UIEventsSO uiEvents, SkillData[] allSkills, GameSessionSO gameSession, UISkillEventsSO uiSkillEvents)
            : base(topElement, uiEvents)
        {
            _allSkills = allSkills;
            _gameSession = gameSession;
            _uiSkillEvents = uiSkillEvents;
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
            // --- Tier 1 ---
            BindNodeToData(m_TopElement.Q<Button>("node_base_jump"), "SKILL_BASE_JUMP");

            // --- Tier 2 ---
            BindNodeToData(m_TopElement.Q<Button>("node_double_jump"), "SKILL_DOUBLE_JUMP");
            BindNodeToData(m_TopElement.Q<Button>("node_dash"), "SKILL_DASH");

            // --- Tier 3 ---
            BindNodeToData(m_TopElement.Q<Button>("node_triple_jump"), "SKILL_TRIPLE_JUMP");
            BindNodeToData(m_TopElement.Q<Button>("node_glide"), "SKILL_GLIDE");
            BindNodeToData(m_TopElement.Q<Button>("node_blink_dash"), "SKILL_BLINK_DASH");

            // --- Unlock Button ---
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

                if (!_nodeMap.ContainsKey(targetSkillID))
                {
                    _nodeMap.Add(targetSkillID, node);
                }
            }
            else
            {
                Debug.LogWarning($"PlayerSkillView: Could not find SkillData for ID {targetSkillID}");
            }
        }

        private void UpdateNodeVisuals(Button node, SkillData skill)
        {
            // Clear all states first to ensure a clean slate
            node.RemoveFromClassList("skill-node--unlocked");
            node.RemoveFromClassList("skill-node--available");
            node.RemoveFromClassList("skill-node--locked");

            // 1. Is it already owned?
            if (_gameSession.PlayerSkills.HasSkill(skill.skillID))
            {
                node.AddToClassList("skill-node--unlocked");
            }
            // 2. Are prerequisites met? (Available to purchase)
            else if (_gameSession.PlayerSkills.ArePrerequisitesMet(skill))
            {
                node.AddToClassList("skill-node--available");
            }
            // 3. Otherwise, it remains strictly locked
            else
            {
                node.AddToClassList("skill-node--locked");
            }
        }

        private void RefreshAllNodes()
        {
            foreach (var kvp in _nodeMap)
            {
                string skillID = kvp.Key;
                Button node = kvp.Value;

                // Find the matching data for this node
                SkillData data = Array.Find(_allSkills, s => s.skillID == skillID);

                if (data != null)
                {
                    UpdateNodeVisuals(node, data);
                }
            }
        }

        private void SelectSkill(SkillData skill)
        {
            _currentlySelectedSkill = skill;

            _infoName.text = skill.skillName;
            _infoDesc.text = skill.description;
            _infoCost.text = $"Cost: {skill.costSP} SP";

            bool isUnlocked = _gameSession.PlayerSkills.HasSkill(skill.skillID);
            bool isAvailable = _gameSession.PlayerSkills.ArePrerequisitesMet(skill);

            if (isUnlocked)
            {
                _infoState.text = "Status: Unlocked";
                _unlockButton.SetEnabled(false); // Already owned
            }
            else if (isAvailable)
            {
                _infoState.text = "Status: Available";
                _unlockButton.SetEnabled(true); // Can be purchased
            }
            else
            {
                _infoState.text = "Status: Locked";
                _unlockButton.SetEnabled(false); // Prerequisites missing
            }
        }

        private void OnUnlockClicked()
        {
            if (_currentlySelectedSkill == null) return;

            _uiSkillEvents.OnRequestUnlock?.Invoke(_currentlySelectedSkill);
        }
        public override void Show()
        {
            base.Show();
            _uiSkillEvents.OnSkillUnlocked += HandleSkillUnlocked;
        }

        public override void Hide()
        {
            base.Hide();
            _uiSkillEvents.OnSkillUnlocked -= HandleSkillUnlocked;
        }

        public override void Setup(object payload)
        {
            base.Setup(payload);

            // Reset the info panel 
            _currentlySelectedSkill = null;
            _infoName.text = "Select a Skill";
            _infoDesc.text = "Hover or click a skill node to see its details here.";
            _infoCost.text = "Cost: -";
            _infoState.text = "Status: -";

            // Evaluate the entire tree based on current save data
            RefreshAllNodes();
        }

        private void HandleSkillUnlocked(string skillID)
        {
            // Refresh the info panel if we are currently looking at the unlocked skill
            if (_currentlySelectedSkill != null && _currentlySelectedSkill.skillID == skillID)
            {
                SelectSkill(_currentlySelectedSkill);
            }

            // Evaluate the entire tree because unlocking this skill 
            // may have made new nodes 'available'
            RefreshAllNodes();
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