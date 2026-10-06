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
        GameState.OnSecurityCardCollected += () => CompleteObjective("security_card");
        GameState.OnFuseCollected         += () => CompleteObjective("fuse");
        GameState.OnPowerRestored         += () => CompleteObjective("power");
        GameState.OnAccessCodeSolved      += () => CompleteObjective("access_code");
        GameState.OnExitUnlocked          += () => CompleteObjective("exit");
        GameState.OnJetFuelCollected      += () => CompleteObjective("fuel");
        GameState.OnRadarActivated        += () => CompleteObjective("radar");
        GameState.OnDomeGateOpened        += () => CompleteObjective("dome_gate");
        GameState.OnHelipadEscaped        += () => CompleteObjective("helipad_escape");
        GameManager.OnGameStart           += InitObjectives;
    }

    private void OnDisable()
    {
        GameState.OnSecurityCardCollected -= () => CompleteObjective("security_card");
        GameState.OnFuseCollected         -= () => CompleteObjective("fuse");
        GameState.OnPowerRestored         -= () => CompleteObjective("power");
        GameState.OnAccessCodeSolved      -= () => CompleteObjective("access_code");
        GameState.OnExitUnlocked          -= () => CompleteObjective("exit");
        GameState.OnJetFuelCollected      -= () => CompleteObjective("fuel");
        GameState.OnRadarActivated        -= () => CompleteObjective("radar");
        GameState.OnDomeGateOpened        -= () => CompleteObjective("dome_gate");
        GameState.OnHelipadEscaped        -= () => CompleteObjective("helipad_escape");
        GameManager.OnGameStart           -= InitObjectives;
    }

    private void Start() => InitObjectives();

    private void InitObjectives()
    {
        objectives.Clear();
        string sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        if (sceneName.Contains("Level2") || sceneName.Contains("Reactor"))
        {
            objectives.Add(new Objective("valves", "Đóng 3 Van Xả Khí Độc (Ngừng ngạt khí)"));
            objectives.Add(new Objective("fuse", "Tìm Cầu Chì Thang Máy (Power Fuse)"));
            objectives.Add(new Objective("power", "Đóng Cầu Dao Điện Tổng"));
            objectives.Add(new Objective("wall_numbers", "Tìm 3 Con Số Trên Tường (1, 4, 2)"));
            objectives.Add(new Objective("call_elevator", "Bấm Nút Mở Cửa Thang Máy"));
            objectives.Add(new Objective("elevator_code", "Vào Thang Máy & Nhập Mã [2 1 4]"));
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
            objectives.Add(new Objective("security_card", "Tìm Thẻ Bảo Mật"));
            objectives.Add(new Objective("fuse", "Tìm Cầu Chì"));
            objectives.Add(new Objective("power", "Khôi phục Điện (Máy Phát Điện)"));
            objectives.Add(new Objective("access_code", "Tìm & Nhập Mã Truy Cập"));
            objectives.Add(new Objective("exit", "Thoát Khỏi Phòng Thí Nghiệm"));
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
