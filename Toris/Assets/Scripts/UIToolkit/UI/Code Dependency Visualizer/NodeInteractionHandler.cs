using UnityEngine;

public class NodeInteractionHandler : MonoBehaviour
{
    private string nodeId;
    private GraphVisualizer visualizer;

    public void Setup(string id, GraphVisualizer viz)
    {
        nodeId = id;
        visualizer = viz;
    }

    void OnMouseDown()
    {
        // Check if this is a single click (not a double click handled by AssetPinger)
        // We'll focus on MouseDown to trigger the filter immediately
        visualizer.FocusOnNode(nodeId);
    }
}
