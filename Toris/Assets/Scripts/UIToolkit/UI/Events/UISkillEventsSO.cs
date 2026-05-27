using UnityEngine;
using UnityEngine.Events;

namespace OutlandHaven.Skills
{
    [CreateAssetMenu(menuName = "Outland Haven/UI/Events/UI Skill Events")]
    public class UISkillEventsSO : ScriptableObject
    {
        [Header("System -> UI Updates")]
        public UnityAction<int> OnSPUpdated;
        public UnityAction<string> OnSkillUnlocked;

        [Header("Ability Updates")]
        public UnityAction<int> OnAbilitySlotPressed; // slotIndex
        public UnityAction<int, float> OnAbilityCooldownStarted; // slotIndex, cooldownDuration
        public UnityAction<int> OnAbilityReady; // slotIndex
        public UnityAction<PlayerAbilitySlotSnapshot[]> OnAbilitySlotsUpdated; // New centralized update event
        public UnityAction<PlayerAbilitySlotSnapshot, Vector2> OnAbilityTooltipShow;
        public UnityAction<Vector2> OnAbilityTooltipMove;
        public UnityAction OnAbilityTooltipHide;

        [Header("UI -> System Requests")]
        public UnityAction<SkillData> OnRequestUnlock;

        [Header("System -> Runtime Integration")]
        public UnityAction<PlayerAbilitySO> OnAbilityAutoEquip;
    }
}
