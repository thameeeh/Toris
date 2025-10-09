using System.Collections.Generic;
using UnityEngine;

/// View = the only place that touches Animator/SpriteRenderer
/// - Caches clip hashes/lengths once
/// - Plays/crossfades by hash
/// - Computes direction prefix (U/S/D for now)  // TODO: switch S -> L/R when art lands
/// - Handles simple facing (flip)               // TODO: remove flip when L/R used
public class PlayerAnimationView : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] Animator _animator;
    [SerializeField] SpriteRenderer _sprite;

    [Header("Optional: character profile for base layer")]
    [SerializeField] CharacterAnimSO _character; // used for baseLayer (and future dir mode)

    // Layer we query/play on (default 0 if no profile assigned)
    int BaseLayer => _character ? _character.baseLayer : 0;

    // Caches to avoid string lookups & clip iteration at runtime
    readonly Dictionary<string, int> _nameToHash = new();
    readonly Dictionary<int, float> _hashToLen = new();

    void Awake()
    {
        if (!_animator) Debug.LogError("[AnimView] Missing Animator.", this);
        if (!_sprite) Debug.LogError("[AnimView] Missing SpriteRenderer.", this);

        var rc = _animator ? _animator.runtimeAnimatorController : null;
        if (!rc) { Debug.LogError("[AnimView] Missing RuntimeAnimatorController.", this); return; }

        _nameToHash.Clear();
        _hashToLen.Clear();

        foreach (var clip in rc.animationClips)
        {
            if (!clip) continue;
            int h = Animator.StringToHash(clip.name);
            _nameToHash[clip.name] = h;
            if (!_hashToLen.ContainsKey(h)) _hashToLen[h] = clip.length;
        }
    }

    #region Helpers

    /// Build directional prefix from a vector using current art mode
    /// Up/Side/Down => U_, S_, D_
    /// TODO: add L/R, return L_ or R_ instead of S_
    public string DirPrefix(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f) dir = Vector2.down; // default facing

        // TODO: when FourDir art is ready -> return "L" or "R" here instead of "S"
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y)) return "S";
        return dir.y >= 0f ? "U" : "D";
    }

    /// Face the sprite: for now we flip on side; remove this when L/R clips add
    public void SetFacing(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f) return;
        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
        {
            // TODO: when using L_/R_ sprites, delete flip entirely
            _sprite.flipX = dir.x > 0f;
        }
    }

    public int StateHash(string stateName)
    {
        if (_nameToHash.TryGetValue(stateName, out var h)) return h;
        // Fallback if clip added after Awake (rare during play)
        h = Animator.StringToHash(stateName);
        _nameToHash[stateName] = h;
        return h;
    }

    public bool IsCurrent(int stateHash)
    {
        var st = Current();
        return st.shortNameHash == stateHash;
    }

    public void CrossFadeIfChanged(int stateHash, float fadeSeconds)
    {
        if (!IsCurrent(stateHash))
            _animator.CrossFade(stateHash, fadeSeconds);
    }
    #endregion

    public float ClipLenByHash(int hash) =>
        _hashToLen.TryGetValue(hash, out var len) ? len : 0.18f; // tiny safe fallback

    public AnimatorStateInfo Current() => _animator.GetCurrentAnimatorStateInfo(BaseLayer);

    public bool HasState(int hash) => _animator.HasState(BaseLayer, hash);

    /// Crossfade by hash (don’t pass names from controller)
    public void CrossFade(int stateHash, float fadeSeconds) =>
        _animator.CrossFade(stateHash, fadeSeconds);

    /// Play at a normalized time (useful for lock/resume)
    public void Play(int stateHash, float normalizedTime) =>
        _animator.Play(stateHash, BaseLayer, normalizedTime);

    /// Centralized pause/resume so speed doesn’t drift
    public void SetPaused(bool paused) => _animator.speed = paused ? 0f : 1f;
}
