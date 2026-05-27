using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerEffectDefinition", menuName = "Outland Haven/Player/Status/Effect Definition")]
public class PlayerEffectDefinitionSO : ScriptableObject
{
    [SerializeField] private List<PlayerEffectModifier> _modifiers = new();

    public IReadOnlyList<PlayerEffectModifier> Modifiers => _modifiers;
}
