using UnityEngine;

public sealed class StaminaChargingSquareEffect : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerStats playerStats;

    [Header("Effect")]
    [SerializeField] private string effectId = "stamina_square_test";
    [SerializeField] private Vector3 localOffset = new Vector3(0f, 1.5f, 0f);

    private EffectHandle activeHandle = EffectHandle.Invalid;

    private void Reset()
    {
        playerStats = GetComponent<PlayerStats>();
    }

    private void Awake()
    {
        if (playerStats == null)
        {
            playerStats = GetComponent<PlayerStats>();
        }

        if (playerStats == null)
        {
            Debug.LogWarning($"{nameof(StaminaChargingSquareEffect)}: Missing PlayerStats reference.", this);
        }
    }

    private void OnEnable()
    {
        if (playerStats == null)
            return;

        playerStats.OnStaminaChanged += OnStaminaChanged;

        // Sync once on enable (in case stamina already below max when scene starts)
        OnStaminaChanged(playerStats.currentStamina, playerStats.maxStamina);
    }

    private void OnDisable()
    {
        if (playerStats != null)
        {
            playerStats.OnStaminaChanged -= OnStaminaChanged;
        }

        ReleaseIfActive();
    }

    private void OnStaminaChanged(float currentStamina, float maxStamina)
    {
        bool isCharging = currentStamina < maxStamina;

        if (isCharging)
        {
            EnsureSpawned();
        }
        else
        {
            ReleaseIfActive();
        }
    }

    private void EnsureSpawned()
    {
        if (activeHandle.IsValid)
            return;

        activeHandle = EffectManagerBehavior.Instance.PlayPersistent(new PersistentEffectRequest
        {
            EffectId = effectId,
            Anchor = transform,
            LocalPosition = localOffset,
            LocalRotation = Quaternion.identity,
            Variant = default,
            Magnitude = 1f
        });
    }

    private void ReleaseIfActive()
    {
        if (!activeHandle.IsValid)
            return;

        EffectManagerBehavior.Instance.Release(activeHandle);
        activeHandle = EffectHandle.Invalid;
    }
}
