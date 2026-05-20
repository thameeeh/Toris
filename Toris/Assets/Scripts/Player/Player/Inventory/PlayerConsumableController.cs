using System;
using System.Collections.Generic;
using UnityEngine;

namespace OutlandHaven.Inventory
{
    public struct PlayerConsumableUseContext
    {
        public ConsumableEffectMode EffectMode;
        public ConsumptionSlot Payload;
        public float Amount;
    }

    /// <summary>
    /// Runtime owner for consumable usage rules, cooldowns, and slot mutation.
    /// </summary>
    public sealed class PlayerConsumableController
    {
        private const string TIMED_CONSUMABLE_SOURCE_PREFIX = "ConsumableTimed_";

        public event Action<PlayerConsumableUseContext> ConsumableUsed;

        private readonly UIInventoryEventsSO _uiInventoryEvents;
        private PlayerStatsAnchorSO _playerStatsAnchor;
        private PlayerStats _playerStatsFallback;
        private PlayerEffectSourceController _playerEffectSourceController;
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

        public void Rebind(
            PlayerStatsAnchorSO playerStatsAnchor,
            PlayerStats playerStatsFallback,
            PlayerEffectSourceController playerEffectSourceController)
        {
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

        public bool ExecuteConsumption(ConsumableComponent consumable, InventorySlot slot)
        {
            if (consumable == null || slot == null || slot.IsEmpty || slot.HeldItem == null)
            {
                return false;
            }

            ItemInstance item = slot.HeldItem;

            if (IsOnCooldown(item.BaseItem))
            {
                return false;
            }

            if (!TryApplyEffect(item, consumable))
            {
                return false;
            }

            slot.DecreaseCount(1);

            if (consumable.CooldownDuration > 0f)
                _nextUseByItem[item.BaseItem] = Time.time + consumable.CooldownDuration;

            ConsumableUsed?.Invoke(new PlayerConsumableUseContext
            {
                EffectMode = consumable.EffectMode,
                Payload = consumable.EffectPayload,
                Amount = consumable.amount
            });

            _uiInventoryEvents?.OnSpecificSlotsUpdated?.Invoke(slot, null);
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

            PlayerEffectDefinitionSO effectDefinition = consumable.ResolveTimedEffectDefinition();
            if (effectDefinition == null)
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
            _playerEffectSourceController.SetSource(sourceKey, effectDefinition);
            _timedConsumableExpirations[sourceKey] = Time.time + consumable.TimedEffectDuration;
            return true;
        }

        private static string BuildTimedConsumableSourceKey(ItemInstance item)
        {
            int baseItemId = item?.BaseItem != null ? item.BaseItem.GetInstanceID() : 0;
            return $"{TIMED_CONSUMABLE_SOURCE_PREFIX}{baseItemId}";
        }
    }
}
