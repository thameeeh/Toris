using UnityEngine;

[CreateAssetMenu(fileName = "TeamSo", menuName = "Scriptable Objects/TeamSo")]
public class TeamSo : ScriptableObject
{
    public string TeamName;
    public string ClassName;
    public Texture2D PlayerIcon;
    public Texture2D ClassIcon;
}
