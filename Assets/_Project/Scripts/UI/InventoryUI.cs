using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// InventoryUI: Quản lý hiển thị Túi Đồ / Trang Bị phong cách Retro PSX Horror & Sci-Fi HUD.
/// - Bấm [TAB] hoặc [I] để mở/đóng túi đồ bất cứ lúc nào.
/// - Hiển thị các ô vật phẩm đồ họa đẹp mắt với ICON RETRO PSX:
///   Thẻ bảo mật, Cờ lê, Cầu chì, Chìa khóa, Đĩa mềm, Pin, Bình nhiên liệu, Van khí.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    public static InventoryUI Instance { get; private set; }
    public static bool IsOpen => Instance != null && Instance.isOpen;

    [Header("UI References")]
    [SerializeField] private GameObject inventoryPanel;
    [SerializeField] private Button toggleButton;
    [SerializeField] private Button closeButton;

    private bool isOpen = false;
    private Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();
    private List<ItemSlotUI> slotList = new List<ItemSlotUI>();

    private class ItemSlotUI
    {
        public GameObject slotGO;
        public Image iconImage;
        public Image bgImage;
        public Outline outline;
        public Text nameText;
        public Text statusText;
        public Text descText;
        public string iconName;
        public System.Func<bool> isCollected;
        public string title;
        public string activeDesc;
        public string inactiveDesc;
        public Color activeColor;
    }

    private static int lastToggleFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            if (!Instance.gameObject.activeInHierarchy || Instance.gameObject.name.Contains("Mobile"))
            {
                Destroy(Instance);
                Instance = this;
            }
            else
            {
                Destroy(this);
                return;
            }
        }
        else
        {
            Instance = this;
        }

        AccessCodeUI.EnsureEventSystem();
    }

    private void Start()
    {
        CleanUpOldInventoryPanels();
        EnsureInventoryPanel();

        if (inventoryPanel != null)
            inventoryPanel.SetActive(false);

        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleInventory);

        if (closeButton != null)
            closeButton.onClick.AddListener(CloseInventory);

        UpdateInventoryText();
    }

    public void CleanUpOldInventoryPanels()
    {
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        if (parentCanvas == null) return;

        var allPanels = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in allPanels)
        {
            if (t == null) continue;
            if (t.gameObject.name == "InventoryPanel")
            {
                if (inventoryPanel != null && t.gameObject == inventoryPanel) continue;
                DestroyImmediate(t.gameObject);
            }
        }
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
        if (Time.frameCount == lastToggleFrame) return;
        lastToggleFrame = Time.frameCount;

        bool currentlyShowing = isOpen || (inventoryPanel != null && inventoryPanel.activeSelf);
        if (currentlyShowing) CloseInventory();
        else OpenInventory();
    }

    public void OpenInventory()
    {
        EnsureInventoryPanel();
        isOpen = true;

        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(true);
            inventoryPanel.transform.SetAsLastSibling();
            UpdateInventoryText();
        }

        if (UnityEngine.EventSystems.EventSystem.current != null)
        {
            UnityEngine.EventSystems.EventSystem.current.SetSelectedGameObject(null);
        }

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

        // Tắt cưỡng chế toàn bộ các GameObject mang tên InventoryPanel trong toàn bộ Scene
        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        if (parentCanvas != null)
        {
            for (int i = 0; i < parentCanvas.transform.childCount; i++)
            {
                var child = parentCanvas.transform.GetChild(i);
                if (child.name == "InventoryPanel")
                {
                    child.gameObject.SetActive(false);
                }
            }
        }

        if (!AccessCodeUI.IsOpen && !TerminalUI.IsOpen && !ElevatorKeypadUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private Sprite GetItemIcon(string name)
    {
        if (string.IsNullOrEmpty(name)) return null;
        if (iconCache.TryGetValue(name, out Sprite s)) return s;

        s = Resources.Load<Sprite>("ItemIcons/" + name);
        if (s != null) iconCache[name] = s;
        return s;
    }

    public void UpdateInventoryText()
    {
        if (slotList == null || slotList.Count == 0) return;

        var gs = GameState.Instance;
        foreach (var slot in slotList)
        {
            bool collected = (slot.isCollected != null) && slot.isCollected();

            if (slot.iconImage != null)
            {
                if (slot.iconImage.sprite == null && !string.IsNullOrEmpty(slot.iconName))
                {
                    slot.iconImage.sprite = GetItemIcon(slot.iconName);
                }
                slot.iconImage.color = collected ? Color.white : new Color(0.2f, 0.25f, 0.3f, 0.35f);
            }

            if (slot.bgImage != null)
            {
                slot.bgImage.color = collected 
                    ? new Color(0.08f, 0.12f, 0.16f, 0.95f) 
                    : new Color(0.04f, 0.05f, 0.07f, 0.70f);
            }

            if (slot.outline != null)
            {
                slot.outline.effectColor = collected 
                    ? new Color(slot.activeColor.r, slot.activeColor.g, slot.activeColor.b, 0.85f) 
                    : new Color(0.18f, 0.22f, 0.28f, 0.35f);
            }

            if (slot.nameText != null)
            {
                slot.nameText.text = slot.title;
                slot.nameText.color = collected ? slot.activeColor : new Color(0.5f, 0.55f, 0.6f);
            }

            if (slot.statusText != null)
            {
                slot.statusText.text = collected ? "✓ [ĐÃ SỞ HỮU]" : "— [CHƯA CÓ]";
                slot.statusText.color = collected ? Color.green : new Color(0.45f, 0.45f, 0.5f);
            }

            if (slot.descText != null)
            {
                slot.descText.text = collected ? slot.activeDesc : slot.inactiveDesc;
                slot.descText.color = collected ? new Color(0.85f, 0.92f, 1f) : new Color(0.4f, 0.45f, 0.5f);
            }
        }
    }

    public void EnsureInventoryPanel()
    {
        if (inventoryPanel != null && slotList.Count > 0) return;

        Canvas parentCanvas = GetComponentInParent<Canvas>();
        if (parentCanvas == null) parentCanvas = FindFirstObjectByType<Canvas>();
        if (parentCanvas == null) return;

        for (int i = parentCanvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = parentCanvas.transform.GetChild(i);
            if (child.name == "InventoryPanel")
            {
                DestroyImmediate(child.gameObject);
            }
        }

        // Tạo Panel chính phong cách PSX Sci-Fi HUD
        inventoryPanel = new GameObject("InventoryPanel");
        inventoryPanel.transform.SetParent(parentCanvas.transform, false);

        var rt = inventoryPanel.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(760, 520);

        var bg = inventoryPanel.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.06f, 0.09f, 0.96f);

        var outline = inventoryPanel.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0.85f, 1f, 0.7f);
        outline.effectDistance = new Vector2(2f, -2f);

        // Header Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(inventoryPanel.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0, 1);
        titleR.anchorMax = new Vector2(1, 1);
        titleR.pivot = new Vector2(0.5f, 1);
        titleR.anchoredPosition = new Vector2(0, -12);
        titleR.sizeDelta = new Vector2(-40, 36);

        var titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "🎒 TÚI ĐỒ TRANG BỊ // RETRO PSX INVENTORY [TAB]";
        titleTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleTxt.fontSize = 18;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.alignment = TextAnchor.MiddleCenter;
        titleTxt.color = new Color(0.2f, 0.9f, 1f);

        // Close Button [X]
        var closeGO = new GameObject("CloseButton");
        closeGO.transform.SetParent(inventoryPanel.transform, false);
        var closeR = closeGO.AddComponent<RectTransform>();
        closeR.anchorMin = new Vector2(1, 1);
        closeR.anchorMax = new Vector2(1, 1);
        closeR.pivot = new Vector2(1, 1);
        closeR.anchoredPosition = new Vector2(-15, -12);
        closeR.sizeDelta = new Vector2(32, 32);

        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.navigation = new Navigation { mode = Navigation.Mode.None };
        var closeBg = closeGO.AddComponent<Image>();
        closeBg.color = new Color(0.8f, 0.15f, 0.2f, 0.85f);
        closeBtn.onClick.AddListener(CloseInventory);
        closeButton = closeBtn;

        var closeTxtGO = new GameObject("Text");
        closeTxtGO.transform.SetParent(closeGO.transform, false);
        var cTxtR = closeTxtGO.AddComponent<RectTransform>();
        cTxtR.anchorMin = Vector2.zero;
        cTxtR.anchorMax = Vector2.one;
        cTxtR.sizeDelta = Vector2.zero;
        var cTxt = closeTxtGO.AddComponent<Text>();
        cTxt.text = "✕";
        cTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        cTxt.fontSize = 18;
        cTxt.fontStyle = FontStyle.Bold;
        cTxt.alignment = TextAnchor.MiddleCenter;
        cTxt.color = Color.white;

        // Divider
        var divGO = new GameObject("Divider");
        divGO.transform.SetParent(inventoryPanel.transform, false);
        var divR = divGO.AddComponent<RectTransform>();
        divR.anchorMin = new Vector2(0, 1);
        divR.anchorMax = new Vector2(1, 1);
        divR.anchoredPosition = new Vector2(0, -52);
        divR.sizeDelta = new Vector2(-40, 2);
        divGO.AddComponent<Image>().color = new Color(0f, 0.85f, 1f, 0.4f);

        // Grid Container cho 6-8 item slots
        var gridGO = new GameObject("GridContainer");
        gridGO.transform.SetParent(inventoryPanel.transform, false);
        var gridR = gridGO.AddComponent<RectTransform>();
        gridR.anchorMin = new Vector2(0, 0);
        gridR.anchorMax = new Vector2(1, 1);
        gridR.anchoredPosition = new Vector2(0, -10);
        gridR.sizeDelta = new Vector2(-40, -120);

        var glg = gridGO.AddComponent<GridLayoutGroup>();
        glg.cellSize = new Vector2(345, 90);
        glg.spacing = new Vector2(16, 12);
        glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
        glg.startAxis = GridLayoutGroup.Axis.Horizontal;
        glg.childAlignment = TextAnchor.UpperCenter;

        slotList.Clear();

        // 1. Thẻ Bảo Mật
        CreateSlot(gridGO.transform, "SecurityCard",
            "THẺ BẢO MẬT (KEYCARD)",
            "Mở khóa Cửa Thoát Hiểm Phòng Thí Nghiệm (Màn 1)",
            "Chưa nhặt - Hãy tìm trên bàn nghiên cứu Lab",
            new Color(0f, 0.9f, 1f),
            () => GameState.Instance != null && GameState.Instance.SecurityCardCollected);

        // 2. Cờ Lê Sửa Chữa
        CreateSlot(gridGO.transform, "Wrench",
            "CỜ LÊ SỬA CHỮA (WRENCH)",
            "Dùng vặn khóa 3 van xả khí độc (Màn 2)",
            "Chưa nhặt - Hãy tìm tại Kho Phụ Tùng Bắc",
            new Color(1f, 0.7f, 0.1f),
            () => GameState.Instance != null && GameState.Instance.WrenchCollected);

        // 3. Cầu Chì Năng Lượng
        CreateSlot(gridGO.transform, "Fuse",
            "CẦU CHÌ CAO ÁP (POWER FUSE)",
            "Lắp vào Cầu Dao Điện / Máy Phát khôi phục nguồn điện",
            "Chưa nhặt - Cần tìm trong phòng thiết bị",
            new Color(1f, 0.88f, 0.2f),
            () => GameState.Instance != null && GameState.Instance.FuseCollected);

        // 4. Chìa Khóa Phòng Lab
        CreateSlot(gridGO.transform, "LaboratoryKey",
            "CHÌA KHÓA PHÒNG LAB",
            "Mở cửa phụ / Tủ lưu trữ bảo mật",
            "Chưa nhặt - Hãy tìm kỹ trong các góc phòng",
            new Color(0.9f, 0.8f, 0.4f),
            () => GameState.Instance != null && GameState.Instance.LaboratoryKeyCollected);

        // 5. Đĩa Mềm Mã Terminal
        CreateSlot(gridGO.transform, "AccessCode",
            "ĐĨA MỀM DỮ LIỆU (FLOPPY DISK)",
            "Lưu mã màu Terminal / Mật mã hệ thống",
            "Chưa giải mã - Xem manh mối trên tường/bàn",
            new Color(0.3f, 0.75f, 1f),
            () => GameState.Instance != null && (GameState.Instance.AccessCodeFound || GameState.Instance.AccessCodeSolved));

        // 6. 3 Van Khí Độc
        CreateSlot(gridGO.transform, "Valve",
            "HỆ THỐNG 3 VAN KHÍ ĐỘC",
            "Cả 3 van đã được đóng kín! Không khí an toàn",
            "Cần dùng Cờ Lê khóa hết 3 van đang xả khí",
            new Color(0.2f, 1f, 0.4f),
            () => ToxicGasManager.IsAirSafe);

        // 7. Pin Năng Lượng (+HP)
        CreateSlot(gridGO.transform, "Battery",
            "PIN NĂNG LƯỢNG (+HP)",
            "Nguồn năng lượng y tế phục hồi máu tức thì",
            "Chưa nhặt - Tìm trong phòng nồi hơi",
            new Color(0.3f, 1f, 0.5f),
            () => GameState.Instance != null && GameState.Instance.BatteryCollected);

        // 8. Bình Nhiên Liệu Trực Thăng
        CreateSlot(gridGO.transform, "JetFuel",
            "NHIÊN LIỆU PHẢN LỰC (JET FUEL)",
            "Tiếp đầy nhiên liệu cho Trực Thăng Tầng Thượng (Màn 3)",
            "Chưa nhặt - Tìm trên sân đỗ trực thăng",
            new Color(1f, 0.35f, 0.35f),
            () => GameState.Instance != null && GameState.Instance.JetFuelCollected);

        // Footer Hint Text
        var footGO = new GameObject("FooterHint");
        footGO.transform.SetParent(inventoryPanel.transform, false);
        var footR = footGO.AddComponent<RectTransform>();
        footR.anchorMin = new Vector2(0, 0);
        footR.anchorMax = new Vector2(1, 0);
        footR.pivot = new Vector2(0.5f, 0);
        footR.anchoredPosition = new Vector2(0, 14);
        footR.sizeDelta = new Vector2(-40, 24);

        var footTxt = footGO.AddComponent<Text>();
        footTxt.text = "💡 Nhấn [TAB] hoặc [ESC] để đóng Túi Đồ • Sử dụng vật phẩm tự động khi tương tác [E]";
        footTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        footTxt.fontSize = 13;
        footTxt.alignment = TextAnchor.MiddleCenter;
        footTxt.color = new Color(0.5f, 0.8f, 1f, 0.8f);

        UpdateInventoryText();
    }

    private void CreateSlot(Transform parent, string iconName, string title, string activeDesc, string inactiveDesc, Color activeColor, System.Func<bool> isCollected)
    {
        var slotGO = new GameObject("Slot_" + iconName);
        slotGO.transform.SetParent(parent, false);

        var slotR = slotGO.AddComponent<RectTransform>();
        slotR.sizeDelta = new Vector2(345, 90);

        var bg = slotGO.AddComponent<Image>();
        bg.color = new Color(0.04f, 0.05f, 0.07f, 0.70f);

        var outl = slotGO.AddComponent<Outline>();
        outl.effectColor = new Color(0.18f, 0.22f, 0.28f, 0.35f);
        outl.effectDistance = new Vector2(1.5f, -1.5f);

        // Icon Box
        var iconBoxGO = new GameObject("IconBox");
        iconBoxGO.transform.SetParent(slotGO.transform, false);
        var iconBoxR = iconBoxGO.AddComponent<RectTransform>();
        iconBoxR.anchorMin = new Vector2(0, 0.5f);
        iconBoxR.anchorMax = new Vector2(0, 0.5f);
        iconBoxR.pivot = new Vector2(0, 0.5f);
        iconBoxR.anchoredPosition = new Vector2(10, 0);
        iconBoxR.sizeDelta = new Vector2(70, 70);

        var iconBoxBg = iconBoxGO.AddComponent<Image>();
        iconBoxBg.color = new Color(0.02f, 0.03f, 0.05f, 0.9f);

        var iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(iconBoxGO.transform, false);
        var iconR = iconGO.AddComponent<RectTransform>();
        iconR.anchorMin = new Vector2(0.5f, 0.5f);
        iconR.anchorMax = new Vector2(0.5f, 0.5f);
        iconR.pivot = new Vector2(0.5f, 0.5f);
        iconR.anchoredPosition = Vector2.zero;
        iconR.sizeDelta = new Vector2(62, 62);

        var img = iconGO.AddComponent<Image>();
        img.sprite = GetItemIcon(iconName);
        img.preserveAspect = true;
        img.color = new Color(0.2f, 0.25f, 0.3f, 0.35f);

        // Content layout
        var contentGO = new GameObject("Content");
        contentGO.transform.SetParent(slotGO.transform, false);
        var contentR = contentGO.AddComponent<RectTransform>();
        contentR.anchorMin = new Vector2(0, 0);
        contentR.anchorMax = new Vector2(1, 1);
        contentR.pivot = new Vector2(0, 0.5f);
        contentR.anchoredPosition = new Vector2(92, 0);
        contentR.sizeDelta = new Vector2(-102, 0);

        // Title
        var nameGO = new GameObject("Name");
        nameGO.transform.SetParent(contentGO.transform, false);
        var nameR = nameGO.AddComponent<RectTransform>();
        nameR.anchorMin = new Vector2(0, 1);
        nameR.anchorMax = new Vector2(1, 1);
        nameR.pivot = new Vector2(0, 1);
        nameR.anchoredPosition = new Vector2(0, -6);
        nameR.sizeDelta = new Vector2(0, 20);

        var nTxt = nameGO.AddComponent<Text>();
        nTxt.text = title;
        nTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        nTxt.fontSize = 13;
        nTxt.fontStyle = FontStyle.Bold;
        nTxt.color = new Color(0.5f, 0.55f, 0.6f);

        // Status
        var statusGO = new GameObject("Status");
        statusGO.transform.SetParent(contentGO.transform, false);
        var statR = statusGO.AddComponent<RectTransform>();
        statR.anchorMin = new Vector2(0, 1);
        statR.anchorMax = new Vector2(1, 1);
        statR.pivot = new Vector2(0, 1);
        statR.anchoredPosition = new Vector2(0, -26);
        statR.sizeDelta = new Vector2(0, 16);

        var sTxt = statusGO.AddComponent<Text>();
        sTxt.text = "— [CHƯA CÓ]";
        sTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        sTxt.fontSize = 11;
        sTxt.fontStyle = FontStyle.Bold;
        sTxt.color = new Color(0.45f, 0.45f, 0.5f);

        // Description
        var descGO = new GameObject("Desc");
        descGO.transform.SetParent(contentGO.transform, false);
        var descR = descGO.AddComponent<RectTransform>();
        descR.anchorMin = new Vector2(0, 0);
        descR.anchorMax = new Vector2(1, 1);
        descR.pivot = new Vector2(0, 0);
        descR.anchoredPosition = new Vector2(0, 6);
        descR.sizeDelta = new Vector2(0, -46);

        var dTxt = descGO.AddComponent<Text>();
        dTxt.text = inactiveDesc;
        dTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        dTxt.fontSize = 11;
        dTxt.color = new Color(0.4f, 0.45f, 0.5f);
        dTxt.verticalOverflow = VerticalWrapMode.Truncate;

        slotList.Add(new ItemSlotUI
        {
            slotGO = slotGO,
            iconImage = img,
            bgImage = bg,
            outline = outl,
            nameText = nTxt,
            statusText = sTxt,
            descText = dTxt,
            iconName = iconName,
            isCollected = isCollected,
            title = title,
            activeDesc = activeDesc,
            inactiveDesc = inactiveDesc,
            activeColor = activeColor
        });
    }
}
