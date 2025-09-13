using UnityEngine;

public class Inventory : MonoBehaviour
{
    private static Inventory _Inventory;
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

    public int Rocks { get; private set;} 
    public int Wood { get; private set; }

    public void AddRocks(int amount)
    {
        Rocks += amount;
        Debug.Log($"Rock: {Rocks}");
    }
    public void AddWood(int amount)
    {
        Wood += amount;
        Debug.Log($"Wood: {Wood}");
    }
}
