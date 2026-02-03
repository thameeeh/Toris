using UnityEngine;

[CreateAssetMenu(fileName = "RambowConfig", menuName = "Game/Abilities/Rambow")]
public class RamboBowConfig : PlayerAbilitySO
{
    [Header("Unlock Requirements")]
    [Min(0)] public int killsRequired = 30;

    [Header("Firing Behaviour")]
    [Min(0.1f)] public float shotsPerSecond = 8f;
    [Min(0f)] public float spreadDegrees = 6f;
    [Min(0f)] public float damagePerShot = 8f;
    [Min(0.1f)] public float speedPerShot = 12f;

    [Header("Cost")]
    [Min(0f)] public float initialStaminaCost = 10f;
    [Min(0f)] public float staminaPerShot = 2f;

    [Header("Misc")]
    [Min(0f)] public float maxDuration = 0f;

    bool _held;
    bool _active;
    float _startTime;
    float _nextShotTime;

    public override bool IsUnlocked(PlayerAbilityContext context)
    {

        bool unlocked = true;   /*kills >= killsRequired; previous logic, kills from Inventory instance.*/

        //Debug.Log($"[Rambow] IsUnlocked? {unlocked} (kills={kills}, required={killsRequired}, stat={killsStat.name})");

        return unlocked;
    }

    public override void OnButtonDown(PlayerAbilityContext context)
    {
        _held = true;

        var stats = context.stats;
        var bow = context.bow;

        if (stats == null || bow == null)
        {
            //Debug.LogWarning("[Rambow] Missing stats or bow in context, aborting OnButtonDown.");
            return;
        }

        if (!IsUnlocked(context))
        {
            //Debug.Log("[Rambow] OnButtonDown but ability is locked (not enough kills).");
            return;
        }

        if (isOnCooldown)
        {
            //Debug.Log("[Rambow] OnButtonDown but ability is on cooldown.");
            return;
        }

        if (initialStaminaCost > 0f && !stats.TryConsumeStamina(initialStaminaCost))
        {
            //Debug.Log("[Rambow] Not enough stamina for INITIAL cost: " + initialStaminaCost);
            return;
        }

        _active = true;
        _startTime = Time.time;
        _nextShotTime = Time.time;

        //Debug.Log("[Rambow] ACTIVATED. Firing first shot.");
        FireRambowShot(context);

        float interval = 1f / Mathf.Max(0.01f, shotsPerSecond);
        _nextShotTime = Time.time + interval;

        if (cooldownSeconds > 0f)
        {
            StartCooldown();
            //Debug.Log($"[Rambow] Starting cooldown: {cooldownSeconds} seconds.");
        }
    }

    public override void OnButtonUp(PlayerAbilityContext context)
    {
        _held = false;
        _active = false;
        //Debug.Log("[Rambow] Button released, deactivating.");
    }

    public override void Tick(PlayerAbilityContext context)
    {
        var stats = context.stats;
        var bow = context.bow;

        if (stats == null || bow == null)
            return;

        if (!_active)
            return;

        if (!_held)
        {
            _active = false;
            //Debug.Log("[Rambow] Tick: no longer held, stopping.");
            return;
        }

        if (maxDuration > 0f && Time.time - _startTime >= maxDuration)
        {
            _active = false;
            //Debug.Log("[Rambow] Tick: maxDuration reached, stopping.");
            return;
        }

        if (Time.time < _nextShotTime)
            return;

        if (staminaPerShot > 0f && !stats.TryConsumeStamina(staminaPerShot))
        {
            //Debug.Log("[Rambow] Out of stamina for per-shot cost: " + staminaPerShot + ". Stopping.");
            _active = false;
            return;
        }

        //Debug.Log("[Rambow] Tick: firing another shot.");
        FireRambowShot(context);

        float interval = 1f / Mathf.Max(0.01f, shotsPerSecond);
        _nextShotTime = Time.time + interval;
    }

    void FireRambowShot(PlayerAbilityContext context)
    {
        var bow = context.bow;

        BowSO.ShotStats shotStats = new BowSO.ShotStats
        {
            power = 1f,
            speed = speedPerShot,
            damage = damagePerShot,
            spreadDeg = spreadDegrees
        };

        //Debug.Log($"[Rambow] FireRambowShot: speed={speedPerShot}, damage={damagePerShot}, spread={spreadDegrees}");
        bow.FireArrow(shotStats);
    }
}
