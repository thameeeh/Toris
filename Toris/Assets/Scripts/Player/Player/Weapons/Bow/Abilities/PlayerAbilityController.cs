using UnityEngine;

public class PlayerAbilityController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerInputReaderSO _input;
    [SerializeField] private PlayerStats _stats;
    [SerializeField] private PlayerBowController _bow;

    [System.Serializable]
    public class AbilitySlot
    {
        [SerializeField] private PlayerAbilitySO _abilityDefinition;

        private PlayerAbilityRuntime _runtime;

        public PlayerAbilitySO AbilityDefinition => _abilityDefinition;
        public PlayerAbilityRuntime Runtime => _runtime;

        public void Initialize()
        {
            _runtime = _abilityDefinition != null
                ? _abilityDefinition.CreateRuntime()
                : null;

            _runtime?.Initialize(_abilityDefinition);
        }
    }

    [Header("Ability Slots")]
    [SerializeField] private AbilitySlot _ability1;
    [SerializeField] private AbilitySlot _ability2;

    public PlayerAbilityRuntime Ability1Runtime => _ability1?.Runtime;
    public PlayerAbilityRuntime Ability2Runtime => _ability2?.Runtime;
    public PlayerAbilityContext AbilityContext => _context;

    private PlayerAbilityContext _context;

    private void Awake()
    {
        _context = new PlayerAbilityContext
        {
            controller = this,
            input = _input,
            stats = _stats,
            bow = _bow
        };

        _ability1?.Initialize();
        _ability2?.Initialize();
    }

    private void OnEnable()
    {
        if (_input == null)
        {
            Debug.LogWarning("[Ability] PlayerInputReader is not assigned on PlayerAbilityController", this);
            return;
        }

        _input.OnAbility1Pressed += OnAbility1Pressed;
        _input.OnAbility2Started += OnAbility2Pressed;
        _input.OnAbility2Released += OnAbility2Released;
    }

    private void OnDisable()
    {
        if (_input == null)
            return;

        _input.OnAbility1Pressed -= OnAbility1Pressed;
        _input.OnAbility2Started -= OnAbility2Pressed;
        _input.OnAbility2Released -= OnAbility2Released;
    }

    private void OnValidate()
    {
        if (_input == null)
        {
            Debug.LogError($"<b><color=red>[PlayerAbilityController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}</b>", this);
        }
    }

    private void Update()
    {
        _ability1?.Runtime?.Tick(_context);
        _ability2?.Runtime?.Tick(_context);
    }

    private void OnAbility1Pressed()
    {
        _ability1?.Runtime?.OnButtonDown(_context);
    }

    private void OnAbility2Pressed()
    {
        _ability2?.Runtime?.OnButtonDown(_context);
    }

    private void OnAbility2Released()
    {
        _ability2?.Runtime?.OnButtonUp(_context);
    }
}