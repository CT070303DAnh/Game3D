using UnityEngine;

/// <summary>
/// ToxicGasManager: Quan ly co che ngat khi doc trong Man 2 (Level2_Reactor).
/// - 3 van dang mo: Tru 1 mau moi 1 giay (-1 HP / 1s)
/// - 2 van dang mo: Tru 1 mau moi 2 giay (-1 HP / 2s)
/// - 1 van dang mo: Tru 1 mau moi 3 giay (-1 HP / 3s)
/// - Dong het (0 van): Khong bi tru mau, khong khi an toan.
/// </summary>
public class ToxicGasManager : MonoBehaviour
{
    public static ToxicGasManager Instance { get; private set; }

    [Header("Gas Settings")]
    [SerializeField] private int totalValves = 3;
    [SerializeField] private int openValves = 3;

    private float timer = 0f;
    private PlayerHealth playerHealth;
    private bool isAllCleared = false;

    public static int OpenValvesCount => Instance != null ? Instance.openValves : 3;
    public static bool IsAirSafe => Instance != null && Instance.openValves <= 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        openValves = totalValves;
        isAllCleared = false;
        timer = 0f;
    }

    private void Start()
    {
        playerHealth = FindFirstObjectByType<PlayerHealth>();

        // Thong bao ngay khi bat dau man choi
        NotificationUI.ShowMessage("CẢNH BÁO: RÒ RỈ KHÍ ĐỘC! Lấy Cờ Lê tại Kho Phụ Tùng (Phía Bắc) để vặn khóa 3 van xả khí!");
    }

    private void Update()
    {
        if (openValves <= 0) return;
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        // Xac dinh khoang thoi gian tru mau dua tren so van dang mo
        float interval;
        if (openValves >= 3)
            interval = 1.0f; // 3 van: -1 mau / 1 giay
        else if (openValves == 2)
            interval = 2.0f; // 2 van: -1 mau / 2 giay
        else
            interval = 3.0f; // 1 van: -1 mau / 3 giay

        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            ApplySuffocationDamage();
        }
    }

    private void ApplySuffocationDamage()
    {
        if (playerHealth == null) playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (playerHealth == null || playerHealth.IsDead) return;

        playerHealth.TakeEnvironmentalDamage(1);

        // Hien thong bao canh bao nhe nhang de nguoi choi biet nguyen nhan mat mau
        string rateStr = openValves >= 3 ? "(-1 HP/s)" : (openValves == 2 ? "(-1 HP/2s)" : "(-1 HP/3s)");
        NotificationUI.ShowMessage($"ĐANG BỊ NGẠT KHÍ ĐỘC! {openValves} van đang xả {rateStr}");
    }

    /// <summary>Goi khi nguoi choi van dong thanh cong 1 van lam mat</summary>
    public static void OnValveClosed()
    {
        if (Instance == null) return;
        Instance.openValves = Mathf.Max(0, Instance.openValves - 1);
        Instance.timer = 0f; // Reset nhip tru mau sang nhip moi

        if (Instance.openValves > 0)
        {
            string newRate = Instance.openValves == 2 ? "-1 HP/2 giây" : "-1 HP/3 giây";
            NotificationUI.ShowMessage($"ĐÃ ĐÓNG VAN KHÍ ({Instance.totalValves - Instance.openValves}/{Instance.totalValves})! Nồng độ giảm: {newRate}.");
        }
        else
        {
            Instance.isAllCleared = true;
            NotificationUI.ShowMessage("✓ ĐÃ ĐÓNG HẾT 3 VAN XẢ KHÍ! KHÔNG KHÍ ĐÃ AN TOÀN.");
            ObjectiveManager.Instance?.CompleteObjective("valves");
        }
    }
}
