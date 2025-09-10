using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.NCalc;
using UnityEngine;
using UnityEngine.InputSystem;

public class Spawner : MonoBehaviour
{
    public GameObject entityToSpawn;

    public TreeScriptableObject spawnManagerValues;

    int instanceNumber = 1;
    private List<GameObject> spawnedEntities = new List<GameObject>();

    Mouse mouse;

    void Start()
    {
        //SpawnEntity();
        mouse = Mouse.current;
    }

    void SpawnEntity() 
    {
        int currentSpawnPointIndex = 0;

        for (int i = 0; i < spawnManagerValues.pointsList.Count; ++i) 
        {
            GameObject currentEntity = Instantiate(entityToSpawn, spawnManagerValues.pointsList[currentSpawnPointIndex], Quaternion.identity);

            currentEntity.name = spawnManagerValues.prefabName + instanceNumber;

            currentSpawnPointIndex = (currentSpawnPointIndex + 1) % spawnManagerValues.pointsList.Count;

            spawnedEntities.Add(currentEntity);
            ++instanceNumber;
        }
    }

    private void Update()
    {
        if (mouse != null && mouse.leftButton.wasPressedThisFrame) 
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, 10f));

            GameObject currentObject = Instantiate(entityToSpawn, worldPos, Quaternion.identity);
            currentObject.name = spawnManagerValues.name;

            spawnedEntities.Add(currentObject);
        }
    }
}
