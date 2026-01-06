using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    void Start()
    {
        Debug.Log(ItemDatabase.Instance.itemLookup[1]);
    }
}
