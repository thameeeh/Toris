using System;
using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;

public class PlayerEquipmentController : MonoBehaviour
{
    [Header("Dependencies")]
    [Tooltip("The InventoryManager configured to act as the equipment container (e.g. 5 slots).")]
    [SerializeField] private InventoryManager _equipmentInventory;
    [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
    [SerializeField] private GameSessionSO _globalSession;

    private readonly Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new();

    public event Action<EquipmentSlot, ItemInstance> OnItemEquipped;
    public event Action<EquipmentSlot, ItemInstance> OnItemUnequipped;
    public event Action<EquipmentSlot, ItemInstance> OnEquippedItemChanged;

    private void OnEnable()
    {
        if (_uiInventoryEvents != null)
        {
            _uiInventoryEvents.OnInventoryUpdated += RefreshEquipmentState;
            _uiInventoryEvents.OnItemClicked += HandleItemClicked;
        }

        RefreshEquipmentState();
    }

    private void OnDisable()
    {
        if (_uiInventoryEvents != null)
        {
            _uiInventoryEvents.OnInventoryUpdated -= RefreshEquipmentState;
            _uiInventoryEvents.OnItemClicked -= HandleItemClicked;
        }
    }

    private void Start()
    {
        RefreshEquipmentState();
    }



    private void HandleItemClicked(InventorySlot clickedSlot)
    {
        if (clickedSlot == null || clickedSlot.IsEmpty) return;
        if (_globalSession == null || _globalSession.PlayerInventory == null) return;
        if (_equipmentInventory == null || _equipmentInventory.LiveSlots == null) return;

        // Check if we are unequipping (clicked item is in equipment inventory)
        if (_equipmentInventory.LiveSlots.Contains(clickedSlot))
        {
            // Try to add to main inventory
            if (_globalSession.PlayerInventory.AddItem(clickedSlot.HeldItem, clickedSlot.Count))
            {
                clickedSlot.Clear();
                _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
            }
            return;
        }

        // Check if we are equipping (clicked item is in main inventory)
        if (_globalSession.PlayerInventory.LiveSlots.Contains(clickedSlot))
        {
            EquipableComponent equipable = clickedSlot.HeldItem.BaseItem.GetComponent<EquipableComponent>();
            if (equipable != null)
            {
                int targetIndex = (int)equipable.TargetSlot;
                if (targetIndex >= 0 && targetIndex < _equipmentInventory.LiveSlots.Count)
                {
                    InventorySlot eqSlot = _equipmentInventory.LiveSlots[targetIndex];

                    if (eqSlot.IsEmpty)
                    {
                        eqSlot.SetItem(clickedSlot.HeldItem, clickedSlot.Count);
                        clickedSlot.Clear();
                    }
                    else
                    {
                        // Swap
                        ItemInstance tempItem = eqSlot.HeldItem;
                        int tempCount = eqSlot.Count;

                        eqSlot.SetItem(clickedSlot.HeldItem, clickedSlot.Count);
                        clickedSlot.SetItem(tempItem, tempCount);
                    }

                    _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
                }
            }
        }
    }

    public void RefreshEquipmentState()
    {
        if (_equipmentInventory == null || _equipmentInventory.LiveSlots == null)
            return;

        // Hardcode mapping from InventorySlot index to EquipmentSlot enum
        // Index 0 = Head, 1 = Chest, 2 = Legs, 3 = Arms, 4 = Weapon
        ProcessSlot(0, EquipmentSlot.Head);
        ProcessSlot(1, EquipmentSlot.Chest);
        ProcessSlot(2, EquipmentSlot.Legs);
        ProcessSlot(3, EquipmentSlot.Arms);
        ProcessSlot(4, EquipmentSlot.Weapon);
    }

    private void ProcessSlot(int index, EquipmentSlot slotType)
    {
        if (index >= _equipmentInventory.LiveSlots.Count)
            return;

        InventorySlot slotData = _equipmentInventory.LiveSlots[index];
        ItemInstance currentItemInSlot = slotData.IsEmpty ? null : slotData.HeldItem;

        _equippedItems.TryGetValue(slotType, out ItemInstance previouslyEquippedItem);

        // If the item has changed or became empty
        if (currentItemInSlot != previouslyEquippedItem)
        {
            if (previouslyEquippedItem != null)
            {
                OnItemUnequipped?.Invoke(slotType, previouslyEquippedItem);
                _equippedItems.Remove(slotType);
            }

            if (currentItemInSlot != null)
            {
                _equippedItems[slotType] = currentItemInSlot;
                OnItemEquipped?.Invoke(slotType, currentItemInSlot);
            }
            else
            {
                _equippedItems.Remove(slotType);
            }

            OnEquippedItemChanged?.Invoke(slotType, currentItemInSlot);
        }
    }

    public ItemInstance GetEquippedItem(EquipmentSlot slot)
    {
        _equippedItems.TryGetValue(slot, out ItemInstance item);
        return item;
    }

    public bool IsSlotOccupied(EquipmentSlot slot)
    {
        return _equippedItems.ContainsKey(slot);
    }

    public bool TryGetEquippedItem(EquipmentSlot slot, out ItemInstance item)
    {
        return _equippedItems.TryGetValue(slot, out item);
    }

    public IReadOnlyDictionary<EquipmentSlot, ItemInstance> GetAllEquippedItems()
    {
        return _equippedItems;
    }
}