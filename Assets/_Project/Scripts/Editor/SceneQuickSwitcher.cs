#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// SceneQuickSwitcher: Menu tiện ích cho phép chuyển nhanh giữa các Scene trong Unity Editor,
/// đồng thời tự động đồng bộ hóa:
/// 1. Thanh máu HP chuẩn như Màn 2 (kích thước, vị trí, màu sắc, phông chữ).
/// 2. Bỏ hoàn toàn hiển thị nút E, RUN, Jump, Joystick ảo trên màn hình như Màn 2.
/// 3. Phím [TAB] để mở Túi Đồ / Trang Bị đồng bộ trên cả 3 màn.
/// 4. Cơ chế Cờ Lê & đồ họa làn hơi URP chân thật cho Màn 2.
/// </summary>
public static class SceneQuickSwitcher
{
    private const string SCENE_LEVEL1 = "Assets/_Project/Scenes/Lab.unity";
    private const string SCENE_LEVEL2 = "Assets/_Project/Scenes/Level2_Reactor.unity";
    private const string SCENE_LEVEL3 = "Assets/_Project/Scenes/Level3_Helipad.unity";
    private const string SCENE_MAINMENU = "Assets/_Project/Scenes/MainMenu.unity";

    private const string TEXTURE_STEAM = "Assets/_Project/Art/Textures/SteamParticle_Soft.png";
    private const string MAT_STEAM = "Assets/_Project/Art/Materials/M_SteamParticle_URP.mat";

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/1️⃣ Màn 1: Phòng Thí Nghiệm (Lab)", false, 1)]
    public static void OpenLevel1()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_LEVEL1);
            SyncCurrentSceneUI();
        }
    }

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/2️⃣ Màn 2: Khu Lò Phản Ứng (Reactor)", false, 2)]
    public static void OpenLevel2()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_LEVEL2);
            EnsureLevel2Upgraded(false);
            SyncCurrentSceneUI();
        }
    }

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/3️⃣ Màn 3: Sân Đỗ Trực Thăng (Helipad)", false, 3)]
    public static void OpenLevel3()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_LEVEL3);
            SyncCurrentSceneUI();
        }
    }

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/🏠 Menu Chính (MainMenu)", false, 4)]
    public static void OpenMainMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene(SCENE_MAINMENU);
        }
    }

    [MenuItem("EscapeTheLab/🔄 Đồng Bộ Hóa Cả 3 Màn (Thanh Máu, Bỏ Nút E/Run, Tab Túi Đồ, Robot Giật Điện)", false, 10)]
    public static void MenuSyncAllScenes()
    {
        SyncAllScenes(true);
    }

    [MenuItem("EscapeTheLab/💡 Tối Ưu Ánh Sáng Màn 1 (Đều, Đẹp, Không Chói)", false, 15)]
    public static void MenuOptimizeLabLighting()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (!scene.name.Contains("Lab"))
        {
            if (File.Exists(SCENE_LEVEL1)) EditorSceneManager.OpenScene(SCENE_LEVEL1);
            scene = EditorSceneManager.GetActiveScene();
        }
        OptimizeLabLighting(scene);
        SyncCurrentSceneUI();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EditorUtility.DisplayDialog("Thành Công",
            "ĐÃ TỐI ƯU HÓA TOÀN BỘ ÁNH SÁNG MÀN 1!\n\n" +
            "• Ánh sáng đồng đều, êm dịu: Không còn vùng tối mù mịt hay vệt chói gắt.\n" +
            "• Không còn cháy trần: Đèn LED âm trần thẩm mỹ cách xa trần (cường độ chuẩn 1.6f).\n" +
            "• Bóng đổ một hướng: Duy nhất 1 Directional Light chính tạo bóng đổ mềm mại, xóa bỏ bóng đổ chằng chịt nhiều hướng.\n" +
            "• Thanh máu & HUD: Đồng bộ chuẩn Màn 2, nổi rõ chữ HP: 100 / 100.",
            "Tuyệt Vời!");
    }

    [MenuItem("EscapeTheLab/🔧 Cập nhật Màn 2 (Cờ Lê & Làn Hơi)", false, 20)]
    public static void MenuUpgradeLevel2()
    {
        EnsureLevel2Upgraded(true);
    }

    [InitializeOnLoadMethod]
    private static void AutoRunOnLoad()
    {
        EditorApplication.delayCall += () =>
        {
            if (SessionState.GetBool("AllScenes_AutoSynced_V6", false)) return;
            SessionState.SetBool("AllScenes_AutoSynced_V6", true);
            SyncAllScenes(false);
        };
    }

    /// <summary>
    /// Đồng bộ hóa toàn bộ 3 màn chơi trong một lượt chạy.
    /// </summary>
    public static void SyncAllScenes(bool showDialog)
    {
        try
        {
            if (showDialog) EditorUtility.DisplayProgressBar("Đồng Bộ Hóa Cả 3 Màn", "Đang đồng bộ Màn 1: Phòng Thí Nghiệm...", 0.15f);
            if (File.Exists(SCENE_LEVEL1))
            {
                EditorSceneManager.OpenScene(SCENE_LEVEL1);
                SyncCurrentSceneUI();
            }

            if (showDialog) EditorUtility.DisplayProgressBar("Đồng Bộ Hóa Cả 3 Màn", "Đang đồng bộ Màn 2: Lò Phản Ứng...", 0.50f);
            if (File.Exists(SCENE_LEVEL2))
            {
                EditorSceneManager.OpenScene(SCENE_LEVEL2);
                EnsureLevel2Upgraded(false);
                SyncCurrentSceneUI();
            }

            if (showDialog) EditorUtility.DisplayProgressBar("Đồng Bộ Hóa Cả 3 Màn", "Đang đồng bộ Màn 3: Sân Đỗ Trực Thăng...", 0.85f);
            if (File.Exists(SCENE_LEVEL3))
            {
                EditorSceneManager.OpenScene(SCENE_LEVEL3);
                SyncCurrentSceneUI();
            }

            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Thành Công",
                    "ĐÃ ĐỒNG BỘ HÓA HOÀN TẤT CẢ 3 MÀN CHƠI!\n\n" +
                    "1. Thanh Máu: Toàn bộ 3 màn đã có thanh máu chuẩn đẹp mắt như Màn 2 (top-left, viền tối, thanh trượt đổi màu xanh/vàng/đỏ).\n" +
                    "2. Không còn nút E, Run: Đã ẩn toàn bộ các nút bấm cảm ứng ảo (E, Run, Jump, Joystick) trên màn hình để giao diện sạch sẽ và chuyên nghiệp như Màn 2.\n" +
                    "3. Túi Đồ [TAB]: Phím TAB hoặc I hoạt động đồng bộ trên cả 3 màn, hiển thị đầy đủ vật phẩm từng màn (Thẻ bảo mật, Cờ lê, Cầu chì, Nhiên liệu...).\n" +
                    "4. Điện Giật -5 Máu (Màn 1 & Màn 2): Robot khi di chuyển chạm vào người chơi sẽ phóng điện giật (-5 HP, tia sét neon, âm thanh BZZT! và đẩy lùi).",
                    "Tuyệt Vời!");
            }
            Debug.Log("<color=green>[SceneQuickSwitcher] ĐÃ ĐỒNG BỘ HÓA HOÀN TẤT CẢ 3 MÀN CHƠI!</color>");
        }
        catch (System.Exception ex)
        {
            if (showDialog)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", ex.Message, "Đóng");
            }
            Debug.LogError("[SceneQuickSwitcher] Lỗi đồng bộ: " + ex);
        }
    }

    /// <summary>
    /// Đồng bộ hóa UI cho Scene hiện đang mở:
    /// - Thanh máu chuẩn Màn 2
    /// - Ẩn nút E, Run ảo
    /// - Tích hợp InventoryUI với phím TAB
    /// </summary>
    /// <summary>
    /// Đồng bộ hóa UI và Hệ Thống cho Scene hiện đang mở:
    /// - GameplayCanvas độc lập luôn hiển thị (HP, Objective, InteractionPrompt, Notification, Túi Đồ TAB, Crosshair)
    /// - Ẩn hoàn toàn các nút ảo cảm ứng (MobileInputCanvas, InteractButton, RunButton, Joystick)
    /// - Robot AI: Đứng thẳng, gán AnimationController tuần hoàn không bị nằm chết, Rigidbody Kinematic, Giật điện -5 HP
    /// - Sửa các vật liệu màu hồng tím (URP Shader)
    /// </summary>
    public static void SyncCurrentSceneUI()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name.Contains("MainMenu")) return;

        EnsureEventSystem();

        // 1. Tìm hoặc TẠO GameplayCanvas độc lập
        Canvas gameplayCanvas = null;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in canvases)
        {
            if (c.name == "GameplayCanvas" || c.name.Contains("Gameplay"))
            {
                gameplayCanvas = c;
                break;
            }
        }

        if (gameplayCanvas == null)
        {
            var canvasGO = new GameObject("GameplayCanvas");
            gameplayCanvas = canvasGO.AddComponent<Canvas>();
            gameplayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            gameplayCanvas.sortingOrder = 10;

            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;

            canvasGO.AddComponent<GraphicRaycaster>();
            canvasGO.AddComponent<GameplayUI>();
            canvasGO.AddComponent<InventoryUI>();
        }

        // Đảm bảo GameplayCanvas luôn bật
        gameplayCanvas.enabled = true;
        gameplayCanvas.gameObject.SetActive(true);

        // 2. Tiêu diệt TẤT CẢ GameplayUI thừa trong Scene (chỉ giữ duy nhất 1 GameplayUI trên GameplayCanvas)
        var allGPUI = Object.FindObjectsByType<GameplayUI>(FindObjectsInactive.Include);
        foreach (var ui in allGPUI)
        {
            if (ui.gameObject != gameplayCanvas.gameObject)
            {
                Object.DestroyImmediate(ui);
            }
        }

        // 3. Tiêu diệt triệt để các panel trùng lặp trên các canvas khác (như MobileInputCanvas)
        string[] hudNames = new string[] {
            "HPPanel", "ObjectivePanel", "NotificationPanel", "InteractionPrompt",
            "HUD_Crosshair", "InventoryPanel", "TerminalUIPanel", "AccessCodeUIPanel",
            "WinPanel", "GameOverPanel"
        };

        foreach (var c in canvases)
        {
            if (c == gameplayCanvas || c == null) continue;
            foreach (var hName in hudNames)
            {
                var childTrans = c.transform.Find(hName);
                if (childTrans != null)
                {
                    if (gameplayCanvas.transform.Find(hName) == null)
                    {
                        childTrans.SetParent(gameplayCanvas.transform, false);
                    }
                    else
                    {
                        Object.DestroyImmediate(childTrans.gameObject);
                    }
                }
            }
        }

        // Xóa sạch các GameObject trùng lặp ngoài GameplayCanvas
        var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
        foreach (var t in allTransforms)
        {
            if (t == null) continue;
            if (t.name == "HPPanel" && t.parent != gameplayCanvas.transform)
            {
                Object.DestroyImmediate(t.gameObject);
            }
            else if (t.name == "ObjectivePanel" && t.parent != gameplayCanvas.transform)
            {
                Object.DestroyImmediate(t.gameObject);
            }
        }

        // Xóa duplicate HPPanel và ObjectivePanel bên trong GameplayCanvas (nếu có nhiều hơn 1)
        for (int i = gameplayCanvas.transform.childCount - 1; i >= 0; i--)
        {
            var child = gameplayCanvas.transform.GetChild(i);
            if (child.name == "HPPanel")
            {
                int firstIdx = -1;
                for (int j = 0; j < gameplayCanvas.transform.childCount; j++)
                {
                    if (gameplayCanvas.transform.GetChild(j).name == "HPPanel") { firstIdx = j; break; }
                }
                if (i != firstIdx) Object.DestroyImmediate(child.gameObject);
            }
            else if (child.name == "ObjectivePanel")
            {
                int firstIdx = -1;
                for (int j = 0; j < gameplayCanvas.transform.childCount; j++)
                {
                    if (gameplayCanvas.transform.GetChild(j).name == "ObjectivePanel") { firstIdx = j; break; }
                }
                if (i != firstIdx) Object.DestroyImmediate(child.gameObject);
            }
        }

        var gpUI = gameplayCanvas.gameObject.GetOrAddComponent<GameplayUI>();
        var invUI = gameplayCanvas.gameObject.GetOrAddComponent<InventoryUI>();

        // Đồng bộ toàn bộ các thành phần visual của GameplayUI
        SyncHealthBarVisuals(gameplayCanvas.transform);
        SyncObjectivePanelVisuals(gameplayCanvas.transform);
        SyncInteractionPromptVisuals(gameplayCanvas.transform);
        SyncNotificationVisuals(gameplayCanvas.transform);
        SyncCrosshairVisuals(gameplayCanvas.transform);
        invUI.EnsureInventoryPanel();
        gpUI.EnsureGameOverPanel();
        gpUI.EnsureWinPanel();

        // Đồng bộ PlayerInteraction & NotificationUI
        SyncPlayerInteractionReferences(gameplayCanvas.transform);

        EditorUtility.SetDirty(gameplayCanvas.gameObject);

        // 3. Ẩn hoàn toàn các nút ảo cảm ứng
        foreach (var c in Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include))
        {
            if (c == gameplayCanvas) continue;
            if (c.name.Contains("Mobile") || c.name.Contains("Touch"))
            {
                c.enabled = false;
                c.gameObject.SetActive(false);
                EditorUtility.SetDirty(c.gameObject);
            }
        }

        var mic = Object.FindAnyObjectByType<MobileInputController>(FindObjectsInactive.Include);
        if (mic != null)
        {
            mic.HideOnScreenButtons();
            EditorUtility.SetDirty(mic.gameObject);
        }

        var buttons = Object.FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (var btn in buttons)
        {
            string bName = btn.name.ToLower();
            if (bName.Contains("interactbutton") || bName.Contains("runbutton") || bName.Contains("jumpbutton") || bName.Contains("joystick"))
            {
                btn.gameObject.SetActive(false);
                EditorUtility.SetDirty(btn.gameObject);
            }
        }

        // 4. Đồng bộ Robot AI (Đứng thẳng, Animator controller không bị chết, Kinematic, Giật điện -5 HP)
        SyncRobots();

        // 5. Khắc phục vật liệu màu hồng tím (URP shader)
        FixPinkMaterials();

        // 6. Tối ưu hóa ánh sáng Màn 1 (Phòng Thí Nghiệm): loại bỏ đèn cháy trần, tạo ánh sáng êm dịu đồng đều
        if (scene.name.Contains("Lab") && !scene.name.Contains("Reactor") && !scene.name.Contains("Helipad"))
        {
            OptimizeLabLighting(scene);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    private static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
    }

    private static void SyncObjectivePanelVisuals(Transform canvasRoot)
    {
        Transform objTrans = canvasRoot.Find("ObjectivePanel");
        if (objTrans == null)
        {
            var pGO = new GameObject("ObjectivePanel");
            pGO.transform.SetParent(canvasRoot, false);
            objTrans = pGO.transform;
        }

        var objPnl = objTrans.gameObject;
        objPnl.SetActive(true);

        var pnlR = objPnl.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(1f, 1f);
        pnlR.anchorMax = new Vector2(1f, 1f);
        pnlR.pivot = new Vector2(1f, 1f);
        pnlR.anchoredPosition = new Vector2(-20f, -20f);
        pnlR.sizeDelta = new Vector2(340f, 85f);

        var pnlImg = objPnl.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        pnlImg.raycastTarget = false;

        var ol = objPnl.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0.1f, 0.5f, 0.9f, 0.7f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // Xóa sạch mọi Text thừa để tránh hiện tượng chữ đè chữ
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
                Object.DestroyImmediate(t.gameObject);
            }
        }

        Transform txtTrans = keeperTxt != null ? keeperTxt.transform : objTrans.Find("ObjectiveText");
        if (txtTrans == null)
        {
            var tGO = new GameObject("ObjectiveText");
            tGO.transform.SetParent(objTrans, false);
            txtTrans = tGO.transform;
        }

        var objTxt = txtTrans.gameObject.GetOrAddComponent<Text>();
        objTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        objTxt.fontSize = 15;
        objTxt.fontStyle = FontStyle.Normal;
        objTxt.alignment = TextAnchor.MiddleCenter;
        objTxt.color = Color.white;
        objTxt.raycastTarget = false;
        objTxt.text = "OBJECTIVE\nTìm Thẻ Bảo Mật (Security Card - Phòng B)";

        RectTransform txtR = txtTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = Vector2.zero;
        txtR.anchorMax = Vector2.one;
        txtR.offsetMin = new Vector2(10, 10);
        txtR.offsetMax = new Vector2(-10, -10);

        var gpUI = canvasRoot.GetComponent<GameplayUI>();
        if (gpUI != null)
        {
            var so = new SerializedObject(gpUI);
            var spPnl = so.FindProperty("objectivePanel");
            var spTxt = so.FindProperty("objectiveText");
            if (spPnl != null) spPnl.objectReferenceValue = objPnl;
            if (spTxt != null) spTxt.objectReferenceValue = objTxt;
            so.ApplyModifiedProperties();
        }
    }

    private static void SyncInteractionPromptVisuals(Transform canvasRoot)
    {
        Transform promptTrans = canvasRoot.Find("InteractionPrompt");
        if (promptTrans == null)
        {
            var pGO = new GameObject("InteractionPrompt");
            pGO.transform.SetParent(canvasRoot, false);
            promptTrans = pGO.transform;
        }

        var promptPnl = promptTrans.gameObject;
        var pnlR = promptPnl.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(0.5f, 0f);
        pnlR.anchorMax = new Vector2(0.5f, 0f);
        pnlR.pivot = new Vector2(0.5f, 0f);
        pnlR.anchoredPosition = new Vector2(0f, 160f);
        pnlR.sizeDelta = new Vector2(400f, 54f);

        var pnlImg = promptPnl.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.05f, 0.08f, 0.15f, 0.94f);
        pnlImg.raycastTarget = false;

        var ol = promptPnl.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0f, 0.85f, 1f, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        Transform txtTrans = promptTrans.Find("PromptText") ?? promptTrans.Find("Text");
        if (txtTrans == null)
        {
            var tGO = new GameObject("PromptText");
            tGO.transform.SetParent(promptTrans, false);
            txtTrans = tGO.transform;
        }

        var promptTxt = txtTrans.gameObject.GetOrAddComponent<Text>();
        promptTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        promptTxt.fontSize = 17;
        promptTxt.fontStyle = FontStyle.Bold;
        promptTxt.alignment = TextAnchor.MiddleCenter;
        promptTxt.color = new Color(1f, 0.92f, 0.3f);
        promptTxt.raycastTarget = false;

        RectTransform txtR = txtTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = Vector2.zero;
        txtR.anchorMax = Vector2.one;
        txtR.offsetMin = Vector2.zero;
        txtR.offsetMax = Vector2.zero;

        promptPnl.SetActive(false);

        var gpUI = canvasRoot.GetComponent<GameplayUI>();
        if (gpUI != null)
        {
            var so = new SerializedObject(gpUI);
            var spPnl = so.FindProperty("interactionPromptPanel");
            var spTxt = so.FindProperty("interactionPromptText");
            if (spPnl != null) spPnl.objectReferenceValue = promptPnl;
            if (spTxt != null) spTxt.objectReferenceValue = promptTxt;
            so.ApplyModifiedProperties();
        }
    }

    private static void SyncNotificationVisuals(Transform canvasRoot)
    {
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

        var cg = pnl.GetOrAddComponent<CanvasGroup>();
        cg.alpha = 0f;
        pnl.SetActive(false);

        NotificationUI nui = Object.FindAnyObjectByType<NotificationUI>(FindObjectsInactive.Include);
        if (nui == null)
        {
            var nuiGO = new GameObject("NotificationUI");
            nuiGO.transform.SetParent(canvasRoot, false);
            nui = nuiGO.AddComponent<NotificationUI>();
        }
        nui.SetNotificationReferences(pnl, txt);
        var soNui = new SerializedObject(nui);
        var spPnl = soNui.FindProperty("notificationPanel");
        var spTxt = soNui.FindProperty("messageText");
        if (spPnl != null) spPnl.objectReferenceValue = pnl;
        if (spTxt != null) spTxt.objectReferenceValue = txt;
        soNui.ApplyModifiedProperties();
        EditorUtility.SetDirty(nui);
    }

    private static void SyncCrosshairVisuals(Transform canvasRoot)
    {
        Transform crosshairTrans = canvasRoot.Find("HUD_Crosshair");
        if (crosshairTrans == null)
        {
            var chGO = new GameObject("HUD_Crosshair");
            chGO.transform.SetParent(canvasRoot, false);
            var rt = chGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(8, 8);
            rt.anchoredPosition = Vector2.zero;

            var img = chGO.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);
            img.raycastTarget = false;

            var ol = chGO.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.8f);
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }

    private static void SyncPlayerInteractionReferences(Transform canvasRoot)
    {
        var pi = Object.FindAnyObjectByType<PlayerInteraction>(FindObjectsInactive.Include);
        if (pi != null)
        {
            Transform promptTrans = canvasRoot.Find("InteractionPrompt");
            if (promptTrans != null)
            {
                var so = new SerializedObject(pi);
                var prop = so.FindProperty("interactionPromptUI");
                if (prop != null)
                {
                    prop.objectReferenceValue = promptTrans.gameObject;
                    so.ApplyModifiedProperties();
                }
            }
            EditorUtility.SetDirty(pi);
        }
    }

    /// <summary>
    /// Đồng bộ cấu hình Robot AI: Đứng thẳng, Animator controller không bị chết, Rigidbody kinematic, Điện Giật -5 Máu.
    /// </summary>
    private static void SyncRobots()
    {
        var animController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>("Assets/SciFiWarriorPBRHPPolyart/Animators/SciFiWarrior.controller");

        var robots = Object.FindObjectsByType<RobotAI>(FindObjectsInactive.Include);
        foreach (var r in robots)
        {
            // 1. Đứng thẳng, không bị nghiêng ngã
            Vector3 curEuler = r.transform.eulerAngles;
            r.transform.eulerAngles = new Vector3(0f, curEuler.y, 0f);

            // 2. Animator controller
            var anim = r.GetComponent<Animator>();
            if (anim != null && animController != null)
            {
                anim.runtimeAnimatorController = animController;
                anim.applyRootMotion = false;
            }

            // 3. Trigger Collider và Kinematic Rigidbody
            var colliders = r.GetComponents<Collider>();
            bool hasTrigger = false;
            foreach (var col in colliders)
            {
                if (col.isTrigger) { hasTrigger = true; break; }
            }
            if (!hasTrigger)
            {
                var sc = r.gameObject.AddComponent<SphereCollider>();
                sc.isTrigger = true;
                sc.radius = 0.95f;
                sc.center = new Vector3(0f, 0.9f, 0f);
            }

            var rb = r.GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = r.gameObject.AddComponent<Rigidbody>();
            }
            rb.isKinematic = true;
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeAll;

            // 4. Điện giật -5 Máu
            var so = new SerializedObject(r);
            var spEnable = so.FindProperty("enableShockOnTouch");
            var spDmg = so.FindProperty("shockDamage");
            var spDist = so.FindProperty("shockTouchDistance");
            var spCd = so.FindProperty("shockCooldown");

            var spAtk = so.FindProperty("attackDamage");
            if (spEnable != null) spEnable.boolValue = true;
            if (spDmg != null) spDmg.intValue = 5;
            if (spAtk != null) spAtk.intValue = 20;
            if (spDist != null) spDist.floatValue = 1.35f;
            if (spCd != null) spCd.floatValue = 1.0f;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(r.gameObject);
        }
    }

    /// <summary>
    /// Sửa các vật liệu màu hồng tím (Pink Shader Error) sang URP Shader tương thích.
    /// </summary>
    private static void FixPinkMaterials()
    {
        Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
        Shader urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (urpParticlesUnlit == null) urpParticlesUnlit = Shader.Find("Universal Render Pipeline/Unlit");

        string[] leafMats = new string[] {
            "Assets/Sci-Fi Styled Modular Pack/Materials/nature_leaves.mat",
            "Assets/Sci-Fi Styled Modular Pack/Materials/nature_bush.mat"
        };

        foreach (var p in leafMats)
        {
            var mat = AssetDatabase.LoadAssetAtPath<Material>(p);
            if (mat != null && urpLit != null)
            {
                mat.shader = urpLit;
                mat.SetFloat("_AlphaClip", 1f);
                mat.SetFloat("_Cutoff", 0.4f);
                mat.EnableKeyword("_ALPHATEST_ON");
                EditorUtility.SetDirty(mat);
            }
        }

        var holo = AssetDatabase.LoadAssetAtPath<Material>("Assets/Sci-Fi Styled Modular Pack/Materials/hologram_particle.mat");
        if (holo != null && urpParticlesUnlit != null)
        {
            holo.shader = urpParticlesUnlit;
            EditorUtility.SetDirty(holo);
        }

        // Quét thêm bất kỳ material nào trong scene đang có shader bị lỗi (Hidden/InternalErrorShader)
        var renderers = Object.FindObjectsByType<Renderer>(FindObjectsInactive.Include);
        foreach (var rend in renderers)
        {
            foreach (var m in rend.sharedMaterials)
            {
                if (m != null && m.shader != null && (m.shader.name.Contains("Error") || m.shader.name.StartsWith("Hidden/")))
                {
                    if (urpLit != null)
                    {
                        m.shader = urpLit;
                        EditorUtility.SetDirty(m);
                    }
                }
            }
        }
    }

    /// <summary>
    /// Tối ưu hóa toàn diện hệ thống ánh sáng Màn 1 (Phòng Thí Nghiệm):
    /// - Loại bỏ hoàn toàn các đèn rọi cũ cường độ quá mạnh (28-30 intensity), đèn trùng lặp gây cháy trần & bóng đổ rối mắt.
    /// - Ambient Light dịu mắt, đồng đều: không còn bất kỳ góc tối mù mịt hay vệt chói loá nào.
    /// - Duy nhất 1 Directional Key Light với Soft Shadow tinh tế (shadowStrength = 0.35), giúp mọi bóng đổ tự nhiên theo một hướng thống nhất.
    /// - 13 bộ đèn LED âm trần Sci-Fi được căn chỉnh chuẩn xác: Y = 3.85m (cách xa trần), intensity = 1.6f, range = 14m, không đổ bóng đa hướng (shadows = None).
    /// </summary>
    public static void OptimizeLabLighting(UnityEngine.SceneManagement.Scene scene)
    {
        // 1. Cấu hình Ambient Light môi trường sạch sẽ, dịu mắt, hiện rõ mọi chi tiết
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.42f, 0.45f, 0.50f); // Tông xám bạc sci-fi phòng thí nghiệm

        // 2. Thiết lập DUY NHẤT 1 Directional Light chính, tạo bóng đổ tự nhiên một hướng
        var allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        Light mainDirLight = null;
        foreach (var l in allLights)
        {
            if (l.type == LightType.Directional)
            {
                if (mainDirLight == null)
                {
                    mainDirLight = l;
                    mainDirLight.name = "Directional_Lab_Light";
                }
                else
                {
                    Undo.DestroyObjectImmediate(l.gameObject);
                }
            }
        }

        if (mainDirLight == null)
        {
            var dirGO = new GameObject("Directional_Lab_Light");
            mainDirLight = dirGO.AddComponent<Light>();
            mainDirLight.type = LightType.Directional;
        }

        mainDirLight.color = new Color(0.94f, 0.97f, 1.0f);
        mainDirLight.intensity = 0.85f;
        mainDirLight.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
        mainDirLight.shadows = LightShadows.Soft;
        mainDirLight.shadowStrength = 0.35f; // Bóng mờ nhẹ, không làm đen kịt mặt sàn hay chân ghế
        mainDirLight.shadowBias = 0.05f;
        mainDirLight.shadowNormalBias = 0.4f;
        EditorUtility.SetDirty(mainDirLight.gameObject);

        // 3. Tìm hoặc làm mới Lighting Group trong === LAB ENVIRONMENT ===
        var envRoot = GameObject.Find("=== LAB ENVIRONMENT ===");
        Transform lightsParent = null;
        if (envRoot != null)
        {
            var oldLighting = envRoot.transform.Find("Lighting");
            if (oldLighting != null)
            {
                Undo.DestroyObjectImmediate(oldLighting.gameObject);
            }
            var oldLights = envRoot.transform.Find("Lights");
            if (oldLights != null)
            {
                Undo.DestroyObjectImmediate(oldLights.gameObject);
            }

            var lightsGO = new GameObject("Lighting");
            lightsGO.transform.SetParent(envRoot.transform, false);
            lightsParent = lightsGO.transform;
        }
        else
        {
            var lightsGO = GameObject.Find("Lab_Lighting_System");
            if (lightsGO != null) Undo.DestroyObjectImmediate(lightsGO);
            lightsGO = new GameObject("Lab_Lighting_System");
            lightsParent = lightsGO.transform;
        }

        // Xóa bất kỳ CeilingLamp hoặc Point Light cũ cường độ cao trôi nổi nào trong scene
        var strayLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        foreach (var sl in strayLights)
        {
            if (sl == null || sl == mainDirLight) continue;
            string n = sl.gameObject.name.ToLower();
            if (n.Contains("ceilinglamp") || n.Contains("lamppoint") || (sl.type == LightType.Point && sl.intensity > 4.5f))
            {
                Undo.DestroyObjectImmediate(sl.gameObject);
            }
        }

        // 4. Sinh các đèn LED Panel âm trần sang trọng, phân bổ đều khắp các phòng:
        // Chiều cao Y = 3.85m (cách trần phòng 1.15m, tránh hoàn toàn hiện tượng cháy/chói trần)
        Color coolLabLight = new Color(0.88f, 0.95f, 1.0f);
        Color serverBlueLight = new Color(0.75f, 0.90f, 1.0f);
        Color machineGreenLight = new Color(0.82f, 0.98f, 0.88f);
        Color exitWarmLight = new Color(0.98f, 0.94f, 0.88f);

        // A. Sảnh chính (Main Hall: 20x24m) - 5 đèn bố trí đều đặn, cân đối
        CreateModernLabLamp(lightsParent, new Vector3(-5f, 3.85f, -5f), coolLabLight);
        CreateModernLabLamp(lightsParent, new Vector3( 5f, 3.85f, -5f), coolLabLight);
        CreateModernLabLamp(lightsParent, new Vector3(-5f, 3.85f,  5f), coolLabLight);
        CreateModernLabLamp(lightsParent, new Vector3( 5f, 3.85f,  5f), coolLabLight);
        CreateModernLabLamp(lightsParent, new Vector3( 0f, 3.85f,  0f), coolLabLight);

        // B. Phòng A - Máy Móc (X=-16, 12x16m) - 2 đèn phân bổ trục giữa
        CreateModernLabLamp(lightsParent, new Vector3(-16f, 3.85f,  6f), machineGreenLight);
        CreateModernLabLamp(lightsParent, new Vector3(-16f, 3.85f, -2f), machineGreenLight);

        // C. Phòng B - Máy Chủ Server (X=16, 12x16m) - 2 đèn phân bổ trục giữa
        CreateModernLabLamp(lightsParent, new Vector3(16f, 3.85f,  6f), serverBlueLight);
        CreateModernLabLamp(lightsParent, new Vector3(16f, 3.85f, -2f), serverBlueLight);

        // D. Phòng C - Cửa Thoát Hiểm (Z=20, 14x10m) - 2 đèn
        CreateModernLabLamp(lightsParent, new Vector3(-3.5f, 3.85f, 20f), exitWarmLight);
        CreateModernLabLamp(lightsParent, new Vector3( 3.5f, 3.85f, 20f), exitWarmLight);

        // E. Hai hành lang kết nối (Corridor A & B) & Hành lang cửa thoát
        CreateModernLabLamp(lightsParent, new Vector3(-9.5f, 3.85f, 4f), coolLabLight);
        CreateModernLabLamp(lightsParent, new Vector3( 9.5f, 3.85f, 4f), coolLabLight);
        CreateModernLabLamp(lightsParent, new Vector3(  0f,  3.85f, 13f), coolLabLight);

        // 5. Đèn cảnh báo Robot Alert Light (mặc định tắt, chỉ nháy khi báo động)
        var alertGO = new GameObject("RobotAlertLight");
        alertGO.transform.SetParent(lightsParent, false);
        alertGO.transform.position = new Vector3(0, 4.0f, 0);
        var al = alertGO.AddComponent<Light>();
        al.type = LightType.Point;
        al.color = Color.red;
        al.intensity = 0f;
        al.range = 25f;
        al.shadows = LightShadows.None;

        Debug.Log("<color=cyan>[SceneQuickSwitcher] Đã tối ưu hóa hệ thống ánh sáng Màn 1: Phân bổ đều, êm dịu, loại bỏ triệt để các khoảng tối/sáng bất thường!</color>");
    }

    private static void CreateModernLabLamp(Transform parent, Vector3 pos, Color col)
    {
        var lampRoot = new GameObject("LabCeilingLamp");
        lampRoot.transform.SetParent(parent, false);
        lampRoot.transform.position = pos;

        // Point Light dịu mát, không đổ bóng để không tạo bóng chồng chéo rối mắt
        var l = lampRoot.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.intensity = 1.6f;     // Cường độ vừa vặn, chuẩn xác (không làm chói/cháy bề mặt)
        l.range = 14f;          // Độ phủ rộng, tán xạ mượt mà
        l.shadows = LightShadows.None; // Không tạo bóng chằng chịt nhiều hướng

        // Mô hình máng đèn LED treo trần Sci-Fi
        var housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
        housing.name = "LampHousing";
        housing.transform.SetParent(lampRoot.transform, false);
        housing.transform.localPosition = new Vector3(0, 0.04f, 0);
        housing.transform.localScale = new Vector3(1.7f, 0.08f, 0.42f);
        var colHousing = housing.GetComponent<Collider>();
        if (colHousing != null) Object.DestroyImmediate(colHousing);

        var housingMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        housingMat.color = new Color(0.2f, 0.22f, 0.25f);
        housingMat.SetFloat("_Smoothness", 0.6f);
        housing.GetComponent<Renderer>().material = housingMat;

        // Dải LED phát sáng bên dưới máng đèn
        var ledStrip = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ledStrip.name = "LEDStrip";
        ledStrip.transform.SetParent(lampRoot.transform, false);
        ledStrip.transform.localPosition = new Vector3(0, -0.01f, 0);
        ledStrip.transform.localScale = new Vector3(1.5f, 0.03f, 0.26f);
        var colStrip = ledStrip.GetComponent<Collider>();
        if (colStrip != null) Object.DestroyImmediate(colStrip);

        var ledMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        ledMat.color = col;
        ledMat.EnableKeyword("_EMISSION");
        ledMat.SetColor("_EmissionColor", col * 1.6f);
        ledStrip.GetComponent<Renderer>().material = ledMat;

        lampRoot.isStatic = true;
        housing.isStatic = true;
        ledStrip.isStatic = true;
    }

    /// <summary>
    /// Đồng bộ kích thước, layout và màu sắc thanh máu chuẩn như Màn 2.
    /// </summary>
    private static void SyncHealthBarVisuals(Transform canvasRoot)
    {
        // Tìm HPPanel
        Transform hpPanelTrans = canvasRoot.Find("HPPanel");
        if (hpPanelTrans == null)
        {
            var pnlGO = new GameObject("HPPanel");
            pnlGO.transform.SetParent(canvasRoot, false);
            hpPanelTrans = pnlGO.transform;
        }

        var hpPnl = hpPanelTrans.gameObject;
        hpPnl.SetActive(true);

        var pnlR = hpPnl.GetOrAddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(0f, 1f);
        pnlR.anchorMax = new Vector2(0f, 1f);
        pnlR.pivot = new Vector2(0f, 1f);
        pnlR.anchoredPosition = new Vector2(20f, -20f);
        pnlR.sizeDelta = new Vector2(290f, 62f);

        var pnlImg = hpPnl.GetOrAddComponent<Image>();
        pnlImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
        pnlImg.raycastTarget = false;

        var pnlOl = hpPnl.GetOrAddComponent<Outline>();
        pnlOl.effectColor = new Color(0.1f, 0.4f, 0.7f, 0.5f);
        pnlOl.effectDistance = new Vector2(1f, -1f);

        // HPSlider
        Transform slTrans = hpPanelTrans.Find("HPSlider") ?? hpPanelTrans.Find("Slider");
        if (slTrans == null)
        {
            var slGO = new GameObject("HPSlider");
            slGO.transform.SetParent(hpPanelTrans, false);
            slTrans = slGO.transform;
        }

        var slR = slTrans.gameObject.GetOrAddComponent<RectTransform>();
        slR.anchorMin = new Vector2(0f, 0f);
        slR.anchorMax = new Vector2(1f, 0f);
        slR.pivot = new Vector2(0.5f, 0f);
        slR.anchoredPosition = new Vector2(0f, 10f);
        slR.sizeDelta = new Vector2(-24f, 18f);

        var sl = slTrans.gameObject.GetOrAddComponent<Slider>();
        sl.interactable = false;
        sl.transition = Selectable.Transition.None;

        var bgImg = slTrans.gameObject.GetOrAddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);
        bgImg.raycastTarget = false;

        // FillArea & Fill
        Transform faTrans = slTrans.Find("FillArea") ?? slTrans.Find("Fill Area");
        if (faTrans == null)
        {
            var faGO = new GameObject("FillArea");
            faGO.transform.SetParent(slTrans, false);
            faTrans = faGO.transform;
        }
        var faR = faTrans.gameObject.GetOrAddComponent<RectTransform>();
        faR.anchorMin = Vector2.zero;
        faR.anchorMax = Vector2.one;
        faR.offsetMin = new Vector2(2f, 2f);
        faR.offsetMax = new Vector2(-2f, -2f);

        Transform fiTrans = faTrans.Find("Fill");
        if (fiTrans == null)
        {
            var fiGO = new GameObject("Fill");
            fiGO.transform.SetParent(faTrans, false);
            fiTrans = fiGO.transform;
        }
        var fiR = fiTrans.gameObject.GetOrAddComponent<RectTransform>();
        fiR.anchorMin = Vector2.zero;
        fiR.anchorMax = Vector2.one;
        fiR.offsetMin = Vector2.zero;
        fiR.offsetMax = Vector2.zero;

        var fiImg = fiTrans.gameObject.GetOrAddComponent<Image>();
        fiImg.color = new Color(0.2f, 0.9f, 0.35f);
        fiImg.raycastTarget = false;

        sl.fillRect = fiR;
        sl.value = 1f;

        // Xóa sạch các Text thừa trong HPPanel
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
                Object.DestroyImmediate(t.gameObject);
            }
        }

        Transform hpTextTrans = keeperTxt != null ? keeperTxt.transform : hpPanelTrans.Find("HPText");
        if (hpTextTrans == null)
        {
            var txtGO = new GameObject("HPText");
            txtGO.transform.SetParent(hpPanelTrans, false);
            hpTextTrans = txtGO.transform;
        }

        var txtR = hpTextTrans.gameObject.GetOrAddComponent<RectTransform>();
        txtR.anchorMin = new Vector2(0.5f, 1f);
        txtR.anchorMax = new Vector2(0.5f, 1f);
        txtR.pivot = new Vector2(0.5f, 1f);
        txtR.anchoredPosition = new Vector2(0f, -6f);
        txtR.sizeDelta = new Vector2(260f, 24f);

        var hpTxt = hpTextTrans.gameObject.GetOrAddComponent<Text>();
        hpTxt.text = "HP: 100 / 100";
        hpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpTxt.fontSize = 15;
        hpTxt.fontStyle = FontStyle.Bold;
        hpTxt.alignment = TextAnchor.MiddleCenter;
        hpTxt.color = Color.white;
        hpTxt.raycastTarget = false;

        var ol = hpTxt.gameObject.GetOrAddComponent<Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.9f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        hpTextTrans.SetAsLastSibling();

        // Cập nhật SerializedObject cho GameplayUI
        var gpUI = canvasRoot.GetComponent<GameplayUI>();
        if (gpUI != null)
        {
            var so = new SerializedObject(gpUI);
            so.FindProperty("hpBar").objectReferenceValue = sl;
            so.FindProperty("hpText").objectReferenceValue = hpTxt;
            so.FindProperty("hpFillImage").objectReferenceValue = fiImg;
            so.ApplyModifiedProperties();
        }
    }

    /// <summary>
    /// Nâng cấp Màn 2: Cờ Lê & Làn hơi nước URP.
    /// </summary>
    public static void EnsureLevel2Upgraded(bool showDialog)
    {
        try
        {
            if (showDialog) EditorUtility.DisplayProgressBar("Cập Nhật Màn 2", "Chuẩn bị Material & Texture...", 0.1f);
            var steamMat = EnsureSteamMaterial();

            if (showDialog) EditorUtility.DisplayProgressBar("Cập Nhật Màn 2", "Mở Scene Level 2...", 0.3f);
            if (EditorSceneManager.GetActiveScene().path != SCENE_LEVEL2)
            {
                EditorSceneManager.OpenScene(SCENE_LEVEL2);
            }
            var scene = EditorSceneManager.GetActiveScene();

            if (showDialog) EditorUtility.DisplayProgressBar("Cập Nhật Màn 2", "Cập nhật hiệu ứng làn hơi cho 3 van...", 0.5f);
            UpgradeValvesSteam(steamMat);

            if (showDialog) EditorUtility.DisplayProgressBar("Cập Nhật Màn 2", "Thiết lập Bàn Kỹ Thuật & Cờ Lê 3D...", 0.7f);
            SetupTechWorkbenchAndWrench();

            if (showDialog) EditorUtility.DisplayProgressBar("Cập Nhật Màn 2", "Lưu Scene & Project...", 0.9f);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();

            if (showDialog)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Thành Công",
                    "Đã nâng cấp Màn 2 thành công!\n\n" +
                    "• Cơ chế: Phải nhặt Cờ Lê tại Kho Phụ Tùng mới vặn được van.\n" +
                    "• Đồ họa: Làn hơi nước URP trắng mờ bốc lên chân thật (không còn màu hồng tím).\n" +
                    "• Cờ Lê 3D: Đặt trên bàn kỹ thuật với đèn chiếu sáng và biển hướng dẫn trực quan.",
                    "OK");
            }
            Debug.Log("<color=green>[SceneQuickSwitcher] Cập nhật Màn 2 (Cờ Lê & Làn hơi) hoàn tất!</color>");
        }
        catch (System.Exception ex)
        {
            if (showDialog)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Lỗi", ex.Message, "Đóng");
            }
            Debug.LogError("[SceneQuickSwitcher] Lỗi cập nhật: " + ex);
        }
    }

    private static Material EnsureSteamMaterial()
    {
        if (File.Exists(TEXTURE_STEAM))
        {
            TextureImporter importer = AssetImporter.GetAtPath(TEXTURE_STEAM) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.sRGBTexture = true;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
        }
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(TEXTURE_STEAM);

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

        var dir = Path.GetDirectoryName(MAT_STEAM);
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        Material mat = AssetDatabase.LoadAssetAtPath<Material>(MAT_STEAM);
        if (mat == null)
        {
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, MAT_STEAM);
        }
        else
        {
            mat.shader = shader;
        }

        mat.SetFloat("_Surface", 1f);
        mat.SetFloat("_Blend", 0f);
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        mat.SetColor("_BaseColor", new Color(0.92f, 0.96f, 1f, 0.45f));
        if (tex != null)
        {
            mat.SetTexture("_BaseMap", tex);
            mat.mainTexture = tex;
        }

        EditorUtility.SetDirty(mat);
        AssetDatabase.SaveAssets();
        return mat;
    }

    private static void UpgradeValvesSteam(Material steamMat)
    {
        var valves = Object.FindObjectsByType<CoolingValve>(FindObjectsInactive.Include);
        foreach (var valve in valves)
        {
            var valveGO = valve.gameObject;
            Transform steamTrans = valveGO.transform.Find("SteamJet");
            GameObject steamGO;
            if (steamTrans == null)
            {
                steamGO = new GameObject("SteamJet");
                steamGO.transform.SetParent(valveGO.transform, false);
                steamGO.transform.localPosition = new Vector3(0, 0, 0.45f);
                steamGO.transform.localRotation = Quaternion.identity;
            }
            else
            {
                steamGO = steamTrans.gameObject;
            }

            var ps = steamGO.GetComponent<ParticleSystem>();
            if (ps == null) ps = steamGO.AddComponent<ParticleSystem>();

            var psr = steamGO.GetComponent<ParticleSystemRenderer>();
            if (psr == null) psr = steamGO.AddComponent<ParticleSystemRenderer>();

            psr.material = steamMat;
            psr.renderMode = ParticleSystemRenderMode.Billboard;
            psr.alignment = ParticleSystemRenderSpace.View;
            psr.sortMode = ParticleSystemSortMode.Distance;
            psr.minParticleSize = 0f;
            psr.maxParticleSize = 1.0f;

            var main = ps.main;
            main.duration = 1.0f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.7f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 6.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.70f);
            main.startColor = new Color(0.92f, 0.96f, 1.0f, 0.50f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
            main.gravityModifier = -0.04f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 90;
            main.playOnAwake = true;

            var emission = ps.emission;
            emission.enabled = true;
            emission.rateOverTime = 32f;

            var shape = ps.shape;
            shape.enabled = true;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f;
            shape.radius = 0.08f;

            var sizeModule = ps.sizeOverLifetime;
            sizeModule.enabled = true;
            AnimationCurve sizeCurve = new AnimationCurve();
            sizeCurve.AddKey(0f, 0.35f);
            sizeCurve.AddKey(0.35f, 1.15f);
            sizeCurve.AddKey(1f, 2.3f);
            sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

            var colorModule = ps.colorOverLifetime;
            colorModule.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.95f, 0.98f, 1.0f), 0f),
                    new GradientColorKey(new Color(0.88f, 0.93f, 0.98f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0f, 0f),
                    new GradientAlphaKey(0.55f, 0.12f),
                    new GradientAlphaKey(0.40f, 0.60f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorModule.color = grad;

            var rotModule = ps.rotationOverLifetime;
            rotModule.enabled = true;
            rotModule.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);

            var lblTrans = valveGO.transform.Find("Label");
            if (lblTrans != null)
            {
                var tm = lblTrans.GetComponent<TextMesh>();
                if (tm != null) tm.text = "VAN XẢ ÁP SUẤT\n[ CẦN CỜ LÊ ]";
            }

            Transform wheel = valveGO.transform.Find("Wheel");
            Light statusLight = valveGO.transform.Find("StatusLight")?.GetComponent<Light>();
            GameObject dmgTrigger = valveGO.transform.Find("GasJetDamageZone")?.gameObject;
            valve.SetupReferences(wheel, statusLight, ps, dmgTrigger);

            var so = new SerializedObject(valve);
            if (wheel != null) so.FindProperty("wheelTransform").objectReferenceValue = wheel;
            if (statusLight != null) so.FindProperty("statusLight").objectReferenceValue = statusLight;
            so.FindProperty("steamEffect").objectReferenceValue = ps;
            if (dmgTrigger != null) so.FindProperty("gasDamageTrigger").objectReferenceValue = dmgTrigger;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(valveGO);
        }
    }

    private static void SetupTechWorkbenchAndWrench()
    {
        var oldWrench = GameObject.Find("Item_Wrench");
        Transform itemsParent = oldWrench != null ? oldWrench.transform.parent : null;
        if (itemsParent == null)
        {
            var itemsStage2 = GameObject.Find("Items_Stage2");
            if (itemsStage2 != null) itemsParent = itemsStage2.transform;
        }

        Vector3 benchPos = new Vector3(-6f, 0f, 22f);

        var benchRoot = GameObject.Find("Workbench_ToolStation");
        if (benchRoot == null)
        {
            benchRoot = new GameObject("Workbench_ToolStation");
            if (itemsParent != null) benchRoot.transform.SetParent(itemsParent.parent);
        }
        benchRoot.transform.position = benchPos;

        while (benchRoot.transform.childCount > 0)
        {
            Object.DestroyImmediate(benchRoot.transform.GetChild(0).gameObject);
        }

        Color steelColor = new Color(0.22f, 0.24f, 0.28f);
        Color topColor = new Color(0.18f, 0.19f, 0.21f);

        MakeBox(benchRoot.transform, "DeskTop", new Vector3(0, 0.85f, 0), new Vector3(2.4f, 0.12f, 1.4f), topColor, true);
        MakeBox(benchRoot.transform, "Leg_FL", new Vector3(-1.05f, 0.42f,  0.55f), new Vector3(0.12f, 0.84f, 0.12f), steelColor, true);
        MakeBox(benchRoot.transform, "Leg_FR", new Vector3( 1.05f, 0.42f,  0.55f), new Vector3(0.12f, 0.84f, 0.12f), steelColor, true);
        MakeBox(benchRoot.transform, "Leg_BL", new Vector3(-1.05f, 0.42f, -0.55f), new Vector3(0.12f, 0.84f, 0.12f), steelColor, true);
        MakeBox(benchRoot.transform, "Leg_BR", new Vector3( 1.05f, 0.42f, -0.55f), new Vector3(0.12f, 0.84f, 0.12f), steelColor, true);
        MakeBox(benchRoot.transform, "Shelf", new Vector3(0, 0.25f, 0), new Vector3(2.2f, 0.06f, 1.2f), steelColor * 0.8f, true);

        MakeBox(benchRoot.transform, "Toolbox", new Vector3(0.7f, 1.05f, 0.35f), new Vector3(0.55f, 0.26f, 0.35f), new Color(0.85f, 0.25f, 0.15f), true);
        MakeBox(benchRoot.transform, "PartsBin", new Vector3(-0.7f, 0.98f, 0.35f), new Vector3(0.45f, 0.14f, 0.32f), new Color(0.2f, 0.5f, 0.85f), true);

        var spotGO = new GameObject("WorkbenchLight");
        spotGO.transform.SetParent(benchRoot.transform);
        spotGO.transform.localPosition = new Vector3(0, 2.2f, 0);
        var light = spotGO.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = new Color(0.9f, 0.96f, 1.0f);
        light.range = 4.5f;
        light.intensity = 2.4f;
        light.shadows = LightShadows.Soft;

        var signGO = new GameObject("WorkbenchSign");
        signGO.transform.SetParent(benchRoot.transform);
        signGO.transform.localPosition = new Vector3(0, 2.0f, 0);
        signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        signGO.transform.localScale = Vector3.one * 0.025f;
        var tmSign = signGO.AddComponent<TextMesh>();
        tmSign.text = "▲ BÀN DỤNG CỤ: CỜ LÊ SỬA CHỮA ▲\n[ Bấm E để nhặt Cờ Lê ]";
        tmSign.fontSize = 36;
        tmSign.alignment = TextAlignment.Center;
        tmSign.anchor = TextAnchor.MiddleCenter;
        tmSign.color = Color.yellow;
        tmSign.fontStyle = FontStyle.Bold;

        if (oldWrench != null)
        {
            Object.DestroyImmediate(oldWrench);
        }

        var wrenchGO = new GameObject("Item_Wrench");
        wrenchGO.transform.SetParent(itemsParent != null ? itemsParent : benchRoot.transform);
        wrenchGO.transform.position = new Vector3(-6f, 1.25f, 22f);

        int intLayer = LayerMask.NameToLayer("Interactable");
        wrenchGO.layer = intLayer >= 0 ? intLayer : 0;

        var wrenchMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wrenchMat.color = new Color(0.88f, 0.90f, 0.94f);
        wrenchMat.SetFloat("_Metallic", 0.92f);
        wrenchMat.SetFloat("_Smoothness", 0.82f);

        var modelRoot = new GameObject("WrenchModel");
        modelRoot.transform.SetParent(wrenchGO.transform, false);
        modelRoot.transform.localRotation = Quaternion.Euler(20f, 45f, 0f);

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(modelRoot.transform, false);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.08f, 0.55f, 0.04f);
        handle.GetComponent<Renderer>().material = wrenchMat;
        Object.DestroyImmediate(handle.GetComponent<Collider>());

        var headBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        headBase.name = "JawHead";
        headBase.transform.SetParent(modelRoot.transform, false);
        headBase.transform.localPosition = new Vector3(0, 0.32f, 0);
        headBase.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f);
        headBase.GetComponent<Renderer>().material = wrenchMat;
        Object.DestroyImmediate(headBase.GetComponent<Collider>());

        var jawL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jawL.name = "Jaw_L";
        jawL.transform.SetParent(modelRoot.transform, false);
        jawL.transform.localPosition = new Vector3(-0.09f, 0.44f, 0);
        jawL.transform.localScale = new Vector3(0.06f, 0.18f, 0.04f);
        jawL.GetComponent<Renderer>().material = wrenchMat;
        Object.DestroyImmediate(jawL.GetComponent<Collider>());

        var jawR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jawR.name = "Jaw_R";
        jawR.transform.SetParent(modelRoot.transform, false);
        jawR.transform.localPosition = new Vector3(0.09f, 0.44f, 0);
        jawR.transform.localScale = new Vector3(0.06f, 0.18f, 0.04f);
        jawR.GetComponent<Renderer>().material = wrenchMat;
        Object.DestroyImmediate(jawR.GetComponent<Collider>());

        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "RingEnd";
        ring.transform.SetParent(modelRoot.transform, false);
        ring.transform.localPosition = new Vector3(0, -0.32f, 0);
        ring.transform.localScale = new Vector3(0.18f, 0.038f, 0.18f);
        ring.GetComponent<Renderer>().material = wrenchMat;
        Object.DestroyImmediate(ring.GetComponent<Collider>());

        var sc = wrenchGO.AddComponent<SphereCollider>();
        sc.radius = 1.2f;
        sc.isTrigger = true;

        var pickup = wrenchGO.AddComponent<PickupItem>();
        var soPickup = new SerializedObject(pickup);
        soPickup.FindProperty("itemType").enumValueIndex = (int)PickupItem.ItemType.Wrench;
        soPickup.FindProperty("itemName").stringValue = "Cờ Lê Sửa Chữa (Wrench)";
        soPickup.FindProperty("pickupMessage").stringValue = "✓ ĐÃ NHẶT CỜ LÊ SỬA CHỮA! Hãy dùng Cờ Lê để vặn khóa 3 van xả khí độc.";
        soPickup.FindProperty("bobSpeed").floatValue = 2.0f;
        soPickup.FindProperty("bobAmount").floatValue = 0.10f;
        soPickup.FindProperty("rotateSpeed").floatValue = 65f;
        soPickup.ApplyModifiedProperties();

        EditorUtility.SetDirty(wrenchGO);
        EditorUtility.SetDirty(benchRoot);
    }

    private static GameObject MakeBox(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool isStatic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.isStatic = isStatic;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.SetFloat("_Smoothness", 0.35f);
        go.GetComponent<Renderer>().material = mat;
        return go;
    }
}
#endif
