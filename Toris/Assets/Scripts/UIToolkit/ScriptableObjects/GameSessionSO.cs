using UnityEngine;
using OutlandHaven.Inventory;
using OutlandHaven.Player.Equipment;

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
        [System.NonSerialized]
        public InventoryManager PlayerInventory;
        [System.NonSerialized]
        public EquipmentManager PlayerEquipment;

        [Header("Save State")]
        [SerializeField] private int CurrentSaveSlotIndex;
        [SerializeField] private string targetSpawnPointID;
    }
}