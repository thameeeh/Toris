using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TreeScriptableObject", menuName = "Scriptable Objects/TreeScriptableObject")]
public class TreeScriptableObject : ScriptableObject
{
    public string prefabName;
    public List<Vector3> pointsList = new();
}
