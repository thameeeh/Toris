using Unity.Properties;
using UnityEngine;

namespace OutlandHaven.UIToolkit
{

    [System.Serializable]
    public class StatEntry
    {
        [SerializeField] private string m_statName;
        [SerializeField] private int m_value;

        [CreateProperty] public string StatName => m_statName;
        [CreateProperty] public string ValueDisplay => m_value.ToString();
        public StatEntry(string name, int value)
        {
            m_statName = name;
            m_value = value;
        }
    }
}