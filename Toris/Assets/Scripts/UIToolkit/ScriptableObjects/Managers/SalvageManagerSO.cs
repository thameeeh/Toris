using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.UIToolkit
{
    public enum SalvageType
    {
        Gold,
        Material
    }

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/SalvageManagerSO")]
    public class SalvageManagerSO : ScriptableObject
    {
        private const string DefaultSalvageSuccessSfxId = "craft_salvage";

        [Header("Dependencies")]
        public GameSessionSO SessionData;
        public PlayerProgressionAnchorSO PlayerAnchor;
        public UIInventoryEventsSO InventoryEvents;
        public CraftingRegistrySO Registry;

        [Header("SFX")]
        [SerializeField] private string salvageSuccessSfxId = DefaultSalvageSuccessSfxId;

        public void Initialize()
        {
            Cleanup();
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestSalvage += HandleRequestSalvage;
            }
        }

        public void Cleanup()
        {
            if (InventoryEvents != null)
            {
                InventoryEvents.OnRequestSalvage -= HandleRequestSalvage;
            }
        }

        public bool CanSalvage(InventoryItemSO itemType)
        {
            if (itemType == null) return false;
            if (SessionData == null || SessionData.PlayerInventory == null) return false;
            if (Registry == null) return false;

            SalvageRecipeSO recipe = Registry.GetSalvageRecipeFor(itemType);
            if (recipe == null) return false;

            int totalItems = 0;
            foreach (var slot in SessionData.PlayerInventory.LiveSlots)
            {
                if (!slot.IsEmpty && slot.HeldItem.IsStackableWith(new ItemInstance(itemType)))
                    totalItems += slot.Count;
            }

            return totalItems > 0;
        }

        private void HandleRequestSalvage(InventorySlot slot, SalvageType salvageType)
        {
            if (slot == null || slot.IsEmpty) return;
            if (SessionData == null || SessionData.PlayerInventory == null) return;
            if (PlayerAnchor == null || !PlayerAnchor.IsReady) return;
            if (Registry == null) return;

            // Cache item type for safety
            InventoryItemSO itemType = slot.HeldItem.BaseItem;

            SalvageRecipeSO recipe = Registry.GetSalvageRecipeFor(itemType);
            if (recipe == null)
            {
#if UNITY_EDITOR
                Debug.LogWarning($"Salvage failed: No recipe found for {itemType.ItemName}.");
#endif
                return;
            }

            // Verify if player wants material but the recipe has no material yield
            if (salvageType == SalvageType.Material
                && (recipe.MaterialYields == null || recipe.MaterialYields.Count == 0))
            {
#if UNITY_EDITOR
                Debug.LogWarning("Salvage failed: Recipe yields no material.");
#endif
                return;
            }

            if (!IsPlayerInventorySlot(slot))
            {
#if UNITY_EDITOR
                Debug.LogWarning("Salvage failed: selected slot does not belong to the player inventory.");
#endif
                return;
            }

            // Keep salvage transactional: material rewards must fit after the selected
            // item is consumed, otherwise the source item is left untouched.
            if (salvageType == SalvageType.Material
                && !InventoryCapacityPreview.CanAddRewardsAfterConsumingSlot(SessionData.PlayerInventory, slot, recipe.MaterialYields))
            {
#if UNITY_EDITOR
                Debug.LogWarning("Salvage failed: not enough inventory space for material rewards.");
#endif
                return;
            }

            slot.DecreaseCount(1);

            if (salvageType == SalvageType.Gold)
            {
                if (recipe.GoldYield > 0)
                {
                    PlayerAnchor.Instance.AddGold(recipe.GoldYield);
                }
#if UNITY_EDITOR
                Debug.Log($"Salvaged {itemType.ItemName} for {recipe.GoldYield} gold.");
#endif
            }
            else if (salvageType == SalvageType.Material)
            {
                // Give material rewards
                foreach (var yield in recipe.MaterialYields)
                {
                    bool added = SessionData.PlayerInventory.AddItem(new ItemInstance(yield.Material), yield.Quantity);
                    if (!added)
                    {
#if UNITY_EDITOR
                        Debug.LogWarning($"Salvage failed to yield {yield.Material.ItemName}: Inventory full.");
#endif
                        // If we fail here, we could refund the salvaged item or handle the overflow, but we'll log it for now
                    }
                }
#if UNITY_EDITOR
                Debug.Log($"Salvaged {itemType.ItemName} for materials.");
#endif
            }

            InventoryEvents?.OnInventoryUpdated?.Invoke();
            PlaySalvageSuccessSfx();
        }

        private void PlaySalvageSuccessSfx()
        {
            // SFX-only hook: called after salvage removes the source item and grants rewards.
            // It must not affect salvage recipes, inventory mutation, gold, or UI refresh behavior.
            if (AudioBootstrap.Sfx == null || string.IsNullOrWhiteSpace(salvageSuccessSfxId))
                return;

            SfxPlayRequest request = SfxPlayRequest.Default;
            request.force2D = true;
            AudioBootstrap.Sfx.Play(salvageSuccessSfxId, request);
        }

        private bool IsPlayerInventorySlot(InventorySlot slot)
        {
            // UI sends a slot as an intent; this manager verifies it belongs to the
            // authoritative player inventory before mutating any item data.
            if (slot == null || SessionData?.PlayerInventory?.LiveSlots == null)
                return false;

            return SessionData.PlayerInventory.LiveSlots.Contains(slot);
        }
    }
}
