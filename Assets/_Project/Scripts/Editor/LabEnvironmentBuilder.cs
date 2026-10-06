#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

/// <summary>
/// LabEnvironmentBuilder: Tu dong xay dung toan bo moi truong Lab.
/// Menu: EscapeTheLab -> Build Lab Environment
/// Chay TRUOC Phase 3-8 Setup!
/// </summary>
public class LabEnvironmentBuilder : Editor
{
    [MenuItem("EscapeTheLab/\U0001f3d7\ufe0f 1) Build Lab Environment (Rooms + Lights)")]
    public static void BuildLabEnvironment()
    {
        var activeScene = EditorSceneManager.GetActiveScene();
        if (!activeScene.name.Contains("Lab"))
        {
            bool open = EditorUtility.DisplayDialog("Open Lab Scene?",
                "Hay mo scene Lab truoc.", "Mo Lab Scene", "Cancel");
            if (!open) return;
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity");
        }

        bool confirm = EditorUtility.DisplayDialog("Build Lab Environment",
            "Script se tao:\n" +
            "- San + Tran + Tuong phong lab\n" +
            "- 3 Phong ket noi nhau + Hanh lang\n" +
            "- Den huynh quang phong cach lab\n" +
            "- Props: ban may tinh, thung, cot\n" +
            "- Dat lai items dung vi tri trong phong\n" +
            "- Tu dong Bake NavMesh\n\nTiep tuc?",
            "Build Now!", "Cancel");
        if (!confirm) return;

        try
        {
            EditorUtility.DisplayProgressBar("Building Lab", "Clearing old environment...", 0.05f);
            ClearOldEnvironment();

            GameObject envRoot = new GameObject("=== LAB ENVIRONMENT ===");

            EditorUtility.DisplayProgressBar("Building Lab", "Main Hall...", 0.15f);
            BuildRoom(envRoot.transform, "MainHall",
                new Vector3(0, 0, 0), new Vector3(20, 5, 24),
                new Color(0.15f, 0.15f, 0.18f), new Color(0.22f, 0.23f, 0.28f),
                holeFront: true, holeBack: false, holeLeft: true, holeRight: true);

            EditorUtility.DisplayProgressBar("Building Lab", "Room A (Machine Room)...", 0.25f);
            BuildRoom(envRoot.transform, "RoomA_MachineRoom",
                new Vector3(-16, 0, 4), new Vector3(12, 5, 16),
                new Color(0.12f, 0.14f, 0.12f), new Color(0.18f, 0.22f, 0.18f),
                holeFront: false, holeBack: false, holeLeft: false, holeRight: true);
            BuildCorridor(envRoot.transform, "CorridorA",
                new Vector3(-10, 0, 4), new Vector3(-6, 0, 4), 4f, 5f, true);

            EditorUtility.DisplayProgressBar("Building Lab", "Room B (Server Room)...", 0.35f);
            BuildRoom(envRoot.transform, "RoomB_ServerRoom",
                new Vector3(16, 0, 4), new Vector3(12, 5, 16),
                new Color(0.10f, 0.12f, 0.15f), new Color(0.15f, 0.18f, 0.25f),
                holeFront: false, holeBack: false, holeLeft: true, holeRight: false);
            BuildCorridor(envRoot.transform, "CorridorB",
                new Vector3(6, 0, 4), new Vector3(10, 0, 4), 4f, 5f, true);

            EditorUtility.DisplayProgressBar("Building Lab", "Room C (Exit Room)...", 0.45f);
            BuildRoom(envRoot.transform, "RoomC_ExitRoom",
                new Vector3(0, 0, 20), new Vector3(14, 5, 10),
                new Color(0.08f, 0.12f, 0.12f), new Color(0.12f, 0.18f, 0.20f),
                holeFront: false, holeBack: true, holeLeft: false, holeRight: false);
            BuildCorridor(envRoot.transform, "CorridorC",
                new Vector3(0, 0, 12), new Vector3(0, 0, 15), 4f, 5f, false);

            EditorUtility.DisplayProgressBar("Building Lab", "Adding props...", 0.55f);
            AddLabProps(envRoot.transform);

            EditorUtility.DisplayProgressBar("Building Lab", "Setting up lighting...", 0.65f);
            SetupLighting(envRoot.transform);

            EditorUtility.DisplayProgressBar("Building Lab", "Repositioning objects...", 0.75f);
            RepositionItems();

            EditorUtility.DisplayProgressBar("Building Lab", "Decorating with Sci-Fi Props...", 0.82f);
            EscapeTheLab.EditorTools.AssetUpgradeTools.DecorateLabWithSciFiPropsInternal(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayProgressBar("Building Lab", "Baking NavMesh...", 0.88f);
            BakeNavMeshNow(envRoot);

            EditorUtility.DisplayProgressBar("Building Lab", "Applying P3D Wall Textures...", 0.95f);
            EscapeTheLab.EditorTools.AssetUpgradeTools.ApplyP3DWallTexturesToScene(EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayProgressBar("Building Lab", "Saving scene...", 0.97f);
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog("Lab Built!",
                "Moi truong Lab hoan chinh!\n\n" +
                "3 phong + hanh lang + den + NavMesh Baked!\n\n" +
                "Buoc tiep:\n" +
                "1. EscapeTheLab > Setup Phase 3-8 (neu chua co Robot/UI)\n" +
                "2. Nhan Play (▶) de test!", "OK");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            Debug.LogError("[LabBuilder] " + ex);
            EditorUtility.DisplayDialog("Error", ex.Message, "OK");
        }
    }

    // ─── CLEAR ───────────────────────────────────────────────────────────────
    static void ClearOldEnvironment()
    {
        var old = GameObject.Find("=== LAB ENVIRONMENT ===");
        if (old != null) DestroyImmediate(old);

        // Dieu chinh directional light tan xa hai hoa
        var lights = Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
        foreach (var l in lights)
            if (l.type == LightType.Directional)
            { l.intensity = 0.85f; l.color = new Color(0.92f, 0.95f, 1.0f); }
    }

    // ─── BUILD ROOM ──────────────────────────────────────────────────────────
    static void BuildRoom(Transform parent, string roomName, Vector3 center, Vector3 size, Color floorCol, Color wallCol, bool holeFront=false, bool holeBack=false, bool holeLeft=false, bool holeRight=false)
    {
        var root = new GameObject(roomName);
        root.transform.SetParent(parent);

        float hw = size.x / 2f;
        float hd = size.z / 2f;
        float h  = size.y;
        float doorWidth = 4f;

        // Floor
        MakeCube(root.transform, "Floor",
            center + Vector3.down * 0.1f, new Vector3(size.x, 0.2f, size.z), floorCol, isStatic: true);
        // Ceiling
        MakeCube(root.transform, "Ceiling",
            center + Vector3.up * h, new Vector3(size.x, 0.2f, size.z), wallCol * 0.55f, isStatic: true);

        // Helper cho tuong
        System.Action<string, Vector3, Vector3, bool, bool> BuildWall = (name, pos, wallSize, hasHole, isZAxis) =>
        {
            if (!hasHole)
            {
                MakeCube(root.transform, name, pos, wallSize, wallCol, isStatic: true);
            }
            else
            {
                // Tao 2 khoi nho 2 ben thay vi 1 khoi dai
                float totalLen = isZAxis ? wallSize.z : wallSize.x;
                float sideLen = (totalLen - doorWidth) / 2f;
                if (sideLen > 0)
                {
                    if (isZAxis)
                    {
                        MakeCube(root.transform, name + "_1", pos + new Vector3(0, 0, -(totalLen/2f) + (sideLen/2f)), new Vector3(wallSize.x, wallSize.y, sideLen), wallCol, true);
                        MakeCube(root.transform, name + "_2", pos + new Vector3(0, 0, (totalLen/2f) - (sideLen/2f)), new Vector3(wallSize.x, wallSize.y, sideLen), wallCol, true);
                    }
                    else
                    {
                        MakeCube(root.transform, name + "_1", pos + new Vector3(-(totalLen/2f) + (sideLen/2f), 0, 0), new Vector3(sideLen, wallSize.y, wallSize.z), wallCol, true);
                        MakeCube(root.transform, name + "_2", pos + new Vector3((totalLen/2f) - (sideLen/2f), 0, 0), new Vector3(sideLen, wallSize.y, wallSize.z), wallCol, true);
                    }
                }
            }
        };

        // Walls
        BuildWall("Wall_Front", center + new Vector3(0, h/2f, hd), new Vector3(size.x, h, 0.3f), holeFront, false);
        BuildWall("Wall_Back",  center + new Vector3(0, h/2f, -hd), new Vector3(size.x, h, 0.3f), holeBack, false);
        BuildWall("Wall_Left",  center + new Vector3(-hw, h/2f, 0), new Vector3(0.3f, h, size.z), holeLeft, true);
        BuildWall("Wall_Right", center + new Vector3(hw, h/2f, 0),  new Vector3(0.3f, h, size.z), holeRight, true);

        // Socle/baseboard accent
        var accentCol = new Color(0.05f, 0.5f, 0.5f);
        MakeCube(root.transform, "Socle_F", center + new Vector3(0, 0.15f, hd - 0.15f),
            new Vector3(size.x, 0.3f, 0.05f), accentCol, isStatic: true);
        MakeCube(root.transform, "Socle_B", center + new Vector3(0, 0.15f, -hd + 0.15f),
            new Vector3(size.x, 0.3f, 0.05f), accentCol, isStatic: true);
    }

    // ─── BUILD CORRIDOR ──────────────────────────────────────────────────────
    static void BuildCorridor(Transform parent, string corridorName,
        Vector3 start, Vector3 end, float width, float height, bool alongX)
    {
        Vector3 center = (start + end) / 2f;
        float   length = Vector3.Distance(start, end);
        var root = new GameObject(corridorName);
        root.transform.SetParent(parent);
        Color wallCol = new Color(0.20f, 0.21f, 0.26f);

        MakeCube(root.transform, "Floor",
            center, new Vector3(alongX ? length : width, 0.2f, alongX ? width : length),
            new Color(0.13f, 0.13f, 0.16f), isStatic: true);
        MakeCube(root.transform, "Ceiling",
            center + Vector3.up * height,
            new Vector3(alongX ? length : width, 0.2f, alongX ? width : length),
            wallCol * 0.55f, isStatic: true);

        if (alongX)
        {
            MakeCube(root.transform, "Wall_L",
                center + new Vector3(0, height/2f, -width/2f), new Vector3(length, height, 0.3f), wallCol, true);
            MakeCube(root.transform, "Wall_R",
                center + new Vector3(0, height/2f,  width/2f), new Vector3(length, height, 0.3f), wallCol, true);
        }
        else
        {
            MakeCube(root.transform, "Wall_L",
                center + new Vector3(-width/2f, height/2f, 0), new Vector3(0.3f, height, length), wallCol, true);
            MakeCube(root.transform, "Wall_R",
                center + new Vector3( width/2f, height/2f, 0), new Vector3(0.3f, height, length), wallCol, true);
        }
    }

    // ─── PROPS ───────────────────────────────────────────────────────────────
    static void AddLabProps(Transform parent)
    {
        var props = new GameObject("Props");
        props.transform.SetParent(parent);

        // Cot tru sanh chinh
        Color pillarCol = new Color(0.18f, 0.20f, 0.22f);
        foreach (var pos in new[] {
            new Vector3(-8,2.5f,-8), new Vector3(8,2.5f,-8),
            new Vector3(-8,2.5f,8),  new Vector3(8,2.5f,8)
        })
        {
            var cyl = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            cyl.name = "Pillar"; cyl.transform.SetParent(props.transform);
            cyl.transform.position = pos; cyl.transform.localScale = new Vector3(0.6f,2.5f,0.6f);
            SetMat(cyl, pillarCol); cyl.isStatic = true;
        }

        // Workbenches - phong server
        MakeWorkbench(props.transform, new Vector3(14, 0, 8));
        MakeWorkbench(props.transform, new Vector3(18, 0, 4));
        MakeWorkbench(props.transform, new Vector3(14, 0, 0));

        // Crates - phong may moc
        Color crateCol = new Color(0.20f,0.18f,0.10f);
        MakeCube(props.transform, "Crate", new Vector3(-15,0.5f, 8), new Vector3(1.2f,1f,1.2f), crateCol, true);
        MakeCube(props.transform, "Crate", new Vector3(-18,0.5f, 5), new Vector3(1f,0.9f,1f),   crateCol, true);
        MakeCube(props.transform, "Crate", new Vector3(-14,0.5f,-2), new Vector3(1.2f,1f,1.2f), crateCol, true);

        // Warning stripe truoc EXIT
        for (int i = -2; i <= 2; i++)
        {
            Color c = (i % 2 == 0) ? new Color(0.85f,0.65f,0f) : new Color(0.1f,0.1f,0.1f);
            MakeCube(props.transform, "Stripe", new Vector3(i*1.3f, 0.02f, 17), new Vector3(0.6f,0.02f,2f), c, true);
        }
    }

    static void MakeWorkbench(Transform parent, Vector3 pos)
    {
        var bench = new GameObject("Workbench"); bench.transform.SetParent(parent); bench.transform.position = pos;
        Color col = new Color(0.12f,0.15f,0.20f);
        // Top
        var top = MakeCube(bench.transform, "Top", Vector3.up * 0.9f, new Vector3(2f,0.1f,0.8f), col, true);
        top.transform.localPosition = Vector3.up * 0.9f;
        // Legs
        for (int i = -1; i <= 1; i += 2)
        {
            var leg = GameObject.CreatePrimitive(PrimitiveType.Cube); leg.transform.SetParent(bench.transform);
            leg.transform.localPosition = new Vector3(i*0.85f, 0.45f, 0);
            leg.transform.localScale = new Vector3(0.07f,0.9f,0.07f);
            SetMat(leg, col * 0.7f); leg.isStatic = true;
        }
        // Screen glowing
        var scr = GameObject.CreatePrimitive(PrimitiveType.Cube); scr.transform.SetParent(bench.transform);
        scr.transform.localPosition = new Vector3(0,1.35f,-0.2f);
        scr.transform.localScale    = new Vector3(0.9f,0.55f,0.05f);
        DestroyImmediate(scr.GetComponent<BoxCollider>());
        var smat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        smat.color = new Color(0.02f,0.1f,0.3f);
        smat.EnableKeyword("_EMISSION");
        smat.SetColor("_EmissionColor", new Color(0f,0.6f,1.5f));
        scr.GetComponent<Renderer>().material = smat;
        scr.isStatic = true;
    }

    // ─── LIGHTING ────────────────────────────────────────────────────────────
    static void SetupLighting(Transform parent)
    {
        EscapeTheLab.EditorTools.AssetUpgradeTools.BrightenLabLightingInternal(EditorSceneManager.GetActiveScene());
    }

    static void MakeLight(Transform parent, Vector3 pos, Color col, float intensity, float range)
    {
        var go = new GameObject("FL"); go.transform.SetParent(parent); go.transform.position = pos;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = col; l.intensity = intensity; l.range = range;
        l.shadows = LightShadows.Soft;
        l.renderMode = LightRenderMode.ForcePixel;
        // Tube visual
        var tube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tube.transform.SetParent(go.transform); tube.transform.localPosition = Vector3.zero;
        tube.transform.localScale = new Vector3(1.6f, 0.07f, 0.07f);
        DestroyImmediate(tube.GetComponent<BoxCollider>());
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = col * 0.4f;
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", col * intensity * 0.25f);
        tube.GetComponent<Renderer>().material = mat;
    }

    static void MakeExitSign(Transform parent, Vector3 pos)
    {
        var go = new GameObject("ExitSign"); go.transform.SetParent(parent); go.transform.position = pos;
        var board = GameObject.CreatePrimitive(PrimitiveType.Cube);
        board.transform.SetParent(go.transform); board.transform.localPosition = Vector3.zero;
        board.transform.localScale = new Vector3(2f, 0.6f, 0.1f);
        DestroyImmediate(board.GetComponent<BoxCollider>());
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = new Color(0f,0.5f,0f);
        mat.EnableKeyword("_EMISSION");
        mat.SetColor("_EmissionColor", new Color(0f,2f,0.3f));
        board.GetComponent<Renderer>().material = mat;
        var l = go.AddComponent<Light>();
        l.type = LightType.Point; l.color = new Color(0f,1f,0.2f); l.intensity = 6f; l.range = 7f;
    }

    // ─── REPOSITION ──────────────────────────────────────────────────────────
    static void RepositionItems()
    {
        Move("SecurityCard",        new Vector3( 14f, 0.8f,  5f));
        Move("Fuse",                new Vector3(-15f, 0.8f, -3f));
        Move("Battery",             new Vector3(  5f, 0.8f, -8f));
        Move("AccessCodeNote",      new Vector3(-12f, 0.8f,  7f));
        Move("Terminal",            new Vector3( 16f, 0.1f,  1f));
        Move("Generator",           new Vector3(-14f, 0.1f,  1f));
        Move("ExitDoor",            new Vector3(  0f, 2.0f, 24.5f));
        Move("AccessCodeTerminal",  new Vector3( -3f, 0.1f, 18f));
        Move("Robot",               new Vector3(  0f, 0.1f,  6f));
        Move("Player",              new Vector3(  0f, 0.5f, -9f));
        Move("Waypoints",           new Vector3(  0f, 0f,    0f));
    }

    static void Move(string name, Vector3 pos)
    {
        var go = GameObject.Find(name);
        if (go != null) { go.transform.position = pos; Debug.Log("[LabBuilder] Moved " + name); }
    }

    // ─── NAVMESH ─────────────────────────────────────────────────────────────
    static void BakeNavMeshNow(GameObject root)
    {
        var surface = Object.FindFirstObjectByType<NavMeshSurface>();
        if (surface == null)
        {
            var nmGO = new GameObject("NavMesh Surface"); nmGO.transform.SetParent(root.transform);
            surface = nmGO.AddComponent<NavMeshSurface>();
        }
        surface.BuildNavMesh();
    }

    // ─── PRIMITIVE HELPERS ───────────────────────────────────────────────────
    static GameObject MakeCube(Transform parent, string name, Vector3 localPos, Vector3 scale, Color color, bool isStatic)
    {
        var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name; go.transform.SetParent(parent);
        go.transform.localPosition = localPos; go.transform.localScale = scale;
        go.isStatic = isStatic;
        SetMat(go, color);
        return go;
    }

    static void SetMat(GameObject go, Color color)
    {
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.color = color;
        go.GetComponent<Renderer>().material = mat;
    }
}
#endif
