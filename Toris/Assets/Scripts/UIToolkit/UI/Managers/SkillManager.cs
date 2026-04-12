using UnityEngine;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Skills
{
    public class SkillManager : MonoBehaviour
    {
        [SerializeField] private GameSessionSO _gameSession;
        [SerializeField] private UISkillEventsSO _skillEvents;

        private void OnEnable()
        {
            // Listen for the UI asking to unlock a skill
            _skillEvents.OnRequestUnlock += HandleUnlockRequest;
        }

        private void OnDisable()
        {
            _skillEvents.OnRequestUnlock -= HandleUnlockRequest;
        }

        private void HandleUnlockRequest(SkillData skill)
        {
            // 1. Try to modify the ultimate source of truth
            bool success = _gameSession.PlayerSkills.TryUnlockSkill(skill);

            // 2. If it worked, broadcast the changes to everyone via the SO
            if (success)
            {
                _skillEvents.OnSkillUnlocked?.Invoke(skill.skillID);
                _skillEvents.OnSPUpdated?.Invoke(_gameSession.PlayerSkills.AvailableSP);
                Debug.Log($"Successfully unlocked {skill.skillName}!");
            }
            else
            {
                Debug.LogWarning("Unlock failed (already owned or not enough SP).");
            }
        }
    }
}