using UnityEngine;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

public class CodeDependencyParser : EditorWindow
{
    [MenuItem("Tools/Analyze Dependencies")]
    public static void Analyze()
    {
        // 1. Define Highly Specific Whitelist of folders to scan
        string[] whitelistFolders = { 
            "Scripts/UIToolkit",
            "Scripts/Items",
            "Scripts/Save System",
            "Scripts/Player/Player/Inventory",
            "Scripts/Player/Player/Anchors",
            "Scripts/Player/Player/View",
            "UI_Toolkit"
        };
        
        // 2. Define Blacklist of exact script names to ignore (Interfaces & Visualizer tools)
        string[] blacklistScripts = { 
            "CodeDependencyParser", 
            "DependencyData", 
            "GraphVisualizer", 
            "BalloonPhysics2D", 
            "LineFollower2D", 
            "GraphCameraController",
            "IUsable",
            "IEquipable",
            "IContainerInteractable",
            "CharacterAnimSO",
            "PlayerAnimationController",
            "PlayerAnimationPresenter",
            "PlayerAnimationView",
            "PlayerAnimationController",
            "ItemPicker",
            "InventoryActionDebugger",
            "IInteractable"
        };

        List<string> filesList = new List<string>();

        foreach (string folder in whitelistFolders)
        {
            // Normalize path separators to avoid cross-platform issues
            string path = Path.Combine(Application.dataPath, folder.Replace('/', Path.DirectorySeparatorChar));
            if (Directory.Exists(path))
            {
                var files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories)
                    // Exclude Editor folders
                    .Where(f => !f.Contains(Path.DirectorySeparatorChar + "Editor" + Path.DirectorySeparatorChar) 
                             && !f.Contains(Path.AltDirectorySeparatorChar + "Editor" + Path.AltDirectorySeparatorChar))
                    // Exclude explicitly blacklisted scripts
                    .Where(f => !blacklistScripts.Contains(Path.GetFileNameWithoutExtension(f)))
                    // Dynamically exclude any file containing 'Debug' or 'Test'
                    .Where(f => !Path.GetFileNameWithoutExtension(f).Contains("Debug") 
                             && !Path.GetFileNameWithoutExtension(f).Contains("Test"));
                
                filesList.AddRange(files);
            }
        }

        string[] allFiles = filesList.ToArray();
        HashSet<string> customTypes = GetCustomDefinedTypes(allFiles);

        GraphData graph = new GraphData();
        HashSet<string> addedNodes = new HashSet<string>(); // Tracker for unique filenames
        HashSet<string> addedEdges = new HashSet<string>();

        foreach (string file in allFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(file);

            // FIX: Only add the node if we haven't seen this filename before
            if (!addedNodes.Contains(fileName))
            {
                graph.nodes.Add(new DependencyNode { id = fileName, path = file });
                addedNodes.Add(fileName);
            }

            string code = File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var usedTypes = tree.GetCompilationUnitRoot().DescendantNodes()
                .OfType<IdentifierNameSyntax>()
                .Select(id => id.Identifier.Text)
                .Distinct();

            foreach (var type in usedTypes)
            {
                if (customTypes.Contains(type) && type != fileName)
                {
                    string edgeId = $"{fileName}->{type}";
                    if (!addedEdges.Contains(edgeId))
                    {
                        graph.edges.Add(new DependencyEdge { source = fileName, target = type });
                        addedEdges.Add(edgeId);
                    }
                }
            }
        }

        string json = JsonUtility.ToJson(graph, true);
        File.WriteAllText(Path.Combine(Application.dataPath, "dependencies.json"), json);
        Debug.Log($"Dependency analysis complete! Scanned {allFiles.Length} files.");
    }

    private static HashSet<string> GetCustomDefinedTypes(string[] files)
    {
        HashSet<string> types = new HashSet<string>();
        foreach (var file in files)
        {
            string code = File.ReadAllText(file);
            SyntaxTree tree = CSharpSyntaxTree.ParseText(code);
            var root = tree.GetCompilationUnitRoot();

            // Find all Class, Struct, and Interface declarations
            var declarations = root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>();
            foreach (var decl in declarations)
            {
                types.Add(decl.Identifier.Text);
            }
        }
        return types;
    }
}