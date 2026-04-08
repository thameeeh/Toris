using UnityEngine;

// PURPOSE: Standard payload describing a hit applied to the player.
// Includes direction (for knockback), damage, source, i-frame bypass flag,
// and optional status-effect application data.

public struct HitData
{
    public Vector2 origin;
    public Vector2 direction;

    public float damage;
    public float knockback;

    public GameObject source;
    public bool bypassIFrames;

    public bool appliesStatus;
    public PlayerStatusEffectType statusType;
    public float statusDamagePerSecond;
    public float statusDuration;
    public float statusTickInterval;
    public int statusStacks;

    public HitData(
        Vector2 origin,
        Vector2 dir,
        float dmg,
        float kb,
        GameObject src,
        bool bypass = false)
    {
        this.origin = origin;
        this.direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        this.damage = dmg;
        this.knockback = kb;
        this.source = src;
        this.bypassIFrames = bypass;

        this.appliesStatus = false;
        this.statusType = default;
        this.statusDamagePerSecond = 0f;
        this.statusDuration = 0f;
        this.statusTickInterval = 1f;
        this.statusStacks = 1;
    }

    public void SetStatus(
        PlayerStatusEffectType type,
        float damagePerSecond,
        float duration,
        float tickInterval = 1f,
        int stacks = 1)
    {
        appliesStatus = true;
        statusType = type;
        statusDamagePerSecond = Mathf.Max(0f, damagePerSecond);
        statusDuration = Mathf.Max(0f, duration);
        statusTickInterval = Mathf.Max(0.01f, tickInterval);
        statusStacks = Mathf.Max(1, stacks);
    }

    public void ClearStatus()
    {
        appliesStatus = false;
        statusType = default;
        statusDamagePerSecond = 0f;
        statusDuration = 0f;
        statusTickInterval = 1f;
        statusStacks = 1;
    }
}
