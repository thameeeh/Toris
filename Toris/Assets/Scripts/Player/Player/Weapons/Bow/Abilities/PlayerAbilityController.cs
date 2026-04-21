using System;
using UnityEngine;
using UnityEngine.Serialization;
using OutlandHaven.UIToolkit;

public class PlayerAbilityController : MonoBehaviour
{
    private const int DefaultSlotCount = 5;

    [Header("Refs")]
    [SerializeField] private PlayerInputReaderSO _input;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerBowController _bow;
    [SerializeField] private GameSessionSO _gameSession;

    [System.Serializable]
    public class AbilitySlot
    {
        [SerializeField] private PlayerAbilitySO _abilityDefinition;

        private PlayerAbilityRuntime _runtime;

        public PlayerAbilitySO AbilityDefinition => _abilityDefinition;
        public PlayerAbilityRuntime Runtime => _runtime;
        public bool HasDefinition => _abilityDefinition != null;

        public void Initialize()
        {
            _runtime = _abilityDefinition != null
                ? _abilityDefinition.CreateRuntime()
                : null;

            _runtime?.Initialize(_abilityDefinition);
        }
    }

    [Header("Ability Slots")]
    [SerializeField] private AbilitySlot[] _abilitySlots = new AbilitySlot[DefaultSlotCount];

    [HideInInspector, FormerlySerializedAs("_ability1")]
    [SerializeField] private AbilitySlot _legacyAbility1;

    [HideInInspector, FormerlySerializedAs("_ability2")]
    [SerializeField] private AbilitySlot _legacyAbility2;

    public PlayerAbilityRuntime Ability1Runtime => GetRuntime(0);
    public PlayerAbilityRuntime Ability2Runtime => GetRuntime(1);
    public PlayerAbilityContext AbilityContext => _context;
    public int SlotCount => _abilitySlots?.Length ?? 0;
    public bool IsBowDrawBlocked
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
            {
                PlayerAbilityRuntime runtime = GetRuntime(i);
                if (runtime != null && runtime.IsUnlocked(_context) && runtime.IsBlockingBowDraw(_context))
                {
                    return true;
                }
            }

            return false;
        }
    }
    public bool IsMovementLocked
    {
        get
        {
            for (int i = 0; i < SlotCount; i++)
            {
                PlayerAbilityRuntime runtime = GetRuntime(i);
                if (runtime != null && runtime.IsUnlocked(_context) && runtime.IsBlockingMovement(_context))
                {
                    return true;
                }
            }

            return false;
        }
    }

    private PlayerAbilityContext _context;

    private void Awake()
    {
        EnsureSlotArray();
        MigrateLegacySlotsIfNeeded();
        _gameSession = ResolveGameSession();

        _context = new PlayerAbilityContext
        {
            controller = this,
            input = _input,
            stats = _stats,
            bow = _bow,
            motor = _bow != null ? _bow.GetComponent<PlayerMotor>() : GetComponent<PlayerMotor>(),
            gameSession = _gameSession
        };

        InitializeSlots();
    }

    private void OnEnable()
    {
        if (_input == null)
        {
            Debug.LogWarning("[Ability] PlayerInputReader is not assigned on PlayerAbilityController", this);
            return;
        }

        _input.OnAbilitySlotStarted += HandleAbilitySlotStarted;
        _input.OnAbilitySlotReleased += HandleAbilitySlotReleased;
    }

    private void OnDisable()
    {
        if (_input == null)
            return;

        _input.OnAbilitySlotStarted -= HandleAbilitySlotStarted;
        _input.OnAbilitySlotReleased -= HandleAbilitySlotReleased;
    }

    private void Update()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            PlayerAbilityRuntime runtime = GetRuntime(i);
            if (runtime != null && runtime.IsUnlocked(_context))
            {
                runtime.Tick(_context);
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        EnsureSlotArray();
        MigrateLegacySlotsIfNeeded();

        if (_input == null)
        {
            Debug.LogError($"<b><color=red>[PlayerAbilityController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}</b>", this);
        }
    }
#endif

    public PlayerAbilityRuntime GetRuntime(int slotIndex)
    {
        AbilitySlot slot = GetSlot(slotIndex);
        return slot?.Runtime;
    }

    public PlayerAbilitySO GetAbilityDefinition(int slotIndex)
    {
        AbilitySlot slot = GetSlot(slotIndex);
        return slot?.AbilityDefinition;
    }

    public bool TryActivateSlot(int slotIndex)
    {
        PlayerAbilityRuntime runtime = GetRuntime(slotIndex);
        if (runtime == null || !runtime.IsUnlocked(_context))
            return false;

        runtime.OnButtonDown(_context);
        return true;
    }

    public bool TryReleaseSlot(int slotIndex)
    {
        PlayerAbilityRuntime runtime = GetRuntime(slotIndex);
        if (runtime == null)
            return false;

        runtime.OnButtonUp(_context);
        return true;
    }

    private void HandleAbilitySlotStarted(int slotIndex)
    {
        TryActivateSlot(slotIndex);
    }

    private void HandleAbilitySlotReleased(int slotIndex)
    {
        TryReleaseSlot(slotIndex);
    }

    private AbilitySlot GetSlot(int slotIndex)
    {
        if (_abilitySlots == null || slotIndex < 0 || slotIndex >= _abilitySlots.Length)
            return null;

        return _abilitySlots[slotIndex];
    }

    private void InitializeSlots()
    {
        for (int i = 0; i < SlotCount; i++)
        {
            _abilitySlots[i]?.Initialize();
        }
    }

    private void EnsureSlotArray()
    {
        if (_abilitySlots == null)
        {
            _abilitySlots = new AbilitySlot[DefaultSlotCount];
        }
        else if (_abilitySlots.Length != DefaultSlotCount)
        {
            AbilitySlot[] resizedSlots = new AbilitySlot[DefaultSlotCount];
            Array.Copy(_abilitySlots, resizedSlots, Mathf.Min(_abilitySlots.Length, DefaultSlotCount));
            _abilitySlots = resizedSlots;
        }

        for (int i = 0; i < _abilitySlots.Length; i++)
        {
            _abilitySlots[i] ??= new AbilitySlot();
        }
    }

    private void MigrateLegacySlotsIfNeeded()
    {
        CopyLegacySlotToIndex(_legacyAbility1, 0);
        CopyLegacySlotToIndex(_legacyAbility2, 1);
    }

    private void CopyLegacySlotToIndex(AbilitySlot legacySlot, int slotIndex)
    {
        if (legacySlot == null || !legacySlot.HasDefinition)
            return;

        if (_abilitySlots[slotIndex] == null || !_abilitySlots[slotIndex].HasDefinition)
        {
            _abilitySlots[slotIndex] = legacySlot;
        }
    }

    private GameSessionSO ResolveGameSession()
    {
        return _gameSession != null
            ? _gameSession
            : GameSessionSO.LoadDefault();
    }
}
