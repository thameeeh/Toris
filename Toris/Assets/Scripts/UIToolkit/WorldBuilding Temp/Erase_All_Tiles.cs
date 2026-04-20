using UnityEngine;
using UnityEngine.Tilemaps;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// A utility component that adds a button in the Inspector to clear all tiles on a Tilemap.
/// Attach this script to any GameObject that has a Tilemap component.
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class TilemapClearer : MonoBehaviour
{
    // The main function that performs the clear.
    // It's decorated with context menu attributes to make it accessible without a custom editor.
    [ContextMenu("Clear All Tiles")]
    public void ClearAllTiles()
    {
        Tilemap tilemap = GetComponent<Tilemap>();
        if (tilemap != null)
        {
            // Record the object state for the Undo system
            Undo.RecordObject(tilemap, "Clear Tilemap");

            // This is the core Unity function to erase all tiles
            tilemap.ClearAllTiles();

            // Notify the editor that something changed, to update the scene view
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(tilemap);
                Debug.Log($"Cleared all tiles on GameObject: '{gameObject.name}'");
            }
        }
        else
        {
            Debug.LogError($"Cannot clear tiles. No Tilemap component found on GameObject: '{gameObject.name}'");
        }
    }
}

// Custom Editor to add a button in the Inspector
#if UNITY_EDITOR
[CustomEditor(typeof(TilemapClearer))]
public class TilemapClearerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        // Draw the default inspector (so you can see other components)
        DrawDefaultInspector();

        // Get a reference to the TilemapClearer script
        TilemapClearer clearer = (TilemapClearer)target;

        // Add some space for separation
        EditorGUILayout.Space();

        // Create the button
        if (GUILayout.Button("Delete All Tiles", GUILayout.Height(30)))
        {
            // This is the warning pop-up
            if (EditorUtility.DisplayDialog("Clear Tilemap",
                "Are you sure you want to delete every tile on this game object? This action cannot be easily undone.",
                "Yes, Clear It", "Cancel"))
            {
                // Call the clear function
                clearer.ClearAllTiles();
            }
        }
    }
}
#endif