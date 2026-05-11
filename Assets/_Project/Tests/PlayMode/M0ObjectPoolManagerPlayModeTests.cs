using System.Collections;
using HorrorGame.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HorrorGame.Tests.PlayMode
{
    public sealed class M0ObjectPoolManagerPlayModeTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
        }

        [UnityTest]
        public IEnumerator ObjectPoolManager_ReusesReturnedInstanceAndRunsCallbacks()
        {
            GameObject managerObject = null;
            GameObject prefab = null;

            try
            {
                managerObject = new GameObject("Object Pool Manager");
                var manager = managerObject.AddComponent<ObjectPoolManager>();
                prefab = new GameObject("Poolable Prefab");
                prefab.AddComponent<PoolCallbackProbe>();

                Assert.IsTrue(ServiceLocator.TryGet(out IObjectPoolManager registeredManager));
                Assert.AreSame(manager, registeredManager);

                GameObject firstInstance = manager.Rent(prefab);
                var firstProbe = firstInstance.GetComponent<PoolCallbackProbe>();

                Assert.IsTrue(firstInstance.activeSelf);
                Assert.AreEqual(1, firstProbe.RentCount);

                manager.Return(firstInstance);

                Assert.IsFalse(firstInstance.activeSelf);
                Assert.AreEqual(1, firstProbe.ReturnCount);

                GameObject secondInstance = manager.Rent(prefab);

                Assert.AreSame(firstInstance, secondInstance);
                Assert.AreEqual(2, firstProbe.RentCount);

                manager.Return(secondInstance);
            }
            finally
            {
                if (prefab != null)
                {
                    Object.Destroy(prefab);
                }

                if (managerObject != null)
                {
                    Object.Destroy(managerObject);
                }
            }

            yield return null;
        }

        private sealed class PoolCallbackProbe : MonoBehaviour, IPoolable
        {
            public int RentCount { get; private set; }

            public int ReturnCount { get; private set; }

            public void OnRentFromPool()
            {
                RentCount++;
            }

            public void OnReturnToPool()
            {
                ReturnCount++;
            }
        }
    }
}
