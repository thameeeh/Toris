using UnityEngine;

// PURPOSE: Standard payload describing a hit applied to the player.
// Includes direction (for knockback), damage, source, and i-frame bypass flag.

public struct HitData
{
    public Vector2 origin, direction;   // direction normalized if non-zero; used for knockback
    public float damage, knockback;     // scalar damage and knockback impulse
    public GameObject source;           // who caused the hit (projectile/enemy/trap)
    public bool bypassIFrames;          // if true, ignore i-frame gating (e.g., lasers/DOT)

    public HitData(Vector2 origin, Vector2 dir, float dmg, float kb, GameObject src, bool bypass = false)
    {
        this.origin = origin;
        this.direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.zero;
        this.damage = dmg;
        this.knockback = kb;
        this.source = src;
        this.bypassIFrames = bypass;
    }
}
