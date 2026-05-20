using OutlandHaven.UIToolkit;
using System;
using System.Collections.Generic;
using System.Linq;
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
        private SkillCategory _currentCategory = SkillCategory.Player;
        private Dictionary<string, Button> _nodeMap = new Dictionary<string, Button>();

        // UI References - Left Panel
        private Button _tabBtnPlayer;
        private Button _tabBtnWeapon;
        private VisualElement _gridContainer;

        // UI References - Right Panel (Info)
        private Label _infoName;
        private Label _infoDesc;
        private Label _infoCost;
        private Label _infoState;
        private Button _unlockButton;
        private VisualElement _videoContainer;

        private GameSessionSO _gameSession;
        private UISkillEventsSO _uiSkillEvents;

        public PlayerSkillView(
            VisualElement topElement, 
            UIEventsSO uiEvents, 
            SkillData[] allSkills, 
            GameSessionSO gameSession, 
            UISkillEventsSO uiSkillEvents)
            : base(topElement, uiEvents)
        {
            _allSkills = allSkills;
            _gameSession = gameSession;
            _uiSkillEvents = uiSkillEvents;
        }

        protected override void SetVisualElements()
        {
            // Left Panel
            _tabBtnPlayer = m_TopElement.Q<Button>("tab-btn-player");
            _tabBtnWeapon = m_TopElement.Q<Button>("tab-btn-weapon");
            _gridContainer = m_TopElement.Q<VisualElement>("skill-grid-container");

            // Right Panel
            _infoName = m_TopElement.Q<Label>("info-skill-name");
            _infoDesc = m_TopElement.Q<Label>("info-skill-desc");
            _infoCost = m_TopElement.Q<Label>("info-skill-cost");
            _infoState = m_TopElement.Q<Label>("info-skill-state");
            _unlockButton = m_TopElement.Q<Button>("btn-unlock-skill");
            _videoContainer = m_TopElement.Q<VisualElement>("video-preview-container");
        }

        protected override void RegisterButtonCallbacks()
        {
            if (_tabBtnPlayer != null) _tabBtnPlayer.clicked += OnPlayerTabClicked;
            if (_tabBtnWeapon != null) _tabBtnWeapon.clicked += OnWeaponTabClicked;

            if (_unlockButton != null)
            {
                _unlockButton.clicked += OnUnlockClicked;
            }
        }

        private void OnPlayerTabClicked() => SwitchCategory(SkillCategory.Player);
        private void OnWeaponTabClicked() => SwitchCategory(SkillCategory.Weapon);

        private void SwitchCategory(SkillCategory category)
        {
            if (_currentCategory == category && _nodeMap.Count > 0) return;

            _currentCategory = category;

            // Update Tab Styles
            if (_tabBtnPlayer != null)
            {
                _tabBtnPlayer.RemoveFromClassList("tab-button--active");
                if (_currentCategory == SkillCategory.Player) _tabBtnPlayer.AddToClassList("tab-button--active");
            }

            if (_tabBtnWeapon != null)
            {
                _tabBtnWeapon.RemoveFromClassList("tab-button--active");
                if (_currentCategory == SkillCategory.Weapon) _tabBtnWeapon.AddToClassList("tab-button--active");
            }

            // Rebuild Grid
            PopulateGrid();
        }

        private void PopulateGrid()
        {
            if (_gridContainer == null) return;

            _gridContainer.Clear();
            _nodeMap.Clear();

            // Filter skills for current tab
            var filteredSkills = _allSkills.Where(s => s != null && s.category == _currentCategory);

            foreach (var skill in filteredSkills)
            {
                Button node = new Button();
                node.AddToClassList("skill-node");
                
                Label label = new Label(skill.skillID);
                label.AddToClassList("skill-node__label");
                node.Add(label);

                BindNodeToData(node, skill);
                UpdateNodeVisuals(node, skill);
                
                _gridContainer.Add(node);
            }
        }

        private void BindNodeToData(Button node, SkillData data)
        {
            if (node == null || data == null) return;

            // When the visual node is clicked, update the info panel
            node.clicked += () => SelectSkill(data);

            if (!_nodeMap.ContainsKey(data.skillID))
            {
                _nodeMap.Add(data.skillID, node);
            }
        }

        private void UpdateNodeVisuals(Button node, SkillData skill)
        {
            node.RemoveFromClassList("skill-node--unlocked");
            node.RemoveFromClassList("skill-node--available");
            node.RemoveFromClassList("skill-node--locked");

            if (_gameSession.PlayerSkills.HasSkill(skill.skillID))
            {
                node.AddToClassList("skill-node--unlocked");
            }
            else
            {
                // Everything not owned is now considered 'Available' (no prerequisites)
                node.AddToClassList("skill-node--available");
            }
        }

        private void RefreshAllNodes()
        {
            foreach (var kvp in _nodeMap)
            {
                string skillID = kvp.Key;
                Button node = kvp.Value;

                SkillData data = _allSkills.FirstOrDefault(s => s.skillID == skillID);
                if (data != null)
                {
                    UpdateNodeVisuals(node, data);
                }
            }
        }

        private void SelectSkill(SkillData skill)
        {
            _currentlySelectedSkill = skill;

            _infoName.text = skill.skillID;
            _infoDesc.text = skill.description;
            _infoCost.text = $"Cost: {skill.costSP} SP";

            bool isUnlocked = _gameSession.PlayerSkills.HasSkill(skill.skillID);

            if (isUnlocked)
            {
                _infoState.text = "Status: Unlocked";
                _unlockButton.SetEnabled(false);
            }
            else
            {
                // Check if they can actually afford it with current SP
                bool canAfford = _gameSession.PlayerSkills.AvailableSP >= skill.costSP;
                _infoState.text = canAfford ? "Status: Available" : "Status: Insufficient SP";
                _unlockButton.SetEnabled(canAfford);
            }

            // Note: Video rendering would be triggered here by assigning a RenderTexture 
            // to _videoContainer.style.backgroundImage or using a VideoPlayer component.
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

            // Reset selection
            _currentlySelectedSkill = null;
            _infoName.text = "Select a Skill";
            _infoDesc.text = "Click a skill node to see its details here.";
            _infoCost.text = "Cost: -";
            _infoState.text = "Status: -";

            // Force initial population for Player tab
            _currentCategory = SkillCategory.Weapon; // Hack to force SwitchCategory to execute
            SwitchCategory(SkillCategory.Player);
        }

        private void HandleSkillUnlocked(string skillID)
        {
            if (_currentlySelectedSkill != null && _currentlySelectedSkill.skillID == skillID)
            {
                SelectSkill(_currentlySelectedSkill);
            }

            RefreshAllNodes();
        }

        public void Dispose()
        {
            if (_tabBtnPlayer != null) _tabBtnPlayer.clicked -= OnPlayerTabClicked;
            if (_tabBtnWeapon != null) _tabBtnWeapon.clicked -= OnWeaponTabClicked;
            if (_unlockButton != null) _unlockButton.clicked -= OnUnlockClicked;
        }
    }
}