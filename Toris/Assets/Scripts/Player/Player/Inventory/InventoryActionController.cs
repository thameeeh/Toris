using System;
using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;

public class InventoryActionController : MonoBehaviour
{
    private const string DefaultEquipSfxId = "ui_equip_armor";

    [Header("Runtime References")]
    private InventoryManager _playerInventory;
    private InventoryManager _equipmentInventory;
    private InventoryManager _potionInventory;
    [SerializeField] private PlayerInputReaderSO _inputReader;
    [SerializeField] private UIInventoryEventsSO _uiInventoryEvents;
    [SerializeField] private PlayerStats _playerStats;
    [SerializeField] private PlayerStatsAnchorSO _playerStatsAnchor;
    [SerializeField] private PlayerEffectSourceController _playerEffectSourceController;
    [SerializeField] private GameSessionSO _gameSession;

    [Header("SFX")]
    [SerializeField] private string equipSfxId = DefaultEquipSfxId;

    private PlayerConsumableController _consumableController;

    public event Action<PlayerConsumableUseContext> ConsumableUsed;

    private void Awake()
    {
        _gameSession = GameSessionSO.LoadDefault();
        ResolveRuntimeReferences();
        EnsureConsumableController();
    }

    private void Update()
    {
        _consumableController?.Tick();
    }

    private void OnEnable()
    {
        _gameSession = GameSessionSO.LoadDefault();
        ResolveRuntimeReferences();

        if (_uiInventoryEvents == null)
            return;

        _uiInventoryEvents.OnRequestEquip += HandleRequestEquip;
        _uiInventoryEvents.OnRequestUse += HandleRequestUse;
        _uiInventoryEvents.OnRequestUnequip += HandleRequestUnequip;

        if (_inputReader != null)
        {
            _inputReader.OnPotion1Pressed += HandlePotion1Pressed;
            _inputReader.OnPotion2Pressed += HandlePotion2Pressed;
        }
    }

    private void OnDisable()
    {
        if (_uiInventoryEvents == null)
            return;

        _uiInventoryEvents.OnRequestEquip -= HandleRequestEquip;
        _uiInventoryEvents.OnRequestUse -= HandleRequestUse;
        _uiInventoryEvents.OnRequestUnequip -= HandleRequestUnequip;

        if (_inputReader != null)
        {
            _inputReader.OnPotion1Pressed -= HandlePotion1Pressed;
            _inputReader.OnPotion2Pressed -= HandlePotion2Pressed;
        }
    }

    private void HandlePotion1Pressed()
    {
#if UNITY_EDITOR
        Debug.Log("[InventoryActionController] Potion 1 hotkey pressed.");
#endif
        ResolveRuntimeReferences();
        TryConsumePotionSlot(0);
    }

    private void HandlePotion2Pressed()
    {
#if UNITY_EDITOR
        Debug.Log("[InventoryActionController] Potion 2 hotkey pressed.");
#endif
        ResolveRuntimeReferences();
        TryConsumePotionSlot(1);
    }

    private void TryConsumePotionSlot(int slotIndex)
    {
        if (_potionInventory == null)
        {
            Debug.LogWarning($"[InventoryActionController] <b>_potionInventory</b> is null! Check if the Potion Bar's <b>InventoryContainerSO</b> has <b>AssociatedView</b> set to <b>ScreenType.Potions</b> and that the object is active in the scene.", this);
            return;
        }

        if (slotIndex < 0 || slotIndex >= _potionInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] slotIndex {slotIndex} is out of range. LiveSlots count: {_potionInventory.LiveSlots.Count}");
            return;
        }

        InventorySlot slot = _potionInventory.LiveSlots[slotIndex];
        
