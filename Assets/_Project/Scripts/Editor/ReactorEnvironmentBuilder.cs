#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;
using System.IO;

/// <summary>
/// ReactorEnvironmentBuilder: Tu dong tao va thiet lap hoan chinh Man 2 (Level2_Reactor).
/// - Khong con bat ky khe ho tuong nao (100% gapless).
/// - Buong thang may mo san de buoc vao, ban phim Keypad 214 ben trong.
/// - 3 Van lam mat hoat dong day du (am thanh, hieu ung, xoay van, cap dien).
/// - 3 Manh moi so tren tuong (1, 4, 2 -> ma: 2 1 4).
/// - Cac vat pham day du: So tay ky su, Pin nang luong, Hop cuu thuong, Co-le.
/// - Nen mong an toan bao ve chong roi vao vung blank.
/// </summary>
public class ReactorEnvironmentBuilder : Editor
{
    private const string SCENE_PATH = "Assets/_Project/Scenes/Level2_Reactor.unity";

    [MenuItem("EscapeTheLab/\U0001f3d7\ufe0f 2) Build Level 2 (Reactor Core + Elevator Keypad)")]
    public static void BuildLevel2()
    {
        EnsureSceneExistsAndOpen();

        bool confirm = EditorUtility.DisplayDialog("Xây dựng Màn 2 (Level 2: Reactor Core)",
            "Script sẽ tự động tạo hoàn chỉnh Màn 2 kín 100%:\n\n" +
            "• Sảnh Lò Phản Ứng, Phòng Nồi Hơi, Phòng Máy Phát, Kho & Thang Máy Hàng Hóa\n" +
            "• Buồng Thang Máy Hàng Hóa mở sẵn, có Bảng Phím Số Keypad (Mã: 2 1 4)\n" +
            "• 3 Van xả khí làm mát (CoolingValve) hoạt động trơn tru\n" +
            "• 3 Số dạ quang neon trên tường (①: 2, ②: 1, ③: 4)\n" +
            "• 4 Vật phẩm (Item): Sổ tay kỹ sư, Pin năng lượng, Hộp cứu thương, Cờ-lê\n" +
            "• Nền móng chống rơi vào vùng void tuyệt đối\n" +
            "• Player, Camera PUBG, Robot AI tuần tra, HUD UI\n" +
            "• Tự động Bake NavMesh\n\nBạn có muốn xây dựng ngay bây giờ?",
            "Xây dựng ngay!", "Hủy");
        if (!confirm) return;

        BuildLevel2_Internal();
    }

    public static void BuildLevel2_Direct(bool showDialog = false)
    {
        EnsureSceneExistsAndOpen();
        BuildLevel2_Internal(showDialog);
    }

