using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// Controller = brain that composes Character + Weapon profiles and drives the View.
/// - Resolves state names (dir + suffix) from data assets
/// - Runs a tiny FSM for hold/lock/release + one-shots
/// - Delegates all Animator calls to PlayerAnimationView
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Scene refs")]
    [SerializeField] PlayerAnimationView view;
    [SerializeField] CharacterAnimSO character;
    [SerializeField] WeaponProfile weapon;

    [Header("General")]
    [Range(0f, 0.2f)] public float resumeEpsilon = 0.02f; // jump past lock when releasing

    #region FSM
    
    enum AnimState { Locomotion, HoldUnlocked, HoldLocked, OneShotBusy, Dead }
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
        if (!view) view = GetComponent<PlayerAnimationView>();
        if (!character) Debug.LogError("[AnimCtrl] Missing CharacterAnimProfile.", this);
        if (!weapon) Debug.LogError("[AnimCtrl] Missing WeaponProfile.", this);
        ValidateCoreStates();
    }

    private void Awake()
    {
        if (!view) Debug.LogError("[AnimCtrl] Missing PlayerAnimationView.", this);
    }

    #region Public API

    /// Call every frame with movement vector
    public void Tick(Vector2 move)
    {
        if (_state == AnimState.Dead) return;

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
        view.SetFacing(_lastDir); // TODO: remove flip when L/R art lands

        string dir = view.DirPrefix(_lastDir); // "U" / "S" / "D"  // TODO: L/R later
        string suffix = move.sqrMagnitude > 0.0001f ? character.locomotionWalkSuffix
                                                    : character.locomotionIdleSuffix;

        int hash = view.StateHash($"{dir}_{suffix}");
        view.CrossFadeIfChanged(hash, 0f); // lightweight; Play on same state is safe
    }


    /// Begin a hold action (defaults to "Shoot").
    public void BeginHold(string actionKey = "Shoot")
    {
        var res = Resolve(actionKey, _lastDir);
        if (!view.HasState(res.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{res.name}' on layer {character.baseLayer}", this);
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

        view.SetPaused(false);
        view.CrossFade(_activeHash, _activeFade);

        if (_activeUsesLock)
        {
            _state = AnimState.HoldUnlocked;
        }
        else
        {
            // one-shot without lock — estimate remaining by clip length
            float dur = Mathf.Max(0.1f, view.ClipLenByHash(_activeHash));
            _state = AnimState.OneShotBusy;
            _stateUntil = Time.time + dur;
        }
    }

    /// Release a hold action (only meaningful if the action uses a lock).
    public void ReleaseHold()
    {
        if (!_holding) return;

        _holding = false;
        view.SetPaused(false);

        float t = Mathf.Clamp01(_activeLockAt + resumeEpsilon);
        view.Play(_activeHash, t);

        float len = view.ClipLenByHash(_activeHash);
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
            view.SetFacing(_lastDir); // TODO: remove flip when L/R art lands
        }

        if (_state != AnimState.HoldUnlocked && _state != AnimState.HoldLocked) return;

        var res = Resolve(_activeActionKey, _lastDir);
        if (res.hash == _activeHash) return; // same directional state, nothing to do

        float t = _activeLockAt;
        var st = view.Current();
        if (!_locked) t = st.normalizedTime % 1f; // preserve progress during hold

        _activeHash = res.hash;
        _activeName = res.name;

        if (_locked)
        {
            view.Play(_activeHash, _activeLockAt);
            view.SetPaused(true);
        }
        else
        {
            view.SetPaused(false);
            view.Play(_activeHash, t);
        }
    }

    /// Optional one-shots
    public void PlayHurt(float hold = 0.18f) => PlayOneShot("Hurt", hold);

    public void PlayDeath()
    {
        string dir = view.DirPrefix(_lastDir);
        int hash = view.StateHash($"{dir}_{character.DefaultSuffixFor("Death")}");
        view.CrossFade(hash, 0.05f);
        _state = AnimState.Dead;
    }
    #endregion

    #region Internals

    void UpdateHoldLock()
    {
        var st = view.Current();
        // Accept during crossfade or when fully in the state
        if (st.shortNameHash != _activeHash && !st.IsName(_activeName)) return;

        float nt = st.normalizedTime % 1f;
        if (!_locked && nt >= _activeLockAt)
        {
            view.Play(_activeHash, _activeLockAt);
            view.SetPaused(true);
            _locked = true;
            _state = AnimState.HoldLocked;
        }
    }

    void PlayOneShot(string actionKey, float hold)
    {
        string dir = view.DirPrefix(_lastDir);
        string suffix = character.DefaultSuffixFor(actionKey); // weapon can override via Resolve if preferred
        int hash = view.StateHash($"{dir}_{suffix}");

        view.CrossFade(hash, 0.05f);
        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + (hold > 0f ? hold : view.ClipLenByHash(hash));
    }

    // Compose Character + Weapon -> final state and behavior knobs
    (int hash, float lockAt, float fade, bool usesLock, string name) Resolve(string actionKey, Vector2 dir)
    {
        // 1) suffix from weapon override or character default
        var w = weapon ? weapon.Get(actionKey) : null;
        string suffix = !string.IsNullOrEmpty(w?.animSuffixOverride)
            ? w.animSuffixOverride
            : character.DefaultSuffixFor(actionKey);

        // 2) direction prefix (U/S/D today) // TODO: L/R when art lands
        string p = view.DirPrefix(dir);

        // 3) final name & hash
        string name = $"{p}_{suffix}";
        int hash = view.StateHash(name);

        // 4) behavior knobs (defaults if weapon is null/missing)
        float lockAt = w?.lockAt ?? 0f;
        float fade = w?.crossFade ?? 0.05f;
        bool usesLock = w?.usesLock ?? false;

        return (hash, lockAt, fade, usesLock, name);
    }

    void ValidateCoreStates()
    {
        if (!character || !view) return;

        // Locomotion U/S/D (we warn but don’t block)
        foreach (var dir in new[] { "U", "S", "D" }) // TODO: migrate to L/R later
        {
            string idle = $"{dir}_{character.locomotionIdleSuffix}";
            string walk = $"{dir}_{character.locomotionWalkSuffix}";
            if (!view.HasState(view.StateHash(idle)))
                Debug.LogWarning($"[AnimCtrl] Missing state: {idle}", this);
            if (!view.HasState(view.StateHash(walk)))
                Debug.LogWarning($"[AnimCtrl] Missing state: {walk}", this);
        }

        // Shoot U/S/D (common pitfall)
        if (weapon != null && weapon.Get("Shoot") != null)
        {
            string shootSuffix = string.IsNullOrEmpty(weapon.Get("Shoot").animSuffixOverride)
                ? character.DefaultSuffixFor("Shoot")
                : weapon.Get("Shoot").animSuffixOverride;

            foreach (var dir in new[] { "U", "S", "D" }) // TODO: migrate to L/R later
            {
                string nm = $"{dir}_{shootSuffix}";
                if (!view.HasState(view.StateHash(nm)))
                    Debug.LogWarning($"[AnimCtrl] Missing state: {nm}", this);
            }
        }
    }
    #endregion
}
