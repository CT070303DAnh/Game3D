using UnityEngine;

/// <summary>
/// PlayerController: Xu ly di chuyen Player Third Person.
/// Su dung CharacterController (on dinh, khong FPS-dependent).
/// Doc input tu IMovementInput (khong phu thuoc truc tiep vao UI).
///
/// Attach vao: Player GameObject
/// Required Components: CharacterController, Animator
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ───────────────────────────────────────────────
    // Inspector Settings
    // ───────────────────────────────────────────────
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float runSpeed = 5.5f;
    [SerializeField] private float jumpHeight = 1.2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float rotationSmoothTime = 0.1f;

    [Header("Ground Check")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask groundMask;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;

    // ───────────────────────────────────────────────
    // Components
    // ───────────────────────────────────────────────
    private CharacterController characterController;
    private Animator animator;
    private IMovementInput input;

    // ───────────────────────────────────────────────
    // Runtime State
    // ───────────────────────────────────────────────
    private Vector3 velocity;           // Y = gravity accumulation
    private bool isGrounded;
    private float rotationVelocity;     // SmoothDamp ref
    private float currentSpeed;
    private bool isFPS;                 // cache: dang dung FPS camera?

    // ───────────────────────────────────────────────
    // Animator Parameter Hashes (hieu nang hon string)
    // ───────────────────────────────────────────────
    private static readonly int HashSpeed = Animator.StringToHash("Speed");
    private static readonly int HashIsGrounded = Animator.StringToHash("IsGrounded");
    private static readonly int HashJump = Animator.StringToHash("Jump");

    // Flag: chi goi Animator khi da co controller
    private bool hasAnimatorController;

    // ───────────────────────────────────────────────
    // Unity Lifecycle
    // ───────────────────────────────────────────────
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        // Kiem tra co AnimatorController chua de tranh warning spam
        hasAnimatorController = animator.runtimeAnimatorController != null;
    }

    // Cache camera mode
    private PubgCamera pubgCam;

    private void Start()
    {
        input = FindFirstObjectByType<MobileInputController>();
        if (input == null)
            Debug.LogWarning("[PlayerController] No IMovementInput found. Player wont move.");

        // Auto-detect camera mode
        isFPS    = FindFirstObjectByType<FirstPersonCamera>() != null;
        pubgCam  = FindFirstObjectByType<PubgCamera>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        string mode = isFPS ? "FPS" : (pubgCam != null ? "PUBG TPS" : "Classic TPS");
        Debug.Log("[PlayerController] Camera mode: " + mode);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        HandleGroundCheck();
        HandleGravity();
        HandleMovement();
        HandleJump();
        UpdateAnimator();
    }

    // ───────────────────────────────────────────────
    // Ground Check
    // ───────────────────────────────────────────────
    private void HandleGroundCheck()
    {
        Vector3 checkPos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.1f;

        isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, groundMask);

        // Reset Y velocity khi cham dat
        if (isGrounded && velocity.y < -2f)
            velocity.y = -2f;
    }

    // ───────────────────────────────────────────────
    // Gravity
    // ───────────────────────────────────────────────
    private void HandleGravity()
    {
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // ───────────────────────────────────────────────
    // Movement
    // ───────────────────────────────────────────────
    private void HandleMovement()
    {
        if (input == null) return;

        Vector2 moveInput = input.Move;
        bool isRunning = input.RunPressed;

        if (moveInput.magnitude < 0.1f)
        {
            currentSpeed = 0f;
            return;
        }

        // Lay yaw cua camera (huong camera dang nhin)
        float camYaw = 0f;
        if (pubgCam != null)
            camYaw = pubgCam.CameraYaw;          // PUBG: lay tu PubgCamera
        else if (cameraTransform != null)
            camYaw = cameraTransform.eulerAngles.y; // FPS / Classic TPS

        // Huong di chuyen = input + camera yaw
        float targetAngle = Mathf.Atan2(moveInput.x, moveInput.y) * Mathf.Rad2Deg + camYaw;
        Vector3 moveDir   = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;

        if (isFPS)
        {
            // FPS: than player xoay cung camera (FirstPersonCamera lo viec nay)
        }
        else
        {
            // PUBG / Classic TPS:
            // Nhan vat xoay mat ve HUONG DI CHUYEN, KHONG xoay theo camera
            float smoothAngle = Mathf.SmoothDampAngle(
                transform.eulerAngles.y, targetAngle,
                ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, smoothAngle, 0f);
        }

        // Di chuyen
        currentSpeed = isRunning ? runSpeed : walkSpeed;
        characterController.Move(moveDir.normalized * currentSpeed * Time.deltaTime);
    }

    // ───────────────────────────────────────────────
    // Jump
    // ───────────────────────────────────────────────
    private void HandleJump()
    {
        if (input == null) return;

        if (input.JumpPressed && isGrounded)
        {
            // v = sqrt(2 * g * h)  — vat ly chinh xac
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (hasAnimatorController) animator.SetTrigger(HashJump);
        }
    }

    // ───────────────────────────────────────────────
    // Animator
    // ───────────────────────────────────────────────
    private void UpdateAnimator()
    {
        // Neu chua co AnimatorController, bo qua (tranh warning)
        if (!hasAnimatorController) return;

        // Normalize speed 0..1 cho Blend Tree
        float normalizedSpeed = currentSpeed / runSpeed;
        animator.SetFloat(HashSpeed, normalizedSpeed, 0.1f, Time.deltaTime);
        animator.SetBool(HashIsGrounded, isGrounded);
    }

    // ───────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────
    public bool IsGrounded => isGrounded;
    public float CurrentSpeed => currentSpeed;

    // ───────────────────────────────────────────────
    // Debug Gizmo
    // ───────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Vector3 checkPos = groundCheck != null
            ? groundCheck.position
            : transform.position + Vector3.down * 0.1f;
        Gizmos.DrawWireSphere(checkPos, groundCheckRadius);
    }
}
