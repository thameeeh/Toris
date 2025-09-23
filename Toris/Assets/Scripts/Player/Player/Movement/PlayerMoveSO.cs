using UnityEngine;

[CreateAssetMenu(fileName = "PlayerMoveConfig", menuName = "Game/Characters/Movement/Player Move")]
public class PlayerMoveConfig : ScriptableObject
{
    [Min(0f)] public float speed = 6f;
    public bool clampDiagonal = true;
    public bool rotateInput45 = false;
}
