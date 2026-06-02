using UnityEngine;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Dynamically enables or disables a GameObject based on Dialogue System quest states or Lua conditions.
/// Keeps the NPC enabled by default in the Unity scene editor, deactivating them on scene load if conditions are not met.
/// </summary>
public class QuestStateActiveSwitch : MonoBehaviour
{
    [Header("Target GameObject")]
    [Tooltip("The GameObject to toggle. If left empty, it will default to this GameObject.")]
    [SerializeField] private GameObject _targetObject;

    [Header("Activation Conditions")]
    [Tooltip("If checked, the conditions will be evaluated instantly on scene load (Start).")]
    [SerializeField] private bool _evaluateOnStart = true;
    
    [Tooltip("If checked, the script will listen for quest changes and toggle the target dynamically in real-time.")]
    [SerializeField] private bool _evaluateOnQuestChanges = true;

    [Tooltip("Dialogue System quest conditions or Lua variables that must be true to enable the target.")]
    [SerializeField] private Condition _condition;

    private bool _isSubscribed = false;

    private void Awake()
    {
        if (_targetObject == null)
        {
            _targetObject = gameObject;
        }
    }

    private void Start()
    {
        if (_evaluateOnStart)
        {
            EvaluateConditions();
        }

        if (_evaluateOnQuestChanges)
        {
            SubscribeToEvents();
        }
    }

    private void OnEnable()
    {
        if (_evaluateOnQuestChanges)
        {
            SubscribeToEvents();
        }
    }

    private void OnDisable()
    {
        UnsubscribeFromEvents();
    }

    private void OnDestroy()
    {
        UnsubscribeFromEvents();
    }

    private void SubscribeToEvents()
    {
        if (_isSubscribed || !DialogueManager.hasInstance) return;

        // Automatically add DialogueSystemEvents to the Dialogue Manager if missing
        // to guarantee that C# event notifications propagate reliably.
        var dsEvents = DialogueManager.instance.GetComponent<DialogueSystemEvents>();
        if (dsEvents == null)
        {
            dsEvents = DialogueManager.instance.gameObject.AddComponent<DialogueSystemEvents>();
        }

        if (dsEvents != null)
        {
            dsEvents.questEvents.onQuestStateChange.AddListener(OnQuestStateChanged);
            _isSubscribed = true;
        }
    }

    private void UnsubscribeFromEvents()
    {
        if (!_isSubscribed || !DialogueManager.hasInstance) return;

        var dsEvents = DialogueManager.instance.GetComponent<DialogueSystemEvents>();
        if (dsEvents != null)
        {
            dsEvents.questEvents.onQuestStateChange.RemoveListener(OnQuestStateChanged);
        }
        _isSubscribed = false;
    }

    private void OnQuestStateChanged(string questName)
    {
        EvaluateConditions();
    }

    public void EvaluateConditions()
    {
        if (_targetObject == null || !DialogueManager.hasInstance) return;

        // If no condition is defined, keep it enabled by default
        bool isConditionMet = _condition == null || _condition.IsTrue(null);

        // Toggle target GameObject visibility
        if (_targetObject.activeSelf != isConditionMet)
        {
            _targetObject.SetActive(isConditionMet);
            
            if (DialogueDebug.logInfo)
            {
                Debug.Log($"[QuestStateActiveSwitch] Set active state of '{_targetObject.name}' to {isConditionMet} based on Dialogue conditions.", this);
            }
        }
    }
}
