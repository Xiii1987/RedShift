using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class RedshiftFirstPersonController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform cameraPitchRoot;
    [SerializeField] private Transform cameraEffectsRoot;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 4f;
    [SerializeField] private float sprintSpeed = 6f;

    [Header("Movement Feel")]
    [SerializeField] private float groundAcceleration = 18f;
    [SerializeField] private float groundDeceleration = 22f;
    [SerializeField] private float airAcceleration = 6f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float groundedStickForce = -2f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 0.12f;
    [SerializeField] private float minLookAngle = -80f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Head Bob")]
    [SerializeField] private bool useHeadBob = true;
    [SerializeField] private float walkBobSpeed = 8f;
    [SerializeField] private float sprintBobSpeed = 11f;
    [SerializeField] private float bobAmount = 0.015f;
    [SerializeField] private float bobReturnSpeed = 10f;

    [Header("Camera Sway")]
    [SerializeField] private bool useCameraSway = true;
    [SerializeField] private float strafeRollAmount = 1.25f;
    [SerializeField] private float movementRollAmount = 0.6f;
    [SerializeField] private float swaySmoothSpeed = 8f;

    [Header("Crouch")]
    [SerializeField] private bool canCrouch = true;
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1.25f;
    [SerializeField] private float crouchSpeedMultiplier = 0.55f;
    [SerializeField] private float crouchTransitionSpeed = 8f;
    [SerializeField] private float standingCameraHeight = 0.8f;
    [SerializeField] private float crouchingCameraHeight = 0.45f;

    private CharacterController characterController;

    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float cameraPitch;
    private float currentCameraRoll;
    private float headBobTimer;
    private bool isCrouching;
	

    private Vector3 cameraEffectsStartLocalPosition;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();

        if (cameraEffectsRoot != null)
        {
            cameraEffectsStartLocalPosition = cameraEffectsRoot.localPosition;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
{
    bool canMove = RedshiftPlayerStateController.Instance == null ||
                   RedshiftPlayerStateController.Instance.CanMove;

    bool canLook = RedshiftPlayerStateController.Instance == null ||
                   RedshiftPlayerStateController.Instance.CanLook;

    if (canMove)
    {
        HandleCrouch();
        HandleMovement();
    }

    if (canLook)
    {
        HandleLook();
    }

    if (canMove)
    {
        HandleHeadBob();
    }
}

    private void HandleMovement()
    {
        Vector2 movementInput = GetMovementInput();
        bool hasMovementInput = movementInput.sqrMagnitude > 0.01f;

        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;
        float targetSpeed = isSprinting ? sprintSpeed : walkSpeed;

        if (isCrouching)
        {
            targetSpeed *= crouchSpeedMultiplier;
        }

        Vector3 wishDirection =
            transform.right * movementInput.x +
            transform.forward * movementInput.y;

        wishDirection = Vector3.ClampMagnitude(wishDirection, 1f);

        Vector3 targetHorizontalVelocity = wishDirection * targetSpeed;

        float accelerationRate;

        if (!characterController.isGrounded)
            accelerationRate = airAcceleration;
        else if (hasMovementInput)
            accelerationRate = groundAcceleration;
        else
            accelerationRate = groundDeceleration;

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetHorizontalVelocity,
            accelerationRate * Time.deltaTime
        );

        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 finalVelocity = horizontalVelocity;
        finalVelocity.y = verticalVelocity;

        characterController.Move(finalVelocity * Time.deltaTime);
    }

    private Vector2 GetMovementInput()
    {
        Vector2 movementInput = Vector2.zero;

        if (Keyboard.current.wKey.isPressed) movementInput.y += 1f;
        if (Keyboard.current.sKey.isPressed) movementInput.y -= 1f;
        if (Keyboard.current.dKey.isPressed) movementInput.x += 1f;
        if (Keyboard.current.aKey.isPressed) movementInput.x -= 1f;

        return Vector2.ClampMagnitude(movementInput, 1f);
    }

    private void HandleLook()
    {
        if (Mouse.current == null || cameraPitchRoot == null)
        {
            return;
        }

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        transform.Rotate(Vector3.up * mouseX);

        cameraPitch -= mouseY;
        cameraPitch = Mathf.Clamp(cameraPitch, minLookAngle, maxLookAngle);

        cameraPitchRoot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);

        HandleCameraSway(mouseX);
    }

    private void HandleCameraSway(float mouseX)
    {
        if (cameraEffectsRoot == null)
        {
            return;
        }

        float targetRoll = 0f;

        if (useCameraSway)
        {
            Vector2 movementInput = GetMovementInput();

            float strafeRoll = -movementInput.x * strafeRollAmount;
            float turnRoll = -mouseX * movementRollAmount;

            targetRoll = strafeRoll + turnRoll;
        }

        currentCameraRoll = Mathf.Lerp(
            currentCameraRoll,
            targetRoll,
            Time.deltaTime * swaySmoothSpeed
        );

        cameraEffectsRoot.localRotation = Quaternion.Euler(0f, 0f, currentCameraRoll);
    }

    private void HandleHeadBob()
    {
        if (!useHeadBob || cameraEffectsRoot == null)
        {
            return;
        }

        bool isMoving = horizontalVelocity.magnitude > 0.1f;
        bool isGrounded = characterController.isGrounded;
        bool isSprinting = Keyboard.current.leftShiftKey.isPressed;

        if (isMoving && isGrounded)
        {
            float bobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;

            headBobTimer += Time.deltaTime * bobSpeed;

            float bobY = Mathf.Sin(headBobTimer) * bobAmount;
            float bobX = Mathf.Cos(headBobTimer * 0.5f) * bobAmount * 0.5f;

            Vector3 targetPosition =
                cameraEffectsStartLocalPosition + new Vector3(bobX, bobY, 0f);

            cameraEffectsRoot.localPosition = Vector3.Lerp(
                cameraEffectsRoot.localPosition,
                targetPosition,
                Time.deltaTime * bobReturnSpeed
            );
        }
        else
        {
            headBobTimer = 0f;

            cameraEffectsRoot.localPosition = Vector3.Lerp(
                cameraEffectsRoot.localPosition,
                cameraEffectsStartLocalPosition,
                Time.deltaTime * bobReturnSpeed
            );
        }
    }

    private void HandleCrouch()
    {
        if (!canCrouch || Keyboard.current == null)
        {
            return;
        }

        isCrouching = Keyboard.current.cKey.isPressed;

        float targetHeight = isCrouching ? crouchingHeight : standingHeight;

        characterController.height = Mathf.Lerp(
            characterController.height,
            targetHeight,
            Time.deltaTime * crouchTransitionSpeed
        );

        characterController.center = new Vector3(
            0f,
            characterController.height * 0.5f,
            0f
        );

        if (cameraPitchRoot != null)
        {
            Vector3 localPosition = cameraPitchRoot.localPosition;

            float targetCameraHeight =
                isCrouching ? crouchingCameraHeight : standingCameraHeight;

            localPosition.y = Mathf.Lerp(
                localPosition.y,
                targetCameraHeight,
                Time.deltaTime * crouchTransitionSpeed
            );

            cameraPitchRoot.localPosition = localPosition;
        }
    }
	
	
	
}