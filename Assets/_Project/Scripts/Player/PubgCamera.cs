using UnityEngine;

/// <summary>
/// PubgCamera: Camera Third Person kieu PUBG.
/// - Nua PHAI man hinh (touch) hoac mouse drag -> xoay camera quanh nhan vat
/// - Camera KHONG xoay khi nhan vat di chuyen
/// - Nhan vat xoay mat ve huong di chuyen (doc tu joystick)
/// - Camera tranh xuyen tuong (collision)
///
/// Attach vao: CameraRig GameObject
/// Main Camera la child cua CameraRig
/// </summary>
public class PubgCamera : MonoBehaviour
{
    // ─────────────────────────────────────────────
    // Inspector
    // ─────────────────────────────────────────────
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private Vector3 shoulderOffset = new Vector3(0.5f, 1.5f, 0f); // lech vai phai

    [Header("Distance")]
    [SerializeField] private float distance     = 3.5f;
    [SerializeField] private float minDistance  = 1.5f;
    [SerializeField] private float maxDistance  = 6f;
    [SerializeField] private float zoomSpeed    = 3f;

    [Header("Rotation Sensitivity")]
    [SerializeField] private float touchSensX = 200f;
    [SerializeField] private float touchSensY = 150f;
    [SerializeField] private float mouseSensX = 160f;
    [SerializeField] private float mouseSensY = 120f;

    [Header("Pitch Clamp")]
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch =  60f;

    [Header("Smoothing")]
    [SerializeField] private float followSmooth   = 0.07f;
    [SerializeField] private float rotationSmooth = 0.04f;

    [Header("Collision")]
    [SerializeField] private float collisionPadding = 0.25f;
    [SerializeField] private LayerMask collisionMask;

    // ─────────────────────────────────────────────
    // Runtime
    // ─────────────────────────────────────────────
    private float yaw;
    private float pitch = 15f;
    private float targetYaw;
    private float targetPitch;
    private float yawVel;
    private float pitchVel;
    private Vector3 followVel;

    private int   lookFingerId = -1;
    private Vector2 lastTouchPos;

    /// <summary>Goc yaw hien tai — PlayerController doc de tinh huong di chuyen.</summary>
    public float CameraYaw => yaw;

    // ─────────────────────────────────────────────
    // Unity Lifecycle
    // ─────────────────────────────────────────────
    private void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        yaw = targetYaw = transform.eulerAngles.y;
        pitch = targetPitch = 15f;

        // Lock cursor khi bat dau (PC)
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        bool isPlaying = GameManager.Instance == null || GameManager.Instance.IsPlaying;
        if (!isPlaying || Time.timeScale == 0f || AccessCodeUI.IsOpen || TerminalUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

        HandleCursorToggle();
        HandleInput();
        ApplyCamera();
    }

    // ─────────────────────────────────────────────
    // Cursor (PC/Editor)
    // ─────────────────────────────────────────────
    private void HandleCursorToggle()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            if (UnityEngine.EventSystems.EventSystem.current != null && 
                UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible   = false;
        }
#endif
    }

    // ─────────────────────────────────────────────
    // Input
    // ─────────────────────────────────────────────
    private void HandleInput()
    {
        float dYaw   = 0f;
        float dPitch = 0f;

        // ── Mobile: keo nua PHAI man hinh ──
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
                    dYaw   += delta.x * touchSensX * Time.deltaTime * 0.05f;
                    dPitch -= delta.y * touchSensY * Time.deltaTime * 0.05f;
                    lastTouchPos = touch.position;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    lookFingerId = -1;
            }
        }

        // ── PC / Editor: mouse (luon xoay khi cursor locked) ──
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            dYaw   += Input.GetAxis("Mouse X") * mouseSensX * Time.deltaTime;
            dPitch -= Input.GetAxis("Mouse Y") * mouseSensY * Time.deltaTime;
        }
        // Scroll zoom
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        distance = Mathf.Clamp(distance - scroll * zoomSpeed, minDistance, maxDistance);
#endif

        targetYaw   += dYaw;
        targetPitch  = Mathf.Clamp(targetPitch + dPitch, minPitch, maxPitch);
    }

    // ─────────────────────────────────────────────
    // Apply Camera Transform
    // ─────────────────────────────────────────────
    private void ApplyCamera()
    {
        // Smooth rotation
        yaw   = Mathf.SmoothDampAngle(yaw,   targetYaw,   ref yawVel,   rotationSmooth);
        pitch = Mathf.SmoothDamp      (pitch, targetPitch, ref pitchVel, rotationSmooth);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);

        // Pivot = vai phai nhan vat (PUBG shoulder cam)
        Vector3 pivot = target.position + target.TransformDirection(shoulderOffset);

        // Vi tri mong muon
        Vector3 desired = pivot - rot * Vector3.forward * distance;

        // Collision check — keo camera lai neu co tuong chan
        float actualDist = CollisionCheck(pivot, desired);
        Vector3 finalPos = pivot - rot * Vector3.forward * actualDist;

        // Smooth follow position
        transform.position = Vector3.SmoothDamp(transform.position, finalPos, ref followVel, followSmooth);
        transform.rotation = rot;
    }

    private float CollisionCheck(Vector3 from, Vector3 to)
    {
        Vector3 dir = to - from;
        if (Physics.SphereCast(from, 0.15f, dir.normalized, out RaycastHit hit, dir.magnitude, collisionMask))
            return Mathf.Max(hit.distance - collisionPadding, minDistance);
        return distance;
    }

    // ─────────────────────────────────────────────
    // Public API
    // ─────────────────────────────────────────────
    public void SetTarget(Transform t) => target = t;
}
