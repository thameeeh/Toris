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

    // Internal dictionary to track spawned balloons and their links
    private Dictionary<string, GameObject> nodes = new Dictionary<string, GameObject>();
    private List<GameObject> allLinks = new List<GameObject>();
    private List<DependencyEdge> edgeDataList = new List<DependencyEdge>();

    void Start()
    {
        // --- ENSURE CAMERA CAN SEE 2D OBJECTS ---
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            if (mainCam.GetComponent<UnityEngine.EventSystems.Physics2DRaycaster>() == null)
            {
                mainCam.gameObject.AddComponent<UnityEngine.EventSystems.Physics2DRaycaster>();
            }
            
            // Add a component to detect clicks on empty space to reset focus
            mainCam.gameObject.AddComponent<BackgroundClickHandler>().OnBackgroundClick += ResetFocus;
        }
        // -----------------------------------------

        GenerateGraph();
    }

    public void FocusOnNode(string nodeId)
    {
        HashSet<string> connectedNodes = new HashSet<string>();
        connectedNodes.Add(nodeId);

        // Find 1st order connections
        foreach (var edge in edgeDataList)
        {
            if (edge.source == nodeId) connectedNodes.Add(edge.target);
            if (edge.target == nodeId) connectedNodes.Add(edge.source);
        }

        // Hide/Show Nodes
        foreach (var node in nodes)
        {
            bool isVisible = connectedNodes.Contains(node.Key);
            node.Value.SetActive(isVisible);
        }

        // Hide/Show Links
        foreach (var link in allLinks)
        {
            bool shouldShow = false;
            // The link name contains source and target IDs
            foreach (var connectedId in connectedNodes)
            {
                if (link.name.Contains(nodeId) && (link.name.Contains(connectedId)))
                {
                    shouldShow = true;
                    break;
                }
            }
            link.SetActive(shouldShow);
        }
    }

    public void ResetFocus()
    {
        foreach (var node in nodes.Values) node.SetActive(true);
        foreach (var link in allLinks) link.SetActive(true);
    }

    // Dictionary to map folder names to unique colors
    private Dictionary<string, Color> folderColors = new Dictionary<string, Color>();

    void GenerateGraph()
    {
        string path = Path.Combine(Application.dataPath, "dependencies.json");
        if (!File.Exists(path)) return;
        string json = File.ReadAllText(path);
        GraphData data = JsonUtility.FromJson<GraphData>(json);
        edgeDataList = data.edges;

        // 2. Create Nodes (Balloons)
        foreach (var nodeData in data.nodes)
        {
            if (nodes.ContainsKey(nodeData.id)) continue;

            Vector2 randomPos = Random.insideUnitCircle * 15f; 
            GameObject nodeObj = Instantiate(nodePrefab, randomPos, Quaternion.identity);
            nodeObj.name = nodeData.id;
            nodeObj.transform.localScale = Vector3.one * nodeScale;

            if (nodeObj.GetComponent<Collider2D>() == null)
            {
                var col = nodeObj.AddComponent<CircleCollider2D>();
                col.radius = 0.5f;
            }

            string folderName = GetFolderName(nodeData.path);
            if (!folderColors.ContainsKey(folderName))
            {
                folderColors[folderName] = possibleColors[folderColors.Count % possibleColors.Length];
            }
            
            var renderer = nodeObj.GetComponent<SpriteRenderer>();
            if (renderer != null) renderer.color = folderColors[folderName];

            nodeObj.AddComponent<NodeDragger>();

            var pinger = nodeObj.AddComponent<AssetPinger>();
            pinger.assetPath = nodeData.path;

            // Add Interaction Handler for Filtering
            var interaction = nodeObj.AddComponent<NodeInteractionHandler>();
            interaction.Setup(nodeData.id, this);

            var label = nodeObj.GetComponentInChildren<TMPro.TextMeshPro>();
            if (label == null)
            {
                GameObject labelObj = new GameObject("Label");
                labelObj.transform.SetParent(nodeObj.transform);
                labelObj.transform.localPosition = Vector3.zero;
                label = labelObj.AddComponent<TMPro.TextMeshPro>();
                label.fontSize = 5;
                label.alignment = TMPro.TextAlignmentOptions.Center;
                label.color = Color.black;
                var meshRenderer = labelObj.GetComponent<MeshRenderer>();
                if (meshRenderer != null) meshRenderer.sortingOrder = 10;
            }
            label.text = nodeData.id;

            var physics = nodeObj.GetComponent<BalloonPhysics2D>();
            if (physics != null)
            {
                physics.centerForce = 0.05f;
                physics.repulsionForce = 25f;
                physics.repulsionRange = 5f;
            }

            nodes.Add(nodeData.id, nodeObj);
        }

        // 3. Create Edges
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
                line.material = lineMaterial;
                line.startWidth = 0.08f;
                line.endWidth = 0.02f;
                line.positionCount = 2;
                line.sortingOrder = -1;

                Gradient gradient = new Gradient();
                gradient.SetKeys(
                    new GradientColorKey[] { new GradientColorKey(Color.white, 0.0f), new GradientColorKey(Color.gray, 1.0f) },
                    new GradientAlphaKey[] { new GradientAlphaKey(1.0f, 0.0f), new GradientAlphaKey(0.2f, 1.0f) }
                );
                line.colorGradient = gradient;

                LineFollower2D follower = lineObj.AddComponent<LineFollower2D>();
                follower.startNode = sourceObj.transform;
                follower.endNode = targetObj.transform;

                allLinks.Add(lineObj);
            }
        }
    }


    private string GetFolderName(string fullPath)
    {
        string directory = Path.GetDirectoryName(fullPath);
        return Path.GetFileName(directory); // Just get the immediate parent folder name
    }
}