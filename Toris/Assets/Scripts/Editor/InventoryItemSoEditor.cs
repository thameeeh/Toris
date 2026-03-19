using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using OutlandHaven.UIToolkit; // Make sure this matches your SO's namespace

namespace OutlandHaven.Inventory
{

    [CustomEditor(typeof(InventoryItemSO))]
    public class InventoryItemSOEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector (shows ItemName, Icon, etc., and the broken list)
            DrawDefaultInspector();

            InventoryItemSO itemSO = (InventoryItemSO)target;

            GUILayout.Space(15);

            // Create a custom button at the bottom of the inspector
            if (GUILayout.Button("Add Modular Component", GUILayout.Height(30)))
            {
                GenericMenu menu = new GenericMenu();

                // Use reflection to find all scripts in your project that inherit from ItemComponent
                // and are NOT abstract (so we can actually instantiate them).
                var types = AppDomain.CurrentDomain.GetAssemblies()
                    .SelectMany(s => s.GetTypes())
                    .Where(p => typeof(ItemComponent).IsAssignableFrom(p) && !p.IsAbstract);

                foreach (var type in types)
                {
                    // Add each valid component to a dropdown menu
                    menu.AddItem(new GUIContent(type.Name), false, () =>
                    {
                        Undo.RecordObject(itemSO, "Add Item Component");

                        // Instantiate the specific subclass and add it to the list
                        ItemComponent newComponent = (ItemComponent)Activator.CreateInstance(type);
                        itemSO.Components.Add(newComponent);

                        EditorUtility.SetDirty(itemSO);
                    });
                }

                menu.ShowAsContext();
            }
        }
    }
}