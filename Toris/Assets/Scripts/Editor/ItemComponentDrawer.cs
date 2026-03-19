using UnityEditor;
using UnityEngine;
using OutlandHaven.Inventory; // Your namespace

// The 'true' parameter tells Unity to apply this drawer to ALL subclasses!
[CustomPropertyDrawer(typeof(ItemComponent), true)]
public class ItemComponentDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        // 1. Extract the actual class name of the referenced object
        string typeName = GetTypeName(property);

        // 2. Replace the default "Element X" label with the class name
        label.text = typeName;

        // 3. Draw the property normally with the new label
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        // Ensures the foldout takes up the correct amount of vertical space
        return EditorGUI.GetPropertyHeight(property, true);
    }

    private string GetTypeName(SerializedProperty property)
    {
        // Unity stores the type string in this format: "AssemblyName Namespace.ClassName"
        string fullTypeName = property.managedReferenceFullTypename;

        if (string.IsNullOrEmpty(fullTypeName))
            return "Empty Component";

        // Extract just the ClassName by finding the last dot (from the namespace)
        int lastDotIndex = fullTypeName.LastIndexOf('.');
        if (lastDotIndex >= 0)
        {
            return fullTypeName.Substring(lastDotIndex + 1);
        }

        // Fallback in case the class doesn't have a namespace
        int spaceIndex = fullTypeName.IndexOf(' ');
        if (spaceIndex >= 0)
        {
            return fullTypeName.Substring(spaceIndex + 1);
        }

        return fullTypeName;
    }
}