using System;
using System.Collections.Generic;

public sealed class WorldEncounterOccupantCollection<TEnemy> where TEnemy : Enemy
{
    private readonly List<TEnemy> tracked = new();
    private readonly Dictionary<TEnemy, Action<Enemy>> despawnHandlers = new();

    public int Count
    {
        get
        {
            RemoveNulls();
            return tracked.Count;
        }
    }

    public void Track(TEnemy enemy, Action<TEnemy> onDespawned)
    {
        if (enemy == null || tracked.Contains(enemy))
            return;

        tracked.Add(enemy);

        Action<Enemy> handler = spawnedEnemy =>
        {
            TEnemy typedEnemy = spawnedEnemy as TEnemy;
            if (typedEnemy == null)
                return;

            Untrack(typedEnemy);
            onDespawned?.Invoke(typedEnemy);
        };

        despawnHandlers.Add(enemy, handler);
        enemy.Despawned += handler;
    }

    public void Untrack(TEnemy enemy)
    {
        if (enemy == null)
            return;

        if (despawnHandlers.TryGetValue(enemy, out Action<Enemy> handler))
        {
            enemy.Despawned -= handler;
            despawnHandlers.Remove(enemy);
        }

        tracked.Remove(enemy);
    }

    public void RemoveNulls()
    {
        tracked.RemoveAll(enemy => enemy == null);
    }

    public TEnemy[] Snapshot()
    {
        RemoveNulls();
        return tracked.ToArray();
    }

    public void ReleaseAll(Action<TEnemy> releaseAction)
    {
        TEnemy[] snapshot = Snapshot();
        Clear();

        for (int i = 0; i < snapshot.Length; i++)
        {
            if (snapshot[i] != null)
                releaseAction?.Invoke(snapshot[i]);
        }
    }

    public void ReleaseWhere(
        Func<TEnemy, bool> keepAlivePredicate,
        Action<TEnemy> detachAction,
        Action<TEnemy> releaseAction)
    {
        TEnemy[] snapshot = Snapshot();
        Clear();

        for (int i = 0; i < snapshot.Length; i++)
        {
            TEnemy enemy = snapshot[i];
            if (enemy == null)
                continue;

            if (keepAlivePredicate != null && keepAlivePredicate(enemy))
            {
                detachAction?.Invoke(enemy);
                continue;
            }

            releaseAction?.Invoke(enemy);
        }
    }

    public void Clear()
    {
        foreach (KeyValuePair<TEnemy, Action<Enemy>> pair in despawnHandlers)
        {
            if (pair.Key != null)
                pair.Key.Despawned -= pair.Value;
        }

        despawnHandlers.Clear();
        tracked.Clear();
    }
}
