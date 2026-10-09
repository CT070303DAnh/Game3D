using UnityEngine;

/// <summary>
/// FirstPersonCamera: Camera gan vao mat nhan vat (First Person View).
/// - Di chuot de xoay camera va than nguoi choi (PC / Editor)
/// - Keo ngon tay nua PHAI man hinh de xoay (Mobile)
/// - Dat Camera la child cua EyePoint tren Player
/// - Phim [V]: Chuyen doi nhanh giua goc nhin thu nhat va thu ba
/// </summary>
public class FirstPersonCamera : MonoBehaviour
{
    [Header("Sensitivity")]
    [SerializeField] private float mouseSensitivityX = 140f;
    [SerializeField] private float mouseSensitivityY = 110f;
    [SerializeField] private float touchSensitivityX = 180f;
    [SerializeField] private float touchSensitivityY = 130f;

    [Header("Vertical Clamp")]
    [SerializeField] private float minPitch = -80f; // nhin xuong toi da
    [SerializeField] private float maxPitch =  80f; // nhin len toi da

    [Header("Smoothing")]
    [SerializeField] private float smoothTime = 0.03f;

    [Header("References")]
    [SerializeField] private Transform playerBody;

    private float pitch;
    private float pitchVel;
    private float targetPitch;
    private float targetYaw;
    private float currentPitch;

    private int lookFingerId = -1;
    private Vector2 lastTouchPos;
    private Renderer[] bodyRenderers;

    public float CameraYaw => playerBody != null ? playerBody.eulerAngles.y : transform.eulerAngles.y;

    private void Awake()
    {
        if (playerBody == null)
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) player = GameObject.Find("Player");
            if (player != null) playerBody = player.transform;
        }

        currentPitch = transform.localEulerAngles.x;
        if (currentPitch > 180f) currentPitch -= 360f;
        targetPitch = currentPitch;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible   = false;
    }

    private void OnEnable()
    {
        // An mesh than player de tranh che khuat tam nhin camera FPS
        if (playerBody != null)
        {
            bodyRenderers = playerBody.GetComponentsInChildren<Renderer>(true);
            foreach (var r in bodyRenderers)
            {
                if (r != null && r.gameObject == playerBody.gameObject)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }
    }

    private void OnDisable()
    {
        // Hien lai mesh khi tat FPS
        if (bodyRenderers != null)
        {
            foreach (var r in bodyRenderers)
            {
                if (r != null && r.gameObject == playerBody.gameObject)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.enabled = true;
                }
            }
        }
    }

    private void LateUpdate()
    {
        bool isPlaying = GameManager.Instance == null || GameManager.Instance.IsPlaying;

        if (!isPlaying || Time.timeScale == 0f || AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen || InventoryUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            return;
        }

#if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible   = true;
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            if (InventoryUI.IsOpen) return;

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

    private void HandleInput()
    {
        float deltaYaw   = 0f;
        float deltaPitch = 0f;

        // ── Mobile ──
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

        // ── PC / Editor Mouse ──
#if UNITY_EDITOR || UNITY_STANDALONE
        if (Cursor.lockState == CursorLockMode.Locked)
        {
            float mx = Input.GetAxis("Mouse X");
            float my = Input.GetAxis("Mouse Y");
            deltaYaw   += mx * mouseSensitivityX * Time.deltaTime;
            deltaPitch -= my * mouseSensitivityY * Time.deltaTime;
        }
#endif

        targetPitch = Mathf.Clamp(targetPitch + deltaPitch, minPitch, maxPitch);

        // Xoay than player theo phuong ngang (Yaw)
        if (playerBody != null && Mathf.Abs(deltaYaw) > 0.0001f)
        {
            playerBody.Rotate(Vector3.up, deltaYaw, Space.World);
        }
    }

    private void ApplyRotation()
    {
        currentPitch = Mathf.SmoothDamp(currentPitch, targetPitch, ref pitchVel, smoothTime);
        transform.localRotation = Quaternion.Euler(currentPitch, 0f, 0f);
    }

    public void SetPlayerBody(Transform body)
    {
        playerBody = body;
        if (playerBody != null)
        {
            bodyRenderers = playerBody.GetComponentsInChildren<Renderer>(true);
            foreach (var r in bodyRenderers)
            {
                if (r != null && r.gameObject == playerBody.gameObject)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }
    }
}
