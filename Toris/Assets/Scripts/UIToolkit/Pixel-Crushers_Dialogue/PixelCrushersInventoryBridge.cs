using System;
using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.UIToolkit;
using PixelCrushers.DialogueSystem;

/// <summary>
/// Registers Toris inventory and progression commands in the Pixel Crushers Dialogue System.
/// This allows dialogues, conditions, and quest scripts to read or modify inventory items and gold.
/// 
/// Example Dialogue Script usages:
///   - Condition: TorisHasItem("GoldOre", 5)
///   - User Script: TorisTakeItem("GoldOre", 5); TorisGiveGold(100)
/// </summary>
[DisallowMultipleComponent]
public class PixelCrushersInventoryBridge : MonoBehaviour
{
    private const string HasItemFuncName = "TorisHasItem";
    private const string GetItemCountFuncName = "TorisGetItemCount";
    private const string GiveItemFuncName = "TorisGiveItem";
    private const string TakeItemFuncName = "TorisTakeItem";
    
    private const string GetGoldFuncName = "TorisGetGold";
    private const string GiveGoldFuncName = "TorisGiveGold";
    private const string TakeGoldFuncName = "TorisTakeGold";

    [Header("Item Database Reference")]
    [Tooltip("The database containing all items, used to map string IDs from dialogue to actual Item ScriptableObjects.")]
    [SerializeField] private ItemDatabaseSO _itemDatabase;

    [Header("Debug Settings")]
    [Tooltip("Enable to log all Lua bridge calls and results in the Unity console.")]
    [SerializeField] private bool _debugBridge = true;

