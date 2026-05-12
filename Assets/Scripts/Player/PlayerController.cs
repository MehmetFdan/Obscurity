using HorrorGame.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace HorrorGame.Player
{
    /// <summary>
    /// M1 first-person controller: CharacterController movement, look, stamina, crouch and lean.
    /// </summary>
    [DefaultExecutionOrder(ServiceExecutionOrder.PlayerServices)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public sealed class PlayerController : MonoBehaviour, IPlayerController
    {
        private const float MovingInputThreshold = 0.05f;
        private const float LeanEventThreshold = 0.01f;

        [Header("Configuration")]
        [SerializeField] private PlayerControllerConfig config;
        [SerializeField] private bool registerService = true;
        [SerializeField] private bool lockCursorOnEnable = true;

        [Header("Input System")]
        [SerializeField] private InputActionAsset inputActions;
        [SerializeField] private string playerActionMapName = "Player";
        [SerializeField] private string moveActionName = "Move";
        [SerializeField] private string lookActionName = "Look";
        [SerializeField] private string sprintActionName = "Sprint";
        [SerializeField] private string crouchActionName = "Crouch";
        [SerializeField] private string leanLeftActionName = "LeanLeft";
        [SerializeField] private string leanRightActionName = "LeanRight";

        [Header("Camera Rig")]
        [SerializeField] private Transform cameraRig;

        private readonly FirstPersonLookModel lookModel = new();
        private readonly Collider[] standCheckBuffer = new Collider[8];
        private CharacterController characterController;
        private PlayerControllerConfig runtimeFallbackConfig;
        private StaminaModel staminaModel;
        private InputActionMap playerActionMap;
        private InputAction moveAction;
        private InputAction lookAction;
        private InputAction sprintAction;
        private InputAction crouchAction;
        private InputAction leanLeftAction;
        private InputAction leanRightAction;
        private Vector3 standingCenter;
        private Vector3 crouchingCenter;
        private Vector3 cameraBaseLocalPosition;
        private Vector3 horizontalVelocity;
        private Vector3 velocity;
        private Vector3 lastPosition;
        private Vector3 groundNormal = Vector3.up;
        private PlayerInputSnapshot inputOverride;
        private bool hasInputOverride;
        private bool crouchRequested;
        private bool isCrouching;
        private bool isGrounded;
        private bool sanityListenerRegistered;
        private bool wasMoving;
        private bool wasSprinting;
        private bool wasCrouching;
        private float previousLean;
        private float verticalVelocity;
        private float cameraHeightVelocity;
        private float currentCameraHeight;
        private float currentLean;
        private float leanVelocity;
        private float sanityNormalized = 1f;
        private float soundRadius;

        public Transform Transform => transform;

        public Vector3 Velocity => velocity;

        public float HorizontalSpeed => new Vector3(velocity.x, 0f, velocity.z).magnitude;

        public bool IsGrounded => isGrounded;

        public bool IsMoving { get; private set; }

        public bool IsSprinting => staminaModel != null && staminaModel.IsSprinting;

        public bool IsCrouching => isCrouching;

        public float LeanNormalized => currentLean;

        public float Stamina => staminaModel?.Current ?? Config.StaminaSettings.Maximum;

        public float StaminaNormalized => staminaModel?.Normalized ?? 1f;

        public float SoundRadius => soundRadius;

        public PlayerControllerConfig Config => config != null ? config : runtimeFallbackConfig;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();

            if (config == null)
            {
                runtimeFallbackConfig = ScriptableObject.CreateInstance<PlayerControllerConfig>();
                runtimeFallbackConfig.hideFlags = HideFlags.HideAndDontSave;
                Debug.LogWarning(
                    "PlayerController has no PlayerControllerConfig assigned. Using runtime defaults.",
                    this);
            }

            if (cameraRig == null)
            {
                cameraRig = transform.Find("CameraHolder");
            }

            if (cameraRig != null)
            {
                cameraBaseLocalPosition = cameraRig.localPosition;
                currentCameraHeight = Config.StandingCameraHeight;
                cameraBaseLocalPosition.y = currentCameraHeight;
                cameraRig.localPosition = cameraBaseLocalPosition;
            }

            standingCenter = GetGroundedControllerCenter(Config.StandingHeight);
            ApplyControllerDimensions(Config.StandingHeight, standingCenter);
            float standingBottom = standingCenter.y - Config.StandingHeight * 0.5f;
            crouchingCenter = new Vector3(
                standingCenter.x,
                standingBottom + Config.CrouchingHeight * 0.5f,
                standingCenter.z);

            characterController.slopeLimit = Config.MaxSlopeAngle;
            staminaModel = new StaminaModel(Config.StaminaSettings);
            lookModel.Reset(transform.eulerAngles.y, 0f);
            lastPosition = transform.position;

            CacheInputActions();

            if (registerService)
            {
                ServiceLocator.Register<IPlayerController>(this);
            }
        }

        private void OnEnable()
        {
            playerActionMap?.Enable();
            SubscribeSanityChanged();

            if (lockCursorOnEnable && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        private void OnDisable()
        {
            playerActionMap?.Disable();
            UnsubscribeSanityChanged();

            if (lockCursorOnEnable && Application.isPlaying)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        private void OnDestroy()
        {
            UnsubscribeSanityChanged();

            if (registerService)
            {
                ServiceLocator.Unregister<IPlayerController>(this);
            }

            if (runtimeFallbackConfig != null)
            {
                Destroy(runtimeFallbackConfig);
            }
        }

        private void Update()
        {
            SimulateFrame(ReadInput(), Time.deltaTime);
        }

        /// <summary>
        /// Advances the controller by one deterministic frame for replay and automation.
        /// </summary>
        public void SimulateFrame(PlayerInputSnapshot input, float deltaTime)
        {
            UpdateLook(input, deltaTime);
            UpdateCrouch(input);
            UpdateMovement(input, deltaTime);
            UpdateCameraRig(input, deltaTime);
            PublishMovementStateIfNeeded();

            velocity = deltaTime > 0f ? (transform.position - lastPosition) / deltaTime : Vector3.zero;
            lastPosition = transform.position;
        }

        public void Teleport(Vector3 position, Quaternion rotation)
        {
            characterController.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            characterController.enabled = true;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = 0f;
            velocity = Vector3.zero;
            lastPosition = transform.position;
            lookModel.Reset(rotation.eulerAngles.y, lookModel.Pitch);
        }

        public void SetInputOverride(PlayerInputSnapshot snapshot)
        {
            hasInputOverride = true;
            inputOverride = snapshot;
        }

        public void ClearInputOverride()
        {
            hasInputOverride = false;
            inputOverride = PlayerInputSnapshot.None;
        }

        public void SetStamina(float value)
        {
            staminaModel.SetCurrent(value);
            PublishStaminaChanged();
        }

        private void CacheInputActions()
        {
            if (inputActions == null)
            {
                return;
            }

            playerActionMap = inputActions.FindActionMap(playerActionMapName, false);
            if (playerActionMap == null)
            {
                Debug.LogWarning($"Input action map '{playerActionMapName}' was not found.", this);
                return;
            }

            moveAction = playerActionMap.FindAction(moveActionName, false);
            lookAction = playerActionMap.FindAction(lookActionName, false);
            sprintAction = playerActionMap.FindAction(sprintActionName, false);
            crouchAction = playerActionMap.FindAction(crouchActionName, false);
            leanLeftAction = playerActionMap.FindAction(leanLeftActionName, false);
            leanRightAction = playerActionMap.FindAction(leanRightActionName, false);
        }

        private PlayerInputSnapshot ReadInput()
        {
            if (hasInputOverride)
            {
                return inputOverride;
            }

            Vector2 move = moveAction?.ReadValue<Vector2>() ?? Vector2.zero;
            Vector2 look = lookAction?.ReadValue<Vector2>() ?? Vector2.zero;
            bool sprint = sprintAction != null && sprintAction.IsPressed();
            bool crouchPressed = crouchAction != null && crouchAction.WasPressedThisFrame();
            bool crouchHeld = crouchAction != null && crouchAction.IsPressed();
            float lean = 0f;

            if (leanLeftAction != null && leanLeftAction.IsPressed())
            {
                lean -= 1f;
            }

            if (leanRightAction != null && leanRightAction.IsPressed())
            {
                lean += 1f;
            }

            bool pointerLook = true;
            InputDevice lookDevice = lookAction?.activeControl?.device;
            if (lookDevice != null)
            {
                pointerLook = lookDevice is Pointer
                    || lookDevice is Mouse
                    || lookDevice is Touchscreen
                    || lookDevice is Pen;
            }

            return new PlayerInputSnapshot(move, look, sprint, crouchPressed, crouchHeld, lean, pointerLook);
        }

        private void UpdateLook(PlayerInputSnapshot input, float deltaTime)
        {
            Vector2 sensitivity = input.PointerLook
                ? Config.MouseSensitivity
                : Config.GamepadSensitivity * Mathf.Max(0f, deltaTime);

            lookModel.ApplyLook(
                input.Look,
                sensitivity,
                Config.MinPitch,
                Config.MaxPitch,
                Config.InvertY);

            transform.localRotation = Quaternion.Euler(0f, lookModel.Yaw, 0f);
        }

        private void UpdateCrouch(PlayerInputSnapshot input)
        {
            bool targetCrouch = Config.CrouchToggle
                ? (input.CrouchPressed ? !crouchRequested : crouchRequested)
                : input.CrouchHeld;

            crouchRequested = targetCrouch;

            if (isCrouching == crouchRequested)
            {
                return;
            }

            if (!crouchRequested && !CanStand())
            {
                return;
            }

            isCrouching = crouchRequested;
            ApplyControllerDimensions(
                isCrouching ? Config.CrouchingHeight : Config.StandingHeight,
                isCrouching ? crouchingCenter : standingCenter);

            EventBus<PlayerCrouchChangedEvent>.Publish(
                new PlayerCrouchChangedEvent(isCrouching, characterController.height));
        }

        private void UpdateMovement(PlayerInputSnapshot input, float deltaTime)
        {
            ProbeGround();

            Vector2 moveInput = Vector2.ClampMagnitude(input.Move, 1f);
            bool hasMoveInput = moveInput.sqrMagnitude > MovingInputThreshold * MovingInputThreshold;
            bool wantsSprint = input.WantsSprint && !isCrouching;
            StaminaTickResult staminaResult = staminaModel.Tick(
                wantsSprint,
                hasMoveInput,
                sanityNormalized,
                deltaTime);

            if (staminaResult.StaminaChanged || staminaResult.SprintStateChanged)
            {
                PublishStaminaChanged();
            }

            if (staminaResult.LowStaminaTriggered)
            {
                EventBus<StaminaLowEvent>.Publish(
                    new StaminaLowEvent(staminaResult.Current, staminaModel.Settings.LowThreshold));
            }

            if (staminaResult.ExhaustionTriggered)
            {
                EventBus<PlayerBreathRequiredEvent>.Publish(
                    new PlayerBreathRequiredEvent(staminaResult.Normalized));
            }

            float speed = Config.WalkSpeed;
            if (isCrouching)
            {
                speed *= Config.CrouchSpeedMultiplier;
            }
            else if (staminaModel.IsSprinting)
            {
                speed *= Config.SprintMultiplier;
            }

            Vector3 desiredDirection = transform.right * moveInput.x + transform.forward * moveInput.y;
            if (desiredDirection.sqrMagnitude > 1f)
            {
                desiredDirection.Normalize();
            }

            if (isGrounded)
            {
                desiredDirection = Vector3.ProjectOnPlane(desiredDirection, groundNormal).normalized;
            }

            Vector3 targetHorizontalVelocity = desiredDirection * speed;
            float acceleration = isGrounded ? Config.Acceleration : Config.AirAcceleration;
            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetHorizontalVelocity,
                acceleration * deltaTime);

            if (isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = Config.GroundedGravity;
            }
            else
            {
                verticalVelocity += Config.Gravity * deltaTime;
            }

            Vector3 motion = horizontalVelocity + Vector3.up * verticalVelocity;
            CollisionFlags flags = characterController.Move(motion * deltaTime);
            if ((flags & CollisionFlags.Below) != 0)
            {
                isGrounded = true;
                verticalVelocity = Config.GroundedGravity;
            }

            IsMoving = hasMoveInput && horizontalVelocity.sqrMagnitude > 0.01f;
            soundRadius = CalculateSoundRadius();
        }

        private void ProbeGround()
        {
            Vector3 origin = transform.TransformPoint(characterController.center);
            float rayDistance = characterController.height * 0.5f + Config.GroundProbeDistance;

            if (Physics.Raycast(
                    origin,
                    Vector3.down,
                    out RaycastHit hit,
                    rayDistance,
                    Config.GroundMask,
                    QueryTriggerInteraction.Ignore))
            {
                float slopeAngle = Vector3.Angle(hit.normal, Vector3.up);
                isGrounded = slopeAngle <= Config.MaxSlopeAngle + 0.5f;
                groundNormal = isGrounded ? hit.normal : Vector3.up;
                return;
            }

            isGrounded = characterController.isGrounded;
            groundNormal = Vector3.up;
        }

        private float CalculateSoundRadius()
        {
            if (!IsMoving)
            {
                return 0f;
            }

            if (isCrouching)
            {
                return Config.CrouchSoundRadius;
            }

            return staminaModel.IsSprinting ? Config.SprintSoundRadius : Config.WalkSoundRadius;
        }

        private void UpdateCameraRig(PlayerInputSnapshot input, float deltaTime)
        {
            if (cameraRig == null)
            {
                return;
            }

            float targetHeight = isCrouching
                ? Config.CrouchingCameraHeight
                : Config.StandingCameraHeight;

            currentCameraHeight = Mathf.SmoothDamp(
                currentCameraHeight,
                targetHeight,
                ref cameraHeightVelocity,
                Config.CameraCrouchSmoothTime,
                Mathf.Infinity,
                deltaTime);

            float targetLean = input.Lean;

            currentLean = Mathf.SmoothDamp(
                currentLean,
                targetLean,
                ref leanVelocity,
                Config.LeanSmoothTime,
                Mathf.Infinity,
                deltaTime);

            Vector3 localPosition = cameraBaseLocalPosition;
            localPosition.y = currentCameraHeight;
            localPosition.x += currentLean * Config.LeanOffset;
            cameraRig.localPosition = localPosition;
            cameraRig.localRotation = Quaternion.Euler(
                lookModel.Pitch,
                0f,
                -currentLean * Config.LeanAngle);

            if (Mathf.Abs(currentLean - previousLean) >= LeanEventThreshold)
            {
                previousLean = currentLean;
                EventBus<PlayerLeanChangedEvent>.Publish(
                    new PlayerLeanChangedEvent(currentLean, currentLean * Config.LeanAngle));
            }
        }

        private bool CanStand()
        {
            GetCapsuleWorldPoints(
                Config.StandingHeight,
                standingCenter,
                out Vector3 bottom,
                out Vector3 top,
                out float radius);

            int count = Physics.OverlapCapsuleNonAlloc(
                bottom,
                top,
                radius + Config.StandCheckRadiusPadding,
                standCheckBuffer,
                Config.CeilingMask,
                QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit = standCheckBuffer[i];
                if (hit == null || hit == characterController || hit.transform.IsChildOf(transform))
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void GetCapsuleWorldPoints(
            float height,
            Vector3 center,
            out Vector3 bottom,
            out Vector3 top,
            out float radius)
        {
            radius = Mathf.Max(0.01f, characterController.radius);
            float segment = Mathf.Max(0f, height * 0.5f - radius);
            Vector3 worldCenter = transform.TransformPoint(center);
            bottom = worldCenter - transform.up * segment;
            top = worldCenter + transform.up * segment;
        }

        private void ApplyControllerDimensions(float height, Vector3 center)
        {
            characterController.height = height;
            characterController.center = center;
        }

        private Vector3 GetGroundedControllerCenter(float height)
        {
            Vector3 center = characterController.center;
            center.y = Mathf.Max(characterController.radius, height * 0.5f);
            return center;
        }

        private void PublishStaminaChanged()
        {
            EventBus<StaminaChangedEvent>.Publish(
                new StaminaChangedEvent(
                    staminaModel.Current,
                    staminaModel.Settings.Maximum,
                    staminaModel.IsSprinting,
                    staminaModel.IsExhausted));
        }

        private void PublishMovementStateIfNeeded()
        {
            if (wasMoving == IsMoving
                && wasSprinting == IsSprinting
                && wasCrouching == isCrouching)
            {
                return;
            }

            wasMoving = IsMoving;
            wasSprinting = IsSprinting;
            wasCrouching = isCrouching;

            EventBus<PlayerMovementStateChangedEvent>.Publish(
                new PlayerMovementStateChangedEvent(
                    isGrounded,
                    IsMoving,
                    IsSprinting,
                    isCrouching,
                    HorizontalSpeed,
                    soundRadius));
        }

        private void SubscribeSanityChanged()
        {
            if (sanityListenerRegistered)
            {
                return;
            }

            EventBus<PlayerSanityChangedEvent>.Subscribe(OnSanityChanged);
            sanityListenerRegistered = true;
        }

        private void UnsubscribeSanityChanged()
        {
            if (!sanityListenerRegistered)
            {
                return;
            }

            EventBus<PlayerSanityChangedEvent>.Unsubscribe(OnSanityChanged);
            sanityListenerRegistered = false;
        }

        private void OnSanityChanged(PlayerSanityChangedEvent eventData)
        {
            sanityNormalized = Mathf.Clamp01(eventData.NormalizedSanity);
        }

        private void OnDrawGizmosSelected()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            if (characterController == null)
            {
                return;
            }

            Gizmos.color = Color.cyan;
            Vector3 origin = transform.TransformPoint(characterController.center);
            float rayDistance = characterController.height * 0.5f + (Config != null ? Config.GroundProbeDistance : 0.35f);
            Gizmos.DrawLine(origin, origin + Vector3.down * rayDistance);

            Gizmos.color = isCrouching ? Color.yellow : Color.green;
            GetCapsuleWorldPoints(
                Config != null ? Config.StandingHeight : characterController.height,
                standingCenter,
                out Vector3 bottom,
                out Vector3 top,
                out float radius);
            Gizmos.DrawWireSphere(bottom, radius);
            Gizmos.DrawWireSphere(top, radius);
            Gizmos.DrawLine(bottom + transform.right * radius, top + transform.right * radius);
            Gizmos.DrawLine(bottom - transform.right * radius, top - transform.right * radius);
        }
    }
}
