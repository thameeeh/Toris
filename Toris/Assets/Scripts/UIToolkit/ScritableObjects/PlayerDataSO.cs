using Unity.Properties;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{
    [System.Serializable]
    public enum ItemType
    {
        Weapon,
        Armor,
        Consumable,
        QuestItem,
        Miscellaneous
    }

    [CreateAssetMenu(menuName = "Skeleton/PlayerData")]
    public class PlayerDataSO : ScriptableObject
    {

        [SerializeField] private string playerName;
        [SerializeField] private int health;

        [CreateProperty] public string NameDisplay => playerName;
        [CreateProperty] public string HealthDisplay => $"HP: {health}";

        public void AddHealth(int amount) 
        {
            health += amount;
            health = Mathf.Clamp(health, 0, 100000); // Assuming max health is 100

            PlayerEvents.OnDataChanged?.Invoke(this);
        }
    }
}