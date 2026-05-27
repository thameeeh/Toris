using UnityEngine;

namespace OutlandHaven.Tutorial
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PrologueWolfEncounterTrigger : MonoBehaviour
    {
        [SerializeField] private string playerTag = "Player";
        [SerializeField] private Wolf encounterWolf;
        [SerializeField] private PrologueTutorialFlowController tutorialFlow;
        [SerializeField] private bool forceWolfAggroOnTrigger = true;
        [SerializeField] private bool triggerOnce = true;

        private bool _triggered;

        private void Awake()
        {
            ResolveReferences();
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if ((_triggered && triggerOnce) || !TryResolvePlayerTarget(other, out IEnemyAggroTarget playerTarget))
                return;

            _triggered = true;
            ResolveReferences();

            encounterWolf?.WakeForEncounter(playerTarget, forceWolfAggroOnTrigger);
            tutorialFlow?.BeginWolfEncounterTutorial(encounterWolf);
        }

        private void ResolveReferences()
        {
            if (encounterWolf == null)
                encounterWolf = FindFirstObjectByType<Wolf>();

            if (tutorialFlow == null)
                tutorialFlow = FindFirstObjectByType<PrologueTutorialFlowController>();
        }

        private bool TryResolvePlayerTarget(Collider2D other, out IEnemyAggroTarget playerTarget)
        {
            playerTarget = null;
            if (other == null)
                return false;

            PlayerDamageReceiver damageReceiver = other.GetComponentInParent<PlayerDamageReceiver>();
            if (damageReceiver != null)
            {
                playerTarget = damageReceiver;
                return true;
            }

            if (!string.IsNullOrWhiteSpace(playerTag) && !other.CompareTag(playerTag))
                return false;

            playerTarget = other.GetComponentInParent<IEnemyAggroTarget>();
            return playerTarget != null || other.GetComponentInParent<PlayerInteractor>() != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Collider2D triggerCollider = GetComponent<Collider2D>();
            if (triggerCollider != null)
                triggerCollider.isTrigger = true;
        }
#endif
    }
}
