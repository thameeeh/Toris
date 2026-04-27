using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reusable authoring asset for mapping Toris gameplay facts to Pixel Crushers quest progress.
/// Add rules here so scenes only reference a rule set instead of owning quest-specific data.
/// Create one per scene, questline, or content pack depending on what is easiest to author.
/// </summary>
[CreateAssetMenu(
    fileName = "QuestFactProgressRuleSet",
    menuName = "Quest/Pixel Crushers/Quest Fact Progress Rule Set")]
public class QuestFactProgressRuleSetSO : ScriptableObject
{
    [Tooltip("Each rule says: when this Toris fact happens, increment this Pixel Crushers quest variable and complete the configured entry when the threshold is reached.")]
    [SerializeField] private QuestFactProgressRule[] _rules = Array.Empty<QuestFactProgressRule>();

    public int RuleCount => _rules == null ? 0 : _rules.Length;

    public void AppendRulesTo(List<QuestFactProgressRule> target)
    {
        if (target == null || _rules == null)
            return;

        for (int i = 0; i < _rules.Length; i++)
        {
            if (_rules[i] != null)
                target.Add(_rules[i]);
        }
    }
}
