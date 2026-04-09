using System;
using UnityEngine;
using OutlandHaven.Inventory;
using System.Collections.Generic;

[RequireComponent(typeof(PlayerEquipmentController))]
public class EquipmentEffectBridge : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentController _equipment;
    [SerializeField] private PlayerEffectSourceController _effectSourceController;

    private readonly Dictionary<EquipmentSlot, ItemInstance> _subscribedItems = new();

    private void Reset()
    {
        if (_equipment == null)
            _equipment = GetComponent<PlayerEquipmentController>();
    }

    private void Awake()
    {
        if (_equipment == null)
            _equipment = GetComponent<PlayerEquipmentController>();
    }

    private void OnEnable()
    {
        if (_equipment != null)
            _equipment.OnEquippedItemChanged += HandleEquippedItemChanged;
    }

    private void OnDisable()
    {
        if (_equipment != null)
            _equipment.OnEquippedItemChanged -= HandleEquippedItemChanged;

        foreach (var pair in _subscribedItems)
        {
            if (pair.Value != null)
                pair.Value.OnStateChanged -= HandleEquippedItemStateChanged;
        }

        _subscribedItems.Clear();
    }

    private void Start()
    {
        RefreshAll();
    }

    private void HandleEquippedItemChanged(EquipmentSlot slot, ItemInstance item)
    {
        UnsubscribeFromSlot(slot);

        if (_effectSourceController == null)
            return;

        string sourceKey = GetSourceKey(slot);

        if (item == null)
        {
            _effectSourceController.RemoveSource(sourceKey);
            return;
        }

        _effectSourceController.SetSource(new EquippedItemEffectSource(sourceKey, item));
        SubscribeToSlot(slot, item);
    }

    public void RefreshSlot(EquipmentSlot slot)
    {
        if (_equipment == null)
            return;

        ItemInstance item = _equipment.GetEquippedItem(slot);
        HandleEquippedItemChanged(slot, item);
    }

    public void RefreshAll()
    {
        foreach (EquipmentSlot slot in Enum.GetValues(typeof(EquipmentSlot)))
        {
            RefreshSlot(slot);
        }
    }

    private string GetSourceKey(EquipmentSlot slot)
    {
        return $"Equipment_{slot}";
    }

    private void SubscribeToSlot(EquipmentSlot slot, ItemInstance item)
    {
        if (item == null)
            return;

        item.OnStateChanged += HandleEquippedItemStateChanged;
        _subscribedItems[slot] = item;
    }
    private void UnsubscribeFromSlot(EquipmentSlot slot)
    {
        if (_subscribedItems.TryGetValue(slot, out ItemInstance item) && item != null)
        {
            item.OnStateChanged -= HandleEquippedItemStateChanged;
        }

        _subscribedItems.Remove(slot);
    }

    private void HandleEquippedItemStateChanged(ItemInstance changedItem)
    {
        if (changedItem == null) 
            return;
    
        foreach(var pair in _subscribedItems)
        {
            if (ReferenceEquals(pair.Value, changedItem))
            {
                RefreshSlot(pair.Key);
                break;
            }
        }
    }
}
