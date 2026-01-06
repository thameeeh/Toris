using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    public enum SaveSlotIndex
    {
        Slot1 = 0,
        Slot2 = 1,
        Slot3 = 2
    }

    public enum PlayerClass
    {
        Archer,
        Warrior, 
        Mage
    }

    [CreateAssetMenu(fileName = "GameSessionSO", menuName = "Scriptable Objects/GameSessionSO")]
    public class GameSessionSO : ScriptableObject
    {
        [SerializeField] private int CurrentSaveSlotIndex;
        [SerializeField] private string targetSpawnPointID;
    }
}

// none of it will be in the final game :D