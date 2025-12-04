using UnityEngine;
using Unity.Properties;

[CreateAssetMenu(fileName = "TeamSo", menuName = "Scriptable Objects/TeamSo")]
public class TeamSo : ScriptableObject
{
    [CreateProperty]
    public string TeamName;
    [CreateProperty]
    public string ClassName;
    [CreateProperty]
    public Texture2D PlayerIcon;
    [CreateProperty]
    public Texture2D ClassIcon;
}
