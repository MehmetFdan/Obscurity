using UnityEngine;

namespace HorrorGame.Core
{
    public enum SaveLoadOperation
    {
        SaveStarted,
        SaveCompleted,
        LoadStarted,
        LoadCompleted
    }

    /// <summary>
    /// Raised whenever the player stamina value or sprint lock state changes.
    /// </summary>
    public readonly struct StaminaChangedEvent
    {
        public StaminaChangedEvent(float current, float maximum, bool isSprinting, bool isExhausted)
        {
            Current = current;
            Maximum = maximum;
            IsSprinting = isSprinting;
            IsExhausted = isExhausted;
        }

        public float Current { get; }

        public float Maximum { get; }

        public bool IsSprinting { get; }

        public bool IsExhausted { get; }

        public float Normalized => Maximum <= 0f ? 0f : Current / Maximum;
    }

    /// <summary>
    /// Raised when stamina drops into the warning band.
    /// </summary>
    public readonly struct StaminaLowEvent
    {
        public StaminaLowEvent(float current, float threshold)
        {
            Current = current;
            Threshold = threshold;
        }

        public float Current { get; }

        public float Threshold { get; }
    }

    /// <summary>
    /// Raised once when the player reaches exhaustion and forced breathing should play.
    /// </summary>
    public readonly struct PlayerBreathRequiredEvent
    {
        public PlayerBreathRequiredEvent(float staminaNormalized)
        {
            StaminaNormalized = staminaNormalized;
        }

        public float StaminaNormalized { get; }
    }

    /// <summary>
    /// Raised when the high-level player locomotion state changes.
    /// </summary>
    public readonly struct PlayerMovementStateChangedEvent
    {
        public PlayerMovementStateChangedEvent(
            bool isGrounded,
            bool isMoving,
            bool isSprinting,
            bool isCrouching,
            float horizontalSpeed,
            float soundRadius)
        {
            IsGrounded = isGrounded;
            IsMoving = isMoving;
            IsSprinting = isSprinting;
            IsCrouching = isCrouching;
            HorizontalSpeed = horizontalSpeed;
            SoundRadius = soundRadius;
        }

        public bool IsGrounded { get; }

        public bool IsMoving { get; }

        public bool IsSprinting { get; }

        public bool IsCrouching { get; }

        public float HorizontalSpeed { get; }

        public float SoundRadius { get; }
    }

    /// <summary>
    /// Raised when the player enters or exits crouch.
    /// </summary>
    public readonly struct PlayerCrouchChangedEvent
    {
        public PlayerCrouchChangedEvent(bool isCrouching, float controllerHeight)
        {
            IsCrouching = isCrouching;
            ControllerHeight = controllerHeight;
        }

        public bool IsCrouching { get; }

        public float ControllerHeight { get; }
    }

    /// <summary>
    /// Raised when camera lean changes enough for downstream systems to react.
    /// </summary>
    public readonly struct PlayerLeanChangedEvent
    {
        public PlayerLeanChangedEvent(float normalizedLean, float angle)
        {
            NormalizedLean = normalizedLean;
            Angle = angle;
        }

        public float NormalizedLean { get; }

        public float Angle { get; }
    }

    /// <summary>
    /// Shared sanity event contract used by stamina and later psychological systems.
    /// </summary>
    public readonly struct PlayerSanityChangedEvent
    {
        public PlayerSanityChangedEvent(float newSanity, float maximumSanity = 100f)
        {
            NewSanity = newSanity;
            MaximumSanity = maximumSanity;
        }

        public float NewSanity { get; }

        public float MaximumSanity { get; }

        public float NormalizedSanity => MaximumSanity <= 0f ? 0f : NewSanity / MaximumSanity;
    }

    public readonly struct SanityChangedEvent
    {
        public SanityChangedEvent(float current, float maximum, float delta)
        {
            Current = current;
            Maximum = maximum;
            Delta = delta;
        }

        public float Current { get; }

        public float Maximum { get; }

        public float Delta { get; }

        public float Normalized => Maximum <= 0f ? 0f : Current / Maximum;
    }

    public readonly struct EnemyDetectedPlayerEvent
    {
        public EnemyDetectedPlayerEvent(Transform enemy, Transform player, Vector3 playerPosition, float awareness = 1f)
        {
            Enemy = enemy;
            Player = player;
            PlayerPosition = playerPosition;
            Awareness = awareness;
        }

        public Transform Enemy { get; }

        public Transform Player { get; }

        public Vector3 PlayerPosition { get; }

        public float Awareness { get; }
    }

    public readonly struct EnemyDetectedSoundEvent
    {
        public EnemyDetectedSoundEvent(Vector3 soundPosition, float radius, float loudness = 1f, Transform source = null)
        {
            SoundPosition = soundPosition;
            Radius = radius;
            Loudness = loudness;
            Source = source;
        }

        public Vector3 SoundPosition { get; }

        public float Radius { get; }

        public float Loudness { get; }

        public Transform Source { get; }
    }

    public readonly struct EnemyStateChangedEvent
    {
        public EnemyStateChangedEvent(Transform enemy, string previousState, string newState)
        {
            Enemy = enemy;
            PreviousState = previousState;
            NewState = newState;
        }

        public Transform Enemy { get; }

        public string PreviousState { get; }

        public string NewState { get; }
    }

