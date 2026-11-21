using UnityEngine;
using UnityEngine.InputSystem;

public class fpsMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Jump and Fall")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -10f;
    [SerializeField] private float initialFallVelocity = -2f;

    [Header("Crouching")]
    [SerializeField] private float standingHeight = 2f;
    [SerializeField] private float crouchingHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 10f;
    [SerializeField] private float cameraOffset = 0.4f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference crouchAction;
    [SerializeField] private InputActionReference sprintAction;

    private CharacterController characterController;
    private Vector2 moveInput;
    private bool isGrounded;
    private bool isRunning;
    private bool isCrouching;
    private float VerticalVelocity;
    private float targetHeight;


    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        targetHeight = standingHeight;
    }

    private void OnEnable()
    {
        moveAction.action.performed += storeMovementInput;
        moveAction.action.canceled += storeMovementInput;
        jumpAction.action.performed += jump;
        sprintAction.action.performed += sprint;
        sprintAction.action.canceled += sprint;
        crouchAction.action.performed += crouch;
    }

    private void OnDisable()
    {
        moveAction.action.performed -= storeMovementInput;
        moveAction.action.canceled -= storeMovementInput;
        jumpAction.action.performed -= jump;
        sprintAction.action.performed -= sprint;
        sprintAction.action.canceled -= sprint;
        crouchAction.action.performed -= crouch;
    }


    private void Update()
    {
        handleMovement();
        handleGravity();
        isGrounded = characterController.isGrounded;
        handleCrouchTransition();
    }


    private void storeMovementInput(InputAction.CallbackContext context)
    {
        moveInput = context.ReadValue<Vector2>();
    }

    private void jump(InputAction.CallbackContext context)
    {
        if (isGrounded)
        {
            VerticalVelocity = jumpForce;
        }
    }

    private void sprint(InputAction.CallbackContext context)
    {
        isRunning = context.performed;
    }

    private void crouch(InputAction.CallbackContext context)
    {
        if (isCrouching)
        {
            if (!canStandUp())
            {
                return;
            }
            targetHeight = standingHeight;
        }
        else
        {
            targetHeight = crouchingHeight;
        }
        isCrouching = !isCrouching;
    }

    private bool canStandUp()
    {
        return !Physics.CapsuleCast(
            transform.position + characterController.center,
            transform.position + (Vector3.up * characterController.height / 2),
            characterController.radius, Vector3.up
            );
    }

    private void handleGravity()
    {
        if (isGrounded && VerticalVelocity < 0)
        {
            VerticalVelocity = initialFallVelocity;
        }

        VerticalVelocity += gravity * Time.deltaTime;
    }

    private void handleMovement()
    {
        var move = cameraTransform.TransformDirection(new Vector3(moveInput.x, 0, moveInput.y)).normalized;
        var currentSpeed = isCrouching ? crouchSpeed : isRunning ? runSpeed : walkSpeed;
        var finalMove = move * currentSpeed;
        finalMove.y = VerticalVelocity;

        var collisions = characterController.Move(finalMove * Time.deltaTime);
        if ((collisions & CollisionFlags.Above) != 0)
        {
            VerticalVelocity = initialFallVelocity;
            // Can switch the initialFallVelocaity to a negative value if we don't like the feel... just don't 0.
        }
    }

    private void handleCrouchTransition()
    {
        var currentHeight = characterController.height;
        if (Mathf.Abs(currentHeight - targetHeight) < 0.01f)
        {
            characterController.height = targetHeight;
            // Return. Good height.
        }

        var newHeight = Mathf.Lerp(currentHeight, targetHeight, crouchTransitionSpeed * Time.deltaTime);
        characterController.height = newHeight;
        characterController.center = Vector3.up * (newHeight * 0.5f);

        var cameraTargetPosition = cameraTransform.localPosition;
        cameraTargetPosition.y = targetHeight - cameraOffset;
        cameraTransform.localPosition = Vector3.Lerp(
            cameraTransform.localPosition,
            cameraTargetPosition,
            crouchTransitionSpeed * Time.deltaTime);
    }
}
