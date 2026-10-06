#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;
using System.Collections.Generic;

/// <summary>
/// HelipadEnvironmentBuilder: Xay dung toan bo Man 3 (Level 3: Helipad Escape).
/// - San do truc thang bat giac tren tang thuong (Rooftop Helipad)
/// - Chiec truc thang cuu ho chi tiet (Than, Canh quat chinh, Canh quat duoi, Cang dap, Den pha)
/// - Thap Dieu Khien & Tram Radar (Dia radar xoay, ban may tinh console)
/// - Kho Tiep Lieu (Chua Can Nhien Lieu Phan Luc Jet Fuel)
/// - Buong thang may den (Diem xuat phat cua nguoi choi tu Man 2)
/// - He thong den duong bang san bay (Runway Lights)
/// - Hang rao bao ve / Cong vom san do (Blast Gates)
/// - Robot AI Sentinel tuan tra tang thuong
/// - First Person Camera, Thanh Mau, HUD Canvas, Man hinh Chien Thang Toan Bo Game!
/// - Tu dong Bake NavMesh & Cap nhat Build Settings.
/// </summary>
public static class HelipadEnvironmentBuilder
{
    private const string SCENE_PATH = "Assets/_Project/Scenes/Level3_Helipad.unity";

    [MenuItem("EscapeTheLab/🏗️ 3) Build Level 3 (Rooftop Helipad & Helicopter Escape)", false, 23)]
    public static void BuildLevel3_Menu()
    {
        bool confirm = EditorUtility.DisplayDialog("Xây dựng Màn 3 (Level 3: Helipad)",
            "Hệ thống sẽ tự động tạo màn 3 (Sân đỗ trực thăng tầng thượng):\n\n" +
            "• Sân đỗ trực thăng hình bát giác với vạch chữ [ H ] dạ quang\n" +
            "• Trực thăng cứu hộ chi tiết (Cánh quạt xoay, động cơ nổ máy, cất cánh)\n" +
            "• Tháp Điều Khiển & Trạm Radar (Đĩa xoay, Bảng điều khiển tín hiệu)\n" +
            "• Kho Tiếp Liệu chứa Can Nhiên Liệu Phản Lực (Jet Fuel)\n" +
            "• Hệ thống 4 Cổng vòm / Rào chắn sân đỗ hạ xuống khi bật Radar\n" +
            "• Buồng thang máy hàng hóa xuất phát từ Màn 2\n" +
            "• Robot AI tuần tra, First Person Camera, HUD & Màn hình Chiến Thắng Toàn Bộ Game\n\n" +
            "Bạn có muốn xây dựng ngay bây giờ?",
            "Xây dựng ngay!", "Hủy");
        if (!confirm) return;

        BuildLevel3_Internal(true);
    }

    public static void BuildLevel3_Direct(bool showDialog = false)
    {
        EnsureSceneExistsAndOpen();
        BuildLevel3_Internal(showDialog);
    }

