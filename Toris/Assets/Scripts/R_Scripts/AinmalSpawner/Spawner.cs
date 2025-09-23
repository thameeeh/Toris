using UnityEngine;
using System.Collections.Generic;
public class Spawner : MonoBehaviour
{
    [Header("Animals")]
    [SerializeField]
    private int _animalCount;
    [SerializeField]
    private GameObject _animalObject;
    [SerializeField]
    private Vector2[] positions;


    [Header("Collectibles")]
    [SerializeField]
    private int _collectibleCount;
    [SerializeField]
    private GameObject _collectibleObject;


    public List<GameObject> animals = new();
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        for (int i = 0; i < _animalCount; i++)
        {
            GameObject animal = Instantiate(_animalObject, positions[i], Quaternion.identity);
            animals.Add(animal);
        }
    }

    private void Update()
    {
        for (int i = animals.Count - 1; i >= 0; i--)
        {
            var animal = animals[i];
            if (animal == null) continue; // destroyed in scene

            var behaviour = animal.GetComponent<AnimalBehaviour>();
            
            if (behaviour != null && behaviour.IsDead)
            {
                Vector3 pos = animal.transform.position;

                // Spawn collectible
                Instantiate(_collectibleObject, pos, Quaternion.identity);

                // Remove from list and destroy
                animals.RemoveAt(i);
                Destroy(animal);
            }
        }
    }
}
