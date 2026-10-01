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
        GameManager.OnGameStart           += InitObjectives;
    }

    private void OnDisable()
    {
        GameState.OnSecurityCardCollected -= () => CompleteObjective("security_card");
        GameState.OnFuseCollected         -= () => CompleteObjective("fuse");
        GameState.OnPowerRestored         -= () => CompleteObjective("power");
        GameState.OnAccessCodeSolved      -= () => CompleteObjective("access_code");
        GameState.OnExitUnlocked          -= () => CompleteObjective("exit");
        GameManager.OnGameStart           -= InitObjectives;
    }

    private void Start() => InitObjectives();

    private void InitObjectives()
    {
        objectives.Clear();
        objectives.Add(new Objective("security_card", "Tìm Thẻ Bảo Mật"));
        objectives.Add(new Objective("fuse", "Tìm Cầu Chì"));
        objectives.Add(new Objective("power", "Khôi phục Điện (Máy Phát Điện)"));
        objectives.Add(new Objective("access_code", "Tìm & Nhập Mã Truy Cập"));
        objectives.Add(new Objective("exit", "Thoát Khỏi Phòng Thí Nghiệm"));
        Debug.Log("[ObjectiveManager] Objectives initialized.");
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
