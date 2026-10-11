using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

/// <summary>
/// LeaderboardUI: Giao diện Bảng Xếp Hạng người chơi hoàn thành thử thách.
/// - Hoạt động độc lập, tự động dựng UI panel nếu chưa có trong scene.
/// - Hiển thị thứ hạng, tên người chơi, thời gian hoàn thành (mm:ss.ff) và ngày đạt.
/// - Có thể gọi từ MainMenu hoặc từ màn hình MISSION PASSED.
/// </summary>
public class LeaderboardUI : MonoBehaviour
{
    public static LeaderboardUI Instance { get; private set; }

    [Header("UI Panels")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform recordsContainer;
    [SerializeField] private Text emptyNoticeText;

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

    public static void ShowLeaderboard()
    {
        var ui = EnsureUI();
        ui.OpenPanel();
    }

    public static void HideLeaderboard()
    {
        if (Instance != null && Instance.leaderboardPanel != null)
        {
            Instance.leaderboardPanel.SetActive(false);
        }
    }

    public static LeaderboardUI EnsureUI()
    {
        if (Instance != null)
        {
            Instance.EnsureHierarchy();
            return Instance;
        }

        var found = FindFirstObjectByType<LeaderboardUI>();
        if (found != null)
        {
            Instance = found;
            Instance.EnsureHierarchy();
            return Instance;
        }

        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            var cGO = new GameObject("LeaderboardCanvas");
            canvas = cGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            cGO.AddComponent<CanvasScaler>();
            cGO.AddComponent<GraphicRaycaster>();
        }

        var go = new GameObject("LeaderboardUI");
        go.transform.SetParent(canvas.transform, false);
        Instance = go.AddComponent<LeaderboardUI>();
        Instance.EnsureHierarchy();
        return Instance;
    }

    public void OpenPanel()
    {
        EnsureHierarchy();
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(true);
            leaderboardPanel.transform.SetAsLastSibling();
            PopulateRecords();
        }
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ClosePanel()
    {
        if (leaderboardPanel != null)
        {
            leaderboardPanel.SetActive(false);
        }
    }

    private void PopulateRecords()
    {
        if (recordsContainer == null) return;

        // Xóa các dòng cũ
        for (int i = recordsContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(recordsContainer.GetChild(i).gameObject);
        }

        var records = LeaderboardManager.GetRecords();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (records == null || records.Count == 0)
        {
            if (emptyNoticeText != null)
            {
                emptyNoticeText.gameObject.SetActive(true);
                emptyNoticeText.text = "CHƯA CÓ KỶ LỤC NÀO!\nHãy hoàn thành tất cả 3 màn chơi và thoát hiểm bằng trực thăng để được ghi danh.";
            }
            return;
        }

        if (emptyNoticeText != null) emptyNoticeText.gameObject.SetActive(false);

        for (int i = 0; i < records.Count; i++)
        {
            var r = records[i];
            int rankNum = i + 1;

            var rowGO = new GameObject($"RecordRow_{rankNum}");
            rowGO.transform.SetParent(recordsContainer, false);
            var rowR = rowGO.AddComponent<RectTransform>();
            rowR.sizeDelta = new Vector2(620f, 40f);

            var rowImg = rowGO.AddComponent<Image>();
            // Xen kẽ màu nền dòng
            rowImg.color = (i % 2 == 0) ? new Color(0.1f, 0.16f, 0.25f, 0.7f) : new Color(0.06f, 0.1f, 0.18f, 0.7f);

            // Màu chữ nổi bật cho Top 3
            Color rankColor;
            string rankBadge;
            if (rankNum == 1) { rankColor = new Color(1f, 0.85f, 0.2f); rankBadge = "🥇 #1"; }
            else if (rankNum == 2) { rankColor = new Color(0.85f, 0.9f, 1f); rankBadge = "🥈 #2"; }
            else if (rankNum == 3) { rankColor = new Color(1f, 0.65f, 0.35f); rankBadge = "🥉 #3"; }
            else { rankColor = new Color(0.6f, 0.85f, 1f); rankBadge = $"  #{rankNum}"; }

            // Cột Hạng
            CreateCell(rowGO.transform, rankBadge, font, 18, FontStyle.Bold, rankColor, TextAnchor.MiddleCenter, new Vector2(-250f, 0f), 90f);

            // Cột Tên Người Chơi
            CreateCell(rowGO.transform, r.playerName, font, 17, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft, new Vector2(-110f, 0f), 180f);

            // Cột Thời Gian Hoàn Thành (Nổi bật)
            CreateCell(rowGO.transform, r.formattedTime, font, 18, FontStyle.Bold, new Color(0.2f, 1f, 0.5f), TextAnchor.MiddleCenter, new Vector2(70f, 0f), 130f);

            // Cột Ngày Đạt
            CreateCell(rowGO.transform, r.dateString, font, 14, FontStyle.Normal, new Color(0.65f, 0.75f, 0.85f), TextAnchor.MiddleRight, new Vector2(220f, 0f), 150f);
        }
    }

