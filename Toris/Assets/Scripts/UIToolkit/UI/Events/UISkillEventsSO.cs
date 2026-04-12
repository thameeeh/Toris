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

        [Header("UI -> System Requests")]
        public UnityAction<SkillData> OnRequestUnlock;
    }
}