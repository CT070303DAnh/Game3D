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
        hasAnimatorController = animator != null && animator.runtimeAnimatorController != null;

        // Xoa CapsuleCollider thua neu duoc tao tu Primitive Capsule
        var capCol = GetComponent<CapsuleCollider>();
        if (capCol != null)
        {
            Destroy(capCol);
        }

        if (walkSpeed <= 0f) walkSpeed = 3.5f;
        if (runSpeed <= 0f) runSpeed = 5.5f;
        if (gravity >= 0f) gravity = -20f;
    }

    // Cache camera mode
    private PubgCamera pubgCam;

    private void Start()
    {
        input = FindFirstObjectByType<MobileInputController>();
        if (input == null)
        {
            // Tu dong tao MobileInputController de dam bao input luon ton tai
            var micGO = new GameObject("MobileInputController_Auto");
            input = micGO.AddComponent<MobileInputController>();
        }

        // Auto-detect camera mode
        isFPS    = FindFirstObjectByType<FirstPersonCamera>() != null;
        pubgCam  = FindFirstObjectByType<PubgCamera>();

        if (cameraTransform == null && Camera.main != null)
            cameraTransform = Camera.main.transform;

        string mode = isFPS ? "FPS" : (pubgCam != null ? "PUBG TPS" : "Classic TPS");
    }

    private void Update()
    {
        // Khong di chuyen khi game dang tam dung, mo tui do, hoac mo menu pause / game over
        if (Time.timeScale == 0f || InventoryUI.IsOpen) return;
        if (GameManager.Instance != null && 
           (GameManager.Instance.CurrentPhase == GameManager.GamePhase.Paused || 
            GameManager.Instance.CurrentPhase == GameManager.GamePhase.GameOver ||
            GameManager.Instance.CurrentPhase == GameManager.GamePhase.MainMenu))
        {
            return;
        }

        // Bao ve chong roi vao vung void
        if (transform.position.y < -3f)
        {
            if (characterController != null) characterController.enabled = false;
            transform.position = new Vector3(0, 1.2f, -8f);
            velocity = Vector3.zero;
            if (characterController != null) characterController.enabled = true;
            NotificationUI.ShowMessage("ĐÃ ĐƯA BẠN VỀ KHU VỰC AN TOÀN!");
            return;
        }

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
        isGrounded = characterController != null && characterController.isGrounded;

        if (!isGrounded)
        {
            Vector3 checkPos = groundCheck != null
                ? groundCheck.position
                : transform.position + Vector3.down * 0.1f;

            LayerMask mask = groundMask.value != 0 ? groundMask : ~LayerMask.GetMask("Ignore Raycast", "UI");
            isGrounded = Physics.CheckSphere(checkPos, groundCheckRadius, mask);
        }

        // Reset Y velocity khi cham dat
        if (isGrounded && velocity.y < -2f)
            velocity.y = -2f;
    }

    // ───────────────────────────────────────────────
    // Gravity
    // ───────────────────────────────────────────────
    private void HandleGravity()
    {
        if (characterController == null) return;
        velocity.y += gravity * Time.deltaTime;
        velocity.y = Mathf.Max(velocity.y, -30f); // Gioi han toc do roi
        characterController.Move(velocity * Time.deltaTime);
    }

    // ───────────────────────────────────────────────
    // Movement
    // ───────────────────────────────────────────────
    private void HandleMovement()
    {
        if (characterController == null) return;

        Vector2 moveInput = Vector2.zero;
        bool isRunning = false;

        if (input != null)
        {
            moveInput = input.Move;
            isRunning = input.RunPressed;
        }

        // Fallback ban phim PC truc tiep neu joystick = 0
        if (moveInput.sqrMagnitude < 0.01f)
        {
            float h = 0f, v = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v -= 1f;
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v += 1f;
            moveInput = new Vector2(h, v);
            if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
                isRunning = true;
        }

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
        bool jumpReq = (input != null && input.JumpPressed) || Input.GetKeyDown(KeyCode.Space);
        if (jumpReq && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (hasAnimatorController && animator != null)
                animator.SetTrigger(HashJump);
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
    // Collision: Chạm vào Robot bị giật điện
    // ───────────────────────────────────────────────
    private void OnControllerColliderHit(ControllerColliderHit hit)
    {
        if (hit.gameObject.CompareTag("Enemy") || hit.gameObject.name.ToLower().Contains("robot"))
        {
            var robot = hit.gameObject.GetComponentInParent<RobotAI>();
            if (robot != null)
            {
                robot.TriggerElectricShock(transform);
            }
        }
    }

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
