using UnityEngine;

/// <summary>
/// ThirdPersonCamera: Camera follow va rotate quanh Player.
/// - Nut cam tay phai man hinh: xoay camera
/// - Scroll (Editor): zoom
/// - Khong cho camera di xuyen tuong (collision)
///
/// Attach vao: CameraRig GameObject (parent cua Main Camera)
/// </summary>
public class ThirdPersonCamera : MonoBehaviour
{
    // ───────────────────────────────────────────────
    // Inspector
    // ───────────────────────────────────────────────
    [Header("Target")]
    [SerializeField] private Transform target;          // Player transform
    [SerializeField] private Vector3 offset = new Vector3(0f, 1.8f, 0f); // Diem nhin tren dau player

    [Header("Distance")]
    [SerializeField] private float distance = 5f;
    [SerializeField] private float minDistance = 2f;
    [SerializeField] private float maxDistance = 8f;
    [SerializeField] private float zoomSpeed = 2f;

    [Header("Rotation")]
    [SerializeField] private float rotationSpeedX = 200f;  // Ngang (Y-axis)
    [SerializeField] private float rotationSpeedY = 150f;  // Doc (X-axis)
    [SerializeField] private float minPitch = -20f;        // Goc nhin xuong toi da
    [SerializeField] private float maxPitch = 60f;         // Goc nhin len toi da

    [Header("Smoothing")]
    [SerializeField] private float followSmoothTime = 0.08f;
    [SerializeField] private float rotationSmoothTime = 0.05f;

    [Header("Collision")]
    [SerializeField] private float collisionOffset = 0.3f;
    [SerializeField] private LayerMask collisionMask;

    // ───────────────────────────────────────────────
    // Runtime State
    // ───────────────────────────────────────────────
    private float yaw;          // Goc quay ngang
    private float pitch;        // Goc quay doc
    private Vector3 followVelocity;
    private float currentYaw;
    private float currentPitch;
    private float yawVelocity;
    private float pitchVelocity;

    // Touch cam tay
    private int cameraFingerId = -1;
    private Vector2 lastTouchPos;

    // ───────────────────────────────────────────────
    // Unity Lifecycle
    // ───────────────────────────────────────────────
    private void Start()
    {
        if (target == null)
        {
            // Tu tim Player neu chua gán
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            else Debug.LogError("[ThirdPersonCamera] Target not set and no Player tag found!");
        }

        // Khoi tao goc tu rotation hien tai
        yaw = transform.eulerAngles.y;
        pitch = 20f; // Goc nhin mac dinh nhin xuong
        currentYaw = yaw;
        currentPitch = pitch;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        HandleInput();
        ApplyCameraTransform();
    }

    // ───────────────────────────────────────────────
    // Input
    // ───────────────────────────────────────────────
    private void HandleInput()
    {
        // ── Mobile touch (nua phai man hinh) ──
        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began
                && touch.position.x > Screen.width * 0.5f
                && cameraFingerId == -1)
            {
                cameraFingerId = touch.fingerId;
                lastTouchPos = touch.position;
            }

            if (touch.fingerId == cameraFingerId)
            {
                if (touch.phase == TouchPhase.Moved)
                {
                    Vector2 delta = touch.position - lastTouchPos;
                    yaw += delta.x * rotationSpeedX * Time.deltaTime * 0.05f;
                    pitch -= delta.y * rotationSpeedY * Time.deltaTime * 0.05f;
                    pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
                    lastTouchPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    cameraFingerId = -1;
                }
            }
        }

        // ── Editor / PC: Mouse drag ──
#if UNITY_EDITOR
        if (Input.GetMouseButton(1)) // Giu chuot phai de xoay
        {
            yaw += Input.GetAxis("Mouse X") * rotationSpeedX * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotationSpeedY * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        // Scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
#endif
    }

    // ───────────────────────────────────────────────
    // Camera Transform
    // ───────────────────────────────────────────────
    private void ApplyCameraTransform()
    {
        // Smooth rotation
        currentYaw = Mathf.SmoothDampAngle(currentYaw, yaw, ref yawVelocity, rotationSmoothTime);
        currentPitch = Mathf.SmoothDamp(currentPitch, pitch, ref pitchVelocity, rotationSmoothTime);

        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

        // Diem nhin (sau lung player)
        Vector3 pivotPoint = target.position + offset;

        // Vi tri camera truoc collision check
        Vector3 desiredPos = pivotPoint - rotation * Vector3.forward * distance;

        // Collision check: neu tuong chắn, keo camera lai gan hon
        float actualDistance = CheckCameraCollision(pivotPoint, desiredPos);
        Vector3 finalPos = pivotPoint - rotation * Vector3.forward * actualDistance;

        // Smooth follow
        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref followVelocity, followSmoothTime);
        transform.rotation = rotation;
    }

    private float CheckCameraCollision(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        float maxDist = dir.magnitude;

        if (Physics.Raycast(from, dir.normalized, out RaycastHit hit, maxDist, collisionMask))
        {
            return hit.distance - collisionOffset;
        }
        return distance;
    }

    // ───────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────

    /// <summary>Dat target moi (vi du khi respawn).</summary>
    public void SetTarget(Transform newTarget) => target = newTarget;

    /// <summary>Goc quay hien tai de PlayerController tinh huong di chuyen.</summary>
    public float CameraYaw => currentYaw;
}
