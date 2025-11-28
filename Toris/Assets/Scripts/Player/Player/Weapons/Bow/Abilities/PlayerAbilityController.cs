using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputReader _input;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerBowController _bow;

    [Header("MultiShot")]
    [SerializeField] private MultiShotConfig _multiShot;
    [SerializeField] private bool _debugMultiShot = false;

    [Header("Rambow (Rambo Bow)")]
    [SerializeField] private RamboBowConfig _ramboConfig;
    [SerializeField] private bool _debugRambow = false;

    private float _multiShotReadyAt = 0f;

    private bool _ramboHeld;
    private bool _ramboActive;
    private float _ramboNextShotTime;
    private float _ramboStartTime;

    private void OnEnable()
    {
        if (_input != null)
        {
            _input.OnAbility1Pressed += HandleMultiShotPressed;

            _input.OnAbility2Started += HandleRamboPressed;
            _input.OnAbility2Released += HandleRamboReleased;
        }
        else
        {
            Debug.LogWarning("[Ability] PlayerInputReader is not assigned on PlayerAbilityController", this);
        }
    }

    private void OnDisable()
    {
        if (_input != null)
        {
            _input.OnAbility1Pressed -= HandleMultiShotPressed;

            _input.OnAbility2Started -= HandleRamboPressed;
            _input.OnAbility2Released -= HandleRamboReleased;
        }
    }

    private void Update()
    {
        TickRambow();
    }

    // -----------------------------------------------------------
    //  MULTISHOT  (Ability1 pressed once => next shot is volley)
    // -----------------------------------------------------------
    private void HandleMultiShotPressed()
    {
        if (_debugMultiShot)
            Debug.Log("[Ability] HandleMultiShotPressed called", this);

        if (_multiShot == null || _bow == null || _stats == null)
        {
            if (_debugMultiShot)
                Debug.Log("[Ability] MultiShot missing refs (config/bow/stats)", this);
            return;
        }

        if (Time.time < _multiShotReadyAt)
        {
            if (_debugMultiShot)
                Debug.Log($"[Ability] MultiShot on cooldown. Ready at {_multiShotReadyAt}, now {Time.time}", this);
            return;
        }

        if (!_stats.TryConsumeStamina(_multiShot.staminaCost))
        {
            if (_debugMultiShot)
                Debug.Log("[Ability] Not enough stamina for MultiShot", this);
            return;
        }

        _bow.QueueMultiShot(_multiShot);
        _multiShotReadyAt = Time.time + _multiShot.cooldown;

        if (_debugMultiShot)
            Debug.Log("[Ability] MultiShot armed for next shot!", this);
    }

    // -----------------------------------------------------------
    //  RAMBOW (Ability2 hold + Attack hold => minigun arrows)
    // -----------------------------------------------------------
    private void HandleRamboPressed()
    {
        _ramboHeld = true;

        if (!CanEnterRambow())
        {
            if (_debugRambow)
                Debug.Log("[Rambow] Cannot enter Rambow mode (locked / missing refs)", this);
            return;
        }

        // Initial stamina cost for entering Rambow, if configured
        if (_ramboConfig.initialStaminaCost > 0f)
        {
            if (!_stats.TryConsumeStamina(_ramboConfig.initialStaminaCost))
            {
                if (_debugRambow)
                    Debug.Log("[Rambow] Not enough stamina for initial activation", this);
                return;
            }
        }

        _ramboActive = true;
        _ramboStartTime = Time.time;
        _ramboNextShotTime = Time.time;

        if (_debugRambow)
            Debug.Log("[Rambow] Rambow mode ACTIVATED", this);
    }

    private void HandleRamboReleased()
    {
        _ramboHeld = false;

        if (_ramboActive && _debugRambow)
            Debug.Log("[Rambow] Rambow mode DEACTIVATED (Ability2 released)", this);

        _ramboActive = false;
    }

    public bool CanEnterRambow()
    {
        if (_ramboConfig == null || _bow == null || _stats == null || _input == null)
            return false;

        // Kill gating via Inventory stats
        if (_ramboConfig.killsStat != null)
        {
            int kills = Inventory.InventoryInstance.GetResourceAmount(_ramboConfig.killsStat);
            if (kills < _ramboConfig.killsRequired)
            {
                if (_debugRambow)
                    Debug.Log($"[Rambow] Locked: {kills}/{_ramboConfig.killsRequired} kills", this);
                return false;
            }
        }

        return true;
    }

    private void TickRambow()
    {
        if (!_ramboActive)
            return;

        if (_ramboConfig != null && _ramboConfig.maxDuration > 0f)
        {
            if (Time.time - _ramboStartTime >= _ramboConfig.maxDuration)
            {
                if (_debugRambow)
                    Debug.Log("[Rambow] Rambow mode ended (duration limit)", this);
                _ramboActive = false;
                return;
            }
        }

        if (!_input.IsShootHeld)
            return;

        if (Time.time < _ramboNextShotTime)
            return;

        if (_ramboConfig == null || _bow == null || _stats == null)
            return;

        if (_ramboConfig.staminaPerShot > 0f)
        {
            if (!_stats.TryConsumeStamina(_ramboConfig.staminaPerShot))
            {
                if (_debugRambow)
                    Debug.Log("[Rambow] Exiting Rambow mode (no stamina)", this);
                _ramboActive = false;
                return;
            }
        }

        FireRambowShot();

        float interval = 1f / Mathf.Max(0.01f, _ramboConfig.shotsPerSecond);
        _ramboNextShotTime = Time.time + interval;
    }

    private void FireRambowShot()
    {
        BowSO.ShotStats stats = new BowSO.ShotStats
        {
            power = 1f,
            speed = _ramboConfig.speedPerShot,
            damage = _ramboConfig.damagePerShot,
            spreadDeg = _ramboConfig.spreadDegrees
        };

        if (_debugRambow)
            Debug.Log("[Rambow] Firing Rambow arrow", this);

        _bow.FireArrow(stats);
    }
}
