#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;

/// <summary>
/// EscapeTheLabSetupPhase2: Auto-setup Phase 3-8.
/// Menu: EscapeTheLab -> Setup Phase 3-8
/// Mo Lab scene truoc khi chay!
/// </summary>
public class EscapeTheLabSetupPhase2 : Editor
{
    // ─── Guard: tat ca menu item PHAI chay o Edit Mode ───────────────────────
    private static bool EnsureEditMode(string menuName)
    {
        if (!Application.isPlaying) return true;
        EditorUtility.DisplayDialog("Dừng Play Mode trước!",
            $"Menu '{menuName}' chỉ có thể chạy ở Edit Mode.\n\n" +
            "Nhấn ▪ STOP trong Unity rồi chạy lại.", "OK");
        return false;
    }

    // ─── Save scene helper (chỉ khi Edit Mode) ───────────────────────────────
    private static void SaveScene()
    {
        if (Application.isPlaying) return;
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    [MenuItem("EscapeTheLab/✨ Làm Nét Toàn Bộ Chữ UI (Sharp Text)")]
    public static void FixBlurryText()
    {
        int canvasCount = 0;
        int textCount = 0;

        var scalers = Object.FindObjectsByType<UnityEngine.UI.CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var scaler in scalers)
        {
            scaler.dynamicPixelsPerUnit = 3f;
            scaler.referencePixelsPerUnit = 100f;
            EditorUtility.SetDirty(scaler);
            canvasCount++;
        }

        var texts = Object.FindObjectsByType<UnityEngine.UI.Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            t.fontStyle = FontStyle.Bold;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;

            var outline = t.GetComponent<UnityEngine.UI.Outline>();
            if (outline == null) outline = t.gameObject.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            EditorUtility.SetDirty(t);
            textCount++;
        }

        SaveScene();

