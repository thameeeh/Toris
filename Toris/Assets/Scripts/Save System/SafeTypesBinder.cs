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
            // Let Json.NET use its default behavior for serialization output.
            assemblyName = null;
            typeName = null;
        }
    }
}
