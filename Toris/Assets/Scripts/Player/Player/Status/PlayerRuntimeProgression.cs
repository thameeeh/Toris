using System;
using UnityEngine;

[Serializable]
public class PlayerRuntimeProgression
{
    [SerializeField] private int _currentLevel;
    [SerializeField] private float _currentExperience;
    [SerializeField] private int _currentGold;

    public int CurrentLevel => _currentLevel;
    public float CurrentExperience => _currentExperience;
    public int CurrentGold => _currentGold;

    public void Initialize(int startingLevel, float startingExperience, int startingGold)
    {
        _currentLevel = Mathf.Max(1, startingLevel);
        _currentExperience = Mathf.Max(0f, startingExperience);
        _currentGold = Mathf.Max(0, startingGold);
    }

    public void AddExperience(float amount)
    {
        _currentExperience += Mathf.Max(0f, amount);
    }

    public void SetLevel(int value)
    {
        _currentLevel = Mathf.Max(1, value);
    }

    public void SetExperience(float value)
    {
        _currentExperience = Mathf.Max(0f, value);
    }

    public void AddGold(int amount)
    {
        _currentGold = Mathf.Max(0, _currentGold + amount);
    }

    public bool TrySpendGold(int amount)
    {
        int validatedAmount = Mathf.Max(0, amount);

        if (_currentGold < validatedAmount)
            return false;

        _currentGold -= validatedAmount;
        return true;
    }

    public void SetGold(int value)
    {
        _currentGold = Mathf.Max(0, value);
    }
}
