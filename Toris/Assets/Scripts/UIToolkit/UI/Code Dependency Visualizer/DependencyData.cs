using System;
using System.Collections.Generic;

[Serializable]
public class DependencyNode
{
    public string id;       // The name of the script/class
    public string path;     // File path (for grouping)
}

[Serializable]
public class DependencyEdge
{
    public string source;   // Script A
    public string target;   // depends on Script B
}

[Serializable]
public class GraphData
{
    public List<DependencyNode> nodes = new List<DependencyNode>();
    public List<DependencyEdge> edges = new List<DependencyEdge>();
}