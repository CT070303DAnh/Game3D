using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ObjectiveManager: Quan ly danh sach objective va theo doi tien trinh.
/// Tu dong cap nhat khi GameState thay doi.
/// Attach vao: ObjectiveManager GameObject
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public class Objective
    {
        public string id;
        public string description;
        public bool completed;
        public Objective(string id, string desc) { this.id = id; description = desc; }
    }

    private List<Objective> objectives = new List<Objective>();
    public static event System.Action<string> OnObjectiveUpdated; // id cua objective vua complete
    public static event System.Action<Objective> OnCurrentObjectiveChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void OnEnable()
    {
        GameState.OnSecurityCardCollected   += () => CompleteObjective("security_card");
        GameState.OnEngineerCardCollected   += () => CompleteObjective("engineer_card");
        GameState.OnFuseCollected           += () => CompleteObjective("fuse");
        GameState.OnAccessCodeFound         += () => CompleteObjective("pickup_note");
        GameState.OnPowerRestored           += () => CompleteObjective("power");
        GameState.OnSecurityTerminalViewed  += () => CompleteObjective("security_terminal");
        GameState.OnAccessCodeSolved        += () => CompleteObjective("access_code");
        GameState.OnExitUnlocked            += () => CompleteObjective("exit");
        GameState.OnJetFuelCollected        += () => CompleteObjective("fuel");
        GameState.OnWrenchCollected         += () => CompleteObjective("wrench");
        GameState.OnRadarActivated          += () => CompleteObjective("radar");
        GameState.OnDomeGateOpened          += () => CompleteObjective("dome_gate");
        GameState.OnHelipadEscaped          += () => CompleteObjective("helipad_escape");
        GameManager.OnGameStart             += InitObjectives;
    }

    private void OnDisable()
    {
        GameState.OnSecurityCardCollected   -= () => CompleteObjective("security_card");
        GameState.OnEngineerCardCollected   -= () => CompleteObjective("engineer_card");
        GameState.OnFuseCollected           -= () => CompleteObjective("fuse");
        GameState.OnAccessCodeFound         -= () => CompleteObjective("pickup_note");
        GameState.OnPowerRestored           -= () => CompleteObjective("power");
        GameState.OnSecurityTerminalViewed  -= () => CompleteObjective("security_terminal");
        GameState.OnAccessCodeSolved        -= () => CompleteObjective("access_code");
        GameState.OnExitUnlocked            -= () => CompleteObjective("exit");
        GameState.OnJetFuelCollected        -= () => CompleteObjective("fuel");
        GameState.OnWrenchCollected         -= () => CompleteObjective("wrench");
        GameState.OnRadarActivated          -= () => CompleteObjective("radar");
        GameState.OnDomeGateOpened          -= () => CompleteObjective("dome_gate");
        GameState.OnHelipadEscaped          -= () => CompleteObjective("helipad_escape");
        GameManager.OnGameStart             -= InitObjectives;
    }

    private void Start() => InitObjectives();

    private void InitObjectives()
    {
        objectives.Clear();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName.Contains("Level2") || sceneName.Contains("Reactor"))
        {
            objectives.Add(new Objective("wrench", "Tìm Cờ Lê Sửa Chữa (Wrench) tại Kho Phụ Tùng"));
            objectives.Add(new Objective("valves", "Dùng Cờ Lê Khóa 2 Van Xả Khí Độc (Ngừng ngạt khí)"));
            objectives.Add(new Objective("fuse", "Tìm Cầu Chì Cao Áp (Power Fuse)"));
            objectives.Add(new Objective("power", "Lắp Cầu Chì & Bật Cầu Dao Điện Tổng (Phòng Bên Phải)"));
            objectives.Add(new Objective("engineer_card", "Tìm Thẻ Kỹ Sư (Engineer Card)"));
            objectives.Add(new Objective("wall_numbers", "Ghi Nhớ Các Con Số Mật Mã Trên Tường"));
            objectives.Add(new Objective("call_elevator", "Quẹt Thẻ Kỹ Sư & Mở Cửa Thang Máy"));
            objectives.Add(new Objective("elevator_code", "Vào Thang Máy & Nhập Mật Mã Mở Khóa"));

            if (GameState.Instance != null && GameState.Instance.WrenchCollected)
            {
                CompleteObjective("wrench");
            }
            if (GameState.Instance != null && GameState.Instance.FuseCollected)
            {
                CompleteObjective("fuse");
            }
            if (GameState.Instance != null && GameState.Instance.PowerRestored)
            {
                CompleteObjective("power");
            }
            if (GameState.Instance != null && GameState.Instance.EngineerCardCollected)
            {
                CompleteObjective("engineer_card");
            }
        }
        else if (sceneName.Contains("Level3") || sceneName.Contains("Helipad"))
        {
            objectives.Add(new Objective("fuel", "Tìm Bình Nhiên Liệu Tại Kho Tiếp Liệu (Phía Tây)"));
            objectives.Add(new Objective("radar", "Kích Hoạt Trạm Radar Trên Tháp Điều Khiển (Phía Đông)"));
            objectives.Add(new Objective("climb_stairs", "Leo Cầu Thang Lên Nóc Nhà Sân Đỗ"));
            objectives.Add(new Objective("helipad_escape", "Tiếp Nhiên Liệu & Lên Trực Thăng Tẩu Thoát! [E]"));
        }
        else
        {
            objectives.Add(new Objective("security_card", "Tìm Thẻ Bảo Mật (Security Card)"));
            objectives.Add(new Objective("fuse", "Tìm Cầu Chì Cao Áp (Power Fuse)"));
            objectives.Add(new Objective("pickup_note", "Tìm Giấy Ghi Chú Mật Mã (Access Code Note)"));
            objectives.Add(new Objective("power", "Lắp Cầu Chì & Bật Máy Phát Điện Cấp Nguồn"));
            objectives.Add(new Objective("security_terminal", "Xem Dữ Liệu Tại Security Terminal"));
            objectives.Add(new Objective("access_code", "Giải Mã Màu Tại Terminal Cửa Thoát Hiểm"));
            objectives.Add(new Objective("exit", "Mở Cửa Thoát Hiểm Rời Khỏi Phòng Lab!"));
        }

        Debug.Log($"[ObjectiveManager] Objectives initialized for scene: {sceneName}");
        OnCurrentObjectiveChanged?.Invoke(GetCurrentObjective());
    }

    public void CompleteObjective(string id)
    {
        Objective obj = objectives.Find(o => o.id == id);
        if (obj == null || obj.completed) return;
        obj.completed = true;
        Debug.Log($"[ObjectiveManager] Completed: {obj.description}");
        OnObjectiveUpdated?.Invoke(id);
        OnCurrentObjectiveChanged?.Invoke(GetCurrentObjective());
        NotificationUI.ShowMessage($"✓ {obj.description}");
    }

    /// <summary>Tra ve objective hien tai chua hoan thanh.</summary>
    public Objective GetCurrentObjective()
    {
        return objectives.Find(o => !o.completed);
    }

    public List<Objective> GetAllObjectives() => objectives;
}
