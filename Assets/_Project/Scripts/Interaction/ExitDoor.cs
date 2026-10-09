using UnityEngine;

/// <summary>
/// ExitDoor: Cua thoat cuoi game.
/// Khi player interact va du dieu kien -> TriggerWin.
/// </summary>
public class ExitDoor : MonoBehaviour, IInteractable
{
    [Header("Exit Door")]
    [SerializeField] private GameObject lockedVisual;   // Hien thi khi chua mo
    [SerializeField] private GameObject unlockedVisual; // Hien thi khi da mo

    public bool CanInteract => true;
    public string InteractPromptText =>
        GameState.Instance != null && GameState.Instance.ExitUnlocked
        ? "THOÁT! (Rời khỏi phòng thí nghiệm)"
        : "Cửa đang khóa";

    private void OnEnable() => GameState.OnExitUnlocked += OnUnlocked;
    private void OnDisable() => GameState.OnExitUnlocked -= OnUnlocked;

    private void Start() => UpdateVisual();

    public void Interact()
    {
        if (GameState.Instance == null || !GameState.Instance.ExitUnlocked)
        {
            NotificationUI.ShowMessage("Cửa thoát đang khóa. Hoàn thành tất cả nhiệm vụ trước.");
            return;
        }

        Debug.Log("[ExitDoor] Player escaped through exit door!");
        NotificationUI.ShowMessage("HOÀN THÀNH MÀN 1! Đang tiến vào Khu Lò Phản Ứng...");
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevelByName("Level2_Reactor");
        }
        else if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("Level2_Reactor");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level2_Reactor");
        }
    }

    private void OnUnlocked()
    {
        NotificationUI.ShowMessage("CỬA THOÁT MỞ KHÓA! Chạy đi ngay!");
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        bool unlocked = GameState.Instance != null && GameState.Instance.ExitUnlocked;
        if (lockedVisual != null) lockedVisual.SetActive(!unlocked);
        if (unlockedVisual != null) unlockedVisual.SetActive(unlocked);
    }
}
