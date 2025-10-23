using UnityEngine;

[CreateAssetMenu(fileName = "DashConfig", menuName = "Game/Characters/Movement/Dash Config")]
public class DashConfig : ScriptableObject
{
    [Header("Tuning")]
    [Min(0f)] public float initialSpeed = 18f;
    [Min(0f)] public float duration = 0.18f;
    [Min(0f)] public float cooldown = 0.30f;
    [Range(0f, 1f)] public float blendToRun = 1f;

    [Header("Shaping")]
    public AnimationCurve speedCurve = AnimationCurve.EaseInOut(0, 1, 1, 0);

    [Header("Gating")]
    [Min(0f)] public float staminaCost = 25f;

    [Header("Animation")]
    public bool lockFacingDuringDash = true;
}
