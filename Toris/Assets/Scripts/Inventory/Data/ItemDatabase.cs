using System.Collections.Generic;
using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [SerializeField] private List<ResourceData> itemLookup;
    
    private Dictionary<string, ResourceData> itemDictionary = new Dictionary<string, ResourceData>();
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        ResourceData[] items = Resources.LoadAll<ResourceData>("Items");

        foreach (ResourceData item in items)
        {
            if (!itemLookup.Contains(item))
            {
                itemLookup.Add(item);
            }

            if(!itemDictionary.ContainsKey(item.ID))
            {
                itemDictionary.Add(item.ID, item);
            }
        }

        string itemId = itemLookup[0].ID;
        Debug.Log($"Item {itemLookup[0].resourceName}, {itemDictionary[itemId].ID}");
    }
}
