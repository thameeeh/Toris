using UnityEngine;

/// Controller = brain that composes Character + Weapon profiles and drives the View.
/// - Resolves state names (dir + suffix) from data assets
/// - Runs a tiny FSM for hold/lock/release + one-shots
/// - Delegates all Animator calls to PlayerAnimationView
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] PlayerAnimationView _view;
    [SerializeField] CharacterAnimSO _character;
    [SerializeField] WeaponProfile _weapon;

    [Header("General")]
    [Range(0f, 0.2f)] public float resumeEpsilon = 0.02f;
    private float _hurtEndTime = 0f;
    private float _deathEndTime = 0f;

    public bool CanMove()
    {
        return _state == AnimState.Locomotion;
    }

    #region FSM

    enum AnimState { Locomotion, HoldUnlocked, HoldLocked, OneShotBusy, Hurt, Dead }
    AnimState _state = AnimState.Locomotion;
    float _stateUntil;

    #endregion

    #region Runtime

    Vector2 _lastDir = Vector2.down; // remembered facing
    string _activeActionKey;        // e.g., "Shoot"
    int _activeHash;             // current action state hash (directional)
    string _activeName;             // current action state name (debug)
    float _activeLockAt;           // weapon.behavior
    float _activeFade;             // weapon.behavior
    bool _activeUsesLock;         // weapon.behavior
    bool _holding;                // currently holding?
    bool _locked;                 // paused at lock?

    #endregion

    private void Reset()
    {
        if (!_view) _view = GetComponent<PlayerAnimationView>();
        if (!_character) Debug.LogError("[AnimCtrl] Missing CharacterAnimProfile.", this);
        if (!_weapon) Debug.LogError("[AnimCtrl] Missing WeaponProfile.", this);
        ValidateCoreStates();
    }

    private void Awake()
    {
        if (!_view) Debug.LogError("[AnimCtrl] Missing PlayerAnimationView.", this);
    }

    #region Public API
    /// Call every frame with movement vector
    public void Tick(Vector2 move)
    {
        if (_state == AnimState.Dead)
        {
            // Wait until animation ends
            if (Time.time >= _deathEndTime && _deathEndTime > 0f)
            {
                _deathEndTime = 0f;
                OnDeathAnimationFinished();
            }
            return; // no updates while dead
        }

        if (_state == AnimState.Hurt)
        {
            // Wait until timer expires, then resume locomotion
            if (Time.time >= _hurtEndTime)
                _state = AnimState.Locomotion;
            else
                return; // still in hurt, skip update
        }


        // State responses first
        if (_state == AnimState.HoldUnlocked || _state == AnimState.HoldLocked)
        {
            UpdateHoldLock();
            return;
        }
        if (_state == AnimState.OneShotBusy)
        {
            if (Time.time < _stateUntil) return;
            _state = AnimState.Locomotion;
        }

        // Locomotion (Idle/Walk)
        if (move.sqrMagnitude > 0.0001f) _lastDir = move.normalized;
        _view.SetFacing(_lastDir); // TODO: remove flip when L/R art lands

        string dir = _view.DirPrefix(_lastDir); // "U" / "S" / "D"  // TODO: L/R later
        string suffix = move.sqrMagnitude > 0.0001f ? _character.locomotionWalkSuffix
                                                    : _character.locomotionIdleSuffix;

        int hash = _view.StateHash($"{dir}_{suffix}");
        _view.CrossFadeIfChanged(hash, 0f); // lightweight; Play on same state is safe
    }

    public Vector2 CurrentFacing => _lastDir;

    /// Begin a hold action (defaults to "Shoot").
    public void BeginHold(string actionKey = "Shoot")
    {
        var res = Resolve(actionKey, _lastDir);
        if (!_view.HasState(res.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{res.name}' on layer {_character.baseLayer}", this);
            return;
        }

        _activeActionKey = actionKey;
        _activeHash = res.hash;
        _activeName = res.name;
        _activeLockAt = res.lockAt;
        _activeFade = res.fade;
        _activeUsesLock = res.usesLock;

        _holding = _activeUsesLock;
        _locked = false;

        _view.SetPaused(false);
        _view.CrossFade(_activeHash, _activeFade);

        if (_activeUsesLock)
        {
            _state = AnimState.HoldUnlocked;
        }
        else
        {
            // one-shot without lock — estimate remaining by clip length
            float dur = Mathf.Max(0.1f, _view.ClipLenByHash(_activeHash));
            _state = AnimState.OneShotBusy;
            _stateUntil = Time.time + dur;
        }
    }

    /// Release a hold action (only meaningful if the action uses a lock).
    public void ReleaseHold()
    {
        if (!_holding) return;

        _holding = false;
        _view.SetPaused(false);

        float t = Mathf.Clamp01(_activeLockAt + resumeEpsilon);
        _view.Play(_activeHash, t);

        float len = _view.ClipLenByHash(_activeHash);
        float remaining = Mathf.Max(0.1f, (1f - t) * (len > 0f ? len : 0.18f));

        _locked = false;
        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + remaining;
    }

    /// Update facing/aim while holding (keeps lock or preserves time).
    public void UpdateAim(Vector2 aim)
    {
        if (aim.sqrMagnitude > 0.0001f)
        {
            _lastDir = aim.normalized;
            _view.SetFacing(_lastDir); // TODO: remove flip when L/R art lands
        }

        if (_state != AnimState.HoldUnlocked && _state != AnimState.HoldLocked) return;

        var res = Resolve(_activeActionKey, _lastDir);
        if (res.hash == _activeHash) return; // same directional state, nothing to do

        float t = _activeLockAt;
        var st = _view.Current();
        if (!_locked) t = st.normalizedTime % 1f; // preserve progress during hold

        _activeHash = res.hash;
        _activeName = res.name;

        if (_locked)
        {
            _view.Play(_activeHash, _activeLockAt);
            _view.SetPaused(true);
        }
        else
        {
            _view.SetPaused(false);
            _view.Play(_activeHash, t);
        }
    }

    /// One-Shots
    public void PlayHurt(float hold = 0f)
    {
        if (_state == AnimState.Dead) return;

        string dir = _view.DirPrefix(_lastDir);
        int hash = _view.StateHash($"{dir}_{_character.DefaultSuffixFor("Hurt")}");

        _view.CrossFade(hash, 0.05f);
        _state = AnimState.Hurt;

        float clipLen = _view.ClipLenByHash(hash);
        _hurtEndTime = Time.time + (hold > 0f ? hold : clipLen);
    }


    public void PlayDeath()
    {
        string dir = _view.DirPrefix(_lastDir);
        int hash = _view.StateHash($"{dir}_{_character.DefaultSuffixFor("Death")}");
        _view.CrossFade(hash, 0.05f);

        _state = AnimState.Dead;

        float clipLen = _view.ClipLenByHash(hash);
        _deathEndTime = Time.time + clipLen; // play full animation once
    }
    private void OnDeathAnimationFinished()
    {
        _view.SetPaused(true); // freeze on last frame

        // TODO: trigger post-death flow later
        Debug.Log("[PlayerAnim] Death animation finished.");
    }


    #endregion

    #region Internals

    void UpdateHoldLock()
    {
        var st = _view.Current();
        // Accept during crossfade or when fully in the state
        if (st.shortNameHash != _activeHash && !st.IsName(_activeName)) return;

        float nt = st.normalizedTime % 1f;
        if (!_locked && nt >= _activeLockAt)
        {
            _view.Play(_activeHash, _activeLockAt);
            _view.SetPaused(true);
            _locked = true;
            _state = AnimState.HoldLocked;
        }
    }

    // Compose Character + Weapon -> final state and behavior knobs
    (int hash, float lockAt, float fade, bool usesLock, string name) Resolve(string actionKey, Vector2 dir)
    {
        // 1) suffix from weapon override or character default
        var w = _weapon ? _weapon.Get(actionKey) : null;
        string suffix = !string.IsNullOrEmpty(w?.animSuffixOverride)
            ? w.animSuffixOverride
            : _character.DefaultSuffixFor(actionKey);

        // 2) direction prefix (U/S/D today) // TODO: L/R when art lands
        string p = _view.DirPrefix(dir);

        // 3) final name & hash
        string name = $"{p}_{suffix}";
        int hash = _view.StateHash(name);

        // 4) behavior knobs (defaults if weapon is null/missing)
        float lockAt = w?.lockAt ?? 0f;
        float fade = w?.crossFade ?? 0.05f;
        bool usesLock = w?.usesLock ?? false;

        return (hash, lockAt, fade, usesLock, name);
    }

    void ValidateCoreStates()
    {
        if (!_character || !_view) return;

        // Locomotion U/S/D (we warn but don’t block)
        foreach (var dir in new[] { "U", "S", "D" }) // TODO: migrate to L/R later
        {
            string idle = $"{dir}_{_character.locomotionIdleSuffix}";
            string walk = $"{dir}_{_character.locomotionWalkSuffix}";
            if (!_view.HasState(_view.StateHash(idle)))
                Debug.LogWarning($"[AnimCtrl] Missing state: {idle}", this);
            if (!_view.HasState(_view.StateHash(walk)))
                Debug.LogWarning($"[AnimCtrl] Missing state: {walk}", this);
        }

        // Shoot U/S/D (common pitfall)
        if (_weapon != null && _weapon.Get("Shoot") != null)
        {
            string shootSuffix = string.IsNullOrEmpty(_weapon.Get("Shoot").animSuffixOverride)
                ? _character.DefaultSuffixFor("Shoot")
                : _weapon.Get("Shoot").animSuffixOverride;

            foreach (var dir in new[] { "U", "S", "D" }) // TODO: migrate to L/R later
            {
                string nm = $"{dir}_{shootSuffix}";
                if (!_view.HasState(_view.StateHash(nm)))
                    Debug.LogWarning($"[AnimCtrl] Missing state: {nm}", this);
            }
        }
    }
    #endregion
}
