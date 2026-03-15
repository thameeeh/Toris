using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OutlandHaven.UIToolkit;

[CustomEditor(typeof(InventoryContainerSO))]
public class InventoryContainerSOEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector
        DrawDefaultInspector();

        InventoryContainerSO containerSO = (InventoryContainerSO)target;

        GUILayout.Space(15);
        GUILayout.Label("Add Modular States to Slots", EditorStyles.boldLabel);

        if (containerSO.Slots == null || containerSO.Slots.Count == 0)
        {
            return;
        }

        for (int i = 0; i < containerSO.Slots.Count; i++)
        {
            var slot = containerSO.Slots[i];

            if (slot == null || slot.HeldItem == null) continue;

            GUILayout.BeginHorizontal();

            string itemName = slot.HeldItem.BaseItem != null ? slot.HeldItem.BaseItem.ItemName : "Empty Slot";
            GUILayout.Label($"Slot {i} ({itemName}):", GUILayout.Width(150));

            if (GUILayout.Button("Add State Component", GUILayout.Height(20)))
            {
                GenericMenu menu = new GenericMenu();
                int slotIndex = i; // Capture for closure

                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(ItemComponentState).IsAssignableFrom(p) && !p.IsAbstract);

                foreach (var type in types)
                {
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        Undo.RecordObject(containerSO, "Add Item Component State");

                        ItemComponentState newState = (ItemComponentState)Activator.CreateInstance(type);

                        if (containerSO.Slots[slotIndex].HeldItem.States == null)
                        {
                            containerSO.Slots[slotIndex].HeldItem.States = new System.Collections.Generic.List<ItemComponentState>();
                        }

                        containerSO.Slots[slotIndex].HeldItem.States.Add(newState);

                        EditorUtility.SetDirty(containerSO);
                    });
                }

                if (!types.Any())
                {
                    menu.AddDisabledItem(new GUIContent("No State Types Found"));
                }

                menu.ShowAsContext();
            }

            GUILayout.EndHorizontal();
        }
    }
}
