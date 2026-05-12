using Unity.Cinemachine;
using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Applies additive first-person camera bob and Cinemachine noise from player telemetry.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class PlayerCameraProceduralAnimator : MonoBehaviour
    {
        [SerializeField] private PlayerController controller;
        [SerializeField] private CinemachineBasicMultiChannelPerlin noise;

        private Transform cachedTransform;
        private Vector3 baseLocalPosition;
        private Vector3 bobVelocity;
        private float bobTime;

        private void Awake()
        {
            cachedTransform = transform;
            baseLocalPosition = cachedTransform.localPosition;

            if (controller == null)
            {
                controller = GetComponentInParent<PlayerController>();
            }

            if (noise == null)
            {
                noise = GetComponent<CinemachineBasicMultiChannelPerlin>();
            }
        }

        private void LateUpdate()
        {
            SimulateFrame(Time.deltaTime);
        }

        /// <summary>
        /// Advances additive camera effects by one deterministic frame.
        /// </summary>
        public void SimulateFrame(float deltaTime)
        {
            if (controller == null || controller.Config == null)
            {
                return;
            }

            PlayerControllerConfig config = controller.Config;
            Vector3 targetBob = CalculateBob(config, deltaTime);
            cachedTransform.localPosition = Vector3.SmoothDamp(
                cachedTransform.localPosition,
                baseLocalPosition + targetBob,
                ref bobVelocity,
                config.BobSmoothTime,
                Mathf.Infinity,
                deltaTime);

            UpdateNoise(config, deltaTime);
        }

        private Vector3 CalculateBob(PlayerControllerConfig config, float deltaTime)
        {
            if (!controller.IsGrounded || !controller.IsMoving)
            {
                bobTime = 0f;
                return Vector3.zero;
            }

            float amplitude = config.WalkBobAmplitude;
            float frequency = config.WalkBobFrequency;

            if (controller.IsCrouching)
            {
                amplitude = config.CrouchBobAmplitude;
                frequency = config.CrouchBobFrequency;
            }
            else if (controller.IsSprinting)
            {
                amplitude = config.SprintBobAmplitude;
                frequency = config.SprintBobFrequency;
            }

            bobTime += deltaTime * frequency;
            float vertical = Mathf.Sin(bobTime) * amplitude;
            float horizontal = Mathf.Cos(bobTime * 0.5f) * amplitude * 0.45f;
            return new Vector3(horizontal, vertical, 0f);
        }

        private void UpdateNoise(PlayerControllerConfig config, float deltaTime)
        {
            if (noise == null)
            {
                return;
            }

            float targetAmplitude = 0f;
            float targetFrequency = config.WalkNoiseFrequency;

            if (controller.StaminaNormalized <= 0.12f)
            {
                targetAmplitude = config.ExhaustedNoiseAmplitude;
                targetFrequency = config.ExhaustedNoiseFrequency;
            }
            else if (controller.IsSprinting)
            {
                targetAmplitude = config.SprintNoiseAmplitude;
                targetFrequency = config.SprintNoiseFrequency;
            }
            else if (controller.IsMoving)
            {
                targetAmplitude = config.WalkNoiseAmplitude;
                targetFrequency = config.WalkNoiseFrequency;
            }

            float t = 1f - Mathf.Exp(-config.NoiseBlendSpeed * deltaTime);
            noise.AmplitudeGain = Mathf.Lerp(noise.AmplitudeGain, targetAmplitude, t);
            noise.FrequencyGain = Mathf.Lerp(noise.FrequencyGain, targetFrequency, t);
        }
    }
}
