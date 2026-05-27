using System;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using OutlandHaven.Inventory;

namespace OutlandHaven.SaveSystem
{
    /// <summary>
    /// A whitelist-based serialization binder that restricts which types can be
    /// instantiated during JSON deserialization. This prevents Remote Code
    /// Execution (RCE) attacks via crafted save files (CWE-502).
    ///
    /// When adding a new ItemComponentState subclass, register it in the
    /// static constructor below or deserialization of old saves will fail
    /// with a clear error message.
    /// </summary>
    public sealed class SafeTypesBinder : ISerializationBinder
    {
        private readonly Dictionary<string, Type> _allowedTypes = new Dictionary<string, Type>();

        public static SafeTypesBinder Instance { get; } = new SafeTypesBinder();

        private SafeTypesBinder()
        {
            // --- Save-pipeline DTO types ---
            // Old save files (written with TypeNameHandling.All) embed $type on
            // every object. New saves (Auto) won't, but we must accept both
            // for backward compatibility.
            Allow<GameSaveData>();
            Allow<SaveMetadata>();
            Allow<SavedInventoryData>();
            Allow<SavedSlotData>();
            Allow<SavedItemData>();
            Allow<SavedSkillProgressData>();
            Allow<SavedTutorialProgressData>();
            Allow<SavedGameplayStatisticsData>();

            // --- ItemComponentState subtypes (the only polymorphic types in saves) ---
            Allow<EvolvingState>();
            Allow<UpgradeableState>();

            // --- Standard .NET collection types that Json.NET may embed $type for ---
            Allow<List<ItemComponentState>>();
            Allow<List<SavedSlotData>>();
            Allow<List<string>>();
            Allow<Dictionary<string, int>>();
        }

        /// <summary>
        /// Registers a type as safe for deserialization, keyed by its
        /// assembly-qualified name (the value Json.NET writes into $type).
        /// </summary>
        private void Allow<T>()
        {
            Type type = typeof(T);
            string key = $"{type.FullName}, {type.Assembly.GetName().Name}";
            _allowedTypes[key] = type;
        }

        public Type BindToType(string assemblyName, string typeName)
        {
            // Guard: empty $type values from edge-case serialization or
            // legacy saves. Return null to let Json.NET fall back to its
            // default type resolution (uses the declared member type).
            if (string.IsNullOrEmpty(typeName))
                return null;

            // Json.NET may pass the full key or split assemblyName/typeName.
            // Build the same key format we used during registration.
            string key = string.IsNullOrEmpty(assemblyName)
                ? typeName
                : $"{typeName}, {assemblyName}";

            if (_allowedTypes.TryGetValue(key, out Type resolvedType))
            {
                return resolvedType;
            }

            throw new InvalidOperationException(
                $"[SaveSystem] Blocked deserialization of untrusted type: '{key}'. " +
                $"If this is a new ItemComponentState subtype, register it in SafeTypesBinder.");
        }

        public void BindToName(Type serializedType, out string assemblyName, out string typeName)
        {
            // Explicitly return the correct type info for whitelisted types.
            // Unity's bundled Newtonsoft.Json fork does not correctly handle
            // the null/null fallback convention — it writes empty "$type"
            // strings into the JSON, which then fail on deserialization.
            string key = $"{serializedType.FullName}, {serializedType.Assembly.GetName().Name}";
            if (_allowedTypes.ContainsKey(key))
            {
                assemblyName = serializedType.Assembly.GetName().Name;
                typeName = serializedType.FullName;
                return;
            }

            // Unknown types: let Json.NET use its default behavior.
            assemblyName = null;
            typeName = null;
        }
    }
}
