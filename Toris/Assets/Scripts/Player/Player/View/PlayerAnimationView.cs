using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationView : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] Animator _animator;
    [SerializeField] SpriteRenderer _sprite;

    [Header("Character profile for base layer")]
    [SerializeField] CharacterAnimSO _character;

    int BaseLayer => _character ? _character.baseLayer : 0;

    readonly Dictionary<string, int> _nameToHash = new();
    readonly Dictionary<int, float> _hashToLen = new();

    void Awake()
    {
        if (!_animator) Debug.LogError("[AnimView] Missing Animator.", this);
        if (!_sprite) Debug.LogError("[AnimView] Missing SpriteRenderer.", this);

        var rc = _animator ? _animator.runtimeAnimatorController : null;
        if (!rc)
        {
            Debug.LogError("[AnimView] Missing RuntimeAnimatorController.", this);
            return;
        }

        _nameToHash.Clear();
        _hashToLen.Clear();

        foreach (var clip in rc.animationClips)
        {
            if (!clip) continue;

            int h = Animator.StringToHash(clip.name);
            _nameToHash[clip.name] = h;

            if (!_hashToLen.ContainsKey(h))
                _hashToLen[h] = clip.length;
        }
    }

    #region Helpers

    public string DirPrefix(Vector2 dir)
    {
        if (dir.sqrMagnitude <= 0.0001f)
            dir = Vector2.down;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x >= 0f ? "R" : "L";

        return dir.y >= 0f ? "U" : "D";
    }

    public int StateHash(string stateName)
    {
        if (_nameToHash.TryGetValue(stateName, out var h))
            return h;

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
        _hashToLen.TryGetValue(hash, out var len) ? len : 0.18f;

    public AnimatorStateInfo Current() => _animator.GetCurrentAnimatorStateInfo(BaseLayer);

    public bool HasState(int hash) => _animator.HasState(BaseLayer, hash);

    public void CrossFade(int stateHash, float fadeSeconds) =>
        _animator.CrossFade(stateHash, fadeSeconds);

    public void Play(int stateHash, float normalizedTime) =>
        _animator.Play(stateHash, BaseLayer, normalizedTime);

    public void SetPaused(bool paused) => _animator.speed = paused ? 0f : 1f;
}