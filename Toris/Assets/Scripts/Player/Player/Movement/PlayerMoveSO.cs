using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMoveSO", menuName = "Outland Haven/Player/Movement/Player Move")]
public class PlayerMoveSO : ScriptableObject
{
    [Min(0f)] public float speed = 6f;
}
