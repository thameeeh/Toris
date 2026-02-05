using OutlandHaven.UIToolkit;
using System.Runtime.CompilerServices;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class WorldItem : MonoBehaviour, IInteractable
{
    [Header("Data")]
    [SerializeField] private InventoryItemSO _itemData;
    [SerializeField] private int _quantity = 1;

    [Header("Visuals")]
    private SpriteRenderer _renderer;

    private void Start()
    {
        // Auto-configure visuals to match the data
        if (_itemData != null)
        {
            _renderer.sprite = _itemData.Icon;
            name = $"WorldItem_{_itemData.ItemName}";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    void OnValidate()
    {
        if (_itemData == null)
        {
            Debug.LogWarning("<color=red>WorldItem</color> has no item data assigned!", this);
        }
    }

    public string GetInteractionPrompt()
    {
        throw new System.NotImplementedException();
    }

    public bool Interact(InventoryContainerSO targetContainer)
    {
        throw new System.NotImplementedException();
    }
}
