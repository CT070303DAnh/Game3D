using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TerminalUI: Hien thi noi dung Terminal khi player tuong tac.
/// AccessCodeUI: Hien thi puzzle chon mau.
/// Static API cho ca hai.
/// Attach vao: TerminalUI GameObject
/// </summary>
public class TerminalUI : MonoBehaviour
{
    public static TerminalUI Instance { get; private set; }

    [Header("Terminal Panel")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private Text terminalTitleText;
    [SerializeField] private Text terminalContentText;
    [SerializeField] private Button closeTerminalButton;

    public static bool IsOpen => Instance != null && Instance.terminalPanel != null && Instance.terminalPanel.activeSelf;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AccessCodeUI.EnsureEventSystem();

        if (terminalPanel == null)
        {
            var pnl = GameObject.Find("TerminalUIPanel");
            if (pnl != null) terminalPanel = pnl;
        }

        if (terminalPanel != null)
        {
            if (terminalTitleText == null) terminalTitleText = terminalPanel.transform.Find("TermTitle")?.GetComponent<Text>();
            if (terminalContentText == null) terminalContentText = terminalPanel.transform.Find("TermContent")?.GetComponent<Text>();
            if (closeTerminalButton == null)
            {
                var cb = terminalPanel.transform.Find("CloseTermBtn")?.GetComponent<Button>();
                if (cb == null)
                {
                    foreach (var b in terminalPanel.GetComponentsInChildren<Button>(true))
                    {
                        if (b.name.IndexOf("close", System.StringComparison.OrdinalIgnoreCase) >= 0)
                        {
                            cb = b;
                            break;
                        }
                    }
                }
                closeTerminalButton = cb;
            }
        }

        if (terminalPanel != null) terminalPanel.SetActive(false);
        if (closeTerminalButton != null)
        {
            closeTerminalButton.onClick.RemoveAllListeners();
            closeTerminalButton.onClick.AddListener(() => CloseTerminal());
        }
    }

    public static void ShowTerminal(string title, string content)
    {
        if (Instance == null) { Debug.LogWarning("[TerminalUI] No instance found."); return; }
        AccessCodeUI.EnsureEventSystem();
        if (Instance.terminalTitleText != null) Instance.terminalTitleText.text = title;
        if (Instance.terminalContentText != null) Instance.terminalContentText.text = content;
        if (Instance.terminalPanel != null)
        {
            Instance.terminalPanel.SetActive(true);
            Instance.terminalPanel.transform.SetAsLastSibling();
        }
        // An prompt tuong tac de khong bi de
        GameplayUI.Instance?.SetPromptVisible(false);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // Goi khi Game Over de dong panel khong de len CAPTURED screen
    public static void ForceClose()
    {
        if (Instance == null) return;
        if (Instance.terminalPanel != null) Instance.terminalPanel.SetActive(false);
        Time.timeScale = 1f;
    }

    public static void Close()
    {
        if (Instance != null) Instance.CloseTerminal();
    }

    public void CloseTerminal()
    {
        if (terminalPanel != null) terminalPanel.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameplayUI.Instance?.SetPromptVisible(true);
    }

    private void Update()
    {
        if (IsOpen)
        {
            // Luon giu chuot hien thi
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            // Dong bang phim Escape hoac phím E
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                CloseTerminal();
            }
        }
    }

    private void OnDestroy()
    {
        if (closeTerminalButton != null) closeTerminalButton.onClick.RemoveAllListeners();
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
