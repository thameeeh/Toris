using UnityEngine;

public class PlayerAnimationView : MonoBehaviour
{
    [SerializeField] Animator animator;        // same GameObject (Visual)
    [SerializeField] SpriteRenderer sr;        // same SpriteRenderer

    [Header("Bow Shoot (single clip)")]
    [Tooltip("Clip/state name used like U_<suffix>, D_<suffix>, S_<suffix>")]
    [SerializeField] string shootStateSuffix = "Shoot";     // e.g. U_Shoot / D_Shoot / S_Shoot
    [Tooltip("Where to freeze inside the Shoot clip (0..1).")]
    [Range(0f, 1f)][SerializeField] float lockAtNormalized = 0.50f; // frame-3 ~ 0.50 for 4 frames
    [SerializeField] float crossFade = 0.05f;

    Vector2 lastDir = Vector2.down;
    float busyUntil = 0f;

    // bow hold runtime state
    bool bowHolding;
    bool bowLocked;                  // we paused on the lock frame
    string activeShootState;         // cached "U_Shoot" / "D_Shoot" / "S_Shoot"

    const int BaseLayer = 0;
    const float ResumeEpsilon = 0.02f; // advance a bit past the lock point when releasing

    string DirPrefix(Vector2 v)
    {
        if (Mathf.Abs(v.x) > Mathf.Abs(v.y)) return "S"; // side
        return v.y >= 0 ? "U" : "D";                     // up/down
    }
    public void Tick(Vector2 move)
    {
        if (bowHolding) { UpdateBowHoldLock(); return; }

        if (move.sqrMagnitude > 0.0001f) lastDir = move.normalized;
        if (Mathf.Abs(lastDir.x) > Mathf.Abs(lastDir.y))
            sr.flipX = lastDir.x > 0;

        if (Time.time < busyUntil) return;

        string dir = DirPrefix(lastDir);
        bool moving = move.sqrMagnitude > 0.0001f;
        animator.Play($"{dir}_{(moving ? "Walk" : "Idle")}");
    }


    void UpdateBowHoldLock()
    {
        // use modulo so loops do not break the check
        var st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        if (!st.IsName(activeShootState)) return;

        float nt = st.normalizedTime % 1f; // 0..1 within current loop
        if (!bowLocked && nt >= lockAtNormalized)
        {
            animator.Play(activeShootState, BaseLayer, lockAtNormalized);
            animator.speed = 0f;        // pause on the lock frame
            bowLocked = true;
            busyUntil = float.MaxValue; // block Tick from switching states
        }
    }

    // === Public API for your bow controller ===

    // Press: start the bow draw. Plays Shoot and will freeze at lockAtNormalized.
    public void BeginBowHold()
    {
        string dir = DirPrefix(lastDir);
        activeShootState = $"{dir}_{shootStateSuffix}";
        bowHolding = true;
        bowLocked = false;

        // safety: ensure the state exists to avoid "state not found"
        int hash = Animator.StringToHash(activeShootState);
        if (!animator.HasState(BaseLayer, hash))
        {
            Debug.LogError($"[Anim] Missing state on layer {BaseLayer}: '{activeShootState}'");
            return;
        }

        animator.speed = 1f; // ensure running before we lock
        animator.CrossFade(activeShootState, crossFade);
        // do not set busyUntil yet; we freeze when we hit the lock frame
    }

    // Release: unfreeze and continue the SAME Shoot clip to the end, then hand control back.
    public void ReleaseBowAndFinish()
    {
        if (!bowHolding) return;

        bowHolding = false;
        animator.speed = 1f; // resume if we paused

        // jump a bit past the lock point so the frame advances
        float t = Mathf.Clamp01(lockAtNormalized + ResumeEpsilon);
        animator.Play(activeShootState, BaseLayer, t);

        // hold control until the remainder of the clip finishes
        float len = ClipLen(activeShootState);
        float remaining = Mathf.Max(0.1f, (1f - t) * (len > 0f ? len : 0.18f)); // simple estimate
        busyUntil = Time.time + remaining;

        bowLocked = false;
    }

    // === Other one-shots (unchanged) ===

    public void PlayShoot(float hold = -1f) // generic one-shot if you ever need it
    {
        string state = $"{DirPrefix(lastDir)}_{shootStateSuffix}";
        animator.CrossFade(state, crossFade);
        busyUntil = Time.time + (hold > 0f ? hold : ClipLen(state));
    }

    public void PlayHurt(float hold = 0.18f) { PlayOneShot("Hurt", hold); }

    public void PlayDeath()
    {
        animator.CrossFade($"{DirPrefix(lastDir)}_Death", crossFade);
        busyUntil = float.MaxValue;
    }

    void PlayOneShot(string action, float hold)
    {
        string dir = DirPrefix(lastDir);
        animator.CrossFade($"{dir}_{action}", crossFade);
        busyUntil = Time.time + hold;
    }

    public void SetFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f) return;
        lastDir = dir.normalized;

        if (Mathf.Abs(lastDir.x) > Mathf.Abs(lastDir.y))
            sr.flipX = lastDir.x > 0;
    }

    float ClipLen(string stateName)
    {
        var rc = animator.runtimeAnimatorController;
        foreach (var c in rc.animationClips)
            if (c && c.name == stateName) return c.length;
        return 0.18f; // fallback
    }

    // Add to your PlayerAnimationView
    public void UpdateShootFacing(Vector2 aim)
    {
        if (aim.sqrMagnitude <= 0.0001f) return;

        // Update facing/flip as usual
        SetFacing(aim);

        if (!bowHolding) return;

        string newState = $"{DirPrefix(lastDir)}_{shootStateSuffix}";
        if (newState == activeShootState) return;

        // Preserve progress if not locked; keep lock if locked
        float t = lockAtNormalized;
        var st = animator.GetCurrentAnimatorStateInfo(BaseLayer);
        if (!bowLocked && st.IsName(activeShootState))
            t = st.normalizedTime % 1f;

        activeShootState = newState;

        // If locked, stay frozen at lock point; else continue from t
        if (bowLocked)
        {
            animator.Play(activeShootState, BaseLayer, lockAtNormalized);
            animator.speed = 0f;
        }
        else
        {
            animator.speed = 1f;
            animator.Play(activeShootState, BaseLayer, t);
        }
    }

}
