using UnityEngine;
using System.Collections;

/// <summary>
/// HelicopterController: Dieu khien chiec truc thang thoat hiem cuoi cung tai Man 3 (Helipad).
/// - Giai doan 1: Nap nhien lieu phan luc (Jet Fuel).
/// - Giai doan 2: Kiem tra tin hieu Radar & Cong vom san do.
/// - Giai doan 3: Len may bay, no may, canh quat quay toc do cao va cat canh dien anh thoat hiem!
/// </summary>
public class HelicopterController : MonoBehaviour, IInteractable
{
    public static HelicopterController Instance { get; private set; }

    [Header("Helicopter Parts")]
    [SerializeField] private Transform mainRotor;
    [SerializeField] private Transform tailRotor;
    [SerializeField] private Light searchLight;
    [SerializeField] private ParticleSystem downwashDust;

    [Header("Flight Animation")]
    [SerializeField] private float idleRotorSpeed = 180f;
    [SerializeField] private float flightRotorSpeed = 1600f;
    [SerializeField] private float liftOffHeight = 35f;

    [Header("State")]
    [SerializeField] private bool requireFuelAndRadar = true;
    [SerializeField] private bool isFueled = false;
    private bool isEscaping = false;
    private float currentRotorSpeed = 0f;

    public bool CanInteract => !isEscaping;

    public string InteractPromptText
    {
        get
        {
            if (isEscaping) return "Trực thăng đang cất cánh...";
            if (requireFuelAndRadar)
            {
                if (!isFueled)
                {
                    bool hasFuel = GameState.Instance != null && GameState.Instance.JetFuelCollected;
                    return hasFuel ? "Nạp Nhiên Liệu Cho Trực Thăng [E]" : "Trực Thăng (Cần Bình Nhiên Liệu Tại Kho Tiếp Liệu) [E]";
                }
                if (GameState.Instance == null || !GameState.Instance.RadarActivated)
                    return "Trực Thăng (Chưa có tín hiệu Radar dẫn đường từ Tháp Điều Khiển) [E]";
            }
            return "LÊN TRỰC THĂNG & TẨU THOÁT! [E]";
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        var col = GetComponent<Collider>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(5f, 3.5f, 9f);
            bc.isTrigger = true;
        }

        if (!requireFuelAndRadar)
        {
            isFueled = true;
            currentRotorSpeed = idleRotorSpeed;
        }
        else
        {
            isFueled = false;
            currentRotorSpeed = 0f;
        }
    }

    private void Update()
    {
        // Quay canh quat
        if (currentRotorSpeed > 0f)
        {
            if (mainRotor != null)
                mainRotor.Rotate(Vector3.up, currentRotorSpeed * Time.deltaTime, Space.Self);
            if (tailRotor != null)
                tailRotor.Rotate(Vector3.right, currentRotorSpeed * 1.5f * Time.deltaTime, Space.Self);
        }
    }

    public void SetupReferences(Transform mainR, Transform tailR, Light light, ParticleSystem dust)
    {
        mainRotor = mainR;
        tailRotor = tailR;
        searchLight = light;
        downwashDust = dust;
    }

    public void Interact()
    {
        if (isEscaping) return;

        if (requireFuelAndRadar)
        {
            // 1. Kiem tra nhien lieu
            if (!isFueled)
            {
                bool hasFuel = GameState.Instance != null && GameState.Instance.JetFuelCollected;
                if (!hasFuel)
                {
                    NotificationUI.ShowMessage("CẢNH BÁO: Trực thăng chưa có nhiên liệu! Hãy qua KHO TIẾP LIỆU (phía Tây) để lấy Bình Nhiên Liệu.");
                    return;
                }

                // Nap nhien lieu
                isFueled = true;
                currentRotorSpeed = idleRotorSpeed;
                NotificationUI.ShowMessage("✓ ĐÃ TIẾP NHIÊN LIỆU PHẢN LỰC CHO TRỰC THĂNG! Động cơ nổ máy chờ lệnh.");
                ObjectiveManager.Instance?.CompleteObjective("fuel");

                if (GameState.Instance == null || !GameState.Instance.RadarActivated)
                {
                    NotificationUI.ShowMessage("✓ ĐÃ NẠP XĂNG! Tiếp theo: Hãy qua THÁP ĐIỀU KHIỂN (phía Đông) kích hoạt trạm Radar không lưu.");
                }
                return;
            }

            // 2. Kiem tra Radar
            if (GameState.Instance == null || !GameState.Instance.RadarActivated)
            {
                NotificationUI.ShowMessage("CẢNH BÁO: Không lưu chưa mở khóa! Hãy lên THÁP ĐIỀU KHIỂN (phía Đông) để Bật Trạm Radar.");
                return;
            }
        }

        // 3. Đủ điều kiện -> CẤT CÁNH TẨU THOÁT NGAY LẬP TỨC!
        StartCoroutine(EscapeSequenceRoutine());
    }

