using System.Collections.Generic;
using System.ComponentModel; // Required for INotifyPropertyChanged
using System.Runtime.CompilerServices; // Required for "CallerMemberName"
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
    public class PlayerDataSO : ScriptableObject, INotifyPropertyChanged
    {

        [SerializeField] private string m_playerName;
        [SerializeField] private int m_health;

        public event PropertyChangedEventHandler PropertyChanged;

        [SerializeField]
        private List<StatEntry> m_stats = new List<StatEntry>();

        public List<StatEntry> Stats => m_stats;

        [CreateProperty] public string NameDisplay => m_playerName;
        [CreateProperty] public string HealthDisplay => $"HP: {m_health}";


        [CreateProperty]
        public int Health
        {
            get => m_health;
            set
            {
                if (m_health != value)
                {
                    m_health = value;
                    OnPropertyChanged();

                    OnPropertyChanged(nameof(HealthDisplay));
                }
            }
        }
        private void OnEnable()
        {
            if (m_stats.Count == 0)
            {
                m_stats.Add(new StatEntry("Strength", 15));
                m_stats.Add(new StatEntry("Agility", 12));
                m_stats.Add(new StatEntry("Intellect", 8));
            }
        }

        // The Helper Method (Boilerplate)
        // This allows to just call OnPropertyChanged() without typing the name string
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null) 
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void ResetToDefaults()
        {
            m_playerName = "Hero";
            Health = 100;
            // Reset other player data as needed
        }

        public void AddHealth(int amount) 
        {
            Health = Mathf.Clamp(Health + amount, 0, 100000); // Assuming max health is 1000...
        }
    }
}