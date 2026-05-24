using UnityEngine;

public sealed class EnemySfx : MonoBehaviour
{
    [SerializeField] private Enemy enemy;
    [SerializeField] private EnemySfxModule[] modules;

    private EnemySfxContext ctx;

    private void Awake()
    {
        if (!enemy)
            enemy = GetComponent<Enemy>();

        ctx = new EnemySfxContext(
            hub: this,
            transform: transform,
            enemy: enemy
        );
    }

    private void OnEnable()
    {
        if (enemy != null)
        {
            enemy.Damaged += OnDamaged;
            enemy.Died += OnDied;
        }
    }

    private void OnDisable()
    {
        if (enemy != null)
        {
            enemy.Damaged -= OnDamaged;
            enemy.Died -= OnDied;
        }
    }

    private void OnDamaged(float damage)
    {
        if (modules == null) return;

        for (int i = 0; i < modules.Length; i++)
            modules[i]?.OnDamaged(ctx, damage);
    }

    private void OnDied(Enemy deadEnemy)
    {
        if (modules == null) return;

        for (int i = 0; i < modules.Length; i++)
            modules[i]?.OnDied(ctx, deadEnemy);
    }
}
