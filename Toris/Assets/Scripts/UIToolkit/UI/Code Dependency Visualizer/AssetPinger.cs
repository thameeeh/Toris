using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AssetPinger : MonoBehaviour
{
    public string assetPath; // Set by GraphVisualizer during instantiation

    private float lastClickTime;
    private const float doubleClickThreshold = 0.3f;

    void OnMouseDown()
    {
        float timeSinceLastClick = Time.time - lastClickTime;

        if (timeSinceLastClick <= doubleClickThreshold)
        {
            PingAsset();
        }

        lastClickTime = Time.time;
    }

    private void PingAsset()
    {
#if UNITY_EDITOR
        if (string.IsNullOrEmpty(assetPath)) return;

        // Convert full system path to relative Unity Assets path
        string relativePath = assetPath;
        if (assetPath.Contains("Assets"))
        {
            relativePath = "Assets" + assetPath.Split(new string[] { "Assets" }, System.StringSplitOptions.None)[1];
        }

        Object asset = AssetDatabase.LoadAssetAtPath<Object>(relativePath);
        if (asset != null)
        {
            EditorGUIUtility.PingObject(asset);
            Selection.activeObject = asset;
            Debug.Log($"[Visualizer] Pinged script: {relativePath}");
        }
        else
        {
            Debug.LogWarning($"[Visualizer] Could not find asset at path: {relativePath}");
        }
#endif
    }
}
