using UnityEngine;

namespace HorrorGame.Core
{
    /// <summary>
    /// Prefab-oriented GameObject pool service.
    /// </summary>
    public interface IObjectPoolManager : IService
    {
        int PoolCount { get; }

        GameObject Rent(GameObject prefab, Transform parent = null);

        void Return(GameObject instance);

        void Return(GameObject prefab, GameObject instance);

        void Prewarm(GameObject prefab, int count, Transform parent = null);

        bool HasPool(GameObject prefab);
    }
}
