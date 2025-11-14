using UnityEngine;

[CreateAssetMenu(fileName = "ResourceData", menuName = "Resources/ResourceData")]
public class ResourceData : ScriptableObject
{
    [SerializeField]
    private string resourceID;

    public string resourceName;
    public Sprite resourceIcon;

    [TextArea(3, 5)]
    public string Description;

    public string ID => resourceID;

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(resourceID))
        {
            resourceID = System.Guid.NewGuid().ToString();
        }
    }
}