    private IEnumerator EscapeSequenceRoutine()
    {
        isEscaping = true;

        // Tat dieu khien nhan vat
        var player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.enabled = false;
        }

        // Bat dau gio thoi bui va den chieu
        if (searchLight != null)
        {
            searchLight.enabled = true;
            searchLight.intensity = 3.5f;
        }
        if (downwashDust != null)
        {
            downwashDust.Play();
        }

        NotificationUI.ShowMessage("TRỰC THĂNG ĐANG TĂNG TỐC ĐỘNG CƠ...");

        // Tang toc canh quat tu idle den max speed
        float rampTime = 0f;
        float rampDuration = 2.0f;
        while (rampTime < rampDuration)
        {
            rampTime += Time.deltaTime;
            currentRotorSpeed = Mathf.Lerp(idleRotorSpeed, flightRotorSpeed, rampTime / rampDuration);
            yield return null;
        }
        currentRotorSpeed = flightRotorSpeed;

        NotificationUI.ShowMessage("TRỰC THĂNG ĐANG CẤT CÁNH RỜI KHỎI TẦNG THƯỢNG...");

        // Cat canh bay len troi
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + transform.forward * 60f + Vector3.up * liftOffHeight;
        Quaternion startRot = transform.rotation;
        Quaternion targetRot = startRot * Quaternion.Euler(15f, 25f, -8f);

        // Chuyen camera sang goc quay dien anh theo may bay
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.SetParent(null);
            cam.transform.position = startPos - transform.forward * 12f + Vector3.up * 4f + transform.right * 6f;
            cam.transform.LookAt(transform.position + Vector3.up * 1.5f);
        }

        float flyTime = 0f;
        float flyDuration = 5.0f;
        while (flyTime < flyDuration)
        {
            flyTime += Time.deltaTime;
            float t = flyTime / flyDuration;
            float easeT = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, easeT);
            transform.rotation = Quaternion.Slerp(startRot, targetRot, easeT);

            if (cam != null)
            {
                cam.transform.LookAt(transform.position + Vector3.up * 1.5f);
            }
            yield return null;
        }

        // Hoan thanh nhiem vu thoat hiem
        if (GameState.Instance != null)
        {
            GameState.Instance.SetHelipadEscaped();
        }
        ObjectiveManager.Instance?.CompleteObjective("helipad_escape");

        // Hien thi man hinh CHIEN THANG TOAN BO GAME
        ShowGrandVictoryScreen();
    }

    private void ShowGrandVictoryScreen()
    {
        var winPnl = GameObject.Find("WinPanel");
        if (winPnl != null)
        {
            winPnl.SetActive(true);
            winPnl.transform.SetAsLastSibling();

            var titleTxt = winPnl.transform.Find("WinTitle")?.GetComponent<UnityEngine.UI.Text>();
            if (titleTxt != null)
            {
                titleTxt.text = "🎉 CHIẾN THẮNG TUYỆT ĐỐI! 🎉\nBẠN ĐÃ THOÁT KHỎI KHU NGHIÊN CỨU!";
                titleTxt.fontSize = 28;
                titleTxt.color = Color.yellow;
            }

            var nextBtnText = winPnl.transform.Find("NextBtn/Text")?.GetComponent<UnityEngine.UI.Text>();
            if (nextBtnText != null)
            {
                nextBtnText.text = "[ CHƠI LẠI TỪ MÀN 1 ]";
            }

            var nextBtn = winPnl.transform.Find("NextBtn")?.GetComponent<UnityEngine.UI.Button>();
            if (nextBtn != null)
            {
                nextBtn.onClick.RemoveAllListeners();
                nextBtn.onClick.AddListener(() => {
                    Time.timeScale = 1f;
                    UnityEngine.SceneManagement.SceneManager.LoadScene("Lab");
                });
            }
        }
        else
        {
            NotificationUI.ShowMessage("🎉 CHIẾN THẮNG TUYỆT ĐỐI! BẠN ĐÃ THOÁT KHỎI KHU NGHIÊN CỨU BÍ MẬT! 🎉");
        }
    }
}
