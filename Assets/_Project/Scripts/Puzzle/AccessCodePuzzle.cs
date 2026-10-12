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
                return "Terminal Cửa Thoát Hiểm [E] (CHƯA CÓ ĐIỆN - CẦN BẬT MÁY PHÁT)";
            if (GameState.Instance != null && !GameState.Instance.SecurityTerminalViewed)
                return "Terminal Cửa Thoát Hiểm [E] (CẦN XEM DỮ LIỆU TẠI SECURITY TERMINAL)";
            return "Nhập Mật Mã Màu Cửa Thoát Hiểm [E]";
        }
    }

    public void Interact()
    {
        if (solved) return;
        if (GameState.Instance != null && !GameState.Instance.PowerRestored)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ Terminal chưa có điện! Hãy lắp Cầu Chì và bật Máy Phát Điện (Phòng Kho) trước.");
            return;
        }

        if (GameState.Instance != null && !GameState.Instance.SecurityTerminalViewed)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ BẠN CHƯA BIẾT THỨ TỰ MẬT MÃ! Hãy tìm Giấy Ghi Chú và đọc dữ liệu tại Security Terminal trước.");
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
                GameAudioManager.PlayTerminalSuccess();
                AccessCodeUI.SetStatus("✓ [3/3] ĐÚNG TOÀN BỘ MÃ MÀU! ĐANG MỞ KHÓA CỬA THOÁT HIỂM...", Color.green);
                NotificationUI.ShowMessage("✓ [3/3] XÁC THỰC MẬT MÃ MÀU THÀNH CÔNG! ĐÃ MỞ KHÓA CỬA THOÁT HIỂM!");
                StartCoroutine(Solve());
            }
            else
            {
                GameAudioManager.PlayTerminalKey();
                AccessCodeUI.SetStatus($"✓ [{currentStep}/3] Đúng màu thứ {currentStep}! Hãy nhập tiếp màu thứ {currentStep + 1}...", Color.cyan);
                NotificationUI.ShowMessage($"✓ [{currentStep}/{correctSequence.Length}] Đúng màu thứ {currentStep}!");
            }
        }
        else
        {
            GameAudioManager.PlayTerminalError();
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
        AccessCodeUI.SetStatus("Vui lòng nhập thứ tự 3 màu bảo mật từ Security Terminal:\n(Bấm nút chuột hoặc phím số [1: Blue] [2: Red] [3: Green])", new Color(0.3f, 0.9f, 1f));
        NotificationUI.ShowMessage("Mã sai! Đã đặt lại. Hãy nhập lại 3 màu từ Security Terminal.");
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
