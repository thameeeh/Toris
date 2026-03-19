using System.Collections.Generic;
using UnityEngine;
using OutlandHaven.Inventory;

namespace OutlandHaven.Player.Equipment
{
    /// <summary>
    /// Listens to the EquipmentManager and injects item stats (EquipableComponent)
    /// as PlayerEffectDefinitions into the PlayerEffectSourceController.
    /// </summary>
    [RequireComponent(typeof(EquipmentManager), typeof(PlayerEffectSourceController))]
    public class EquipmentEffectBridge : MonoBehaviour
    {
        [Header("Required Managers")]
        [SerializeField] private EquipmentManager _equipmentManager;
        [SerializeField] private PlayerEffectSourceController _effectSourceController;

        private void Awake()
        {
            // Auto-cache if not assigned in Inspector
            if (_equipmentManager == null) _equipmentManager = GetComponent<EquipmentManager>();
            if (_effectSourceController == null) _effectSourceController = GetComponent<PlayerEffectSourceController>();
        }

        private void OnEnable()
        {
            if (_equipmentManager != null)
            {
                _equipmentManager.OnEquipmentChanged += HandleEquipmentChanged;
            }
        }

        private void OnDisable()
        {
            if (_equipmentManager != null)
            {
                _equipmentManager.OnEquipmentChanged -= HandleEquipmentChanged;
            }
        }

        private void HandleEquipmentChanged(EquipmentSlot slot, ItemInstance oldItem, ItemInstance newItem)
        {
            string sourceKey = $"EquipSlot_{slot}";

            if (newItem == null)
            {
                // Unregister the old stats from the PlayerEffectSourceController
                _effectSourceController.RemoveSource(sourceKey);
                return;
            }

            // A new item was equipped. Translate its stats into a PlayerEffectDefinitionSO
            EquipableComponent equipable = newItem.BaseItem.GetComponent<EquipableComponent>();
            if (equipable == null)
            {
                // This shouldn't happen due to EquipmentManager validation, but just in case:
                _effectSourceController.RemoveSource(sourceKey);
                return;
            }

            // Create a runtime scriptable object to hold our modifiers
            PlayerEffectDefinitionSO runtimeDefinition = ScriptableObject.CreateInstance<PlayerEffectDefinitionSO>();

            // Generate modifiers based on the item's stats
            List<PlayerEffectModifier> modifiers = new List<PlayerEffectModifier>();

            if (equipable.StrengthBonus != 0f)
            {
                modifiers.Add(new PlayerEffectModifier
                {
                    effectType = PlayerEffectType.StrengthBonus,
                    modifierMode = PlayerEffectModifierMode.Additive,
                    numericValue = equipable.StrengthBonus
                });
            }

            if (equipable.DefenceBonus != 0f)
            {
                modifiers.Add(new PlayerEffectModifier
                {
                    effectType = PlayerEffectType.DefenceBonus,
                    modifierMode = PlayerEffectModifierMode.Additive,
                    numericValue = equipable.DefenceBonus
                });
            }

            // Using Reflection or a custom initializer method on PlayerEffectDefinitionSO to set modifiers
            // We use Reflection here since PlayerEffectDefinitionSO does not expose a setter for its private list.
            var fieldInfo = typeof(PlayerEffectDefinitionSO).GetField("_modifiers", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (fieldInfo != null)
            {
                fieldInfo.SetValue(runtimeDefinition, modifiers);
            }

            // Inject the new source into the controller
            _effectSourceController.SetSource(sourceKey, runtimeDefinition);
        }
    }
}