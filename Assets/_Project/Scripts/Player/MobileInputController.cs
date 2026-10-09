using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// MobileInputController: Quan ly input tu virtual joystick va cac nut cam ung.
/// Implement IMovementInput de gameplay code khong phu thuoc truc tiep vao UI.
/// Attach vao: MobileInputController GameObject (ton tai trong gameplay scene).
/// </summary>
public class MobileInputController : MonoBehaviour, IMovementInput
{
    // ───────────────────────────────────────────────
    // Singleton
    // ───────────────────────────────────────────────
    public static MobileInputController Instance { get; private set; }

    // ───────────────────────────────────────────────
    // Joystick References
    // ───────────────────────────────────────────────
    [Header("Joystick")]
    [SerializeField] private RectTransform joystickBg;
    [SerializeField] private RectTransform joystickHandle;
    [SerializeField] private float joystickRadius = 60f;

    // ───────────────────────────────────────────────
    // Button References
    // ───────────────────────────────────────────────
    [Header("Buttons")]
    [SerializeField] private Button interactButton;
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button runButton;

    // ───────────────────────────────────────────────
    // IMovementInput State
    // ───────────────────────────────────────────────
    private Vector2 moveInput = Vector2.zero;
    private bool jumpPressed;
    private bool runHeld;
    private bool interactPressed;

    // Joystick tracking
    private int joystickFingerId = -1;
    private Vector2 joystickStartPos;

    // ───────────────────────────────────────────────
    // IMovementInput Implementation
    // ───────────────────────────────────────────────
    public Vector2 Move => moveInput;
    public bool JumpPressed => jumpPressed;
    public bool RunPressed => runHeld;
    public bool InteractPressed => interactPressed;

    // ───────────────────────────────────────────────
    // Unity Lifecycle
    // ───────────────────────────────────────────────
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        SetupButtons();

        // Dong bo voi Man 2: Khong hien thi nut E, Run, Jump, Joystick ao tren man hinh
        HideOnScreenButtons();
    }

    public void HideOnScreenButtons()
    {
        if (interactButton != null) interactButton.gameObject.SetActive(false);
        if (runButton != null) runButton.gameObject.SetActive(false);
        if (jumpButton != null) jumpButton.gameObject.SetActive(false);
        if (joystickBg != null) joystickBg.gameObject.SetActive(false);
        if (joystickHandle != null) joystickHandle.gameObject.SetActive(false);

        // Chi tat canvas neu no KHONG chua bat ky HUD phan tu nao (tranh lam mat thanh mau/objective)
        Canvas canvas = GetComponent<Canvas>();
        if (canvas != null && canvas.transform.Find("HPPanel") == null)
            canvas.enabled = false;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas != null && parentCanvas.gameObject.name.Contains("Mobile") && parentCanvas.transform.Find("HPPanel") == null)
        {
            parentCanvas.enabled = false;
        }
    }

    private void LateUpdate()
    {
        // Reset one-frame flags sau khi duoc doc
        jumpPressed = false;
        interactPressed = false;
        ProcessJoystickInput();
    }

    // ───────────────────────────────────────────────
    // Button Setup
    // ───────────────────────────────────────────────
    private void SetupButtons()
    {
        // Jump button — one-shot khi nhan
        if (jumpButton != null)
        {
            jumpButton.onClick.AddListener(() => jumpPressed = true);
        }

        // Run button — giu (EventTrigger)
        if (runButton != null)
        {
            AddEventTrigger(runButton.gameObject, EventTriggerType.PointerDown, (_) => runHeld = true);
            AddEventTrigger(runButton.gameObject, EventTriggerType.PointerUp, (_) => runHeld = false);
        }

        // Interact button — one-shot
        if (interactButton != null)
        {
            interactButton.onClick.AddListener(() => interactPressed = true);
        }
    }

    // ───────────────────────────────────────────────
    // Joystick Input
    // ───────────────────────────────────────────────
    private void ProcessJoystickInput()
    {
        // ── Keyboard / PC input (uu tien hon joystick) ──
#if UNITY_EDITOR || UNITY_STANDALONE
        float h = 0f, v = 0f;
        // Dung GetKey thay GetAxis khi cursor bi lock (FPS mode)
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  h -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) h += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))  v -= 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))    v += 1f;

        // Neu khong co phim bam, thu GetAxis (smooth)
        if (h == 0f && v == 0f)
        {
            h = Input.GetAxisRaw("Horizontal");
            v = Input.GetAxisRaw("Vertical");
        }

        bool hasKeyboard = (h != 0f || v != 0f);
        if (hasKeyboard)
        {
            moveInput = new Vector2(h, v).normalized;
        }
        else if (joystickFingerId == -1)
        {
            moveInput = Vector2.zero;
        }

        // Space = jump, Shift = run, E = interact
        if (Input.GetKeyDown(KeyCode.Space)) jumpPressed = true;
        if (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
            runHeld = true;
        else if (!IsRunButtonHeld())
            runHeld = false;
        if (Input.GetKeyDown(KeyCode.E)) interactPressed = true;

        // Neu co keyboard input thi bo qua joystick touch
        if (hasKeyboard) return;
#endif

        // ── Touch joystick ──
        if (Input.touchCount == 0 && joystickFingerId != -1)
        {
            ResetJoystick();
            return;
        }

        foreach (Touch touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began && joystickFingerId == -1)
            {
                if (IsTouchInJoystickArea(touch.position))
                {
                    joystickFingerId = touch.fingerId;
                    joystickStartPos = touch.position;
                }
            }

            if (touch.fingerId == joystickFingerId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    Vector2 delta = touch.position - joystickStartPos;
                    float dist = Mathf.Min(delta.magnitude, joystickRadius);
                    Vector2 direction = delta.normalized;
                    moveInput = direction * (dist / joystickRadius);

                    if (joystickHandle != null)
                        joystickHandle.anchoredPosition = direction * dist;
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    ResetJoystick();
                }
            }
        }
    }

    private bool IsTouchInJoystickArea(Vector2 screenPos)
    {
        if (joystickBg == null) return false;
        // Vung trai man hinh (nua trai)
        return screenPos.x < Screen.width * 0.5f;
    }

    private void ResetJoystick()
    {
        joystickFingerId = -1;
        moveInput = Vector2.zero;
        if (joystickHandle != null)
            joystickHandle.anchoredPosition = Vector2.zero;
    }

    private bool IsRunButtonHeld()
    {
        if (runButton == null) return false;
        // Kiem tra qua EventSystem
        return runHeld;
    }

    // ───────────────────────────────────────────────
    // Utility: Them EventTrigger vao GameObject
    // ───────────────────────────────────────────────
    private void AddEventTrigger(GameObject go, EventTriggerType type,
        UnityEngine.Events.UnityAction<BaseEventData> action)
    {
        EventTrigger trigger = go.GetComponent<EventTrigger>();
        if (trigger == null) trigger = go.AddComponent<EventTrigger>();

        EventTrigger.Entry entry = new EventTrigger.Entry { eventID = type };
        entry.callback.AddListener(action);
        trigger.triggers.Add(entry);
    }
}
