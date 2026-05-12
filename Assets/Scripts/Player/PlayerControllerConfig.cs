using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// ScriptableObject tuning for the M1 first-person movement stack.
    /// </summary>
    [CreateAssetMenu(menuName = "Horror Game/Player/M1 Player Controller Config")]
    public sealed class PlayerControllerConfig : ScriptableObject
    {
        [Header("Movement")]
        [SerializeField, Min(0.1f)] private float walkSpeed = 4.2f;
        [SerializeField, Min(1f)] private float sprintMultiplier = 1.8f;
        [SerializeField, Range(0.1f, 1f)] private float crouchSpeedMultiplier = 0.55f;
        [SerializeField, Min(0.1f)] private float acceleration = 22f;
        [SerializeField, Min(0.1f)] private float airAcceleration = 7f;
        [SerializeField] private float gravity = -24f;
        [SerializeField] private float groundedGravity = -2f;
        [SerializeField, Range(0f, 89f)] private float maxSlopeAngle = 45f;
        [SerializeField, Min(0.01f)] private float groundProbeDistance = 0.35f;
        [SerializeField] private LayerMask groundMask = ~0;

        [Header("Look")]
        [SerializeField] private Vector2 mouseSensitivity = new(0.12f, 0.12f);
        [SerializeField] private Vector2 gamepadSensitivity = new(180f, 140f);
        [SerializeField, Range(-89f, 0f)] private float minPitch = -89f;
        [SerializeField, Range(0f, 89f)] private float maxPitch = 89f;
        [SerializeField] private bool invertY;

        [Header("Stamina")]
        [SerializeField, Min(1f)] private float maxStamina = 100f;
        [SerializeField, Min(0f)] private float staminaDrainPerSecond = 18f;
        [SerializeField, Min(0f)] private float staminaRegenPerSecond = 14f;
        [SerializeField, Min(0f)] private float staminaRegenDelay = 0.75f;
        [SerializeField, Min(0f)] private float exhaustedReenableThreshold = 22f;
        [SerializeField, Min(0f)] private float lowStaminaThreshold = 20f;
        [SerializeField, Min(0.1f)] private float lowStaminaEventCooldown = 3f;
        [SerializeField, Range(0f, 1f)] private float lowSanityThreshold = 0.35f;
        [SerializeField, Range(0.05f, 1f)] private float lowSanityRegenMultiplier = 0.55f;

        [Header("Crouch")]
        [SerializeField] private bool crouchToggle = true;
        [SerializeField, Min(0.25f)] private float standingHeight = 2f;
        [SerializeField, Min(0.25f)] private float crouchingHeight = 1f;
        [SerializeField, Min(0f)] private float standingCameraHeight = 1.65f;
        [SerializeField, Min(0f)] private float crouchingCameraHeight = 0.95f;
        [SerializeField, Min(0.1f)] private float cameraCrouchSmoothTime = 0.08f;
        [SerializeField, Min(0.01f)] private float standCheckRadiusPadding = 0.02f;
        [SerializeField] private LayerMask ceilingMask = ~0;

        [Header("Lean")]
        [SerializeField, Range(0f, 30f)] private float leanAngle = 15f;
        [SerializeField, Range(0f, 0.5f)] private float leanOffset = 0.22f;
        [SerializeField, Min(0.01f)] private float leanSmoothTime = 0.075f;
        [SerializeField, Min(1f)] private float leanFootstepIntervalMultiplier = 1.25f;

        [Header("Procedural Camera")]
        [SerializeField, Min(0f)] private float walkBobAmplitude = 0.035f;
        [SerializeField, Min(0f)] private float sprintBobAmplitude = 0.06f;
        [SerializeField, Min(0f)] private float crouchBobAmplitude = 0.018f;
        [SerializeField, Min(0.1f)] private float walkBobFrequency = 7.5f;
        [SerializeField, Min(0.1f)] private float sprintBobFrequency = 11.5f;
        [SerializeField, Min(0.1f)] private float crouchBobFrequency = 4.8f;
        [SerializeField, Min(0.01f)] private float bobSmoothTime = 0.05f;
        [SerializeField, Min(0f)] private float walkNoiseAmplitude = 0.05f;
        [SerializeField, Min(0f)] private float sprintNoiseAmplitude = 0.12f;
        [SerializeField, Min(0f)] private float exhaustedNoiseAmplitude = 0.2f;
        [SerializeField, Min(0.1f)] private float walkNoiseFrequency = 0.7f;
        [SerializeField, Min(0.1f)] private float sprintNoiseFrequency = 1.15f;
        [SerializeField, Min(0.1f)] private float exhaustedNoiseFrequency = 1.6f;
        [SerializeField, Min(0.1f)] private float noiseBlendSpeed = 7f;

        [Header("Sound Telemetry")]
        [SerializeField, Min(0f)] private float walkSoundRadius = 3f;
        [SerializeField, Min(0f)] private float sprintSoundRadius = 7f;
        [SerializeField, Min(0f)] private float crouchSoundRadius = 1.2f;

        public float WalkSpeed => walkSpeed;

        public float SprintMultiplier => sprintMultiplier;

        public float SprintSpeed => walkSpeed * sprintMultiplier;

        public float CrouchSpeedMultiplier => crouchSpeedMultiplier;

        public float Acceleration => acceleration;

        public float AirAcceleration => airAcceleration;

        public float Gravity => gravity;

        public float GroundedGravity => groundedGravity;

        public float MaxSlopeAngle => maxSlopeAngle;

        public float GroundProbeDistance => groundProbeDistance;

        public LayerMask GroundMask => groundMask;

        public Vector2 MouseSensitivity => mouseSensitivity;

        public Vector2 GamepadSensitivity => gamepadSensitivity;

        public float MinPitch => minPitch;

        public float MaxPitch => maxPitch;

        public bool InvertY => invertY;

        public bool CrouchToggle => crouchToggle;

        public float StandingHeight => standingHeight;

        public float CrouchingHeight => crouchingHeight;

        public float StandingCameraHeight => standingCameraHeight;

        public float CrouchingCameraHeight => crouchingCameraHeight;

        public float CameraCrouchSmoothTime => cameraCrouchSmoothTime;

        public float StandCheckRadiusPadding => standCheckRadiusPadding;

        public LayerMask CeilingMask => ceilingMask;

        public float LeanAngle => leanAngle;

        public float LeanOffset => leanOffset;

        public float LeanSmoothTime => leanSmoothTime;

        public float LeanFootstepIntervalMultiplier => leanFootstepIntervalMultiplier;

        public float WalkBobAmplitude => walkBobAmplitude;

        public float SprintBobAmplitude => sprintBobAmplitude;

        public float CrouchBobAmplitude => crouchBobAmplitude;

        public float WalkBobFrequency => walkBobFrequency;

        public float SprintBobFrequency => sprintBobFrequency;

        public float CrouchBobFrequency => crouchBobFrequency;

        public float BobSmoothTime => bobSmoothTime;

        public float WalkNoiseAmplitude => walkNoiseAmplitude;

        public float SprintNoiseAmplitude => sprintNoiseAmplitude;

        public float ExhaustedNoiseAmplitude => exhaustedNoiseAmplitude;

        public float WalkNoiseFrequency => walkNoiseFrequency;

        public float SprintNoiseFrequency => sprintNoiseFrequency;

        public float ExhaustedNoiseFrequency => exhaustedNoiseFrequency;

        public float NoiseBlendSpeed => noiseBlendSpeed;

        public float WalkSoundRadius => walkSoundRadius;

        public float SprintSoundRadius => sprintSoundRadius;

        public float CrouchSoundRadius => crouchSoundRadius;

        public StaminaSettings StaminaSettings => new(
            maxStamina,
            staminaDrainPerSecond,
            staminaRegenPerSecond,
            staminaRegenDelay,
            exhaustedReenableThreshold,
            lowStaminaThreshold,
            lowStaminaEventCooldown,
            lowSanityThreshold,
            lowSanityRegenMultiplier);

        private void OnValidate()
        {
            maxPitch = Mathf.Max(0f, maxPitch);
            minPitch = Mathf.Min(0f, minPitch);
            crouchingHeight = Mathf.Min(crouchingHeight, standingHeight);
            exhaustedReenableThreshold = Mathf.Clamp(exhaustedReenableThreshold, 0f, maxStamina);
            lowStaminaThreshold = Mathf.Clamp(lowStaminaThreshold, 0f, maxStamina);
        }
    }
}
