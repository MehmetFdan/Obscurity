using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Immutable stamina tuning used by the testable stamina model.
    /// </summary>
    public readonly struct StaminaSettings
    {
        public StaminaSettings(
            float maximum,
            float drainPerSecond,
            float regenPerSecond,
            float regenDelay,
            float exhaustedReenableThreshold,
            float lowThreshold,
            float lowEventCooldown,
            float lowSanityThreshold,
            float lowSanityRegenMultiplier)
        {
            Maximum = Mathf.Max(1f, maximum);
            DrainPerSecond = Mathf.Max(0f, drainPerSecond);
            RegenPerSecond = Mathf.Max(0f, regenPerSecond);
            RegenDelay = Mathf.Max(0f, regenDelay);
            ExhaustedReenableThreshold = Mathf.Clamp(exhaustedReenableThreshold, 0f, Maximum);
            LowThreshold = Mathf.Clamp(lowThreshold, 0f, Maximum);
            LowEventCooldown = Mathf.Max(0.1f, lowEventCooldown);
            LowSanityThreshold = Mathf.Clamp01(lowSanityThreshold);
            LowSanityRegenMultiplier = Mathf.Clamp(lowSanityRegenMultiplier, 0.05f, 1f);
        }

        public static StaminaSettings Default => new(100f, 18f, 14f, 0.75f, 22f, 20f, 3f, 0.35f, 0.55f);

        public float Maximum { get; }

        public float DrainPerSecond { get; }

        public float RegenPerSecond { get; }

        public float RegenDelay { get; }

        public float ExhaustedReenableThreshold { get; }

        public float LowThreshold { get; }

        public float LowEventCooldown { get; }

        public float LowSanityThreshold { get; }

        public float LowSanityRegenMultiplier { get; }
    }

    /// <summary>
    /// Result packet emitted by StaminaModel after each simulation step.
    /// </summary>
    public readonly struct StaminaTickResult
    {
        public StaminaTickResult(
            float previous,
            float current,
            float maximum,
            bool isSprinting,
            bool isExhausted,
            bool staminaChanged,
            bool sprintStateChanged,
            bool exhaustionTriggered,
            bool lowStaminaTriggered)
        {
            Previous = previous;
            Current = current;
            Maximum = maximum;
            IsSprinting = isSprinting;
            IsExhausted = isExhausted;
            StaminaChanged = staminaChanged;
            SprintStateChanged = sprintStateChanged;
            ExhaustionTriggered = exhaustionTriggered;
            LowStaminaTriggered = lowStaminaTriggered;
        }

        public float Previous { get; }

        public float Current { get; }

        public float Maximum { get; }

        public bool IsSprinting { get; }

        public bool IsExhausted { get; }

        public bool StaminaChanged { get; }

        public bool SprintStateChanged { get; }

        public bool ExhaustionTriggered { get; }

        public bool LowStaminaTriggered { get; }

        public float Normalized => Maximum <= 0f ? 0f : Current / Maximum;
    }

    /// <summary>
    /// Pure stamina simulation. MonoBehaviours publish events from its deterministic output.
    /// </summary>
    public sealed class StaminaModel
    {
        private float regenDelayRemaining;
        private float lowEventCooldownRemaining;

        public StaminaModel(StaminaSettings settings)
        {
            Settings = settings;
            Current = settings.Maximum;
        }

        public StaminaSettings Settings { get; }

        public float Current { get; private set; }

        public bool IsSprinting { get; private set; }

        public bool IsExhausted { get; private set; }

        public float Normalized => Settings.Maximum <= 0f ? 0f : Current / Settings.Maximum;

        public void Reset(float value = -1f)
        {
            Current = value < 0f ? Settings.Maximum : Mathf.Clamp(value, 0f, Settings.Maximum);
            IsSprinting = false;
            IsExhausted = Current <= 0f;
            regenDelayRemaining = 0f;
            lowEventCooldownRemaining = 0f;
        }

        public void SetCurrent(float value)
        {
            Current = Mathf.Clamp(value, 0f, Settings.Maximum);
            if (Current <= 0f)
            {
                IsExhausted = true;
            }
            else if (Current >= Settings.ExhaustedReenableThreshold)
            {
                IsExhausted = false;
            }
        }

        public StaminaTickResult Tick(
            bool wantsSprint,
            bool hasMovementInput,
            float sanityNormalized,
            float deltaTime)
        {
            float dt = Mathf.Max(0f, deltaTime);
            float previous = Current;
            bool previousSprinting = IsSprinting;
            bool exhaustionTriggered = false;

            if (lowEventCooldownRemaining > 0f)
            {
                lowEventCooldownRemaining = Mathf.Max(0f, lowEventCooldownRemaining - dt);
            }

            bool canSprint = wantsSprint
                && hasMovementInput
                && !IsExhausted
                && Current > 0f;

            IsSprinting = canSprint;

            if (IsSprinting)
            {
                regenDelayRemaining = Settings.RegenDelay;
                Current = Mathf.Max(0f, Current - Settings.DrainPerSecond * dt);

                if (Current <= 0f)
                {
                    IsSprinting = false;
                    IsExhausted = true;
                    exhaustionTriggered = true;
                }
            }
            else
            {
                if (regenDelayRemaining > 0f)
                {
                    regenDelayRemaining = Mathf.Max(0f, regenDelayRemaining - dt);
                }
                else if (Current < Settings.Maximum)
                {
                    float regenMultiplier = Mathf.Clamp01(sanityNormalized) <= Settings.LowSanityThreshold
                        ? Settings.LowSanityRegenMultiplier
                        : 1f;

                    Current = Mathf.Min(
                        Settings.Maximum,
                        Current + Settings.RegenPerSecond * regenMultiplier * dt);
                }

                if (IsExhausted && Current >= Settings.ExhaustedReenableThreshold)
                {
                    IsExhausted = false;
                }
            }

            bool lowTriggered = Current <= Settings.LowThreshold && lowEventCooldownRemaining <= 0f;
            if (lowTriggered)
            {
                lowEventCooldownRemaining = Settings.LowEventCooldown;
            }

            return new StaminaTickResult(
                previous,
                Current,
                Settings.Maximum,
                IsSprinting,
                IsExhausted,
                !Mathf.Approximately(previous, Current),
                previousSprinting != IsSprinting,
                exhaustionTriggered,
                lowTriggered);
        }
    }
}
