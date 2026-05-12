using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

namespace HorrorGame.Core
{
    /// <summary>
    /// ServiceLocator-registered GameObject pool built on UnityEngine.Pool.
    /// </summary>
    [DefaultExecutionOrder(ServiceExecutionOrder.CoreServices)]
    [DisallowMultipleComponent]
    public sealed class ObjectPoolManager : MonoBehaviour, IObjectPoolManager
    {
        [SerializeField, Min(1)] private int defaultCapacity = 16;
        [SerializeField, Min(1)] private int maxSize = 128;
        [SerializeField] private bool collectionChecks = true;
        [SerializeField] private bool registerOnAwake = true;

        private readonly Dictionary<int, PoolEntry> pools = new();
        private readonly Dictionary<GameObject, int> activeInstances = new();

        public int PoolCount => pools.Count;

        private void Awake()
        {
            if (registerOnAwake)
            {
                ServiceLocator.Register<IObjectPoolManager>(this);
            }
        }

        public GameObject Rent(GameObject prefab, Transform parent = null)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            int prefabKey = prefab.GetInstanceID();
            PoolEntry entry = GetOrCreatePool(prefab, prefabKey, parent);
            GameObject instance = entry.Pool.Get();

            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            activeInstances[instance] = prefabKey;
            return instance;
        }

        public void Return(GameObject instance)
        {
            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            if (!activeInstances.TryGetValue(instance, out int prefabKey))
            {
                throw new InvalidOperationException("Instance was not rented from this pool manager.");
            }

            activeInstances.Remove(instance);
            pools[prefabKey].Pool.Release(instance);
        }

        public void Return(GameObject prefab, GameObject instance)
        {
            if (prefab == null)
            {
                throw new ArgumentNullException(nameof(prefab));
            }

            if (instance == null)
            {
                throw new ArgumentNullException(nameof(instance));
            }

            int prefabKey = prefab.GetInstanceID();

            if (!activeInstances.TryGetValue(instance, out int rentedPrefabKey))
            {
                throw new InvalidOperationException("Instance was not rented from this pool manager.");
            }

            if (rentedPrefabKey != prefabKey)
            {
                throw new InvalidOperationException("Instance belongs to a different prefab pool.");
            }

            activeInstances.Remove(instance);
            pools[prefabKey].Pool.Release(instance);
        }

        public void Prewarm(GameObject prefab, int count, Transform parent = null)
        {
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), "Prewarm count cannot be negative.");
            }

            var instances = new List<GameObject>(count);

            for (int i = 0; i < count; i++)
            {
                instances.Add(Rent(prefab, parent));
            }

            for (int i = 0; i < instances.Count; i++)
            {
                Return(instances[i]);
            }
        }

        public bool HasPool(GameObject prefab)
        {
            if (prefab == null)
            {
                return false;
            }

            return pools.ContainsKey(prefab.GetInstanceID());
        }

        private PoolEntry GetOrCreatePool(GameObject prefab, int prefabKey, Transform parent)
        {
            if (pools.TryGetValue(prefabKey, out PoolEntry entry))
            {
                return entry;
            }

            Transform poolRoot = CreatePoolRoot(prefab.name, parent);
            int safeMaxSize = Mathf.Max(maxSize, defaultCapacity);
            var pool = new ObjectPool<GameObject>(
                createFunc: () => CreateInstance(prefab, poolRoot),
                actionOnGet: OnGet,
                actionOnRelease: instance => OnRelease(instance, poolRoot),
                actionOnDestroy: DestroyInstance,
                collectionCheck: collectionChecks,
                defaultCapacity: defaultCapacity,
                maxSize: safeMaxSize);

            entry = new PoolEntry(pool);
            pools.Add(prefabKey, entry);
            return entry;
        }

        private Transform CreatePoolRoot(string prefabName, Transform parent)
        {
            var poolRoot = new GameObject($"{prefabName} Pool").transform;
            poolRoot.SetParent(parent != null ? parent : transform, false);
            return poolRoot;
        }

        private static GameObject CreateInstance(GameObject prefab, Transform parent)
        {
            GameObject instance = Instantiate(prefab, parent);
            instance.SetActive(false);
            return instance;
        }

        private static void OnGet(GameObject instance)
        {
            instance.SetActive(true);
            NotifyRent(instance);
        }

        private static void OnRelease(GameObject instance, Transform poolRoot)
        {
            NotifyReturn(instance);
            instance.transform.SetParent(poolRoot, false);
            instance.SetActive(false);
        }

        private static void DestroyInstance(GameObject instance)
        {
            if (instance != null)
            {
                Destroy(instance);
            }
        }

        private static void NotifyRent(GameObject instance)
        {
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPoolable poolable)
                {
                    poolable.OnRentFromPool();
                }
            }
        }

        private static void NotifyReturn(GameObject instance)
        {
            MonoBehaviour[] behaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);

            for (int i = 0; i < behaviours.Length; i++)
            {
                if (behaviours[i] is IPoolable poolable)
                {
                    poolable.OnReturnToPool();
                }
            }
        }

        private void OnValidate()
        {
            maxSize = Mathf.Max(maxSize, defaultCapacity);
        }

        private void OnDestroy()
        {
            if (ServiceLocator.TryGet(out IObjectPoolManager service) && ReferenceEquals(service, this))
            {
                ServiceLocator.Unregister<IObjectPoolManager>(this);
            }
        }

        private sealed class PoolEntry
        {
            public PoolEntry(IObjectPool<GameObject> pool)
            {
                Pool = pool;
            }

            public IObjectPool<GameObject> Pool { get; }
        }
    }
}
