using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private static Inventory _Inventory;

    private Dictionary<ResourceData, int> ResourcesCount = new Dictionary<ResourceData, int>();
    
    public event Action OnInventoryChanged;
    public static Inventory InventoryInstance
    {
        get
        {
            if (_Inventory == null)
            {
                _Inventory = new GameObject().AddComponent<Inventory>();

                _Inventory.name = _Inventory.GetType().ToString();

                DontDestroyOnLoad(_Inventory.gameObject);
            }
            return _Inventory;
        }
    }

    public Dictionary<ResourceData, int> GetAllResources()
    {
        return ResourcesCount;
    }

    public void AddResource(ResourceData resource, int amount)
    {
        //TryGetValue returns bool, 'out' returns value of coresponding key
        if (ResourcesCount.TryGetValue(resource, out int currentAmount))
        {
            ResourcesCount[resource] = currentAmount + amount;
            Debug.Log($"{resource.name}: added {amount}, new amount: {ResourcesCount[resource]}");
        }
        else
        {
            ResourcesCount.Add(resource, amount);
            Debug.Log($"{resource.name}: added {amount}, new amount: {ResourcesCount[resource]}");
        }
        // The "?" checks if there are any subscribers before invoking the event
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveResource(ResourceData resource, int amount)
    {
        if (ResourcesCount.TryGetValue(resource, out int currentAmount))
        {
            if(currentAmount - amount >= 0)
            {
                ResourcesCount[resource] = currentAmount - amount;
                // The "?" checks if there are any subscribers before invoking the event
                OnInventoryChanged?.Invoke();
                return true;
            }
            else
            {
                Debug.Log($"{resource.name}: Not enough resources to remove!");
            }
        }
        else
        {
            Debug.Log($"{resource.name}: was not found in dictionary!");
        }
        return false;
    }

    public int GetResourceAmount(ResourceData resource)
    {
        if (ResourcesCount.TryGetValue(resource, out int currentAmount))
        {
            return currentAmount;
        }
        else
        {
            Debug.Log($"{resource.name}: was not found in dictionary!");
            return 0;
        }
    }
}
