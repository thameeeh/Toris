using UnityEngine;
using UnityEditor;
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;

public class DependencyValidatorWindow : EditorWindow
{
    private Vector2 _scrollPos;
    private List<ValidationResult> _results = new List<ValidationResult>();

    [MenuItem("Tools/Project Dependency Validator")]
    public static void ShowWindow()
    {
        GetWindow<DependencyValidatorWindow>("Dependency Validator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Scan Scene for Missing Dependencies", EditorStyles.boldLabel);

        if (GUILayout.Button("Scan Current Scene", GUILayout.Height(30)))
        {
            PerformScan();
        }

        EditorGUILayout.Space();

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos);
        foreach (var result in _results)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            GUI.color = result.IsError ? Color.red : Color.yellow;
            GUILayout.Label($"[{result.Level}]", GUILayout.Width(60));
            GUI.color = Color.white;

            if (GUILayout.Button(result.ObjectName, EditorStyles.linkLabel))
            {
                Selection.activeObject = result.TargetObject;
                EditorGUIUtility.PingObject(result.TargetObject);
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.LabelField($"Field: {result.FieldName}", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
    }

    private void PerformScan()
    {
        _results.Clear();

        // Finds ALL MonoBehaviours in the scene, including inactive ones
        var allComponents = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var comp in allComponents)
        {
            if (comp == null) continue;

            Type type = comp.GetType();
            // We use Reflection to get all fields, including private ones
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                // Logic: Only check if it's a SerializeField or Public, and is a Unity Object type
                bool isSerialized = field.IsDefined(typeof(SerializeField), true) || field.IsPublic;
                bool isUnityObject = typeof(UnityEngine.Object).IsAssignableFrom(field.FieldType);

                if (isSerialized && isUnityObject)
                {
                    object value = field.GetValue(comp);

                    if (value == null || value.Equals(null))
                    {
                        bool isCritical = field.Name.EndsWith("SO") || field.Name.Contains("Event") || field.Name.Contains("Data");

                        _results.Add(new ValidationResult
                        {
                            TargetObject = comp.gameObject,
                            ObjectName = comp.gameObject.name,
                            FieldName = field.Name,
                            Level = isCritical ? "ERROR" : "WARNING",
                            IsError = isCritical
                        });
                    }
                }
            }
        }

        Debug.Log($"Scan Complete: Found {_results.Count} missing dependencies.");
    }

    private struct ValidationResult
    {
        public GameObject TargetObject;
        public string ObjectName;
        public string FieldName;
        public string Level;
        public bool IsError;
    }
}