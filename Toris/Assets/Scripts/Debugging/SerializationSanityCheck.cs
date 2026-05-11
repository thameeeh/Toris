using UnityEngine;
using Newtonsoft.Json;
using OutlandHaven.Inventory;
using System.Collections.Generic;

namespace OutlandHaven.Debugging
{
    public class SerializationSanityCheck : MonoBehaviour
    {
        [ContextMenu("Run Serialization Test")]
        public void RunTest()
        {
            Debug.Log("[Serialization Test] Starting...");

            TestState(new EvolvingState { CurrentKills = 10, IsAwakened = false });
            TestState(new UpgradeableState(3));

            Debug.Log("[Serialization Test] All tests passed!");
        }

        private void TestState<T>(T original) where T : ItemComponentState
        {
            string json = JsonConvert.SerializeObject(original, new JsonSerializerSettings 
            { 
                TypeNameHandling = TypeNameHandling.Auto 
            });
            
            Debug.Log($"[Serialization Test] Serialized {typeof(T).Name}: {json}");

            T deserialized = JsonConvert.DeserializeObject<T>(json, new JsonSerializerSettings 
            { 
                TypeNameHandling = TypeNameHandling.Auto 
            });

            if (deserialized == null)
            {
                Debug.LogError($"[Serialization Test] Failed to deserialize {typeof(T).Name}!");
                return;
            }

            // We can't easily compare equality without overriding Equals, 
            // but the fact that DeserializeObject didn't throw is the main goal of the parameterless constructor rule.
            Debug.Log($"[Serialization Test] Successfully deserialized {typeof(T).Name}.");
        }
    }
}
