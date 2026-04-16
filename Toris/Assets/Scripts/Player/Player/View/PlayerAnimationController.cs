using UnityEngine;

/// Controller = animation-facing runtime glue between gameplay intent and Animator.
/// Animator owns state routing, while this controller keeps shoot visuals aligned
/// to gameplay timing and handles dash particle playback.
public class PlayerAnimationController : MonoBehaviour
{
    private const float MIN_DIRECTION_SQR_MAGNITUDE = 0.0001f;
    private const float MIN_READY_DURATION = 0.01f;
    private const string DashKey = "Dash";
    private const string DashParticleKey = "DashP";
    private const string DeathKey = "Death";
    private const string HurtKey = "Hurt";
    private const string ShootDrawKey = "ShootDraw";
    private const string ShootHoldKey = "ShootHold";
    private const string ShootReleaseKey = "ShootRelease";

    private const string FacingIndexParam = "FacingIndex";
    private const string IsMovingParam = "IsMoving";
    private const string IsInActionParam = "IsInAction";
    private const string IsDeadParam = "IsDead";

    [Header("Scene refs")]
    [SerializeField] private PlayerAnimationView _view;
    [SerializeField] private CharacterAnimSO _character;
    [SerializeField] private WeaponProfile _weapon;

    [SerializeField] private int _dashParticleSortingOrderOffset = -1;
    [SerializeField] private float _dashParticleDestroyPadding = 0.05f;

    private enum AnimState
    {
        Locomotion,
        ShootDrawing,
        ShootHolding,
        OneShotBusy,
        Dead
    }

    private struct ActionRuntime
    {
        public string actionKey;
        public string dirToken;
        public string name;
        public int hash;
        public float fade;
    }

    private AnimState _state = AnimState.Locomotion;
    private Vector2 _lastDir = Vector2.down;
    private float _stateUntil;

    private string _activeActionKey;
    private string _activeName;
    private int _activeHash;

    private void LogShoot(string message)
    {
        PlayerShootDebug.Log(this, "AnimCtrl", message);
    }

    private static string FormatVector(Vector2 value)
    {
        return $"({value.x:F2}, {value.y:F2})";
    }

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

        if (_state == AnimState.ShootDrawing || _state == AnimState.ShootHolding)
        {
            return;
        }

