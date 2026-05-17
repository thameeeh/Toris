using UnityEngine;
using UnityEngine.Events;

namespace OutlandHaven.Skills
{
    [CreateAssetMenu(menuName = "UI/Scriptable Objects/Events/UISkillEventsSO")]
    public class UISkillEventsSO : ScriptableObject
    {
        [Header("System -> UI Updates")]
        public UnityAction<int> OnSPUpdated;
        public UnityAction<string> OnSkillUnlocked;

        [Header("Ability Updates")]
        public UnityAction<int> OnAbilitySlotPressed; // slotIndex
        public UnityAction<int, float> OnAbilityCooldownStarted; // slotIndex, cooldownDuration
        public UnityAction<int> OnAbilityReady; // slotIndex

        [Header("UI -> System Requests")]
        public UnityAction<SkillData> OnRequestUnlock;
    }
}