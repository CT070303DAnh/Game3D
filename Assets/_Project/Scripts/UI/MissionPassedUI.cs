using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MissionPassedUI: Màn hình Chiến Thắng Toàn Game (MISSION PASSED).
/// - Hiển thị chữ MISSION PASSED lớn và nổi bật.
/// - Ghi danh người chơi vào Bảng Xếp Hạng với tổng thời gian hoàn thành.
/// - Hiển thị thành tích cá nhân và vị trí Rank đạt được.
/// - Phát âm thanh chiến thắng hào hùng (sfx_mission_passed).
/// </summary>
public class MissionPassedUI : MonoBehaviour
{
    public static MissionPassedUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject missionPassedPanel;
    [SerializeField] private Text titleText;
    [SerializeField] private Text playerStatText;
    [SerializeField] private Text timeStatText;
    [SerializeField] private Text rankStatText;

    private static bool isShown = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        AccessCodeUI.EnsureEventSystem();
    }

    /// <summary>
    /// Kích hoạt màn hình MISSION PASSED, ghi danh vào Rank và phát âm thanh chiến thắng
    /// </summary>
    public static void ShowMissionPassed()
    {
        if (isShown) return;
        isShown = true;

        var ui = EnsureUI();
        ui.TriggerVictory();
    }

    public static MissionPassedUI EnsureUI()
    {
        if (Instance != null)
        {
            Instance.EnsureHierarchy();
            return Instance;
        }

        var found = FindFirstObjectByType<MissionPassedUI>();
        if (found != null)
        {
            Instance = found;
            Instance.EnsureHierarchy();
            return Instance;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cGO = new GameObject("MissionPassedCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        var go = new GameObject("MissionPassedUI");
        go.transform.SetParent(canvas.transform, false);
        Instance = go.AddComponent<MissionPassedUI>();
        Instance.EnsureHierarchy();
        return Instance;
    }

    public void TriggerVictory()
    {
        EnsureHierarchy();

        // 1. Dừng đếm thời gian và ghi danh vào Bảng Xếp Hạng
        string pName = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.CurrentPlayerName : "Chiến Binh";
        float finalTime = LeaderboardManager.Instance != null ? LeaderboardManager.Instance.CurrentRunTime : 0f;
        string formattedTime = LeaderboardManager.FormatTime(finalTime);

        int rankEarned = LeaderboardManager.RecordVictory(pName);

        // 2. Cập nhật Text thành tích
        if (playerStatText != null)
            playerStatText.text = $"NGƯỜI CHƠI:  <color=#FFFF00>{pName}</color>";

        if (timeStatText != null)
            timeStatText.text = $"THỜI GIAN HOÀN THÀNH:  <color=#00FF88>{formattedTime}</color>";

        if (rankStatText != null)
        {
            string rankBadge = rankEarned switch
            {
                1 => "🥇 BẠN ĐẠT HẠNG #1 TRÊN BẢNG XẾP HẠNG!",
                2 => "🥈 BẠN ĐẠT HẠNG #2 TRÊN BẢNG XẾP HẠNG!",
                3 => "🥉 BẠN ĐẠT HẠNG #3 TRÊN BẢNG XẾP HẠNG!",
                _ => $"🎖 BẠN ĐẠT HẠNG #{rankEarned} TRÊN BẢNG XẾP HẠNG!"
            };
            rankStatText.text = rankBadge;
        }

        // 3. Hiển thị Panel
        if (missionPassedPanel != null)
        {
            missionPassedPanel.SetActive(true);
            missionPassedPanel.transform.SetAsLastSibling();
        }

        // 4. Phát âm thanh chiến thắng
        GameAudioManager.PlayMissionPassed();

        // 5. Khóa tương tác & giải phóng chuột
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Đóng các popup khác nếu có
        try { AccessCodeUI.ForceClose(); } catch { }
        try { TerminalUI.ForceClose(); } catch { }
        try { ElevatorKeypadUI.ForceClose(); } catch { }
        try { InventoryUI.Instance?.CloseInventory(); } catch { }
    }

    private void EnsureHierarchy()
    {
        if (missionPassedPanel != null) return;

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Toàn bộ màn hình mờ đen điện ảnh
        missionPassedPanel = new GameObject("MissionPassedPanel");
        missionPassedPanel.transform.SetParent(canvas.transform, false);
        var pR = missionPassedPanel.AddComponent<RectTransform>();
        pR.anchorMin = Vector2.zero;
        pR.anchorMax = Vector2.one;
        pR.offsetMin = Vector2.zero;
        pR.offsetMax = Vector2.zero;

        var pImg = missionPassedPanel.AddComponent<Image>();
        pImg.color = new Color(0.01f, 0.03f, 0.05f, 0.96f);
        pImg.raycastTarget = true;

        // 2. Khung Box Trung Tâm
        var boxGO = new GameObject("DialogBox");
        boxGO.transform.SetParent(missionPassedPanel.transform, false);
        var boxR = boxGO.AddComponent<RectTransform>();
        boxR.anchorMin = new Vector2(0.5f, 0.5f);
        boxR.anchorMax = new Vector2(0.5f, 0.5f);
        boxR.pivot = new Vector2(0.5f, 0.5f);
        boxR.anchoredPosition = Vector2.zero;
        boxR.sizeDelta = new Vector2(720f, 480f);

        var boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.04f, 0.09f, 0.14f, 0.98f);

        var boxOl = boxGO.AddComponent<Outline>();
        boxOl.effectColor = new Color(1f, 0.85f, 0.15f, 0.95f);
        boxOl.effectDistance = new Vector2(2.5f, -2.5f);

        // 3. TIÊU ĐỀ MISSION PASSED LỚN VÀNG KIM
        var titleGO = new GameObject("TitleText");
        titleGO.transform.SetParent(boxGO.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchoredPosition = new Vector2(0f, 180f);
        titleR.sizeDelta = new Vector2(680f, 65f);

        titleText = titleGO.AddComponent<Text>();
        titleText.text = "★ MISSION PASSED ★";
        titleText.font = font;
        titleText.fontSize = 44;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = new Color(1f, 0.88f, 0.2f); // Vàng kim rực rỡ

        var titleOl = titleGO.AddComponent<Outline>();
        titleOl.effectColor = new Color(0.6f, 0.2f, 0f, 1f);
        titleOl.effectDistance = new Vector2(3f, -3f);

        // Subtitle
        var subGO = new GameObject("SubtitleText");
        subGO.transform.SetParent(boxGO.transform, false);
        var subR = subGO.AddComponent<RectTransform>();
        subR.anchoredPosition = new Vector2(0f, 125f);
        subR.sizeDelta = new Vector2(680f, 32f);

        var subTxt = subGO.AddComponent<Text>();
        subTxt.text = "XUẤT SẮC HOÀN THÀNH TẤT CẢ THỬ THÁCH & THOÁT HIỂM THÀNH CÔNG!";
        subTxt.font = font;
        subTxt.fontSize = 16;
        subTxt.fontStyle = FontStyle.Bold;
        subTxt.alignment = TextAnchor.MiddleCenter;
        subTxt.color = new Color(0.35f, 0.95f, 1f);

        // 4. Score Card Panel
        var cardGO = new GameObject("ScoreCard");
        cardGO.transform.SetParent(boxGO.transform, false);
        var cardR = cardGO.AddComponent<RectTransform>();
        cardR.anchoredPosition = new Vector2(0f, 20f);
        cardR.sizeDelta = new Vector2(640f, 140f);

        var cardImg = cardGO.AddComponent<Image>();
        cardImg.color = new Color(0.08f, 0.14f, 0.22f, 0.9f);

        var cardOl = cardGO.AddComponent<Outline>();
        cardOl.effectColor = new Color(0.2f, 0.5f, 0.8f, 0.8f);
        cardOl.effectDistance = new Vector2(1f, -1f);

        // Text Player
        var pStatGO = new GameObject("PlayerStat");
        pStatGO.transform.SetParent(cardGO.transform, false);
        var pStatR = pStatGO.AddComponent<RectTransform>();
        pStatR.anchoredPosition = new Vector2(0f, 38f);
        pStatR.sizeDelta = new Vector2(600f, 30f);

        playerStatText = pStatGO.AddComponent<Text>();
        playerStatText.font = font;
        playerStatText.fontSize = 19;
        playerStatText.fontStyle = FontStyle.Bold;
        playerStatText.alignment = TextAnchor.MiddleCenter;
        playerStatText.color = Color.white;

        // Text Time
        var tStatGO = new GameObject("TimeStat");
        tStatGO.transform.SetParent(cardGO.transform, false);
        var tStatR = tStatGO.AddComponent<RectTransform>();
        tStatR.anchoredPosition = new Vector2(0f, 2f);
        tStatR.sizeDelta = new Vector2(600f, 30f);

        timeStatText = tStatGO.AddComponent<Text>();
        timeStatText.font = font;
        timeStatText.fontSize = 20;
        timeStatText.fontStyle = FontStyle.Bold;
        timeStatText.alignment = TextAnchor.MiddleCenter;
        timeStatText.color = Color.white;

        // Text Rank
        var rStatGO = new GameObject("RankStat");
        rStatGO.transform.SetParent(cardGO.transform, false);
        var rStatR = rStatGO.AddComponent<RectTransform>();
        rStatR.anchoredPosition = new Vector2(0f, -36f);
        rStatR.sizeDelta = new Vector2(600f, 32f);

        rankStatText = rStatGO.AddComponent<Text>();
        rankStatText.font = font;
        rankStatText.fontSize = 18;
        rankStatText.fontStyle = FontStyle.Bold;
        rankStatText.alignment = TextAnchor.MiddleCenter;
        rankStatText.color = new Color(1f, 0.85f, 0.2f);

        // 5. Nút Bấm: XEM BẢNG XẾP HẠNG
        CreateActionButton(boxGO.transform, "LeaderboardBtn", "🏆 XEM BẢNG XẾP HẠNG", new Vector2(-210f, -145f), new Vector2(210f, 48f),
            new Color(0.75f, 0.55f, 0.1f), new Color(1f, 0.85f, 0.2f), () =>
            {
                LeaderboardUI.ShowLeaderboard();
            });

        // 6. Nút Bấm: CHƠI LẠI TỪ MÀN 1
        CreateActionButton(boxGO.transform, "RestartBtn", "🔄 CHƠI LẠI TỪ MÀN 1", new Vector2(0f, -145f), new Vector2(200f, 48f),
            new Color(0.15f, 0.55f, 0.35f), new Color(0.3f, 1f, 0.5f), () =>
            {
                isShown = false;
                Time.timeScale = 1f;
                LeaderboardManager.StartNewRun();
                if (GameManager.Instance != null) GameManager.Instance.RestartGame();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("Lab");
            });

        // 7. Nút Bấm: MENU CHÍNH
        CreateActionButton(boxGO.transform, "MenuBtn", "🏠 MENU CHÍNH", new Vector2(210f, -145f), new Vector2(190f, 48f),
            new Color(0.2f, 0.28f, 0.42f), new Color(0.4f, 0.85f, 1f), () =>
            {
                isShown = false;
                Time.timeScale = 1f;
                if (GameManager.Instance != null) GameManager.Instance.LoadMainMenu();
                else UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
            });

        missionPassedPanel.SetActive(false);
    }

    private void CreateActionButton(Transform parent, string name, string text, Vector2 pos, Vector2 size, Color bgColor, Color outlineColor, UnityEngine.Events.UnityAction onClick)
    {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        var btnGO = new GameObject(name);
        btnGO.transform.SetParent(parent, false);
        var r = btnGO.AddComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = size;

        var img = btnGO.AddComponent<Image>();
        img.color = bgColor;

        var ol = btnGO.AddComponent<Outline>();
        ol.effectColor = outlineColor;
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        var btn = btnGO.AddComponent<Button>();
        btn.onClick.AddListener(onClick);

        var txtGO = new GameObject("Text");
        txtGO.transform.SetParent(btnGO.transform, false);
        var txtR = txtGO.AddComponent<RectTransform>();
        txtR.anchorMin = Vector2.zero;
        txtR.anchorMax = Vector2.one;
        txtR.offsetMin = Vector2.zero;
        txtR.offsetMax = Vector2.zero;

        var txt = txtGO.AddComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = 15;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        txt.raycastTarget = false;
    }
}
