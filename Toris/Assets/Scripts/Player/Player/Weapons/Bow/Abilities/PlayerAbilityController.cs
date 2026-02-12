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
        public PlayerAbilitySO ability;
    }

    [Header("Ability Slots")]
    [SerializeField] private AbilitySlot _ability1;
    [SerializeField] private AbilitySlot _ability2;

    public PlayerAbilitySO Ability1 => _ability1.ability;
    public PlayerAbilitySO Ability2 => _ability2.ability;
    public PlayerAbilityContext AbilityContext => _context;

    PlayerAbilityContext _context;

    private void Awake()
    {
        _context = new PlayerAbilityContext
        {
            controller = this,
            input = _input,
            stats = _stats,
            bow = _bow
        };

        if (_ability1.ability != null)
            _ability1.ability.ResetCooldown();

        if (_ability2.ability != null)
            _ability2.ability.ResetCooldown();
        
        /*
        // Create a clone of the SO so we don't modify the file on disk  <=========
        if (_ability1.ability != null)
            _ability1.ability = Instantiate(_ability1.ability);

        if (_ability2.ability != null)
            _ability2.ability = Instantiate(_ability2.ability);

        // Now it's safe to reset and tick
        _ability1.ability?.ResetCooldown();
        _ability2.ability?.ResetCooldown();
        */
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
            Debug.LogError($"<b><color=red>[PlayerAbilityController]</color></b> is missing PlayerInputReaderSO on GameObject: <b>{name}<b>", this);
        }
    }
    private void Update()
    {
        if (_ability1.ability != null)
            _ability1.ability.Tick(_context);

        if (_ability2.ability != null)
            _ability2.ability.Tick(_context);
    }

    void OnAbility1Pressed()
    {
        _ability1.ability?.OnButtonDown(_context);
    }

    private void OnAbility2Pressed()
    {
        _ability2.ability?.OnButtonDown(_context);
    }

    private void OnAbility2Released()
    {
        _ability2.ability?.OnButtonUp(_context);
    }
}
