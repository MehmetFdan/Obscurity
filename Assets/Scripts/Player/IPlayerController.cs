using HorrorGame.Core;
using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Public player service contract exposed through the ServiceLocator.
    /// </summary>
    public interface IPlayerController : IService
    {
        Transform Transform { get; }

        Vector3 Velocity { get; }

        float HorizontalSpeed { get; }

        bool IsGrounded { get; }

        bool IsMoving { get; }

        bool IsSprinting { get; }

        bool IsCrouching { get; }

        float LeanNormalized { get; }

        float Stamina { get; }

        float StaminaNormalized { get; }

        float SoundRadius { get; }

        void Teleport(Vector3 position, Quaternion rotation);
    }
}
