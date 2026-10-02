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

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Sân đỗ trực thăng trung tâm...", 0.25f);
            var blastGates = BuildHelipadPlatform(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Trực thăng cứu hộ thoát hiểm...", 0.40f);
            BuildRescueHelicopter(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Tháp Điều Khiển & Trạm Radar...", 0.55f);
            BuildControlTowerAndRadar(envRoot.transform, blastGates);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Kho Tiếp Liệu & Chứa Nhiên Liệu...", 0.68f);
            BuildFuelDepot(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Xây dựng Buồng Thang Máy Đến...", 0.78f);
            BuildElevatorArrival(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Thiết lập Ánh sáng đêm tầng thượng...", 0.84f);
            SetupRooftopLighting(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Thiết lập Người chơi & Robot AI...", 0.90f);
            SetupPlayerAndRobot(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Thiết lập UI Canvas & Victory Screen...", 0.94f);
            SetupCanvasUI(envRoot.transform);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Bake NavMesh cho AI...", 0.97f);
            BakeNavMesh(envRoot);

            EditorUtility.DisplayProgressBar("Xây dựng Màn 3", "Cập nhật Build Settings...", 0.99f);
            UpdateBuildSettings();

            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorUtility.ClearProgressBar();

            if (showDialog)
            {
                EditorUtility.DisplayDialog("Thành công!",
                    "Màn 3 (Level 3: Helipad Escape) đã được xây dựng hoàn tất 100%!\n\n" +
                    "• Bước ra khỏi thang máy đến sân đỗ trực thăng tầng thượng.\n" +
                    "• Vào Kho Tiếp Liệu (Tây Bắc) lấy Can Nhiên Liệu Phản Lực và nạp cho trực thăng.\n" +
                    "• Lên Tháp Điều Khiển (Đông Bắc) bật Trạm Radar để mở không lưu & hạ rào chắn sân đỗ.\n" +
                    "• Lên Trực thăng, nổ máy cất cánh và CHIẾN THẮNG TOÀN BỘ TRÒ CHƠI!\n\n" +
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

        foreach (var name in new[] { "Player", "Robot", "GameplayCanvas", "EventSystem", "Waypoints", "Main Camera", "NavMesh Surface", "SafetyPerimeter" })
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

    // ─── 2. SÂN ĐỖ TRỰC THĂNG TRUNG TÂM (CENTRAL HELIPAD) ────────────────
    private static Transform[] BuildHelipadPlatform(Transform parent)
    {
        var root = new GameObject("CentralHelipad");
        root.transform.SetParent(parent);

        // Tấm kim loại bệ đáp bát giác lớn: 24m x 24m
        var padFloor = MakeCube(root.transform, "HelipadPlate", new Vector3(0, 0.05f, 0), new Vector3(24f, 0.1f, 24f), new Color(0.12f, 0.13f, 0.15f), true);

        // Vòng tròn vàng bao quanh viền sân đỗ
        MakeCube(root.transform, "Border_N", new Vector3(0, 0.11f, 11.5f), new Vector3(22f, 0.03f, 0.6f), new Color(1f, 0.85f, 0.1f), true);
        MakeCube(root.transform, "Border_S", new Vector3(0, 0.11f, -11.5f), new Vector3(22f, 0.03f, 0.6f), new Color(1f, 0.85f, 0.1f), true);
        MakeCube(root.transform, "Border_W", new Vector3(-11.5f, 0.11f, 0), new Vector3(0.6f, 0.03f, 22f), new Color(1f, 0.85f, 0.1f), true);
        MakeCube(root.transform, "Border_E", new Vector3(11.5f, 0.11f, 0), new Vector3(0.6f, 0.03f, 22f), new Color(1f, 0.85f, 0.1f), true);

        // Biểu tượng chữ [ H ] màu vàng dạ quang ở trung tâm sân đỗ
        Color hCol = new Color(1f, 0.88f, 0.15f);
        MakeCube(root.transform, "Mark_H_Left",   new Vector3(-2.8f, 0.12f, 0), new Vector3(0.9f, 0.04f, 6.0f), hCol, true);
        MakeCube(root.transform, "Mark_H_Right",  new Vector3( 2.8f, 0.12f, 0), new Vector3(0.9f, 0.04f, 6.0f), hCol, true);
        MakeCube(root.transform, "Mark_H_Center", new Vector3(    0, 0.12f, 0), new Vector3(5.6f, 0.04f, 0.9f), hCol, true);

        // Hệ thống 8 đèn tín hiệu đường băng sân bay (Runway Lights)
        Vector3[] lightPositions = new Vector3[] {
            new Vector3(-11f, 0.35f, -11f),
            new Vector3(  0f, 0.35f, -11.8f),
            new Vector3( 11f, 0.35f, -11f),
            new Vector3( 11.8f, 0.35f, 0f),
            new Vector3( 11f, 0.35f, 11f),
            new Vector3(  0f, 0.35f, 11.8f),
            new Vector3(-11f, 0.35f, 11f),
            new Vector3(-11.8f, 0.35f, 0f)
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
            new Vector3(-13f, 2.0f, -13f),
            new Vector3( 13f, 2.0f, -13f),
            new Vector3( 13f, 2.0f,  13f),
            new Vector3(-13f, 2.0f,  13f)
        };

        Transform[] gates = new Transform[4];
        var gatesRoot = new GameObject("BlastShieldPillars");
        gatesRoot.transform.SetParent(root.transform);

        for (int i = 0; i < 4; i++)
        {
            var pillar = MakeCube(gatesRoot.transform, $"Pillar_{i+1}", gatePositions[i], new Vector3(1.2f, 4.5f, 1.2f), new Color(0.25f, 0.28f, 0.32f), false);
            // Cánh cổng tia sáng / lưới an toàn hạ xuống khi bật Radar
            var barrier = MakeCube(pillar.transform, "BarrierPlate", new Vector3(0, 0.5f, 0), new Vector3(0.9f, 3.5f, 0.9f), new Color(0.9f, 0.3f, 0.1f), false);
            gates[i] = barrier.transform;
        }

        return gates;
    }

    // ─── 3. TRỰC THĂNG CỨU HỘ THOÁT HIỂM (RESCUE HELICOPTER) ────────────
    private static void BuildRescueHelicopter(Transform parent)
    {
        var heliRoot = new GameObject("RescueHelicopter");
        heliRoot.transform.SetParent(parent);
        heliRoot.transform.position = new Vector3(0, 0.4f, 0);

        int intLayer = LayerMask.NameToLayer("Interactable");
        heliRoot.layer = intLayer >= 0 ? intLayer : 0;

        Color heliBodyCol = new Color(0.18f, 0.24f, 0.22f); // Xanh rêu quân sự sang trọng
        Color stripeCol   = new Color(1f, 0.45f, 0.05f);    // Sọc cam cứu nạn
        Color glassCol    = new Color(0.05f, 0.15f, 0.25f);  // Kính buồng lái

        // Thân chính (Fuselage)
        var body = MakeCube(heliRoot.transform, "Fuselage", new Vector3(0, 1.6f, 0), new Vector3(2.4f, 2.2f, 5.8f), heliBodyCol, false);
        body.layer = heliRoot.layer;

        // Kính buồng lái (Cockpit)
        var cockpit = MakeCube(heliRoot.transform, "CockpitGlass", new Vector3(0, 1.8f, 2.2f), new Vector3(2.2f, 1.6f, 2.0f), glassCol, false);

        // Mũi máy bay (Nose cone)
        var nose = MakeCube(heliRoot.transform, "NoseCone", new Vector3(0, 1.2f, 3.4f), new Vector3(1.8f, 1.2f, 1.2f), heliBodyCol, false);

        // Sọc cam cứu hộ trên thân
        MakeCube(heliRoot.transform, "RescueStripe", new Vector3(0, 1.6f, -0.5f), new Vector3(2.45f, 0.6f, 1.8f), stripeCol, false);

        // Đuôi trực thăng (Tail Boom)
        var tail = MakeCube(heliRoot.transform, "TailBoom", new Vector3(0, 1.8f, -4.5f), new Vector3(0.6f, 0.6f, 5.0f), heliBodyCol, false);
        // Cánh đuôi đứng (Vertical Fin)
        var fin = MakeCube(heliRoot.transform, "TailFin", new Vector3(0, 2.6f, -6.8f), new Vector3(0.2f, 1.8f, 1.2f), stripeCol, false);

        // Càng đáp (Skids)
        var skidL = MakeCube(heliRoot.transform, "Skid_L", new Vector3(-1.2f, 0.2f, 0), new Vector3(0.15f, 0.15f, 6.2f), new Color(0.1f, 0.1f, 0.12f), false);
        var skidR = MakeCube(heliRoot.transform, "Skid_R", new Vector3( 1.2f, 0.2f, 0), new Vector3(0.15f, 0.15f, 6.2f), new Color(0.1f, 0.1f, 0.12f), false);

        // Thanh chống càng đáp
        MakeCube(heliRoot.transform, "SkidStrut_FL", new Vector3(-1.1f, 0.55f,  1.5f), new Vector3(0.1f, 0.7f, 0.1f), Color.gray, false);
        MakeCube(heliRoot.transform, "SkidStrut_FR", new Vector3( 1.1f, 0.55f,  1.5f), new Vector3(0.1f, 0.7f, 0.1f), Color.gray, false);
        MakeCube(heliRoot.transform, "SkidStrut_RL", new Vector3(-1.1f, 0.55f, -1.5f), new Vector3(0.1f, 0.7f, 0.1f), Color.gray, false);
        MakeCube(heliRoot.transform, "SkidStrut_RR", new Vector3( 1.1f, 0.55f, -1.5f), new Vector3(0.1f, 0.7f, 0.1f), Color.gray, false);

        // CÁNH QUẠT CHÍNH (MAIN ROTOR)
        var mastGO = new GameObject("MainRotorMast");
        mastGO.transform.SetParent(heliRoot.transform, false);
        mastGO.transform.localPosition = new Vector3(0, 2.8f, 0.2f);
        MakeCube(mastGO.transform, "Shaft", Vector3.zero, new Vector3(0.25f, 0.6f, 0.25f), Color.gray, false);

        var rotorHub = new GameObject("RotorBladesHub");
        rotorHub.transform.SetParent(mastGO.transform, false);
        rotorHub.transform.localPosition = new Vector3(0, 0.35f, 0);

        // 4 Lá cánh quạt lớn (đường kính 11m)
        Color bladeCol = new Color(0.1f, 0.1f, 0.12f);
        MakeCube(rotorHub.transform, "Blade_1", new Vector3(0, 0,  2.7f), new Vector3(0.35f, 0.05f, 5.4f), bladeCol, false);
        MakeCube(rotorHub.transform, "Blade_2", new Vector3(0, 0, -2.7f), new Vector3(0.35f, 0.05f, 5.4f), bladeCol, false);
        MakeCube(rotorHub.transform, "Blade_3", new Vector3( 2.7f, 0, 0), new Vector3(5.4f, 0.05f, 0.35f), bladeCol, false);
        MakeCube(rotorHub.transform, "Blade_4", new Vector3(-2.7f, 0, 0), new Vector3(5.4f, 0.05f, 0.35f), bladeCol, false);

        // CÁNH QUẠT ĐUÔI (TAIL ROTOR)
        var tailRotorGO = new GameObject("TailRotorHub");
        tailRotorGO.transform.SetParent(heliRoot.transform, false);
        tailRotorGO.transform.localPosition = new Vector3(0.35f, 2.7f, -6.8f);
        MakeCube(tailRotorGO.transform, "TailBlade_1", new Vector3(0,  0.6f, 0), new Vector3(0.04f, 1.2f, 0.18f), Color.yellow, false);
        MakeCube(tailRotorGO.transform, "TailBlade_2", new Vector3(0, -0.6f, 0), new Vector3(0.04f, 1.2f, 0.18f), Color.yellow, false);

        // Đèn pha tìm kiếm dưới mũi
        var searchLightGO = new GameObject("SearchLight");
        searchLightGO.transform.SetParent(heliRoot.transform, false);
        searchLightGO.transform.localPosition = new Vector3(0, 0.8f, 3.0f);
        searchLightGO.transform.localRotation = Quaternion.Euler(30f, 0, 0);
        var sl = searchLightGO.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = new Color(0.9f, 0.95f, 1f);
        sl.range = 30f;
        sl.spotAngle = 65f;
        sl.intensity = 2.5f;

        // Hiệu ứng gió bụi khi cất cánh (ParticleSystem Downwash Dust)
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

        // Biển báo chỉ dẫn tương tác trực thăng
        var signGO = new GameObject("HeliSign");
        signGO.transform.SetParent(heliRoot.transform, false);
        signGO.transform.localPosition = new Vector3(0, 0.2f, 3.8f);
        signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        signGO.transform.localScale = Vector3.one * 0.025f;
        var tm = signGO.AddComponent<TextMesh>();
        tm.text = "▲ TRỰC THĂNG THOÁT HIỂM ▲\n[ Bấm E để tương tác ]";
        tm.fontSize = 36;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.yellow;

        // Collider tương tác
        var col = heliRoot.AddComponent<BoxCollider>();
        col.size = new Vector3(5.5f, 3.5f, 9.5f);
        col.isTrigger = true;

        // Controller
        var ctrl = heliRoot.AddComponent<HelicopterController>();
        ctrl.SetupReferences(rotorHub.transform, tailRotorGO.transform, sl, ps);

        var so = new SerializedObject(ctrl);
        so.FindProperty("mainRotor").objectReferenceValue = rotorHub.transform;
        so.FindProperty("tailRotor").objectReferenceValue = tailRotorGO.transform;
        so.FindProperty("searchLight").objectReferenceValue = sl;
        so.FindProperty("downwashDust").objectReferenceValue = ps;
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

        // Tầng 1: Phòng kỹ thuật (8m x 4m x 8m)
        MakeCube(root.transform, "TowerFloor1", new Vector3(0, 2f, 0), new Vector3(8f, 4f, 8f), wallCol, true);

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

        // BÀN ĐIỀU KHIỂN RADAR CONSOLE (BÊN DƯỚI TẦNG 1 HOẶC TẦNG 2)
        var consoleGO = new GameObject("RadarControlConsole");
        consoleGO.transform.SetParent(root.transform, false);
        consoleGO.transform.localPosition = new Vector3(-2.5f, 0.8f, -2.5f);
        consoleGO.transform.localRotation = Quaternion.Euler(0, 45f, 0);

        int intLayer = LayerMask.NameToLayer("Interactable");
        consoleGO.layer = intLayer >= 0 ? intLayer : 0;

        // Bàn máy tính
        MakeCube(consoleGO.transform, "DeskBody", Vector3.zero, new Vector3(1.8f, 1.2f, 0.8f), new Color(0.12f, 0.14f, 0.16f), false);
        var screen = MakeCube(consoleGO.transform, "RadarScreen", new Vector3(0, 0.7f, -0.1f), new Vector3(1.2f, 0.8f, 0.15f), new Color(0.05f, 0.15f, 0.2f), false);

        // Đèn trạng thái trên console
        var lGO = new GameObject("ConsoleScreenLight");
        lGO.transform.SetParent(consoleGO.transform, false);
        lGO.transform.localPosition = new Vector3(0, 0.8f, 0.25f);
        var scrLight = lGO.AddComponent<Light>();
        scrLight.type = LightType.Point;
        scrLight.color = Color.red;
        scrLight.intensity = 1.5f;
        scrLight.range = 3f;

        // Chữ chỉ dẫn
        var lblGO = new GameObject("ConsoleSign");
        lblGO.transform.SetParent(consoleGO.transform, false);
        lblGO.transform.localPosition = new Vector3(0, 1.3f, 0);
        lblGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
        lblGO.transform.localScale = Vector3.one * 0.02f;
        var tm = lblGO.AddComponent<TextMesh>();
        tm.text = "[ TRẠM ĐIỀU HÀNH RADAR ]\nKích hoạt dẫn đường\n[ Bấm E ]";
        tm.fontSize = 32;
        tm.alignment = TextAlignment.Center;
        tm.anchor = TextAnchor.MiddleCenter;
        tm.color = Color.cyan;

        var col = consoleGO.AddComponent<BoxCollider>();
        col.size = new Vector3(2.2f, 2.0f, 1.6f);
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
        RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.18f); // Đêm công nghiệp

        var lRoot = new GameObject("RooftopFloodlights");
        lRoot.transform.SetParent(parent);

        // 4 Cột đèn pha cao áp rọi sáng sân đỗ từ 4 góc
        Vector3[] polePositions = new Vector3[] {
            new Vector3(-20f, 0, -20f),
            new Vector3( 20f, 0, -20f),
            new Vector3( 20f, 0,  20f),
            new Vector3(-20f, 0,  20f)
        };

        for (int i = 0; i < 4; i++)
        {
            var pole = MakeCube(lRoot.transform, $"FloodPole_{i+1}", polePositions[i] + Vector3.up * 4f, new Vector3(0.4f, 8f, 0.4f), Color.gray, true);
            var fLightGO = new GameObject($"Floodlight_{i+1}");
            fLightGO.transform.SetParent(pole.transform, false);
            fLightGO.transform.localPosition = new Vector3(0, 4f, 0);
            fLightGO.transform.LookAt(Vector3.up * 1.5f); // Rọi vào trực thăng trung tâm
            var fl = fLightGO.AddComponent<Light>();
            fl.type = LightType.Spot;
            fl.color = new Color(0.85f, 0.92f, 1f);
            fl.range = 45f;
            fl.spotAngle = 75f;
            fl.intensity = 2.8f;
        }

        // Đèn mặt trăng dịu nhẹ toàn cảnh
        var moonGO = new GameObject("MoonDirectionalLight");
        moonGO.transform.SetParent(lRoot.transform, false);
        moonGO.transform.position = new Vector3(0, 40f, 0);
        moonGO.transform.rotation = Quaternion.Euler(50f, -30f, 0);
        var ml = moonGO.AddComponent<Light>();
        ml.type = LightType.Directional;
        ml.color = new Color(0.4f, 0.5f, 0.65f);
        ml.intensity = 0.6f;
    }

    // ─── 8. NGƯỜI CHƠI & ROBOT AI TUẦN TRA ─────────────────────────────
    private static void SetupPlayerAndRobot(Transform parent)
    {
        // 1. Player
        GameObject player = GameObject.FindWithTag("Player") ?? GameObject.Find("Player");
        if (player == null)
        {
            player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player";
            player.tag = "Player";
            SetColor(player, new Color(0.2f, 0.4f, 0.8f));
        }

        // Xuất phát bên trong buồng thang máy nhìn ra sân đỗ
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
        cc.stepOffset = 0.3f;
        cc.minMoveDistance = 0f;

        if (player.GetComponent<PlayerHealth>() == null) player.AddComponent<PlayerHealth>();

        var pi = player.GetComponent<PlayerInteraction>();
        if (pi == null) pi = player.AddComponent<PlayerInteraction>();
        var soPI = new SerializedObject(pi);
        soPI.FindProperty("interactRange").floatValue = 3.5f;
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

        var fpc = cam.GetComponent<FirstPersonCamera>() ?? cam.gameObject.AddComponent<FirstPersonCamera>();
        fpc.enabled = true;
        fpc.SetPlayerBody(player.transform);

        var rend = player.GetComponent<Renderer>();
        if (rend != null)
        {
            rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        }

        var cmm = player.GetComponent<CameraModeManager>() ?? player.AddComponent<CameraModeManager>();

        var soPC = new SerializedObject(pc);
        soPC.FindProperty("cameraTransform").objectReferenceValue = cam.transform;
        soPC.ApplyModifiedProperties();

        // 2. Heavy Sentinel Robot
        GameObject robot = GameObject.Find("Robot");
        if (robot == null)
        {
            robot = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            robot.name = "Robot";
            robot.tag = "Enemy";
            SetColor(robot, new Color(0.95f, 0.15f, 0.1f));
        }
        robot.transform.position = new Vector3(10f, 1.1f, 0);

        if (robot.GetComponent<UnityEngine.AI.NavMeshAgent>() == null)
        {
            var agent = robot.AddComponent<UnityEngine.AI.NavMeshAgent>();
            agent.speed = 3.6f;
            agent.angularSpeed = 120f;
            agent.acceleration = 8f;
        }

        var sensor = new GameObject("SensorLight");
        sensor.transform.SetParent(robot.transform);
        sensor.transform.localPosition = new Vector3(0, 1.6f, 0.4f);
        var sl = sensor.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = Color.red;
        sl.range = 14f;
        sl.spotAngle = 65f;

        // Waypoints tuần tra hình chữ nhật bao quanh sân đỗ trực thăng
        var wpRoot = new GameObject("Waypoints");
        wpRoot.transform.SetParent(parent);
        Vector3[] wpPositions = new[] {
            new Vector3( 15f, 0.1f, -15f),
            new Vector3( 15f, 0.1f,  15f),
            new Vector3(-15f, 0.1f,  15f),
            new Vector3(-15f, 0.1f, -15f)
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
        sc.radius = 1.6f;
        sc.isTrigger = true;

        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * 1.5f);
        go.GetComponent<Renderer>().material = mat;

        var lGO = new GameObject("ItemGlow");
        lGO.transform.SetParent(go.transform, false);
        var l = lGO.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = col;
        l.range = 3.5f;
        l.intensity = 1.5f;

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
}
#endif
