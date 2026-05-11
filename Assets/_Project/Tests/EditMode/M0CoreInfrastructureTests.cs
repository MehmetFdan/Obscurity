using System;
using HorrorGame.Core;
using NUnit.Framework;
using UnityEngine;

namespace HorrorGame.Tests.EditMode
{
    public sealed class M0CoreInfrastructureTests
    {
        [SetUp]
        public void SetUp()
        {
            ServiceLocator.Clear();
            EventBus.ClearAll();
        }

        [TearDown]
        public void TearDown()
        {
            ServiceLocator.Clear();
            EventBus.ClearAll();
        }

        [Test]
        public void ServiceLocator_RegistersAndResolvesByContract()
        {
            var service = new FakeService();

            ServiceLocator.Register<IFakeService>(service);

            Assert.IsTrue(ServiceLocator.IsRegistered<IFakeService>());
            Assert.IsTrue(ServiceLocator.TryGet(out IFakeService resolvedService));
            Assert.AreSame(service, resolvedService);
            Assert.AreSame(service, ServiceLocator.Get<IFakeService>());
        }

        [Test]
        public void ServiceLocator_DuplicateRegistrationRequiresOverwrite()
        {
            var firstService = new FakeService();
            var secondService = new FakeService();

            ServiceLocator.Register<IFakeService>(firstService);

            Assert.Throws<InvalidOperationException>(
                () => ServiceLocator.Register<IFakeService>(secondService));

            ServiceLocator.Register<IFakeService>(secondService, overwrite: true);

            Assert.AreSame(secondService, ServiceLocator.Get<IFakeService>());
        }

        [Test]
        public void EventBus_PublishesStructEventsAndUnsubscribes()
        {
            int receivedValue = 0;

            void OnEvent(TestEvent eventData)
            {
                receivedValue += eventData.Value;
            }

            EventBus<TestEvent>.Subscribe(OnEvent);
            EventBus<TestEvent>.Publish(new TestEvent(7));
            EventBus<TestEvent>.Unsubscribe(OnEvent);
            EventBus<TestEvent>.Publish(new TestEvent(7));

            Assert.AreEqual(7, receivedValue);
            Assert.AreEqual(0, EventBus<TestEvent>.ListenerCount);
        }

        [Test]
        public void AddressableSceneLoader_RejectsEmptySceneAddress()
        {
            var loaderObject = new GameObject("Addressable Scene Loader");

            try
            {
                var loader = loaderObject.AddComponent<AddressableSceneLoader>();

                Assert.Throws<ArgumentException>(
                    () => loader.LoadSceneAsync(string.Empty));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(loaderObject);
            }
        }

        private interface IFakeService : IService
        {
        }

        private sealed class FakeService : IFakeService
        {
        }

        private readonly struct TestEvent
        {
            public TestEvent(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }
    }
}
