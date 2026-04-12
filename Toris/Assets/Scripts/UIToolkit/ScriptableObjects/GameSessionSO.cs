using UnityEngine;
using OutlandHaven.Inventory;

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

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/GameSessionSO")]
    public class GameSessionSO : ScriptableObject
    {
        [Header("Data References")]
        // public PlayerDataSO PlayerData; // Deprecated
        [System.NonSerialized]
        public InventoryManager PlayerInventory;

        [Header("Save State")]
        [SerializeField] private int CurrentSaveSlotIndex;
        [SerializeField] private string targetSpawnPointID;

        [Header("Skill System")]
        [SerializeField] private PlayerSkillTracker _playerSkills = new PlayerSkillTracker();

        public PlayerSkillTracker PlayerSkills => _playerSkills;
    }
}