using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Pure yaw/pitch accumulator with deterministic vertical clamp.
    /// </summary>
    public sealed class FirstPersonLookModel
    {
        public float Yaw { get; private set; }

        public float Pitch { get; private set; }

        public void Reset(float yaw, float pitch)
        {
            Yaw = yaw;
            Pitch = pitch;
        }

        public void ApplyLook(
            Vector2 lookDelta,
            Vector2 sensitivity,
            float minPitch,
            float maxPitch,
            bool invertY)
        {
            Yaw += lookDelta.x * sensitivity.x;

            float pitchDelta = lookDelta.y * sensitivity.y;
            Pitch += invertY ? pitchDelta : -pitchDelta;
            Pitch = Mathf.Clamp(Pitch, minPitch, maxPitch);
        }
    }
}
