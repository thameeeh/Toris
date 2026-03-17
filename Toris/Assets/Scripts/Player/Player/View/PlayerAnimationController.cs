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

    #region FSM

    enum AnimState { Locomotion, HoldUnlocked, HoldLocked, OneShotBusy, Hurt, Dead }
    AnimState _state = AnimState.Locomotion;
    float _stateUntil;

    #endregion

    #region Runtime

    Vector2 _lastDir = Vector2.down;
    string _activeActionKey;
    int _activeHash;
    string _activeName;
    float _activeLockAt;
    float _activeFade;
    bool _activeUsesLock;
    bool _holding;
    bool _locked;

    static readonly string[] FourDirs = { "U", "L", "R", "D" };

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

    public void Tick(Vector2 move)
    {
        if (_state == AnimState.Dead)
        {
            if (Time.time >= _deathEndTime && _deathEndTime > 0f)
            {
                _deathEndTime = 0f;
                OnDeathAnimationFinished();
            }
            return;
        }

        if (_state == AnimState.Hurt)
        {
            if (Time.time >= _hurtEndTime)
                _state = AnimState.Locomotion;
            else
                return;
        }

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

        if (move.sqrMagnitude > 0.0001f)
            _lastDir = move.normalized;

        string dir = _view.DirPrefix(_lastDir);
        string suffix = move.sqrMagnitude > 0.0001f
            ? _character.locomotionWalkSuffix
            : _character.locomotionIdleSuffix;

        int hash = _view.StateHash($"{dir}_{suffix}");
        _view.CrossFadeIfChanged(hash, 0f);
    }

    public Vector2 CurrentFacing => _lastDir;

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
            float dur = Mathf.Max(0.1f, _view.ClipLenByHash(_activeHash));
            _state = AnimState.OneShotBusy;
            _stateUntil = Time.time + dur;
        }
    }

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

    public void UpdateAim(Vector2 aim)
    {
        if (aim.sqrMagnitude > 0.0001f)
        {
            _lastDir = aim.normalized;
        }

        if (_state != AnimState.HoldUnlocked && _state != AnimState.HoldLocked) return;

        var res = Resolve(_activeActionKey, _lastDir);
        if (res.hash == _activeHash) return;

        float t = _activeLockAt;
        var st = _view.Current();
        if (!_locked) t = st.normalizedTime % 1f;

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
        _deathEndTime = Time.time + clipLen;
    }

    private void OnDeathAnimationFinished()
    {
        _view.SetPaused(true);
        Debug.Log("[PlayerAnim] Death animation finished.");
    }

    #endregion

    #region Internals

    void UpdateHoldLock()
    {
        var st = _view.Current();

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

    (int hash, float lockAt, float fade, bool usesLock, string name) Resolve(string actionKey, Vector2 dir)
    {
        var w = _weapon ? _weapon.Get(actionKey) : null;

        string suffix = !string.IsNullOrEmpty(w?.animSuffixOverride)
            ? w.animSuffixOverride
            : _character.DefaultSuffixFor(actionKey);

        string p = _view.DirPrefix(dir);

        string name = $"{p}_{suffix}";
        int hash = _view.StateHash(name);

        float lockAt = w?.lockAt ?? 0f;
        float fade = w?.crossFade ?? 0.05f;
        bool usesLock = w?.usesLock ?? false;

        return (hash, lockAt, fade, usesLock, name);
    }

    void ValidateCoreStates()
    {
        if (!_character || !_view) return;

        foreach (var dir in FourDirs)
        {
            string idle = $"{dir}_{_character.locomotionIdleSuffix}";
            string walk = $"{dir}_{_character.locomotionWalkSuffix}";

            if (!_view.HasState(_view.StateHash(idle)))
                Debug.LogWarning($"[AnimCtrl] Missing state: {idle}", this);

            if (!_view.HasState(_view.StateHash(walk)))
                Debug.LogWarning($"[AnimCtrl] Missing state: {walk}", this);
        }

        if (_weapon != null && _weapon.Get("Shoot") != null)
        {
            string shootSuffix = string.IsNullOrEmpty(_weapon.Get("Shoot").animSuffixOverride)
                ? _character.DefaultSuffixFor("Shoot")
                : _weapon.Get("Shoot").animSuffixOverride;

            foreach (var dir in FourDirs)
            {
                string nm = $"{dir}_{shootSuffix}";
                if (!_view.HasState(_view.StateHash(nm)))
                    Debug.LogWarning($"[AnimCtrl] Missing state: {nm}", this);
            }
        }
    }

    #endregion
}