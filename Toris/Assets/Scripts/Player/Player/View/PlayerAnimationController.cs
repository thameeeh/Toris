using UnityEngine;

/// Controller = animation-facing runtime glue between gameplay intent and Animator.
/// Animator owns state routing, while this controller keeps special timing for
/// shoot hold/release and dash particle playback.
public class PlayerAnimationController : MonoBehaviour
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const string DashKey = "Dash";
    private const string DashParticleKey = "DashP";
    private const string DeathKey = "Death";
    private const string HurtKey = "Hurt";
    private const string ShootFullKey = "ShootF";
    private const string ShootShortKey = "ShootS";

    private const string FacingIndexParam = "FacingIndex";
    private const string IsMovingParam = "IsMoving";
    private const string IsInActionParam = "IsInAction";
    private const string IsDeadParam = "IsDead";

    [Header("Scene refs")]
    [SerializeField] private PlayerAnimationView _view;
    [SerializeField] private CharacterAnimSO _character;
    [SerializeField] private WeaponProfile _weapon;

    [Header("General")]
    [Range(0f, 0.2f)] public float resumeEpsilon = 0.02f;
    [SerializeField] private int _dashParticleSortingOrderOffset = -1;
    [SerializeField] private float _dashParticleDestroyPadding = 0.05f;

    private enum AnimState
    {
        Locomotion,
        HoldUnlocked,
        HoldLocked,
        OneShotBusy,
        Dead
    }

    private struct ActionRuntime
    {
        public string actionKey;
        public string dirToken;
        public string name;
        public int hash;
        public float lockAt;
        public bool usesLock;
        public float repeatWindow;
        public float fade;
    }

    private AnimState _state = AnimState.Locomotion;
    private Vector2 _lastDir = Vector2.down;
    private float _stateUntil;
    private float _lastShotReleaseTime = float.NegativeInfinity;

    private string _activeActionKey;
    private string _activeName;
    private int _activeHash;
    private float _activeLockAt;
    private bool _activeUsesLock;
    private bool _holding;
    private bool _locked;
    private bool _shotNockReached;

    public event System.Action ShootNockReached;

    private void Reset()
    {
        if (!_view) _view = GetComponent<PlayerAnimationView>();
        if (!_character) Debug.LogError("[AnimCtrl] Missing CharacterAnimProfile.", this);
        if (!_weapon) Debug.LogError("[AnimCtrl] Missing WeaponProfile.", this);
    }

    private void Awake()
    {
        if (!_view) Debug.LogError("[AnimCtrl] Missing PlayerAnimationView.", this);
        if (!_character) Debug.LogError("[AnimCtrl] Missing CharacterAnimProfile.", this);
        if (!_weapon) Debug.LogError("[AnimCtrl] Missing WeaponProfile.", this);
    }

    public Vector2 CurrentFacing => _lastDir;

    public void Tick(Vector2 move)
    {
        if (_view == null || _character == null)
            return;

        bool isMoving = move.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE;
        if (isMoving && _state == AnimState.Locomotion)
            _lastDir = move.normalized;

        SyncFacingParameter();

        if (_state == AnimState.Dead)
        {
            _view.SetBool(IsMovingParam, false);
            return;
        }

        _view.SetBool(IsMovingParam, isMoving);

        if (_state == AnimState.HoldUnlocked || _state == AnimState.HoldLocked)
        {
            return;
        }

        if (_state == AnimState.OneShotBusy && Time.time >= _stateUntil)
        {
            _state = AnimState.Locomotion;
            SetActionLock(false);
        }
    }

    public void BeginShoot()
    {
        if (_state == AnimState.Dead)
            return;

        ActionRuntime action = Resolve(ShouldUseShortShoot() ? ShootShortKey : ShootFullKey, _lastDir);
        if (!_view.HasState(action.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{action.name}' on layer {_character.baseLayer}", this);
            return;
        }

        CancelCurrentHold();

        _activeActionKey = action.actionKey;
        _activeName = action.name;
        _activeHash = action.hash;
        _activeLockAt = action.lockAt;
        _activeUsesLock = action.usesLock;
        _holding = action.usesLock;
        _locked = false;
        _shotNockReached = false;

        SetActionLock(true);
        _view.SetBool(IsMovingParam, false);
        _view.SetPaused(false);

        // Consecutive short shots can request the same Animator state again while the
        // previous release tail is still active. Force a restart from frame 0 in that
        // case so the authored hold event fires again and the shot can nock properly.
        if (_view.IsCurrent(_activeHash))
        {
            _view.Play(_activeHash, 0f);
        }
        else
        {
            _view.CrossFade(_activeHash, action.fade);
        }

        if (_activeUsesLock)
        {
            _state = AnimState.HoldUnlocked;
        }
        else
        {
            _state = AnimState.OneShotBusy;
            _stateUntil = Time.time + Mathf.Max(0.1f, _view.ClipLenByHash(_activeHash));
        }
    }

    public void ReleaseShoot()
    {
        if (_state == AnimState.Dead || !_holding)
            return;

        _holding = false;
        _view.SetPaused(false);

        float startTime = _locked
            ? Mathf.Clamp01(_activeLockAt + resumeEpsilon)
            : Mathf.Clamp01(CurrentNormalizedTime());

        _view.Play(_activeHash, startTime);

        float len = _view.ClipLenByHash(_activeHash);
        float remaining = Mathf.Max(0.1f, (1f - startTime) * len);

        _locked = false;
        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + remaining;
        _lastShotReleaseTime = Time.time;
    }

    public void PlayAbilityShootRelease(Vector2 shotDirection, bool useShortVariant)
    {
        if (_state == AnimState.Dead)
            return;

        if (shotDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            _lastDir = shotDirection.normalized;

        BreakShootChain();
        CancelCurrentHold();

        ActionRuntime action = Resolve(useShortVariant ? ShootShortKey : ShootFullKey, _lastDir);
        if (!_view.HasState(action.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{action.name}' on layer {_character.baseLayer}", this);
            return;
        }

        float startTime = ResolveShootReleaseStartTime(action);

        _activeActionKey = action.actionKey;
        _activeName = action.name;
        _activeHash = action.hash;
        _activeLockAt = startTime;
        _activeUsesLock = false;
        _holding = false;
        _locked = false;
        _shotNockReached = false;

        SyncFacingParameter();
        SetActionLock(true);
        _view.SetBool(IsMovingParam, false);
        _view.SetPaused(false);

        if (_view.IsCurrent(action.hash))
        {
            _view.Play(action.hash, startTime);
        }
        else
        {
            _view.CrossFade(action.hash, action.fade, startTime);
        }

        float len = _view.ClipLenByHash(action.hash);
        float remaining = Mathf.Max(0.1f, (1f - startTime) * len);

        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + remaining;
    }

    public void CancelShoot()
    {
        if (_state == AnimState.Dead)
            return;

        if (_state != AnimState.HoldUnlocked && _state != AnimState.HoldLocked)
            return;

        CancelCurrentHold();
        SetActionLock(false);
        _state = AnimState.Locomotion;
    }

    public void OnShootHoldFrame(AnimationEvent animationEvent)
    {
        if (_state != AnimState.HoldUnlocked || !_holding || !_activeUsesLock || _shotNockReached)
            return;

        AnimatorStateInfo stateInfo = _view.Current();
        if (stateInfo.shortNameHash != _activeHash && !stateInfo.IsName(_activeName))
            return;

        float clipLength = _view.ClipLenByHash(_activeHash);
        float normalizedTime = clipLength > 0f
            ? Mathf.Clamp01(animationEvent.time / clipLength)
            : Mathf.Clamp01(CurrentNormalizedTime());

        _activeLockAt = normalizedTime;
        _shotNockReached = true;
        _locked = true;
        _state = AnimState.HoldLocked;

        _view.Play(_activeHash, _activeLockAt);
        _view.SetPaused(true);
        ShootNockReached?.Invoke();
    }

    public void UpdateAim(Vector2 aim)
    {
        if (aim.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            return;

        _lastDir = aim.normalized;
        SyncFacingParameter();

        if (_state != AnimState.HoldUnlocked && _state != AnimState.HoldLocked)
            return;

        ActionRuntime updated = Resolve(_activeActionKey, _lastDir);
        if (updated.hash == _activeHash)
            return;

        float normalizedTime = _locked
            ? _activeLockAt
            : Mathf.Clamp01(CurrentNormalizedTime());

        _activeName = updated.name;
        _activeHash = updated.hash;

        _view.SetPaused(false);
        _view.Play(_activeHash, normalizedTime);

        if (_locked)
            _view.SetPaused(true);
    }

    public void PlayDash(Vector2 dashDirection)
    {
        if (_state == AnimState.Dead)
            return;

        if (dashDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            _lastDir = dashDirection.normalized;

        BreakShootChain();
        CancelCurrentHold();

        ActionRuntime dash = Resolve(DashKey, _lastDir);
        if (!_view.HasState(dash.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{dash.name}' on layer {_character.baseLayer}", this);
            return;
        }

        SyncFacingParameter();
        SetActionLock(true);
        _view.SetBool(IsMovingParam, false);
        _view.CrossFade(dash.hash, dash.fade);

        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + Mathf.Max(0.1f, _view.ClipLenByHash(dash.hash));

        SpawnDashParticles();
    }

    public void PlayHurt()
    {
        if (_state == AnimState.Dead)
            return;

        BreakShootChain();
        CancelCurrentHold();

        ActionRuntime hurt = Resolve(HurtKey, _lastDir);
        if (!_view.HasState(hurt.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{hurt.name}' on layer {_character.baseLayer}", this);
            return;
        }

        SyncFacingParameter();
        SetActionLock(true);
        _view.SetBool(IsMovingParam, false);
        _view.CrossFade(hurt.hash, hurt.fade);

        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + Mathf.Max(0.1f, _view.ClipLenByHash(hurt.hash));
    }

    public void PlayDeath()
    {
        BreakShootChain();
        CancelCurrentHold();

        ActionRuntime death = Resolve(DeathKey, _lastDir);
        if (!_view.HasState(death.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{death.name}' on layer {_character.baseLayer}", this);
            return;
        }

        SyncFacingParameter();
        SetActionLock(true);
        _view.SetBool(IsMovingParam, false);
        _view.CrossFade(death.hash, death.fade);
        _view.SetBool(IsDeadParam, true);
        _state = AnimState.Dead;
    }

    private float CurrentNormalizedTime()
    {
        return _view.Current().normalizedTime % 1f;
    }

    private float ResolveShootReleaseStartTime(ActionRuntime action)
    {
        if (_view.TryGetEventNormalizedTime(action.hash, nameof(OnShootHoldFrame), out float eventNormalizedTime))
            return eventNormalizedTime;

        return Mathf.Clamp01(action.lockAt);
    }

    private bool ShouldUseShortShoot()
    {
        ActionRuntime shortShoot = Resolve(ShootShortKey, _lastDir);
        if (shortShoot.repeatWindow <= 0f)
            return false;

        return Time.time - _lastShotReleaseTime <= shortShoot.repeatWindow;
    }

    private ActionRuntime Resolve(string actionKey, Vector2 dir)
    {
        WeaponProfile.ActionDef weaponAction = _weapon ? _weapon.Get(actionKey) : null;
        string dirToken = _view.DirectionToken(dir);
        string stateName = _character.BuildStateName(actionKey, dirToken, weaponAction?.animSuffixOverride ?? "");

        return new ActionRuntime
        {
            actionKey = actionKey,
            dirToken = dirToken,
            name = stateName,
            hash = _view.StateHash(stateName),
            lockAt = weaponAction?.lockAt ?? 0f,
            usesLock = weaponAction?.usesLock ?? false,
            repeatWindow = weaponAction?.repeatWindow ?? 0f,
            fade = weaponAction?.crossFade ?? 0.05f
        };
    }

    private void SyncFacingParameter()
    {
        _view.SetInt(FacingIndexParam, _view.FacingIndex(_lastDir));
    }

    private void SetActionLock(bool isInAction)
    {
        _view.SetBool(IsInActionParam, isInAction);
    }

    private void CancelCurrentHold()
    {
        _holding = false;
        _locked = false;
        _activeActionKey = string.Empty;
        _activeName = string.Empty;
        _activeHash = 0;
        _activeLockAt = 0f;
        _activeUsesLock = false;
        _shotNockReached = false;
        _view.SetPaused(false);
    }

    private void BreakShootChain()
    {
        _lastShotReleaseTime = float.NegativeInfinity;
    }

    private void SpawnDashParticles()
    {
        RuntimeAnimatorController runtimeController = _view.RuntimeController;
        SpriteRenderer sourceSprite = _view.SpriteRenderer;
        if (runtimeController == null || sourceSprite == null)
            return;

        ActionRuntime dashParticles = Resolve(DashParticleKey, _lastDir);
        float lifeTime = _view.ClipLenByHash(dashParticles.hash);
        if (lifeTime <= 0f)
            return;

        GameObject fxObject = new GameObject($"DashFX_{dashParticles.dirToken}");
        fxObject.transform.position = transform.position;
        fxObject.transform.rotation = Quaternion.identity;

        SpriteRenderer fxSprite = fxObject.AddComponent<SpriteRenderer>();
        fxSprite.sprite = sourceSprite.sprite;
        fxSprite.sharedMaterial = sourceSprite.sharedMaterial;
        fxSprite.color = sourceSprite.color;
        fxSprite.sortingLayerID = sourceSprite.sortingLayerID;
        fxSprite.sortingOrder = sourceSprite.sortingOrder + _dashParticleSortingOrderOffset;

        Animator fxAnimator = fxObject.AddComponent<Animator>();
        fxAnimator.runtimeAnimatorController = runtimeController;
        fxAnimator.Rebind();
        fxAnimator.SetBool(IsDeadParam, false);
        fxAnimator.SetBool(IsMovingParam, false);
        fxAnimator.SetBool(IsInActionParam, true);
        fxAnimator.SetInteger(FacingIndexParam, _view.FacingIndex(_lastDir));
        fxAnimator.Update(0f);
        fxAnimator.Play(dashParticles.hash, 0, 0f);
        fxAnimator.Update(0f);

        Destroy(fxObject, lifeTime + _dashParticleDestroyPadding);
    }
}