    public readonly struct InteractionEvent
    {
        public InteractionEvent(Transform interactor, Transform target, string interactionId, Vector3 position)
        {
            Interactor = interactor;
            Target = target;
            InteractionId = interactionId;
            Position = position;
        }

        public Transform Interactor { get; }

        public Transform Target { get; }

        public string InteractionId { get; }

        public Vector3 Position { get; }
    }

    public readonly struct PlayerDiedEvent
    {
        public PlayerDiedEvent(Transform player, Vector3 position, string cause)
        {
            Player = player;
            Position = position;
            Cause = cause;
        }

        public Transform Player { get; }

        public Vector3 Position { get; }

        public string Cause { get; }
    }

    public readonly struct FootstepEvent
    {
        public FootstepEvent(
            Vector3 position,
            float soundRadius,
            string surfaceId,
            bool isSprinting,
            bool isCrouching)
        {
            Position = position;
            SoundRadius = soundRadius;
            SurfaceId = surfaceId;
            IsSprinting = isSprinting;
            IsCrouching = isCrouching;
        }

        public Vector3 Position { get; }

        public float SoundRadius { get; }

        public string SurfaceId { get; }

        public bool IsSprinting { get; }

        public bool IsCrouching { get; }
    }

    public readonly struct JumpscareTriggeredEvent
    {
        public JumpscareTriggeredEvent(string jumpscareId, Vector3 position, float sanityDamage, Transform source = null)
        {
            JumpscareId = jumpscareId;
            Position = position;
            SanityDamage = sanityDamage;
            Source = source;
        }

        public string JumpscareId { get; }

        public Vector3 Position { get; }

        public float SanityDamage { get; }

        public Transform Source { get; }
    }

    public readonly struct PlayerHidingEvent
    {
        public PlayerHidingEvent(Transform player, Transform hideSpot, Vector3 position)
        {
            Player = player;
            HideSpot = hideSpot;
            Position = position;
        }

        public Transform Player { get; }

        public Transform HideSpot { get; }

        public Vector3 Position { get; }
    }

    public readonly struct PlayerHidingEndEvent
    {
        public PlayerHidingEndEvent(Transform player, Transform hideSpot, Vector3 position, float duration)
        {
            Player = player;
            HideSpot = hideSpot;
            Position = position;
            Duration = duration;
        }

        public Transform Player { get; }

        public Transform HideSpot { get; }

        public Vector3 Position { get; }

        public float Duration { get; }
    }

    public readonly struct HallucinationSpawnEvent
    {
        public HallucinationSpawnEvent(Vector3 spawnPosition, GameObject prefab, float lifetime, float sanityImpact = 0f)
        {
            SpawnPosition = spawnPosition;
            Prefab = prefab;
            Lifetime = lifetime;
            SanityImpact = sanityImpact;
        }

        public Vector3 SpawnPosition { get; }

        public GameObject Prefab { get; }

        public float Lifetime { get; }

        public float SanityImpact { get; }
    }

    public readonly struct LightFlickerEvent
    {
        public LightFlickerEvent(Vector3 position, float duration, AnimationCurve flickerCurve, float sanityImpact = 0f)
        {
            Position = position;
            Duration = duration;
            FlickerCurve = flickerCurve;
            SanityImpact = sanityImpact;
        }

        public Vector3 Position { get; }

        public float Duration { get; }

        public AnimationCurve FlickerCurve { get; }

        public float SanityImpact { get; }
    }

    public readonly struct AmbientStingerEvent
    {
        public AmbientStingerEvent(AudioClip clip, Vector3 position, float volume)
        {
            Clip = clip;
            Position = position;
            Volume = volume;
        }

        public AudioClip Clip { get; }

        public Vector3 Position { get; }

        public float Volume { get; }
    }

    public readonly struct PuzzleSolvedEvent
    {
        public PuzzleSolvedEvent(string puzzleId, Transform puzzleRoot, float solveTime)
        {
            PuzzleId = puzzleId;
            PuzzleRoot = puzzleRoot;
            SolveTime = solveTime;
        }

        public string PuzzleId { get; }

        public Transform PuzzleRoot { get; }

        public float SolveTime { get; }
    }

    public readonly struct ItemCollectedEvent
    {
        public ItemCollectedEvent(string itemId, int quantity, Transform collector)
        {
            ItemId = itemId;
            Quantity = quantity;
            Collector = collector;
        }

        public string ItemId { get; }

        public int Quantity { get; }

        public Transform Collector { get; }
    }

    public readonly struct DoorStateChangedEvent
    {
        public DoorStateChangedEvent(string doorId, Transform door, bool isOpen, bool isLocked)
        {
            DoorId = doorId;
            Door = door;
            IsOpen = isOpen;
            IsLocked = isLocked;
        }

        public string DoorId { get; }

        public Transform Door { get; }

        public bool IsOpen { get; }

        public bool IsLocked { get; }
    }

    public readonly struct SaveLoadEvent
    {
        public SaveLoadEvent(SaveLoadOperation operation, string slotId, bool succeeded, string message = null)
        {
            Operation = operation;
            SlotId = slotId;
            Succeeded = succeeded;
            Message = message;
        }

        public SaveLoadOperation Operation { get; }

        public string SlotId { get; }

        public bool Succeeded { get; }

        public string Message { get; }
    }
}
