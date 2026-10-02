using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ElevatorKeypadUI: Ban phim so dien tu trong buong thang may Man 2.
/// Cho phep nguoi choi bam nut bang chuot hoac go phim so tren ban phim PC (0-9, Backspace, Enter, Esc).
/// Mat khau mac dinh: "214"
/// </summary>
public class ElevatorKeypadUI : MonoBehaviour
{
    public static ElevatorKeypadUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject keypadPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;
    [SerializeField] private Text codeDisplayText;

    [Header("Keypad Buttons")]
    [SerializeField] private Button[] numberButtons; // 0-9
    [SerializeField] private Button clearButton;
    [SerializeField] private Button enterButton;
    [SerializeField] private Button closeButton;

    [Header("Settings")]
    [SerializeField] private string targetCode = "214";
    [SerializeField] private int maxDigits = 3;

    private string currentInput = "";
    private ElevatorController currentElevator;
    private bool isLocked = false;

    public static bool IsOpen => Instance != null && Instance.keypadPanel != null && Instance.keypadPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AccessCodeUI.EnsureEventSystem();

        if (keypadPanel != null)
            keypadPanel.SetActive(false);

        // Bind button listeners
        if (numberButtons != null)
        {
            for (int i = 0; i < numberButtons.Length; i++)
            {
                int digit = i;
                if (numberButtons[i] != null)
                {
                    numberButtons[i].onClick.RemoveAllListeners();
                    numberButtons[i].onClick.AddListener(() => AddDigit(digit.ToString()));
                }
            }
        }

        if (clearButton != null)
        {
            clearButton.onClick.RemoveAllListeners();
            clearButton.onClick.AddListener(ClearInput);
        }

        if (enterButton != null)
        {
            enterButton.onClick.RemoveAllListeners();
            enterButton.onClick.AddListener(SubmitCode);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseKeypad);
        }
    }

    public static void ShowKeypad(ElevatorController elevator, string code = "214")
    {
        if (Instance == null)
        {
            Debug.LogWarning("[ElevatorKeypadUI] No Instance found.");
            return;
        }

        AccessCodeUI.EnsureEventSystem();
        Instance.currentElevator = elevator;
        Instance.targetCode = string.IsNullOrEmpty(code) ? "214" : code;
        Instance.currentInput = "";
        Instance.isLocked = false;

        if (Instance.keypadPanel != null)
        {
            Instance.keypadPanel.SetActive(true);
            Instance.keypadPanel.transform.SetAsLastSibling();
        }

        GameplayUI.Instance?.SetPromptVisible(false);

        if (Instance.titleText != null)
            Instance.titleText.text = "THANG MÁY HÀNG HÓA - TẦNG THƯỢNG";

        if (Instance.statusText != null)
        {
            Instance.statusText.text = "Gợi ý: Tìm 3 chữ số ① ➔ ② ➔ ③ trên tường";
            Instance.statusText.color = new Color(0.2f, 0.8f, 1f);
        }

        Instance.UpdateDisplay();

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public static void ForceClose()
    {
        if (Instance == null) return;
        if (Instance.keypadPanel != null) Instance.keypadPanel.SetActive(false);
        Instance.currentElevator = null;
        Time.timeScale = 1f;
    }

    public void CloseKeypad()
    {
        if (keypadPanel != null) keypadPanel.SetActive(false);
        currentElevator = null;
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameplayUI.Instance?.SetPromptVisible(true);
    }

    private void Update()
    {
        if (!IsOpen) return;

        // Giu chuot luon tu do khi keypad dang mo
        if (Cursor.lockState != CursorLockMode.None)
            Cursor.lockState = CursorLockMode.None;
        if (!Cursor.visible)
            Cursor.visible = true;

        if (isLocked) return;

        // Phim Esc hoac E de dong
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
        {
            CloseKeypad();
            return;
        }

        // Ho tro go ban phim may tinh
        for (int i = 0; i <= 9; i++)
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i) || Input.GetKeyDown(KeyCode.Keypad0 + i))
            {
                AddDigit(i.ToString());
                return;
            }
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
        {
            RemoveLastDigit();
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            SubmitCode();
            return;
        }
    }

    public void AddDigit(string digit)
    {
        if (isLocked) return;
        if (currentInput.Length < maxDigits)
        {
            currentInput += digit;
            UpdateDisplay();
        }
    }

    public void RemoveLastDigit()
    {
        if (isLocked) return;
        if (currentInput.Length > 0)
        {
            currentInput = currentInput.Substring(0, currentInput.Length - 1);
            UpdateDisplay();
        }
    }

    public void ClearInput()
    {
        if (isLocked) return;
        currentInput = "";
        UpdateDisplay();
    }

    public void SubmitCode()
    {
        if (isLocked) return;
        if (currentInput.Length < maxDigits)
        {
            if (statusText != null)
            {
                statusText.text = $"Vui lòng nhập đủ {maxDigits} chữ số!";
                statusText.color = Color.yellow;
            }
            return;
        }

        if (currentInput == targetCode)
        {
            // THANH CONG!
            isLocked = true;
            if (statusText != null)
            {
                statusText.text = "✓ MẬT MÃ CHÍNH XÁC! KHỞI ĐỘNG THANG MÁY...";
                statusText.color = new Color(0.2f, 1f, 0.3f);
            }
            NotificationUI.ShowMessage("MẬT MÃ ĐÚNG! Thang máy đang đưa bạn lên Tầng Thượng...");
            StartCoroutine(SuccessRoutine());
        }
        else
        {
            // THAT BAI!
            if (statusText != null)
            {
                statusText.text = "✗ TỪ CHỐI TRUY CẬP - SAI MÃ!";
                statusText.color = new Color(1f, 0.2f, 0.2f);
            }
            NotificationUI.ShowMessage("SAI MÃ THANG MÁY! Xem lại các số trên tường.");
            StartCoroutine(ResetAfterWrongCode());
        }
    }

    private System.Collections.IEnumerator ResetAfterWrongCode()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        currentInput = "";
        UpdateDisplay();
        if (statusText != null)
        {
            statusText.text = "Gợi ý: Tìm 3 chữ số ① ➔ ② ➔ ③ trên tường";
            statusText.color = new Color(0.2f, 0.8f, 1f);
        }
    }

    private System.Collections.IEnumerator SuccessRoutine()
    {
        yield return new WaitForSecondsRealtime(1.2f);
        var elevator = currentElevator;
        CloseKeypad();

        if (elevator != null)
        {
            elevator.OnCodeCorrect();
        }
    }

    private void UpdateDisplay()
    {
        if (codeDisplayText == null) return;

        string display = "";
        for (int i = 0; i < maxDigits; i++)
        {
            if (i < currentInput.Length)
                display += $"{currentInput[i]} ";
            else
                display += "_ ";
        }
        codeDisplayText.text = $"[ {display.Trim()} ]";
    }

    private void OnDestroy()
    {
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
