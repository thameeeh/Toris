using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject entityToSpawn;

    public TreeScriptableObject spawnManagerValues;

    int instanceNumber = 1;
    private List<GameObject> spawnedEntities = new List<GameObject>();

    private IEnumerator coroutine;

    void Start()
    {
        SpawnEntities();
        coroutine = DestroyEntities(2);
        StartCoroutine(coroutine);
    }

    void SpawnEntities() 
    {
        int currentSpawnPointIndex = 0;

        for (int i = 0; i < spawnManagerValues.numberOfPrefabsToCreate; ++i) 
        {
            GameObject currentEntity = Instantiate(entityToSpawn, spawnManagerValues.spawnPoints[currentSpawnPointIndex], Quaternion.identity);

            currentEntity.name = spawnManagerValues.prefabName + instanceNumber;

            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnManagerValues.spawnPoints.Length;

            spawnedEntities.Add(currentEntity);

            ++instanceNumber;
        }
    }
    private IEnumerator DestroyEntities(float time) 
    {
        yield return new WaitForSeconds(time);
        foreach (var entity in spawnedEntities) {
            Destroy(entity);
        }
    }

}
