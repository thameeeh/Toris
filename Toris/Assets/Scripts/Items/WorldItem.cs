using OutlandHaven.UIToolkit;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class WorldItem : MonoBehaviour, IContainerInteractable
{
    [Header("Data")]
    [SerializeField] private InventoryItemSO _itemData;
    [SerializeField] private int _quantity = 1;

    [Header("Visuals")]
    private SpriteRenderer _renderer;
    private void Awake()
    {
        _renderer = GetComponent<SpriteRenderer>();
    }
    private void Start()
    {
        // Auto-configure visuals to match the data
        if (_itemData != null)
        {
            _renderer.sprite = _itemData.Icon;
            name = $"WorldItem_{_itemData.ItemName}";
        }
    }

    void OnValidate()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("<color=red>WorldItem</color> has no item data assigned!", this);
        }
    }

    public bool Interact(InventoryContainerSO targetContainer)
    {
        if (targetContainer == null) return false;

        // Attempt to add the item to the container passed in
        bool success = targetContainer.AddItem(_itemData, _quantity);

        if (success)
        {
            // Visual feedback, sound effects go here
            Destroy(gameObject);
            return true;
        }
        else
        {
            Debug.Log("Inventory is full!");
            return false;
        }
    }

    public string GetInteractionPrompt()
    {
        return $"Pick up {_itemData.ItemName} (x{_quantity})";
    }
}
