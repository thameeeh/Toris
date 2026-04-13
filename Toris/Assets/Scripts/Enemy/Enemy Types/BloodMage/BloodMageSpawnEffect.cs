using UnityEngine;

public class BloodMageSpawnEffect : Projectile
{
    private BloodMage _bloodMagePrefab;
    private Necromancer _owner;
    private Vector3 _spawnPosition;
    private int _summonIndex;
    private int _summonGroupSize = 1;
    private bool _hasSpawnedBloodMage;

    public void Initialize(BloodMage bloodMagePrefab, Necromancer owner, Vector3 spawnPosition, int summonIndex, int summonGroupSize)
    {
        _bloodMagePrefab = bloodMagePrefab;
        _owner = owner;
        _spawnPosition = spawnPosition;
        _summonIndex = summonIndex;
        _summonGroupSize = Mathf.Max(1, summonGroupSize);
        _hasSpawnedBloodMage = false;

        transform.position = spawnPosition;
    }

    public void Anim_SpawnComplete()
    {
        SpawnBloodMageOnce();
    }

    public void Anim_AttackFinished()
    {
        FinishSpawnEffect();
    }

    public void Anim_Finished()
    {
        FinishSpawnEffect();
    }

    private void FinishSpawnEffect()
    {
        SpawnBloodMageOnce();
        Despawn();
    }

    private void SpawnBloodMageOnce()
    {
        if (_hasSpawnedBloodMage)
            return;

        _hasSpawnedBloodMage = true;
        SpawnBloodMage();
    }

    private void SpawnBloodMage()
    {
        if (_bloodMagePrefab == null || _owner == null || _owner.CurrentHealth <= 0f || _owner.IsHumanForm)
            return;

        Quaternion spawnRotation = Quaternion.identity;
        BloodMage spawnedBloodMage = null;

        if (GameplayPoolManager.Instance != null)
        {
            spawnedBloodMage = GameplayPoolManager.Instance.SpawnEnemy(
                _bloodMagePrefab,
                _spawnPosition,
                spawnRotation) as BloodMage;
        }

        if (spawnedBloodMage == null)
        {
            // Safety fallback for scenes/tests without configured gameplay pools.
            spawnedBloodMage = Instantiate(_bloodMagePrefab, _spawnPosition, spawnRotation);
            spawnedBloodMage.OnSpawned();
        }

        if (spawnedBloodMage != null)
            spawnedBloodMage.ConfigureSummon(_owner, _summonIndex, _summonGroupSize);
    }

    public override void OnSpawned()
    {
        _bloodMagePrefab = null;
        _owner = null;
        _spawnPosition = Vector3.zero;
        _summonIndex = -1;
        _summonGroupSize = 1;
        _hasSpawnedBloodMage = false;
    }

    public override void OnDespawned()
    {
        _bloodMagePrefab = null;
        _owner = null;
        _spawnPosition = Vector3.zero;
        _summonIndex = -1;
        _summonGroupSize = 1;
        _hasSpawnedBloodMage = false;
    }
}
