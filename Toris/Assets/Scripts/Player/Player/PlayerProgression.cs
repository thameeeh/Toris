using System;
using UnityEngine;

// PURPOSE:
// - PlayerProgressionConfigSO = authored defaults and level curve basics
// - PlayerRuntimeProgression = mutable runtime XP / level / gold
// - PlayerProgression = gameplay-facing owner and event source for progression

public class PlayerProgression : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerProgressionConfigSO _config;

    [Header("Runtime")]
    [SerializeField] private bool _initializeFromConfigOnAwake = true;

    private PlayerRuntimeProgression _runtimeProgression;

    public event Action<int, float> OnLevelChanged;
    public event Action<float, int> OnExperienceChanged;
    public event Action<int, int> OnGoldChanged;

    public PlayerRuntimeProgression RuntimeProgression => _runtimeProgression;

    public int CurrentLevel => _runtimeProgression != null ? _runtimeProgression.CurrentLevel : GetStartingLevel();
    public float CurrentExperience => _runtimeProgression != null ? _runtimeProgression.CurrentExperience : GetStartingExperience();
    public int CurrentGold => _runtimeProgression != null ? _runtimeProgression.CurrentGold : GetStartingGold();

    public int ExperiencePerLevel => _config != null ? Mathf.Max(1, _config.experiencePerLevel) : 100;

    private void Awake()
    {
        _runtimeProgression = new PlayerRuntimeProgression();

        if (_initializeFromConfigOnAwake)
        {
            _runtimeProgression.Initialize(
                GetStartingLevel(),
                GetStartingExperience(),
                GetStartingGold());
        }
    }

    private void OnValidate()
    {
        if (_config == null)
        {
            Debug.LogWarning($"[PlayerProgression] Missing PlayerProgressionConfigSO on {name}. Fallback defaults will be used.", this);
        }
    }

    public void AddExperience(int amount)
    {
        if (_runtimeProgression == null)
            return;

        float validatedAmount = Mathf.Max(0, amount);
        if (validatedAmount <= 0f)
            return;

        int previousLevel = _runtimeProgression.CurrentLevel;

        _runtimeProgression.AddExperience(validatedAmount);
        RecalculateLevelFromExperience();

        OnExperienceChanged?.Invoke(_runtimeProgression.CurrentExperience, _runtimeProgression.CurrentLevel);

        if (_runtimeProgression.CurrentLevel != previousLevel)
        {
            OnLevelChanged?.Invoke(_runtimeProgression.CurrentLevel, _runtimeProgression.CurrentExperience);
        }
    }

    public void AddGold(int amount)
    {
        if (_runtimeProgression == null)
            return;

        int validatedAmount = Mathf.Max(0, amount);
        if (validatedAmount <= 0)
            return;

        _runtimeProgression.AddGold(validatedAmount);
        OnGoldChanged?.Invoke(_runtimeProgression.CurrentGold, validatedAmount);

        Debug.Log($"[PlayerProgression] Gold is now {CurrentGold}", this);
    }

    public bool TrySpendGold(int amount)
    {
        if (_runtimeProgression == null)
            return false;

        int validatedAmount = Mathf.Max(0, amount);
        if (validatedAmount <= 0)
            return true;

        bool spent = _runtimeProgression.TrySpendGold(validatedAmount);
        if (spent)
        {
            OnGoldChanged?.Invoke(_runtimeProgression.CurrentGold, -validatedAmount);
        }

        return spent;
    }

    public void SetGold(int value)
    {
        if (_runtimeProgression == null)
            return;

        int previousGold = _runtimeProgression.CurrentGold;
        _runtimeProgression.SetGold(value);

        int delta = _runtimeProgression.CurrentGold - previousGold;
        OnGoldChanged?.Invoke(_runtimeProgression.CurrentGold, delta);
    }

    public float GetExperienceIntoCurrentLevel()
    {
        float levelFloor = (CurrentLevel - 1) * ExperiencePerLevel;
        return Mathf.Max(0f, CurrentExperience - levelFloor);
    }

    public float GetExperienceNeededForNextLevel()
    {
        return ExperiencePerLevel;
    }

    public float GetExperienceProgressNormalized()
    {
        float experienceNeeded = GetExperienceNeededForNextLevel();
        if (experienceNeeded <= 0f)
            return 0f;

        return Mathf.Clamp01(GetExperienceIntoCurrentLevel() / experienceNeeded);
    }

    private void RecalculateLevelFromExperience()
    {
        int recalculatedLevel = Mathf.FloorToInt(CurrentExperience / ExperiencePerLevel) + 1;
        _runtimeProgression.SetLevel(recalculatedLevel);
    }

    private int GetStartingLevel()
    {
        return _config != null ? Mathf.Max(1, _config.startingLevel) : 1;
    }

    private float GetStartingExperience()
    {
        return _config != null ? Mathf.Max(0f, _config.startingExperience) : 0f;
    }

    private int GetStartingGold()
    {
        return _config != null ? Mathf.Max(0, _config.startingGold) : 0;
    }
}