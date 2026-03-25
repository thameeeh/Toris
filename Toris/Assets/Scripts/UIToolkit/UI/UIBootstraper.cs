using UnityEngine;

namespace OutlandHaven.Core
{
    public class SystemBootstrapper : MonoBehaviour
    {
        [Header("Global System Managers")]
        [Tooltip("Drop your ScriptableObject managers here to force them to load and listen to events.")]
        [SerializeField] private ScriptableObject[] _persistentManagers;
    }
}