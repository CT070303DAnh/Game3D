using UnityEngine;

/// <summary>
/// AccessCodePuzzle: Puzzle giai ma truy cap tai Terminal Cua Thoat Hiem.
/// Sequence: BLUE -> RED -> GREEN (3 nut bam theo thu tu).
/// Implement IInteractable.
/// Attach vao: AccessCodeTerminal GameObject
/// </summary>
public class AccessCodePuzzle : MonoBehaviour, IInteractable
{
    [Header("Puzzle Settings")]
    [SerializeField] private string[] correctSequence = { "BLUE", "RED", "GREEN" };
    [SerializeField] private float resetDelay = 1.5f;

    [Header("UI References")]
    [SerializeField] private GameObject puzzlePanel;

    [Header("Visual Indicators (Optional)")]
    [SerializeField] private Renderer[] indicatorLamps; // 0: Blue, 1: Red, 2: Green

    private int currentStep = 0;
    private bool solved = false;

    public bool CanInteract => !solved;
    public string InteractPromptText
    {
        get
        {
            if (solved) return "Quyền truy cập đã mở (Đã giải mã màu)";
            if (GameState.Instance != null && !GameState.Instance.PowerRestored)
                return "Terminal Mã Màu Thoát Hiểm [E] (CẦN BẬT ĐIỆN MÁY PHÁT TRƯỚC)";
            return "Nhập Mã Màu Cửa Thoát Hiểm [E] (BLUE \u2794 RED \u2794 GREEN)";
        }
    }

    public void Interact()
    {
        if (solved) return;
        if (GameState.Instance != null && !GameState.Instance.PowerRestored)
        {
            NotificationUI.ShowMessage("Terminal chưa có điện! Hãy lắp Cầu Chì và bật Máy Phát Điện (Phòng A) trước.");
            return;
        }

        // Hien thi panel puzzle
        if (puzzlePanel != null) puzzlePanel.SetActive(true);
        AccessCodeUI.ShowPuzzle(this);
    }

    /// <summary>Duoc goi tu UI khi player chon mau hoac bam phim 1/2/3.</summary>
    public void SubmitColor(string color)
    {
        if (solved) return;
        if (color == correctSequence[currentStep])
        {
            // Bat den tuong ung neu co
            SetLampState(currentStep, true);

            currentStep++;
            if (currentStep >= correctSequence.Length)
            {
                AccessCodeUI.SetStatus("✓ [3/3] ĐÚNG TOÀN BỘ MÃ MÀU! ĐANG MỞ KHÓA CỬA THOÁT HIỂM...", Color.green);
                NotificationUI.ShowMessage($"✓ [3/3] ĐÚNG MÀU {color}! HOÀN TẤT MẬT MÃ MÀU!");
                StartCoroutine(Solve());
            }
            else
            {
                string nextHint = currentStep == 1 ? "RED" : "GREEN";
                AccessCodeUI.SetStatus($"✓ [{currentStep}/3] Đúng màu {color}! Tiếp theo: {nextHint}", Color.cyan);
                NotificationUI.ShowMessage($"✓ [{currentStep}/{correctSequence.Length}] Đúng màu {color}! Tiếp theo: {nextHint}");
            }
        }
        else
        {
            AccessCodeUI.SetStatus($"✗ TỪ CHỐI TRUY CẬP - Sai màu '{color}'! Đang đặt lại...", Color.red);
            NotificationUI.ShowMessage($"✗ TỪ CHỐI TRUY CẬP - Sai màu '{color}'! Thử lại từ đầu.");
            StartCoroutine(ResetAfterDelay());
        }
    }

    private System.Collections.IEnumerator Solve()
    {
        solved = true;
        NotificationUI.ShowMessage("✓ XÁC THỰC MẬT MÃ MÀU THÀNH CÔNG! ĐÃ MỞ KHÓA CỬA THOÁT HIỂM!");
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        GameState.Instance?.SetAccessCodeSolved();
        yield return new WaitForSecondsRealtime(0.8f);
        AccessCodeUI.Close();
    }

    private System.Collections.IEnumerator ResetAfterDelay()
    {
        yield return new WaitForSecondsRealtime(resetDelay);
        currentStep = 0;
        ResetAllLamps();
        AccessCodeUI.SetStatus("Vui lòng nhập thứ tự màu: BLUE \u2794 RED \u2794 GREEN\n(Bấm nút chuột hoặc phím số [1] [2] [3])", new Color(0.3f, 0.9f, 1f));
        NotificationUI.ShowMessage("Đã đặt lại mã màu. Hãy nhập lại: BLUE ➔ RED ➔ GREEN.");
    }

    private void SetLampState(int index, bool on)
    {
        if (indicatorLamps == null || index < 0 || index >= indicatorLamps.Length) return;
        var r = indicatorLamps[index];
        if (r != null && r.material != null)
        {
            Color baseCol = index == 0 ? Color.blue : (index == 1 ? Color.red : Color.green);
            if (on)
            {
                r.material.EnableKeyword("_EMISSION");
                r.material.SetColor("_EmissionColor", baseCol * 3f);
            }
            else
            {
                r.material.DisableKeyword("_EMISSION");
            }
        }
    }

    private void ResetAllLamps()
    {
        if (indicatorLamps == null) return;
        for (int i = 0; i < indicatorLamps.Length; i++)
        {
            SetLampState(i, false);
        }
    }
}
