using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameplayUI: Quan ly HUD gameplay (HP bar, objective text, win/gameover screen, crosshair).
/// Attach vao: GameplayCanvas / GameplayUI GameObject
/// </summary>
public class GameplayUI : MonoBehaviour
{
    public static GameplayUI Instance { get; private set; }

    [Header("HP")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private Text hpText;
    [SerializeField] private Image hpFillImage;

    [Header("Objective")]
    [SerializeField] private Text objectiveText;
    [SerializeField] private GameObject objectivePanel;

    [Header("Win Screen")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button goMenuButton;

    [Header("Interaction Prompt")]
    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private Text interactionPromptText;

    private GameObject crosshairGO;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AccessCodeUI.EnsureEventSystem();

        Canvas c = GetComponentInParent<Canvas>();
        if (c == null) c = FindFirstObjectByType<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null)
        {
            c.gameObject.AddComponent<GraphicRaycaster>();
        }

        if (interactionPromptPanel != null)
        {
            var img = interactionPromptPanel.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }
        if (interactionPromptText != null)
        {
            interactionPromptText.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHP;
        PlayerHealth.OnPlayerDied += ShowGameOver;
        GameManager.OnGameWin += ShowWin;
        ObjectiveManager.OnCurrentObjectiveChanged += UpdateObjective;
        PlayerInteraction.OnInteractableFound += ShowPrompt;
        PlayerInteraction.OnInteractableLost += HidePrompt;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHP;
        PlayerHealth.OnPlayerDied -= ShowGameOver;
        GameManager.OnGameWin -= ShowWin;
        ObjectiveManager.OnCurrentObjectiveChanged -= UpdateObjective;
        PlayerInteraction.OnInteractableFound -= ShowPrompt;
        PlayerInteraction.OnInteractableLost -= HidePrompt;
        gameOverCanvasGO = null;
    }

    private void Start()
    {
        CleanUpDuplicateUI();

        // Đảm bảo Canvas và toàn bộ các thành phần HUD luôn hiển thị đầy đủ
        EnsureCanvasIsActive();
        EnsureHealthBar();
        EnsureObjectivePanel();
        EnsureInteractionPrompt();
        EnsureNotificationUI();
        EnsureCrosshair();
        EnsureInventoryUI();
        EnsureGameOverPanel();
        EnsureWinPanel();

        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null && !isGameOverShown) gameOverPanel.SetActive(false);
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
    }

    /// <summary>
    /// Tiêu diệt triệt để các Canvas hoặc Panel bị nhân đôi do setup cũ, loại bỏ hoàn toàn lỗi đè chữ và che khuất thanh máu.
    /// </summary>
    public void CleanUpDuplicateUI()
    {
        Canvas myCanvas = GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
        Transform canvasRoot = myCanvas != null ? myCanvas.transform : transform;

        // 1. Quét tìm và tiêu diệt các Panel trùng lặp ngoài canvas chính
        var allPanels = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        Transform mainHP = canvasRoot.Find("HPPanel");
        Transform mainObj = canvasRoot.Find("ObjectivePanel");

        // Tiêu diệt triệt để TẤT CẢ các InventoryPanel cũ trong toàn bộ Scene để tránh panel ma che khuất
        for (int i = canvasRoot.childCount - 1; i >= 0; i--)
        {
            var child = canvasRoot.GetChild(i);
            if (child.name == "InventoryPanel")
            {
                DestroyImmediate(child.gameObject);
            }
        }

        foreach (var t in allPanels)
        {
            if (t == null) continue;
            if (t.gameObject.name == "HPPanel" && t != mainHP && !t.IsChildOf(canvasRoot))
            {
                DestroyImmediate(t.gameObject);
            }
            else if (t.gameObject.name == "ObjectivePanel" && t != mainObj && !t.IsChildOf(canvasRoot))
            {
                DestroyImmediate(t.gameObject);
            }
            else if (t.gameObject.name == "InventoryPanel")
            {
                DestroyImmediate(t.gameObject);
            }
        }

        // Tiêu diệt tất cả InventoryUI thừa ngoài canvas chính
        var myInvUI = GetComponent<InventoryUI>();
        var allInv = FindObjectsByType<InventoryUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var inv in allInv)
        {
            if (inv != null && inv != myInvUI && inv.gameObject != myCanvas.gameObject)
            {
                DestroyImmediate(inv);
            }
        }

