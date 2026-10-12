using UnityEngine;

/// <summary>
/// Terminal: Màn hình máy tính an ninh (Security Terminal) Màn 1.
/// - Yêu cầu:
///   1. Phải có điện (Đã mở cầu dao / bật máy phát điện).
///   2. Phải có 'Giấy Ghi Chú Mật Mã' (Access Code Note) thì mới xem được dữ liệu mã màu.
/// - Khi xem: Mở giao diện Terminal hiển thị thứ tự màu sắc (BLUE ➔ RED ➔ GREEN).
/// - Đánh dấu trạng thái SecurityTerminalViewed để người chơi được phép giải mã tại Terminal Cửa Thoát Hiểm.
/// </summary>
public class Terminal : MonoBehaviour, IInteractable
{
    [Header("Terminal Settings")]
    [SerializeField] private string terminalTitle = "SECURITY TERMINAL";
    [SerializeField, TextArea(3, 8)] private string clueText = "";
    [SerializeField] private bool requiresPower = true;
    [SerializeField] private GameObject screenGlowObject;

    [Header("VFX")]
    [SerializeField] private Light screenLight;
    [SerializeField] private Color onColor = new Color(0f, 0.85f, 1f);
    [SerializeField] private Color offColor = Color.black;

    private bool isPowered;
    private bool isOn;

    public bool IsPowered => !requiresPower || isPowered || (GameState.Instance != null && GameState.Instance.PowerRestored);

    // IInteractable — Luôn cho phép tiếp cận tương tác để có prompt và thông báo rõ ràng
    public bool CanInteract => true;

    public string InteractPromptText
    {
        get
        {
            if (!IsPowered)
                return "Security Terminal [E] (CHƯA CÓ ĐIỆN - CẦN BẬT MÁY PHÁT)";
            bool hasNote = GameState.Instance != null && (GameState.Instance.AccessCodeFound || GameState.Instance.AccessCodeSolved);
            if (!hasNote)
                return "Security Terminal [E] (CẦN ACCESS CODE NOTE)";
            return $"Xem {terminalTitle} [E]";
        }
    }

    private void Awake()
    {
        // Đảm bảo luôn có BoxCollider Trigger kích thước lớn để người chơi dễ dàng tương tác
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(3.2f, 3.0f, 3.2f);
            bc.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
            if (col is BoxCollider bc)
            {
                bc.size = new Vector3(Mathf.Max(bc.size.x, 3.0f), Mathf.Max(bc.size.y, 3.0f), Mathf.Max(bc.size.z, 3.0f));
            }
            else if (col is SphereCollider sc)
            {
                sc.radius = Mathf.Max(sc.radius, 2.0f);
            }
        }
    }

    private void OnEnable()
    {
        GameState.OnPowerRestored += OnPowerRestored;
        CheckPowerState();
    }

    private void OnDisable()
    {
        GameState.OnPowerRestored -= OnPowerRestored;
    }

    private void Start()
    {
        CheckPowerState();
    }

    private void CheckPowerState()
    {
        if (IsPowered) TurnOn();
        else TurnOff();
    }

    public void Interact()
    {
        // 1. Kiểm tra nguồn điện
        if (!IsPowered)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ TERMINAL CHƯA CÓ ĐIỆN! Hãy tìm Cầu Chì (Fuse) và khởi động Máy Phát Điện (Phòng Kho) trước.");
            return;
        }

        // 2. Kiểm tra việc đã nhặt Access Code Note chưa
        bool hasNote = GameState.Instance != null && (GameState.Instance.AccessCodeFound || GameState.Instance.AccessCodeSolved);
        if (!hasNote)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ TRUY CẬP BỊ TỪ CHỐI! Cần tìm 'Giấy Ghi Chú Mật Mã' (Access Code Note) trong phòng để tra cứu dữ liệu an ninh.");
            return;
        }

        // 3. Đã có điện & Đã có Access Code Note -> Ghi nhận và hiển thị Terminal!
        GameState.Instance?.SetSecurityTerminalViewed();
        GameAudioManager.PlayTerminalSuccess(transform.position);

        string defaultClue =
            "XÁC THỰC DỮ LIỆU THÀNH CÔNG TỪ GIẤY GHI CHÚ AN NINH!\n\n" +
            "► MẬT MÃ MÀU MỞ CỬA THOÁT HIỂM:\n" +
            "   THỨ TỰ BẢO MẬT:  BLUE ➔ RED ➔ GREEN\n" +
            "   (Thứ tự màu cần nhập: 1. BLUE  ➔  2. RED  ➔  3. GREEN)\n\n" +
            "► HƯỚNG DẪN TIẾP THEO:\n" +
            "   Hãy di chuyển tới Terminal Cửa Thoát Hiểm ở cuối phòng để nhập chuỗi 3 màu này và mở khóa cửa thoát hiểm!";

        string contentToShow = (!string.IsNullOrWhiteSpace(clueText) && !clueText.Contains("Clue goes here"))
            ? clueText : defaultClue;

        TerminalUI.ShowTerminal(terminalTitle, contentToShow);
        NotificationUI.ShowMessage("✓ ĐÃ ĐỌC MÃ BẢO MẬT TỪ SECURITY TERMINAL! Hãy tới Terminal Cửa Thoát Hiểm để giải mã.");
        Debug.Log($"[Terminal] Player successfully read: {terminalTitle}");
    }

    private void OnPowerRestored()
    {
        isPowered = true;
        TurnOn();
    }

    public void TurnOn()
    {
        isOn = true;
        isPowered = true;
        if (screenLight != null) { screenLight.enabled = true; screenLight.color = onColor; }
        if (screenGlowObject != null) screenGlowObject.SetActive(true);
    }

    public void TurnOff()
    {
        isOn = false;
        if (screenLight != null) { screenLight.enabled = false; }
        if (screenGlowObject != null) screenGlowObject.SetActive(false);
    }
}
