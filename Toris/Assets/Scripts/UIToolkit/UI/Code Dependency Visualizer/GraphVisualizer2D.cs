using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class GraphVisualizer : MonoBehaviour
{
    [Header("Visual Settings")]
    public GameObject nodePrefab;   // Your Balloon Prefab
    public Material lineMaterial;   // Material for the lines
    public float nodeScale = 1.0f;
    public Color[] possibleColors = { Color.cyan, Color.green, Color.yellow, Color.magenta, Color.white };

    [Header("Physics Settings")]
    public float connectionDistance = 5.0f; 
    public float springFrequency = 1.0f;
    public float repulsionForce = 2.0f;

    // Internal dictionary to track spawned balloons
    private Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();

    void Start()
    {
        GenerateGraph();
    }

    void GenerateGraph()
    {
        // 1. Load Data
        string path = Path.Combine(Application.dataPath, "dependencies.json");
        if (!File.Exists(path))
        {
            Debug.LogError("dependencies.json not found! Go to Tools > Analyze Dependencies first.");
            return;
        }

        string json = File.ReadAllText(path);
        GraphData data = JsonUtility.FromJson<GraphData>(json);

        if (data == null || data.nodes == null) return;

        // 2. Create Nodes (Balloons)
        foreach (var nodeData in data.nodes)
        {
            if (nodes.ContainsKey(nodeData.id)) continue;

            if (nodePrefab == null)
            {
                Debug.LogError("Please assign a Node Prefab in the Inspector!");
                return;
            }

            Vector2 randomPos = Random.insideUnitCircle * 10f;
            GameObject nodeObj = Instantiate(nodePrefab, randomPos, Quaternion.identity);
            nodeObj.name = nodeData.id;
            nodeObj.transform.localScale = Vector3.one * nodeScale;

            // Set a random color
            var renderer = nodeObj.GetComponent<SpriteRenderer>();
            if (renderer != null)
            {
                renderer.color = possibleColors[Random.Range(0, possibleColors.Length)];
            }

            // --- ADDED/UPDATED LABEL LOGIC ---
            var label = nodeObj.GetComponentInChildren<TMPro.TextMeshPro>();
            if (label == null)
            {
                // If the prefab doesn't have a label, create one dynamically
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(nodeObj.transform);
                labelObj.transform.localPosition = Vector3.zero;
                
                label = labelObj.AddComponent<TMPro.TextMeshPro>();
                label.fontSize = 5;
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.color = Color.black; // Better contrast on bright colored balloons
                
                // Ensure text is on top of the balloon sprite
                var meshRenderer = labelObj.GetComponent<MeshRenderer>();
                if (meshRenderer != null)
                {
                    meshRenderer.sortingOrder = 10; 
                }
            }
            label.text = nodeData.id;
            // ---------------------------------

            // Update physics settings if the balloon has our script
            var physics = nodeObj.GetComponent<BalloonPhysics2D>();
            if (physics != null)
            {
                physics.centerForce = 0.2f; // Reduced so they can spread out more
                physics.jitterAmount = 0.1f;
            }

            nodes.Add(nodeData.id, nodeObj);
        }

        // 3. Create Edges (Spring Connections)
        foreach (var edgeData in data.edges)
        {
            if (nodes.TryGetValue(edgeData.source, out GameObject sourceObj) && 
                nodes.TryGetValue(edgeData.target, out GameObject targetObj))
            {
                Rigidbody2D rbSource = sourceObj.GetComponent<Rigidbody2D>();
                Rigidbody2D rbTarget = targetObj.GetComponent<Rigidbody2D>();

                if (rbSource == null || rbTarget == null) continue;

                SpringJoint2D joint = sourceObj.AddComponent<SpringJoint2D>();
                joint.connectedBody = rbTarget;
                joint.autoConfigureDistance = false;
                joint.distance = connectionDistance;
                joint.frequency = springFrequency;
                joint.dampingRatio = 0.5f;

                GameObject lineObj = new GameObject("Link_" + edgeData.source + "_" + edgeData.target);
                LineRenderer line = lineObj.AddComponent<LineRenderer>();
                if (lineMaterial != null) line.material = lineMaterial;

                line.startWidth = 0.03f;
                line.endWidth = 0.03f;
                line.positionCount = 2;
                line.sortingOrder = -1;

                LineFollower2D follower = lineObj.AddComponent<LineFollower2D>();
                follower.startNode = sourceObj.transform;
                follower.endNode = targetObj.transform;
            }
        }
    }
}