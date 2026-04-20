using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    /// <summary>
    /// Runtime owner for consumable usage rules, cooldowns, and slot mutation.
    /// </summary>
    public sealed class PlayerConsumableController
    {
        private const string TIMED_CONSUMABLE_SOURCE_PREFIX = "ConsumableTimed_";

        private readonly UIInventoryEventsSO _uiInventoryEvents;
        private readonly PlayerStatsAnchorSO _playerStatsAnchor;
        private readonly PlayerStats _playerStatsFallback;
        private readonly PlayerEffectSourceController _playerEffectSourceController;
        private readonly Dictionary<InventoryItemSO, float> _nextUseByItem = new();
        private readonly Dictionary<string, float> _timedConsumableExpirations = new();
        private readonly List<string> _expiredTimedConsumableKeys = new();

        public PlayerConsumableController(
            UIInventoryEventsSO uiInventoryEvents,
            PlayerStatsAnchorSO playerStatsAnchor,
            PlayerStats playerStatsFallback,
            PlayerEffectSourceController playerEffectSourceController)
        {
            _uiInventoryEvents = uiInventoryEvents;
            _playerStatsAnchor = playerStatsAnchor;
            _playerStatsFallback = playerStatsFallback;
            _playerEffectSourceController = playerEffectSourceController;
        }

        public void Tick()
        {
            if (_playerEffectSourceController == null || _timedConsumableExpirations.Count == 0)
                return;

            float currentTime = Time.time;
            _expiredTimedConsumableKeys.Clear();

            foreach (KeyValuePair<string, float> pair in _timedConsumableExpirations)
            {
                if (pair.Value <= currentTime)
                {
                    _expiredTimedConsumableKeys.Add(pair.Key);
                }
            }

            for (int i = 0; i < _expiredTimedConsumableKeys.Count; i++)
            {
                string sourceKey = _expiredTimedConsumableKeys[i];
                _playerEffectSourceController.RemoveSource(sourceKey);
                _timedConsumableExpirations.Remove(sourceKey);
            }
        }

        public bool TryUseConsumable(InventorySlot slot)
        {
            if (!TryResolveConsumable(slot, out ItemInstance item, out ConsumableComponent consumable, out ConsumableState state))
                return false;

            if (state != null && state.CurrentCharges <= 0)
            {
                Debug.LogWarning($"[PlayerConsumableController] Consumable '{item.BaseItem.ItemName}' has invalid CurrentCharges={state.CurrentCharges}.");
                return false;
            }

            if (state != null && state.CurrentCharges > 1 && slot.Count > 1)
            {
                Debug.LogWarning($"[PlayerConsumableController] Refusing to use stacked charged consumable '{item.BaseItem.ItemName}'. Multi-charge consumables should not stack.");
                return false;
            }

            if (IsOnCooldown(item.BaseItem))
            {
                return false;
            }

            if (!TryApplyEffect(item, consumable))
            {
                return false;
            }

            ConsumeUse(slot, state);

            if (consumable.CooldownDuration > 0f)
                _nextUseByItem[item.BaseItem] = Time.time + consumable.CooldownDuration;

            _uiInventoryEvents?.OnSpecificSlotsUpdated?.Invoke(slot, null);
            return true;
        }

        private bool TryResolveConsumable(
            InventorySlot slot,
            out ItemInstance item,
            out ConsumableComponent consumable,
            out ConsumableState state)
        {
            item = null;
            consumable = null;
            state = null;

            if (slot == null || slot.IsEmpty || slot.HeldItem?.BaseItem == null)
                return false;

            item = slot.HeldItem;
            consumable = item.BaseItem.GetComponent<ConsumableComponent>();
            if (consumable == null)
                return false;

            state = item.GetState<ConsumableState>();
            return true;
        }

        private PlayerStats ResolvePlayerStats()
        {
            if (_playerStatsAnchor != null && _playerStatsAnchor.IsReady)
                return _playerStatsAnchor.Instance;

            return _playerStatsFallback;
        }

        private bool IsOnCooldown(InventoryItemSO item)
        {
            if (item == null)
                return false;

            if (!_nextUseByItem.TryGetValue(item, out float nextAllowedTime))
                return false;

            return nextAllowedTime > Time.time;
        }

        private bool TryApplyEffect(ItemInstance item, ConsumableComponent consumable)
        {
            switch (consumable.EffectMode)
            {
                case ConsumableEffectMode.InstantResource:
                    PlayerStats playerStats = ResolvePlayerStats();
                    if (playerStats == null)
                    {
                        Debug.LogWarning("[PlayerConsumableController] Cannot use instant consumable because PlayerStats could not be resolved.");
                        return false;
                    }

                    return ApplyInstantEffect(consumable, playerStats, item);
                case ConsumableEffectMode.TimedPlayerEffect:
                    return ApplyTimedEffect(item, consumable);
                default:
                    Debug.LogWarning($"[PlayerConsumableController] Consumable '{item?.BaseItem?.ItemName}' uses unsupported effect mode '{consumable.EffectMode}'.");
                    return false;
            }
        }

        private static bool ApplyInstantEffect(ConsumableComponent consumable, PlayerStats playerStats, ItemInstance item)
        {
            switch (consumable.EffectPayload)
            {
                case ConsumptionSlot.HP:
                    playerStats.RestoreHealth(consumable.amount);
                    return true;
                case ConsumptionSlot.Mana:
                    playerStats.RestoreStamina(consumable.amount);
                    return true;
                default:
                    Debug.LogWarning($"[PlayerConsumableController] Consumable '{item?.BaseItem?.ItemName}' uses unsupported payload '{consumable.EffectPayload}'.");
                    return false;
            }
        }

        private bool ApplyTimedEffect(ItemInstance item, ConsumableComponent consumable)
        {
            if (_playerEffectSourceController == null)
            {
                Debug.LogWarning("[PlayerConsumableController] Cannot use timed consumable because PlayerEffectSourceController could not be resolved.");
                return false;
            }

            if (consumable.TimedEffectDefinition == null)
            {
                Debug.LogWarning($"[PlayerConsumableController] Timed consumable '{item?.BaseItem?.ItemName}' is missing a PlayerEffectDefinitionSO.");
                return false;
            }

            if (consumable.TimedEffectDuration <= 0f)
            {
                Debug.LogWarning($"[PlayerConsumableController] Timed consumable '{item?.BaseItem?.ItemName}' must have a duration greater than 0.");
                return false;
            }

            string sourceKey = BuildTimedConsumableSourceKey(item);
            _playerEffectSourceController.SetSource(sourceKey, consumable.TimedEffectDefinition);
            _timedConsumableExpirations[sourceKey] = Time.time + consumable.TimedEffectDuration;
            return true;
        }

        private static string BuildTimedConsumableSourceKey(ItemInstance item)
        {
            int baseItemId = item?.BaseItem != null ? item.BaseItem.GetInstanceID() : 0;
            return $"{TIMED_CONSUMABLE_SOURCE_PREFIX}{baseItemId}";
        }

        private static void ConsumeUse(InventorySlot slot, ConsumableState state)
        {
            if (state != null && state.CurrentCharges > 1)
            {
                state.CurrentCharges -= 1;
                slot.HeldItem?.NotifyStateChanged();
                return;
            }

            slot.DecreaseCount(1);
        }
    }
}
