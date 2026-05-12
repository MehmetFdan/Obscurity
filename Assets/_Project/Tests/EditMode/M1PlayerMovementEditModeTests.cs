using HorrorGame.Player;
using NUnit.Framework;
using UnityEngine;

namespace HorrorGame.Tests.EditMode
{
    public sealed class M1PlayerMovementEditModeTests
    {
        [Test]
        public void FirstPersonLookModel_ClampsPitchAndAccumulatesYaw()
        {
            var look = new FirstPersonLookModel();
            look.Reset(10f, 0f);

            look.ApplyLook(new Vector2(100f, 1000f), new Vector2(0.5f, 1f), -89f, 89f, false);

            Assert.AreEqual(60f, look.Yaw);
            Assert.AreEqual(-89f, look.Pitch);

            look.ApplyLook(new Vector2(0f, -1000f), new Vector2(0.5f, 1f), -89f, 89f, false);

            Assert.AreEqual(89f, look.Pitch);
        }

        [Test]
        public void StaminaModel_DrainsStopsSprintAndSignalsLowStamina()
        {
            var settings = new StaminaSettings(
                maximum: 100f,
                drainPerSecond: 50f,
                regenPerSecond: 25f,
                regenDelay: 0f,
                exhaustedReenableThreshold: 20f,
                lowThreshold: 25f,
                lowEventCooldown: 10f,
                lowSanityThreshold: 0.35f,
                lowSanityRegenMultiplier: 0.5f);
            var stamina = new StaminaModel(settings);

            StaminaTickResult warning = stamina.Tick(
                wantsSprint: true,
                hasMovementInput: true,
                sanityNormalized: 1f,
                deltaTime: 1.6f);

            Assert.AreEqual(20f, warning.Current, 0.001f);
            Assert.IsTrue(warning.LowStaminaTriggered);
            Assert.IsTrue(warning.IsSprinting);

            StaminaTickResult exhausted = stamina.Tick(
                wantsSprint: true,
                hasMovementInput: true,
                sanityNormalized: 1f,
                deltaTime: 0.5f);

            Assert.AreEqual(0f, exhausted.Current);
            Assert.IsTrue(exhausted.ExhaustionTriggered);
            Assert.IsTrue(exhausted.IsExhausted);
            Assert.IsFalse(exhausted.IsSprinting);
        }

        [Test]
        public void StaminaModel_LowSanitySlowsRegeneration()
        {
            var settings = new StaminaSettings(
                maximum: 100f,
                drainPerSecond: 0f,
                regenPerSecond: 20f,
                regenDelay: 0f,
                exhaustedReenableThreshold: 10f,
                lowThreshold: 5f,
                lowEventCooldown: 1f,
                lowSanityThreshold: 0.5f,
                lowSanityRegenMultiplier: 0.25f);
            var calm = new StaminaModel(settings);
            var panicked = new StaminaModel(settings);
            calm.SetCurrent(40f);
            panicked.SetCurrent(40f);

            calm.Tick(false, false, 1f, 1f);
            panicked.Tick(false, false, 0.25f, 1f);

            Assert.AreEqual(60f, calm.Current);
            Assert.AreEqual(45f, panicked.Current);
        }
    }
}
