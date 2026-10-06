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
            if (SessionState.GetBool("AllScenes_AutoSynced_V2", false)) return;
            SessionState.SetBool("AllScenes_AutoSynced_V2", true);
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
    public static void SyncCurrentSceneUI()
    {
        var scene = EditorSceneManager.GetActiveScene();
        if (scene.name.Contains("MainMenu")) return;

        // 1. Tìm hoặc kiểm tra GameplayCanvas
        Canvas gameplayCanvas = null;
        var canvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include);
        foreach (var c in canvases)
        {
            if (c.name.Contains("Gameplay") || c.GetComponent<GameplayUI>() != null)
            {
                gameplayCanvas = c;
                break;
            }
        }

        if (gameplayCanvas != null)
        {
            var gpUI = gameplayCanvas.GetComponent<GameplayUI>();
            if (gpUI == null) gpUI = gameplayCanvas.gameObject.AddComponent<GameplayUI>();

            // Đồng bộ thanh máu HP chuẩn Màn 2
            SyncHealthBarVisuals(gameplayCanvas.transform);

            // Đồng bộ Túi Đồ TAB
            var invUI = gameplayCanvas.GetComponent<InventoryUI>();
            if (invUI == null) invUI = gameplayCanvas.gameObject.AddComponent<InventoryUI>();
            invUI.EnsureInventoryPanel();

            EditorUtility.SetDirty(gameplayCanvas.gameObject);
        }

        // 2. Ẩn hoàn toàn các nút ảo cảm ứng (MobileInputCanvas, InteractButton, RunButton, Joystick)
        foreach (var c in canvases)
        {
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

        // Tìm thêm các nút bấm ảo riêng rẽ nếu còn sót
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

        // 3. Đồng bộ cơ chế Điện Giật -5 Máu cho Robot AI ở Màn 1 & Màn 2
        SyncRobotElectricShock();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    /// <summary>
    /// Đồng bộ cấu hình Điện Giật -5 Máu khi chạm vào Robot AI.
    /// </summary>
    private static void SyncRobotElectricShock()
    {
        var robots = Object.FindObjectsByType<RobotAI>(FindObjectsInactive.Include);
        foreach (var r in robots)
        {
            var so = new SerializedObject(r);
            var spEnable = so.FindProperty("enableShockOnTouch");
            var spDmg = so.FindProperty("shockDamage");
            var spDist = so.FindProperty("shockTouchDistance");
            var spCd = so.FindProperty("shockCooldown");

            if (spEnable != null) spEnable.boolValue = true;
            if (spDmg != null) spDmg.intValue = 5;
            if (spDist != null) spDist.floatValue = 1.35f;
            if (spCd != null) spCd.floatValue = 1.0f;
            so.ApplyModifiedProperties();

            // Đảm bảo có Trigger Collider và Kinematic Rigidbody
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
                rb.isKinematic = true;
                rb.useGravity = false;
            }

            EditorUtility.SetDirty(r.gameObject);
        }
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

        var pnlR = hpPnl.GetComponent<RectTransform>() ?? hpPnl.AddComponent<RectTransform>();
        pnlR.anchorMin = new Vector2(0f, 1f);
        pnlR.anchorMax = new Vector2(0f, 1f);
        pnlR.pivot = new Vector2(0f, 1f);
        pnlR.anchoredPosition = new Vector2(20f, -20f);
        pnlR.sizeDelta = new Vector2(290f, 62f);

        var pnlImg = hpPnl.GetComponent<Image>() ?? hpPnl.AddComponent<Image>();
        pnlImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        // HPText
        Transform hpTextTrans = hpPanelTrans.Find("HPText") ?? hpPanelTrans.Find("Text");
        if (hpTextTrans == null)
        {
            var txtGO = new GameObject("HPText");
            txtGO.transform.SetParent(hpPanelTrans, false);
            hpTextTrans = txtGO.transform;
        }

        var txtR = hpTextTrans.GetComponent<RectTransform>() ?? hpTextTrans.gameObject.AddComponent<RectTransform>();
        txtR.anchorMin = new Vector2(0.5f, 1f);
        txtR.anchorMax = new Vector2(0.5f, 1f);
        txtR.pivot = new Vector2(0.5f, 1f);
        txtR.anchoredPosition = new Vector2(0f, -6f);
        txtR.sizeDelta = new Vector2(260f, 24f);

        var hpTxt = hpTextTrans.GetComponent<Text>() ?? hpTextTrans.gameObject.AddComponent<Text>();
        hpTxt.text = "HP: 100 / 100";
        hpTxt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        hpTxt.fontSize = 15;
        hpTxt.fontStyle = FontStyle.Bold;
        hpTxt.alignment = TextAnchor.MiddleCenter;
        hpTxt.color = Color.white;

        var ol = hpTxt.GetComponent<Outline>() ?? hpTxt.gameObject.AddComponent<Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.9f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        // HPSlider
        Transform slTrans = hpPanelTrans.Find("HPSlider") ?? hpPanelTrans.Find("Slider");
        if (slTrans == null)
        {
            var slGO = new GameObject("HPSlider");
            slGO.transform.SetParent(hpPanelTrans, false);
            slTrans = slGO.transform;
        }

        var slR = slTrans.GetComponent<RectTransform>() ?? slTrans.gameObject.AddComponent<RectTransform>();
        slR.anchorMin = new Vector2(0f, 0f);
        slR.anchorMax = new Vector2(1f, 0f);
        slR.pivot = new Vector2(0.5f, 0f);
        slR.anchoredPosition = new Vector2(0f, 10f);
        slR.sizeDelta = new Vector2(-24f, 20f);

        var sl = slTrans.GetComponent<Slider>() ?? slTrans.gameObject.AddComponent<Slider>();
        sl.interactable = false;
        sl.transition = Selectable.Transition.None;

        var bgImg = slTrans.GetComponent<Image>() ?? slTrans.gameObject.AddComponent<Image>();
        bgImg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);

        // FillArea & Fill
        Transform faTrans = slTrans.Find("FillArea") ?? slTrans.Find("Fill Area");
        if (faTrans == null)
        {
            var faGO = new GameObject("FillArea");
            faGO.transform.SetParent(slTrans, false);
            faTrans = faGO.transform;
        }
        var faR = faTrans.GetComponent<RectTransform>() ?? faTrans.gameObject.AddComponent<RectTransform>();
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
        var fiR = fiTrans.GetComponent<RectTransform>() ?? fiTrans.gameObject.AddComponent<RectTransform>();
        fiR.anchorMin = Vector2.zero;
        fiR.anchorMax = Vector2.one;
        fiR.offsetMin = Vector2.zero;
        fiR.offsetMax = Vector2.zero;

        var fiImg = fiTrans.GetComponent<Image>() ?? fiTrans.gameObject.AddComponent<Image>();
        fiImg.color = new Color(0.2f, 0.9f, 0.35f);

        sl.fillRect = fiR;
        sl.value = 1f;

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
