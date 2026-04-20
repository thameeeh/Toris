using OutlandHaven.UIToolkit;
using System;
using UnityEngine;

namespace OutlandHaven.Inventory
{

    [RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
    public class WorldItem : MonoBehaviour, IContainerInteractable
    {
        [Header("Data")]
        [SerializeField] private InventoryItemSO _itemData;
        [SerializeField] private int _quantity = 1;

        public Vector3 InteractionPosition => transform.position + Vector3.up * 1.0f;

        [Header("Visuals")]
        private SpriteRenderer _renderer;
        private Collider2D _collider;

        private void Awake()
        {
            _renderer = GetComponent<SpriteRenderer>();
            _collider = GetComponent<Collider2D>();
            _collider.isTrigger = true;
            ApplyVisuals();
        }

        public void Initialize(InventoryItemSO itemData, int quantity)
        {
            _itemData = itemData;
            _quantity = Mathf.Max(1, quantity);
            ApplyVisuals();
        }

        void OnValidate()
        {
            if (Application.isPlaying)
                return;

            if (_itemData == null)
            {
                Debug.LogWarning("<color=red>WorldItem</color> has no item data assigned!", this);
            }
        }

        public bool Interact(InventoryManager targetContainer)
        {
            if (targetContainer == null) return false;
            if (_itemData == null) return false;

            ItemInstance item = new ItemInstance(_itemData);
            // Attempt to add the item to the container passed in
            bool success = targetContainer.AddItem(item, _quantity);

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
            return "E";
        }

        private void ApplyVisuals()
        {
            if (_renderer == null || _itemData == null)
                return;

            _renderer.sprite = _itemData.Icon;
            name = $"WorldItem_{_itemData.ItemName}";
        }
    }

}