        if (slot == null || slot.IsEmpty)
        {
#if UNITY_EDITOR
            Debug.Log("[InventoryActionController] Potion slot is null or empty.");
#endif
            return;
        }

#if UNITY_EDITOR
        Debug.Log($"[InventoryActionController] Attempting to consume potion in slot {slotIndex}. Item: {slot.HeldItem.BaseItem.name}");
#endif
        HandleRequestUse(slot);
    }

    private void OnDestroy()
    {
        if (_consumableController != null)
        {
            _consumableController.ConsumableUsed -= HandleConsumableUsed;
        }
    }

    private void HandleRequestEquip(InventorySlot slot)
    {
        ResolveRuntimeReferences();
        TryEquipFromInventorySlot(slot);
    }

    private void HandleRequestUse(InventorySlot slot)
    {
        ResolveRuntimeReferences();
        if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null)
            return;

        if (!CanUse(slot))
            return;

        EnsureConsumableController();

        // Pass control directly to the interface
        slot.HeldItem.BaseItem.UsableBehavior.TryUse(_consumableController, _playerInventory, slot);
    }

    private void HandleRequestUnequip(EquipmentSlot slot)
    {
        ResolveRuntimeReferences();
        TryUnequip(slot);
    }

    public bool TryEquipFromInventorySlot(int slotIndex)
    {
        if (_playerInventory == null || _equipmentInventory == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing player inventory or equipment inventory reference.");
            return false;
        }

        if (slotIndex < 0 || slotIndex >= _playerInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Slot index {slotIndex} is out of range.");
            return false;
        }

        return TryEquipFromInventorySlot(_playerInventory.LiveSlots[slotIndex]);
    }

    public bool TryEquipFromInventorySlot(InventorySlot sourceSlot)
    {
        if (_playerInventory == null || _equipmentInventory == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing player inventory or equipment inventory reference.");
            return false;
        }

        if (sourceSlot == null || sourceSlot.IsEmpty || sourceSlot.HeldItem == null || sourceSlot.HeldItem.BaseItem == null)
        {
            Debug.LogWarning("[InventoryActionController] Cannot equip from a null or empty slot.");
            return false;
        }

        IEquipable equipable = sourceSlot.HeldItem.BaseItem.EquipableBehavior;
        if (equipable == null)
        {
            Debug.LogWarning("[InventoryActionController] Item is not equippable.");
            return false;
        }

        int equipmentIndex = (int)equipable.TargetSlot;
        if (equipmentIndex < 0 || equipmentIndex >= _equipmentInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Equipment slot index {equipmentIndex} is out of range.");
            return false;
        }

        InventorySlot equipmentSlot = _equipmentInventory.LiveSlots[equipmentIndex];

        if (!equipmentSlot.IsEmpty && ReferenceEquals(equipmentSlot.HeldItem, sourceSlot.HeldItem))
            return true;

        if (equipmentSlot.IsEmpty)
        {
#if UNITY_EDITOR
            Debug.Log($"[InventoryActionController] <b>EQUIP</b>: Moving '{sourceSlot.HeldItem.BaseItem.ItemName}' from <b>{_playerInventory.name}</b> (SaveID: {_playerInventory.SaveID}) to <b>{_equipmentInventory.name}</b> (Slot: {equipable.TargetSlot})");
#endif
            equipmentSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
            sourceSlot.Clear();
        }
        else
        {
            ItemInstance tempItem = equipmentSlot.HeldItem;
            int tempCount = equipmentSlot.Count;

#if UNITY_EDITOR
            Debug.Log($"[InventoryActionController] <b>SWAP-EQUIP</b>: Swapping '{sourceSlot.HeldItem.BaseItem.ItemName}' from <b>{_playerInventory.name}</b> with '{tempItem.BaseItem.ItemName}' in <b>{_equipmentInventory.name}</b>");
#endif

            equipmentSlot.SetItem(sourceSlot.HeldItem, sourceSlot.Count);
            sourceSlot.SetItem(tempItem, tempCount);
        }

        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
        PlayEquipSfx();
        return true;
    }

    public bool TryUnequip(EquipmentSlot equipmentSlotType)
    {
        if (_playerInventory == null || _equipmentInventory == null)
        {
            Debug.LogWarning("[InventoryActionController] Missing player inventory or equipment inventory reference.");
            return false;
        }

        int equipmentIndex = (int)equipmentSlotType;
        if (equipmentIndex < 0 || equipmentIndex >= _equipmentInventory.LiveSlots.Count)
        {
            Debug.LogWarning($"[InventoryActionController] Equipment slot index {equipmentIndex} is out of range.");
            return false;
        }

        InventorySlot equipmentSlot = _equipmentInventory.LiveSlots[equipmentIndex];
        if (equipmentSlot == null || equipmentSlot.IsEmpty || equipmentSlot.HeldItem == null)
        {
            Debug.LogWarning($"[InventoryActionController] Equipment slot {equipmentSlotType} is empty.");
            return false;
        }

        string itemName = equipmentSlot.HeldItem.BaseItem.ItemName;
        bool addedBack = _playerInventory.AddItem(equipmentSlot.HeldItem, equipmentSlot.Count);
        if (!addedBack)
        {
            Debug.LogWarning($"[InventoryActionController] Could not unequip '{itemName}' because the <b>{_playerInventory.name}</b> (SaveID: {_playerInventory.SaveID}) has no space.");
            return false;
        }

#if UNITY_EDITOR
        Debug.Log($"[InventoryActionController] <b>UNEQUIP</b>: Moving '{itemName}' from <b>{_equipmentInventory.name}</b> back to <b>{_playerInventory.name}</b> (SaveID: {_playerInventory.SaveID})");
#endif

        equipmentSlot.Clear();
        _uiInventoryEvents?.OnInventoryUpdated?.Invoke();
        PlayEquipSfx();
        return true;
    }

    private void PlayEquipSfx()
    {
        // SFX-only hook: called after equip or unequip inventory moves have succeeded.
        // It must not affect inventory, equipment slots, stats, or UI refresh behavior.
        if (AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(equipSfxId))
            return;

        SfxPlayRequest request = SfxPlayRequest.Default;
        request.force2D = true;
        AudioBootstrap.Sfx.Play(equipSfxId, request);
    }

    public bool CanEquip(InventorySlot slot)
    {
        return slot != null &&
               !slot.IsEmpty &&
               slot.HeldItem?.BaseItem != null &&
               slot.HeldItem.BaseItem.EquipableBehavior != null;
    }

    public bool CanUse(InventorySlot slot)
    {
        return slot != null &&
               !slot.IsEmpty &&
               slot.HeldItem?.BaseItem != null &&
               slot.HeldItem.BaseItem.UsableBehavior != null;
    }

    private void EnsureConsumableController()
    {
        if (_consumableController == null)
        {
            _consumableController = new PlayerConsumableController(
                _uiInventoryEvents,
                _playerStatsAnchor,
                _playerStats,
                _playerEffectSourceController);
            _consumableController.ConsumableUsed += HandleConsumableUsed;
            return;
        }

        _consumableController.Rebind(
            _playerStatsAnchor,
            _playerStats,
            _playerEffectSourceController);
    }

    private void HandleConsumableUsed(PlayerConsumableUseContext context)
    {
        ConsumableUsed?.Invoke(context);
    }

    private void ResolveRuntimeReferences()
    {
        if (_gameSession == null) _gameSession = GameSessionSO.LoadDefault();

        _playerInventory = _gameSession.PlayerInventory;
        _equipmentInventory = _gameSession.PlayerEquipment;
        _potionInventory = _gameSession.PlayerPotionInventory;

        if (_playerStats == null)
            TryGetComponent(out _playerStats);

        if (_playerEffectSourceController == null)
            TryGetComponent(out _playerEffectSourceController);

        _playerStatsAnchor = PlayerInventorySceneResolver.ResolvePlayerStatsAnchor(_playerStatsAnchor);
        EnsureConsumableController();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (_inputReader == null)
            Debug.LogWarning($"[UI/Inventory] <b><color=yellow>InventoryActionController</color></b>: Missing <b>Player Input Reader SO</b> reference on {name}.", this);
        if (_uiInventoryEvents == null)
            Debug.LogWarning($"[UI/Inventory] <b><color=yellow>InventoryActionController</color></b>: Missing <b>UI Inventory Events SO</b> reference on {name}.", this);
    }
#endif
}
