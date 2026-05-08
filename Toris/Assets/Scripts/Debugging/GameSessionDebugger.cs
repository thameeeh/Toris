using UnityEngine;
using OutlandHaven.Inventory;
using System.Collections.Generic;
using System.Text;
using OutlandHaven.UIToolkit;

namespace OutlandHaven.Debugging
{
    /// <summary>
    /// Attach this to a persistent GameObject or the Player to monitor GameSession state.
    /// </summary>
    public class GameSessionDebugger : MonoBehaviour
    {
        [Header("Settings")]
        public GameSessionSO Session;
        public bool LogOnSceneChange = true;

        private void OnEnable()
        {
            if (Session == null) Session = GameSessionSO.LoadDefault();
        }

        [ContextMenu("Log Current Session State")]
        public void LogSessionState()
        {
            if (Session == null)
            {
                Debug.LogError("[DEBUG] GameSessionSO is null!");
                return;
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("<b>=== GameSessionSO Diagnostic Report ===</b>");
            
            // 1. References
            sb.AppendLine($"Live PlayerInventory: {(Session.PlayerInventory != null ? Session.PlayerInventory.name : "NULL")}");
            sb.AppendLine($"Live PlayerEquipment: {(Session.PlayerEquipment != null ? Session.PlayerEquipment.name : "NULL")}");

            // 2. Snapshots & Live Data
            sb.AppendLine("\n<b>[Inventory State]</b>");
            AppendInventoryDetails(sb, "Backpack", Session.GetPlayerInventorySnapshot(), Session.PlayerInventory);
            AppendInventoryDetails(sb, "Equipment", Session.GetEquipmentInventorySnapshot(), Session.PlayerEquipment);

            // 3. Stats & Progression
            sb.AppendLine("\n<b>[Stats & Progression]</b>");
            if (Session.TryGetPlayerProgressionState(out int lvl, out float xp, out int gold))
                sb.AppendLine($"Progression: Level {lvl}, XP {xp}, Gold {gold}");
            else
                sb.AppendLine("Progression: NO SNAPSHOT");

            if (Session.TryGetPlayerStatsState(out float hp, out float stamina))
                sb.AppendLine($"Stats: HP {hp}, Stamina {stamina}");
            else
                sb.AppendLine("Stats: NO SNAPSHOT");

            Debug.Log(sb.ToString());
        }

        private void AppendInventoryDetails(StringBuilder sb, string label, object snapshot, InventoryManager liveInventory)
        {
            if (snapshot == null)
            {
                sb.AppendLine($"{label} Snapshot: NULL (Already applied or not yet captured)");
                
                // Fallback to showing live slots if available
                if (liveInventory != null && liveInventory.LiveSlots != null)
                {
                    sb.AppendLine($"  -> Live {label} currently has {liveInventory.LiveSlots.Count} slots:");
                    int occupied = 0;
                    foreach (var slot in liveInventory.LiveSlots)
                    {
                        if (slot != null && !slot.IsEmpty && slot.HeldItem?.BaseItem != null)
                        {
                            sb.AppendLine($"     - {slot.HeldItem.BaseItem.ItemName} (x{slot.Count}) [ID: {slot.HeldItem.InstanceID}]");
                            occupied++;
                        }
                    }
                    if (occupied == 0) sb.AppendLine("     - (All live slots are currently empty)");
                }
                else
                {
                    sb.AppendLine($"  -> No live {label} inventory is currently bound.");
                }
                return;
            }

            // Using reflection to read the private '_slots' field
            var slotsField = snapshot.GetType().GetField("_slots", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (slotsField != null)
            {
                var slots = (System.Array)slotsField.GetValue(snapshot);
                sb.AppendLine($"{label} Snapshot: {slots.Length} slots tracked in memory.");
                
                int occupiedCount = 0;
                foreach (var slot in slots)
                {
                    if (slot == null) continue;

                    // Item and Count are properties, not fields
                    var itemProp = slot.GetType().GetProperty("Item", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                    var item = itemProp?.GetValue(slot) as ItemInstance;
                    
                    if (item != null && item.BaseItem != null)
                    {
                        var countProp = slot.GetType().GetProperty("Count", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        int count = (int)(countProp?.GetValue(slot) ?? 0);
                        sb.AppendLine($"  - SNAPSHOT: {item.BaseItem.ItemName} (x{count}) [ID: {item.InstanceID}]");
                        occupiedCount++;
                    }
                }
                if (occupiedCount == 0) sb.AppendLine("  - (All snapshot slots empty)");
            }
            else
            {
                sb.AppendLine($"{label} Snapshot: Reflection failed to find '_slots' field.");
            }
        }
    }
}