using System;
using UnityEngine;
using OutlandHaven.Inventory;

[RequireComponent(typeof(PlayerEquipmentController))]
public class EquipmentEffectBridge : MonoBehaviour
{
    [SerializeField] private PlayerEquipmentController _equipment;
    [SerializeField] private PlayerEffectSourceController _effectSourceController;

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
    }

    private void Start()
    {
        RefreshAll();
    }

    private void HandleEquippedItemChanged(EquipmentSlot slot, ItemInstance item)
    {
        if (_effectSourceController == null)
            return;

        string sourceKey = GetSourceKey(slot);

        if (item == null)
        {
            _effectSourceController.RemoveSource(sourceKey);
            return;
        }

        _effectSourceController.SetSource(new EquippedItemEffectSource(sourceKey, item));
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
}