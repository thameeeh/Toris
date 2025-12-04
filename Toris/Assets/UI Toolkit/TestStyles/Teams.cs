using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;

[CreateAssetMenu(fileName = "Teams", menuName = "Scriptable Objects/Teams")]
public class Teams : ScriptableObject
{
    [SerializeField, CreateProperty]
    public List<TeamSo> teams = new List<TeamSo>();
}
