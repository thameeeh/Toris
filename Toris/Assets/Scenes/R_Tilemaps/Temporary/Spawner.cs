using NUnit.Framework;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Spawner : MonoBehaviour
{
    public GameObject ObjectToSpawn;
    List<GameObject> gameObjects = new List<GameObject>();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < 500; i++) 
        {
            GameObject currententity = Instantiate(ObjectToSpawn, new Vector3(Random.Range(-20f, 20f), Random.Range(-20f, 20f), 0), Quaternion.identity);
            gameObjects.Add(currententity);
        }
    }

    private void Update()
    {
        foreach (var entity in gameObjects.ToArray())
        {
            if (entity == null)
            {
                gameObjects.Remove(entity);
            }
        }

        Debug.Log(gameObjects.Count);
        if (gameObjects.Count <= 0)
        {
            
            SceneManager.UnloadSceneAsync(SceneManager.GetSceneByName("StressTest"));
        }
    }
}
