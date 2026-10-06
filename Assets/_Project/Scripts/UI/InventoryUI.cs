using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// InventoryUI: Quản lý hiển thị Túi Đồ / Trang Bị cho cả 3 Màn chơi.
/// - Bấm [TAB] hoặc [I] để mở/đóng túi đồ bất cứ lúc nào.
/// - Hiển thị danh sách vật phẩm chi tiết: Thẻ bảo mật, Cờ lê, Cầu chì, Bình nhiên liệu, v.v.
/// - Tự động tạo giao diện Sci-Fi HUD đẹp mắt nếu Scene chưa có.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.isOpen;

    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Text itemListText;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;

    private bool isOpen = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        EnsureInventoryPanel();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleInventory);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        UpdateInventoryText();
    }

    private void OnEnable()
    {
        GameState.OnSecurityCardCollected += UpdateInventoryText;
        GameState.OnFuseCollected         += UpdateInventoryText;
        GameState.OnAccessCodeSolved      += UpdateInventoryText;
        GameState.OnWrenchCollected       += UpdateInventoryText;
        GameState.OnJetFuelCollected      += UpdateInventoryText;
        GameState.OnRadarActivated        += UpdateInventoryText;
        GameState.OnExitUnlocked          += UpdateInventoryText;
    }

    private void OnDisable()
    {
        GameState.OnSecurityCardCollected -= UpdateInventoryText;
        GameState.OnFuseCollected         -= UpdateInventoryText;
        GameState.OnAccessCodeSolved      -= UpdateInventoryText;
        GameState.OnWrenchCollected       -= UpdateInventoryText;
        GameState.OnJetFuelCollected      -= UpdateInventoryText;
        GameState.OnRadarActivated        -= UpdateInventoryText;
        GameState.OnExitUnlocked          -= UpdateInventoryText;
    }

    private void Update()
    {
        // Bấm TAB hoặc I để bật/tắt túi đồ
        if (Input.GetKeyDown(KeyCode.Tab) || Input.GetKeyDown(KeyCode.I))
        {
            ToggleInventory();
        }
        else if (isOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            CloseInventory();
        }
    }

    public void ToggleInventory()
    {
        if (isOpen) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        EnsureInventoryPanel();
        isOpen = true;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            UpdateInventoryText();
        }

        // Mở con trỏ chuột để người chơi tương tác
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void CloseInventory()
    {
        isOpen = false;
        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }

        // Khôi phục trạng thái khóa chuột gameplay nếu không mở modal khác
        if (!AccessCodeUI.IsOpen && !TerminalUI.IsOpen && !ElevatorKeypadUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void UpdateInventoryText()
    {
        if (itemListText == null) return;

        string txt = "";
        var gs = GameState.Instance;

        if (gs != null)
        {
            // ─── Màn 1: Phòng Thí Nghiệm (Lab) ───
            if (gs.SecurityCardCollected)
                txt += "<color=#00e5ff>• Thẻ Bảo Mật</color> (Security Card - Cửa Thoát Hiểm)\n";

            if (gs.LaboratoryKeyCollected)
                txt += "<color=#00e5ff>• Chìa Khóa Phòng Lab</color> (Mở Phòng B)\n";

            if (gs.AccessCodeFound || gs.AccessCodeSolved)
                txt += "<color=#ffea00>• Mã Truy Cập</color> (Mã màu Terminal)\n";

            // ─── Màn 2: Khu Lò Phản Ứng (Reactor) ───
            if (gs.WrenchCollected)
                txt += "<color=#ffab00>• Cờ Lê Sửa Chữa (Wrench)</color> [Đã Trang Bị - Vặn Khóa Van]\n";

            // ─── Dùng chung / Cầu chì ───
            if (gs.FuseCollected)
                txt += "<color=#ffea00>• Cầu Chì Năng Lượng (Power Fuse)</color>\n";

            if (gs.BatteryCollected)
                txt += "<color=#69f0ae>• Pin Năng Lượng (+HP)</color>\n";

            // ─── Màn 3: Sân Đỗ Trực Thăng (Helipad) ───
            if (gs.JetFuelCollected)
                txt += "<color=#ff5252>• Bình Nhiên Liệu Trực Thăng (Jet Fuel)</color>\n";

            if (gs.RadarActivated)
                txt += "<color=#00e5ff>• Tín Hiệu Định Vị Radar</color> [Sẵn Sàng Cất Cánh]\n";
        }

        if (string.IsNullOrEmpty(txt))
        {
            txt = "<i><color=#9e9e9e>(Túi đồ hiện đang trống - Chưa nhặt vật phẩm)</color></i>";
        }

        itemListText.text = txt;
    }

    /// <summary>
    /// Đảm bảo giao diện Túi Đồ tồn tại đầy đủ trên Canvas theo chuẩn Sci-Fi HUD đồng bộ 3 màn.
    /// </summary>
    public void EnsureInventoryPanel()
    {
        if (inventoryPanel != null && itemListText != null) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        if (parentCanvas == null) return;

        // Tìm panel có sẵn nếu đã có
        Transform existing = parentCanvas.transform.Find("InventoryPanel");
        if (existing != null)
        {
            inventoryPanel = existing.gameObject;
            var t = inventoryPanel.GetComponentInChildren<Text>(true);
            if (t != null) itemListText = t;
            return;
        }

        // Tạo mới panel Túi Đồ phong cách Sci-Fi HUD
        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(parentCanvas.transform, false);

        var rt = inventoryPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(440, 320);

        var bg = inventoryPanel.AddComponent<Image>();
        bg.color = new Color(0.06f, 0.08f, 0.12f, 0.94f);

        var outline = inventoryPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.8f, 1f, 0.6f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(inventoryPanel.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0, 1);
        titleR.anchorMax = new Vector2(1, 1);
        titleR.pivot = new Vector2(0.5f, 1);
        titleR.anchoredPosition = new Vector2(0, -12);
        titleR.sizeDelta = new Vector2(-30, 36);

        var titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "🎒 TÚI ĐỒ / TRANG BỊ [TAB]";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 18;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.2f, 0.9f, 1f);

        // Divider
        var divGO = new GameObject("Divider");
        divGO.transform.SetParent(inventoryPanel.transform, false);
        var divR = divGO.AddComponent<RectTransform>();
        divR.anchorMin = new Vector2(0, 1);
        divR.anchorMax = new Vector2(1, 1);
        divR.anchoredPosition = new Vector2(0, -50);
        divR.sizeDelta = new Vector2(-40, 2);
        divGO.AddComponent<Image>().color = new Color(0f, 0.8f, 1f, 0.35f);

        // Item List Text
        var textGO = new GameObject("ItemListText");
        textGO.transform.SetParent(inventoryPanel.transform, false);
        var textR = textGO.AddComponent<RectTransform>();
        textR.anchorMin = new Vector2(0, 0);
        textR.anchorMax = new Vector2(1, 1);
        textR.pivot = new Vector2(0.5f, 0.5f);
        textR.anchoredPosition = new Vector2(0, -5);
        textR.sizeDelta = new Vector2(-40, -100);

        itemListText = textGO.AddComponent<Text>();
        itemListText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        itemListText.fontSize = 15;
        itemListText.lineSpacing = 1.35f;
        itemListText.alignment = TextAnchor.UpperLeft;
        itemListText.color = Color.white;
        itemListText.supportRichText = true;

        // Footer Hint Text
        var footGO = new GameObject("FooterHint");
        footGO.transform.SetParent(inventoryPanel.transform, false);
        var footR = footGO.AddComponent<RectTransform>();
        footR.anchorMin = new Vector2(0, 0);
        footR.anchorMax = new Vector2(1, 0);
        footR.pivot = new Vector2(0.5f, 0);
        footR.anchoredPosition = new Vector2(0, 12);
        footR.sizeDelta = new Vector2(-30, 24);

        var footTxt = footGO.AddComponent<Text>();
        footTxt.text = "[ Nhấn TAB hoặc I để đóng túi đồ ]";
        footTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        footTxt.fontSize = 13;
        footTxt.fontStyle = FontStyle.Italic;
        footTxt.alignment = TextAnchor.MiddleCenter;
        footTxt.color = new Color(0.7f, 0.75f, 0.8f, 0.85f);

        // Nút đóng [X]
        var btnGO = new GameObject("CloseButton");
        btnGO.transform.SetParent(inventoryPanel.transform, false);
        var btnR = btnGO.AddComponent<RectTransform>();
        btnR.anchorMin = new Vector2(1, 1);
        btnR.anchorMax = new Vector2(1, 1);
        btnR.pivot = new Vector2(1, 1);
        btnR.anchoredPosition = new Vector2(-10, -10);
        btnR.sizeDelta = new Vector2(28, 28);
        btnGO.AddComponent<Image>().color = new Color(0.8f, 0.2f, 0.2f, 0.85f);
        closeButton = btnGO.AddComponent<Button>();
        closeButton.onClick.AddListener(CloseInventory);

        var xGO = new GameObject("Text");
        xGO.transform.SetParent(btnGO.transform, false);
        var xR = xGO.AddComponent<RectTransform>();
        xR.anchorMin = Vector2.zero; xR.anchorMax = Vector2.one;
        xR.sizeDelta = Vector2.zero;
        var xTxt = xGO.AddComponent<Text>();
        xTxt.text = "✕";
        xTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        xTxt.fontSize = 16;
        xTxt.fontStyle = FontStyle.Bold;
        xTxt.alignment = TextAnchor.MiddleCenter;
        xTxt.color = Color.white;

        inventoryPanel.SetActive(false);
    }
}
