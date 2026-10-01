using UnityEngine;

/// <summary>
/// AccessCodePuzzle: Puzzle giai ma truy cap.
/// Sequence: BLUE -> RED -> GREEN (3 nut bam theo thu tu).
/// Implement IInteractable cho moi nut.
/// Attach vao: AccessCodeTerminal GameObject
/// </summary>
public class AccessCodePuzzle : MonoBehaviour, IInteractable
{
    [Header("Puzzle Settings")]
    [SerializeField] private string[] correctSequence = { "BLUE", "RED", "GREEN" };
    [SerializeField] private float resetDelay = 2f;

    [Header("UI References")]
    [SerializeField] private GameObject puzzlePanel;

    private int currentStep = 0;
    private bool solved = false;

    public bool CanInteract => !solved && GameState.Instance != null && GameState.Instance.PowerRestored;
    public string InteractPromptText => solved ? "Quyền truy cập đã mở" :
        GameState.Instance != null && !GameState.Instance.PowerRestored ? "Không có điện" :
        "Sử dụng Terminal mã truy cập";

    public void Interact()
    {
        if (solved) return;
        if (!CanInteract) { NotificationUI.ShowMessage("Terminal chưa có điện."); return; }
        // Hien thi panel puzzle
        if (puzzlePanel != null) puzzlePanel.SetActive(true);
        AccessCodeUI.ShowPuzzle(this);
    }

    /// <summary>Duoc goi tu UI khi player chon mau.</summary>
    public void SubmitColor(string color)
    {
        if (solved) return;
        if (color == correctSequence[currentStep])
        {
            currentStep++;
            NotificationUI.ShowMessage($"Bước {currentStep}/{correctSequence.Length} đúng!");
            if (currentStep >= correctSequence.Length)
                StartCoroutine(Solve());
        }
        else
        {
            NotificationUI.ShowMessage("TỪ CHỐI TRUY CẬP - Sai thứ tự!");
            StartCoroutine(ResetAfterDelay());
        }
    }

    private System.Collections.IEnumerator Solve()
    {
        solved = true;
        NotificationUI.ShowMessage("TRUY CẬP THÀNH CÔNG!");
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        GameState.Instance?.SetAccessCodeSolved();
        yield return new WaitForSecondsRealtime(0.6f);
        AccessCodeUI.Close();
    }

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resetDelay);
        currentStep = 0;
        NotificationUI.ShowMessage("Thử lại. Đã đặt lại thứ tự.");
    }
}
