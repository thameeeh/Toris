using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    private static Inventory _Inventory;

    //for resources like wood, stone, flowers
    private Dictionary<ResourceData, int> ResourcesCount = new Dictionary<ResourceData, int>();
    //for stats like kills, coins collected
    private Dictionary<ResourceData, int> ResourcesStats = new Dictionary<ResourceData, int>();

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
        return new Dictionary<ResourceData, int>(ResourcesCount);
    }

    public void AddResource(ResourceData resource, int amount)
    {

        AddToDictionary(ResourcesCount, resource, amount);
    }
    public void AddResourceStat(ResourceData resource, int amount)
    {
        AddToDictionary(ResourcesStats, resource, amount);
    }

    private void AddToDictionary(in Dictionary<ResourceData, int> dic, ResourceData resource, int amount) 
    {
        //TryGetValue returns bool, 'out' returns value of coresponding key
        if (dic.TryGetValue(resource, out int currentAmount))
        {
            dic[resource] = currentAmount + amount;
            Debug.Log($"{resource.name}: added {amount}, new amount: {dic[resource]}");
        }
        else
        {
            dic.Add(resource, amount);
            Debug.Log($"{resource.name}: added {amount}, new amount: {dic[resource]}");
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
        if (resource == null)
        {
            return 0;
        }

        if (ResourcesCount.TryGetValue(resource, out int currentAmount))
        {
            return currentAmount;
        }
        else if (ResourcesStats.TryGetValue(resource, out int currentAmount1))
        {
            return currentAmount1;
        }
        else
        {
            return 0;
        }
    }
}
