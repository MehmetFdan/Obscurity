using System;
using HorrorGame.Audio;
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
        public void ServiceLocator_GetMissingServiceThrowsActionableException()
        {
            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ServiceLocator.Get<IFakeService>());

            Assert.That(exception.Message, Does.Contain(nameof(IFakeService)));
            Assert.That(exception.Message, Does.Contain("Awake()"));
            Assert.That(exception.Message, Does.Contain("Start()"));
        }

        [Test]
        public void ServiceLocator_TryGetMissingServiceReturnsFalseWithoutThrowing()
        {
            bool found = ServiceLocator.TryGet(out IFakeService service);

            Assert.IsFalse(found);
            Assert.IsNull(service);
        }

        [Test]
        public void ServiceLocator_UnregisterRemovesOnlyMatchingInstance()
        {
            var registeredService = new FakeService();
            var otherService = new FakeService();
            ServiceLocator.Register<IFakeService>(registeredService);

            Assert.IsFalse(ServiceLocator.Unregister<IFakeService>(otherService));
            Assert.IsTrue(ServiceLocator.IsRegistered<IFakeService>());

            Assert.IsTrue(ServiceLocator.Unregister<IFakeService>(registeredService));
            Assert.IsFalse(ServiceLocator.IsRegistered<IFakeService>());
        }

        [Test]
        public void ServiceLocator_ClearRemovesAllRegistrations()
        {
            ServiceLocator.Register<IFakeService>(new FakeService());

            ServiceLocator.Clear();

            Assert.IsFalse(ServiceLocator.IsRegistered<IFakeService>());
            Assert.IsFalse(ServiceLocator.TryGet(out IFakeService service));
            Assert.IsNull(service);
        }

        [Test]
        public void ServiceLocator_RejectsConcreteServiceContracts()
        {
            var service = new ConcreteService();

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                () => ServiceLocator.Register<ConcreteService>(service));

            Assert.That(exception.Message, Does.Contain("must be an interface"));
        }

        [Test]
        public void ServiceLocator_AllowsMockServiceRegistrationForTests()
        {
            var mockAudioManager = new MockAudioManager();
            ServiceLocator.Register<IAudioManager>(mockAudioManager);

            ServiceLocator.Get<IAudioManager>().PlaySFX("door_creak", Vector3.zero);

            Assert.AreEqual(1, mockAudioManager.OneShotRequests.Count);
            Assert.AreEqual("door_creak", mockAudioManager.OneShotRequests[0].SoundId);
        }

        [Test]
        public void EventBus_PublishesStructEventsAndUnsubscribes()
        {
            int receivedValue = 0;

            void OnEvent(TestEvent eventData)
            {
                receivedValue += eventData.Value;
            }

            Assert.IsFalse(EventBus<TestEvent>.HasListeners());

            EventBus<TestEvent>.Subscribe(OnEvent);
            Assert.IsTrue(EventBus<TestEvent>.HasListeners());

            EventBus<TestEvent>.Publish(new TestEvent(7));
            EventBus<TestEvent>.Unsubscribe(OnEvent);
            EventBus<TestEvent>.Publish(new TestEvent(7));

            Assert.AreEqual(7, receivedValue);
            Assert.AreEqual(0, EventBus<TestEvent>.ListenerCount);
            Assert.IsFalse(EventBus<TestEvent>.HasListeners());
        }

        [Test]
        public void EventBus_RejectsSubscriptionsAboveChannelLimit()
        {
            int previousLimit = EventBus<LimitedEvent>.ListenerLimit;

            void First(LimitedEvent _)
            {
            }

            void Second(LimitedEvent _)
            {
            }

            try
            {
                EventBus<LimitedEvent>.Clear();
                EventBus<LimitedEvent>.ListenerLimit = 1;
                EventBus<LimitedEvent>.Subscribe(First);

                InvalidOperationException exception = Assert.Throws<InvalidOperationException>(
                    () => EventBus<LimitedEvent>.Subscribe(Second));

                Assert.That(exception.Message, Does.Contain(nameof(LimitedEvent)));
                Assert.That(exception.Message, Does.Contain("listener limit"));
                Assert.AreEqual(1, EventBus<LimitedEvent>.ListenerCount);
            }
            finally
            {
                EventBus<LimitedEvent>.Clear();
                EventBus<LimitedEvent>.ListenerLimit = previousLimit;
            }
        }

        [Test]
        public void EventBus_RequiredM0EventsAreStructChannels()
        {
            AssertEventChannel<PlayerSanityChangedEvent>();
            AssertEventChannel<EnemyDetectedPlayerEvent>();
            AssertEventChannel<EnemyStateChangedEvent>();
            AssertEventChannel<InteractionEvent>();
            AssertEventChannel<PlayerDiedEvent>();
            AssertEventChannel<FootstepEvent>();
            AssertEventChannel<StaminaLowEvent>();
            AssertEventChannel<StaminaChangedEvent>();
            AssertEventChannel<JumpscareTriggeredEvent>();
            AssertEventChannel<PlayerHidingEvent>();
            AssertEventChannel<PlayerHidingEndEvent>();
            AssertEventChannel<HallucinationSpawnEvent>();
            AssertEventChannel<LightFlickerEvent>();
            AssertEventChannel<AmbientStingerEvent>();
            AssertEventChannel<PuzzleSolvedEvent>();
            AssertEventChannel<SanityChangedEvent>();
            AssertEventChannel<EnemyDetectedSoundEvent>();
            AssertEventChannel<ItemCollectedEvent>();
            AssertEventChannel<DoorStateChangedEvent>();
            AssertEventChannel<SaveLoadEvent>();
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

        private sealed class ConcreteService : IService
        {
        }

        private static void AssertEventChannel<TEvent>()
            where TEvent : struct
        {
            Assert.IsFalse(EventBus<TEvent>.HasListeners());
            EventBus<TEvent>.Publish(default);
        }

        private readonly struct TestEvent
        {
            public TestEvent(int value)
            {
                Value = value;
            }

            public int Value { get; }
        }

        private readonly struct LimitedEvent
        {
        }
    }
}
