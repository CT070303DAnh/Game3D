using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TerminalUI: Quản lý giao diện hiển thị dữ liệu Security Terminal khi người chơi tương tác.
/// - Tự động tạo và bảo trì UI (Fallback Panel) nếu Scene bị thiếu.
/// - Hỗ trợ đóng bằng phím ESC hoặc phím E, tự động khóa/mở chuột và dừng/tiếp tục thời gian.
/// </summary>
public class TerminalUI : MonoBehaviour
{
    public static TerminalUI Instance { get; private set; }

    [Header("Terminal Panel References")]
    [SerializeField] private GameObject terminalPanel;
    [SerializeField] private Text terminalTitleText;
    [SerializeField] private Text terminalContentText;
    [SerializeField] private Button closeTerminalButton;

    public static bool IsOpen => Instance != null && Instance.terminalPanel != null && Instance.terminalPanel.activeSelf;

    public static TerminalUI EnsureInstance()
    {
        if (Instance != null)
        {
            Instance.EnsureHierarchy();
            return Instance;
        }

        var found = FindFirstObjectByType<TerminalUI>();
        if (found != null)
        {
            Instance = found;
            Instance.EnsureHierarchy();
            return Instance;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cGO = new GameObject("TerminalCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        var go = new GameObject("TerminalUI");
        go.transform.SetParent(canvas.transform, false);
        Instance = go.AddComponent<TerminalUI>();
        Instance.EnsureHierarchy();
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AccessCodeUI.EnsureEventSystem();
        EnsureHierarchy();
    }

    public static void ShowTerminal(string title, string content)
    {
        var inst = EnsureInstance();
        AccessCodeUI.EnsureEventSystem();

        if (inst.terminalTitleText != null)
            inst.terminalTitleText.text = !string.IsNullOrEmpty(title) ? title : "SECURITY TERMINAL";

        if (inst.terminalContentText != null)
            inst.terminalContentText.text = content;

        if (inst.terminalPanel != null)
        {
            inst.terminalPanel.SetActive(true);
            inst.terminalPanel.transform.SetAsLastSibling();
        }

        // Ẩn prompt tương tác HUD
        GameplayUI.Instance?.SetPromptVisible(false);

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

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
        if (terminalPanel != null)
            terminalPanel.SetActive(false);

        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        GameplayUI.Instance?.SetPromptVisible(true);
    }

    private void Update()
    {
        if (IsOpen)
        {
            // Luôn giữ chuột hiển thị khi đang đọc Terminal
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            // Đóng bằng phím Escape hoặc phím E
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                CloseTerminal();
            }
        }
    }

    public void EnsureHierarchy()
    {
        if (terminalPanel != null) return;

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Panel mờ toàn màn hình
        terminalPanel = new GameObject("TerminalUIPanel");
        terminalPanel.transform.SetParent(canvas.transform, false);
        var pR = terminalPanel.AddComponent<RectTransform>();
        pR.anchorMin = Vector2.zero;
        pR.anchorMax = Vector2.one;
        pR.offsetMin = Vector2.zero;
        pR.offsetMax = Vector2.zero;

        var pImg = terminalPanel.AddComponent<Image>();
        pImg.color = new Color(0.01f, 0.03f, 0.06f, 0.94f);
        pImg.raycastTarget = true;

        // 2. Khung Box CRT Trung Tâm
        var boxGO = new GameObject("TerminalBox");
        boxGO.transform.SetParent(terminalPanel.transform, false);
        var boxR = boxGO.AddComponent<RectTransform>();
        boxR.anchorMin = new Vector2(0.5f, 0.5f);
        boxR.anchorMax = new Vector2(0.5f, 0.5f);
        boxR.pivot = new Vector2(0.5f, 0.5f);
        boxR.anchoredPosition = Vector2.zero;
        boxR.sizeDelta = new Vector2(680f, 480f);

        var boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.03f, 0.07f, 0.12f, 0.98f);

        var boxOl = boxGO.AddComponent<Outline>();
        boxOl.effectColor = new Color(0.2f, 0.85f, 1f, 0.95f); // Viền Cyan neon
        boxOl.effectDistance = new Vector2(2f, -2f);

        // Header Title
        var titleGO = new GameObject("TermTitle");
        titleGO.transform.SetParent(boxGO.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchoredPosition = new Vector2(0f, 200f);
        titleR.sizeDelta = new Vector2(640f, 45f);

        terminalTitleText = titleGO.AddComponent<Text>();
        terminalTitleText.text = "SECURITY TERMINAL - HỆ THỐNG AN NINH";
        terminalTitleText.font = font;
        terminalTitleText.fontSize = 24;
        terminalTitleText.fontStyle = FontStyle.Bold;
        terminalTitleText.color = new Color(0.35f, 0.95f, 1f);
        terminalTitleText.alignment = TextAnchor.MiddleCenter;

        // Content Text Box (CRT Background)
        var contentBoxGO = new GameObject("ContentBox");
        contentBoxGO.transform.SetParent(boxGO.transform, false);
        var contentBoxR = contentBoxGO.AddComponent<RectTransform>();
        contentBoxR.anchoredPosition = new Vector2(0f, 15f);
        contentBoxR.sizeDelta = new Vector2(620f, 290f);

        var cBoxImg = contentBoxGO.AddComponent<Image>();
        cBoxImg.color = new Color(0.01f, 0.04f, 0.08f, 0.9f);

        var cBoxOl = contentBoxGO.AddComponent<Outline>();
        cBoxOl.effectColor = new Color(0.15f, 0.45f, 0.65f, 0.6f);
        cBoxOl.effectDistance = new Vector2(1f, -1f);

        // Content Text
        var contentGO = new GameObject("TermContent");
        contentGO.transform.SetParent(contentBoxGO.transform, false);
        var contentR = contentGO.AddComponent<RectTransform>();
        contentR.anchorMin = Vector2.zero;
        contentR.anchorMax = Vector2.one;
        contentR.offsetMin = new Vector2(20f, 15f);
        contentR.offsetMax = new Vector2(-20f, -15f);

        terminalContentText = contentGO.AddComponent<Text>();
        terminalContentText.font = font;
        terminalContentText.fontSize = 17;
        terminalContentText.lineSpacing = 1.25f;
        terminalContentText.fontStyle = FontStyle.Normal;
        terminalContentText.color = new Color(0.85f, 0.95f, 1f);
        terminalContentText.alignment = TextAnchor.UpperLeft;

        // Nút Đóng [ ĐÓNG TERMINAL (ESC / E) ]
        var closeGO = new GameObject("CloseTermBtn");
        closeGO.transform.SetParent(boxGO.transform, false);
        var closeR = closeGO.AddComponent<RectTransform>();
        closeR.anchoredPosition = new Vector2(0f, -185f);
        closeR.sizeDelta = new Vector2(280f, 48f);

        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.15f, 0.25f, 0.38f);

        var closeOl = closeGO.AddComponent<Outline>();
        closeOl.effectColor = new Color(0.4f, 0.85f, 1f);
        closeOl.effectDistance = new Vector2(1.5f, -1.5f);

        closeTerminalButton = closeGO.AddComponent<Button>();
        closeTerminalButton.onClick.AddListener(CloseTerminal);

        var cTxtGO = new GameObject("Text");
        cTxtGO.transform.SetParent(closeGO.transform, false);
        var cTxtR = cTxtGO.AddComponent<RectTransform>();
        cTxtR.anchorMin = Vector2.zero;
        cTxtR.anchorMax = Vector2.one;
        cTxtR.offsetMin = Vector2.zero;
        cTxtR.offsetMax = Vector2.zero;

        var cTxt = cTxtGO.AddComponent<Text>();
        cTxt.text = "[ ĐÓNG TERMINAL (ESC / E) ]";
        cTxt.font = font;
        cTxt.fontSize = 16;
        cTxt.fontStyle = FontStyle.Bold;
        cTxt.color = Color.white;
        cTxt.alignment = TextAnchor.MiddleCenter;

        terminalPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (closeTerminalButton != null) closeTerminalButton.onClick.RemoveAllListeners();
        if (Time.timeScale == 0f) Time.timeScale = 1f;
    }
}
