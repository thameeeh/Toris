using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationView : MonoBehaviour
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;

    [Header("Scene refs")]
    [SerializeField] Animator _animator;
    [SerializeField] SpriteRenderer _sprite;

    [Header("Character profile for base layer")]
    [SerializeField] CharacterAnimSO _character;

    int BaseLayer => _character ? _character.baseLayer : 0;

    readonly Dictionary<string, int> _nameToHash = new();
    readonly Dictionary<int, float> _hashToLen = new();
    readonly Dictionary<int, AnimationClip> _hashToClip = new();

    public RuntimeAnimatorController RuntimeController => _animator ? _animator.runtimeAnimatorController : null;
    public SpriteRenderer SpriteRenderer => _sprite;

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
        _hashToClip.Clear();

        foreach (var clip in rc.animationClips)
        {
            if (!clip) continue;

            int h = Animator.StringToHash(clip.name);
            _nameToHash[clip.name] = h;

            if (!_hashToLen.ContainsKey(h))
                _hashToLen[h] = clip.length;

            if (!_hashToClip.ContainsKey(h))
                _hashToClip[h] = clip;
        }
    }

    #region Helpers

    public int FacingIndex(Vector2 dir)
    {
        if (dir.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            dir = Vector2.down;

        if (Mathf.Abs(dir.x) > Mathf.Abs(dir.y))
            return dir.x >= 0f ? 2 : 1;

        return dir.y >= 0f ? 3 : 0;
    }

    public string DirectionToken(Vector2 dir) => DirectionToken(FacingIndex(dir));

    public string DirectionToken(int facingIndex)
    {
        return facingIndex switch
        {
            1 => "L",
            2 => "R",
            3 => "U",
            _ => "D",
        };
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

    public bool TryGetEventNormalizedTime(int hash, string functionName, out float normalizedTime)
    {
        normalizedTime = 0f;

        if (!_hashToClip.TryGetValue(hash, out var clip) || clip == null)
            return false;

        float clipLength = clip.length;
        if (clipLength <= 0f)
            return false;

        var events = clip.events;
        for (int i = 0; i < events.Length; i++)
        {
            AnimationEvent animationEvent = events[i];
            if (animationEvent == null || animationEvent.functionName != functionName)
                continue;

            normalizedTime = Mathf.Clamp01(animationEvent.time / clipLength);
            return true;
        }

        return false;
    }

    public AnimatorStateInfo Current() => _animator.GetCurrentAnimatorStateInfo(BaseLayer);

    public bool HasState(int hash) => _animator.HasState(BaseLayer, hash);

    public void CrossFade(int stateHash, float fadeSeconds) =>
        _animator.CrossFade(stateHash, fadeSeconds);

    public void CrossFade(int stateHash, float fadeSeconds, float normalizedTime) =>
        _animator.CrossFade(stateHash, fadeSeconds, BaseLayer, normalizedTime);

    public void SetBool(string name, bool value) => _animator.SetBool(name, value);

    public void SetInt(string name, int value) => _animator.SetInteger(name, value);

    public void SetTrigger(string name) => _animator.SetTrigger(name);

    public void ResetTrigger(string name) => _animator.ResetTrigger(name);

    public void Play(int stateHash, float normalizedTime) =>
        _animator.Play(stateHash, BaseLayer, normalizedTime);

    public void SetPaused(bool paused) => _animator.speed = paused ? 0f : 1f;
}
