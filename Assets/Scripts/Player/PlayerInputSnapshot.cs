using UnityEngine;

namespace HorrorGame.Player
{
    /// <summary>
    /// Frame-local input values consumed by PlayerController.
    /// </summary>
    public readonly struct PlayerInputSnapshot
    {
        public PlayerInputSnapshot(
            Vector2 move,
            Vector2 look,
            bool wantsSprint,
            bool crouchPressed,
            bool crouchHeld,
            float lean,
            bool pointerLook)
        {
            Move = Vector2.ClampMagnitude(move, 1f);
            Look = look;
            WantsSprint = wantsSprint;
            CrouchPressed = crouchPressed;
            CrouchHeld = crouchHeld;
            Lean = Mathf.Clamp(lean, -1f, 1f);
            PointerLook = pointerLook;
        }

        public static PlayerInputSnapshot None => new(
            Vector2.zero,
            Vector2.zero,
            false,
            false,
            false,
            0f,
            true);

        public Vector2 Move { get; }

        public Vector2 Look { get; }

        public bool WantsSprint { get; }

        public bool CrouchPressed { get; }

        public bool CrouchHeld { get; }

        public float Lean { get; }

        public bool PointerLook { get; }
    }
}
