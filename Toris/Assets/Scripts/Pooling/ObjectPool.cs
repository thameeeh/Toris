using System;
using System.Collections.Generic;
using UnityEngine;

namespace Pooling
{
    public interface IPoolable
    {
        void OnAfterSpawn();
        void OnBeforeReturn();
    }

    public readonly struct SpawnParameters
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Transform Parent { get; }

        public SpawnParameters(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            Position = position;
            Rotation = rotation;
            Parent = parent;
        }

        public SpawnParameters WithParent(Transform parent)
        {
            return new SpawnParameters(Position, Rotation, parent);
        }

        public static SpawnParameters Default => new SpawnParameters(Vector3.zero, Quaternion.identity);
    }

    public class ObjectPool<T> where T : Component
    {
        private readonly Stack<T> _pool = new Stack<T>();
        private readonly T _prefab;
        private readonly Transform _poolRoot;
        private readonly Func<T> _factory;

        public event Action<T> Spawned;
        public event Action<T> Despawned;

        public ObjectPool(T prefab, int prewarmCount = 0, Transform poolRoot = null, Func<T> factory = null)
        {
            _prefab = prefab;
            _poolRoot = poolRoot;
            _factory = factory;

            if (prewarmCount > 0)
            {
                Prewarm(prewarmCount);
            }
        }

        public T Spawn() => Spawn(SpawnParameters.Default);

        public T Spawn(Vector3 position, Quaternion rotation, Transform parent = null)
        {
            return Spawn(new SpawnParameters(position, rotation, parent));
        }

        public T Spawn(SpawnParameters spawnParameters)
        {
            var instance = _pool.Count > 0 ? _pool.Pop() : CreateInstance();

            ApplySpawnParameters(instance.transform, spawnParameters);
            instance.gameObject.SetActive(true);

            if (instance is IPoolable poolable)
            {
                poolable.OnAfterSpawn();
            }

            Spawned?.Invoke(instance);
            return instance;
        }

        public void Return(T instance)
        {
            if (instance == null)
            {
                return;
            }

            if (instance is IPoolable poolable)
            {
                poolable.OnBeforeReturn();
            }

            Despawned?.Invoke(instance);

            instance.gameObject.SetActive(false);
            if (_poolRoot)
            {
                instance.transform.SetParent(_poolRoot, false);
            }

            _pool.Push(instance);
        }

        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var instance = CreateInstance();
                instance.gameObject.SetActive(false);
                _pool.Push(instance);
            }
        }

        private T CreateInstance()
        {
            var instance = _factory != null ? _factory() : UnityEngine.Object.Instantiate(_prefab);
            if (_poolRoot)
            {
                instance.transform.SetParent(_poolRoot, false);
            }

            return instance;
        }

        private static void ApplySpawnParameters(Transform transform, SpawnParameters spawnParameters)
        {
            transform.SetParent(spawnParameters.Parent, false);
            transform.SetPositionAndRotation(spawnParameters.Position, spawnParameters.Rotation);
        }
    }
}