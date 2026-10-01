using UnityEngine;

/// <summary>
/// FirstPersonCamera: Camera gan vao mat nhan vat (First Person View).
/// - Keo ngon tay trai/phai tren nua PHAI man hinh -> xoay ngang + doc
/// - Mouse: di chuot de xoay (editor/PC)
/// - Camera child cua EyePoint (khong can CameraRig rieng)
///
/// Attach vao: Main Camera GameObject
/// Dat Camera la child cua EyePoint tren Player
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────
    [Header("Sensitivity")]
    [SerializeField] private float touchSensitivityX = 180f;  // xoay ngang (mobile)
    [SerializeField] private float touchSensitivityY = 130f;  // xoay doc   (mobile)
    [SerializeField] private float mouseSensitivityX = 120f;  // (editor)
    [SerializeField] private float mouseSensitivityY = 90f;

    [Header("Vertical Clamp")]
    [SerializeField] private float minPitch = -80f; // nhin xuong toi da
    [SerializeField] private float maxPitch =  80f; // nhin len toi da

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.04f;

    [Header("References")]
    [Tooltip("Player body (Capsule). Camera se xoay ngang bang cach xoay body.")]
    [SerializeField] private Transform playerBody;

    // ─────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────
    private float pitch;          // goc nhin len/xuong (X-axis cua camera)
    private float yawVel;
    private float pitchVel;
    private float targetPitch;
    private float targetYaw;      // yaw dua vao body
    private float currentPitch;
    private float currentYawDelta; // chi luu delta yaw (body tu xoay)

    private int lookFingerId = -1;
    private Vector2 lastTouchPos;

    // Public de PlayerController lay huong camera
    public float CameraYaw => playerBody != null ? playerBody.eulerAngles.y : transform.eulerAngles.y;

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Awake()
    {
        // Neu chua gan playerBody, tu tim Player
        if (playerBody == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerBody = player.transform;
        }

        currentPitch = transform.localEulerAngles.x;
        // Chuyen tu 0-360 sang -180 den 180
        if (currentPitch > 180f) currentPitch -= 360f;
        targetPitch = currentPitch;

        // An cursor o editor
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        bool isPlaying = GameManager.Instance == null || GameManager.Instance.IsPlaying;

        if (!isPlaying || Time.timeScale == 0f || AccessCodeUI.IsOpen || TerminalUI.IsOpen)
        {
            // UI dang mo hoac dang pause hoac Game Over / Win: luon giu cursor hien de bam nut UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        // ESC = mo cursor (de bam UI), click lai vao game = khoa cursor va tiep tuc choi
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            // Khong lock lai chuot neu nguoi choi dang click len bat ky nut UI nao
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
#endif

        HandleInput();
        ApplyRotation();
    }

    // ─────────────────────────────────────────────
    // Input
    // ─────────────────────────────────────────────
    private void HandleInput()
    {
        float deltaYaw   = 0f;
        float deltaPitch = 0f;

        // ── Mobile: ngon tay nua PHAI man hinh ──
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began
                && touch.position.x > Screen.width * 0.45f
                && lookFingerId == -1)
            {
                lookFingerId = touch.fingerId;
                lastTouchPos = touch.position;
            }

            if (touch.fingerId == lookFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 delta = touch.deltaPosition;
                    deltaYaw   += delta.x * touchSensitivityX * Time.deltaTime * 0.05f;
                    deltaPitch -= delta.y * touchSensitivityY * Time.deltaTime * 0.05f;
                    lastTouchPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    lookFingerId = -1;
                }
            }
        }

        // ── Editor / PC: Mouse ──
#if UNITY_EDITOR
        deltaYaw   += Input.GetAxis("Mouse X") * mouseSensitivityX * Time.deltaTime;
        deltaPitch -= Input.GetAxis("Mouse Y") * mouseSensitivityY * Time.deltaTime;
#endif

        targetYaw     = playerBody != null ? playerBody.eulerAngles.y + deltaYaw : deltaYaw;
        targetPitch   = Mathf.Clamp(targetPitch + deltaPitch, minPitch, maxPitch);

        // Xoay THAN player theo huong ngang
        if (playerBody != null && Mathf.Abs(deltaYaw) > 0.001f)
        {
            playerBody.Rotate(Vector3.up, deltaYaw, Space.World);
        }
    }

    // ─────────────────────────────────────────────
    // Apply
    // ─────────────────────────────────────────────
    private void ApplyRotation()
    {
        // Smooth pitch
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVel, smoothTime);

        // Camera chi xoay doc (pitch), ngang do playerBody lo
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    // ─────────────────────────────────────────────
    // Public
    // ─────────────────────────────────────────────
    public void SetPlayerBody(Transform body) => playerBody = body;
}