    private static void BuildLevel3_Internal(bool showDialog = true)
    {
        try
        {
            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Dọn dẹp môi trường cũ...", 0.05f);
            ClearExistingObjects();

            GameObject envRoot = new GameObject("=== LEVEL 3: HELIPAD ENVIRONMENT ===");

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Tạo nền móng tầng thượng & Lan can bảo vệ...", 0.12f);
            BuildRooftopBase(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Tòa nhà Nóc Sân Đỗ & Cầu Thang Lên Nóc...", 0.22f);
            BuildHelipadBuildingAndStairs(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Sân đỗ trực thăng trên nóc nhà...", 0.34f);
            var blastGates = BuildHelipadPlatform(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Trực thăng cứu hộ trên nóc nhà...", 0.45f);
            BuildRescueHelicopter(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Tháp Điều Khiển & Trạm Radar...", 0.55f);
            BuildControlTowerAndRadar(envRoot.transform, blastGates);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Kho Tiếp Liệu & Chứa Nhiên Liệu...", 0.68f);
            BuildFuelDepot(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Buồng Thang Máy Đến...", 0.78f);
            BuildElevatorArrival(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Thiết lập Ánh sáng đêm tầng thượng...", 0.84f);
            SetupRooftopLighting(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Thiết lập Người chơi & 2 Robot Tuần Tra Trang Bị Súng...", 0.90f);
            SetupPlayerAndTwoRobots(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Thiết lập UI Canvas & Victory Screen...", 0.94f);
            SetupCanvasUI(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Bake NavMesh cho AI...", 0.97f);
            BakeNavMesh(envRoot);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Cập nhật Build Settings...", 0.98f);
            UpdateBuildSettings();

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Nâng cấp texture tường P3D Outdoor Wall Tile...", 0.99f);
            EscapeTheLab.EditorTools.AssetUpgradeTools.ApplyP3DWallTexturesToScene(EditorSceneManager.GetActiveScene());

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorUtility.ClearProgressBar();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Thành công!",
                    "Màn 3 (Level 3: Rooftop Helipad & Escape) đã được xây dựng hoàn tất 100%!\n\n" +
                    "• 2 Robot tuần tra trang bị súng laser di chuyển khắp toàn bộ bản đồ.\n" +
                    "• Vào Kho Tiếp Liệu (phía Tây) để nhặt BÌNH NHIÊN LIỆU PHẢN LỰC (Jet Fuel).\n" +
                    "• Vào Tháp Điều Khiển (phía Đông) để KÍCH HOẠT TRẠM RADAR không lưu.\n" +
                    "• Leo cầu thang lên nóc nhà tiếp cận Trực Thăng, bấm [E] để TIẾP NHIÊN LIỆU & CẤT CÁNH TẨU THOÁT!\n\n" +
                    "Nhấn nút ▶ PLAY để trải nghiệm ngay!", "Tuyệt vời!");
            }
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[HelipadBuilder] " + ex);
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
        else
        {
            EditorSceneManager.OpenScene(SCENE_PATH);
        }
    }

    private static void ClearExistingObjects()
    {
        var oldEnv = GameObject.Find("=== LEVEL 3: HELIPAD ENVIRONMENT ===");
        if (oldEnv != null) Object.DestroyImmediate(oldEnv);

        foreach (var name in new[] { "Player", "Robot", "Robot_1", "Robot_2", "GameplayCanvas", "EventSystem", "MobileInputController", "Waypoints", "Waypoints_Robot_1", "Waypoints_Robot_2", "Main Camera", "NavMesh Surface", "SafetyPerimeter" })
        {
            var go = GameObject.Find(name);
            if (go != null) Object.DestroyImmediate(go);
        }
    }

    // ─── 1. NỀN TẦNG THƯỢNG & LAN CAN BẢO VỆ ────────────────────────────
    private static void BuildRooftopBase(Transform parent)
    {
        var root = new GameObject("RooftopFoundation");
        root.transform.SetParent(parent);

        // Nền an toàn chống rơi dưới cùng
        MakeCube(root.transform, "SafetyPerimeterFloor", new Vector3(0, -1.0f, 0), new Vector3(140f, 0.6f, 140f), new Color(0.06f, 0.06f, 0.08f), true);

        // Sàn tầng thượng bê tông công nghiệp: 70m x 70m
        Color floorCol = new Color(0.18f, 0.19f, 0.22f);
        MakeCube(root.transform, "MainRooftopFloor", new Vector3(0, -0.2f, 0), new Vector3(70f, 0.4f, 70f), floorCol, true);

        // Lan can an toàn bằng thép & kính cường lực xung quanh 4 phía
        Color fenceCol = new Color(0.12f, 0.14f, 0.16f);
        float half = 35f;

        // Bac
        MakeCube(root.transform, "Fence_North", new Vector3(0, 1.2f, half), new Vector3(70f, 2.4f, 0.5f), fenceCol, true);
        // Nam
        MakeCube(root.transform, "Fence_South", new Vector3(0, 1.2f, -half), new Vector3(70f, 2.4f, 0.5f), fenceCol, true);
        // Tay
        MakeCube(root.transform, "Fence_West", new Vector3(-half, 1.2f, 0), new Vector3(0.5f, 2.4f, 70f), fenceCol, true);
        // Dong
        MakeCube(root.transform, "Fence_East", new Vector3(half, 1.2f, 0), new Vector3(0.5f, 2.4f, 70f), fenceCol, true);

        // Hàng rào vô hình bảo vệ chống nhảy ra ngoài (10m)
        MakeInvisibleWall(root.transform, "SafetyWall_N", new Vector3(0, 5f, half + 1f), new Vector3(75f, 10f, 1f));
        MakeInvisibleWall(root.transform, "SafetyWall_S", new Vector3(0, 5f, -half - 1f), new Vector3(75f, 10f, 1f));
        MakeInvisibleWall(root.transform, "SafetyWall_W", new Vector3(-half - 1f, 5f, 0), new Vector3(1f, 10f, 75f));
        MakeInvisibleWall(root.transform, "SafetyWall_E", new Vector3(half + 1f, 5f, 0), new Vector3(1f, 10f, 75f));
    }

    private static void MakeInvisibleWall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = scale;
        var rend = go.GetComponent<Renderer>();
        if (rend != null) rend.enabled = false;
    }

    // ─── 1B. TÒA NHÀ NÓC SÂN ĐỖ & CẦU THANG LÊN NÓC (BUILDING & ROOFTOP STAIRS) ──
    private static void BuildHelipadBuildingAndStairs(Transform parent)
    {
        var root = new GameObject("HelipadBuilding_And_Stairs");
        root.transform.SetParent(parent);

        // Khối tòa nhà kiên cố đỡ sân đỗ trực thăng: 26m (rộng) x 4.8m (cao) x 26m (sâu)
        // Tâm tại: X = 0, Y = 2.4f, Z = 7.5f
        Color buildingCol = new Color(0.20f, 0.22f, 0.25f);
        Color accentCol = new Color(0.14f, 0.15f, 0.17f);
        MakeCube(root.transform, "Building_MainCore", new Vector3(0, 2.4f, 7.5f), new Vector3(26f, 4.8f, 26f), buildingCol, true);
        MakeCube(root.transform, "Building_BaseTrim", new Vector3(0, 0.4f, 7.5f), new Vector3(27f, 0.8f, 27f), accentCol, true);
        MakeCube(root.transform, "Building_RoofCornice", new Vector3(0, 4.7f, 7.5f), new Vector3(27f, 0.4f, 27f), accentCol, true);

        // Lan can an toàn trên nóc nhà (bao quanh 3 mặt Tây, Đông, Bắc và 2 bên lối vào cầu thang phía Nam)
        Color railCol = new Color(0.85f, 0.65f, 0.12f); // Màu vàng bảo hộ lao động
        MakeCube(root.transform, "RooftopRailing_North", new Vector3(0, 5.5f, 20.6f), new Vector3(26.2f, 1.4f, 0.4f), railCol, true);
        MakeCube(root.transform, "RooftopRailing_West",  new Vector3(-13.1f, 5.5f, 7.5f), new Vector3(0.4f, 1.4f, 26.2f), railCol, true);
        MakeCube(root.transform, "RooftopRailing_East",  new Vector3( 13.1f, 5.5f, 7.5f), new Vector3(0.4f, 1.4f, 26.2f), railCol, true);
        MakeCube(root.transform, "RooftopRailing_South_L", new Vector3(-8.0f, 5.5f, -5.6f), new Vector3(10.2f, 1.4f, 0.4f), railCol, true);
        MakeCube(root.transform, "RooftopRailing_South_R", new Vector3( 8.0f, 5.5f, -5.6f), new Vector3(10.2f, 1.4f, 0.4f), railCol, true);

        // HỆ THỐNG CẦU THANG CÔNG NGHIỆP DẪN LÊN NÓC NHÀ (STAIRCASE)
        // Bắt đầu từ mặt đất: Z = -14.5f, Y = 0 đến mép nóc nhà: Z = -5.5f, Y = 4.8f (chênh lệch: cao 4.8m, dài 9.0m)
        var stairsRoot = new GameObject("IndustrialStairs");
        stairsRoot.transform.SetParent(root.transform);

        int numSteps = 16;
        float totalHeight = 4.8f;
        float totalLength = 9.0f;
        float startZ = -14.5f;
        float stepHeight = totalHeight / numSteps; // 0.3f
        float stepLength = totalLength / numSteps; // 0.5625f
        float stairWidth = 3.6f;

        Color stepCol = new Color(0.26f, 0.28f, 0.32f);

        for (int i = 0; i < numSteps; i++)
        {
            float stepY = (i + 0.5f) * stepHeight;
            float stepZ = startZ + (i + 0.5f) * stepLength;
            MakeCube(stairsRoot.transform, $"StairStep_{i+1}", new Vector3(0, stepY, stepZ), new Vector3(stairWidth, stepHeight, stepLength * 1.15f), stepCol, true);
        }

        // Tấm dốc vô hình (Invisible Smooth Ramp Collider) để người chơi leo mượt mà 100% không khựng giật
        float slopeAngle = Mathf.Atan2(totalHeight, totalLength) * Mathf.Rad2Deg; // ~28.07 deg
        float rampLength = Mathf.Sqrt(totalHeight * totalHeight + totalLength * totalLength); // ~10.2m
        Vector3 rampCenter = new Vector3(0, totalHeight * 0.5f, startZ + totalLength * 0.5f);

        var ramp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        ramp.name = "StairSmoothRampCollider";
        ramp.transform.SetParent(stairsRoot.transform);
        ramp.transform.position = rampCenter + new Vector3(0, 0.05f, 0);
        ramp.transform.rotation = Quaternion.Euler(-slopeAngle, 0, 0);
        ramp.transform.localScale = new Vector3(stairWidth, 0.15f, rampLength + 0.3f);
        var rampRend = ramp.GetComponent<Renderer>();
        if (rampRend != null) rampRend.enabled = false;

        // Lan can 2 bên tay vịn cầu thang (Handrails)
        Vector3 railOffsetL = new Vector3(-stairWidth * 0.5f, totalHeight * 0.5f + 0.7f, startZ + totalLength * 0.5f);
        Vector3 railOffsetR = new Vector3( stairWidth * 0.5f, totalHeight * 0.5f + 0.7f, startZ + totalLength * 0.5f);

        var railL = MakeCube(stairsRoot.transform, "Handrail_L", railOffsetL, new Vector3(0.12f, 0.12f, rampLength), railCol, true);
        railL.transform.rotation = Quaternion.Euler(-slopeAngle, 0, 0);
        var railR = MakeCube(stairsRoot.transform, "Handrail_R", railOffsetR, new Vector3(0.12f, 0.12f, rampLength), railCol, true);
        railR.transform.rotation = Quaternion.Euler(-slopeAngle, 0, 0);

        // Các cột chống lan can cầu thang
        for (int p = 0; p <= 4; p++)
        {
            float t = p / 4.0f;
            float py = t * totalHeight + 0.35f;
            float pz = startZ + t * totalLength;
            MakeCube(stairsRoot.transform, $"Post_L_{p}", new Vector3(-stairWidth * 0.5f, py, pz), new Vector3(0.1f, 0.8f, 0.1f), Color.gray, true);
            MakeCube(stairsRoot.transform, $"Post_R_{p}", new Vector3( stairWidth * 0.5f, py, pz), new Vector3(0.1f, 0.8f, 0.1f), Color.gray, true);
        }

        // Cổng chào & Biển hiệu hướng dẫn ở chân cầu thang (Stairs Entrance Arch)
        var archRoot = new GameObject("StairsEntranceArch");
        archRoot.transform.SetParent(stairsRoot.transform);
        MakeCube(archRoot.transform, "Arch_Pillar_L", new Vector3(-2.0f, 1.8f, startZ - 0.4f), new Vector3(0.35f, 3.6f, 0.35f), Color.gray, true);
        MakeCube(archRoot.transform, "Arch_Pillar_R", new Vector3( 2.0f, 1.8f, startZ - 0.4f), new Vector3(0.35f, 3.6f, 0.35f), Color.gray, true);
        MakeCube(archRoot.transform, "Arch_Crossbeam", new Vector3(0, 3.6f, startZ - 0.4f), new Vector3(4.35f, 0.4f, 0.4f), railCol, true);

        // Đèn dẫn lối chân cầu thang
        var lGO_L = new GameObject("StairsLight_L");
        lGO_L.transform.SetParent(archRoot.transform);
        lGO_L.transform.position = new Vector3(-2.0f, 2.2f, startZ - 0.2f);
        var slL = lGO_L.AddComponent<Light>();
        slL.type = LightType.Point;
        slL.color = new Color(0.2f, 1f, 0.5f);
        slL.intensity = 2.5f;
        slL.range = 6f;

        var lGO_R = new GameObject("StairsLight_R");
        lGO_R.transform.SetParent(archRoot.transform);
        lGO_R.transform.position = new Vector3(2.0f, 2.2f, startZ - 0.2f);
        var slR = lGO_R.AddComponent<Light>();
        slR.type = LightType.Point;
        slR.color = new Color(0.2f, 1f, 0.5f);
        slR.intensity = 2.5f;
        slR.range = 6f;

        // Biển chữ neon hướng dẫn leo lên nóc nhà
        var signGO = new GameObject("StairsNeonSign");
        signGO.transform.SetParent(archRoot.transform, false);
        signGO.transform.localPosition = new Vector3(0, 3.6f, startZ - 0.7f);
        signGO.transform.localScale = Vector3.one * 0.025f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "▲ CẦU THANG LÊN NÓC SÂN ĐỖ ▲\n[ HELIPAD ROOFTOP STAIRS ]";
        tm.fontSize = 36;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.cyan;

        // VÙNG KÍCH HOẠT NHIỆM VỤ KHI LÊN ĐẾN NÓC NHÀ (ROOFTOP STAIRS TRIGGER)
        var triggerGO = new GameObject("RooftopStairsTrigger");
        triggerGO.transform.SetParent(stairsRoot.transform);
        triggerGO.transform.position = new Vector3(0, 5.5f, -5.0f);
        var triggerCol = triggerGO.AddComponent<BoxCollider>();
        triggerCol.size = new Vector3(4.5f, 3.0f, 3.0f);
        triggerCol.isTrigger = true;
        triggerGO.AddComponent<RooftopStairsTrigger>();

        // Vật cản & Thùng hàng che chắn trong sân trước cầu thang (Cover crates for player)
        var cratesRoot = new GameObject("CourtyardCoverObstacles");
        cratesRoot.transform.SetParent(root.transform);
        Color crateCol = new Color(0.45f, 0.32f, 0.2f);
        MakeCube(cratesRoot.transform, "Crate_L1", new Vector3(-6.5f, 1.0f, -18f), new Vector3(2.2f, 2.0f, 2.2f), crateCol, true);
        MakeCube(cratesRoot.transform, "Crate_L2", new Vector3(-7.5f, 2.5f, -18f), new Vector3(1.6f, 1.2f, 1.6f), crateCol * 1.1f, true);
        MakeCube(cratesRoot.transform, "Crate_R1", new Vector3( 6.5f, 1.0f, -18f), new Vector3(2.2f, 2.0f, 2.2f), crateCol, true);
        MakeCube(cratesRoot.transform, "Crate_R2", new Vector3( 7.5f, 2.5f, -18f), new Vector3(1.6f, 1.2f, 1.6f), crateCol * 1.1f, true);

        // Tấm chắn bê tông phòng thủ giữa sân
        Color barrierCol = new Color(0.35f, 0.37f, 0.4f);
        MakeCube(cratesRoot.transform, "ConcreteBarrier_L", new Vector3(-3.0f, 0.6f, -16f), new Vector3(2.8f, 1.2f, 0.7f), barrierCol, true);
        MakeCube(cratesRoot.transform, "ConcreteBarrier_R", new Vector3( 3.0f, 0.6f, -16f), new Vector3(2.8f, 1.2f, 0.7f), barrierCol, true);

        // CÁC VẬT PHẨM HỖ TRỢ TRONG SÂN TRƯỚC (MEDKITS & NĂNG LƯỢNG)
        // 1. Hộp Cứu Thương trên thùng hàng bên trái (ngay tầm nhìn khi rời thang máy)
        CreateItemPickup(cratesRoot.transform, "Item_Medkit_Courtyard_1",
            PickupItem.ItemType.Medkit,
            "Hộp Cứu Thương Tiền Tuyến (+50 HP)",
            "ĐÃ NHẶT: Hộp Cứu Thương Tiền Tuyến! (+50 HP)",
            new Vector3(-6.5f, 2.2f, -18f),
            new Color(0.2f, 0.95f, 0.4f),
            PrimitiveType.Cube,
            new Vector3(0.6f, 0.45f, 0.6f));

        // 2. Bình Năng Lượng / Khiên Giáp trên thùng hàng bên phải
        CreateItemPickup(cratesRoot.transform, "Item_Battery_Courtyard_2",
            PickupItem.ItemType.Battery,
            "Bình Năng Lượng Giáp Bền (+35 HP)",
            "ĐÃ NHẶT: Bình Năng Lượng! Phục hồi sinh lực.",
            new Vector3(6.5f, 2.2f, -18f),
            new Color(0.2f, 0.75f, 1.0f),
            PrimitiveType.Cylinder,
            new Vector3(0.5f, 0.65f, 0.5f));

        // 3. Hộp Cứu Thương ngay chân cầu thang lên nóc nhà
        CreateItemPickup(stairsRoot.transform, "Item_Medkit_Stairs",
            PickupItem.ItemType.Medkit,
            "Hộp Cứu Thương Khẩn Cấp (+50 HP)",
            "ĐÃ NHẶT: Hộp Cứu Thương Chân Cầu Thang! (+50 HP)",
            new Vector3(-2.8f, 0.6f, -14.5f),
            new Color(0.2f, 0.95f, 0.4f),
            PrimitiveType.Cube,
            new Vector3(0.6f, 0.45f, 0.6f));

        // 4. Bình Năng Lượng / Khiên Giáp đặt ở rào chắn sân trước
        CreateItemPickup(cratesRoot.transform, "Item_Courtyard_Battery_Extra",
            PickupItem.ItemType.Battery,
            "Bình Năng Lượng Giáp Bền (+35 HP)",
            "ĐÃ NHẶT: Bình Năng Lượng Giáp Bền! Gia tăng phòng ngự trước laser robot.",
            new Vector3(3.2f, 1.35f, -16f),
            new Color(0.2f, 0.75f, 1.0f),
            PrimitiveType.Cylinder,
            new Vector3(0.45f, 0.6f, 0.45f));
    }

    // ─── 2. SÂN ĐỖ TRỰC THĂNG TRÊN NÓC NHÀ (ROOFTOP HELIPAD) ─────────────
    private static Transform[] BuildHelipadPlatform(Transform parent)
    {
        var root = new GameObject("CentralHelipad");
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(-3.5f, 4.8f, 6.5f); // Tọa độ chính xác giữa sân tầng thượng người chơi nhắm tới

        // Tấm kim loại bệ đáp bát giác lớn: 18m x 18m
        var padFloor = MakeCube(root.transform, "HelipadPlate", new Vector3(0, 0.05f, 0), new Vector3(18f, 0.1f, 18f), new Color(0.12f, 0.13f, 0.15f), true);

        // Vòng viền vàng bao quanh viền sân đỗ
        MakeCube(root.transform, "Border_N", new Vector3(0, 0.11f, 8.5f), new Vector3(17f, 0.03f, 0.5f), new Color(1f, 0.85f, 0.1f), true);
        MakeCube(root.transform, "Border_S", new Vector3(0, 0.11f, -8.5f), new Vector3(17f, 0.03f, 0.5f), new Color(1f, 0.85f, 0.1f), true);
        MakeCube(root.transform, "Border_W", new Vector3(-8.5f, 0.11f, 0), new Vector3(0.5f, 0.03f, 17f), new Color(1f, 0.85f, 0.1f), true);
        MakeCube(root.transform, "Border_E", new Vector3(8.5f, 0.11f, 0), new Vector3(0.5f, 0.03f, 17f), new Color(1f, 0.85f, 0.1f), true);

        // Biểu tượng chữ [ H ] màu vàng dạ quang phát sáng ở trung tâm sân đỗ
        Color hCol = new Color(1f, 0.9f, 0.2f);
        var hL = MakeCube(root.transform, "Mark_H_Left",   new Vector3(-2.4f, 0.12f, 0), new Vector3(0.8f, 0.04f, 5.2f), hCol, true);
        var hR = MakeCube(root.transform, "Mark_H_Right",  new Vector3( 2.4f, 0.12f, 0), new Vector3(0.8f, 0.04f, 5.2f), hCol, true);
        var hC = MakeCube(root.transform, "Mark_H_Center", new Vector3(    0, 0.12f, 0), new Vector3(4.8f, 0.04f, 0.8f), hCol, true);
        SetGlowColor(hL, hCol, 2.0f);
        SetGlowColor(hR, hCol, 2.0f);
        SetGlowColor(hC, hCol, 2.0f);

        // Hệ thống 8 đèn tín hiệu đường băng sân bay (Runway Lights)
        Vector3[] lightPositions = new Vector3[] {
            new Vector3(-8.2f, 0.35f, -8.2f),
            new Vector3(  0f,  0.35f, -8.6f),
            new Vector3( 8.2f, 0.35f, -8.2f),
            new Vector3( 8.6f, 0.35f,   0f),
            new Vector3( 8.2f, 0.35f,  8.2f),
            new Vector3(  0f,  0.35f,  8.6f),
            new Vector3(-8.2f, 0.35f,  8.2f),
            new Vector3(-8.6f, 0.35f,   0f)
        };

        var lightsRoot = new GameObject("RunwayLights");
        lightsRoot.transform.SetParent(root.transform);

        for (int i = 0; i < lightPositions.Length; i++)
        {
            var lPost = MakeCube(lightsRoot.transform, $"LightPost_{i+1}", lightPositions[i], new Vector3(0.25f, 0.6f, 0.25f), new Color(0.2f, 0.2f, 0.25f), true);
            var lGO = new GameObject($"PointLight_{i+1}");
            lGO.transform.SetParent(lPost.transform, false);
            lGO.transform.localPosition = new Vector3(0, 0.4f, 0);
            var pl = lGO.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.color = (i % 2 == 0) ? new Color(0.2f, 0.8f, 1f) : new Color(1f, 0.75f, 0.1f);
            pl.range = 5f;
            pl.intensity = 1.6f;
        }

        // 4 Cột tháp Cổng Vòm / Rào Chắn Sân Đỗ (Blast Gates) ở 4 góc
        Vector3[] gatePositions = new Vector3[] {
            new Vector3(-8.8f, 2.0f, -8.8f),
            new Vector3( 8.8f, 2.0f, -8.8f),
            new Vector3( 8.8f, 2.0f,  8.8f),
            new Vector3(-8.8f, 2.0f,  8.8f)
        };

        Transform[] gates = new Transform[4];
        var gatesRoot = new GameObject("BlastShieldPillars");
        gatesRoot.transform.SetParent(root.transform);

        for (int i = 0; i < 4; i++)
        {
            var pillar = MakeCube(gatesRoot.transform, $"Pillar_{i+1}", gatePositions[i], new Vector3(1.2f, 4.5f, 1.2f), new Color(0.25f, 0.28f, 0.32f), false);
            var barrier = MakeCube(pillar.transform, "BarrierPlate", new Vector3(0, 0.5f, 0), new Vector3(0.9f, 3.5f, 0.9f), new Color(0.9f, 0.3f, 0.1f), false);
            gates[i] = barrier.transform;
        }

        return gates;
    }

    // ─── 3. TRỰC THĂNG CỨU HỘ THOÁT HIỂM TRÊN NÓC NHÀ (RESCUE HELICOPTER) ──
    private static void BuildRescueHelicopter(Transform parent)
    {
        var heliRoot = new GameObject("RescueHelicopter");
        heliRoot.transform.SetParent(parent);
        heliRoot.transform.position = new Vector3(-3.5f, 4.8f, 6.5f); // Đặt trên sàn nóc nhà (Y = 4.8f)
        heliRoot.transform.rotation = Quaternion.Euler(0, 180f, 0);   // Quay đầu về hướng cầu thang đón người chơi

        int intLayer = LayerMask.NameToLayer("Interactable");
        heliRoot.layer = intLayer >= 0 ? intLayer : 0;

        var heliPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Helicopter/Resources/Models/HelicopterModel/OH-58D.fbx");
        if (heliPrefab != null)
        {
            BuildMilitaryRescueHelicopter(heliRoot, heliPrefab);
            return;
        }

        // BẢNG MÀU CHUẨN CỨU HỘ QUỐC TẾ (US COAST GUARD / AIR AMBULANCE STYLING)
        Color whiteCol   = new Color(0.96f, 0.97f, 0.98f); // Thân trắng máy bay sang trọng
        Color orangeCol  = new Color(1.0f, 0.38f, 0.05f);  // Cam cứu hộ dạ quang nổi bật
        Color blackCol   = new Color(0.12f, 0.13f, 0.15f); // Đen nhám chi tiết
        Color glassCol   = new Color(0.25f, 0.85f, 1.0f);  // Kính buồng lái vòm xanh ngọc phát sáng
        Color metalCol   = new Color(0.72f, 0.75f, 0.78f); // Kim loại crom sáng bóng ống xả
        Color yellowCol  = new Color(1.0f, 0.88f, 0.12f);  // Vàng cảnh báo an toàn cánh quạt

        // ── 1. CÀNG ĐÁP ỐNG TRÒN NÂNG THÂN MÁY BAY LÊN CAO 1.1M (TUBULAR SKIDS) ──
        // 2 Thanh trượt càng đáp kim loại dài 6.6m với đầu mũi cong vuốt lên
        MakeCylinder(heliRoot.transform, "Skid_Tube_L", new Vector3(-1.35f, 0.1f, 0.2f), new Vector3(0.16f, 3.3f, 0.16f), Quaternion.Euler(90f, 0, 0), metalCol, false);
        MakeCylinder(heliRoot.transform, "Skid_Tube_R", new Vector3( 1.35f, 0.1f, 0.2f), new Vector3(0.16f, 3.3f, 0.16f), Quaternion.Euler(90f, 0, 0), metalCol, false);

        // Mũi càng đáp uốn cong vểnh lên 35 độ đặc trưng của trực thăng
        MakeCylinder(heliRoot.transform, "Skid_Tip_L", new Vector3(-1.35f, 0.35f, 3.65f), new Vector3(0.16f, 0.5f, 0.16f), Quaternion.Euler(55f, 0, 0), metalCol, false);
        MakeCylinder(heliRoot.transform, "Skid_Tip_R", new Vector3( 1.35f, 0.35f, 3.65f), new Vector3(0.16f, 0.5f, 0.16f), Quaternion.Euler(55f, 0, 0), metalCol, false);

        // 4 Cột chống xiên (Cross-Tubes) nối càng đáp nâng bổng bụng trực thăng
        MakeCylinder(heliRoot.transform, "Strut_FL", new Vector3(-1.1f, 0.6f,  1.5f), new Vector3(0.12f, 0.65f, 0.12f), Quaternion.Euler(0, 0, -28f), metalCol, false);
        MakeCylinder(heliRoot.transform, "Strut_FR", new Vector3( 1.1f, 0.6f,  1.5f), new Vector3(0.12f, 0.65f, 0.12f), Quaternion.Euler(0, 0,  28f), metalCol, false);
        MakeCylinder(heliRoot.transform, "Strut_RL", new Vector3(-1.1f, 0.6f, -1.3f), new Vector3(0.12f, 0.65f, 0.12f), Quaternion.Euler(0, 0, -28f), metalCol, false);
        MakeCylinder(heliRoot.transform, "Strut_RR", new Vector3( 1.1f, 0.6f, -1.3f), new Vector3(0.12f, 0.65f, 0.12f), Quaternion.Euler(0, 0,  28f), metalCol, false);

        // Bậc bước chân lên khoang có sọc vàng đen
        MakeCube(heliRoot.transform, "FootStep_L", new Vector3(-1.35f, 0.2f, 0.2f), new Vector3(0.28f, 0.06f, 1.6f), yellowCol, false);
        MakeCube(heliRoot.transform, "FootStep_R", new Vector3( 1.35f, 0.2f, 0.2f), new Vector3(0.28f, 0.06f, 1.6f), yellowCol, false);

        // ── 2. THÂN KHOANG HÀNH KHÁCH KHÍ ĐỘNG HỌC (STREAMLINED CABIN) ──
        // Khoang chính màu trắng (elevated from Y = 1.1m to 2.8m)
        var cabin = MakeCube(heliRoot.transform, "Cabin_Body", new Vector3(0, 1.95f, 0.2f), new Vector3(2.2f, 1.7f, 4.4f), whiteCol, false);
        cabin.layer = heliRoot.layer;

        // Bụng dưới thuôn dốc màu cam cứu hộ
        MakeCube(heliRoot.transform, "Cabin_Belly", new Vector3(0, 1.18f, 0.2f), new Vector3(1.95f, 0.25f, 3.8f), orangeCol, false);

        // Sọc cam cứu hộ dọc 2 bên hông thân máy bay
        MakeCube(heliRoot.transform, "RescueStripe_L", new Vector3(-1.12f, 1.95f, 0.2f), new Vector3(0.04f, 0.5f, 4.4f), orangeCol, false);
        MakeCube(heliRoot.transform, "RescueStripe_R", new Vector3( 1.12f, 1.95f, 0.2f), new Vector3(0.04f, 0.5f, 4.4f), orangeCol, false);

        // ── 3. VÒM KÍNH BUỒNG LÁI TRƯỢT TRÒN ĐẶC TRƯNG (BUBBLE COCKPIT CANOPY) ──
        // Vòm kính bán cầu bầu dục lớn nhô ra phía trước mũi
        var canopy = MakeSphere(heliRoot.transform, "Cockpit_CanopyBubble", new Vector3(0, 1.92f, 2.5f), new Vector3(2.15f, 1.65f, 2.6f), glassCol, false);
        SetGlowColor(canopy, glassCol, 0.8f);

        // Mũi nhọn radar khí tượng phía trước (Nose Cone Pod)
        MakeSphere(heliRoot.transform, "NoseConePod", new Vector3(0, 1.4f, 3.65f), new Vector3(1.2f, 0.9f, 1.2f), blackCol, false);

        // Khung viền kính buồng lái màu đen
        MakeCube(heliRoot.transform, "Windshield_Frame_V", new Vector3(0, 2.1f, 2.85f), new Vector3(0.1f, 1.4f, 1.6f), blackCol, false);
        MakeCube(heliRoot.transform, "Windshield_Frame_H", new Vector3(0, 1.42f, 3.0f), new Vector3(2.1f, 0.1f, 1.4f), blackCol, false);

        // Cửa trượt bên hông (Side Doors) với kính quan sát
        MakeCube(heliRoot.transform, "DoorWindow_L", new Vector3(-1.12f, 2.0f, -0.4f), new Vector3(0.05f, 0.7f, 1.4f), glassCol, false);
        MakeCube(heliRoot.transform, "DoorWindow_R", new Vector3( 1.12f, 2.0f, -0.4f), new Vector3(0.05f, 0.7f, 1.4f), glassCol, false);

        // Chữ hiệu cứu hộ lớn bên sườn "RESCUE - 01"
        var rescueSignL = new GameObject("SideDecal_L");
        rescueSignL.transform.SetParent(heliRoot.transform, false);
        rescueSignL.transform.localPosition = new Vector3(-1.13f, 2.15f, 0.9f);
        rescueSignL.transform.localRotation = Quaternion.Euler(0, -90f, 0);
        rescueSignL.transform.localScale = Vector3.one * 0.02f;
        var rtmL = rescueSignL.AddComponent<TextMesh>();
        rtmL.text = "RESCUE - 01";
        rtmL.fontSize = 42;
        rtmL.fontStyle = FontStyle.Bold;
        rtmL.color = Color.white;

        var rescueSignR = new GameObject("SideDecal_R");
        rescueSignR.transform.SetParent(heliRoot.transform, false);
        rescueSignR.transform.localPosition = new Vector3(1.13f, 2.15f, 0.9f);
        rescueSignR.transform.localRotation = Quaternion.Euler(0, 90f, 0);
        rescueSignR.transform.localScale = Vector3.one * 0.02f;
        var rtmR = rescueSignR.AddComponent<TextMesh>();
        rtmR.text = "RESCUE - 01";
        rtmR.fontSize = 42;
        rtmR.fontStyle = FontStyle.Bold;
        rtmR.color = Color.white;

        // ── 4. ĐỘNG CƠ TURBO KÉP & ỐNG XẢ KIM LOẠI TRÊN NÓC (TWIN TURBINE ENGINES) ──
        // Vỏ che động cơ (Doghouse)
        MakeCube(heliRoot.transform, "Engine_Doghouse", new Vector3(0, 2.95f, -0.1f), new Vector3(1.6f, 0.65f, 3.0f), whiteCol, false);

        // 2 Cụm động cơ turboshaft tròn bên trái & phải
        MakeCylinder(heliRoot.transform, "Turbine_L", new Vector3(-0.52f, 3.0f, -0.05f), new Vector3(0.55f, 1.4f, 0.55f), Quaternion.Euler(90f, 0, 0), orangeCol, false);
        MakeCylinder(heliRoot.transform, "Turbine_R", new Vector3( 0.52f, 3.0f, -0.05f), new Vector3(0.55f, 1.4f, 0.55f), Quaternion.Euler(90f, 0, 0), orangeCol, false);

        // 2 Ống xả kim loại sáng bóng hướng chếch ra phía sau
        MakeCylinder(heliRoot.transform, "Exhaust_Pipe_L", new Vector3(-0.52f, 2.95f, -1.6f), new Vector3(0.42f, 0.45f, 0.42f), Quaternion.Euler(75f, 0, 0), metalCol, false);
        MakeCylinder(heliRoot.transform, "Exhaust_Pipe_R", new Vector3( 0.52f, 2.95f, -1.6f), new Vector3(0.42f, 0.45f, 0.42f), Quaternion.Euler(75f, 0, 0), metalCol, false);

        // Cần tời cứu hộ (Rescue Hoist / Winch Crane) bên mạn phải buồng lái
        var hoistArm = MakeCube(heliRoot.transform, "RescueWinch_Arm", new Vector3(1.25f, 2.85f, 1.4f), new Vector3(0.12f, 0.12f, 0.7f), yellowCol, false);
        MakeCube(hoistArm.transform, "Winch_Motor", new Vector3(0, 0.1f, 0), new Vector3(0.28f, 0.28f, 0.35f), metalCol, false);

        // ── 5. ĐUÔI TRỰC THĂNG DÀI VƯƠN XA & CÁNH ĐUÔI ĐỨNG (TAIL BOOM & FIN) ──
        // Đuôi dài 8 mét vuốt thon về sau (3 đoạn khí động học)
        MakeCube(heliRoot.transform, "TailBoom_Sec1", new Vector3(0, 2.25f, -3.2f), new Vector3(0.85f, 0.85f, 2.8f), whiteCol, false);
        MakeCube(heliRoot.transform, "TailBoom_Sec2", new Vector3(0, 2.35f, -5.8f), new Vector3(0.6f, 0.6f, 2.6f), orangeCol, false);
        MakeCube(heliRoot.transform, "TailBoom_Sec3", new Vector3(0, 2.45f, -7.8f), new Vector3(0.4f, 0.45f, 2.0f), whiteCol, false);

        // Cánh ổn định ngang (Horizontal Stabilizer) với 2 vây cánh ngoài
        MakeCube(heliRoot.transform, "Horiz_Stabilizer", new Vector3(0, 2.4f, -6.6f), new Vector3(2.8f, 0.08f, 0.7f), orangeCol, false);
        MakeCube(heliRoot.transform, "EndplateFin_L", new Vector3(-1.4f, 2.4f, -6.6f), new Vector3(0.08f, 0.65f, 0.7f), whiteCol, false);
        MakeCube(heliRoot.transform, "EndplateFin_R", new Vector3( 1.4f, 2.4f, -6.6f), new Vector3(0.08f, 0.65f, 0.7f), whiteCol, false);

        // Cánh đuôi đứng cao vút vuốt nhọn (Swept Vertical Tail Fin)
        var tailFin = MakeCube(heliRoot.transform, "SweptTailFin", new Vector3(0, 3.4f, -8.7f), new Vector3(0.18f, 2.0f, 1.4f), orangeCol, false);
        tailFin.transform.localRotation = Quaternion.Euler(22f, 0, 0);

        // Biển cảnh báo đuôi "KEEP AWAY"
        MakeCube(heliRoot.transform, "TailHazardStripe", new Vector3(0, 2.45f, -7.2f), new Vector3(0.42f, 0.25f, 0.6f), yellowCol, false);

        // ── 6. CÁNH QUẠT ĐUÔI 2 LÁ XOAY TỐC ĐỘ CAO (SPINNING TAIL ROTOR) ──
        var tailRotorGO = new GameObject("TailRotorHub");
        tailRotorGO.transform.SetParent(heliRoot.transform, false);
        tailRotorGO.transform.localPosition = new Vector3(0.32f, 3.65f, -8.9f);

        // Trục xoay tròn cánh đuôi
        MakeSphere(tailRotorGO.transform, "TailSpinner", Vector3.zero, new Vector3(0.24f, 0.24f, 0.24f), metalCol, false);

        // 2 Lá cánh quạt đuôi với đầu vạch vàng an toàn
        var tb1 = MakeCube(tailRotorGO.transform, "TailBlade_1", new Vector3(0,  0.75f, 0), new Vector3(0.05f, 1.4f, 0.18f), blackCol, false);
        MakeCube(tb1.transform, "TipYellow1", new Vector3(0, 0.55f, 0), new Vector3(0.06f, 0.35f, 0.2f), yellowCol, false);

        var tb2 = MakeCube(tailRotorGO.transform, "TailBlade_2", new Vector3(0, -0.75f, 0), new Vector3(0.05f, 1.4f, 0.18f), blackCol, false);
        MakeCube(tb2.transform, "TipYellow2", new Vector3(0, -0.55f, 0), new Vector3(0.06f, 0.35f, 0.2f), yellowCol, false);

        // ── 7. CỤM TRỤC CÁNH QUẠT CHÍNH CAO & 4 LÁ CÁNH RỘNG LỚN (MAIN ROTOR ASSEMBLY) ──
        var mastGO = new GameObject("MainRotorMast");
        mastGO.transform.SetParent(heliRoot.transform, false);
        mastGO.transform.localPosition = new Vector3(0, 3.3f, 0.2f); // Đặt cao trên nóc buồng lái

        // Trục rotor mast kim loại
        MakeCylinder(mastGO.transform, "Shaft", Vector3.zero, new Vector3(0.22f, 0.55f, 0.22f), Quaternion.identity, metalCol, false);
        // Đĩa swashplate điều khiển góc nghiêng
        MakeCylinder(mastGO.transform, "Swashplate", new Vector3(0, 0.15f, 0), new Vector3(0.7f, 0.08f, 0.7f), Quaternion.identity, blackCol, false);
        // Nắp chụp titan trung tâm (Titanium Hub Cap)
        MakeSphere(mastGO.transform, "TitaniumHubCap", new Vector3(0, 0.5f, 0), new Vector3(0.65f, 0.35f, 0.65f), metalCol, false);

        // Rotor Blades Hub (chứa 4 lá cánh quay xoay tròn)
        var rotorHub = new GameObject("RotorBladesHub");
        rotorHub.transform.SetParent(mastGO.transform, false);
        rotorHub.transform.localPosition = new Vector3(0, 0.52f, 0);

        // 4 Lá cánh quạt lớn (đường kính 11 mét) với đầu mút sơn vàng phản quang rực rỡ
        float bladeLength = 5.2f;
        Color bladeCol = new Color(0.18f, 0.2f, 0.22f);

        CreateBladeWithTip(rotorHub.transform, "Blade_North", new Vector3(0, 0,  bladeLength * 0.5f), new Vector3(0.32f, 0.04f, bladeLength), bladeCol, yellowCol, true,  1);
        CreateBladeWithTip(rotorHub.transform, "Blade_South", new Vector3(0, 0, -bladeLength * 0.5f), new Vector3(0.32f, 0.04f, bladeLength), bladeCol, yellowCol, true, -1);
        CreateBladeWithTip(rotorHub.transform, "Blade_East",  new Vector3( bladeLength * 0.5f, 0, 0), new Vector3(bladeLength, 0.04f, 0.32f), bladeCol, yellowCol, false, 1);
        CreateBladeWithTip(rotorHub.transform, "Blade_West",  new Vector3(-bladeLength * 0.5f, 0, 0), new Vector3(bladeLength, 0.04f, 0.32f), bladeCol, yellowCol, false,-1);

        // ── 8. HỆ THỐNG ĐÈN HÀNG HẢI & ĐÈN PHA RỌI SÂN BAY (AVIONICS LIGHTS) ──
        // Đèn pha tìm kiếm siêu sáng dưới mũi rọi sàn
        var searchLightGO = new GameObject("SearchLight");
        searchLightGO.transform.SetParent(heliRoot.transform, false);
        searchLightGO.transform.localPosition = new Vector3(0, 1.05f, 3.2f);
        searchLightGO.transform.localRotation = Quaternion.Euler(30f, 0, 0);
        var sl = searchLightGO.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = new Color(0.95f, 0.98f, 1f);
        sl.range = 35f;
        sl.spotAngle = 65f;
        sl.intensity = 3.5f;

        // Đèn định vị mạn trái (Cảng - Đỏ)
        var navLightL = new GameObject("NavLight_Red_L");
        navLightL.transform.SetParent(heliRoot.transform, false);
        navLightL.transform.localPosition = new Vector3(-1.35f, 2.1f, 0.2f);
        var nlLightL = navLightL.AddComponent<Light>();
        nlLightL.type = LightType.Point;
        nlLightL.color = Color.red;
        nlLightL.range = 3.5f;
        nlLightL.intensity = 2.5f;

        // Đèn định vị mạn phải (Phải - Xanh lá)
        var navLightR = new GameObject("NavLight_Green_R");
        navLightR.transform.SetParent(heliRoot.transform, false);
        navLightR.transform.localPosition = new Vector3(1.35f, 2.1f, 0.2f);
        var nlLightR = navLightR.AddComponent<Light>();
        nlLightR.type = LightType.Point;
        nlLightR.color = Color.green;
        nlLightR.range = 3.5f;
        nlLightR.intensity = 2.5f;

        // Đèn chớp Strobe trắng trên chóp đuôi máy bay
        var strobeLight = new GameObject("Tail_StrobeLight");
        strobeLight.transform.SetParent(heliRoot.transform, false);
        strobeLight.transform.localPosition = new Vector3(0, 4.35f, -8.7f);
        var strL = strobeLight.AddComponent<Light>();
        strL.type = LightType.Point;
        strL.color = Color.white;
        strL.range = 8.0f;
        strL.intensity = 4.0f;

        // Đèn cabin hắt sáng nội thất
        var cabinLight = new GameObject("Cockpit_InteriorLight");
        cabinLight.transform.SetParent(heliRoot.transform, false);
        cabinLight.transform.localPosition = new Vector3(0, 1.9f, 1.8f);
        var cl = cabinLight.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.4f, 0.85f, 1.0f);
        cl.range = 4.5f;
        cl.intensity = 2.2f;

        // ── 9. HIỆU ỨNG GIÓ BỤI & BIỂN HIỆU HƯỚNG DẪN LÊN TRỰC THĂNG ──
        var dustGO = new GameObject("DownwashDust");
        dustGO.transform.SetParent(heliRoot.transform, false);
        dustGO.transform.localPosition = new Vector3(0, -0.3f, 0);
        var ps = dustGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 7f;
        main.startSize = 0.8f;
        main.startColor = new Color(0.8f, 0.8f, 0.85f, 0.35f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        ps.Stop();

        // Biển báo neon chỉ dẫn lên máy bay đặt ngay phía trước mũi đón người chơi
        var signGO = new GameObject("HeliSign");
        signGO.transform.SetParent(heliRoot.transform, false);
        signGO.transform.localPosition = new Vector3(0, 0.5f, 4.5f);
        signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Hướng mặt về người chơi nhìn tới
        signGO.transform.localScale = Vector3.one * 0.03f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "🚁 TRỰC THĂNG CỨU HỘ KHẨN CẤP [RESCUE-01] 🚁\n► NẠP NHIÊN LIỆU & BẬT RADAR TRƯỚC KHI CẤT CÁNH! ◄";
        tm.fontSize = 38;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;

        // Tấm nền bảng chỉ dẫn
        MakeCube(signGO.transform, "SignBacking", new Vector3(0, 0, 0.05f), new Vector3(8.5f, 2.2f, 0.1f), new Color(0.08f, 0.1f, 0.14f, 0.95f), false);

        // Vùng va chạm tương tác rộng rãi (BoxCollider)
        var col = heliRoot.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 1.5f, 0);
        col.size = new Vector3(12.0f, 5.0f, 16.0f);
        col.isTrigger = true;

        // Cài đặt HelicopterController
        var ctrl = heliRoot.AddComponent<HelicopterController>();
        ctrl.SetupReferences(rotorHub.transform, tailRotorGO.transform, sl, ps);

        var so = new SerializedObject(ctrl);
        so.FindProperty("mainRotor").objectReferenceValue = rotorHub.transform;
        so.FindProperty("tailRotor").objectReferenceValue = tailRotorGO.transform;
        so.FindProperty("searchLight").objectReferenceValue = sl;
        so.FindProperty("downwashDust").objectReferenceValue = ps;
        so.FindProperty("requireFuelAndRadar").boolValue = true;
        so.FindProperty("isFueled").boolValue = false;
        so.ApplyModifiedProperties();
    }

    private static void CreateBladeWithTip(Transform parent, string name, Vector3 pos, Vector3 scale, Color bodyCol, Color tipCol, bool isAlongZ, int dir)
    {
        var blade = MakeCube(parent, name, pos, scale, bodyCol, false);
        // Vạch vàng an toàn ở đầu cánh
        Vector3 tipPos = isAlongZ 
            ? new Vector3(0, 0.01f, dir * (scale.z * 0.5f - 0.4f)) 
            : new Vector3(dir * (scale.x * 0.5f - 0.4f), 0.01f, 0);
        Vector3 tipScale = isAlongZ 
            ? new Vector3(scale.x * 1.05f, scale.y * 1.1f, 0.8f) 
            : new Vector3(0.8f, scale.y * 1.1f, scale.z * 1.05f);
        MakeCube(blade.transform, "YellowHazardTip", tipPos, tipScale, tipCol, false);
    }

    private static void BuildMilitaryRescueHelicopter(GameObject heliRoot, GameObject heliPrefab)
    {
        // 1. Instantiate mô hình 3D quân sự OH-58D Kiowa với kích thước to chuẩn quân sự thực tế (2.3x)
        var inst = (GameObject)PrefabUtility.InstantiatePrefab(heliPrefab, heliRoot.transform);
        inst.name = "Model_OH58D";
        inst.transform.localPosition = new Vector3(0, 0.05f, 0);
        inst.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        inst.transform.localScale = Vector3.one * 2.3f;

        foreach (var cam in inst.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(cam.gameObject);
        foreach (var al in inst.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(al);

        // 2. Tìm kiếm cánh quạt
        Transform mainRotorNode = null;
        Transform tailRotorNode = null;

        var childTransforms = inst.GetComponentsInChildren<Transform>(true);
        foreach (var t in childTransforms)
        {
            if (t == inst.transform) continue;
            string lower = t.name.ToLower();
            if (lower.Contains("rotar") || lower == "rotar")
                mainRotorNode = t;
            else if (lower.Contains("screw") || lower.Contains("tailrot") || lower.Contains("backrot"))
                tailRotorNode = t;
            else if (lower.Contains("rotor") || lower.Contains("blade") || lower.Contains("propeller"))
            {
                if (lower.Contains("tail") || lower.Contains("rear") || lower.Contains("back") || lower.Contains("sub"))
                    tailRotorNode = t;
                else if (mainRotorNode == null)
                    mainRotorNode = t;
            }
        }

        if (mainRotorNode == null || tailRotorNode == null)
        {
            float maxY = -999f;
            float maxZ = -999f;
            Transform highestNode = null;
            Transform backNode = null;

            foreach (var t in childTransforms)
            {
                if (t == inst.transform) continue;
                var rend = t.GetComponent<Renderer>();
                if (rend != null)
                {
                    if (t.localPosition.y > maxY)
                    {
                        maxY = t.localPosition.y;
                        highestNode = t;
                    }
                    if (Mathf.Abs(t.localPosition.z) > maxZ)
                    {
                        maxZ = Mathf.Abs(t.localPosition.z);
                        backNode = t;
                    }
                }
            }

            if (mainRotorNode == null && highestNode != null) mainRotorNode = highestNode;
            if (tailRotorNode == null && backNode != null) tailRotorNode = backNode;
        }

        // 3. Đèn pha rọi sàn
        var searchLightGO = new GameObject("SearchLight");
        searchLightGO.transform.SetParent(heliRoot.transform, false);
        searchLightGO.transform.localPosition = new Vector3(0, 1.3f, 4.0f);
        searchLightGO.transform.localRotation = Quaternion.Euler(30f, 0, 0);
        var sl = searchLightGO.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = new Color(0.95f, 0.98f, 1f);
        sl.range = 35f;
        sl.spotAngle = 65f;
        sl.intensity = 3.5f;

        // 4. Bụi gió cánh quạt
        var dustGO = new GameObject("DownwashDust");
        dustGO.transform.SetParent(heliRoot.transform, false);
        dustGO.transform.localPosition = new Vector3(0, -0.3f, 0);
        var ps = dustGO.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 1.2f;
        main.startSpeed = 7f;
        main.startSize = 0.8f;
        main.startColor = new Color(0.8f, 0.8f, 0.85f, 0.35f);
        main.maxParticles = 80;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;
        ps.Stop();

        // 5. Biển báo đón người chơi
        var signGO = new GameObject("HeliSign");
        signGO.transform.SetParent(heliRoot.transform, false);
        signGO.transform.localPosition = new Vector3(0, 0.45f, 5.8f);
        signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        signGO.transform.localScale = Vector3.one * 0.03f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "🚁 TRỰC THĂNG CỨU HỘ KHẨN CẤP [RESCUE-01] 🚁\n► NẠP NHIÊN LIỆU & BẬT RADAR TRƯỚC KHI CẤT CÁNH! ◄";
        tm.fontSize = 38;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;
        MakeCube(signGO.transform, "SignBacking", new Vector3(0, 0, 0.05f), new Vector3(8.5f, 2.2f, 0.1f), new Color(0.08f, 0.1f, 0.14f, 0.95f), false);

        // 6. BoxCollider tương tác
        var col = heliRoot.AddComponent<BoxCollider>();
        col.center = new Vector3(0, 2.2f, 0);
        col.size = new Vector3(8.0f, 5.5f, 15.0f);
        col.isTrigger = true;

        // 7. Âm thanh trực thăng
        var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Helicopter/Resources/Audio/Helicopter.wav");
        var aud = heliRoot.AddComponent<AudioSource>();
        if (audioClip != null)
        {
            aud.clip = audioClip;
            aud.loop = true;
            aud.playOnAwake = false;
            aud.spatialBlend = 1f;
            aud.minDistance = 5f;
            aud.maxDistance = 60f;
        }

        // 8. HelicopterController
        var ctrl = heliRoot.AddComponent<HelicopterController>();
        ctrl.SetupReferences(mainRotorNode, tailRotorNode, sl, ps);

        var so = new SerializedObject(ctrl);
        if (mainRotorNode != null) so.FindProperty("mainRotor").objectReferenceValue = mainRotorNode;
        if (tailRotorNode != null) so.FindProperty("tailRotor").objectReferenceValue = tailRotorNode;
        so.FindProperty("searchLight").objectReferenceValue = sl;
        so.FindProperty("downwashDust").objectReferenceValue = ps;
        so.FindProperty("requireFuelAndRadar").boolValue = true;
        so.FindProperty("isFueled").boolValue = false;
        so.ApplyModifiedProperties();
    }

    // ─── 4. THÁP ĐIỀU KHIỂN & TRẠM RADAR (CONTROL TOWER) ───────────────
    private static void BuildControlTowerAndRadar(Transform parent, Transform[] blastGates)
    {
        var root = new GameObject("ControlTower_RadarStation");
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(22f, 0, 16f);

        Color wallCol  = new Color(0.22f, 0.25f, 0.28f);
        Color glassCol = new Color(0.1f, 0.3f, 0.45f);

        // Tầng 1: Sàn, Trần & Tường với Cửa Ra Vào Rộng Rãi
        MakeCube(root.transform, "TowerFloor_Base", new Vector3(0, 0.05f, 0), new Vector3(8.4f, 0.1f, 8.4f), new Color(0.12f, 0.12f, 0.14f), true);
        MakeCube(root.transform, "TowerFloor1_Ceiling", new Vector3(0, 4.0f, 0), new Vector3(8.4f, 0.2f, 8.4f), wallCol, true);

        // Tường Bắc, Đông, Tây
        MakeCube(root.transform, "TowerWall_North", new Vector3(0, 2.0f, 4.0f), new Vector3(8.0f, 4.0f, 0.3f), wallCol, true);
        MakeCube(root.transform, "TowerWall_East",  new Vector3(4.0f, 2.0f, 0), new Vector3(0.3f, 4.0f, 8.0f), wallCol, true);
        MakeCube(root.transform, "TowerWall_West",  new Vector3(-4.0f, 2.0f, 0), new Vector3(0.3f, 4.0f, 8.0f), wallCol, true);

        // Tường Nam có lối vào mở rộng đón người chơi từ sân vào (Doorway rộng 3m)
        MakeCube(root.transform, "TowerWall_South_L", new Vector3(-2.8f, 2.0f, -4.0f), new Vector3(2.4f, 4.0f, 0.3f), wallCol, true);
        MakeCube(root.transform, "TowerWall_South_R", new Vector3( 2.8f, 2.0f, -4.0f), new Vector3(2.4f, 4.0f, 0.3f), wallCol, true);
        MakeCube(root.transform, "TowerWall_South_Top", new Vector3(0, 3.4f, -4.0f), new Vector3(3.2f, 1.2f, 0.3f), new Color(0.1f, 0.55f, 0.9f), true);

        // Biển hiệu trước cửa vào Tháp Điều Khiển
        var entranceSign = new GameObject("ControlTowerEntranceSign");
        entranceSign.transform.SetParent(root.transform, false);
        entranceSign.transform.localPosition = new Vector3(0, 3.4f, -4.2f);
        entranceSign.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        entranceSign.transform.localScale = Vector3.one * 0.025f;
        var tmEntry = entranceSign.AddComponent<TextMesh>();
        tmEntry.text = "▲ THÁP ĐIỀU KHIỂN & TRẠM RADAR ▲\n[ CONTROL TOWER & RADAR STATION ]\n◄ LỐI VÀO PHÒNG ĐIỀU HÀNH KHÔNG LƯU ►";
        tmEntry.fontSize = 32;
        tmEntry.alignment = TextAlignment.Center;
        tmEntry.anchor = TextAnchor.MiddleCenter;
        tmEntry.color = Color.cyan;

        // Đèn chiếu sáng bên trong phòng điều hành
        var interiorLightGO = new GameObject("TowerInteriorLight");
        interiorLightGO.transform.SetParent(root.transform, false);
        interiorLightGO.transform.localPosition = new Vector3(0, 3.2f, 0);
        var inLight = interiorLightGO.AddComponent<Light>();
        inLight.type = LightType.Point;
        inLight.color = new Color(0.3f, 0.7f, 1.0f);
        inLight.intensity = 2.0f;
        inLight.range = 8.0f;

        // Tầng 2: Phòng điều hành kính quan sát (7m x 3.5m x 7m)
        MakeCube(root.transform, "TowerFloor2_Core", new Vector3(0, 5.5f, 0), new Vector3(5f, 3f, 5f), wallCol, true);
        MakeCube(root.transform, "TowerFloor2_GlassN", new Vector3(0, 5.5f, 3f), new Vector3(6.5f, 2.5f, 0.3f), glassCol, true);
        MakeCube(root.transform, "TowerFloor2_GlassW", new Vector3(-3f, 5.5f, 0), new Vector3(0.3f, 2.5f, 6.5f), glassCol, true);
        MakeCube(root.transform, "TowerRoof", new Vector3(0, 7.2f, 0), new Vector3(8f, 0.4f, 8f), wallCol * 0.8f, true);

        // Cầu thang leo lên tầng 2
        MakeCube(root.transform, "StairsRamp", new Vector3(-4.8f, 2.5f, 0), new Vector3(1.4f, 5f, 6f), new Color(0.15f, 0.16f, 0.18f), true);

        // TRẠM RADAR TRÊN MÁI THÁP (RADAR MAST & DISH)
        var mast = MakeCube(root.transform, "RadarMast", new Vector3(0, 8.5f, 0), new Vector3(0.5f, 2.5f, 0.5f), Color.gray, true);

        var dishHub = new GameObject("RadarDishHub");
        dishHub.transform.SetParent(root.transform, false);
        dishHub.transform.localPosition = new Vector3(0, 9.8f, 0);

        // Đĩa radar hình bán cầu cong
        var dish = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        dish.name = "RadarDish";
        dish.transform.SetParent(dishHub.transform, false);
        dish.transform.localPosition = new Vector3(0, 0, 0.4f);
        dish.transform.localRotation = Quaternion.Euler(60f, 0, 0);
        dish.transform.localScale = new Vector3(2.5f, 0.15f, 2.5f);
        SetColor(dish, new Color(0.9f, 0.9f, 0.95f));

        var dishAntenna = MakeCube(dishHub.transform, "AntennaFeed", new Vector3(0, 0.2f, 1.2f), new Vector3(0.15f, 0.15f, 1.2f), Color.yellow, false);

        // BÀN ĐIỀU KHIỂN RADAR CONSOLE (ĐẶT BÊN TRONG TẦNG 1 ĐỐI DIỆN CỬA VÀO)
        var consoleGO = new GameObject("RadarControlConsole");
        consoleGO.transform.SetParent(root.transform, false);
        consoleGO.transform.localPosition = new Vector3(0, 0.6f, 1.5f);
        consoleGO.transform.localRotation = Quaternion.identity;

        int intLayer = LayerMask.NameToLayer("Interactable");
        consoleGO.layer = intLayer >= 0 ? intLayer : 0;

        // Bàn máy tính
        MakeCube(consoleGO.transform, "DeskBody", Vector3.zero, new Vector3(2.2f, 1.2f, 0.9f), new Color(0.12f, 0.14f, 0.16f), false);
        var screen = MakeCube(consoleGO.transform, "RadarScreen", new Vector3(0, 0.7f, -0.1f), new Vector3(1.5f, 0.8f, 0.15f), new Color(0.05f, 0.15f, 0.2f), false);

        // Đèn trạng thái trên console
        var lGO = new GameObject("ConsoleScreenLight");
        lGO.transform.SetParent(consoleGO.transform, false);
        lGO.transform.localPosition = new Vector3(0, 0.8f, -0.25f);
        var scrLight = lGO.AddComponent<Light>();
        scrLight.type = LightType.Point;
        scrLight.color = Color.red;
        scrLight.intensity = 2.0f;
        scrLight.range = 4f;

        // Chữ chỉ dẫn
        var lblGO = new GameObject("ConsoleSign");
        lblGO.transform.SetParent(consoleGO.transform, false);
        lblGO.transform.localPosition = new Vector3(0, 1.35f, -0.1f);
        lblGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        lblGO.transform.localScale = Vector3.one * 0.022f;
        var tm = lblGO.AddComponent<TextMesh>();
        tm.text = "[ BÀN ĐIỀU KHIỂN RADAR KHÔNG LƯU ]\n► Bấm [E] Để Kích Hoạt Dẫn Đường Trực Thăng ◄";
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.cyan;

        var col = consoleGO.AddComponent<BoxCollider>();
        col.size = new Vector3(2.6f, 2.0f, 2.0f);
        col.isTrigger = true;

        var radarConsole = consoleGO.AddComponent<HelipadRadarConsole>();
        radarConsole.SetupReferences(scrLight, dishHub.transform, blastGates);

        var so = new SerializedObject(radarConsole);
        so.FindProperty("screenLight").objectReferenceValue = scrLight;
        so.FindProperty("radarDishTransform").objectReferenceValue = dishHub.transform;
        var gatesProp = so.FindProperty("blastGateDoors");
        gatesProp.arraySize = blastGates.Length;
        for (int i = 0; i < blastGates.Length; i++)
            gatesProp.GetArrayElementAtIndex(i).objectReferenceValue = blastGates[i];
        so.ApplyModifiedProperties();
    }

    // ─── 5. KHO TIẾP LIỆU & BÌNH NHIÊN LIỆU PHẢN LỰC (FUEL DEPOT) ───────
    private static void BuildFuelDepot(Transform parent)
    {
        var root = new GameObject("FuelDepot");
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(-22f, 0, 16f);

        Color bunkerCol = new Color(0.24f, 0.26f, 0.28f);

        // Nhà kho chứa nhiên liệu: 10m x 4.5m x 10m
        MakeCube(root.transform, "DepotFloor", new Vector3(0, 0.05f, 0), new Vector3(10f, 0.1f, 10f), new Color(0.12f, 0.12f, 0.14f), true);
        MakeCube(root.transform, "DepotRoof",  new Vector3(0, 4.5f, 0), new Vector3(10.5f, 0.3f, 10.5f), bunkerCol * 0.8f, true);

        // Tường kho
        MakeCube(root.transform, "Wall_North", new Vector3(0, 2.2f, 5f), new Vector3(10f, 4.4f, 0.4f), bunkerCol, true);
        MakeCube(root.transform, "Wall_West",  new Vector3(-5f, 2.2f, 0), new Vector3(0.4f, 4.4f, 10f), bunkerCol, true);
        MakeCube(root.transform, "Wall_East",  new Vector3(5f, 2.2f, 0), new Vector3(0.4f, 4.4f, 10f), bunkerCol, true);

        // Tường Nam có cửa mở rộng vào kho
        MakeCube(root.transform, "Wall_South_L", new Vector3(-3.2f, 2.2f, -5f), new Vector3(3.6f, 4.4f, 0.4f), bunkerCol, true);
        MakeCube(root.transform, "Wall_South_R", new Vector3( 3.2f, 2.2f, -5f), new Vector3(3.6f, 4.4f, 0.4f), bunkerCol, true);
        MakeCube(root.transform, "Wall_South_H", new Vector3(0, 3.8f, -5f), new Vector3(3.0f, 1.2f, 0.4f), new Color(0.9f, 0.4f, 0.1f), true);

        // Biển hiệu Kho Nhiên Liệu
        var signGO = new GameObject("FuelSign");
        signGO.transform.SetParent(root.transform, false);
        signGO.transform.localPosition = new Vector3(0, 3.6f, -5.3f);
        signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        signGO.transform.localScale = Vector3.one * 0.03f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "▲ KHO NHIÊN LIỆU PHẢN LỰC ▲\n[ JET FUEL DEPOT ]";
        tm.fontSize = 36;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;

        // Các thùng phuy dầu trang trí
        Color drumCol = new Color(0.8f, 0.2f, 0.15f);
        var d1 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d1.name = "FuelDrum_1"; d1.transform.SetParent(root.transform);
        d1.transform.localPosition = new Vector3(-3f, 0.8f, 3f);
        d1.transform.localScale = new Vector3(1.0f, 0.8f, 1.0f);
        SetColor(d1, drumCol);

        var d2 = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        d2.name = "FuelDrum_2"; d2.transform.SetParent(root.transform);
        d2.transform.localPosition = new Vector3(-3f, 0.8f, 1.5f);
        d2.transform.localScale = new Vector3(1.0f, 0.8f, 1.0f);
        SetColor(d2, drumCol);

        // Đặt BÌNH NHIÊN LIỆU TRỰC THĂNG (Item_JetFuel) trên bàn trong kho
        var table = MakeCube(root.transform, "FuelWorkbench", new Vector3(2.5f, 0.5f, 2.5f), new Vector3(1.6f, 1.0f, 2.4f), new Color(0.2f, 0.22f, 0.25f), true);

        CreateItemPickup(root.transform, "Item_JetFuel",
            PickupItem.ItemType.JetFuel,
            "Bình Nhiên Liệu Phản Lực (Jet Fuel)",
            "ĐÃ NHẶT: Bình Nhiên Liệu Phản Lực! Mang đến Trực Thăng tại trung tâm sân đỗ để tiếp xăng.",
            root.transform.position + new Vector3(2.5f, 1.3f, 2.5f),
            new Color(1f, 0.5f, 0.05f),
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 0.45f, 0.35f));

        // Hộp Cứu Thương Khẩn Cấp (+50 HP)
        CreateItemPickup(root.transform, "Item_Medkit_3",
            PickupItem.ItemType.Medkit,
            "Hộp Cứu Thương Tầng Thượng (+50 HP)",
            "ĐÃ NHẶT: Hộp Cứu Thương! Phục hồi 50 HP.",
            root.transform.position + new Vector3(-2f, 0.6f, -2f),
            new Color(0.2f, 0.7f, 1f),
            PrimitiveType.Cube,
            new Vector3(0.5f, 0.35f, 0.5f));
    }

    // ─── 6. BUỒNG THANG MÁY ĐẾN TỪ MÀN 2 (ELEVATOR ARRIVAL) ────────────
    private static void BuildElevatorArrival(Transform parent)
    {
        var root = new GameObject("ElevatorArrivalShaft");
        root.transform.SetParent(parent);
        root.transform.position = new Vector3(0, 0, -26f);

        Color elevCol = new Color(0.22f, 0.24f, 0.28f);

        // Buồng thang máy: 6m x 5m x 6m
        MakeCube(root.transform, "Elev_Floor", new Vector3(0, -0.1f, 0), new Vector3(6f, 0.2f, 6f), new Color(0.28f, 0.3f, 0.35f), false);
        MakeCube(root.transform, "Elev_Ceiling", new Vector3(0, 4.8f, 0), new Vector3(6f, 0.2f, 6f), elevCol * 0.7f, false);
        MakeCube(root.transform, "Elev_WallL", new Vector3(-3f, 2.4f, 0), new Vector3(0.4f, 4.8f, 6f), elevCol, false);
        MakeCube(root.transform, "Elev_WallR", new Vector3( 3f, 2.4f, 0), new Vector3(0.4f, 4.8f, 6f), elevCol, false);
        MakeCube(root.transform, "Elev_WallBack", new Vector3(0, 2.4f, -3f), new Vector3(6f, 4.8f, 0.4f), elevCol, false);

        // Cửa mở sẵn hướng ra sân đỗ phía Bắc
        MakeCube(root.transform, "Elev_DoorArch", new Vector3(0, 4.2f, 3f), new Vector3(6f, 1.2f, 0.4f), new Color(0.85f, 0.65f, 0.1f), false);

        // Đèn trần thang máy
        var lGO = new GameObject("ElevatorLight");
        lGO.transform.SetParent(root.transform, false);
        lGO.transform.localPosition = new Vector3(0, 4.2f, 0);
        var el = lGO.AddComponent<Light>();
        el.type = LightType.Point;
        el.color = new Color(0.3f, 0.8f, 1f);
        el.range = 8f;
        el.intensity = 1.6f;

        // Biển chỉ dẫn
        var signGO = new GameObject("ExitSign");
        signGO.transform.SetParent(root.transform, false);
        signGO.transform.localPosition = new Vector3(0, 4.3f, 3.25f);
        signGO.transform.localScale = Vector3.one * 0.03f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "▲ LỐI RA SÂN ĐỖ TRỰC THĂNG ▲\n[ HELIPAD ROOFTOP ]";
        tm.fontSize = 36;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.green;
    }

    // ─── 7. HỆ THỐNG ÁNH SÁNG ĐÊM TẦNG THƯỢNG ──────────────────────────
    private static void SetupRooftopLighting(Transform parent)
    {
        RenderSettings.ambientMode  = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.55f); // Đêm công nghiệp sáng rõ

        var lRoot = new GameObject("RooftopFloodlights");
        lRoot.transform.SetParent(parent);

        // 4 Cột đèn pha cao áp rọi sáng sân đỗ và nóc nhà từ 4 góc
        Vector3[] polePositions = new Vector3[] {
            new Vector3(-24f, 0, -10f),
            new Vector3( 24f, 0, -10f),
            new Vector3( 24f, 0,  24f),
            new Vector3(-24f, 0,  24f)
        };

        for (int i = 0; i < 4; i++)
        {
            var pole = MakeCube(lRoot.transform, $"FloodPole_{i+1}", polePositions[i] + Vector3.up * 5f, new Vector3(0.4f, 10f, 0.4f), Color.gray, true);
            var fLightGO = new GameObject($"Floodlight_{i+1}");
            fLightGO.transform.SetParent(pole.transform, false);
            fLightGO.transform.localPosition = new Vector3(0, 5f, 0);
            fLightGO.transform.LookAt(new Vector3(-3.5f, 5.5f, 6.5f)); // Rọi vào trực thăng trên nóc nhà
            var fl = fLightGO.AddComponent<Light>();
            fl.type = LightType.Spot;
            fl.color = new Color(0.85f, 0.92f, 1f);
            fl.range = 80f;
            fl.spotAngle = 75f;
            fl.intensity = 75f;
        }

        // Đèn mặt trăng dịu nhẹ toàn cảnh
        var moonGO = new GameObject("MoonDirectionalLight");
        moonGO.transform.SetParent(lRoot.transform, false);
        moonGO.transform.position = new Vector3(0, 40f, 0);
        moonGO.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        var ml = moonGO.AddComponent<Light>();
        ml.type = LightType.Directional;
        ml.color = new Color(0.7f, 0.82f, 1.0f);
        ml.intensity = 1.35f;

        EscapeTheLab.EditorTools.AssetUpgradeTools.BrightenHelipadLightingInternal(EditorSceneManager.GetActiveScene());
    }

    // ─── 8. NGƯỜI CHƠI & 2 ROBOT TUẦN TRA TRANG BỊ SÚNG ────────────────
    private static void SetupPlayerAndTwoRobots(Transform parent)
    {
        // 1. Player
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null) player = GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            SetColor(player, new Color(0.2f, 0.4f, 0.8f));
        }

        int pLayer = LayerMask.NameToLayer("Player");
        if (pLayer >= 0) player.layer = pLayer;

        // Xuất phát bên trong buồng thang máy nhìn ra sân và cầu thang nóc nhà
        player.transform.position = new Vector3(0, 1.1f, -24f);
        player.transform.rotation = Quaternion.identity;

        var capCol = player.GetComponent<CapsuleCollider>();
        if (capCol != null) Object.DestroyImmediate(capCol);

        var rbOld = player.GetComponent<Rigidbody>();
        if (rbOld != null) Object.DestroyImmediate(rbOld);

        CharacterController cc = player.GetComponent<CharacterController>();
        if (cc == null) cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0, 0.9f, 0);
        cc.skinWidth = 0.05f;
        cc.stepOffset = 0.35f;
        cc.minMoveDistance = 0f;

        if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();

        var pi = player.GetComponent<PlayerInteraction>();
        if (pi == null) pi = player.AddComponent<PlayerInteraction>();
        var soPI = new SerializedObject(pi);
        soPI.FindProperty("interactRange").floatValue = 3.8f;
        soPI.ApplyModifiedProperties();

        PlayerController pc = player.GetComponent<PlayerController>();
        if (pc == null) pc = player.AddComponent<PlayerController>();

        // MobileInputController
        if (Object.FindFirstObjectByType<MobileInputController>() == null)
        {
            var micGO = new GameObject("MobileInputController");
            micGO.transform.SetParent(parent);
            micGO.AddComponent<MobileInputController>();
        }

        // First Person Camera
        var oldRig = GameObject.Find("CameraRig_PUBG");
        if (oldRig != null) Object.DestroyImmediate(oldRig);

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

        var fpc = cam.GetComponent<FirstPersonCamera>();
        if (fpc == null) fpc = cam.gameObject.AddComponent<FirstPersonCamera>();
        fpc.enabled = true;
        fpc.SetPlayerBody(player.transform);

        var rend = player.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        var cmm = player.GetComponent<CameraModeManager>();
        if (cmm == null) cmm = player.AddComponent<CameraModeManager>();

        var soPC = new SerializedObject(pc);
        soPC.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        soPC.ApplyModifiedProperties();

        // 2. ROBOT_1: Tuần tra toàn diện nửa Tây bản đồ & Kho Tiếp Liệu
        Vector3[] wpRobot1 = new Vector3[] {
            new Vector3(-10f, 0.2f, -18f), // Sân trước gần lối ra thang máy
            new Vector3(-20f, 0.2f, -14f), // Lối vào hành lang Tây
            new Vector3(-22f, 0.2f,  -2f), // Giữa hành lang Tây
            new Vector3(-22f, 0.2f,   9f), // Trước cửa Kho Tiếp Liệu (Fuel Depot)
            new Vector3(-18f, 0.2f,  18f), // Vành đai Kho Tiếp Liệu
            new Vector3(-16f, 0.2f,  26f), // Góc Tây Bắc sau tòa nhà
            new Vector3( -5f, 0.2f,  26f), // Hành lang phía sau tòa nhà
            new Vector3(-15f, 0.2f,  12f), // Lối đi phụ phía Tây
            new Vector3(-15f, 0.2f,  -5f), // Quay lại hành lang Tây
            new Vector3( -4f, 0.2f, -14f)  // Quét qua mặt tiền chân cầu thang
        };
        CreateShootingRobot(parent, "Robot_1", new Vector3(-10f, 1.0f, -18f), wpRobot1, new Color(0.95f, 0.2f, 0.15f), Color.green);

        // 3. ROBOT_2: Tuần tra toàn diện nửa Đông bản đồ & Tháp Điều Khiển
        Vector3[] wpRobot2 = new Vector3[] {
            new Vector3( 10f, 0.2f, -18f), // Sân trước gần lối ra thang máy
            new Vector3( 20f, 0.2f, -14f), // Lối vào hành lang Đông
            new Vector3( 22f, 0.2f,  -2f), // Giữa hành lang Đông
            new Vector3( 22f, 0.2f,   9f), // Trước cửa Tháp Điều Khiển (Control Tower)
            new Vector3( 18f, 0.2f,  18f), // Vành đai Tháp Điều Khiển
            new Vector3( 16f, 0.2f,  26f), // Góc Đông Bắc sau tòa nhà
            new Vector3(  5f, 0.2f,  26f), // Hành lang phía sau tòa nhà
            new Vector3( 15f, 0.2f,  12f), // Lối đi phụ phía Đông
            new Vector3( 15f, 0.2f,  -5f), // Quay lại hành lang Đông
            new Vector3(  4f, 0.2f, -14f)  // Quét qua mặt tiền chân cầu thang
        };
        CreateShootingRobot(parent, "Robot_2", new Vector3(10f, 1.0f, -18f), wpRobot2, new Color(0.95f, 0.45f, 0.1f), Color.green);

        // 4. CÁC VẬT PHẨM HỖ TRỢ TRÊN SÂN & CẦU THANG (DỄ THẤY & NHẶT TỰ ĐỘNG HOẶC PHÍM [E])
        CreateItemPickup(parent, "Item_Courtyard_Medkit",
            PickupItem.ItemType.Medkit,
            "Túi Cứu Thương Mặt Sân (+45 HP)",
            "ĐÃ NHẶT: Túi Cứu Thương! Hồi phục 45 HP.",
            new Vector3(0, 0.5f, -18.5f),
            new Color(0.2f, 0.9f, 0.3f),
            PrimitiveType.Cube,
            new Vector3(0.5f, 0.35f, 0.5f));

        CreateItemPickup(parent, "Item_Courtyard_Battery",
            PickupItem.ItemType.Battery,
            "Pin Năng Lượng Shield (+30 Giáp)",
            "ĐÃ NHẶT: Pin Năng Lượng Shield! Gia tăng phòng ngự trước súng laser robot.",
            new Vector3(-4f, 0.5f, -13f),
            new Color(0.2f, 0.65f, 1f),
            PrimitiveType.Cylinder,
            new Vector3(0.35f, 0.45f, 0.35f));

        CreateItemPickup(parent, "Item_Stairs_Medkit",
            PickupItem.ItemType.Medkit,
            "Hộp Thuốc Tiếp Sức Trên Cầu Thang (+35 HP)",
            "ĐÃ NHẶT: Hộp Thuốc Tiếp Sức! Hồi phục 35 HP.",
            new Vector3(0f, 5.3f, -4.5f),
            new Color(0.15f, 0.85f, 0.45f),
            PrimitiveType.Cube,
            new Vector3(0.5f, 0.35f, 0.5f));
    }

    private static void CreateShootingRobot(Transform parent, string robotName, Vector3 spawnPos, Vector3[] wpPositions, Color robotColor, Color sensorColor)
    {
        GameObject robot = GameObject.Find(robotName);
        if (robot == null)
        {
            robot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            robot.name = robotName;
            robot.tag = "Enemy";
        }
        robot.transform.position = new Vector3(spawnPos.x, 1.0f, spawnPos.z);
        SetColor(robot, robotColor);

        var agent = robot.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent == null) agent = robot.AddComponent<UnityEngine.AI.NavMeshAgent>();
        agent.speed = 3.4f;
        agent.angularSpeed = 160f;
        agent.acceleration = 10f;
        agent.stoppingDistance = 0.5f;

        // Đèn rọi cảm biến nghiêng 20 độ rọi sàn
        var sensor = robot.transform.Find("SensorLight")?.gameObject;
        if (sensor == null)
        {
            sensor = new GameObject("SensorLight");
            sensor.transform.SetParent(robot.transform);
        }
        sensor.transform.localPosition = new Vector3(0, 1.5f, 0.4f);
        sensor.transform.localRotation = Quaternion.Euler(20f, 0, 0);
        var sl = sensor.GetComponent<Light>();
        if (sl == null) sl = sensor.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = sensorColor;
        sl.intensity = 5.0f;
        sl.range = 16f;
        sl.spotAngle = 75f;

        // Mắt kính quét & ăng-ten radar
        MakeCube(robot.transform, "RobotVisor", new Vector3(0, 1.5f, 0.45f), new Vector3(0.55f, 0.2f, 0.2f), new Color(0.1f, 1.0f, 0.4f), false);
        MakeCube(robot.transform, "HeadRadarMast", new Vector3(0, 2.1f, 0), new Vector3(0.08f, 0.4f, 0.08f), Color.gray, false);
        MakeCube(robot.transform, "HeadRadarDish", new Vector3(0, 2.3f, 0), new Vector3(0.6f, 0.1f, 0.35f), Color.yellow, false);

        // Vùng VisionCone quét sàn (fan radar mesh)
        var coneGO = robot.transform.Find("VisionCone_RadarSweep")?.gameObject;
        if (coneGO == null)
        {
            coneGO = new GameObject("VisionCone_RadarSweep");
            coneGO.transform.SetParent(robot.transform, false);
        }
        coneGO.transform.localPosition = Vector3.zero;
        coneGO.transform.localRotation = Quaternion.identity;
        var rvc = coneGO.GetComponent<RobotVisionCone>();
        if (rvc == null) rvc = coneGO.AddComponent<RobotVisionCone>();

        // EyePoint và RobotDetection
        var eyeGO = robot.transform.Find("EyePoint")?.gameObject;
        if (eyeGO == null)
        {
            eyeGO = new GameObject("EyePoint");
            eyeGO.transform.SetParent(robot.transform);
        }
        eyeGO.transform.localPosition = new Vector3(0, 1.5f, 0.4f);

        var det = robot.GetComponent<RobotDetection>();
        if (det == null) det = robot.AddComponent<RobotDetection>();
        var soDet = new SerializedObject(det);
        soDet.FindProperty("eyePoint").objectReferenceValue = eyeGO.transform;
        int plLayer = LayerMask.NameToLayer("Player");
        if (plLayer >= 0) soDet.FindProperty("playerMask").intValue = 1 << plLayer;
        int envLayer = LayerMask.NameToLayer("Environment");
        int gndLayer = LayerMask.NameToLayer("Ground");
        int obs = 0;
        if (envLayer >= 0) obs |= 1 << envLayer;
        if (gndLayer >= 0) obs |= 1 << gndLayer;
        if (obs == 0) obs = 1;
        soDet.FindProperty("obstacleMask").intValue = obs;
        soDet.FindProperty("detectionRange").floatValue = 16f;
        soDet.FindProperty("fieldOfView").floatValue = 75f;
        soDet.ApplyModifiedProperties();

        // KHẨU SÚNG BẮN TIA LASER TRANG BỊ CHO ROBOT (BLASTER GUN MODEL & LASER)
        var gunRoot = robot.transform.Find("RobotBlasterGun")?.gameObject;
        if (gunRoot != null) Object.DestroyImmediate(gunRoot);
        gunRoot = new GameObject("RobotBlasterGun");
        gunRoot.transform.SetParent(robot.transform, false);
        gunRoot.transform.localPosition = new Vector3(0.45f, 1.25f, 0.35f);

        // Khối súng
        MakeCube(gunRoot.transform, "GunBody", new Vector3(0, 0, 0.15f), new Vector3(0.14f, 0.16f, 0.5f), new Color(0.15f, 0.16f, 0.18f), false);
        MakeCube(gunRoot.transform, "GunBarrel", new Vector3(0, 0.02f, 0.5f), new Vector3(0.08f, 0.08f, 0.3f), new Color(0.08f, 0.08f, 0.08f), false);
        MakeCube(gunRoot.transform, "GunScope", new Vector3(0, 0.12f, 0.15f), new Vector3(0.06f, 0.06f, 0.35f), Color.red, false);

        var muzzleGO = new GameObject("Muzzle");
        muzzleGO.transform.SetParent(gunRoot.transform, false);
        muzzleGO.transform.localPosition = new Vector3(0, 0.02f, 0.7f);

        // Đèn chớp đầu nòng (Muzzle Flash)
        var flashGO = new GameObject("MuzzleFlashLight");
        flashGO.transform.SetParent(muzzleGO.transform, false);
        flashGO.transform.localPosition = Vector3.zero;
        var flashLight = flashGO.AddComponent<Light>();
        flashLight.type = LightType.Point;
        flashLight.color = new Color(1f, 0.25f, 0.1f);
        flashLight.intensity = 5.0f;
        flashLight.range = 4f;
        flashLight.enabled = false;

        // Tia đạn laser (LineRenderer)
        var laserGO = new GameObject("LaserBeamLine");
        laserGO.transform.SetParent(gunRoot.transform, false);
        var lineRend = laserGO.AddComponent<LineRenderer>();
        lineRend.startWidth = 0.08f;
        lineRend.endWidth = 0.08f;
        lineRend.positionCount = 2;
        lineRend.useWorldSpace = true;
        lineRend.enabled = false;

        var laserMat = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
        laserMat.color = new Color(1f, 0.15f, 0.1f);
        lineRend.material = laserMat;
        lineRend.startColor = new Color(1f, 0.3f, 0.1f);
        lineRend.endColor = new Color(1f, 0.1f, 0.05f);

        // Waypoints tuần tra cho robot này
        string wpRootName = $"Waypoints_{robotName}";
        var oldWp = GameObject.Find(wpRootName);
        if (oldWp != null) Object.DestroyImmediate(oldWp);

        var wpRoot = new GameObject(wpRootName);
        wpRoot.transform.SetParent(parent);
        Transform[] wps = new Transform[wpPositions.Length];
        for (int i = 0; i < wpPositions.Length; i++)
        {
            var wp = new GameObject($"WP_{i + 1}");
            wp.transform.SetParent(wpRoot.transform);
            wp.transform.position = wpPositions[i];
            wps[i] = wp.transform;
        }

        // Cấu hình RobotAI
        var ai = robot.GetComponent<RobotAI>();
        if (ai == null) ai = robot.AddComponent<RobotAI>();
        ai.SetupGun(muzzleGO.transform, lineRend, flashLight);

        var soAI = new SerializedObject(ai);
        soAI.FindProperty("sensorLight").objectReferenceValue = sl;
        soAI.FindProperty("visionCone").objectReferenceValue = rvc;
        soAI.FindProperty("canShoot").boolValue = true;
        soAI.FindProperty("shootRange").floatValue = 18f;
        soAI.FindProperty("shootCooldown").floatValue = 1.2f;
        soAI.FindProperty("shootDamage").intValue = 15;
        soAI.FindProperty("patrolSpeed").floatValue = 3.2f;
        soAI.FindProperty("chaseSpeed").floatValue = 4.8f;
        soAI.FindProperty("waypointWaitTime").floatValue = 0.8f;
        soAI.FindProperty("gunMuzzle").objectReferenceValue = muzzleGO.transform;
        soAI.FindProperty("laserBeamLine").objectReferenceValue = lineRend;
        soAI.FindProperty("muzzleLight").objectReferenceValue = flashLight;
        soAI.FindProperty("shootObstacleMask").intValue = obs;

        var wpProp = soAI.FindProperty("waypoints");
        wpProp.arraySize = wps.Length;
        for (int i = 0; i < wps.Length; i++)
            wpProp.GetArrayElementAtIndex(i).objectReferenceValue = wps[i];
        soAI.ApplyModifiedProperties();
    }

    // ─── 9. CANVAS UI & VICTORY SCREEN ────────────────────────────────
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
        slR.anchorMin = new Vector2(0f, 0f); slR.anchorMax = new Vector2(1f, 0f);
        slR.pivot = new Vector2(0.5f, 0f); slR.anchoredPosition = new Vector2(0, 10);
        slR.sizeDelta = new Vector2(-24, 20);

        var bgImg = slGO.AddComponent<UnityEngine.UI.Image>();
        bgImg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);

        var faGO = new GameObject("FillArea");
        faGO.transform.SetParent(slGO.transform, false);
        var faR = faGO.AddComponent<RectTransform>();
        faR.anchorMin = Vector2.zero; faR.anchorMax = Vector2.one;
        faR.offsetMin = new Vector2(2, 2); faR.offsetMax = new Vector2(-2, -2);

        var fiGO = new GameObject("Fill");
        fiGO.transform.SetParent(faGO.transform, false);
        var fiR = fiGO.AddComponent<RectTransform>();
        fiR.anchorMin = Vector2.zero; fiR.anchorMax = Vector2.one;
        var fiImg = fiGO.AddComponent<UnityEngine.UI.Image>();
        fiImg.color = new Color(0.2f, 0.92f, 0.38f);
        sl.fillRect = fiR; sl.value = 1f;

        // Objective Panel
        var objPnl = MakeUIPanel(cvGO.transform, "ObjectivePanel", new Vector2(1,1), new Vector2(1,1), new Vector2(-15,-15), new Vector2(360,105));
        var objTxt = MakeUIText(objPnl.transform, "ObjectiveText", "NHIỆM VỤ TẦNG THƯỢNG\nMàn 3: Thoát Hiểm", 15, Vector2.zero);

        // Notification Panel
        var notifPnl = MakeUIPanel(cvGO.transform, "NotificationPanel", new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0,120), new Vector2(580,55));
        var notifTxt = MakeUIText(notifPnl.transform, "NotifText", "Notification", 18, Vector2.zero);
        notifPnl.SetActive(false);

