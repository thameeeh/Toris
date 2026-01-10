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

    [CreateAssetMenu(menuName = "UI/Scriptable Objects/GameSessionSO")]
    public class GameSessionSO : ScriptableObject
    {
        [Header("Data References")]
        public PlayerDataSO PlayerData;
        public InventoryContainerSO PlayerInventory;

        [Header("Save State")]
        [SerializeField] private int CurrentSaveSlotIndex;
        [SerializeField] private string targetSpawnPointID;
    }
}