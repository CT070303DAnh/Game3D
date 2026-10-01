using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AccessCodeUI: UI cho puzzle nhap access code.
/// Hien thi 3 nut mau: BLUE, RED, GREEN.
/// Static: AccessCodeUI.ShowPuzzle(puzzle)
/// Attach vao: AccessCodeUI GameObject
/// </summary>
public class AccessCodeUI : MonoBehaviour
{
    public static AccessCodeUI Instance { get; private set; }

    [Header("Panel")]
    [SerializeField] private GameObject puzzlePanel;

    [Header("Buttons")]
    [SerializeField] private Button blueButton;
    [SerializeField] private Button redButton;
    [SerializeField] private Button greenButton;
    [SerializeField] private Button closePuzzleButton;

    [Header("Status")]
    [SerializeField] private Text statusText;

    private AccessCodePuzzle currentPuzzle;

    public static bool IsOpen => Instance != null && Instance.puzzlePanel != null && Instance.puzzlePanel.activeSelf;

    public static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null && 
            Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem_Auto");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[AccessCodeUI] Auto-created missing EventSystem with StandaloneInputModule.");
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        EnsureEventSystem();

        // Tu tim cac references neu chua duoc gan trong inspector
        if (puzzlePanel == null)
        {
            var pnl = GameObject.Find("AccessCodeUIPanel");
            if (pnl != null) puzzlePanel = pnl;
        }

        if (puzzlePanel != null)
        {
            if (blueButton == null) blueButton = puzzlePanel.transform.Find("BlueBtn")?.GetComponent<Button>();
            if (redButton == null) redButton = puzzlePanel.transform.Find("RedBtn")?.GetComponent<Button>();
            if (greenButton == null) greenButton = puzzlePanel.transform.Find("GreenBtn")?.GetComponent<Button>();
            if (closePuzzleButton == null)
            {
                var cb = puzzlePanel.transform.Find("CloseACBtn")?.GetComponent<Button>();
                if (cb == null)
                {
                    foreach (var b in puzzlePanel.GetComponentsInChildren<Button>(true))
                    {
                        if (b.name.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cb = b;
                            break;
                        }
                    }
                }
                closePuzzleButton = cb;
            }
            if (statusText == null) statusText = puzzlePanel.transform.Find("ACStatus")?.GetComponent<Text>();
        }

        if (puzzlePanel != null) puzzlePanel.SetActive(false);

        // Gan listeners
        if (blueButton != null)
        {
            blueButton.onClick.RemoveAllListeners();
            blueButton.onClick.AddListener(() => Submit("BLUE"));
        }
        if (redButton != null)
        {
            redButton.onClick.RemoveAllListeners();
            redButton.onClick.AddListener(() => Submit("RED"));
        }
        if (greenButton != null)
        {
            greenButton.onClick.RemoveAllListeners();
            greenButton.onClick.AddListener(() => Submit("GREEN"));
        }
        if (closePuzzleButton != null)
        {
            closePuzzleButton.onClick.RemoveAllListeners();
            closePuzzleButton.onClick.AddListener(ClosePuzzle);
        }
    }

    private void Update()
    {
        if (IsOpen)
        {
            // Luon giu con tro chuot hien ro rang de bam nut
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            // Cho phep dong bang phim Escape hoac phim E
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                ClosePuzzle();
            }
        }
    }

    public static void ShowPuzzle(AccessCodePuzzle puzzle)
    {
        if (Instance == null) { Debug.LogWarning("[AccessCodeUI] No instance."); return; }
        EnsureEventSystem();
        Instance.currentPuzzle = puzzle;
        if (Instance.puzzlePanel != null)
        {
            Instance.puzzlePanel.SetActive(true);
            Instance.puzzlePanel.transform.SetAsLastSibling(); // Dua len tren cung Canvas de khong bi che khuat
        }
        // An prompt tuong tac de tranh bi de len panel
        GameplayUI.Instance?.SetPromptVisible(false);

        if (Instance.statusText != null) Instance.statusText.text = "Enter sequence: BLUE \u2192 RED \u2192 GREEN";
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f; // Dung game de robot khong tan cong khi dang nhap ma
    }

    // Goi khi Game Over de dong panel khong de len CAPTURED screen
    public static void ForceClose()
    {
        if (Instance == null) return;
        if (Instance.puzzlePanel != null) Instance.puzzlePanel.SetActive(false);
        Instance.currentPuzzle = null;
        Time.timeScale = 1f;
    }

    public static void Close()
    {
        if (Instance != null) Instance.ClosePuzzle();
    }

    private void Submit(string color)
    {
        if (currentPuzzle == null) return;
        currentPuzzle.SubmitColor(color);
        if (statusText != null) statusText.text = $"Submitted: {color}";
    }

    public void ClosePuzzle()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        currentPuzzle = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f; // Tiep tuc game

        // Hien lai prompt tuong tac neu can
        GameplayUI.Instance?.SetPromptVisible(true);
    }

    private void OnDestroy()
    {
        if (blueButton != null) blueButton.onClick.RemoveAllListeners();
        if (redButton != null) redButton.onClick.RemoveAllListeners();
        if (greenButton != null) greenButton.onClick.RemoveAllListeners();
        if (closePuzzleButton != null) closePuzzleButton.onClick.RemoveAllListeners();
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