        // Interaction Prompt
        var promptPnl = MakeUIPanel(cvGO.transform, "InteractionPrompt", new Vector2(0.5f,0), new Vector2(0.5f,0), new Vector2(0,75), new Vector2(440,45));
        promptPnl.GetComponent<UnityEngine.UI.Image>().raycastTarget = false;
        var promptTxt = MakeUIText(promptPnl.transform, "PromptText", "Press E to Interact", 18, Vector2.zero);
        promptTxt.GetComponent<UnityEngine.UI.Text>().raycastTarget = false;
        promptPnl.SetActive(false);

        // GRAND VICTORY PANEL (Màn hình Chiến Thắng Toàn Bộ Game)
        var winPnl = MakeUIPanel(cvGO.transform, "WinPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(680,440));
        winPnl.GetComponent<UnityEngine.UI.Image>().color = new Color(0.04f, 0.07f, 0.12f, 0.96f);

        MakeUIText(winPnl.transform, "WinTitle", "🎉 CHIẾN THẮNG TUYỆT ĐỐI! 🎉\nBẠN ĐÃ THOÁT KHỎI KHU NGHIÊN CỨU!", 26, new Vector2(0,120));
        MakeUIText(winPnl.transform, "WinDesc", "Chúc mừng! Bạn đã hoàn thành xuất sắc tất cả 3 Màn chơi:\n• Màn 1: Thoát khỏi Phòng Thí Nghiệm\n• Màn 2: Vượt qua Lò Phản Ứng & Thang Máy\n• Màn 3: Tiếp xăng & Cất cánh Trực Thăng Tầng Thượng!", 16, new Vector2(0,10));

        var paBtn = MakeUIButton(winPnl.transform, "NextBtn", "[ CHƠI LẠI TỪ MÀN 1 ]", new Vector2(0,-85), new Vector2(280, 52));
        winPnl.SetActive(false);

        // Game Over Panel
        var goPnl = MakeUIPanel(cvGO.transform, "GameOverPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(600,380));
        MakeUIText(goPnl.transform, "GOTitle", "BỊ BẮT!", 36, new Vector2(0,120));
        var retBtn = MakeUIButton(goPnl.transform, "RetryBtn", "[ THỬ LẠI ]", new Vector2(0,-60));
        goPnl.SetActive(false);

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

        // ── Action Buttons Area (Góc dưới bên phải - Phím E nhặt đồ, JUMP, RUN) ──
        GameObject btnArea = new GameObject("ButtonsArea");
        btnArea.transform.SetParent(cvGO.transform, false);
        RectTransform btnAreaRect = btnArea.AddComponent<RectTransform>();
        btnAreaRect.anchorMin = new Vector2(1, 0);
        btnAreaRect.anchorMax = new Vector2(1, 0);
        btnAreaRect.pivot = new Vector2(1, 0);
        btnAreaRect.anchoredPosition = new Vector2(-25, 25);
        btnAreaRect.sizeDelta = new Vector2(360, 260);

        // Nút tròn [ E ] to nổi bật trên màn hình để nhặt đồ & lên trực thăng
        GameObject interactBtn = CreateMobileCircleButton(btnArea.transform, "InteractButton", "E", new Vector2(-190, 140), new Vector2(105, 105), new Color(0.15f, 0.55f, 0.95f, 0.9f));
        GameObject jumpBtn = CreateMobileCircleButton(btnArea.transform, "JumpButton", "JUMP", new Vector2(-65, 140), new Vector2(90, 90), new Color(0.2f, 0.6f, 0.3f, 0.85f));
        GameObject runBtn = CreateMobileCircleButton(btnArea.transform, "RunButton", "RUN", new Vector2(-130, 45), new Vector2(90, 90), new Color(0.85f, 0.45f, 0.1f, 0.85f));

        // Kết nối nút vào MobileInputController
        var mic = Object.FindFirstObjectByType<MobileInputController>();
        if (mic == null)
        {
            var micGO = new GameObject("MobileInputController");
            micGO.transform.SetParent(parent);
            mic = micGO.AddComponent<MobileInputController>();
        }
        var soMIC = new SerializedObject(mic);
        soMIC.FindProperty("interactButton").objectReferenceValue = interactBtn.GetComponent<UnityEngine.UI.Button>();
        soMIC.FindProperty("jumpButton").objectReferenceValue = jumpBtn.GetComponent<UnityEngine.UI.Button>();
        soMIC.FindProperty("runButton").objectReferenceValue = runBtn.GetComponent<UnityEngine.UI.Button>();
        soMIC.ApplyModifiedProperties();

        // Kết nối Interaction Prompt UI vào PlayerInteraction
        var pi = Object.FindFirstObjectByType<PlayerInteraction>();
        if (pi != null)
        {
            var soPI = new SerializedObject(pi);
            soPI.FindProperty("interactionPromptUI").objectReferenceValue = promptPnl;
            soPI.ApplyModifiedProperties();
        }

        if (Object.FindFirstObjectByType<ObjectiveManager>() == null)
        {
            var om = new GameObject("ObjectiveManager");
            om.transform.SetParent(parent);
            om.AddComponent<ObjectiveManager>();
        }
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

        Object.DestroyImmediate(go.GetComponent<Collider>());
        var sc = go.AddComponent<SphereCollider>();
        sc.radius = 2.2f;
        sc.isTrigger = true;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 2.5f);
        go.GetComponent<Renderer>().material = mat;

        var lGO = new GameObject("ItemGlow");
        lGO.transform.SetParent(go.transform, false);
        var l = lGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.range = 5.5f;
        l.intensity = 2.8f;

        var pi = go.AddComponent<PickupItem>();
        var so = new SerializedObject(pi);
        so.FindProperty("itemType").enumValueIndex = (int)type;
        so.FindProperty("itemName").stringValue = itemName;
        so.FindProperty("pickupMessage").stringValue = msg;
        so.ApplyModifiedProperties();
    }