        EditorUtility.DisplayDialog("Làm Nét Chữ",
            $"Đã tối ưu làm nét thành công:\n• {canvasCount} CanvasScaler (Dynamic Pixels Per Unit = 3.0x)\n• {textCount} UI Text (Font Bold + Outline sắc nét)\n\nChữ trong game giờ đã cực kỳ rõ và nét căng!", "OK");
    }

    [MenuItem("EscapeTheLab/🎥 Switch to FIRST PERSON Camera")]
    public static void SwitchToFirstPersonCamera(bool showDialog = true)
    {
        if (!EnsureEditMode("Switch to FIRST PERSON Camera")) return;
        // 1. Tim Player
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Player trong scene!\nChạy Setup Phase 1 trước.", "OK");
            return;
        }

        // 2. Tim/Tao EyePoint tren Player
        Transform eyePoint = player.transform.Find("EyePoint");
        if (eyePoint == null)
        {
            GameObject ep = new GameObject("EyePoint");
            ep.transform.SetParent(player.transform, false);
            ep.transform.localPosition = new Vector3(0f, 1.6f, 0.12f); // Mat ngang tam nhin
            ep.transform.localRotation = Quaternion.identity;
            eyePoint = ep.transform;
            Debug.Log("[Setup] Created EyePoint on Player.");
        }
        else
        {
            eyePoint.localPosition = new Vector3(0f, 1.6f, 0.12f);
        }

        // 3. Xoa ThirdPersonCamera & CameraRig_PUBG cu neu co
        var oldTPC = Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (oldTPC != null)
        {
            GameObject oldRig = oldTPC.gameObject;
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.transform.parent == oldRig.transform)
                mainCam.transform.SetParent(null);
            DestroyImmediate(oldRig);
        }

        var oldPubg = Object.FindFirstObjectByType<PubgCamera>();
        if (oldPubg != null)
        {
            GameObject oldRig = oldPubg.gameObject;
            Camera mainCam = Camera.main;
            if (mainCam != null && mainCam.transform.parent == oldRig.transform)
                mainCam.transform.SetParent(null);
            DestroyImmediate(oldRig);
        }

        var oldRigGO = GameObject.Find("CameraRig_PUBG");
        if (oldRigGO != null) DestroyImmediate(oldRigGO);

        // 4. Gan Main Camera vao EyePoint
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            cam.gameObject.AddComponent<AudioListener>();
        }

        cam.transform.SetParent(eyePoint);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f; // Gan hon de tranh thay qua tay

        // 5. Xoa FirstPersonCamera cu neu co
        var oldFPC = cam.GetComponent<FirstPersonCamera>();
        if (oldFPC != null) DestroyImmediate(oldFPC);

        // 6. Them FirstPersonCamera vao EyePoint (hay Camera object)
        FirstPersonCamera fpc = cam.gameObject.AddComponent<FirstPersonCamera>();
        var soFPC = new SerializedObject(fpc);
        soFPC.FindProperty("playerBody").objectReferenceValue = player.transform;
        soFPC.ApplyModifiedProperties();

        // 7. Gan camera transform vao PlayerController
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            var soPC = new SerializedObject(pc);
            soPC.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
            soPC.ApplyModifiedProperties();
        }

        // 8. An Renderer nhan vat (tranh thay than minh khi FPS)
        Renderer[] renderers = player.GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
        {
            // Giu lai collider, tat render than chinh
            if (r.gameObject != player) continue; // chi tat Capsule chinh, giu GroundCheck
            r.enabled = false;
        }

        SaveScene();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("✅ First Person Camera!",
                "Camera đã gắn vào mắt nhân vật!\n\n" +
                "• EyePoint tạo tại đầu Player\n" +
                "• Camera gắn vào EyePoint\n" +
                "• FOV = 75° (phù hợp FPS)\n" +
                "• Thân player ẩn (không thấy mình)\n\n" +
                "Điều khiển:\n" +
                "• Mobile: kéo nửa PHẢI màn hình để nhìn\n" +
                "• PC/Editor: di chuột để nhìn (chuột bị khóa)\n" +
                "• Joystick trái: di chuyển", "OK");
        }
    }

    [MenuItem("EscapeTheLab/🎮 Switch to PUBG TPS Camera (Recommended)")]
    public static void SwitchToPubgCamera()
    {
        if (!EnsureEditMode("Switch to PUBG TPS Camera")) return;
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Player!\nChạy Setup Phase 1 trước.", "OK");
            return;
        }

        // --- Xoá camera mode cũ ---
        // Xoá FPS camera
        var oldFPS = Object.FindFirstObjectByType<FirstPersonCamera>();
        if (oldFPS != null)
        {
            // Lấy camera ra khỏi EyePoint trước
            Camera fpsCam = oldFPS.GetComponent<Camera>();
            if (fpsCam != null) fpsCam.transform.SetParent(null);
            DestroyImmediate(oldFPS);
        }

        // Xoá PubgCamera cũ nếu có
        var oldPubg = Object.FindFirstObjectByType<PubgCamera>();
        if (oldPubg != null) DestroyImmediate(oldPubg.gameObject);

        // Xoá ThirdPersonCamera cũ nếu có
        var oldTPC = Object.FindFirstObjectByType<ThirdPersonCamera>();
        if (oldTPC != null) DestroyImmediate(oldTPC.gameObject);

        // --- Tạo CameraRig mới ---
        GameObject rig = new GameObject("CameraRig_PUBG");
        rig.transform.position = Vector3.zero;
        PubgCamera pubgCam = rig.AddComponent<PubgCamera>();

        // --- Gắn Main Camera vào CameraRig ---
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            cam.gameObject.AddComponent<AudioListener>();
        }

        // Tách camera khỏi parent cũ (EyePoint nếu FPS)
        cam.transform.SetParent(rig.transform);
        cam.transform.localPosition = new Vector3(0, 0, -3.5f);
        cam.transform.localRotation = Quaternion.identity;
        cam.fieldOfView = 70f;
        cam.nearClipPlane = 0.1f;

        // --- Cấu hình PubgCamera ---
        var soPubg = new SerializedObject(pubgCam);
        soPubg.FindProperty("target").objectReferenceValue = player.transform;
        // Collision mask: Environment + Ground
        int envL   = LayerMask.NameToLayer("Environment");
        int groundL = LayerMask.NameToLayer("Ground");
        int mask = 0;
        if (envL   >= 0) mask |= 1 << envL;
        if (groundL >= 0) mask |= 1 << groundL;
        // Default mask nếu layer chưa tồn tại
        if (mask == 0) mask = ~(1 << 2); // everything except Ignore Raycast
        soPubg.FindProperty("collisionMask").intValue = mask;
        soPubg.ApplyModifiedProperties();

        // --- Gắn camera transform vào PlayerController ---
        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc != null)
        {
            var soPC = new SerializedObject(pc);
            soPC.FindProperty("cameraTransform").objectReferenceValue = rig.transform;
            soPC.ApplyModifiedProperties();
        }

        // --- Bật lại Renderer player (nếu bị ẩn từ FPS) ---
        foreach (var r in player.GetComponentsInChildren<Renderer>(true))
            r.enabled = true;

        SaveScene();

        EditorUtility.DisplayDialog("✅ PUBG TPS Camera!",
            "Camera PUBG-style đã được thiết lập!\n\n" +
            "Điều khiển:\n" +
            "• WASD / Joystick trái → Di chuyển\n" +
            "  (nhân vật xoay theo hướng đi)\n" +
            "• Di chuột / Kéo nửa PHẢI → Camera quay\n" +
            "  (nhân vật KHÔNG xoay theo camera)\n" +
            "• Scroll → Zoom in/out\n" +
            "• ESC → Mở chuột  |  Click → Khóa chuột\n" +
            "• Shift → Chạy  |  Space → Nhảy  |  E → Tương tác",
            "OK");
    }

    [MenuItem("EscapeTheLab/🎒 Thêm Túi Đồ (Inventory UI)")]
    public static void SetupInventoryUI()
    {
        if (!EnsureEditMode("Thêm Túi Đồ")) return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy Canvas. Vui lòng chạy Setup Phase 3-8 trước.", "OK");
            return;
        }

        Transform uiRoot = canvas.transform;

        // Xóa túi đồ cũ nếu có
        Transform oldInv = uiRoot.Find("InventoryPanel");
        if (oldInv != null) DestroyImmediate(oldInv.gameObject);
        Transform oldBtn = uiRoot.Find("ToggleInventoryButton");
        if (oldBtn != null) DestroyImmediate(oldBtn.gameObject);
        
        InventoryUI oldScript = canvas.GetComponent<InventoryUI>();
        if (oldScript != null) DestroyImmediate(oldScript);

        // Nút mở/đóng túi đồ (Góc trên bên phải)
        GameObject btnGO = new GameObject("ToggleInventoryButton");
        btnGO.transform.SetParent(uiRoot, false);
        UnityEngine.UI.Image btnImg = btnGO.AddComponent<UnityEngine.UI.Image>();
        btnImg.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
        UnityEngine.UI.Button toggleBtn = btnGO.AddComponent<UnityEngine.UI.Button>();
        RectTransform btnRect = btnGO.GetComponent<RectTransform>();
        btnRect.anchorMin = new Vector2(1, 1);
        btnRect.anchorMax = new Vector2(1, 1);
        btnRect.pivot = new Vector2(1, 1);
        btnRect.anchoredPosition = new Vector2(-15, -15);
        btnRect.sizeDelta = new Vector2(120, 50);

        GameObject btnTxt = MT(btnGO.transform, "Text", "Túi đồ (Tab)", 16, Vector2.zero);
        btnTxt.GetComponent<UnityEngine.UI.Text>().color = Color.white;
        btnTxt.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.MiddleCenter;

        // Panel túi đồ (Giữa màn hình)
        GameObject invPanel = MP(uiRoot, "InventoryPanel", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(400, 300));
        invPanel.GetComponent<UnityEngine.UI.Image>().color = new Color(0, 0, 0, 0.9f);

        GameObject titleTxt = MT(invPanel.transform, "Title", "TÚI ĐỒ", 24, new Vector2(0, 120));
        titleTxt.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.MiddleCenter;
        titleTxt.GetComponent<UnityEngine.UI.Text>().fontStyle = FontStyle.Bold;

        GameObject listTxt = MT(invPanel.transform, "ItemListText", "• Thẻ Bảo Mật\n• Cầu Chì", 18, new Vector2(0, -20));
        RectTransform listRect = listTxt.GetComponent<RectTransform>();
        listRect.sizeDelta = new Vector2(360, 200);
        UnityEngine.UI.Text textUI = listTxt.GetComponent<UnityEngine.UI.Text>();
        textUI.alignment = TextAnchor.UpperLeft;
        textUI.lineSpacing = 1.5f;

        // Gắn script vào Canvas
        InventoryUI invScript = canvas.gameObject.AddComponent<InventoryUI>();
        SerializedObject so = new SerializedObject(invScript);
        so.FindProperty("inventoryPanel").objectReferenceValue = invPanel;
        so.FindProperty("itemListText").objectReferenceValue = textUI;
        so.FindProperty("toggleButton").objectReferenceValue = toggleBtn;
        so.ApplyModifiedProperties();

        // Ẩn panel mặc định
        invPanel.SetActive(false);

        SaveScene();

        EditorUtility.DisplayDialog("Thành Công", "Đã thêm Túi Đồ (Inventory UI)!\n- Trên PC: Bấm Tab hoặc phím I để mở.\n- Trên Mobile: Bấm nút Túi Đồ ở góc trên bên phải.", "OK");
    }

    [MenuItem("EscapeTheLab/Bake NavMesh (Auto 1-Click)")]
    public static void AutoBakeNavMesh()
    {
        BakeNavMeshInternal();
        SaveScene();
        EditorUtility.DisplayDialog("Bake NavMesh", "Bake NavMesh thành công 100%! Bạn sẽ thấy vùng màu xanh trên mặt sàn.", "OK");
    }

    private static void BakeNavMeshInternal()
    {
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            GameObject navObj = GameObject.Find("NavMesh Surface");
            if (navObj == null) navObj = new GameObject("NavMesh Surface");
            surface = navObj.AddComponent<NavMeshSurface>();
        }
        surface.BuildNavMesh();
    }

    [MenuItem("EscapeTheLab/Setup Phase 3-8 (Lab Scene)")]
    public static void RunPhase2Setup()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Lab"))
        {
            bool open = EditorUtility.DisplayDialog("Open Lab Scene?",
                "Mo Lab scene truoc, hoac de script mo.", "Open Lab Scene", "Cancel");
            if (!open) return;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity");
        }

        bool confirm = EditorUtility.DisplayDialog("Phase 3-8 Setup",
            "Script se them vao Lab scene:\n- Items (SecurityCard, Fuse, Battery)\n" +
            "- Terminal, Generator, ExitDoor, AccessCodeTerminal\n" +
            "- Robot voi NavMesh + AI + Detection\n" +
            "- Gameplay UI (HP, Objective, Notification, Win, GameOver)\n" +
            "- ObjectiveManager, AudioManager\n\nTiep tuc?",
            "Yes, Setup Now", "Cancel");
        if (!confirm) return;

        try
        {
            EditorUtility.DisplayProgressBar("Phase 3-8", "Interactables...", 0.1f);
            SetupInteractables();
            EditorUtility.DisplayProgressBar("Phase 3-8", "Robot...", 0.35f);
            SetupRobot();
            EditorUtility.DisplayProgressBar("Phase 3-8", "UI...", 0.6f);
            SetupGameplayUI();
            EditorUtility.DisplayProgressBar("Phase 3-8", "Managers...", 0.85f);
            SetupManagers();
            EditorUtility.DisplayProgressBar("Phase 3-8", "Baking NavMesh...", 0.92f);
            BakeNavMeshInternal();
            EditorUtility.DisplayProgressBar("Phase 3-8", "Saving...", 0.98f);
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Done!",
                "Phase 3-8 hoàn thành & NavMesh đã được Bake tự động!\n\n" +
                "1. Nhấn Play (▶) để test ngay\n" +
                "2. Đến nhặt Fuse -> ra Generator -> Nhấn E để Bật Điện -> Robot thức tỉnh\n" +
                "3. Giải Access Code Terminal: BLUE > RED > GREEN -> Ra cửa Exit!", "OK");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[Phase2Setup] " + ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    // ─── INTERACTABLES ───────────────────────────────
    private static void SetupInteractables()
    {
        GameObject root = new GameObject("Interactables");
        CreateItem(root.transform, "SecurityCard", PickupItem.ItemType.SecurityCard,
            "Thẻ Bảo Mật", new Vector3(8, 0.8f, 8), Color.cyan);
        CreateItem(root.transform, "Fuse", PickupItem.ItemType.Fuse,
            "Cầu Chì", new Vector3(-8, 0.8f, 6), new Color(1f, 0.7f, 0f));
        CreateItem(root.transform, "Battery", PickupItem.ItemType.Battery,
            "Pin Dự Phòng", new Vector3(5, 0.8f, -8), Color.green);
        CreateItem(root.transform, "AccessCodeNote", PickupItem.ItemType.AccessCode,
            "Ghi Chú Mã Truy Cập", new Vector3(-6, 0.8f, -10), Color.white);
        CreateTerminalObj(root.transform, new Vector3(10, 0, 3));
        CreateGeneratorObj(root.transform, new Vector3(-10, 0, -5));
        CreateExitDoorObj(root.transform, new Vector3(0, 2, 24));
        CreateAccessTerminalObj(root.transform, new Vector3(-8, 0, -15));
    }

    private static void CreateItem(Transform p, string n, PickupItem.ItemType t,
        string iname, Vector3 pos, Color col)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = n; go.transform.SetParent(p); go.transform.position = pos;
        go.transform.localScale = Vector3.one * 0.4f;
        int lay = LayerMask.NameToLayer("Interactable");
        go.layer = lay >= 0 ? lay : 0;
        DestroyImmediate(go.GetComponent<SphereCollider>());
        SphereCollider c = go.AddComponent<SphereCollider>(); c.radius = 1.5f;
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")); mat.color = col;
        go.GetComponent<Renderer>().material = mat;
        PickupItem pi = go.AddComponent<PickupItem>();
        var so = new SerializedObject(pi);
        so.FindProperty("itemType").enumValueIndex = (int)t;
        so.FindProperty("itemName").stringValue = iname;
        so.ApplyModifiedProperties();
    }

    private static void CreateTerminalObj(Transform p, Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ClueTerminal"; go.transform.SetParent(p);
        go.transform.position = pos; go.transform.localScale = new Vector3(0.6f, 1.2f, 0.2f);
        int lay = LayerMask.NameToLayer("Interactable"); go.layer = lay >= 0 ? lay : 0;
        go.GetComponent<Renderer>().material.color = new Color(0.1f, 0.1f, 0.3f);
        GameObject lg = new GameObject("ScreenLight"); lg.transform.SetParent(go.transform);
        lg.transform.localPosition = new Vector3(0, 0, -0.6f);
        Light l = lg.AddComponent<Light>(); l.type = LightType.Point;
        l.color = new Color(0, 0.8f, 1f); l.intensity = 1f; l.range = 3f;
        Terminal term = go.AddComponent<Terminal>();
        var so = new SerializedObject(term);
        so.FindProperty("terminalTitle").stringValue = "SECURITY TERMINAL";
        so.FindProperty("clueText").stringValue =
            "SECURITY LOG:\n" +
            "Security Card location: East cabinet (8,0,8)\n" +
            "Generator Fuse: Storage room (-8,0,6)\n" +
            "Access Code sequence: BLUE - RED - GREEN\n" +
            "WARNING: Generator offline. Power needed.";
        so.FindProperty("requiresPower").boolValue = false;
        so.FindProperty("screenLight").objectReferenceValue = l;
        so.ApplyModifiedProperties();
    }

    private static void CreateGeneratorObj(Transform p, Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = "Generator"; go.transform.SetParent(p);
        go.transform.position = pos; go.transform.localScale = new Vector3(1.2f, 1f, 1.2f);
        int lay = LayerMask.NameToLayer("Interactable"); go.layer = lay >= 0 ? lay : 0;
        go.GetComponent<Renderer>().material.color = new Color(0.3f, 0.3f, 0.35f);
        GameObject glowGO = new GameObject("GenGlow"); glowGO.transform.SetParent(go.transform);
        glowGO.transform.localPosition = Vector3.up;
        Light glow = glowGO.AddComponent<Light>(); glow.type = LightType.Point;
        glow.color = Color.green; glow.intensity = 2f; glow.range = 5f; glow.enabled = false;
        GameObject sGO = new GameObject("Sparks"); sGO.transform.SetParent(go.transform);
        sGO.transform.localPosition = Vector3.up;
        ParticleSystem ps = sGO.AddComponent<ParticleSystem>();
        var main = ps.main; main.startColor = new Color(0.4f, 0.8f, 1f);
        main.startSize = 0.05f; main.startLifetime = 0.3f; main.startSpeed = 2f; main.maxParticles = 30;
        ps.Stop();
        Generator gen = go.AddComponent<Generator>();
        var so = new SerializedObject(gen);
        so.FindProperty("generatorGlow").objectReferenceValue = glow;
        so.FindProperty("electricSparks").objectReferenceValue = ps;
        so.ApplyModifiedProperties();
    }

    private static void CreateExitDoorObj(Transform p, Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "ExitDoor"; go.transform.SetParent(p);
        go.transform.position = pos; go.transform.localScale = new Vector3(3f, 4f, 0.3f);
        int lay = LayerMask.NameToLayer("Interactable"); go.layer = lay >= 0 ? lay : 0;
        go.GetComponent<Renderer>().material.color = new Color(0.8f, 0.1f, 0.1f);
        go.AddComponent<Animator>(); go.AddComponent<ExitDoor>();
    }

    private static void CreateAccessTerminalObj(Transform p, Vector3 pos)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "AccessCodeTerminal"; go.transform.SetParent(p);
        go.transform.position = pos; go.transform.localScale = new Vector3(0.8f, 1.5f, 0.2f);
        int lay = LayerMask.NameToLayer("Interactable"); go.layer = lay >= 0 ? lay : 0;
        go.GetComponent<Renderer>().material.color = new Color(0.05f, 0.2f, 0.4f);
        go.AddComponent<AccessCodePuzzle>();
    }

    // ─── ROBOT ───────────────────────────────────────
    private static void SetupRobot()
    {
        GameObject robot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        robot.name = "Robot"; robot.transform.position = new Vector3(0, 1, 12);
        robot.GetComponent<Renderer>().material.color = new Color(0.2f, 0.2f, 0.25f);
        DestroyImmediate(robot.GetComponent<CapsuleCollider>());
        CapsuleCollider col = robot.AddComponent<CapsuleCollider>();
        col.center = Vector3.zero; col.radius = 0.3f; col.height = 1.8f;

        NavMeshAgent agent = robot.AddComponent<NavMeshAgent>();
        agent.radius = 0.3f; agent.height = 1.8f; agent.speed = 2f;
        agent.angularSpeed = 120f; agent.acceleration = 8f; agent.stoppingDistance = 0.5f;
        robot.AddComponent<Animator>();

        // Sensor light
        GameObject sGO = new GameObject("SensorLight"); sGO.transform.SetParent(robot.transform);
        sGO.transform.localPosition = new Vector3(0, 0.7f, 0.35f);
        Light sensor = sGO.AddComponent<Light>(); sensor.type = LightType.Spot;
        sensor.color = Color.green; sensor.intensity = 3f; sensor.range = 15f; sensor.spotAngle = 45f;

        // Eye point
        GameObject eyeGO = new GameObject("EyePoint"); eyeGO.transform.SetParent(robot.transform);
        eyeGO.transform.localPosition = new Vector3(0, 0.7f, 0.3f);

        // Detection
        RobotDetection det = robot.AddComponent<RobotDetection>();
        var soD = new SerializedObject(det);
        soD.FindProperty("eyePoint").objectReferenceValue = eyeGO.transform;
        int pl = LayerMask.NameToLayer("Player");
        int el = LayerMask.NameToLayer("Environment");
        int gl = LayerMask.NameToLayer("Ground");
        if (pl >= 0) soD.FindProperty("playerMask").intValue = 1 << pl;
        int obs = 0;
        if (el >= 0) obs |= 1 << el;
        if (gl >= 0) obs |= 1 << gl;
        soD.FindProperty("obstacleMask").intValue = obs;
        soD.ApplyModifiedProperties();

        // Waypoints
        GameObject wpP = new GameObject("Waypoints");
        Vector3[] wpPos = { new Vector3(6,0,6), new Vector3(-6,0,6), new Vector3(-6,0,-6), new Vector3(6,0,-6) };
        Transform[] wps = new Transform[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject wp = new GameObject("WP_" + (i + 1));
            wp.transform.SetParent(wpP.transform);
            wp.transform.position = wpPos[i];
            wps[i] = wp.transform;
        }

        // AI
        RobotAI ai = robot.AddComponent<RobotAI>();
        var soA = new SerializedObject(ai);
        soA.FindProperty("sensorLight").objectReferenceValue = sensor;
        var wpProp = soA.FindProperty("waypoints"); wpProp.arraySize = 4;
        for (int i = 0; i < 4; i++) wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        soA.ApplyModifiedProperties();
        Debug.Log("[Phase2Setup] Robot created. BAKE NAVMESH required!");
    }

    // ─── GAMEPLAY UI ─────────────────────────────────
    private static void SetupGameplayUI()
    {
        // EventSystem
        if (FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("[Phase2Setup] Created missing EventSystem.");
        }

        Canvas c = FindFirstObjectByType<Canvas>();
        Transform cTr = c != null ? c.transform : CreateCanvas().transform;

        // HP
        GameObject hpPnl = MP(cTr, "HPPanel", new Vector2(0,1), new Vector2(0,1),
            new Vector2(15,-15), new Vector2(280,55));
        hpPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,0.6f);
        GameObject hpTxt = MT(hpPnl.transform, "HPText", "HP: 100/100", 16, new Vector2(0,12));
        GameObject slGO = new GameObject("HPSlider"); slGO.transform.SetParent(hpPnl.transform, false);
        UnityEngine.UI.Slider sl = slGO.AddComponent<UnityEngine.UI.Slider>(); sl.value = 1f;
        RectTransform slR = slGO.GetComponent<RectTransform>();
        slR.anchorMin = Vector2.zero; slR.anchorMax = Vector2.one;
        slR.offsetMin = Vector2.zero; slR.offsetMax = Vector2.zero;
        GameObject fa = new GameObject("FillArea"); fa.transform.SetParent(slGO.transform, false);
        RectTransform faR = fa.AddComponent<RectTransform>();
        faR.anchorMin = Vector2.zero; faR.anchorMax = Vector2.one;
        faR.offsetMin = Vector2.zero; faR.offsetMax = Vector2.zero;
        GameObject fi = new GameObject("Fill"); fi.transform.SetParent(fa.transform, false);
        UnityEngine.UI.Image fiI = fi.AddComponent<UnityEngine.UI.Image>();
        fiI.color = new Color(0.2f, 0.9f, 0.3f);
        RectTransform fiR = fi.GetComponent<RectTransform>();
        fiR.anchorMin = Vector2.zero; fiR.anchorMax = Vector2.one;
        fiR.offsetMin = Vector2.zero; fiR.offsetMax = Vector2.zero;
        sl.fillRect = fiR;

        // Objective
        GameObject objPnl = MP(cTr, "ObjectivePanel", new Vector2(1,1), new Vector2(1,1),
            new Vector2(-15,-15), new Vector2(300,80));
        objPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,0.6f);
        GameObject objTxt = MT(objPnl.transform, "ObjectiveText", "OBJECTIVE\nFind Security Card", 15, Vector2.zero);

        // Notification
        GameObject notifPnl = MP(cTr, "NotificationPanel", new Vector2(0.5f,0), new Vector2(0.5f,0),
            new Vector2(0,120), new Vector2(500,55));
        notifPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0.5f,1f,0.85f);
        GameObject notifTxt = MT(notifPnl.transform, "NotifText", "Notification", 20, Vector2.zero);
        notifPnl.AddComponent<CanvasGroup>(); notifPnl.SetActive(false);

        // Win
        GameObject winPnl = MP(cTr, "WinPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(600,380));
        winPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0.05f,0.1f,0.97f);
        MT(winPnl.transform, "WinTitle", "ESCAPE SUCCESSFUL", 32, new Vector2(0,120));
        MT(winPnl.transform, "WinSub", "You escaped the laboratory.", 20, new Vector2(0,60));
        GameObject paBtn = MB(winPnl.transform, "PlayAgainBtn", "[ PLAY AGAIN ]", new Vector2(0,-50));
        GameObject mmBtn = MB(winPnl.transform, "MainMenuBtn", "[ MAIN MENU ]", new Vector2(0,-120));
        winPnl.SetActive(false);

        // GameOver
        GameObject goPnl = MP(cTr, "GameOverPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(600,380));
        goPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f,0,0,0.97f);
        MT(goPnl.transform, "GOTitle", "CAPTURED", 36, new Vector2(0,120));
        MT(goPnl.transform, "GOSub", "The robot caught you.", 20, new Vector2(0,60));
        GameObject retBtn = MB(goPnl.transform, "RetryBtn", "[ TRY AGAIN ]", new Vector2(0,-50));
        GameObject goMBtn = MB(goPnl.transform, "GOMenuBtn", "[ MAIN MENU ]", new Vector2(0,-120));
        goPnl.SetActive(false);

        // Interaction Prompt - Dat o duoi thap va tat raycast de khong de len UI panel khac
        GameObject promptPnl = MP(cTr, "InteractionPrompt", new Vector2(0.5f,0), new Vector2(0.5f,0),
            new Vector2(0,75), new Vector2(360,45));
        var pImg = promptPnl.GetComponent<UnityEngine.UI.Image>();
        pImg.color = new Color(0,0,0,0.75f);
        pImg.raycastTarget = false;
        GameObject promptTxt = MT(promptPnl.transform, "PromptText", "Press E to Interact", 18, Vector2.zero);
        promptTxt.GetComponent<UnityEngine.UI.Text>().raycastTarget = false;
        promptPnl.SetActive(false);

        // Terminal UI
        GameObject termPnl = MP(cTr, "TerminalUIPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(700,500));
        termPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0.05f,0.15f,0.97f);
        GameObject termTitle = MT(termPnl.transform, "TermTitle", "TERMINAL", 26, new Vector2(0,200));
        termTitle.GetComponent<UnityEngine.UI.Text>().color = new Color(0,0.8f,1f);
        GameObject termContent = MT(termPnl.transform, "TermContent", "Content...", 15, new Vector2(0,10));
        termContent.GetComponent<UnityEngine.UI.Text>().alignment = TextAnchor.UpperLeft;
        termContent.GetComponent<RectTransform>().sizeDelta = new Vector2(600,300);
        GameObject closeTermBtn = MB(termPnl.transform, "CloseTermBtn", "[ CLOSE ]", new Vector2(0,-200));
        termPnl.SetActive(false);

        // AccessCode UI - Cac nut mau cach nhau ro rang, khong de len nhau
        GameObject acPnl = MP(cTr, "AccessCodeUIPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f),
            Vector2.zero, new Vector2(500,380));
        acPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0,0.05f,0.15f,0.97f);
        MT(acPnl.transform, "ACTitle", "ACCESS TERMINAL", 24, new Vector2(0,140));
        GameObject acStatus = MT(acPnl.transform, "ACStatus", "Enter sequence: BLUE \u2192 RED \u2192 GREEN", 15, new Vector2(0,80));
        GameObject blueBtn  = MB(acPnl.transform, "BlueBtn",  "BLUE",  new Vector2(-140,-10), new Vector2(125,55));
        GameObject redBtn   = MB(acPnl.transform, "RedBtn",   "RED",   new Vector2(0,-10),    new Vector2(125,55));
        GameObject greenBtn = MB(acPnl.transform, "GreenBtn", "GREEN", new Vector2(140,-10),  new Vector2(125,55));
        GameObject closeACBtn = MB(acPnl.transform, "CloseACBtn", "[ CLOSE ]", new Vector2(0,-120), new Vector2(180,48));
        blueBtn.GetComponent<UnityEngine.UI.Image>().color  = new Color(0.1f,0.3f,0.9f);
        redBtn.GetComponent<UnityEngine.UI.Image>().color   = new Color(0.9f,0.1f,0.1f);
        greenBtn.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f,0.8f,0.2f);
        acPnl.SetActive(false);

        // Attach scripts
        GameObject guiGO = new GameObject("GameplayUI"); guiGO.transform.SetParent(cTr, false);
        GameplayUI gui = guiGO.AddComponent<GameplayUI>();
        var soGUI = new SerializedObject(gui);
        soGUI.FindProperty("hpBar").objectReferenceValue = sl;
        soGUI.FindProperty("hpText").objectReferenceValue = hpTxt.GetComponent<UnityEngine.UI.Text>();
        soGUI.FindProperty("objectiveText").objectReferenceValue = objTxt.GetComponent<UnityEngine.UI.Text>();
        soGUI.FindProperty("objectivePanel").objectReferenceValue = objPnl;
        soGUI.FindProperty("winPanel").objectReferenceValue = winPnl;
        soGUI.FindProperty("gameOverPanel").objectReferenceValue = goPnl;
        soGUI.FindProperty("playAgainButton").objectReferenceValue = paBtn.GetComponent<UnityEngine.UI.Button>();
        soGUI.FindProperty("mainMenuButton").objectReferenceValue  = mmBtn.GetComponent<UnityEngine.UI.Button>();
        soGUI.FindProperty("retryButton").objectReferenceValue     = retBtn.GetComponent<UnityEngine.UI.Button>();
        soGUI.FindProperty("goMenuButton").objectReferenceValue    = goMBtn.GetComponent<UnityEngine.UI.Button>();
        soGUI.FindProperty("interactionPromptPanel").objectReferenceValue = promptPnl;
        soGUI.FindProperty("interactionPromptText").objectReferenceValue  = promptTxt.GetComponent<UnityEngine.UI.Text>();
        soGUI.ApplyModifiedProperties();

        GameObject nuiGO = new GameObject("NotificationUI"); nuiGO.transform.SetParent(cTr, false);
        NotificationUI nui = nuiGO.AddComponent<NotificationUI>();
        var soNUI = new SerializedObject(nui);
        soNUI.FindProperty("notificationPanel").objectReferenceValue = notifPnl;
        soNUI.FindProperty("messageText").objectReferenceValue = notifTxt.GetComponent<UnityEngine.UI.Text>();
        soNUI.ApplyModifiedProperties();

        GameObject tuiGO = new GameObject("TerminalUI"); tuiGO.transform.SetParent(cTr, false);
        TerminalUI tui = tuiGO.AddComponent<TerminalUI>();
        var soTUI = new SerializedObject(tui);
        soTUI.FindProperty("terminalPanel").objectReferenceValue       = termPnl;
        soTUI.FindProperty("terminalTitleText").objectReferenceValue   = termTitle.GetComponent<UnityEngine.UI.Text>();
        soTUI.FindProperty("terminalContentText").objectReferenceValue = termContent.GetComponent<UnityEngine.UI.Text>();
        soTUI.FindProperty("closeTerminalButton").objectReferenceValue = closeTermBtn.GetComponent<UnityEngine.UI.Button>();
        soTUI.ApplyModifiedProperties();

        GameObject auiGO = new GameObject("AccessCodeUI"); auiGO.transform.SetParent(cTr, false);
        AccessCodeUI aui = auiGO.AddComponent<AccessCodeUI>();
        var soAUI = new SerializedObject(aui);
        soAUI.FindProperty("puzzlePanel").objectReferenceValue       = acPnl;
        soAUI.FindProperty("blueButton").objectReferenceValue        = blueBtn.GetComponent<UnityEngine.UI.Button>();
        soAUI.FindProperty("redButton").objectReferenceValue         = redBtn.GetComponent<UnityEngine.UI.Button>();
        soAUI.FindProperty("greenButton").objectReferenceValue       = greenBtn.GetComponent<UnityEngine.UI.Button>();
        soAUI.FindProperty("closePuzzleButton").objectReferenceValue = closeACBtn.GetComponent<UnityEngine.UI.Button>();
        soAUI.FindProperty("statusText").objectReferenceValue        = acStatus.GetComponent<UnityEngine.UI.Text>();
        soAUI.ApplyModifiedProperties();
        Debug.Log("[Phase2Setup] UI done.");
    }

    // ─── MANAGERS ────────────────────────────────────
    private static void SetupManagers()
    {
        if (FindFirstObjectByType<ObjectiveManager>() == null)
            new GameObject("ObjectiveManager").AddComponent<ObjectiveManager>();

        if (FindFirstObjectByType<AudioManager>() == null)
        {
            GameObject amGO = new GameObject("AudioManager");
            AudioSource bgm = amGO.AddComponent<AudioSource>(); bgm.playOnAwake = false; bgm.loop = true;
            AudioSource sfx = amGO.AddComponent<AudioSource>(); sfx.playOnAwake = false;
            AudioSource amb = amGO.AddComponent<AudioSource>(); amb.playOnAwake = false; amb.loop = true;
            AudioManager am = amGO.AddComponent<AudioManager>();
            var so = new SerializedObject(am);
            AudioSource[] srcs = amGO.GetComponents<AudioSource>();
            if (srcs.Length >= 3)
            {
                so.FindProperty("bgmSource").objectReferenceValue     = srcs[0];
                so.FindProperty("sfxSource").objectReferenceValue     = srcs[1];
                so.FindProperty("ambientSource").objectReferenceValue = srcs[2];
            }
            so.ApplyModifiedProperties();
        }
        Debug.Log("[Phase2Setup] Managers done.");
    }

    // ─── CANVAS HELPER ───────────────────────────────
    private static GameObject CreateCanvas()
    {
        GameObject go = new GameObject("GameplayCanvas");
        var cv = go.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay; cv.sortingOrder = 5;
        var cs = go.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920,1080); cs.matchWidthOrHeight = 0.5f;
        cs.dynamicPixelsPerUnit = 3f;
        cs.referencePixelsPerUnit = 100f;
        go.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        return go;
    }

    // ─── SHORTHAND UI HELPERS ────────────────────────
    // Panel
    private static GameObject MP(Transform p, string n, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sz)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(amin.x > 0.6f ? 1 : amin.x < 0.4f ? 0 : 0.5f,
                               amin.y > 0.6f ? 1 : amin.y < 0.4f ? 0 : 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        go.AddComponent<UnityEngine.UI.Image>(); return go;
    }
    // Text
    private static GameObject MT(Transform p, string n, string txt, int sz, Vector2 pos)
    {
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(320,50);
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = txt; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = sz; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.raycastTarget = false; // Khong chan tia raycast chuot
        t.horizontalOverflow = HorizontalWrapMode.Wrap; t.verticalOverflow = VerticalWrapMode.Overflow;
        var ol = go.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }
    // Button
    private static GameObject MB(Transform p, string n, string lbl, Vector2 pos, Vector2 sz = default)
    {
        if (sz == default) sz = new Vector2(200, 50);
        GameObject go = new GameObject(n); go.transform.SetParent(p, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        go.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f,0.4f,0.8f,0.9f);
        go.AddComponent<UnityEngine.UI.Button>();
        GameObject tgo = new GameObject("Text"); tgo.transform.SetParent(go.transform, false);
        RectTransform trt = tgo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<UnityEngine.UI.Text>();
        t.text = lbl; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 20; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.raycastTarget = false; // De click chuot luon truyen vao Image cua Button
        var ol = tgo.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }
}
#endif
