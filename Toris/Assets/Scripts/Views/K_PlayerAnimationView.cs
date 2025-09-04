using UnityEngine;

public class PlayerAnimationView : MonoBehaviour
{
    [SerializeField] Animator animator;        // on this same GameObject (Visual)
    [SerializeField] SpriteRenderer sr;        // same SpriteRenderer

    Vector2 lastDir = Vector2.down;
    float busyUntil = 0f;                      // while playing Shoot/Hurt

    string DirPrefix(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return "S";
        return v.y >= 0 ? "U" : "D";
    }

    public void Tick(Vector2 move)             // call every frame with input
    {
        if (Time.time < busyUntil) return;     // don't override special anims

        if (move.sqrMagnitude > 0.0001f) lastDir = move.normalized;

        // flip for left/right when using side clips
        if (Mathf.Abs(lastDir.x) > Mathf.Abs(lastDir.y))
            sr.flipX = lastDir.x > 0;

        string dir = DirPrefix(lastDir);
        bool moving = move.sqrMagnitude > 0.0001f;
        animator.Play($"{dir}_{(moving ? "Walk" : "Idle")}");
    }

    public void PlayShoot(float hold = 0.12f)  { PlayOneShot("Shoot", hold); }
    public void PlayHurt(float hold = 0.18f)   { PlayOneShot("Hurt",  hold); }
    public void PlayDeath()                    { animator.CrossFade($"{DirPrefix(lastDir)}_Death", 0.05f); busyUntil = float.MaxValue; }

    void PlayOneShot(string action, float hold)
    {
        string dir = DirPrefix(lastDir);
        animator.CrossFade($"{dir}_{action}", 0.05f);
        busyUntil = Time.time + hold;          // return control after brief hold
    }
}