    // ─── 10. BAKE NAVMESH ─────────────────────────────────────────────
    private static void BakeNavMesh(GameObject envRoot)
    {
        var surfGO = new GameObject("NavMesh Surface");
        surfGO.transform.SetParent(envRoot.transform);
        var surface = surfGO.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
        surface.collectObjects = Unity.AI.Navigation.CollectObjects.All;
        surface.BuildNavMesh();
    }

    // ─── 11. BUILD SETTINGS ───────────────────────────────────────────
    private static void UpdateBuildSettings()
    {
        var currentScenes = EditorBuildSettings.scenes;
        bool hasLevel3 = false;

        foreach (var s in currentScenes)
        {
            if (s.path.Contains("Level3_Helipad")) hasLevel3 = true;
        }

        if (!hasLevel3)
        {
            var sceneList = new List<EditorBuildSettingsScene>(currentScenes);
            sceneList.Add(new EditorBuildSettingsScene(SCENE_PATH, true));
            EditorBuildSettings.scenes = sceneList.ToArray();
        }
    }

    // ─── HELPER METHODS ───────────────────────────────────────────────
    private static GameObject MakeCube(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool isStatic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localScale = scale;
        go.isStatic = isStatic;
        SetColor(go, color);
        return go;
    }

    private static GameObject MakeCylinder(Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot, Color color, bool isStatic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.isStatic = isStatic;
        SetColor(go, color);
        return go;
    }

    private static GameObject MakeSphere(Transform parent, string name, Vector3 pos, Vector3 scale, Color color, bool isStatic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
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

    private static void SetGlowColor(GameObject go, Color color, float emissionIntensity = 1.8f)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", color * emissionIntensity);
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
        rt.anchoredPosition = pos; rt.sizeDelta = new Vector2(480,80);
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
        go.AddComponent<UnityEngine.UI.Image>().color = new Color(0.1f,0.5f,0.85f,0.95f);
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

    private static GameObject CreateMobileCircleButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color bgColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        var img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = bgColor;
        go.AddComponent<UnityEngine.UI.Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var txt = textGO.AddComponent<UnityEngine.UI.Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 26;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;
        var ol = textGO.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        return go;
    }
}
#endif
