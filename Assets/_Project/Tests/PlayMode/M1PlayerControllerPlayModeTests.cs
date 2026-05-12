using System.Collections;
using HorrorGame.Core;
using HorrorGame.Player;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HorrorGame.Tests.PlayMode
{
    public sealed class M1PlayerControllerPlayModeTests
    {
        private const float TestDeltaTime = 1f / 60f;

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

        [UnityTest]
        public IEnumerator PlayerController_RegistersServiceAndMovesForward()
        {
            TestRig rig = CreateRig();

            try
            {
                Assert.IsTrue(ServiceLocator.TryGet(out IPlayerController registeredController));
                Assert.AreSame(rig.Controller, registeredController);
                Assert.AreEqual(1f, rig.CharacterController.center.y, 0.01f);
                Assert.AreEqual(1.65f, rig.CameraHolder.localPosition.y, 0.01f);
                Assert.Less(
                    rig.CameraHolder.localPosition.y,
                    rig.CharacterController.center.y + rig.CharacterController.height * 0.5f);

                PlayerInputSnapshot moveInput = new(
                    Vector2.up,
                    Vector2.zero,
                    false,
                    false,
                    false,
                    0f,
                    true);

                for (int i = 0; i < 8; i++)
                {
                    rig.Controller.SimulateFrame(moveInput, TestDeltaTime);
                }

                Assert.Greater(rig.Player.transform.position.z, 0.1f);
                Assert.IsTrue(rig.Controller.IsGrounded);
                Assert.IsTrue(rig.Controller.IsMoving);
                yield return null;
            }
            finally
            {
                rig.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator PlayerController_StaminaPublishesLowEventAndStopsAtExhaustion()
        {
            TestRig rig = CreateRig();
            int lowEvents = 0;
            EventBus<StaminaLowEvent>.Subscribe(OnLowStamina);

            try
            {
                rig.Controller.SetStamina(23f);
                PlayerInputSnapshot sprintInput = new(
                    Vector2.up,
                    Vector2.zero,
                    true,
                    false,
                    false,
                    0f,
                    true);

                for (int i = 0; i < 12; i++)
                {
                    rig.Controller.SimulateFrame(sprintInput, TestDeltaTime);
                }

                Assert.GreaterOrEqual(lowEvents, 1);

                for (int i = 0; i < 90; i++)
                {
                    rig.Controller.SimulateFrame(sprintInput, TestDeltaTime);
                }

                Assert.IsFalse(rig.Controller.IsSprinting);
                Assert.AreEqual(0f, rig.Controller.Stamina, 0.2f);
                yield return null;
            }
            finally
            {
                EventBus<StaminaLowEvent>.Unsubscribe(OnLowStamina);
                rig.Destroy();
            }

            void OnLowStamina(StaminaLowEvent _)
            {
                lowEvents++;
            }
        }

        [UnityTest]
        public IEnumerator PlayerController_CrouchAndLeanAffectCameraRigOnly()
        {
            TestRig rig = CreateRig();
            Vector3 startingBodyPosition = rig.Player.transform.position;

            try
            {
                PlayerInputSnapshot crouchPressedInput = new(
                    Vector2.zero,
                    Vector2.zero,
                    false,
                    true,
                    true,
                    0f,
                    true);
                rig.Controller.SimulateFrame(crouchPressedInput, TestDeltaTime);

                for (int i = 0; i < 5; i++)
                {
                    rig.Controller.SimulateFrame(PlayerInputSnapshot.None, TestDeltaTime);
                }

                Assert.IsTrue(rig.Controller.IsCrouching);
                Assert.AreEqual(1f, rig.CharacterController.height, 0.01f);
                Assert.Less(rig.CameraHolder.localPosition.y, 1.65f);

                PlayerInputSnapshot leanRightInput = new(
                    Vector2.zero,
                    Vector2.zero,
                    false,
                    false,
                    false,
                    1f,
                    true);
                for (int i = 0; i < 8; i++)
                {
                    rig.Controller.SimulateFrame(leanRightInput, TestDeltaTime);
                }

                Assert.Greater(rig.Controller.LeanNormalized, 0.1f);
                Assert.Greater(rig.CameraHolder.localPosition.x, 0.01f);
                Assert.AreEqual(startingBodyPosition.x, rig.Player.transform.position.x, 0.01f);
                Assert.AreEqual(startingBodyPosition.z, rig.Player.transform.position.z, 0.01f);
                yield return null;
            }
            finally
            {
                rig.Destroy();
            }
        }

        [UnityTest]
        public IEnumerator ProceduralCameraAnimator_BobsWhenPlayerMoves()
        {
            TestRig rig = CreateRig(addProceduralAnimator: true);
            Vector3 startingCameraPosition = rig.CinemachineCamera.localPosition;

            try
            {
                PlayerInputSnapshot moveInput = new(
                    Vector2.up,
                    Vector2.zero,
                    false,
                    false,
                    false,
                    0f,
                    true);

                for (int i = 0; i < 12; i++)
                {
                    rig.Controller.SimulateFrame(moveInput, TestDeltaTime);
                    rig.CameraAnimator.SimulateFrame(TestDeltaTime);
                }

                Assert.AreNotEqual(startingCameraPosition, rig.CinemachineCamera.localPosition);
                yield return null;
            }
            finally
            {
                rig.Destroy();
            }
        }

        private static TestRig CreateRig(bool addProceduralAnimator = false)
        {
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "M1 Test Ground";

            GameObject player = new("M1 Test Player");
            player.transform.position = Vector3.zero;

            CharacterController characterController = player.AddComponent<CharacterController>();
            characterController.height = 2f;
            characterController.radius = 0.5f;
            characterController.center = Vector3.zero;
            characterController.stepOffset = 0.3f;
            characterController.slopeLimit = 45f;

            Transform cameraHolder = new GameObject("CameraHolder").transform;
            cameraHolder.SetParent(player.transform, false);

            Transform cinemachineCamera = new GameObject("CinemachineCamera").transform;
            cinemachineCamera.SetParent(cameraHolder, false);

            PlayerController controller = player.AddComponent<PlayerController>();

            PlayerCameraProceduralAnimator cameraAnimator = null;
            if (addProceduralAnimator)
            {
                cameraAnimator = cinemachineCamera.gameObject.AddComponent<PlayerCameraProceduralAnimator>();
            }

            return new TestRig(
                ground,
                player,
                characterController,
                controller,
                cameraHolder,
                cinemachineCamera,
                cameraAnimator);
        }

        private readonly struct TestRig
        {
            public TestRig(
                GameObject ground,
                GameObject player,
                CharacterController characterController,
                PlayerController controller,
                Transform cameraHolder,
                Transform cinemachineCamera,
                PlayerCameraProceduralAnimator cameraAnimator)
            {
                Ground = ground;
                Player = player;
                CharacterController = characterController;
                Controller = controller;
                CameraHolder = cameraHolder;
                CinemachineCamera = cinemachineCamera;
                CameraAnimator = cameraAnimator;
            }

            public GameObject Ground { get; }

            public GameObject Player { get; }

            public CharacterController CharacterController { get; }

            public PlayerController Controller { get; }

            public Transform CameraHolder { get; }

            public Transform CinemachineCamera { get; }

            public PlayerCameraProceduralAnimator CameraAnimator { get; }

            public void Destroy()
            {
                if (Ground != null)
                {
                    Object.Destroy(Ground);
                }

                if (Player != null)
                {
                    Object.Destroy(Player);
                }
            }
        }
    }
}
