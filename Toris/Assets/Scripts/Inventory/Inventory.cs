using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private static Inventory _Inventory;

    //for resources like wood, stone, flowers
    private Dictionary<ResourceData, ResourceAmount> ResourcesCount = new Dictionary<ResourceData, ResourceAmount>();

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

    public Dictionary<ResourceData, ResourceAmount> GetAllResources()
    {
        return new Dictionary<ResourceData, ResourceAmount>(ResourcesCount);
    }

    private void AddToDictionary(ResourceData resource, int amount) 
    {
        //TryGetValue returns bool, 'out' returns value of coresponding key
        if (ResourcesCount.TryGetValue(resource, out ResourceAmount currentAmount))
        {
            currentAmount.amount += amount;
            ResourcesCount[resource] = currentAmount;
        }
        else
        {
            ResourceAmount newResourcesCount = new ResourceAmount();
            newResourcesCount.amount = amount;
            ResourcesCount.Add(resource, newResourcesCount);
        }
        // The "?" checks if there are any subscribers before invoking the event
        OnInventoryChanged?.Invoke();
    }

    public bool RemoveResource(ResourceData resource, int amount)
    {
        if (ResourcesCount.TryGetValue(resource, out ResourceAmount currentAmount))
        {
            if(currentAmount.amount - amount >= 0)
            {
                currentAmount.amount = currentAmount.amount - amount;
                ResourcesCount[resource] = currentAmount;
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
        if (resource == null)
        {
            return 0;
        }

        if (ResourcesCount.TryGetValue(resource, out ResourceAmount currentAmount))
        {
            return currentAmount.amount;
        }
        else
        {
            return 0;
        }
    }
    public struct ResourceAmount
    {
        public int amount;
    }
}