    private static void BuildLevel2_Internal(bool showDialog = true)
    {
        try
        {
            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Dọn dẹp môi trường cũ...", 0.05f);
            ClearExistingObjects();

            GameObject envRoot = new GameObject("=== LEVEL 2: REACTOR ENVIRONMENT ===");

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Tạo nền móng bảo vệ chống rơi...", 0.10f);
            BuildSafetyFoundation(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Xây dựng Sảnh Lò Phản Ứng...", 0.18f);
            BuildReactorHall(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Xây dựng Phòng Nồi Hơi (Boiler)...", 0.28f);
            BuildBoilerRoom(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Xây dựng Phòng Máy Phát (Generator)...", 0.38f);
            BuildGeneratorRoom(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Xây dựng Khu Kho & Thang Máy Hàng Hóa...", 0.48f);
            BuildElevatorArea(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Thiết lập hệ thống van làm mát...", 0.58f);
            SetupValves(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Thiết lập điểm manh mối số trên tường...", 0.66f);
            SetupWallClues(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Đặt các vật phẩm (Items) trong phòng...", 0.74f);
            SetupItems(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Thiết lập ánh sáng công nghiệp...", 0.80f);
            SetupLighting(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Thiết lập Người chơi & Robot AI...", 0.86f);
            SetupPlayerAndRobot(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Thiết lập UI Canvas & Bàn phím Thang máy...", 0.92f);
            SetupCanvasUI(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Trang hoàng nội thất Sci-Fi Props...", 0.94f);
            EscapeTheLab.EditorTools.AssetUpgradeTools.DecorateReactorWithSciFiPropsInternal(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Bake NavMesh cho AI...", 0.96f);
            BakeNavMesh(envRoot);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Cập nhật Build Settings...", 0.98f);
            UpdateBuildSettings();

            EditorUtility.DisplayProgressBar("Xây dựng Màn 2", "Nâng cấp texture tường P3D Outdoor Wall Tile...", 0.99f);
            EscapeTheLab.EditorTools.AssetUpgradeTools.ApplyP3DWallTexturesToScene(EditorSceneManager.GetActiveScene());

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorUtility.ClearProgressBar();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Thành công!",
                    "Màn 2 (Level 2: Reactor Core) đã được xây dựng hoàn tất kín 100%!\n\n" +
                    "• Khí độc đang xả: 3 van (-1 HP/s) -> 2 van (-1 HP/2s) -> 1 van (-1 HP/3s) -> 0 (An toàn).\n" +
                    "• Tránh chạm vào luồng hơi nước xả (bị trừ máu).\n" +
                    "• Tìm Cầu Chì (Phòng Nồi Hơi) và bật Cầu Dao Điện Tổng (Phòng Máy Phát).\n" +
                    "• Bấm nút mở cửa thang máy bên ngoài.\n" +
                    "• Vào thang máy, nhấn [E] vào Bảng điều khiển và bấm mã 214 để lên Màn 3!\n\n" +
                    "Nhấn nút ▶ PLAY để trải nghiệm ngay!", "Tuyệt vời!");
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[ReactorBuilder] " + ex);
            if (showDialog)
            {
                EditorUtility.DisplayDialog("Lỗi", ex.Message, "OK");
            }
        }
    }

    private static void EnsureSceneExistsAndOpen()
    {
        if (!File.Exists(SCENE_PATH))
        {
            var newScene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            EditorSceneManager.SaveScene(newScene, SCENE_PATH);
        }
        else if (EditorSceneManager.GetActiveScene().path != SCENE_PATH)
        {
            EditorSceneManager.OpenScene(SCENE_PATH);
        }
    }

    private static void ClearExistingObjects()
    {
        var oldEnv = GameObject.Find("=== LEVEL 2: REACTOR ENVIRONMENT ===");
        if (oldEnv != null) DestroyImmediate(oldEnv);

        var oldLab = GameObject.Find("=== LAB ENVIRONMENT ===");
        if (oldLab != null) DestroyImmediate(oldLab);

        foreach (var name in new[] { "Player", "Robot", "GameplayCanvas", "EventSystem", "Waypoints", "CameraRig_PUBG", "Main Camera", "NavMesh Surface" })
        {
            var go = GameObject.Find(name);
            if (go != null) DestroyImmediate(go);
        }
    }

    // ─── 0. NỀN MÓNG BẢO VỆ CHỐNG RƠI KHỎI BẢN ĐỒ (SAFETY FOUNDATION) ────
    private static void BuildSafetyFoundation(Transform parent)
    {
        var root = new GameObject("SafetyPerimeter");
        root.transform.SetParent(parent);

        // San mong bao ve ben duoi toan bo khu nha
        MakeCube(root.transform, "SafetyFloor", new Vector3(0, -0.6f, 12f), new Vector3(120f, 0.6f, 120f), new Color(0.08f, 0.08f, 0.09f), true);

        // Tuong ranh gioi xa (phong ho)
        MakeCube(root.transform, "SafetyWall_N", new Vector3(0, 10f, 65f), new Vector3(130f, 20f, 2f), Color.black, true);
        MakeCube(root.transform, "SafetyWall_S", new Vector3(0, 10f, -45f), new Vector3(130f, 20f, 2f), Color.black, true);
        MakeCube(root.transform, "SafetyWall_W", new Vector3(-55f, 10f, 12f), new Vector3(2f, 20f, 120f), Color.black, true);
        MakeCube(root.transform, "SafetyWall_E", new Vector3(55f, 10f, 12f), new Vector3(2f, 20f, 120f), Color.black, true);
    }

    // ─── 1. SẢNH LÒ PHẢN ỨNG (CENTRAL REACTOR HALL) ─────────────────────
    private static void BuildReactorHall(Transform parent)
    {
        var root = new GameObject("Room_ReactorHall");
        root.transform.SetParent(parent);

        Color floorCol = new Color(0.13f, 0.14f, 0.16f);
        Color wallCol  = new Color(0.25f, 0.23f, 0.22f);

        // San & Tran: X [-12, 12], Z [-12, 12]
        MakeCube(root.transform, "Floor", new Vector3(0, -0.1f, 0), new Vector3(24f, 0.2f, 24f), floorCol, true);
        MakeCube(root.transform, "Ceiling", new Vector3(0, 5.1f, 0), new Vector3(24f, 0.2f, 24f), wallCol * 0.6f, true);

        // Tuong Nam (Z = -12): Tuong dac kin
        MakeCube(root.transform, "Wall_South", new Vector3(0, 2.5f, -12f), new Vector3(24f, 5f, 0.4f), wallCol, true);

        // Tuong Tay (X = -12): Cua thong ra Hanh lang Tay tai Z = 0 (rong 4m: Z [-2, 2])
        MakeCube(root.transform, "Wall_West_South", new Vector3(-12f, 2.5f, -7f), new Vector3(0.4f, 5f, 10f), wallCol, true);
        MakeCube(root.transform, "Wall_West_North", new Vector3(-12f, 2.5f, 7f), new Vector3(0.4f, 5f, 10f), wallCol, true);
        MakeCube(root.transform, "Wall_West_Header", new Vector3(-12f, 4.5f, 0), new Vector3(0.4f, 1f, 4f), wallCol, true);

        // Tuong Dong (X = 12): Cua thong ra Hanh lang Dong tai Z = 0 (rong 4m: Z [-2, 2])
        MakeCube(root.transform, "Wall_East_South", new Vector3(12f, 2.5f, -7f), new Vector3(0.4f, 5f, 10f), wallCol, true);
        MakeCube(root.transform, "Wall_East_North", new Vector3(12f, 2.5f, 7f), new Vector3(0.4f, 5f, 10f), wallCol, true);
        MakeCube(root.transform, "Wall_East_Header", new Vector3(12f, 4.5f, 0), new Vector3(0.4f, 1f, 4f), wallCol, true);

        // Tuong Bac (Z = 12): Cua thong ra Hanh lang Bac tai X = 0 (rong 4m: X [-2, 2])
        MakeCube(root.transform, "Wall_North_West", new Vector3(-7f, 2.5f, 12f), new Vector3(10f, 5f, 0.4f), wallCol, true);
        MakeCube(root.transform, "Wall_North_East", new Vector3(7f, 2.5f, 12f), new Vector3(10f, 5f, 0.4f), wallCol, true);
        MakeCube(root.transform, "Wall_North_Header", new Vector3(0, 4.5f, 12f), new Vector3(4f, 1f, 0.4f), wallCol, true);

        // LÒ PHẢN ỨNG NĂNG LƯỢNG TRUNG TÂM
        var core = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        core.name = "ReactorCore";
        core.transform.SetParent(root.transform);
        core.transform.position = new Vector3(0, 2.5f, 0);
        core.transform.localScale = new Vector3(5.5f, 2.5f, 5.5f);
        core.isStatic = true;
        var coreMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        coreMat.color = new Color(0.12f, 0.12f, 0.15f);
        coreMat.EnableKeyword("_EMISSION");
        coreMat.SetColor("_EmissionColor", new Color(1f, 0.45f, 0.05f) * 2.8f);
        core.GetComponent<Renderer>().material = coreMat;

        // Vong bao ve quang lo
        for (int i = 0; i < 4; i++)
        {
            float angle = (i * 90f + 45f) * Mathf.Deg2Rad;
            Vector3 pPos = new Vector3(Mathf.Cos(angle) * 4.6f, 1.5f, Mathf.Sin(angle) * 4.6f);
            MakeCube(root.transform, $"SafetyPillar_{i}", pPos, new Vector3(0.5f, 3f, 0.5f), new Color(0.85f, 0.65f, 0.1f), true);
        }

        // Hanh lang Tay (West Corridor): Noi X [-12, -18], Z [-2, 2]
        BuildStraightCorridor(root.transform, "Corridor_West", new Vector3(-15f, 0, 0), new Vector3(6f, 5f, 4f), true);

        // Hanh lang Dong (East Corridor): Noi X [12, 18], Z [-2, 2]
        BuildStraightCorridor(root.transform, "Corridor_East", new Vector3(15f, 0, 0), new Vector3(6f, 5f, 4f), true);

        // Hanh lang Bac (North Corridor): Noi Z [12, 18], X [-2, 2]
        BuildStraightCorridor(root.transform, "Corridor_North", new Vector3(0, 0, 15f), new Vector3(4f, 5f, 6f), false);
    }

    // ─── 2. PHÒNG NỒI HƠI (BOILER ROOM - WEST) ────────────────────────
    private static void BuildBoilerRoom(Transform parent)
    {
        var root = new GameObject("Room_Boiler");
        root.transform.SetParent(parent);

        Color floorCol = new Color(0.14f, 0.13f, 0.12f);
        Color wallCol  = new Color(0.27f, 0.22f, 0.20f);

        // Tam phong: X = -26, Z = 0. Kich thuoc: 16m x 5m x 16m. X [-34, -18], Z [-8, 8]
        MakeCube(root.transform, "Floor", new Vector3(-26f, -0.1f, 0), new Vector3(16f, 0.2f, 16f), floorCol, true);
        MakeCube(root.transform, "Ceiling", new Vector3(-26f, 5.1f, 0), new Vector3(16f, 0.2f, 16f), wallCol * 0.6f, true);

        // Tuong Tay (X = -34): Dac
        MakeCube(root.transform, "Wall_West", new Vector3(-34f, 2.5f, 0), new Vector3(0.4f, 5f, 16f), wallCol, true);
        // Tuong Bac (Z = 8): Dac
        MakeCube(root.transform, "Wall_North", new Vector3(-26f, 2.5f, 8f), new Vector3(16f, 5f, 0.4f), wallCol, true);
        // Tuong Nam (Z = -8): Dac
        MakeCube(root.transform, "Wall_South", new Vector3(-26f, 2.5f, -8f), new Vector3(16f, 5f, 0.4f), wallCol, true);

        // Tuong Dong (X = -18): Cua thong ra Hanh lang Tay tai Z = 0 (rong 4m: Z [-2, 2])
        MakeCube(root.transform, "Wall_East_South", new Vector3(-18f, 2.5f, -5f), new Vector3(0.4f, 5f, 6f), wallCol, true);
        MakeCube(root.transform, "Wall_East_North", new Vector3(-18f, 2.5f, 5f), new Vector3(0.4f, 5f, 6f), wallCol, true);
        MakeCube(root.transform, "Wall_East_Header", new Vector3(-18f, 4.5f, 0), new Vector3(0.4f, 1f, 4f), wallCol, true);

        // Bon noi hoi cong nghiep
        var tank1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tank1.name = "BoilerTank_1"; tank1.transform.SetParent(root.transform);
        tank1.transform.position = new Vector3(-29f, 2.2f, 4f);
        tank1.transform.localScale = new Vector3(2.6f, 2.2f, 2.6f);
        SetColor(tank1, new Color(0.35f, 0.26f, 0.2f));

        var tank2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tank2.name = "BoilerTank_2"; tank2.transform.SetParent(root.transform);
        tank2.transform.position = new Vector3(-29f, 2.2f, -4f);
        tank2.transform.localScale = new Vector3(2.6f, 2.2f, 2.6f);
        SetColor(tank2, new Color(0.35f, 0.26f, 0.2f));
    }

    // ─── 3. PHÒNG MÁY PHÁT (GENERATOR ROOM - EAST) ────────────────────
    private static void BuildGeneratorRoom(Transform parent)
    {
        var root = new GameObject("Room_Generator");
        root.transform.SetParent(parent);

        Color floorCol = new Color(0.12f, 0.14f, 0.16f);
        Color wallCol  = new Color(0.20f, 0.24f, 0.27f);

        // Tam phong: X = 26, Z = 0. Kich thuoc: 16m x 5m x 16m. X [18, 34], Z [-8, 8]
        MakeCube(root.transform, "Floor", new Vector3(26f, -0.1f, 0), new Vector3(16f, 0.2f, 16f), floorCol, true);
        MakeCube(root.transform, "Ceiling", new Vector3(26f, 5.1f, 0), new Vector3(16f, 0.2f, 16f), wallCol * 0.6f, true);

        // Tuong Dong (X = 34): Dac
        MakeCube(root.transform, "Wall_East", new Vector3(34f, 2.5f, 0), new Vector3(0.4f, 5f, 16f), wallCol, true);
        // Tuong Bac (Z = 8): Dac
        MakeCube(root.transform, "Wall_North", new Vector3(26f, 2.5f, 8f), new Vector3(16f, 5f, 0.4f), wallCol, true);
        // Tuong Nam (Z = -8): Dac
        MakeCube(root.transform, "Wall_South", new Vector3(26f, 2.5f, -8f), new Vector3(16f, 5f, 0.4f), wallCol, true);

        // Tuong Tay (X = 18): Cua thong ra Hanh lang Dong tai Z = 0 (rong 4m: Z [-2, 2])
        MakeCube(root.transform, "Wall_West_South", new Vector3(18f, 2.5f, -5f), new Vector3(0.4f, 5f, 6f), wallCol, true);
        MakeCube(root.transform, "Wall_West_North", new Vector3(18f, 2.5f, 5f), new Vector3(0.4f, 5f, 6f), wallCol, true);
        MakeCube(root.transform, "Wall_West_Header", new Vector3(18f, 4.5f, 0), new Vector3(0.4f, 1f, 4f), wallCol, true);

        // To hop may phat dien
        MakeCube(root.transform, "MainTurbine", new Vector3(29f, 1.8f, 0), new Vector3(3.2f, 3.6f, 7f), new Color(0.2f, 0.36f, 0.48f), true);
        MakeCube(root.transform, "ControlRack", new Vector3(23f, 1.2f, 5.5f), new Vector3(2.5f, 2.4f, 0.8f), new Color(0.15f, 0.18f, 0.22f), true);

        // Tủ Cầu Dao Điện Tổng (Circuit Breaker) gắn trên Tường Đông
        SetupCircuitBreaker(root.transform, new Vector3(33.7f, 1.8f, 2.5f), Vector3.left);
    }

    // ─── 4. KHO VÀ BUỒNG THANG MÁY HÀNG HÓA (CARGO ELEVATOR AREA) ─────
    private static void BuildElevatorArea(Transform parent)
    {
        var root = new GameObject("Room_CargoElevator");
        root.transform.SetParent(parent);

        Color floorCol = new Color(0.13f, 0.14f, 0.16f);
        Color wallCol  = new Color(0.23f, 0.25f, 0.28f);

        // 1. Tien sanh kho hang: Tam (0, 0, 24). Kich thuoc: 20m x 5m x 12m. X [-10, 10], Z [18, 30]
        MakeCube(root.transform, "StagingFloor", new Vector3(0, -0.1f, 24f), new Vector3(20f, 0.2f, 12f), floorCol, true);
        MakeCube(root.transform, "StagingCeiling", new Vector3(0, 5.1f, 24f), new Vector3(20f, 0.2f, 12f), wallCol * 0.6f, true);

        // Tuong Tay (X = -10): Dac
        MakeCube(root.transform, "Wall_West", new Vector3(-10f, 2.5f, 24f), new Vector3(0.4f, 5f, 12f), wallCol, true);
        // Tuong Dong (X = 10): Dac
        MakeCube(root.transform, "Wall_East", new Vector3(10f, 2.5f, 24f), new Vector3(0.4f, 5f, 12f), wallCol, true);

        // Tuong Nam (Z = 18): Cua thong ra Hanh lang Bac tai X = 0 (rong 4m: X [-2, 2])
        MakeCube(root.transform, "Wall_South_West", new Vector3(-6f, 2.5f, 18f), new Vector3(8f, 5f, 0.4f), wallCol, true);
        MakeCube(root.transform, "Wall_South_East", new Vector3(6f, 2.5f, 18f), new Vector3(8f, 5f, 0.4f), wallCol, true);
        MakeCube(root.transform, "Wall_South_Header", new Vector3(0, 4.5f, 18f), new Vector3(4f, 1f, 0.4f), wallCol, true);

        // Tuong Bac (Z = 30): Co cua vom dan vao Buong Thang May (rong 6m: X [-3, 3])
        MakeCube(root.transform, "Wall_North_West", new Vector3(-6.5f, 2.5f, 30f), new Vector3(7f, 5f, 0.4f), wallCol, true);
        MakeCube(root.transform, "Wall_North_East", new Vector3(6.5f, 2.5f, 30f), new Vector3(7f, 5f, 0.4f), wallCol, true);
        MakeCube(root.transform, "Wall_North_Arch", new Vector3(0, 4.5f, 30f), new Vector3(6f, 1f, 0.4f), new Color(0.7f, 0.55f, 0.1f), true);

        // Bien hieu tren cong thang may
        var archSign = new GameObject("ElevatorArchSign");
        archSign.transform.SetParent(root.transform);
        archSign.transform.position = new Vector3(0, 4.4f, 29.75f);
        var tmSign = archSign.AddComponent<TextMesh>();
        tmSign.text = "▲ BUỒNG THANG MÁY HÀNG HÓA ▲\n[ TẦNG THƯỢNG - HELIPAD ]";
        tmSign.fontSize = 44;
        tmSign.characterSize = 1f;
        tmSign.alignment = TextAlignment.Center;
        tmSign.anchor = TextAnchor.MiddleCenter;
        tmSign.color = Color.yellow;
        tmSign.fontStyle = FontStyle.Bold;
        archSign.transform.localScale = Vector3.one * 0.035f;

        // Thung hang trang tri trong kho
        MakeCube(root.transform, "Crate_1", new Vector3(-6f, 1f, 22f), new Vector3(2f, 2f, 2f), new Color(0.48f, 0.35f, 0.2f), true);
        MakeCube(root.transform, "Crate_2", new Vector3(-6f, 1f, 25f), new Vector3(2f, 2f, 2f), new Color(0.42f, 0.3f, 0.18f), true);
        MakeCube(root.transform, "Crate_3", new Vector3(6f, 1.25f, 23f), new Vector3(2.5f, 2.5f, 2.5f), new Color(0.3f, 0.38f, 0.45f), true);

        // 2. ─── BUỒNG THANG MÁY HÀNG HÓA (ELEVATOR CABIN) ───
        // Nam o Z [30, 36], X [-3, 3]. Tam: (0, 0, 33)
        var elevRoot = new GameObject("ElevatorCabin");
        elevRoot.transform.SetParent(root.transform);
        elevRoot.transform.position = new Vector3(0, 0, 33f);

        // San kim loai thang may
        MakeCube(elevRoot.transform, "Elevator_Floor", new Vector3(0, -0.05f, 33f), new Vector3(6f, 0.3f, 6f), new Color(0.28f, 0.3f, 0.35f), false);
        // Tran thang may
        MakeCube(elevRoot.transform, "Elevator_Ceiling", new Vector3(0, 5.0f, 33f), new Vector3(6f, 0.3f, 6f), new Color(0.18f, 0.2f, 0.24f), false);
        // Tuong trai (X = -3)
        MakeCube(elevRoot.transform, "Elevator_WallL", new Vector3(-3f, 2.5f, 33f), new Vector3(0.4f, 5f, 6f), new Color(0.22f, 0.24f, 0.28f), false);
        // Tuong phai (X = 3)
        MakeCube(elevRoot.transform, "Elevator_WallR", new Vector3(3f, 2.5f, 33f), new Vector3(0.4f, 5f, 6f), new Color(0.22f, 0.24f, 0.28f), false);
        // Tuong sau (Z = 36)
        MakeCube(elevRoot.transform, "Elevator_WallBack", new Vector3(0, 2.5f, 36f), new Vector3(6f, 5f, 0.4f), new Color(0.22f, 0.24f, 0.28f), false);

        // Canh cua thang may (ElevatorDoor)
        // Ban dau DONG KIN: Vi tri dong: (0, 2.1f, 30f) | Vi tri mo: (0, 6.2f, 30f)
        Vector3 doorClosedPos = new Vector3(0, 2.1f, 30f);
        Vector3 doorOpenPos   = new Vector3(0, 6.2f, 30f);
        var door = MakeCube(elevRoot.transform, "ElevatorDoor", doorClosedPos, new Vector3(5.8f, 4.2f, 0.2f), new Color(0.85f, 0.65f, 0.1f), false);

        // Den chieu sang ben trong buong thang may
        var elLight = new GameObject("ElevatorInteriorLight");
        elLight.transform.SetParent(elevRoot.transform);
        elLight.transform.position = new Vector3(0, 4.4f, 33f);
        var lComp = elLight.AddComponent<Light>();
        lComp.type = LightType.Point;
        lComp.color = new Color(0.3f, 0.75f, 1f);
        lComp.intensity = 1.8f;
        lComp.range = 9f;

        // BẢNG ĐIỀU KHIỂN BÀN PHÍM (KEYPAD CONSOLE) GẮN TRÊN TƯỜNG BÊN TRONG THANG MÁY
        var keypadConsole = MakeCube(elevRoot.transform, "KeypadConsole", new Vector3(2.75f, 1.6f, 33f), new Vector3(0.3f, 0.8f, 0.6f), new Color(0.08f, 0.08f, 0.12f), false);
        int intLayer = LayerMask.NameToLayer("Interactable");
        keypadConsole.layer = intLayer >= 0 ? intLayer : 0;

        // Man hinh LED tren bang dieu khien
        var kpText = new GameObject("KeypadScreenText");
        kpText.transform.SetParent(keypadConsole.transform);
        kpText.transform.localPosition = new Vector3(-0.6f, 0.15f, 0);
        kpText.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        kpText.transform.localScale = Vector3.one * 0.022f;
        var tm = kpText.AddComponent<TextMesh>();
        tm.text = "[BẢNG ĐIỀU KHIỂN]\nNHẬP MÃ: _ _ _\n[Bấm E để nhập]";
        tm.fontSize = 36;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.cyan;

        // Gan ElevatorController
        var ctrl = keypadConsole.AddComponent<ElevatorController>();
        var soCtrl = new SerializedObject(ctrl);
        soCtrl.FindProperty("elevatorDoor").objectReferenceValue = door.transform;
        soCtrl.FindProperty("doorClosedOffset").vector3Value = doorClosedPos;
        soCtrl.FindProperty("doorOpenOffset").vector3Value = doorOpenPos;
        soCtrl.FindProperty("interiorLight").objectReferenceValue = lComp;
        soCtrl.FindProperty("targetCode").stringValue = "214";
        soCtrl.ApplyModifiedProperties();
        ctrl.SetupDoor(door.transform, doorClosedPos, doorOpenPos, lComp);

        // NÚT BẤM GỌI / MỞ CỬA THANG MÁY BÊN NGOÀI BUỒNG THANG
        SetupElevatorCallButton(root.transform, new Vector3(3.4f, 1.6f, 29.75f), Vector3.back);
    }

    // ─── 5. HỆ THỐNG 3 VAN LÀM MÁT (COOLING VALVES) ──────────────────
    private static void SetupValves(Transform parent)
    {
        var vRoot = new GameObject("CoolingValves");
        vRoot.transform.SetParent(parent);

        // Quản lý cơ chế ngạt khí độc toàn màn: 3 van (-1 HP/s), 2 van (-1 HP/2s), 1 van (-1 HP/3s), 0 van (An toàn)
        vRoot.AddComponent<ToxicGasManager>();

        CoolingValve.ResetValveProgress();

        // Van 1: Phong Noi Hoi (Boiler) - gan tren tuong Bac
        CreateValveObject(vRoot.transform, "Valve_Boiler", new Vector3(-26f, 1.8f, 7.6f), Vector3.back);

        // Van 2: Phong May Phat (Generator) - gan tren tuong Nam
        CreateValveObject(vRoot.transform, "Valve_Generator", new Vector3(26f, 1.8f, -7.6f), Vector3.forward);

        // Van 3: Sanh Lo Phan Ung (Reactor Hall) - gan tren tuong Nam
        CreateValveObject(vRoot.transform, "Valve_Reactor", new Vector3(0f, 1.8f, -11.6f), Vector3.forward);
    }

    private static void CreateValveObject(Transform parent, string name, Vector3 pos, Vector3 faceDir)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.forward = faceDir;

        int intLayer = LayerMask.NameToLayer("Interactable");
        go.layer = intLayer >= 0 ? intLayer : 0;

        // Gia do kim loai & Ong dan
        var pipe = MakeCube(go.transform, "PipeStand", Vector3.zero, new Vector3(0.4f, 1.6f, 0.4f), new Color(0.35f, 0.35f, 0.4f), false);
        var basePlate = MakeCube(go.transform, "BasePlate", new Vector3(0, 0, -0.15f), new Vector3(1.2f, 1.2f, 0.1f), new Color(0.15f, 0.15f, 0.18f), false);

        // Tay quay van (Wheel)
        var wheel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        wheel.name = "Wheel";
        wheel.transform.SetParent(go.transform, false);
        wheel.transform.localPosition = new Vector3(0, 0, 0.35f);
        wheel.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        wheel.transform.localScale = new Vector3(0.85f, 0.08f, 0.85f);
        wheel.layer = go.layer;
        SetColor(wheel, new Color(0.9f, 0.18f, 0.12f));

        // Den trang thai (Do -> Xanh khi xoay)
        var lightGO = new GameObject("StatusLight");
        lightGO.transform.SetParent(go.transform, false);
        lightGO.transform.localPosition = new Vector3(0, 0.75f, 0.25f);
        var l = lightGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.red;
        l.intensity = 1.5f;
        l.range = 3.5f;

        // HIỆU ỨNG LUỒNG HƠI NƯỚC / KHÍ ĐỘC PHÚT RA TỪ VAN (STEAM PARTICLE SYSTEM)
        var steamGO = new GameObject("SteamJet");
        steamGO.transform.SetParent(go.transform, false);
        steamGO.transform.localPosition = new Vector3(0, 0, 0.45f);
        steamGO.transform.localRotation = Quaternion.identity;

        var ps = steamGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.1f;
        main.startSpeed = 4.2f;
        main.startSize = 0.45f;
        main.startColor = new Color(0.85f, 0.95f, 1f, 0.55f);
        main.maxParticles = 60;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = true;

        var emission = ps.emission;
        emission.rateOverTime = 30f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 14f;
        shape.radius = 0.12f;

        // VÙNG SÁT THƯƠNG KHI CHẠM TRỰC TIẾP VÀO LUỒNG HƠI (GAS JET DAMAGE TRIGGER)
        var dmgZone = new GameObject("GasJetDamageZone");
        dmgZone.transform.SetParent(go.transform, false);
        dmgZone.transform.localPosition = new Vector3(0, 0, 1.3f);
        var scDmg = dmgZone.AddComponent<SphereCollider>();
        scDmg.radius = 1.25f;
        scDmg.isTrigger = true;
        dmgZone.AddComponent<GasJetDamageTrigger>();

        // Bien chi dan nho
        var lblGO = new GameObject("Label");
        lblGO.transform.SetParent(go.transform, false);
        lblGO.transform.localPosition = new Vector3(0, -0.6f, 0.22f);
        lblGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        lblGO.transform.localScale = Vector3.one * 0.02f;
        var tm = lblGO.AddComponent<TextMesh>();
        tm.text = "VAN XẢ ÁP SUẤT\n[ Bấm E ]";
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.white;

        // Collider tuong tac
        var col = go.AddComponent<SphereCollider>();
        col.radius = 1.8f;
        col.isTrigger = true;

        // Script CoolingValve
        var valve = go.AddComponent<CoolingValve>();
        valve.SetupReferences(wheel.transform, l, ps, dmgZone);

        var so = new SerializedObject(valve);
        so.FindProperty("wheelTransform").objectReferenceValue = wheel.transform;
        so.FindProperty("statusLight").objectReferenceValue = l;
        so.FindProperty("steamEffect").objectReferenceValue = ps;
        so.FindProperty("gasDamageTrigger").objectReferenceValue = dmgZone;
        so.ApplyModifiedProperties();
    }

    // ─── 6. 6 ĐIỂM NEO MANH MỐI SỐ TRÊN TƯỜNG (WALL CODE CLUES) ───────
    private static void SetupWallClues(Transform parent)
    {
        var spawnerGO = new GameObject("WallCodeSpawner");
        spawnerGO.transform.SetParent(parent);

        Transform[] anchors = new Transform[6];

        // 6 vi tri tren tuong cac phong (huong mat quay vao trong phong):
        // 1. Tuong Tay Phong Noi Hoi (X = -34)
        anchors[0] = CreateAnchor(spawnerGO.transform, "Anchor_Boiler_West", new Vector3(-33.7f, 2.2f, 2f), Vector3.right);
        // 2. Tuong Nam Phong Noi Hoi (Z = -8)
        anchors[1] = CreateAnchor(spawnerGO.transform, "Anchor_Boiler_South", new Vector3(-24f, 2.2f, -7.7f), Vector3.forward);
        // 3. Tuong Dong Phong May Phat (X = 34)
        anchors[2] = CreateAnchor(spawnerGO.transform, "Anchor_Gen_East", new Vector3(33.7f, 2.2f, 2f), Vector3.left);
        // 4. Tuong Bac Phong May Phat (Z = 8)
        anchors[3] = CreateAnchor(spawnerGO.transform, "Anchor_Gen_North", new Vector3(24f, 2.2f, 7.7f), Vector3.back);
        // 5. Tuong Nam Sanh Lo Phan Ung (Z = -12)
        anchors[4] = CreateAnchor(spawnerGO.transform, "Anchor_Hall_SouthWest", new Vector3(-8f, 2.2f, -11.7f), Vector3.forward);
        // 6. Tuong Tay Kho Hang (X = -10)
        anchors[5] = CreateAnchor(spawnerGO.transform, "Anchor_Storage_West", new Vector3(-9.7f, 2.2f, 24f), Vector3.right);

        var spawner = spawnerGO.AddComponent<WallCodeSpawner>();
        var so = new SerializedObject(spawner);
        var prop = so.FindProperty("wallPoints");
        prop.arraySize = 6;
        for (int i = 0; i < 6; i++)
            prop.GetArrayElementAtIndex(i).objectReferenceValue = anchors[i];
        so.ApplyModifiedProperties();
    }

    private static Transform CreateAnchor(Transform parent, string name, Vector3 pos, Vector3 forward)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.forward = forward;
        return go.transform;
    }

    // ─── 7. CÁC VẬT PHẨM (ITEMS) TRONG MÀN 2 ───────────────────────────
    private static void SetupItems(Transform parent)
    {
        var itemRoot = new GameObject("Items_Stage2");
        itemRoot.transform.SetParent(parent);

        // Item 1: Sổ tay ghi chép Kỹ sư (Clue Note) trong Sanh Lo Phan Ung
        CreateItemPickup(itemRoot.transform, "Item_EngineerLog",
            PickupItem.ItemType.EngineerNote,
            "Sổ Tay Kỹ Sư Trưởng",
            "NHẬT KÝ KỸ SƯ: 'Thang máy tự động ngắt điện khi áp suất cao! Phải xoay 3 van xả khí ở 3 phòng. Mật mã vận hành gồm 3 số ①, ②, ③ vẽ trên tường.'",
            new Vector3(-6f, 0.8f, -4f),
            new Color(0.95f, 0.85f, 0.2f),
            PrimitiveType.Cube,
            new Vector3(0.5f, 0.1f, 0.4f));

        // Item 2: Pin Nang Luong (+40 HP) trong Phong Noi Hoi
        CreateItemPickup(itemRoot.transform, "Item_Battery",
            PickupItem.ItemType.Battery,
            "Pin Năng Lượng (+40 HP)",
            "ĐÃ NHẶT: Pin Năng Lượng! Phục hồi 40 HP.",
            new Vector3(-28f, 0.8f, 4f),
            Color.green,
            PrimitiveType.Cylinder,
            new Vector3(0.4f, 0.4f, 0.4f));

        // Item 3: Hop Cuu Thuong (+50 HP) trong Phong May Phat
        CreateItemPickup(itemRoot.transform, "Item_Medkit",
            PickupItem.ItemType.Medkit,
            "Hộp Cứu Thương Khẩn Cấp (+50 HP)",
            "ĐÃ NHẶT: Hộp Cứu Thương! Phục hồi 50 HP.",
            new Vector3(28f, 0.8f, -4f),
            new Color(0.2f, 0.7f, 1f),
            PrimitiveType.Cube,
            new Vector3(0.5f, 0.35f, 0.5f));

        // Item 4: Co-le Ky Su trong Phong Kho Thang May
        CreateItemPickup(itemRoot.transform, "Item_Wrench",
            PickupItem.ItemType.Wrench,
            "Cờ-lê Kỹ Sư",
            "ĐÃ NHẶT: Cờ-lê Kỹ Sư! Trang bị hỗ trợ thao tác nhanh các van áp suất.",
            new Vector3(-6f, 1.8f, 22f),
            new Color(0.9f, 0.45f, 0.1f),
            PrimitiveType.Capsule,
            new Vector3(0.25f, 0.5f, 0.25f));

        // Item 5: Cầu Chì Cao Áp (Fuse) trong Phòng Nồi Hơi (Boiler Room)
        CreateItemPickup(itemRoot.transform, "Item_Fuse",
            PickupItem.ItemType.Fuse,
            "Cầu Chì Cao Áp (Power Fuse)",
            "ĐÃ NHẶT: Cầu Chì Cao Áp! Dùng để lắp vào Cầu Dao Điện Tổng (Generator Room) nhằm khôi phục điện thang máy.",
            new Vector3(-27f, 0.8f, -5f),
            new Color(1f, 0.85f, 0.15f),
            PrimitiveType.Cylinder,
            new Vector3(0.22f, 0.45f, 0.22f));
    }

    private static void CreateItemPickup(Transform parent, string name, PickupItem.ItemType type, string itemName, string msg, Vector3 pos, Color col, PrimitiveType primType, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(primType);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;

        int intLayer = LayerMask.NameToLayer("Interactable");
        go.layer = intLayer >= 0 ? intLayer : 0;

        DestroyImmediate(go.GetComponent<Collider>());
        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 1.6f;
        sc.isTrigger = true;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 1.5f);
        go.GetComponent<Renderer>().material = mat;

        // Den sang nhe
        var lGO = new GameObject("ItemGlow");
        lGO.transform.SetParent(go.transform, false);
        var l = lGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.range = 3f;
        l.intensity = 1.2f;

        var pi = go.AddComponent<PickupItem>();
        var so = new SerializedObject(pi);
        so.FindProperty("itemType").enumValueIndex = (int)type;
        so.FindProperty("itemName").stringValue = itemName;
        so.FindProperty("pickupMessage").stringValue = msg;
        so.ApplyModifiedProperties();
    }

    private static void SetupCircuitBreaker(Transform parent, Vector3 pos, Vector3 forward)
    {
        var cbGO = new GameObject("CircuitBreakerCabinet");
        cbGO.transform.SetParent(parent);
        cbGO.transform.position = pos;
        cbGO.transform.forward = forward;

        int intLayer = LayerMask.NameToLayer("Interactable");
        cbGO.layer = intLayer >= 0 ? intLayer : 0;

        // Thân tủ điện kim loại
        var cabinet = MakeCube(cbGO.transform, "CabinetBody", Vector3.zero, new Vector3(1.1f, 1.5f, 0.35f), new Color(0.18f, 0.2f, 0.22f), false);
        var innerPlate = MakeCube(cbGO.transform, "InnerPlate", new Vector3(0, 0, 0.12f), new Vector3(0.95f, 1.35f, 0.08f), new Color(0.1f, 0.12f, 0.14f), false);

        // Khớp xoay tay gạt cần cầu dao (Lever Hinge)
        var hingeGO = new GameObject("LeverHinge");
        hingeGO.transform.SetParent(cbGO.transform, false);
        hingeGO.transform.localPosition = new Vector3(0, 0.1f, 0.22f);

        // Cần gạt
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        handle.name = "SwitchLever";
        handle.transform.SetParent(hingeGO.transform, false);
        handle.transform.localPosition = new Vector3(0, 0.22f, 0);
        handle.transform.localScale = new Vector3(0.1f, 0.22f, 0.1f);
        handle.layer = cbGO.layer;
        SetColor(handle, new Color(1f, 0.45f, 0.05f));

        // Đèn báo điện (Đỏ = mất điện / Xanh = có điện)
        var lGO = new GameObject("StatusLight");
        lGO.transform.SetParent(cbGO.transform, false);
        lGO.transform.localPosition = new Vector3(0, 0.55f, 0.25f);
        var l = lGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.red;
        l.range = 3.5f;
        l.intensity = 1.5f;

        // Hiệu ứng tia lửa điện khi bật cầu dao
        var sparkGO = new GameObject("SparkVFX");
        sparkGO.transform.SetParent(cbGO.transform, false);
        sparkGO.transform.localPosition = new Vector3(0, 0.1f, 0.25f);
        var ps = sparkGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.4f, 0.85f, 1f);
        main.startSize = 0.08f;
        main.startLifetime = 0.35f;
        main.startSpeed = 2.5f;
        main.maxParticles = 35;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        ps.Stop();

        // Biển báo chỉ dẫn
        var signGO = new GameObject("BreakerSign");
        signGO.transform.SetParent(cbGO.transform, false);
        signGO.transform.localPosition = new Vector3(0, -0.55f, 0.22f);
        signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        signGO.transform.localScale = Vector3.one * 0.018f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "CẦU DAO ĐIỆN TỔNG\n[ CẦN CẦU CHÌ ]\n[ Bấm E ]";
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;

        // Collider tương tác
        var col = cbGO.AddComponent<BoxCollider>();
        col.size = new Vector3(1.6f, 1.8f, 1.4f);
        col.isTrigger = true;

        var cb = cbGO.AddComponent<CircuitBreaker>();
        cb.SetupReferences(hingeGO.transform, l, ps);

        var so = new SerializedObject(cb);
        so.FindProperty("switchLever").objectReferenceValue = hingeGO.transform;
        so.FindProperty("statusLight").objectReferenceValue = l;
        so.FindProperty("sparkVFX").objectReferenceValue = ps;
        so.ApplyModifiedProperties();
    }

    private static void SetupElevatorCallButton(Transform parent, Vector3 pos, Vector3 forward)
    {
        var panelGO = new GameObject("ElevatorCallPanel");
        panelGO.transform.SetParent(parent);
        panelGO.transform.position = pos;
        panelGO.transform.forward = forward;

        int intLayer = LayerMask.NameToLayer("Interactable");
        panelGO.layer = intLayer >= 0 ? intLayer : 0;

        // Đế kim loại gắn tường
        var frame = MakeCube(panelGO.transform, "PanelFrame", Vector3.zero, new Vector3(0.5f, 0.9f, 0.15f), new Color(0.15f, 0.16f, 0.18f), false);

        // Nút bấm tròn phát sáng
        var btnGO = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        btnGO.name = "CallButton";
        btnGO.transform.SetParent(panelGO.transform, false);
        btnGO.transform.localPosition = new Vector3(0, -0.05f, 0.1f);
        btnGO.transform.localRotation = Quaternion.Euler(90f, 0, 0);
        btnGO.transform.localScale = new Vector3(0.22f, 0.05f, 0.22f);
        btnGO.layer = panelGO.layer;

        var btnMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        btnMat.color = Color.red;
        btnMat.EnableKeyword("_EMISSION");
        btnMat.SetColor("_EmissionColor", Color.red * 1.5f);
        var btnRend = btnGO.GetComponent<Renderer>();
        btnRend.material = btnMat;

        // Đèn báo trạng thái
        var lGO = new GameObject("CallLight");
        lGO.transform.SetParent(panelGO.transform, false);
        lGO.transform.localPosition = new Vector3(0, 0.25f, 0.15f);
        var l = lGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = Color.red;
        l.range = 2.5f;
        l.intensity = 1.2f;

        // Bảng chữ chỉ dẫn
        var lblGO = new GameObject("CallSign");
        lblGO.transform.SetParent(panelGO.transform, false);
        lblGO.transform.localPosition = new Vector3(0, 0.6f, 0.1f);
        lblGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        lblGO.transform.localScale = Vector3.one * 0.018f;
        var tm = lblGO.AddComponent<TextMesh>();
        tm.text = "GỌI THANG MÁY\n[ Bấm E ]";
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;

        // Collider tương tác
        var col = panelGO.AddComponent<BoxCollider>();
        col.size = new Vector3(1.2f, 1.4f, 1.2f);
        col.isTrigger = true;

        var callBtn = panelGO.AddComponent<ElevatorCallButton>();
        callBtn.SetupReferences(l, btnRend);

        var so = new SerializedObject(callBtn);
        so.FindProperty("buttonLight").objectReferenceValue = l;
        so.FindProperty("buttonRenderer").objectReferenceValue = btnRend;
        so.ApplyModifiedProperties();
    }

    // ─── 8. ÁNH SÁNG CÔNG NGHIỆP (LIGHTING) ───────────────────────────
    private static void SetupLighting(Transform parent)
    {
        EscapeTheLab.EditorTools.AssetUpgradeTools.BrightenReactorLightingInternal(EditorSceneManager.GetActiveScene());
    }

    private static void MakeLight(Transform parent, Vector3 pos, Color col, float intensity, float range)
    {
        var go = new GameObject("Light_Industrial");
        go.transform.SetParent(parent);
        go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.Soft;
    }

    // ─── 9. PLAYER & ROBOT AI ─────────────────────────────────────────
    private static void SetupPlayerAndRobot(Transform parent)
    {
        // 1. Player
        GameObject player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
        }
        player.layer = LayerMask.NameToLayer("Default");
        player.transform.position = new Vector3(0, 1.2f, -8f);

        var capCol = player.GetComponent<CapsuleCollider>();
        if (capCol != null) DestroyImmediate(capCol);

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.skinWidth = 0.05f;
        cc.stepOffset = 0.3f;
        cc.minMoveDistance = 0f;

        if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();

        var pi = player.GetComponent<PlayerInteraction>();
        if (pi == null) pi = player.AddComponent<PlayerInteraction>();
        var soPI = new SerializedObject(pi);
        soPI.FindProperty("interactRange").floatValue = 3.2f;
        soPI.ApplyModifiedProperties();

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.AddComponent<PlayerController>();

        // 2. MobileInputController
        if (Object.FindFirstObjectByType<MobileInputController>() == null)
        {
            var micGO = new GameObject("MobileInputController");
            micGO.transform.SetParent(parent);
            micGO.AddComponent<MobileInputController>();
        }

        // 3. First Person Camera (Góc nhìn thứ nhất mặc định)
        var oldRig = GameObject.Find("CameraRig_PUBG");
        if (oldRig != null) DestroyImmediate(oldRig);

        Transform eyePoint = player.transform.Find("EyePoint");
        if (eyePoint == null)
        {
            var epGO = new GameObject("EyePoint");
            epGO.transform.SetParent(player.transform, false);
            epGO.transform.localPosition = new Vector3(0f, 1.6f, 0.12f);
            epGO.transform.localRotation = Quaternion.identity;
            eyePoint = epGO.transform;
        }

        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            cam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        cam.transform.SetParent(eyePoint, false);
        cam.transform.localPosition = Vector3.zero;
        cam.transform.localRotation = Quaternion.identity;
        cam.fieldOfView = 75f;
        cam.nearClipPlane = 0.05f;

        var fpc = cam.GetComponent<FirstPersonCamera>() ?? cam.gameObject.AddComponent<FirstPersonCamera>();
        fpc.enabled = true;
        fpc.SetPlayerBody(player.transform);

        // Ẩn mesh thân player để không cản trở tầm nhìn góc nhìn thứ nhất
        var rend = player.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        // Hỗ trợ đổi góc nhìn bằng phím V (First Person <-> PUBG Third Person)
        var cmm = player.GetComponent<CameraModeManager>() ?? player.AddComponent<CameraModeManager>();

        var soPC = new SerializedObject(pc);
        soPC.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        soPC.ApplyModifiedProperties();

        // 4. Robot AI
        GameObject robot = GameObject.Find("Robot");
        if (robot == null)
        {
            robot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            robot.name = "Robot";
            robot.tag = "Enemy";
            SetColor(robot, new Color(0.9f, 0.2f, 0.1f));
        }
        robot.transform.position = new Vector3(0, 1.1f, 5f);
        if (robot.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
        {
            var agent = robot.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
        }

        var sensor = new GameObject("SensorLight");
        sensor.transform.SetParent(robot.transform);
        sensor.transform.localPosition = new Vector3(0, 1.5f, 0.4f);
        sensor.transform.localRotation = Quaternion.Euler(20f, 0, 0);
        var sl = sensor.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = Color.green;
        sl.intensity = 4.5f;
        sl.range = 14f;
        sl.spotAngle = 70f;

        var coneGO = new GameObject("VisionCone_RadarSweep");
        coneGO.transform.SetParent(robot.transform, false);
        coneGO.transform.localPosition = Vector3.zero;
        coneGO.transform.localRotation = Quaternion.identity;
        var rvc = coneGO.AddComponent<RobotVisionCone>();

        // Waypoints tuan tra
        var wpRoot = new GameObject("Waypoints");
        wpRoot.transform.SetParent(parent);
        Vector3[] wpPositions = new[] {
            new Vector3(0, 0.1f, -5f),
            new Vector3(-20f, 0.1f, 0f),
            new Vector3(0, 0.1f, 5f),
            new Vector3(20f, 0.1f, 0f),
            new Vector3(0, 0.1f, 22f)
        };
        Transform[] wps = new Transform[wpPositions.Length];
        for (int i = 0; i < wpPositions.Length; i++)
        {
            var wp = new GameObject($"WP_{i + 1}");
            wp.transform.SetParent(wpRoot.transform);
            wp.transform.position = wpPositions[i];
            wps[i] = wp.transform;
        }

        var ai = robot.GetComponent<RobotAI>() ?? robot.AddComponent<RobotAI>();
        var soAI = new SerializedObject(ai);
        soAI.FindProperty("sensorLight").objectReferenceValue = sl;
        soAI.FindProperty("visionCone").objectReferenceValue = rvc;
        var wpProp = soAI.FindProperty("waypoints");
        wpProp.arraySize = wps.Length;
        for (int i = 0; i < wps.Length; i++)
            wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        soAI.ApplyModifiedProperties();
    }

    // ─── 10. CANVAS UI & KEYPAD PANEL ────────────────────────────────
    private static void SetupCanvasUI(Transform parent)
    {
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        GameObject cvGO = new GameObject("GameplayCanvas");
        cvGO.transform.SetParent(parent);
        var cv = cvGO.AddComponent<Canvas>();
        cv.renderMode = RenderMode.ScreenSpaceOverlay;
        cv.sortingOrder = 5;
        var cs = cvGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        cs.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cvGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // HP Panel
        var hpPnl = MakeUIPanel(cvGO.transform, "HPPanel", new Vector2(0,1), new Vector2(0,1), new Vector2(20,-20), new Vector2(290,62));
        hpPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0.06f, 0.08f, 0.12f, 0.92f);

        var hpTxt = MakeUIText(hpPnl.transform, "HPText", "HP: 100 / 100", 15, new Vector2(0, 12));

        var slGO = new GameObject("HPSlider");
        slGO.transform.SetParent(hpPnl.transform, false);
        var sl = slGO.AddComponent<UnityEngine.UI.Slider>();
        sl.interactable = false;
        sl.transition = UnityEngine.UI.Selectable.Transition.None;
        var slR = slGO.GetComponent<RectTransform>();
        slR.anchorMin = new Vector2(0f, 0f);
        slR.anchorMax = new Vector2(1f, 0f);
        slR.pivot = new Vector2(0.5f, 0f);
        slR.anchoredPosition = new Vector2(0, 10);
        slR.sizeDelta = new Vector2(-24, 20);

        var bgImg = slGO.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);

        var faGO = new GameObject("FillArea");
        faGO.transform.SetParent(slGO.transform, false);
        var faR = faGO.AddComponent<RectTransform>();
        faR.anchorMin = Vector2.zero;
        faR.anchorMax = Vector2.one;
        faR.offsetMin = new Vector2(2, 2);
        faR.offsetMax = new Vector2(-2, -2);

        var fiGO = new GameObject("Fill");
        fiGO.transform.SetParent(faGO.transform, false);
        var fiR = fiGO.AddComponent<RectTransform>();
        fiR.anchorMin = Vector2.zero;
        fiR.anchorMax = Vector2.one;
        fiR.offsetMin = Vector2.zero;
        fiR.offsetMax = Vector2.zero;
        var fiImg = fiGO.AddComponent<UnityEngine.UI.Image>();
        fiImg.color = new Color(0.2f, 0.92f, 0.38f);
        sl.fillRect = fiR;
        sl.value = 1f;

        // Objective Panel
        var objPnl = MakeUIPanel(cvGO.transform, "ObjectivePanel", new Vector2(1,1), new Vector2(1,1), new Vector2(-15,-15), new Vector2(340,90));
        var objTxt = MakeUIText(objPnl.transform, "ObjectiveText", "OBJECTIVE\nLevel 2", 15, Vector2.zero);

        // Notification Panel
        var notifPnl = MakeUIPanel(cvGO.transform, "NotificationPanel", new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0,120), new Vector2(560,55));
        var notifTxt = MakeUIText(notifPnl.transform, "NotifText", "Notification", 18, Vector2.zero);
        notifPnl.SetActive(false);

        // Interaction Prompt
        var promptPnl = MakeUIPanel(cvGO.transform, "InteractionPrompt", new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0,75), new Vector2(400,45));
        promptPnl.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var promptTxt = MakeUIText(promptPnl.transform, "PromptText", "Press E to Interact", 18, Vector2.zero);
        promptTxt.GetComponent<UnityEngine.UI.Text>().raycastTarget = false;
        promptPnl.SetActive(false);

        // Win Panel & Game Over Panel
        var winPnl = MakeUIPanel(cvGO.transform, "WinPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(600,380));
        MakeUIText(winPnl.transform, "WinTitle", "MÀN 2 HOÀN THÀNH", 32, new Vector2(0,120));
        var paBtn = MakeUIButton(winPnl.transform, "NextBtn", "[ TIẾP TỤC ]", new Vector2(0,-60));
        winPnl.SetActive(false);

        var goPnl = MakeUIPanel(cvGO.transform, "GameOverPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(600,380));
        MakeUIText(goPnl.transform, "GOTitle", "BỊ BẮT!", 36, new Vector2(0,120));
        var retBtn = MakeUIButton(goPnl.transform, "RetryBtn", "[ THỬ LẠI ]", new Vector2(0,-60));
        goPnl.SetActive(false);

        // ─── ELEVATOR KEYPAD UI PANEL ───
        var kpPnl = MakeUIPanel(cvGO.transform, "ElevatorKeypadPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(460,540));
        kpPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0.05f, 0.08f, 0.12f, 0.98f);

        var kpTitle = MakeUIText(kpPnl.transform, "KPTitle", "THANG MÁY - TẦNG THƯỢNG", 22, new Vector2(0, 230));
        kpTitle.GetComponent<UnityEngine.UI.Text>().color = new Color(0.2f, 0.8f, 1f);

        var kpStatus = MakeUIText(kpPnl.transform, "KPStatus", "Gợi ý: Tìm 3 chữ số ① ➔ ② ➔ ③ trên tường", 14, new Vector2(0, 185));

        var displayBox = MakeUIPanel(kpPnl.transform, "DisplayBox", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), new Vector2(0, 130), new Vector2(280, 50));
        displayBox.GetComponent<UnityEngine.UI.Image>().color = new Color(0f, 0.2f, 0.3f, 0.85f);
        var dispTxt = MakeUIText(displayBox.transform, "CodeDisplay", "[ _ _ _ ]", 26, Vector2.zero);
        dispTxt.GetComponent<UnityEngine.UI.Text>().color = Color.green;

        UnityEngine.UI.Button[] numBtns = new UnityEngine.UI.Button[10];

        // Hang 1: 1, 2, 3
        numBtns[1] = MakeUIButton(kpPnl.transform, "Btn_1", "1", new Vector2(-90, 60), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        numBtns[2] = MakeUIButton(kpPnl.transform, "Btn_2", "2", new Vector2(  0, 60), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        numBtns[3] = MakeUIButton(kpPnl.transform, "Btn_3", "3", new Vector2( 90, 60), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();

        // Hang 2: 4, 5, 6
        numBtns[4] = MakeUIButton(kpPnl.transform, "Btn_4", "4", new Vector2(-90, -5), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        numBtns[5] = MakeUIButton(kpPnl.transform, "Btn_5", "5", new Vector2(  0, -5), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        numBtns[6] = MakeUIButton(kpPnl.transform, "Btn_6", "6", new Vector2( 90, -5), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();

        // Hang 3: 7, 8, 9
        numBtns[7] = MakeUIButton(kpPnl.transform, "Btn_7", "7", new Vector2(-90, -70), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        numBtns[8] = MakeUIButton(kpPnl.transform, "Btn_8", "8", new Vector2(  0, -70), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        numBtns[9] = MakeUIButton(kpPnl.transform, "Btn_9", "9", new Vector2( 90, -70), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();

        // Hang 4: Clear, 0, Enter
        var clrBtn = MakeUIButton(kpPnl.transform, "Btn_Clear", "C", new Vector2(-90, -135), new Vector2(75, 55));
        clrBtn.GetComponent<UnityEngine.UI.Image>().color = new Color(0.7f, 0.2f, 0.2f);
        numBtns[0] = MakeUIButton(kpPnl.transform, "Btn_0", "0", new Vector2(0, -135), new Vector2(75, 55)).GetComponent<UnityEngine.UI.Button>();
        var entBtn = MakeUIButton(kpPnl.transform, "Btn_Enter", "OK", new Vector2(90, -135), new Vector2(75, 55));
        entBtn.GetComponent<UnityEngine.UI.Image>().color = new Color(0.1f, 0.7f, 0.3f);

        var clsBtn = MakeUIButton(kpPnl.transform, "Btn_Close", "[ ĐÓNG (ESC) ]", new Vector2(0, -210), new Vector2(255, 45));

        kpPnl.SetActive(false);

        var kpUI = cvGO.AddComponent<ElevatorKeypadUI>();
        var soKP = new SerializedObject(kpUI);
        soKP.FindProperty("keypadPanel").objectReferenceValue = kpPnl;
        soKP.FindProperty("titleText").objectReferenceValue = kpTitle.GetComponent<UnityEngine.UI.Text>();
        soKP.FindProperty("statusText").objectReferenceValue = kpStatus.GetComponent<UnityEngine.UI.Text>();
        soKP.FindProperty("codeDisplayText").objectReferenceValue = dispTxt.GetComponent<UnityEngine.UI.Text>();
        soKP.FindProperty("clearButton").objectReferenceValue = clrBtn.GetComponent<UnityEngine.UI.Button>();
        soKP.FindProperty("enterButton").objectReferenceValue = entBtn.GetComponent<UnityEngine.UI.Button>();
        soKP.FindProperty("closeButton").objectReferenceValue = clsBtn.GetComponent<UnityEngine.UI.Button>();

        var numProp = soKP.FindProperty("numberButtons");
        numProp.arraySize = 10;
        for (int i = 0; i < 10; i++)
            numProp.GetArrayElementAtIndex(i).objectReferenceValue = numBtns[i];
        soKP.ApplyModifiedProperties();

        var gui = cvGO.AddComponent<GameplayUI>();
        var soGUI = new SerializedObject(gui);
        soGUI.FindProperty("hpBar").objectReferenceValue = sl;
        soGUI.FindProperty("hpText").objectReferenceValue = hpTxt.GetComponent<UnityEngine.UI.Text>();
        soGUI.FindProperty("objectiveText").objectReferenceValue = objTxt.GetComponent<UnityEngine.UI.Text>();
        soGUI.FindProperty("objectivePanel").objectReferenceValue = objPnl;
        soGUI.FindProperty("winPanel").objectReferenceValue = winPnl;
        soGUI.FindProperty("gameOverPanel").objectReferenceValue = goPnl;
        soGUI.FindProperty("playAgainButton").objectReferenceValue = paBtn.GetComponent<UnityEngine.UI.Button>();
        soGUI.FindProperty("retryButton").objectReferenceValue = retBtn.GetComponent<UnityEngine.UI.Button>();
        soGUI.FindProperty("interactionPromptPanel").objectReferenceValue = promptPnl;
        soGUI.FindProperty("interactionPromptText").objectReferenceValue = promptTxt.GetComponent<UnityEngine.UI.Text>();
        soGUI.ApplyModifiedProperties();

        var nui = cvGO.AddComponent<NotificationUI>();
        var soNUI = new SerializedObject(nui);
        soNUI.FindProperty("notificationPanel").objectReferenceValue = notifPnl;
        soNUI.FindProperty("messageText").objectReferenceValue = notifTxt.GetComponent<UnityEngine.UI.Text>();
        soNUI.ApplyModifiedProperties();

        if (Object.FindFirstObjectByType<ObjectiveManager>() == null)
        {
            var om = new GameObject("ObjectiveManager");
            om.transform.SetParent(parent);
            om.AddComponent<ObjectiveManager>();
        }
    }

    // ─── 11. NAVMESH ─────────────────────────────────────────────────
    private static void BakeNavMesh(GameObject root)
    {
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            var nmGO = new GameObject("NavMesh Surface");
            nmGO.transform.SetParent(root.transform);
            surface = nmGO.AddComponent<NavMeshSurface>();
        }
        surface.BuildNavMesh();
    }

    // ─── 12. BUILD SETTINGS ──────────────────────────────────────────
    private static void UpdateBuildSettings()
    {
        var currentScenes = EditorBuildSettings.scenes;
        bool hasMainMenu = false;
        bool hasLab = false;
        bool hasLevel2 = false;

        foreach (var s in currentScenes)
        {
            if (s.path.Contains("MainMenu")) hasMainMenu = true;
            if (s.path.Contains("Lab.unity")) hasLab = true;
            if (s.path.Contains("Level2_Reactor")) hasLevel2 = true;
        }

        var sceneList = new System.Collections.Generic.List<EditorBuildSettingsScene>(currentScenes);

        if (!hasMainMenu && File.Exists("Assets/_Project/Scenes/MainMenu.unity"))
            sceneList.Insert(0, new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity", true));

        if (!hasLab && File.Exists("Assets/_Project/Scenes/Lab.unity"))
            sceneList.Add(new EditorBuildSettingsScene("Assets/_Project/Scenes/Lab.unity", true));

        if (!hasLevel2 && File.Exists(SCENE_PATH))
            sceneList.Add(new EditorBuildSettingsScene(SCENE_PATH, true));

        EditorBuildSettings.scenes = sceneList.ToArray();
    }

    // ─── HELPERS ─────────────────────────────────────────────────────
    private static void BuildStraightCorridor(Transform parent, string name, Vector3 center, Vector3 size, bool alongX)
    {
        var root = new GameObject(name);
        root.transform.SetParent(parent);

        Color wallCol = new Color(0.20f, 0.22f, 0.24f);
        Color floorCol = new Color(0.12f, 0.13f, 0.14f);

        // San va Tran hanh lang
        MakeCube(root.transform, "Floor", center + Vector3.down * 0.1f, new Vector3(size.x, 0.2f, size.z), floorCol, true);
        MakeCube(root.transform, "Ceiling", center + Vector3.up * size.y, new Vector3(size.x, 0.2f, size.z), wallCol * 0.5f, true);

        // Tuong 2 ben hanh lang
        if (alongX)
        {
            float halfZ = size.z / 2f;
            MakeCube(root.transform, "Wall_South", center + new Vector3(0, size.y / 2f, -halfZ), new Vector3(size.x, size.y, 0.4f), wallCol, true);
            MakeCube(root.transform, "Wall_North", center + new Vector3(0, size.y / 2f,  halfZ), new Vector3(size.x, size.y, 0.4f), wallCol, true);
        }
        else
        {
            float halfX = size.x / 2f;
            MakeCube(root.transform, "Wall_West", center + new Vector3(-halfX, size.y / 2f, 0), new Vector3(0.4f, size.y, size.z), wallCol, true);
            MakeCube(root.transform, "Wall_East", center + new Vector3( halfX, size.y / 2f, 0), new Vector3(0.4f, size.y, size.z), wallCol, true);
        }
    }

    private static GameObject MakeCube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool isStatic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        go.isStatic = isStatic;
        SetColor(go, color);
        return go;
    }

    private static void SetColor(GameObject go, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().material = mat;
    }

    private static GameObject MakeUIPanel(Transform p, string n, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 sz)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = amin; rt.anchorMax = amax;
        rt.pivot = new Vector2(amin.x > 0.6f ? 1 : amin.x < 0.4f ? 0 : 0.5f, amin.y > 0.6f ? 1 : amin.y < 0.4f ? 0 : 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        go.AddComponent<UnityEngine.UI.Image>().color = new Color(0,0,0,0.7f);
        return go;
    }

    private static GameObject MakeUIText(Transform p, string n, string txt, int sz, Vector2 pos)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(340,50);
        var t = go.AddComponent<UnityEngine.UI.Text>();
        t.text = txt; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = sz; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.raycastTarget = false;
        var ol = go.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }

    private static GameObject MakeUIButton(Transform p, string n, string lbl, Vector2 pos, Vector2 sz = default)
    {
        if (sz == default) sz = new Vector2(200, 50);
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f,0.5f); rt.anchorMax = new Vector2(0.5f,0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = sz;
        go.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f,0.4f,0.8f,0.9f);
        go.AddComponent<UnityEngine.UI.Button>();
        var tgo = new GameObject("Text"); tgo.transform.SetParent(go.transform, false);
        var trt = tgo.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero; trt.offsetMax = Vector2.zero;
        var t = tgo.AddComponent<UnityEngine.UI.Text>();
        t.text = lbl; t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = 20; t.fontStyle = FontStyle.Bold;
        t.alignment = TextAnchor.MiddleCenter; t.color = Color.white;
        t.raycastTarget = false;
        var ol = tgo.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }
}
#endif
