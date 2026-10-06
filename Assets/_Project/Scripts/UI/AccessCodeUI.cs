using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// AccessCodeUI: UI cho puzzle nhap ma mau tai Terminal Màn 1.
/// Hien thi 3 nut mau: BLUE (Xanh duong), RED (Do), GREEN (Xanh la).
/// Ho tro bam chuot hoac an phim so tren ban phim PC (1: Blue, 2: Red, 3: Green, Esc/E: Dong).
/// Tu dong tim hoac tao UI neu scene bi thieu.
/// Static: AccessCodeUI.ShowPuzzle(puzzle)
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

    [Header("Status & Info")]
    [SerializeField] private Text titleText;
    [SerializeField] private Text statusText;

    private AccessCodePuzzle currentPuzzle;

    public static bool IsOpen => Instance != null && Instance.puzzlePanel != null && Instance.puzzlePanel.activeSelf;

    public static void EnsureEventSystem()
    {
        if (UnityEngine.EventSystems.EventSystem.current == null && 
            Object.FindAnyObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem_Auto");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[AccessCodeUI] Auto-created missing EventSystem with StandaloneInputModule.");
        }
    }

    public static AccessCodeUI EnsureInstance()
    {
        if (Instance != null) return Instance;

        Instance = Object.FindAnyObjectByType<AccessCodeUI>(FindObjectsInactive.Include);
        if (Instance != null) return Instance;

        // Neu chua co, tim Canvas va gan vao
        Canvas canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
        if (canvas == null)
        {
            GameObject cGO = new GameObject("Canvas_Auto");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        GameObject uiGO = new GameObject("AccessCodeUI");
        uiGO.transform.SetParent(canvas.transform, false);
        Instance = uiGO.AddComponent<AccessCodeUI>();
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

        EnsureEventSystem();
        ResolveReferences();
    }

    public void ResolveReferences()
    {
        // 1. Tim panel
        if (puzzlePanel == null)
        {
            // Tim trong tat ca children ke ca inactive
            var allTransforms = Resources.FindObjectsOfTypeAll<Transform>();
            foreach (var t in allTransforms)
            {
                if (t != null && t.gameObject.name == "AccessCodeUIPanel" && t.gameObject.scene.isLoaded)
                {
                    puzzlePanel = t.gameObject;
                    break;
                }
            }
        }

        // Neu van khong co, tu dong tao Fallback UI Panel tren Canvas
        if (puzzlePanel == null)
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = Object.FindAnyObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                CreateFallbackPanel(canvas);
            }
        }

        if (puzzlePanel != null)
        {
            if (titleText == null) titleText = puzzlePanel.transform.Find("ACTitle")?.GetComponent<Text>();
            if (statusText == null) statusText = puzzlePanel.transform.Find("ACStatus")?.GetComponent<Text>();

            if (blueButton == null) blueButton = FindButtonByNameOrText(puzzlePanel, "BlueBtn", "BLUE");
            if (redButton == null) redButton = FindButtonByNameOrText(puzzlePanel, "RedBtn", "RED");
            if (greenButton == null) greenButton = FindButtonByNameOrText(puzzlePanel, "GreenBtn", "GREEN");
            if (closePuzzleButton == null) closePuzzleButton = FindButtonByNameOrText(puzzlePanel, "CloseACBtn", "CLOSE");

            BindButtonListeners();
            puzzlePanel.SetActive(false);
        }
    }

    private Button FindButtonByNameOrText(GameObject root, string nameExact, string matchKeyword)
    {
        var tr = root.transform.Find(nameExact);
        if (tr != null)
        {
            var btn = tr.GetComponent<Button>();
            if (btn != null) return btn;
        }

        foreach (var b in root.GetComponentsInChildren<Button>(true))
        {
            if (b.name.IndexOf(matchKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return b;

            var txt = b.GetComponentInChildren<Text>(true);
            if (txt != null && txt.text.IndexOf(matchKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0)
                return b;
        }
        return null;
    }

    private void BindButtonListeners()
    {
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
            // Luon giai phong con tro chuot de nguoi choi bam nut thoai mai
            if (Cursor.lockState != CursorLockMode.None)
                Cursor.lockState = CursorLockMode.None;
            if (!Cursor.visible)
                Cursor.visible = true;

            // Phim tat ban phim PC:
            // 1 hoac B: BLUE
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1) || Input.GetKeyDown(KeyCode.B))
            {
                Submit("BLUE");
            }
            // 2 hoac R: RED
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2) || Input.GetKeyDown(KeyCode.R))
            {
                Submit("RED");
            }
            // 3 hoac G: GREEN
            else if (Input.GetKeyDown(KeyCode.Alpha3) || Input.GetKeyDown(KeyCode.Keypad3) || Input.GetKeyDown(KeyCode.G))
            {
                Submit("GREEN");
            }
            // Escape hoac E: Dong Terminal
            else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.E))
            {
                ClosePuzzle();
            }
        }
    }

    public static void ShowPuzzle(AccessCodePuzzle puzzle)
    {
        var inst = EnsureInstance();
        EnsureEventSystem();

        inst.currentPuzzle = puzzle;
        inst.ResolveReferences();

        if (inst.puzzlePanel != null)
        {
            inst.puzzlePanel.SetActive(true);
            inst.puzzlePanel.transform.SetAsLastSibling(); // Luon tren cung Canvas
        }

        // An prompt tuong tac khoi de len giao dien
        GameplayUI.Instance?.SetPromptVisible(false);

        if (inst.titleText != null)
        {
            inst.titleText.text = "TERMINAL BẢO MẬT - MÃ MÀU CỬA THOÁT";
        }

        if (inst.statusText != null)
        {
            inst.statusText.text = "Vui lòng nhập thứ tự màu: BLUE \u2794 RED \u2794 GREEN\n(Bấm nút chuột hoặc phím số [1] [2] [3])";
            inst.statusText.color = new Color(0.3f, 0.9f, 1f);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f; // Tam dung game khi dang giai puzzle
    }

    public static void SetStatus(string text, Color color)
    {
        if (Instance != null && Instance.statusText != null)
        {
            Instance.statusText.text = text;
            Instance.statusText.color = color;
        }
    }

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
    }

    public void ClosePuzzle()
    {
        if (puzzlePanel != null) puzzlePanel.SetActive(false);
        currentPuzzle = null;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Time.timeScale = 1f;

        // Hien lai prompt tuong tac
        GameplayUI.Instance?.SetPromptVisible(true);
    }

    private void CreateFallbackPanel(Canvas canvas)
    {
        Font arial = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (arial == null) arial = Resources.GetBuiltinResource<Font>("Arial.ttf");

        // Panel goc
        GameObject pnl = new GameObject("AccessCodeUIPanel");
        pnl.transform.SetParent(canvas.transform, false);
        var rt = pnl.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(560, 420);

        var img = pnl.AddComponent<Image>();
        img.color = new Color(0.04f, 0.08f, 0.16f, 0.96f);

        // Header Title
        GameObject titleGO = new GameObject("ACTitle");
        titleGO.transform.SetParent(pnl.transform, false);
        var titleRt = titleGO.AddComponent<RectTransform>();
        titleRt.anchoredPosition = new Vector2(0, 150);
        titleRt.sizeDelta = new Vector2(500, 50);
        titleText = titleGO.AddComponent<Text>();
        titleText.font = arial;
        titleText.fontSize = 24;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(0.4f, 0.85f, 1f);
        titleText.text = "TERMINAL BẢO MẬT - MÃ MÀU CỬA THOÁT";

        // Status Text
        GameObject statusGO = new GameObject("ACStatus");
        statusGO.transform.SetParent(pnl.transform, false);
        var statusRt = statusGO.AddComponent<RectTransform>();
        statusRt.anchoredPosition = new Vector2(0, 85);
        statusRt.sizeDelta = new Vector2(520, 60);
        statusText = statusGO.AddComponent<Text>();
        statusText.font = arial;
        statusText.fontSize = 16;
        statusText.alignment = TextAnchor.MiddleCenter;
        statusText.color = new Color(0.2f, 0.9f, 1f);
        statusText.text = "Vui lòng nhập thứ tự màu: BLUE \u2794 RED \u2794 GREEN\n(Bấm nút chuột hoặc phím số [1] [2] [3])";

        // Nut 1: BLUE
        blueButton = CreateButton(pnl.transform, "BlueBtn", "1. BLUE [1]", new Vector2(-155, -15), new Vector2(140, 65), new Color(0.12f, 0.38f, 0.95f), arial);

        // Nut 2: RED
        redButton = CreateButton(pnl.transform, "RedBtn", "2. RED [2]", new Vector2(0, -15), new Vector2(140, 65), new Color(0.92f, 0.15f, 0.15f), arial);

        // Nut 3: GREEN
        greenButton = CreateButton(pnl.transform, "GreenBtn", "3. GREEN [3]", new Vector2(155, -15), new Vector2(140, 65), new Color(0.12f, 0.78f, 0.25f), arial);

        // Nut Close
        closePuzzleButton = CreateButton(pnl.transform, "CloseACBtn", "[ ĐÓNG TERMINAL (ESC) ]", new Vector2(0, -135), new Vector2(280, 50), new Color(0.25f, 0.28f, 0.35f), arial);

        puzzlePanel = pnl;
        BindButtonListeners();
        pnl.SetActive(false);
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor, Font font)
    {
        GameObject btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var rt = btnGO.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        var btn = btnGO.AddComponent<Button>();
        var colors = btn.colors;
        colors.highlightedColor = bgColor * 1.25f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;

        GameObject txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtRt = txtGO.AddComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = Vector2.zero;
        txtRt.offsetMax = Vector2.zero;

        var txt = txtGO.AddComponent<Text>();
        txt.font = font;
        txt.fontSize = 17;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.text = label;

        return btn;
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
