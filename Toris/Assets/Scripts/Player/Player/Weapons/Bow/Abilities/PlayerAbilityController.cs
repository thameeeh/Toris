using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerBowController _bow;

    [Header("Abilities")]
    [SerializeField] private MultiShotConfig _multiShot;

    [Header("Debug")]
    [SerializeField] private bool _debugAbility = false;

    private float _multiShotReadyAt = 0f;

    void OnEnable()
    {
        if (_input != null)
            _input.OnAbility1Pressed += HandleMultiShotPressed;
        else if (_debugAbility)
            Debug.LogWarning("[Ability] _input is null on PlayerAbilityController", this);
    }

    void OnDisable()
    {
        if (_input != null)
            _input.OnAbility1Pressed -= HandleMultiShotPressed;
    }

    void HandleMultiShotPressed()
    {
        if (_debugAbility)
            Debug.Log("[Ability] HandleMultiShotPressed called", this);

        if (_multiShot == null)
        {
            if (_debugAbility) Debug.LogWarning("[Ability] MultiShotConfig is null", this);
            return;
        }

        if (_bow == null)
        {
            if (_debugAbility) Debug.LogWarning("[Ability] PlayerBowController is null", this);
            return;
        }

        if (_stats == null)
        {
            if (_debugAbility) Debug.LogWarning("[Ability] PlayerStats is null", this);
            return;
        }

        if (Time.time < _multiShotReadyAt)
        {
            if (_debugAbility) Debug.Log($"[Ability] MultiShot on cooldown. Ready at {_multiShotReadyAt}, now {Time.time}", this);
            return;
        }

        if (!_stats.TryConsumeStamina(_multiShot.staminaCost))
        {
            if (_debugAbility) Debug.Log("[Ability] Not enough stamina for MultiShot", this);
            return;
        }

        if (_debugAbility)
            Debug.Log("[Ability] MultiShot armed for next shot!", this);

        _bow.QueueMultiShot(_multiShot);

        _multiShotReadyAt = Time.time + _multiShot.cooldown;
    }
}