    private void CreateCell(Transform parent, string text, Font font, int size, FontStyle style, Color color, TextAnchor align, Vector2 pos, float width)
    {
        var cellGO = new GameObject("Cell");
        cellGO.transform.SetParent(parent, false);
        var r = cellGO.AddComponent<RectTransform>();
        r.anchoredPosition = pos;
        r.sizeDelta = new Vector2(width, 36f);

        var txt = cellGO.AddComponent<Text>();
        txt.text = text;
        txt.font = font;
        txt.fontSize = size;
        txt.fontStyle = style;
        txt.color = color;
        txt.alignment = align;
        txt.raycastTarget = false;
    }

    public void EnsureHierarchy()
    {
        if (leaderboardPanel != null) return;

        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Panel đen mờ toàn màn hình
        leaderboardPanel = new GameObject("LeaderboardPanel");
        leaderboardPanel.transform.SetParent(canvas.transform, false);
        var pR = leaderboardPanel.AddComponent<RectTransform>();
        pR.anchorMin = Vector2.zero;
        pR.anchorMax = Vector2.one;
        pR.offsetMin = Vector2.zero;
        pR.offsetMax = Vector2.zero;

        var pImg = leaderboardPanel.AddComponent<Image>();
        pImg.color = new Color(0.02f, 0.05f, 0.1f, 0.95f);
        pImg.raycastTarget = true;

        // 2. Dialog Khung Chính
        var boxGO = new GameObject("LeaderboardBox");
        boxGO.transform.SetParent(leaderboardPanel.transform, false);
        var boxR = boxGO.AddComponent<RectTransform>();
        boxR.anchorMin = new Vector2(0.5f, 0.5f);
        boxR.anchorMax = new Vector2(0.5f, 0.5f);
        boxR.pivot = new Vector2(0.5f, 0.5f);
        boxR.anchoredPosition = Vector2.zero;
        boxR.sizeDelta = new Vector2(680f, 540f);

        var boxImg = boxGO.AddComponent<Image>();
        boxImg.color = new Color(0.04f, 0.08f, 0.16f, 0.98f);

        var boxOl = boxGO.AddComponent<Outline>();
        boxOl.effectColor = new Color(1f, 0.8f, 0.2f, 0.9f);
        boxOl.effectDistance = new Vector2(2f, -2f);

        // Header Title
        var titleGO = new GameObject("Title");
        titleGO.transform.SetParent(boxGO.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchoredPosition = new Vector2(0f, 230f);
        titleR.sizeDelta = new Vector2(640f, 45f);

        var titleTxt = titleGO.AddComponent<Text>();
        titleTxt.text = "🏆 BẢNG XẾP HẠNG THÀNH TÍCH 🏆";
        titleTxt.font = font;
        titleTxt.fontSize = 26;
        titleTxt.fontStyle = FontStyle.Bold;
        titleTxt.color = new Color(1f, 0.85f, 0.2f);
        titleTxt.alignment = TextAnchor.MiddleCenter;

        // Subtitle
        var subGO = new GameObject("Subtitle");
        subGO.transform.SetParent(boxGO.transform, false);
        var subR = subGO.AddComponent<RectTransform>();
        subR.anchoredPosition = new Vector2(0f, 195f);
        subR.sizeDelta = new Vector2(640f, 30f);

        var subTxt = subGO.AddComponent<Text>();
        subTxt.text = "Top người chơi hoàn thành thử thách với thời gian nhanh nhất";
        subTxt.font = font;
        subTxt.fontSize = 15;
        subTxt.color = new Color(0.4f, 0.85f, 1f);
        subTxt.alignment = TextAnchor.MiddleCenter;

        // Header Columns Row
        var colGO = new GameObject("ColumnHeader");
        colGO.transform.SetParent(boxGO.transform, false);
        var colR = colGO.AddComponent<RectTransform>();
        colR.anchoredPosition = new Vector2(0f, 155f);
        colR.sizeDelta = new Vector2(620f, 36f);

        var colImg = colGO.AddComponent<Image>();
        colImg.color = new Color(0.12f, 0.22f, 0.38f, 0.9f);

        CreateCell(colGO.transform, "HẠNG", font, 15, FontStyle.Bold, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleCenter, new Vector2(-250f, 0f), 90f);
        CreateCell(colGO.transform, "TÊN NGƯỜI CHƠI", font, 15, FontStyle.Bold, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleLeft, new Vector2(-110f, 0f), 180f);
        CreateCell(colGO.transform, "THỜI GIAN", font, 15, FontStyle.Bold, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleCenter, new Vector2(70f, 0f), 130f);
        CreateCell(colGO.transform, "NGÀY ĐẠT", font, 15, FontStyle.Bold, new Color(0.3f, 0.9f, 1f), TextAnchor.MiddleRight, new Vector2(220f, 0f), 150f);

        // Scroll Area / Container
        var scrollGO = new GameObject("ScrollContainer");
        scrollGO.transform.SetParent(boxGO.transform, false);
        var scrollR = scrollGO.AddComponent<RectTransform>();
        scrollR.anchoredPosition = new Vector2(0f, -10f);
        scrollR.sizeDelta = new Vector2(620f, 270f);

        var mask = scrollGO.AddComponent<Mask>();
        mask.showMaskGraphic = false;
        var maskImg = scrollGO.AddComponent<Image>();
        maskImg.color = Color.white;

        var contentGO = new GameObject("RecordsContent");
        contentGO.transform.SetParent(scrollGO.transform, false);
        var contentR = contentGO.AddComponent<RectTransform>();
        contentR.anchorMin = new Vector2(0.5f, 1f);
        contentR.anchorMax = new Vector2(0.5f, 1f);
        contentR.pivot = new Vector2(0.5f, 1f);
        contentR.anchoredPosition = Vector2.zero;
        contentR.sizeDelta = new Vector2(620f, 270f);

        var vlg = contentGO.AddComponent<VerticalLayoutGroup>();
        vlg.childForceExpandHeight = false;
        vlg.childForceExpandWidth = true;
        vlg.spacing = 3f;

        var csf = contentGO.AddComponent<ContentSizeFitter>();
        csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        recordsContainer = contentGO.transform;

        // Empty Notice
        var emptyGO = new GameObject("EmptyNotice");
        emptyGO.transform.SetParent(boxGO.transform, false);
        var emptyR = emptyGO.AddComponent<RectTransform>();
        emptyR.anchoredPosition = new Vector2(0f, -10f);
        emptyR.sizeDelta = new Vector2(580f, 100f);

        emptyNoticeText = emptyGO.AddComponent<Text>();
        emptyNoticeText.font = font;
        emptyNoticeText.fontSize = 18;
        emptyNoticeText.fontStyle = FontStyle.Bold;
        emptyNoticeText.color = new Color(0.8f, 0.8f, 0.8f);
        emptyNoticeText.alignment = TextAnchor.MiddleCenter;

        // Bottom Button: ĐÓNG
        var closeGO = new GameObject("CloseBtn");
        closeGO.transform.SetParent(boxGO.transform, false);
        var closeR = closeGO.AddComponent<RectTransform>();
        closeR.anchoredPosition = new Vector2(0f, -210f);
        closeR.sizeDelta = new Vector2(260f, 48f);

        var closeImg = closeGO.AddComponent<Image>();
        closeImg.color = new Color(0.18f, 0.28f, 0.42f);

        var closeOl = closeGO.AddComponent<Outline>();
        closeOl.effectColor = new Color(0.4f, 0.85f, 1f);
        closeOl.effectDistance = new Vector2(1.5f, -1.5f);

        var closeBtn = closeGO.AddComponent<Button>();
        closeBtn.onClick.AddListener(ClosePanel);

        var cTxtGO = new GameObject("Text");
        cTxtGO.transform.SetParent(closeGO.transform, false);
        var cTxtR = cTxtGO.AddComponent<RectTransform>();
        cTxtR.anchorMin = Vector2.zero;
        cTxtR.anchorMax = Vector2.one;
        cTxtR.offsetMin = Vector2.zero;
        cTxtR.offsetMax = Vector2.zero;

        var cTxt = cTxtGO.AddComponent<Text>();
        cTxt.text = "[ ĐÓNG BẢNG XẾP HẠNG ]";
        cTxt.font = font;
        cTxt.fontSize = 16;
        cTxt.fontStyle = FontStyle.Bold;
        cTxt.color = Color.white;
        cTxt.alignment = TextAnchor.MiddleCenter;

        leaderboardPanel.SetActive(false);
    }
}