        // 2. Tắt hoàn toàn MobileInputCanvas
        var allCanvases = FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var c in allCanvases)
        {
            if (c != null && c != myCanvas && (c.name.Contains("Mobile") || c.name.Contains("Touch")))
            {
                c.enabled = false;
                c.gameObject.SetActive(false);
            }
        }
    }

    private void EnsureCanvasIsActive()
    {
        Canvas c = GetComponentInParent<Canvas>();
        if (c != null)
        {
            if (!c.enabled) c.enabled = true;
            if (!c.gameObject.activeSelf) c.gameObject.SetActive(true);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            var inv = InventoryUI.Instance ?? GetComponent<InventoryUI>() ?? FindFirstObjectByType<InventoryUI>();
            inv?.ToggleInventory();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (InventoryUI.IsOpen)
            {
                var inv = InventoryUI.Instance ?? GetComponent<InventoryUI>() ?? FindFirstObjectByType<InventoryUI>();
                inv?.CloseInventory();
            }
        }

        // An tam ngam Crosshair khi mo cac menu/modal/inventory
        if (crosshairGO != null)
        {
            bool modalOpen = AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen || InventoryUI.IsOpen ||
                (winPanel != null && winPanel.activeSelf) || (gameOverPanel != null && gameOverPanel.activeSelf);
            crosshairGO.SetActive(!modalOpen);
        }
    }

    /// <summary>
    /// Tự động thiết lập đầy đủ các layer visual của thanh máu chuẩn Màn 2 (HPPanel, Slider, Fill, Text)
    /// </summary>
    public void EnsureHealthBar()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform;

        // Tiêu diệt HPPanel trùng lặp trong cùng Canvas
        for (int i = canvasRoot.childCount - 1; i >= 0; i--)
        {
            var child = canvasRoot.GetChild(i);
            if (child.name == "HPPanel")
            {
                int firstIdx = -1;
                for (int j = 0; j < canvasRoot.childCount; j++)
                {
                    if (canvasRoot.GetChild(j).name == "HPPanel") { firstIdx = j; break; }
                }
                if (i != firstIdx)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        // 1. Tìm hoặc tạo HPPanel
        Transform hpPanelTrans = canvasRoot.Find("HPPanel");
        if (hpPanelTrans == null)
        {
            var pnlGO = new GameObject("HPPanel");
            pnlGO.transform.SetParent(canvasRoot, false);
            hpPanelTrans = pnlGO.transform;
        }

        var hpPnl = hpPanelTrans.gameObject;
        if (!hpPnl.activeSelf) hpPnl.SetActive(true);

        var pnlR = hpPnl.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(0f, 1f);
        pnlR.anchorMax = new Vector2(0f, 1f);
        pnlR.pivot = new Vector2(0f, 1f);
        pnlR.anchoredPosition = new Vector2(20f, -20f);
        pnlR.sizeDelta = new Vector2(290f, 62f);

        var pnlImg = hpPnl.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);
        pnlImg.raycastTarget = false;

        var pnlOl = hpPnl.GetOrAddComponent<Outline>();
        pnlOl.effectColor = new Color(0.1f, 0.45f, 0.75f, 0.7f);
        pnlOl.effectDistance = new Vector2(1.5f, -1.5f);

        // 2. Tìm hoặc tạo Slider
        Transform sliderTrans = hpPanelTrans.Find("HPSlider");
        if (sliderTrans == null)
        {
            var sGO = new GameObject("HPSlider");
            sGO.transform.SetParent(hpPanelTrans, false);
            sliderTrans = sGO.transform;
        }
        hpBar = sliderTrans.gameObject.GetOrAddComponent<Slider>();
        hpBar.interactable = false;
        hpBar.transition = Selectable.Transition.None;

        RectTransform slR = sliderTrans.gameObject.GetOrAddComponent<RectTransform>();
        slR.anchorMin = new Vector2(0f, 0f);
        slR.anchorMax = new Vector2(1f, 0f);
        slR.pivot = new Vector2(0.5f, 0f);
        slR.anchoredPosition = new Vector2(0, 10);
        slR.sizeDelta = new Vector2(-24, 18);

        Image bg = sliderTrans.gameObject.GetOrAddComponent<Image>();
        bg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);
        bg.raycastTarget = false;

        // 3. FillArea
        Transform fa = sliderTrans.Find("FillArea") ?? sliderTrans.Find("Fill Area");
        if (fa == null)
        {
            var faGO = new GameObject("FillArea");
            faGO.transform.SetParent(sliderTrans, false);
            fa = faGO.transform;
        }
        RectTransform faR = fa.gameObject.GetOrAddComponent<RectTransform>();
        faR.anchorMin = Vector2.zero;
        faR.anchorMax = Vector2.one;
        faR.offsetMin = new Vector2(2, 2);
        faR.offsetMax = new Vector2(-2, -2);

        // 4. Fill Image
        Transform fi = fa.Find("Fill");
        if (fi == null)
        {
            var fiGO = new GameObject("Fill");
            fiGO.transform.SetParent(fa, false);
            fi = fiGO.transform;
        }
        RectTransform fiR = fi.gameObject.GetOrAddComponent<RectTransform>();
        fiR.anchorMin = Vector2.zero;
        fiR.anchorMax = Vector2.one;
        fiR.offsetMin = Vector2.zero;
        fiR.offsetMax = Vector2.zero;

        hpFillImage = fi.gameObject.GetOrAddComponent<Image>();
        hpFillImage.color = new Color(0.2f, 0.92f, 0.38f);
        hpFillImage.raycastTarget = false;
        hpBar.fillRect = fiR;

        // 5. HPText
        Text keeperTxt = null;
        var allTxts = hpPanelTrans.GetComponentsInChildren<Text>(true);
        foreach (var t in allTxts)
        {
            if (keeperTxt == null && (t.name == "HPText" || t.name == "Text"))
            {
                keeperTxt = t;
                keeperTxt.name = "HPText";
            }
            else
            {
                DestroyImmediate(t.gameObject);
            }
        }

        Transform txtTrans = keeperTxt != null ? keeperTxt.transform : hpPanelTrans.Find("HPText");
        if (txtTrans == null)
        {
            var tGO = new GameObject("HPText");
            tGO.transform.SetParent(hpPanelTrans, false);
            txtTrans = tGO.transform;
        }
        hpText = txtTrans.gameObject.GetOrAddComponent<Text>();
        hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpText.fontSize = 15;
        hpText.fontStyle = FontStyle.Bold;
        hpText.alignment = TextAnchor.MiddleCenter;
        hpText.color = Color.white;
        hpText.raycastTarget = false;
        hpText.text = "HP: 100 / 100";

        RectTransform txtR = txtTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = new Vector2(0.5f, 1f);
        txtR.anchorMax = new Vector2(0.5f, 1f);
        txtR.pivot = new Vector2(0.5f, 1f);
        txtR.anchoredPosition = new Vector2(0, -6);
        txtR.sizeDelta = new Vector2(260, 24);

        var ol = hpText.gameObject.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.95f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        txtTrans.SetAsLastSibling();

        // Cập nhật giá trị máu ban đầu
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            UpdateHP(ph.CurrentHealth, ph.MaxHealth);
        }
    }

    /// <summary>
    /// Đảm bảo bảng nhiệm vụ (ObjectivePanel) hiển thị rõ ràng trên Canvas, loại bỏ chữ đè chữ
    /// </summary>
    public void EnsureObjectivePanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform;

        // Tiêu diệt ObjectivePanel trùng lặp trong cùng Canvas
        for (int i = canvasRoot.childCount - 1; i >= 0; i--)
        {
            var child = canvasRoot.GetChild(i);
            if (child.name == "ObjectivePanel")
            {
                int firstIdx = -1;
                for (int j = 0; j < canvasRoot.childCount; j++)
                {
                    if (canvasRoot.GetChild(j).name == "ObjectivePanel") { firstIdx = j; break; }
                }
                if (i != firstIdx)
                {
                    DestroyImmediate(child.gameObject);
                }
            }
        }

        Transform objTrans = canvasRoot.Find("ObjectivePanel");
        if (objTrans == null)
        {
            var pGO = new GameObject("ObjectivePanel");
            pGO.transform.SetParent(canvasRoot, false);
            objTrans = pGO.transform;
        }

        objectivePanel = objTrans.gameObject;
        if (!objectivePanel.activeSelf) objectivePanel.SetActive(true);

        var pnlR = objectivePanel.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(1f, 1f);
        pnlR.anchorMax = new Vector2(1f, 1f);
        pnlR.pivot = new Vector2(1f, 1f);
        pnlR.anchoredPosition = new Vector2(-20f, -20f);
        pnlR.sizeDelta = new Vector2(340f, 85f);

        var pnlImg = objectivePanel.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        pnlImg.raycastTarget = false;

        var ol = objectivePanel.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0.1f, 0.5f, 0.9f, 0.7f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // Xóa sạch mọi Text thừa để tránh tình trạng chữ đè chữ
        Text keeperTxt = null;
        var allTxts = objTrans.GetComponentsInChildren<Text>(true);
        foreach (var t in allTxts)
        {
            if (keeperTxt == null && (t.name == "ObjectiveText" || t.name == "Text"))
            {
                keeperTxt = t;
                keeperTxt.name = "ObjectiveText";
            }
            else
            {
                DestroyImmediate(t.gameObject);
            }
        }

        Transform txtTrans = keeperTxt != null ? keeperTxt.transform : objTrans.Find("ObjectiveText");
        if (txtTrans == null)
        {
            var tGO = new GameObject("ObjectiveText");
            tGO.transform.SetParent(objTrans, false);
            txtTrans = tGO.transform;
        }

        objectiveText = txtTrans.gameObject.GetOrAddComponent<Text>();
        objectiveText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        objectiveText.fontSize = 15;
        objectiveText.fontStyle = FontStyle.Normal;
        objectiveText.alignment = TextAnchor.MiddleCenter;
        objectiveText.color = Color.white;
        objectiveText.raycastTarget = false;

        RectTransform txtR = txtTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = Vector2.zero;
        txtR.anchorMax = Vector2.one;
        txtR.offsetMin = new Vector2(10, 10);
        txtR.offsetMax = new Vector2(-10, -10);

        if (ObjectiveManager.Instance != null)
        {
            UpdateObjective(ObjectiveManager.Instance.GetCurrentObjective());
        }
        else
        {
            objectiveText.text = "OBJECTIVE\nTìm Thẻ Bảo Mật & Cầu Chì để mở lối thoát!";
        }
    }

    /// <summary>
    /// Đảm bảo thanh chỉ dẫn tương tác (InteractionPrompt: "Nhặt item [E]", "Thao tác [E]")
    /// </summary>
    public void EnsureInteractionPrompt()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform;

        Transform promptTrans = canvasRoot.Find("InteractionPrompt");
        if (promptTrans == null)
        {
            var pGO = new GameObject("InteractionPrompt");
            pGO.transform.SetParent(canvasRoot, false);
            promptTrans = pGO.transform;
        }

        interactionPromptPanel = promptTrans.gameObject;
        var pnlR = interactionPromptPanel.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(0.5f, 0f);
        pnlR.anchorMax = new Vector2(0.5f, 0f);
        pnlR.pivot = new Vector2(0.5f, 0f);
        pnlR.anchoredPosition = new Vector2(0f, 160f);
        pnlR.sizeDelta = new Vector2(400f, 54f);

        var pnlImg = interactionPromptPanel.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.05f, 0.08f, 0.15f, 0.94f);
        pnlImg.raycastTarget = false;

        var ol = interactionPromptPanel.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0f, 0.85f, 1f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Transform txtTrans = promptTrans.Find("PromptText") ?? promptTrans.Find("Text");
        if (txtTrans == null)
        {
            var tGO = new GameObject("PromptText");
            tGO.transform.SetParent(promptTrans, false);
            txtTrans = tGO.transform;
        }

        interactionPromptText = txtTrans.gameObject.GetOrAddComponent<Text>();
        interactionPromptText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        interactionPromptText.fontSize = 17;
        interactionPromptText.fontStyle = FontStyle.Bold;
        interactionPromptText.alignment = TextAnchor.MiddleCenter;
        interactionPromptText.color = new Color(1f, 0.92f, 0.3f);
        interactionPromptText.raycastTarget = false;

        RectTransform txtR = txtTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = Vector2.zero;
        txtR.anchorMax = Vector2.one;
        txtR.offsetMin = Vector2.zero;
        txtR.offsetMax = Vector2.zero;

        interactionPromptPanel.SetActive(false);
    }

    /// <summary>
    /// Đảm bảo bảng thông báo NotificationUI luôn có mặt trên màn hình
    /// </summary>
    public void EnsureNotificationUI()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform;

        NotificationUI nui = FindFirstObjectByType<NotificationUI>();
        if (nui == null)
        {
            var nuiGO = new GameObject("NotificationUI");
            nuiGO.transform.SetParent(canvasRoot, false);
            nui = nuiGO.AddComponent<NotificationUI>();
        }

        Transform notifTrans = canvasRoot.Find("NotificationPanel");
        if (notifTrans == null)
        {
            var pGO = new GameObject("NotificationPanel");
            pGO.transform.SetParent(canvasRoot, false);
            notifTrans = pGO.transform;
        }

        var pnl = notifTrans.gameObject;
        var pnlR = pnl.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(0.5f, 1f);
        pnlR.anchorMax = new Vector2(0.5f, 1f);
        pnlR.pivot = new Vector2(0.5f, 1f);
        pnlR.anchoredPosition = new Vector2(0f, -90f);
        pnlR.sizeDelta = new Vector2(540f, 60f);

        var pnlImg = pnl.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.04f, 0.12f, 0.25f, 0.95f);
        pnlImg.raycastTarget = false;

        var ol = pnl.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0f, 0.8f, 1f, 0.9f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Transform txtTrans = notifTrans.Find("NotifText") ?? notifTrans.Find("Text");
        if (txtTrans == null)
        {
            var tGO = new GameObject("NotifText");
            tGO.transform.SetParent(notifTrans, false);
            txtTrans = tGO.transform;
        }

        var txt = txtTrans.gameObject.GetOrAddComponent<Text>();
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 17;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;

        RectTransform txtR = txtTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = Vector2.zero;
        txtR.anchorMax = Vector2.one;
        txtR.offsetMin = Vector2.zero;
        txtR.offsetMax = Vector2.zero;

        nui.SetNotificationReferences(pnl, txt);
    }

    public void EnsureInventoryUI()
    {
        var inv = GetComponent<InventoryUI>();
        if (inv == null) inv = FindFirstObjectByType<InventoryUI>();
        if (inv == null) inv = gameObject.AddComponent<InventoryUI>();
        inv.EnsureInventoryPanel();
    }

    private void EnsureCrosshair()
    {
        if (crosshairGO == null)
        {
            crosshairGO = new GameObject("HUD_Crosshair");
            crosshairGO.transform.SetParent(transform, false);
            var rt = crosshairGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(8, 8);
            rt.anchoredPosition = Vector2.zero;

            var img = crosshairGO.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);
            img.raycastTarget = false;

            var ol = crosshairGO.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.8f);
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }

    private void UpdateHP(int current, int max)
    {
        float pct = max > 0 ? (float)current / max : 0f;
        if (hpBar != null) hpBar.value = pct;
        if (hpText != null) hpText.text = $"HP: {current} / {max}";

        if (hpFillImage != null)
        {
            if (pct > 0.55f)
                hpFillImage.color = new Color(0.2f, 0.92f, 0.38f); // Xanh la tuoi
            else if (pct > 0.25f)
                hpFillImage.color = new Color(1.0f, 0.72f, 0.1f);  // Vang cam canh bao
            else
                hpFillImage.color = new Color(0.95f, 0.2f, 0.2f);  // Do nguy hiem
        }
    }

    private void UpdateObjective(ObjectiveManager.Objective obj)
    {
        if (objectiveText == null) return;
        objectiveText.text = obj != null ? $"OBJECTIVE\n{obj.description}" : "OBJECTIVE\nEscape!";
    }

    private void ShowWin()
    {
        EnsureWinPanel();
        AccessCodeUI.ForceClose();
        TerminalUI.ForceClose();
        ElevatorKeypadUI.ForceClose();
        if (InventoryUI.IsOpen)
        {
            var inv = GetComponent<InventoryUI>() ?? FindFirstObjectByType<InventoryUI>();
            inv?.CloseInventory();
        }

        if (winPanel != null)
        {
            winPanel.SetActive(true);
            winPanel.transform.SetAsLastSibling();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;
    }

    private static GameObject gameOverCanvasGO;
    private bool isGameOverShown = false;

    /// <summary>
    /// Tìm hoặc tạo GameOverCanvas và hiển thị popup Game Over.
    /// </summary>
    public static bool ForceShowGameOver()
    {
        EnsureGameOverUI();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        return true;
    }

    public void ShowGameOver()
    {
        isGameOverShown = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        EnsureGameOverUI();

        // Đóng các popup khác an toàn
        try { AccessCodeUI.ForceClose(); } catch { }
        try { TerminalUI.ForceClose(); } catch { }
        try { ElevatorKeypadUI.ForceClose(); } catch { }
        try
        {
            if (InventoryUI.IsOpen)
            {
                var inv = GetComponent<InventoryUI>() ?? FindFirstObjectByType<InventoryUI>();
                inv?.CloseInventory();
            }
        }
        catch { }

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void EnsureGameOverPanel()
    {
        // Khi Start(), không hiện popup trừ khi đã game over
        if (isGameOverShown)
        {
            EnsureGameOverUI();
        }
    }

    public static GameObject EnsureGameOverUI()
    {
        AccessCodeUI.EnsureEventSystem();

        if (gameOverCanvasGO != null)
        {
            gameOverCanvasGO.SetActive(true);
            gameOverCanvasGO.transform.SetAsLastSibling();
            return gameOverCanvasGO;
        }

        var existing = GameObject.Find("GameOverCanvas_Global");
        if (existing != null)
        {
            gameOverCanvasGO = existing;
            gameOverCanvasGO.SetActive(true);
            gameOverCanvasGO.transform.SetAsLastSibling();
            return gameOverCanvasGO;
        }

        // Tạo Canvas độc lập trên cùng (ScreenSpaceOverlay, sortingOrder 99999)
        gameOverCanvasGO = new GameObject("GameOverCanvas_Global");
        var canvas = gameOverCanvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 99999;

        var scaler = gameOverCanvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        gameOverCanvasGO.AddComponent<GraphicRaycaster>();

        // 1. Nền mờ tối che kín toàn màn hình
        var bgGO = new GameObject("BackgroundFade");
        bgGO.transform.SetParent(gameOverCanvasGO.transform, false);
        var bgR = bgGO.AddComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero;
        bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero;
        bgR.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.03f, 0.015f, 0.015f, 0.94f);
        bgImg.raycastTarget = true;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 2. Dialog Box ở giữa màn hình
        var boxGO = new GameObject("DialogBox");
        boxGO.transform.SetParent(gameOverCanvasGO.transform, false);
        var boxR = boxGO.AddComponent<RectTransform>();
        boxR.anchorMin = new Vector2(0.5f, 0.5f);
        boxR.anchorMax = new Vector2(0.5f, 0.5f);
        boxR.pivot = new Vector2(0.5f, 0.5f);
        boxR.anchoredPosition = Vector2.zero;
        boxR.sizeDelta = new Vector2(620f, 430f);

        var boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.08f, 0.09f, 0.13f, 0.98f);
        boxImg.raycastTarget = true;

        var boxOl = boxGO.AddComponent<Outline>();
        boxOl.effectColor = new Color(0.95f, 0.22f, 0.22f, 0.9f);
        boxOl.effectDistance = new Vector2(2.5f, -2.5f);

        // 3. Tiêu đề THẤT BẠI
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(boxGO.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0f, 1f);
        titleR.anchorMax = new Vector2(1f, 1f);
        titleR.pivot = new Vector2(0.5f, 1f);
        titleR.anchoredPosition = new Vector2(0f, -30f);
        titleR.sizeDelta = new Vector2(-40f, 60f);

        var titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "THẤT BẠI";
        titleTxt.font = defaultFont;
        titleTxt.fontSize = 42;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(1f, 0.25f, 0.25f);
        titleTxt.raycastTarget = false;

        var titleOl = titleGO.AddComponent<Outline>();
        titleOl.effectColor = Color.black;
        titleOl.effectDistance = new Vector2(2f, -2f);

        // 4. Phụ đề
        var subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(boxGO.transform, false);
        var subR = subGO.AddComponent<RectTransform>();
        subR.anchorMin = new Vector2(0f, 1f);
        subR.anchorMax = new Vector2(1f, 1f);
        subR.pivot = new Vector2(0.5f, 1f);
        subR.anchoredPosition = new Vector2(0f, -95f);
        subR.sizeDelta = new Vector2(-40f, 35f);

        var subTxt = subGO.AddComponent<Text>();
        subTxt.text = "LƯỢNG MÁU ĐÃ CẠN KIỆT (0 HP)!";
        subTxt.font = defaultFont;
        subTxt.fontSize = 19;
        subTxt.fontStyle = FontStyle.Bold;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = Color.white;
        subTxt.raycastTarget = false;

        // 5. Mô tả
        var descGO = new GameObject("DescText");
        descGO.transform.SetParent(boxGO.transform, false);
        var descR = descGO.AddComponent<RectTransform>();
        descR.anchorMin = new Vector2(0f, 1f);
        descR.anchorMax = new Vector2(1f, 1f);
        descR.pivot = new Vector2(0.5f, 1f);
        descR.anchoredPosition = new Vector2(0f, -140f);
        descR.sizeDelta = new Vector2(-60f, 40f);

        var descTxt = descGO.AddComponent<Text>();
        descTxt.text = "Bạn đã bị robot an ninh hạ gục. Hãy thử lại để tiếp tục cuộc trốn thoát!";
        descTxt.font = defaultFont;
        descTxt.fontSize = 15;
        descTxt.fontStyle = FontStyle.Normal;
        descTxt.alignment = TextAnchor.MiddleCenter;
        descTxt.color = new Color(0.75f, 0.8f, 0.85f);
        descTxt.raycastTarget = false;

        // 6. Nút 🔄 CHƠI LẠI
        var retryGO = new GameObject("RetryButton");
        retryGO.transform.SetParent(boxGO.transform, false);
        var retryR = retryGO.AddComponent<RectTransform>();
        retryR.anchorMin = new Vector2(0.5f, 0.5f);
        retryR.anchorMax = new Vector2(0.5f, 0.5f);
        retryR.pivot = new Vector2(0.5f, 0.5f);
        retryR.anchoredPosition = new Vector2(0f, -30f);
        retryR.sizeDelta = new Vector2(340f, 54f);

        var retryImg = retryGO.AddComponent<Image>();
        retryImg.color = new Color(0.85f, 0.2f, 0.18f);

        var retryOl = retryGO.AddComponent<Outline>();
        retryOl.effectColor = new Color(1f, 0.6f, 0.2f, 0.9f);
        retryOl.effectDistance = new Vector2(1.5f, -1.5f);

        var retryBtn = retryGO.AddComponent<Button>();
        retryBtn.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            string curScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            Debug.Log($"[GameOverUI] Retry clicked -> Reloading scene: {curScene}");
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(curScene);
        });

        var rTxtGO = new GameObject("Text");
        rTxtGO.transform.SetParent(retryGO.transform, false);
        var rTxtR = rTxtGO.AddComponent<RectTransform>();
        rTxtR.anchorMin = Vector2.zero;
        rTxtR.anchorMax = Vector2.one;
        rTxtR.offsetMin = Vector2.zero;
        rTxtR.offsetMax = Vector2.zero;

        var rTxt = rTxtGO.AddComponent<Text>();
        rTxt.text = "🔄 CHƠI LẠI";
        rTxt.font = defaultFont;
        rTxt.fontSize = 20;
        rTxt.fontStyle = FontStyle.Bold;
        rTxt.alignment = TextAnchor.MiddleCenter;
        rTxt.color = Color.white;
        rTxt.raycastTarget = false;

        // 7. Nút 🏠 MENU CHÍNH
        var menuGO = new GameObject("MainMenuButton");
        menuGO.transform.SetParent(boxGO.transform, false);
        var menuR = menuGO.AddComponent<RectTransform>();
        menuR.anchorMin = new Vector2(0.5f, 0.5f);
        menuR.anchorMax = new Vector2(0.5f, 0.5f);
        menuR.pivot = new Vector2(0.5f, 0.5f);
        menuR.anchoredPosition = new Vector2(0f, -100f);
        menuR.sizeDelta = new Vector2(340f, 48f);

        var menuImg = menuGO.AddComponent<Image>();
        menuImg.color = new Color(0.18f, 0.24f, 0.35f);

        var menuOl = menuGO.AddComponent<Outline>();
        menuOl.effectColor = new Color(0.2f, 0.7f, 1f, 0.7f);
        menuOl.effectDistance = new Vector2(1.5f, -1.5f);

        var menuBtn = menuGO.AddComponent<Button>();
        menuBtn.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            Debug.Log("[GameOverUI] MainMenu clicked");
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });

        var mTxtGO = new GameObject("Text");
        mTxtGO.transform.SetParent(menuGO.transform, false);
        var mTxtR = mTxtGO.AddComponent<RectTransform>();
        mTxtR.anchorMin = Vector2.zero;
        mTxtR.anchorMax = Vector2.one;
        mTxtR.offsetMin = Vector2.zero;
        mTxtR.offsetMax = Vector2.zero;

        var mTxt = mTxtGO.AddComponent<Text>();
        mTxt.text = "🏠 MENU CHÍNH";
        mTxt.font = defaultFont;
        mTxt.fontSize = 17;
        mTxt.fontStyle = FontStyle.Bold;
        mTxt.alignment = TextAnchor.MiddleCenter;
        mTxt.color = Color.white;
        mTxt.raycastTarget = false;

        return gameOverCanvasGO;
    }

    public void EnsureWinPanel()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform canvasRoot = canvas != null ? canvas.transform : transform;

        Transform pnlTrans = canvasRoot.Find("WinPanel");
        if (pnlTrans == null)
        {
            var pGO = new GameObject("WinPanel");
            pGO.transform.SetParent(canvasRoot, false);
            pnlTrans = pGO.transform;
        }

        winPanel = pnlTrans.gameObject;
        var pnlR = winPanel.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = Vector2.zero;
        pnlR.anchorMax = Vector2.one;
        pnlR.offsetMin = Vector2.zero;
        pnlR.offsetMax = Vector2.zero;

        var pnlImg = winPanel.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.02f, 0.05f, 0.04f, 0.94f);
        pnlImg.raycastTarget = true;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // Dialog Box (Center)
        Transform boxTrans = pnlTrans.Find("DialogBox");
        if (boxTrans == null)
        {
            var bGO = new GameObject("DialogBox");
            bGO.transform.SetParent(pnlTrans, false);
            boxTrans = bGO.transform;
        }
        var boxR = boxTrans.gameObject.GetOrAddComponent<RectTransform>();
        boxR.anchorMin = new Vector2(0.5f, 0.5f);
        boxR.anchorMax = new Vector2(0.5f, 0.5f);
        boxR.pivot = new Vector2(0.5f, 0.5f);
        boxR.anchoredPosition = Vector2.zero;
        boxR.sizeDelta = new Vector2(580f, 380f);

        var boxImg = boxTrans.gameObject.GetOrAddComponent<Image>();
        boxImg.color = new Color(0.06f, 0.12f, 0.1f, 0.98f);
        boxImg.raycastTarget = true;

        var boxOl = boxTrans.gameObject.GetOrAddComponent<Outline>();
        boxOl.effectColor = new Color(0.2f, 0.9f, 0.45f, 0.9f);
        boxOl.effectDistance = new Vector2(2f, -2f);

        // Title Text
        Transform titleTrans = boxTrans.Find("TitleText");
        if (titleTrans == null)
        {
            var tGO = new GameObject("TitleText");
            tGO.transform.SetParent(boxTrans, false);
            titleTrans = tGO.transform;
        }
        var titleR = titleTrans.gameObject.GetOrAddComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0f, 1f);
        titleR.anchorMax = new Vector2(1f, 1f);
        titleR.pivot = new Vector2(0.5f, 1f);
        titleR.anchoredPosition = new Vector2(0f, -30f);
        titleR.sizeDelta = new Vector2(-40f, 50f);

        var titleTxt = titleTrans.gameObject.GetOrAddComponent<Text>();
        titleTxt.text = "VƯỢT MÀN THÀNH CÔNG!";
        titleTxt.font = defaultFont;
        titleTxt.fontSize = 32;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.3f, 1f, 0.5f);
        titleTxt.raycastTarget = false;

        var titleOl = titleTrans.gameObject.GetOrAddComponent<Outline>();
        titleOl.effectColor = Color.black;
        titleOl.effectDistance = new Vector2(2f, -2f);

        // Subtitle Text
        Transform subTrans = boxTrans.Find("SubtitleText");
        if (subTrans == null)
        {
            var sGO = new GameObject("SubtitleText");
            sGO.transform.SetParent(boxTrans, false);
            subTrans = sGO.transform;
        }
        var subR = subTrans.gameObject.GetOrAddComponent<RectTransform>();
        subR.anchorMin = new Vector2(0f, 1f);
        subR.anchorMax = new Vector2(1f, 1f);
        subR.pivot = new Vector2(0.5f, 1f);
        subR.anchoredPosition = new Vector2(0f, -85f);
        subR.sizeDelta = new Vector2(-40f, 32f);

        var subTxt = subTrans.gameObject.GetOrAddComponent<Text>();
        subTxt.text = "BẠN ĐÃ TRỐN THOÁT THÀNH CÔNG!";
        subTxt.font = defaultFont;
        subTxt.fontSize = 17;
        subTxt.fontStyle = FontStyle.Bold;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = Color.white;
        subTxt.raycastTarget = false;

        // Play Again Button
        Transform btnTrans = boxTrans.Find("PlayAgainButton");
        if (btnTrans == null)
        {
            var rGO = new GameObject("PlayAgainButton");
            rGO.transform.SetParent(boxTrans, false);
            btnTrans = rGO.transform;
        }
        var btnR = btnTrans.gameObject.GetOrAddComponent<RectTransform>();
        btnR.anchorMin = new Vector2(0.5f, 0.5f);
        btnR.anchorMax = new Vector2(0.5f, 0.5f);
        btnR.pivot = new Vector2(0.5f, 0.5f);
        btnR.anchoredPosition = new Vector2(0f, -25f);
        btnR.sizeDelta = new Vector2(320f, 52f);

        var btnImg = btnTrans.gameObject.GetOrAddComponent<Image>();
        btnImg.color = new Color(0.15f, 0.65f, 0.35f);

        var btnOl = btnTrans.gameObject.GetOrAddComponent<Outline>();
        btnOl.effectColor = new Color(0.3f, 1f, 0.5f, 0.85f);
        btnOl.effectDistance = new Vector2(1.5f, -1.5f);

        playAgainButton = btnTrans.gameObject.GetOrAddComponent<Button>();
        playAgainButton.onClick.RemoveAllListeners();
        playAgainButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.RestartGame();
            else UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        });

        Transform bTxtTrans = btnTrans.Find("Text");
        if (bTxtTrans == null)
        {
            var btGO = new GameObject("Text");
            btGO.transform.SetParent(btnTrans, false);
            bTxtTrans = btGO.transform;
        }
        var bTxtR = bTxtTrans.gameObject.GetOrAddComponent<RectTransform>();
        bTxtR.anchorMin = Vector2.zero;
        bTxtR.anchorMax = Vector2.one;
        bTxtR.offsetMin = Vector2.zero;
        bTxtR.offsetMax = Vector2.zero;

        var bTxt = bTxtTrans.gameObject.GetOrAddComponent<Text>();
        bTxt.text = "🔄 CHƠI LẠI";
        bTxt.font = defaultFont;
        bTxt.fontSize = 18;
        bTxt.fontStyle = FontStyle.Bold;
        bTxt.alignment = TextAnchor.MiddleCenter;
        bTxt.color = Color.white;
        bTxt.raycastTarget = false;

        // Main Menu Button
        Transform wMenuTrans = boxTrans.Find("MainMenuButton");
        if (wMenuTrans == null)
        {
            var mGO = new GameObject("MainMenuButton");
            mGO.transform.SetParent(boxTrans, false);
            wMenuTrans = mGO.transform;
        }
        var wMenuR = wMenuTrans.gameObject.GetOrAddComponent<RectTransform>();
        wMenuR.anchorMin = new Vector2(0.5f, 0.5f);
        wMenuR.anchorMax = new Vector2(0.5f, 0.5f);
        wMenuR.pivot = new Vector2(0.5f, 0.5f);
        wMenuR.anchoredPosition = new Vector2(0f, -95f);
        wMenuR.sizeDelta = new Vector2(320f, 48f);

        var wMenuImg = wMenuTrans.gameObject.GetOrAddComponent<Image>();
        wMenuImg.color = new Color(0.18f, 0.24f, 0.35f);

        var wMenuOl = wMenuTrans.gameObject.GetOrAddComponent<Outline>();
        wMenuOl.effectColor = new Color(0.2f, 0.7f, 1f, 0.7f);
        wMenuOl.effectDistance = new Vector2(1.5f, -1.5f);

        mainMenuButton = wMenuTrans.gameObject.GetOrAddComponent<Button>();
        mainMenuButton.onClick.RemoveAllListeners();
        mainMenuButton.onClick.AddListener(() =>
        {
            Time.timeScale = 1f;
            if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
            else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
        });

        Transform wmTxtTrans = wMenuTrans.Find("Text");
        if (wmTxtTrans == null)
        {
            var wmtGO = new GameObject("Text");
            wmtGO.transform.SetParent(wMenuTrans, false);
            wmTxtTrans = wmtGO.transform;
        }
        var wmTxtR = wmTxtTrans.gameObject.GetOrAddComponent<RectTransform>();
        wmTxtR.anchorMin = Vector2.zero;
        wmTxtR.anchorMax = Vector2.one;
        wmTxtR.offsetMin = Vector2.zero;
        wmTxtR.offsetMax = Vector2.zero;

        var wmTxt = wmTxtTrans.gameObject.GetOrAddComponent<Text>();
        wmTxt.text = "🏠 MENU CHÍNH";
        wmTxt.font = defaultFont;
        wmTxt.fontSize = 16;
        wmTxt.fontStyle = FontStyle.Bold;
        wmTxt.alignment = TextAnchor.MiddleCenter;
        wmTxt.color = Color.white;
        wmTxt.raycastTarget = false;

        winPanel.SetActive(false);
    }

    public void SetPromptVisible(bool visible)
    {
        if (interactionPromptPanel != null)
        {
            if (visible && (AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen))
                return;
            interactionPromptPanel.SetActive(visible);
        }
    }

    private void ShowPrompt(IInteractable interactable)
    {
        if (interactionPromptPanel == null) return;
        if (AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen) return;

        interactionPromptPanel.SetActive(true);
        if (interactionPromptText != null)
            interactionPromptText.text = interactable.InteractPromptText;
    }

    private void HidePrompt()
    {
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playAgainButton != null) playAgainButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();
        if (retryButton != null) retryButton.onClick.RemoveAllListeners();
        if (goMenuButton != null) goMenuButton.onClick.RemoveAllListeners();
    }
}

/// <summary>
/// Tiá»‡n Ã­ch láº¥y hoáº·c thÃªm Component an toÃ n.
/// KHÃ”NG dÃ¹ng "GetComponent<T>() ?? AddComponent<T>()" vÃ¬ Unity tráº£ vá» "fake null"
/// (toÃ¡n tá»­ ?? khÃ´ng nháº­n ra), khiáº¿n AddComponent khÃ´ng bao giá» Ä‘Æ°á»£c gá»i.
/// </summary>
public static class ComponentExtensions
{
    public static T GetOrAddComponent<T>(this GameObject go) where T : Component
    {
        T c = go.GetComponent<T>();
        if (c == null) c = go.AddComponent<T>();
        return c;
    }

    public static T GetOrAddComponent<T>(this Component comp) where T : Component
    {
        return comp.gameObject.GetOrAddComponent<T>();
    }
}