    private void OnEnable()
    {
        // Register Inventory Lua Functions
        Lua.RegisterFunction(HasItemFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisHasItem(string.Empty, 0D)));
        Lua.RegisterFunction(GetItemCountFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisGetItemCount(string.Empty)));
        Lua.RegisterFunction(GiveItemFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisGiveItem(string.Empty, 0D)));
        Lua.RegisterFunction(TakeItemFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisTakeItem(string.Empty, 0D)));

        // Register Currency/Gold Lua Functions
        Lua.RegisterFunction(GetGoldFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisGetGold()));
        Lua.RegisterFunction(GiveGoldFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisGiveGold(0D)));
        Lua.RegisterFunction(TakeGoldFuncName, this, SymbolExtensions.GetMethodInfo(() => TorisTakeGold(0D)));

        LogDebug("Registered all Toris Inventory and Gold Lua functions successfully.");
    }

    private void OnDisable()
    {
        // Unregister to prevent memory leaks or duplicate registrations
        Lua.UnregisterFunction(HasItemFuncName);
        Lua.UnregisterFunction(GetItemCountFuncName);
        Lua.UnregisterFunction(GiveItemFuncName);
        Lua.UnregisterFunction(TakeItemFuncName);

        Lua.UnregisterFunction(GetGoldFuncName);
        Lua.UnregisterFunction(GiveGoldFuncName);
        Lua.UnregisterFunction(TakeGoldFuncName);

        LogDebug("Unregistered all Toris Inventory and Gold Lua functions.");
    }

    #region Lua Inventory Interface

    /// <summary>
    /// Checks if the player has at least the specified quantity of an item in their backpack.
    /// Usage in Lua: TorisHasItem("Wood", 5)
    /// </summary>
    public bool TorisHasItem(string itemID, double quantity)
    {
        int requiredQty = Mathf.Max(0, Mathf.RoundToInt((float)quantity));
        if (requiredQty <= 0) return true;

        InventoryManager backpack = GetPlayerBackpack();
        if (backpack == null)
        {
            LogWarning($"TorisHasItem failed: Player Backpack inventory cannot be resolved.");
            return false;
        }

        InventoryItemSO itemSO = ResolveItemSO(itemID);
        if (itemSO == null)
        {
            LogWarning($"TorisHasItem failed: Item '{itemID}' could not be found in the database.");
            return false;
        }

        int count = GetTotalItemCountInBackpack(backpack, itemSO);
        bool hasEnough = count >= requiredQty;

        LogDebug($"TorisHasItem check: item='{itemID}', required={requiredQty}, available={count} -> Result={hasEnough}");
        return hasEnough;
    }

    /// <summary>
    /// Gets the total quantity of an item currently stored in the player's backpack.
    /// Usage in Lua: TorisGetItemCount("Wood")
    /// </summary>
    public double TorisGetItemCount(string itemID)
    {
        InventoryManager backpack = GetPlayerBackpack();
        if (backpack == null)
        {
            LogWarning($"TorisGetItemCount failed: Player Backpack inventory cannot be resolved.");
            return 0D;
        }

        InventoryItemSO itemSO = ResolveItemSO(itemID);
        if (itemSO == null)
        {
            LogWarning($"TorisGetItemCount failed: Item '{itemID}' could not be found in the database.");
            return 0D;
        }

        double count = GetTotalItemCountInBackpack(backpack, itemSO);
        LogDebug($"TorisGetItemCount: item='{itemID}', count={count}");
        return count;
    }

    /// <summary>
    /// Adds a quantity of items to the player's backpack.
    /// Usage in Lua: TorisGiveItem("HealthPotion", 2)
    /// </summary>
    public bool TorisGiveItem(string itemID, double quantity)
    {
        int amountToAdd = Mathf.Max(0, Mathf.RoundToInt((float)quantity));
        if (amountToAdd <= 0) return true;

        InventoryManager backpack = GetPlayerBackpack();
        if (backpack == null)
        {
            LogWarning($"TorisGiveItem failed: Player Backpack inventory cannot be resolved.");
            return false;
        }

        InventoryItemSO itemSO = ResolveItemSO(itemID);
        if (itemSO == null)
        {
            LogWarning($"TorisGiveItem failed: Item '{itemID}' could not be found in the database.");
            return false;
        }

        // Create a runtime instance of the item with fresh state
        ItemInstance freshInstance = new ItemInstance(itemSO);
        bool success = backpack.AddItem(freshInstance, amountToAdd);

        LogDebug($"TorisGiveItem transaction: item='{itemID}', amount={amountToAdd} -> Success={success}");
        return success;
    }

    /// <summary>
    /// Deducts a quantity of items from the player's backpack.
    /// Usage in Lua: TorisTakeItem("GoldOre", 3)
    /// </summary>
    public bool TorisTakeItem(string itemID, double quantity)
    {
        int amountToRemove = Mathf.Max(0, Mathf.RoundToInt((float)quantity));
        if (amountToRemove <= 0) return true;

        InventoryManager backpack = GetPlayerBackpack();
        if (backpack == null)
        {
            LogWarning($"TorisTakeItem failed: Player Backpack inventory cannot be resolved.");
            return false;
        }

        InventoryItemSO itemSO = ResolveItemSO(itemID);
        if (itemSO == null)
        {
            LogWarning($"TorisTakeItem failed: Item '{itemID}' could not be found in the database.");
            return false;
        }

        // Create a state-compatible instance stack to match with inside the InventoryManager
        ItemInstance searchInstance = new ItemInstance(itemSO);
        bool success = backpack.RemoveItem(searchInstance, amountToRemove);

        LogDebug($"TorisTakeItem transaction: item='{itemID}', amount={amountToRemove} -> Success={success}");
        return success;
    }

    #endregion

    #region Lua Gold Interface

    /// <summary>
    /// Gets the current amount of gold the player has.
    /// Usage in Lua: TorisGetGold()
    /// </summary>
    public double TorisGetGold()
    {
        PlayerProgression progression = GetPlayerProgression();
        if (progression == null)
        {
            LogWarning("TorisGetGold failed: PlayerProgression cannot be resolved.");
            return 0D;
        }

        double gold = progression.CurrentGold;
        LogDebug($"TorisGetGold: current gold={gold}");
        return gold;
    }

    /// <summary>
    /// Gives gold to the player.
    /// Usage in Lua: TorisGiveGold(150)
    /// </summary>
    public bool TorisGiveGold(double amount)
    {
        int goldToAdd = Mathf.Max(0, Mathf.RoundToInt((float)amount));
        if (goldToAdd <= 0) return true;

        PlayerProgression progression = GetPlayerProgression();
        if (progression == null)
        {
            LogWarning("TorisGiveGold failed: PlayerProgression cannot be resolved.");
            return false;
        }

        progression.AddGold(goldToAdd);
        LogDebug($"TorisGiveGold transaction: added={goldToAdd}, new total={progression.CurrentGold}");
        return true;
    }

    /// <summary>
    /// Spends or deducts gold from the player. Returns false if the player doesn't have enough gold.
    /// Usage in Lua: TorisTakeGold(50)
    /// </summary>
    public bool TorisTakeGold(double amount)
    {
        int goldToSpend = Mathf.Max(0, Mathf.RoundToInt((float)amount));
        if (goldToSpend <= 0) return true;

        PlayerProgression progression = GetPlayerProgression();
        if (progression == null)
        {
            LogWarning("TorisTakeGold failed: PlayerProgression cannot be resolved.");
            return false;
        }

        bool success = progression.TrySpendGold(goldToSpend);
        LogDebug($"TorisTakeGold transaction: spend={goldToSpend} -> Success={success}, new total={progression.CurrentGold}");
        return success;
    }

    #endregion

    #region Helper Internal Methods

    private InventoryManager GetPlayerBackpack()
    {
        GameSessionSO session = GameSessionSO.LoadDefault();
        if (session != null)
        {
            return session.PlayerInventory;
        }
        return null;
    }

    private PlayerProgression GetPlayerProgression()
    {
        GameSessionSO session = GameSessionSO.LoadDefault();
        if (session != null && session.ProgressionAnchor != null && session.ProgressionAnchor.IsReady)
        {
            return session.ProgressionAnchor.Instance;
        }
        return null;
    }

    private InventoryItemSO ResolveItemSO(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID)) return null;

        if (_itemDatabase == null)
        {
            LogWarning("ItemDatabase is not assigned on the bridge! Attempting to find standard database asset.");
            _itemDatabase = Resources.Load<ItemDatabaseSO>("GameData/Item Database SO");
            if (_itemDatabase == null)
            {
                _itemDatabase = Resources.Load<ItemDatabaseSO>("Data/ItemDatabase");
            }
            if (_itemDatabase == null)
            {
                LogWarning("Could not find standard ItemDatabase asset at Resources/GameData/Item Database SO.");
                return null;
            }
        }

        return _itemDatabase.GetItemByID(itemID);
    }

    private int GetTotalItemCountInBackpack(InventoryManager backpack, InventoryItemSO itemSO)
    {
        int total = 0;
        foreach (var slot in backpack.LiveSlots)
        {
            if (!slot.IsEmpty && slot.HeldItem.BaseItem == itemSO)
            {
                total += slot.Count;
            }
        }
        return total;
    }

    private void LogDebug(string message)
    {
        if (_debugBridge)
        {
            Debug.Log($"[PixelCrushersInventoryBridge] {message}", this);
        }
    }

    private void LogWarning(string message)
    {
        Debug.LogWarning($"[PixelCrushersInventoryBridge] {message}", this);
    }

    #endregion
}