        if (_state == AnimState.OneShotBusy && Time.time >= _stateUntil)
        {
            LogShoot($"Returning to locomotion from OneShotBusy. activeAction={_activeActionKey} facing={FormatVector(_lastDir)}");
            _state = AnimState.Locomotion;
            SetActionLock(false);
            _view.SetPlaybackSpeed(1f);
        }
    }

    public void BeginShoot(float readyDuration, Vector2 initialAim)
    {
        if (_state == AnimState.Dead)
            return;

        if (initialAim.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            _lastDir = initialAim.normalized;

        ActionRuntime draw = Resolve(ShootDrawKey, _lastDir);
        float drawSpeed = ResolveShootDrawSpeed(draw.hash, readyDuration);
        LogShoot($"BeginShoot. facing={FormatVector(_lastDir)} initialAim={FormatVector(initialAim)} readyDuration={readyDuration:F3} drawState={draw.name} drawSpeed={drawSpeed:F2}");
        if (!TryPlayShootAction(
                ShootDrawKey,
                restartIfCurrent: true,
                playbackSpeed: drawSpeed,
                playDirect: true,
                normalizedTime: 0f,
                out _))
        {
            return;
        }

        _state = AnimState.ShootDrawing;
    }

    public void EnterShootHold()
    {
        if (_state == AnimState.Dead)
            return;

        if (_state != AnimState.ShootDrawing && _state != AnimState.ShootHolding)
            return;

        LogShoot($"EnterShootHold. facing={FormatVector(_lastDir)}");
        if (!TryPlayShootAction(
                ShootHoldKey,
                restartIfCurrent: true,
                playbackSpeed: 1f,
                playDirect: true,
                normalizedTime: 0f,
                out _))
        {
            return;
        }

        _state = AnimState.ShootHolding;
    }

    public void ReleaseShoot()
    {
        if (_state == AnimState.Dead)
            return;

        if (_state != AnimState.ShootDrawing && _state != AnimState.ShootHolding)
            return;

        LogShoot($"ReleaseShoot. facing={FormatVector(_lastDir)} state={_state}");
        if (!TryPlayShootAction(
                ShootReleaseKey,
                restartIfCurrent: false,
                playbackSpeed: 1f,
                playDirect: true,
                normalizedTime: 0f,
                out ActionRuntime action))
        {
            return;
        }

        float len = _view.ClipLenByHash(action.hash);
        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + Mathf.Max(0.1f, len);
    }

    public void PlayAbilityShootRelease(Vector2 shotDirection)
    {
        if (_state == AnimState.Dead)
            return;

        if (shotDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            _lastDir = shotDirection.normalized;

        CancelCurrentHold();
        LogShoot($"PlayAbilityShootRelease. dir={FormatVector(_lastDir)}");

        if (!TryPlayShootAction(
                ShootReleaseKey,
                restartIfCurrent: false,
                playbackSpeed: 1f,
                playDirect: true,
                normalizedTime: 0f,
                out ActionRuntime action))
        {
            return;
        }

        float len = _view.ClipLenByHash(action.hash);
        _state = AnimState.OneShotBusy;
        _stateUntil = Time.time + Mathf.Max(0.1f, len);
    }

    public void CancelShoot()
    {
        if (_state == AnimState.Dead)
            return;

        if (_state != AnimState.ShootDrawing && _state != AnimState.ShootHolding)
            return;

        LogShoot($"CancelShoot. state={_state} facing={FormatVector(_lastDir)}");
        CancelCurrentHold();
        SetActionLock(false);
        _view.SetPlaybackSpeed(1f);
        _state = AnimState.Locomotion;
    }

    public void UpdateAim(Vector2 aim)
    {
        if (aim.sqrMagnitude <= MIN_DIRECTION_SQR_MAGNITUDE)
            return;

        _lastDir = aim.normalized;
        SyncFacingParameter();

        if (_state != AnimState.ShootDrawing && _state != AnimState.ShootHolding)
            return;

        ActionRuntime updated = Resolve(_state == AnimState.ShootHolding ? ShootHoldKey : ShootDrawKey, _lastDir);
        if (updated.hash == _activeHash)
            return;

        LogShoot($"UpdateAim swapped shoot clip. action={updated.actionKey} from={_activeName} to={updated.name} aim={FormatVector(aim)}");
        _activeName = updated.name;
        _activeHash = updated.hash;
        float normalizedTime = _state == AnimState.ShootHolding ? 0f : Mathf.Clamp01(CurrentNormalizedTime());
        _view.Play(_activeHash, normalizedTime);
    }

    public void PlayDash(Vector2 dashDirection)
    {
        if (_state == AnimState.Dead)
            return;

        if (dashDirection.sqrMagnitude > MIN_DIRECTION_SQR_MAGNITUDE)
            _lastDir = dashDirection.normalized;

        CancelCurrentHold();
        LogShoot($"PlayDash. dir={FormatVector(_lastDir)}");

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

        CancelCurrentHold();
        LogShoot($"PlayHurt. facing={FormatVector(_lastDir)}");

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
        CancelCurrentHold();
        LogShoot($"PlayDeath. facing={FormatVector(_lastDir)}");

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

    private float CurrentNormalizedTime() => _view.Current().normalizedTime % 1f;

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
        if (!string.IsNullOrEmpty(_activeActionKey))
        {
            LogShoot($"CancelCurrentHold. action={_activeActionKey} stateName={_activeName}");
        }

        _activeActionKey = string.Empty;
        _activeName = string.Empty;
        _activeHash = 0;
        _view.SetPlaybackSpeed(1f);
    }

    private float ResolveShootDrawSpeed(int drawHash, float readyDuration)
    {
        float drawClipLength = _view.ClipLenByHash(drawHash);
        if (drawClipLength <= MIN_READY_DURATION || readyDuration <= MIN_READY_DURATION)
            return 1f;

        return Mathf.Max(0.01f, drawClipLength / readyDuration);
    }

    private bool TryPlayShootAction(
        string actionKey,
        bool restartIfCurrent,
        float playbackSpeed,
        bool playDirect,
        float normalizedTime,
        out ActionRuntime action)
    {
        action = Resolve(actionKey, _lastDir);
        if (!_view.HasState(action.hash))
        {
            Debug.LogError($"[AnimCtrl] Missing state '{action.name}' on layer {_character.baseLayer}", this);
            return false;
        }

        _activeActionKey = action.actionKey;
        _activeName = action.name;
        _activeHash = action.hash;

        LogShoot(
            $"TryPlayShootAction. action={action.actionKey} state={action.name} facing={action.dirToken} speed={playbackSpeed:F2} direct={playDirect} normalizedTime={normalizedTime:F2}");

        SyncFacingParameter();
        SetActionLock(true);
        _view.SetBool(IsMovingParam, false);
        _view.SetPlaybackSpeed(playbackSpeed);

        if (playDirect)
        {
            _view.Play(action.hash, normalizedTime);
        }
        else if (restartIfCurrent && _view.IsCurrent(action.hash))
        {
            _view.Play(action.hash, 0f);
        }
        else
        {
            _view.CrossFade(action.hash, action.fade);
        }

        return true;
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
