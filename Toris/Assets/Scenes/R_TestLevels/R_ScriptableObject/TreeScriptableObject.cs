using UnityEngine;

[CreateAssetMenu(fileName = "TreeScriptableObject", menuName = "Scriptable Objects/TreeScriptableObject")]
public class TreeScriptableObject : ScriptableObject
{
    public string prefabName;

    public int numberOfPrefabsToCreate;
    public Vector3[] spawnPoints;
}
