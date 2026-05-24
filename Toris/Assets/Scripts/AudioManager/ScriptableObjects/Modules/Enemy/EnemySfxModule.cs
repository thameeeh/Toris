using UnityEngine;

public abstract class EnemySfxModule : ScriptableObject
{
    public virtual void OnDamaged(in EnemySfxContext ctx, float damage) { }
    public virtual void OnDied(in EnemySfxContext ctx, Enemy enemy) { }
}
