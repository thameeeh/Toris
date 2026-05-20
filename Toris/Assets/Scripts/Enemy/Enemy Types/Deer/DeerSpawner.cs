using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DeerSpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    [SerializeField] private Deer deerPrefab;
    [SerializeField, Min(1)] private int spawnBatchCount = 1;
    [SerializeField, Min(0f)] private float spawnDelay = 0.2f;
    [SerializeField] private bool spawnOnStart = true;

    [Header("Repeated Spawning Settings")]
    [SerializeField] private bool repeatSpawning = false;
    [SerializeField, Min(0.1f)] private float repeatInterval = 10f;
    [Tooltip("Maximum allowed active deer spawned by this spawner at any time to preserve performance.")]
    [SerializeField, Min(1)] private int maxActiveSpawns = 10;

    [Header("Transformation Path (Optional)")]
    [Tooltip("If assigned, spawned deer will teleport here and run to the finish point.")]
    [SerializeField] private Transform initialStartPoint;
    [Tooltip("If assigned, spawned deer will teleport here and run to the finish point.")]
    [SerializeField] private Transform initialFinishPoint;
    [Tooltip("Applies a random offset in units around the finish point so deers don't all run to the exact same spot.")]
    [SerializeField, Min(0f)] private float finishPointDeviation = 2f;

    [Header("Alternative Spawn Location")]
    [Tooltip("Used as spawn center if initialStartPoint is unassigned.")]
    [SerializeField] private Transform spawnCenter;
    [SerializeField, Min(0f)] private float spawnRandomRadius = 2f;

    [Header("Despawn Bounds")]
    [Tooltip("The center point used to calculate the boundary box. If unassigned, the spawner's transform is used.")]
    [SerializeField] private Transform boundsCenter;
    [Tooltip("Maximum allowed distance from the center along the X-axis before the deer is despawned.")]
    [SerializeField, Min(0f)] private float despawnHalfWidth = 5f;
    [Tooltip("Maximum allowed distance from the center along the Y-axis before the deer is despawned.")]
    [SerializeField, Min(0f)] private float despawnHalfHeight = 5f;

    private readonly List<Deer> activeSpawns = new List<Deer>();
    private Coroutine spawnLoopRoutine;

    private void Start()
    {
        if (spawnOnStart)
        {
            if (repeatSpawning)
            {
                spawnLoopRoutine = StartCoroutine(RepeatedSpawnLoop());
            }
            else
            {
                StartCoroutine(SpawnSequence());
            }
        }
    }

    private void Update()
    {
        Vector3 centerPos = boundsCenter != null ? boundsCenter.position : transform.position;

        for (int i = activeSpawns.Count - 1; i >= 0; i--)
        {
            Deer spawned = activeSpawns[i];
            if (spawned == null)
            {
                activeSpawns.RemoveAt(i);
                continue;
            }

            Vector3 deerPos = spawned.transform.position;
            if (Mathf.Abs(deerPos.x - centerPos.x) > despawnHalfWidth ||
                Mathf.Abs(deerPos.y - centerPos.y) > despawnHalfHeight)
            {
                spawned.RequestDespawn();
                activeSpawns.RemoveAt(i);
            }
        }
    }

    public void TriggerSpawn()
    {
        StartCoroutine(SpawnSequence());
    }

    public void StartRepeatedSpawning()
    {
        StopRepeatedSpawning();
        spawnLoopRoutine = StartCoroutine(RepeatedSpawnLoop());
    }

    public void StopRepeatedSpawning()
    {
        if (spawnLoopRoutine != null)
        {
            StopCoroutine(spawnLoopRoutine);
            spawnLoopRoutine = null;
        }
    }

    private IEnumerator RepeatedSpawnLoop()
    {
        while (true)
        {
            yield return StartCoroutine(SpawnSequence());
            yield return new WaitForSeconds(repeatInterval);
        }
    }

    private IEnumerator SpawnSequence()
    {
        if (deerPrefab == null)
        {
            Debug.LogError($"[DeerSpawner:{name}] Deer Prefab is not assigned!", this);
            yield break;
        }

        // Clean up any destroyed/despawned instances
        activeSpawns.RemoveAll(d => d == null);

        Vector3 baseSpawnPos = transform.position;
        if (initialStartPoint != null)
        {
            baseSpawnPos = initialStartPoint.position;
        }
        else if (spawnCenter != null)
        {
            baseSpawnPos = spawnCenter.position;
        }

        for (int i = 0; i < spawnBatchCount; i++)
        {
            // Respect performance cap
            if (activeSpawns.Count >= maxActiveSpawns)
            {
                yield break;
            }

            Vector3 spawnPos = baseSpawnPos;
            if (initialStartPoint == null)
            {
                Vector2 randomOffset = Random.insideUnitCircle * spawnRandomRadius;
                spawnPos += new Vector3(randomOffset.x, randomOffset.y, 0f);
            }

            Deer spawned = Instantiate(deerPrefab, spawnPos, Quaternion.identity);

            // Dynamically assign transformation path references before the first update frame
            if (spawned != null)
            {
                activeSpawns.Add(spawned);
                spawned.SetInitialPath(initialStartPoint, initialFinishPoint, finishPointDeviation);
            }

            if (spawnDelay > 0f && i < spawnBatchCount - 1)
            {
                yield return new WaitForSeconds(spawnDelay);
            }
        }
    }
}
