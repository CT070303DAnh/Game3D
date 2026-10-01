#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using System.IO;

/// <summary>
/// EscapeTheLabSetup: Editor script tu dong setup Phase 1 va Phase 2.
/// Su dung: Menu bar → EscapeTheLab → Setup Phase 1 & 2
/// Chi chay trong Editor, khong anh huong build.
/// </summary>
public class EscapeTheLabSetup : EditorWindow
{
    // ───────────────────────────────────────────────
    // Menu Entry
    // ───────────────────────────────────────────────
    [MenuItem("EscapeTheLab/Setup Phase 1 & 2 (Auto)")]
    public static void RunFullSetup()
    {
        bool confirm = EditorUtility.DisplayDialog(
            "Escape The Lab - Auto Setup",
            "Script se tu dong:\n" +
            "- Tao scene MainMenu va Lab\n" +
            "- Tao GameManager, GameState prefabs\n" +
            "- Tao Player, Camera, MobileInput\n" +
            "- Cau hinh tat ca components\n\n" +
            "Tiep tuc?",
            "Yes, Setup Now",
            "Cancel"
        );

        if (!confirm) return;

        try
        {
            EditorUtility.DisplayProgressBar("Setting up...", "Creating folders...", 0.05f);
            CreateFolders();

            EditorUtility.DisplayProgressBar("Setting up...", "Creating Tags & Layers...", 0.1f);
            SetupTagsAndLayers();

            EditorUtility.DisplayProgressBar("Setting up...", "Creating MainMenu scene...", 0.25f);
            CreateMainMenuScene();

            EditorUtility.DisplayProgressBar("Setting up...", "Creating Lab scene...", 0.55f);
            CreateLabScene();

            EditorUtility.DisplayProgressBar("Setting up...", "Adding scenes to Build Settings...", 0.85f);
            AddScenesToBuildSettings();

            EditorUtility.DisplayProgressBar("Setting up...", "Done!", 1f);
            EditorUtility.ClearProgressBar();

            EditorUtility.DisplayDialog(
                "Setup Complete!",
                "Phase 1 & 2 da duoc setup thanh cong!\n\n" +
                "Kiem tra:\n" +
                "1. Scene MainMenu trong Assets/_Project/Scenes/\n" +
                "2. Scene Lab trong Assets/_Project/Scenes/\n" +
                "3. Mo Lab scene va nhan Play de test Player\n\n" +
                "Luu y: Animator Controller can tao thu cong.",
                "OK"
            );

            // Mo Lab scene de user test ngay
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity");
        }
        catch (System.Exception ex)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Setup Error", "Loi: " + ex.Message + "\n\nXem Console de biet them.", "OK");
            Debug.LogError("[EscapeTheLabSetup] Error: " + ex);
        }
    }

    // ───────────────────────────────────────────────
    // Step 1: Tao thu muc
    // ───────────────────────────────────────────────
    private static void CreateFolders()
    {
        string[] folders = {
            "Assets/_Project/Scenes",
            "Assets/_Project/Prefabs",
            "Assets/_Project/Prefabs/Player",
            "Assets/_Project/Prefabs/Robot",
            "Assets/_Project/Prefabs/Interactables",
            "Assets/_Project/Prefabs/UI",
            "Assets/_Project/Prefabs/VFX",
            "Assets/_Project/Scripts/Core",
            "Assets/_Project/Scripts/Player",
            "Assets/_Project/Scripts/AI",
            "Assets/_Project/Scripts/Interaction",
            "Assets/_Project/Scripts/Puzzle",
            "Assets/_Project/Scripts/UI",
            "Assets/_Project/Scripts/Audio",
            "Assets/_Project/Art/Materials",
            "Assets/_Project/Animations/Player",
            "Assets/_Project/Animations/Robot",
        };

        foreach (string path in folders)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = Path.GetFileName(path);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
        AssetDatabase.Refresh();
        Debug.Log("[Setup] Folders created.");
    }

    // ───────────────────────────────────────────────
    // Step 2: Tags va Layers
    // ───────────────────────────────────────────────
    private static void SetupTagsAndLayers()
    {
        // Them tag Player
        AddTag("Player");

        // Them layers
        AddLayer("Player");
        AddLayer("Ground");
        AddLayer("Interactable");
        AddLayer("Environment");
        Debug.Log("[Setup] Tags & Layers configured.");
    }

    private static void AddTag(string tagName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty tagsProp = tagManager.FindProperty("tags");

        for (int i = 0; i < tagsProp.arraySize; i++)
        {
            if (tagsProp.GetArrayElementAtIndex(i).stringValue == tagName) return;
        }
        tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
        tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tagName;
        tagManager.ApplyModifiedProperties();
    }

    private static void AddLayer(string layerName)
    {
        SerializedObject tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layersProp = tagManager.FindProperty("layers");

        // Check if already exists
        for (int i = 0; i < layersProp.arraySize; i++)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
            if (sp.stringValue == layerName) return;
        }

        // Tim slot trong (bat dau tu layer 6 - user layers)
        for (int i = 6; i < layersProp.arraySize; i++)
        {
            SerializedProperty sp = layersProp.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(sp.stringValue))
            {
                sp.stringValue = layerName;
                tagManager.ApplyModifiedProperties();
                return;
            }
        }
    }

    // ───────────────────────────────────────────────
    // Step 3: MainMenu Scene
    // ───────────────────────────────────────────────
    private static void CreateMainMenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── Bootstrapper ──
        GameObject bootstrapper = new GameObject("Bootstrapper");
        bootstrapper.AddComponent<Bootstrapper>();

        // ── GameManager prefab GameObject ──
        GameObject gmGO = new GameObject("GameManager");
        GameManager gm = gmGO.AddComponent<GameManager>();

        // ── GameState prefab GameObject ──
        GameObject gsGO = new GameObject("GameState");
        gsGO.AddComponent<GameState>();

        // Luu prefabs
        string gmPrefabPath = "Assets/_Project/Prefabs/GameManager.prefab";
        string gsPrefabPath = "Assets/_Project/Prefabs/GameState.prefab";

        GameObject gmPrefab = PrefabUtility.SaveAsPrefabAsset(gmGO, gmPrefabPath);
        GameObject gsPrefab = PrefabUtility.SaveAsPrefabAsset(gsGO, gsPrefabPath);

        // Xoa GameObjects tam
        GameObject.DestroyImmediate(gmGO);
        GameObject.DestroyImmediate(gsGO);

        // Gán prefabs vào Bootstrapper
        Bootstrapper bootstrapComp = bootstrapper.GetComponent<Bootstrapper>();
        SerializedObject soBootstrap = new SerializedObject(bootstrapComp);
        soBootstrap.FindProperty("gameManagerPrefab").objectReferenceValue = gmPrefab.GetComponent<GameManager>();
        soBootstrap.FindProperty("gameStatePrefab").objectReferenceValue = gsPrefab.GetComponent<GameState>();
        soBootstrap.ApplyModifiedProperties();

        // ── Canvas + MainMenuUI ──
        CreateMainMenuUI();

        // Luu scene
        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/MainMenu.unity");
        Debug.Log("[Setup] MainMenu scene created.");
    }

    private static void CreateMainMenuUI()
    {
        // Canvas
        GameObject canvasGO = new GameObject("MainMenuCanvas");
        UnityEngine.Canvas canvas = canvasGO.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // EventSystem
        GameObject esGO = new GameObject("EventSystem");
        esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
        esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

        // Main Panel
        GameObject mainPanel = CreateUIPanel(canvasGO.transform, "MainPanel");

        // Title
        GameObject titleGO = CreateUIText(mainPanel.transform, "TitleText", "ESCAPE THE LAB");
        RectTransform titleRect = titleGO.GetComponent<RectTransform>();
        titleRect.anchoredPosition = new Vector2(0, 150);
        titleRect.sizeDelta = new Vector2(600, 100);

        // Buttons
        GameObject playBtn = CreateUIButton(mainPanel.transform, "PlayButton", "[ PLAY ]", new Vector2(0, 20));
        GameObject settingsBtn = CreateUIButton(mainPanel.transform, "SettingsButton", "[ SETTINGS ]", new Vector2(0, -70));
        GameObject exitBtn = CreateUIButton(mainPanel.transform, "ExitButton", "[ EXIT ]", new Vector2(0, -160));

        // Settings Panel
        GameObject settingsPanel = CreateUIPanel(canvasGO.transform, "SettingsPanel");
        settingsPanel.SetActive(false);
        GameObject closeBtn = CreateUIButton(settingsPanel.transform, "CloseButton", "[ CLOSE ]", new Vector2(0, -150));
        GameObject settingsTitle = CreateUIText(settingsPanel.transform, "SettingsTitle", "SETTINGS");
        settingsTitle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, 150);

        // MainMenuUI script
        GameObject uiController = new GameObject("MainMenuUI");
        MainMenuUI menuUI = uiController.AddComponent<MainMenuUI>();
        SerializedObject soUI = new SerializedObject(menuUI);
        soUI.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<UnityEngine.UI.Button>();
        soUI.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<UnityEngine.UI.Button>();
        soUI.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<UnityEngine.UI.Button>();
        soUI.FindProperty("mainPanel").objectReferenceValue = mainPanel;
        soUI.FindProperty("settingsPanel").objectReferenceValue = settingsPanel;
        soUI.FindProperty("closeSettingsButton").objectReferenceValue = closeBtn.GetComponent<UnityEngine.UI.Button>();
        soUI.ApplyModifiedProperties();
    }

    // ───────────────────────────────────────────────
    // Step 4: Lab Scene (Gameplay)
    // ───────────────────────────────────────────────
    private static void CreateLabScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        // ── Environment ──
        SetupEnvironment();

        // ── Player ──
        GameObject player = SetupPlayer();

        // ── Camera ──
        SetupCamera(player.transform);

        // ── Mobile Input ──
        SetupMobileInput();

        // ── Lighting ──
        SetupLighting();

        // Luu scene
        EditorSceneManager.SaveScene(scene, "Assets/_Project/Scenes/Lab.unity");
        Debug.Log("[Setup] Lab scene created.");
    }

    private static void SetupEnvironment()
    {
        // Tao Environment parent
        GameObject env = new GameObject("Environment");

        // Floor / Plane
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
        floor.name = "Floor";
        floor.transform.SetParent(env.transform);
        floor.transform.localScale = new Vector3(5, 1, 5);
        floor.layer = LayerMask.NameToLayer("Ground") >= 0 ? LayerMask.NameToLayer("Ground") : 0;

        // Material mau xam cho san
        Material floorMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        floorMat.color = new Color(0.25f, 0.25f, 0.3f);
        floor.GetComponent<Renderer>().material = floorMat;
        AssetDatabase.CreateAsset(floorMat, "Assets/_Project/Art/Materials/FloorMaterial.mat");

        // 4 tuong don gian
        CreateWall(env.transform, "WallNorth", new Vector3(0, 2, 25), new Vector3(50, 4, 1));
        CreateWall(env.transform, "WallSouth", new Vector3(0, 2, -25), new Vector3(50, 4, 1));
        CreateWall(env.transform, "WallEast", new Vector3(25, 2, 0), new Vector3(1, 4, 50));
        CreateWall(env.transform, "WallWest", new Vector3(-25, 2, 0), new Vector3(1, 4, 50));

        // Prop mau: mot so hop de test
        CreateBox(env.transform, "Box1", new Vector3(5, 0.5f, 5));
        CreateBox(env.transform, "Box2", new Vector3(-8, 0.5f, 3));
        CreateBox(env.transform, "Box3", new Vector3(3, 0.5f, -7));
    }

    private static void CreateWall(Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent);
        wall.transform.position = pos;
        wall.transform.localScale = scale;
        int envLayer = LayerMask.NameToLayer("Environment");
        wall.layer = envLayer >= 0 ? envLayer : 0;

        Material wallMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        wallMat.color = new Color(0.35f, 0.37f, 0.42f);
        wall.GetComponent<Renderer>().material = wallMat;
    }

    private static void CreateBox(Transform parent, string name, Vector3 pos)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.SetParent(parent);
        box.transform.position = pos;
        box.transform.localScale = new Vector3(1, 1, 1);
        int envLayer = LayerMask.NameToLayer("Environment");
        box.layer = envLayer >= 0 ? envLayer : 0;
    }

    private static GameObject SetupPlayer()
    {
        // Player Capsule
        GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        player.name = "Player";
        player.tag = "Player";
        int playerLayer = LayerMask.NameToLayer("Player");
        player.layer = playerLayer >= 0 ? playerLayer : 0;
        player.transform.position = new Vector3(0, 1, 0);

        // Xoa CapsuleCollider mac dinh (CharacterController tu co)
        GameObject.DestroyImmediate(player.GetComponent<CapsuleCollider>());

        // CharacterController
        CharacterController cc = player.AddComponent<CharacterController>();
        cc.center = new Vector3(0, 0, 0);
        cc.radius = 0.3f;
        cc.height = 1.8f;

        // Animator
        player.AddComponent<Animator>();

        // PlayerController
        PlayerController pc = player.AddComponent<PlayerController>();

        // PlayerHealth
        player.AddComponent<PlayerHealth>();

        // PlayerInteraction
        PlayerInteraction pi = player.AddComponent<PlayerInteraction>();

        // GroundCheck child
        GameObject groundCheck = new GameObject("GroundCheck");
        groundCheck.transform.SetParent(player.transform);
        groundCheck.transform.localPosition = new Vector3(0, -0.85f, 0);

        // Gán GroundCheck vào PlayerController qua SerializedObject
        SerializedObject soPC = new SerializedObject(pc);
        soPC.FindProperty("groundCheck").objectReferenceValue = groundCheck.transform;

        // Ground mask: lay layer Ground
        int groundLayerIdx = LayerMask.NameToLayer("Ground");
        if (groundLayerIdx >= 0)
            soPC.FindProperty("groundMask").intValue = 1 << groundLayerIdx;

        // Interactable mask
        SerializedObject soPI = new SerializedObject(pi);
        int interactLayerIdx = LayerMask.NameToLayer("Interactable");
        if (interactLayerIdx >= 0)
            soPI.FindProperty("interactableMask").intValue = 1 << interactLayerIdx;
        soPI.ApplyModifiedProperties();

        soPC.ApplyModifiedProperties();

        // Player material mau xanh
        Material playerMat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        playerMat.color = new Color(0.2f, 0.6f, 1f);
        player.GetComponent<Renderer>().material = playerMat;
        AssetDatabase.CreateAsset(playerMat, "Assets/_Project/Art/Materials/PlayerMaterial.mat");

        Debug.Log("[Setup] Player created.");
        return player;
    }

    private static void SetupCamera(Transform playerTransform)
    {
        // Xoa Main Camera cu trong scene mac dinh neu co
        // (DefaultGameObjects da co san Main Camera)

        // Tao CameraRig
        GameObject cameraRig = new GameObject("CameraRig");
        cameraRig.transform.position = Vector3.zero;

        ThirdPersonCamera tpc = cameraRig.AddComponent<ThirdPersonCamera>();

        // Di chuyen Main Camera vao trong CameraRig
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            mainCam.transform.SetParent(cameraRig.transform);
            mainCam.transform.localPosition = new Vector3(0, 0, -5);
            mainCam.transform.localRotation = Quaternion.Euler(15, 0, 0);
        }

        // Gan target va collision mask vao ThirdPersonCamera
        SerializedObject soTPC = new SerializedObject(tpc);
        soTPC.FindProperty("target").objectReferenceValue = playerTransform;

        int envLayer = LayerMask.NameToLayer("Environment");
        int groundLayer = LayerMask.NameToLayer("Ground");
        int colMask = 0;
        if (envLayer >= 0) colMask |= 1 << envLayer;
        if (groundLayer >= 0) colMask |= 1 << groundLayer;
        soTPC.FindProperty("collisionMask").intValue = colMask;
        soTPC.ApplyModifiedProperties();

        // Gan CameraRig vao PlayerController
        PlayerController pc = GameObject.FindFirstObjectByType<PlayerController>();
        if (pc != null)
        {
            SerializedObject soPC = new SerializedObject(pc);
            soPC.FindProperty("cameraTransform").objectReferenceValue = cameraRig.transform;
            soPC.ApplyModifiedProperties();
        }

        Debug.Log("[Setup] Camera rig created.");
    }

    private static void SetupMobileInput()
    {
        // Canvas
        GameObject canvasGO = new GameObject("MobileInputCanvas");
        UnityEngine.Canvas canvas = canvasGO.AddComponent<UnityEngine.Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        UnityEngine.UI.CanvasScaler scaler = canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
        scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        scaler.dynamicPixelsPerUnit = 3f;
        scaler.referencePixelsPerUnit = 100f;
        canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        // EventSystem
        if (GameObject.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // ── Joystick Area (trai) ──
        GameObject joystickBg = new GameObject("JoystickBg");
        joystickBg.transform.SetParent(canvasGO.transform, false);
        RectTransform jBgRect = joystickBg.AddComponent<RectTransform>();
        jBgRect.anchorMin = new Vector2(0, 0);
        jBgRect.anchorMax = new Vector2(0, 0);
        jBgRect.pivot = new Vector2(0.5f, 0.5f);
        jBgRect.anchoredPosition = new Vector2(180, 180);
        jBgRect.sizeDelta = new Vector2(200, 200);
        UnityEngine.UI.Image jBgImg = joystickBg.AddComponent<UnityEngine.UI.Image>();
        jBgImg.color = new Color(1, 1, 1, 0.15f);

        GameObject joystickHandle = new GameObject("JoystickHandle");
        joystickHandle.transform.SetParent(joystickBg.transform, false);
        RectTransform jHandleRect = joystickHandle.AddComponent<RectTransform>();
        jHandleRect.anchoredPosition = Vector2.zero;
        jHandleRect.sizeDelta = new Vector2(90, 90);
        UnityEngine.UI.Image jHandleImg = joystickHandle.AddComponent<UnityEngine.UI.Image>();
        jHandleImg.color = new Color(1, 1, 1, 0.5f);

        // ── Buttons (phai) ──
        GameObject btnArea = new GameObject("ButtonsArea");
        btnArea.transform.SetParent(canvasGO.transform, false);
        RectTransform btnAreaRect = btnArea.AddComponent<RectTransform>();
        btnAreaRect.anchorMin = new Vector2(1, 0);
        btnAreaRect.anchorMax = new Vector2(1, 0);
        btnAreaRect.pivot = new Vector2(1, 0);
        btnAreaRect.anchoredPosition = new Vector2(-30, 30);
        btnAreaRect.sizeDelta = new Vector2(300, 250);

        GameObject interactBtn = CreateMobileButton(btnArea.transform, "InteractButton", "E", new Vector2(-200, 150));
        GameObject jumpBtn = CreateMobileButton(btnArea.transform, "JumpButton", "JUMP", new Vector2(-80, 150));
        GameObject runBtn = CreateMobileButton(btnArea.transform, "RunButton", "RUN", new Vector2(-140, 40));

        // Interaction Prompt
        GameObject promptGO = new GameObject("InteractionPrompt");
        promptGO.transform.SetParent(canvasGO.transform, false);
        RectTransform promptRect = promptGO.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.5f);
        promptRect.anchorMax = new Vector2(0.5f, 0.5f);
        promptRect.anchoredPosition = new Vector2(0, -150);
        promptRect.sizeDelta = new Vector2(400, 60);
        UnityEngine.UI.Image promptBg = promptGO.AddComponent<UnityEngine.UI.Image>();
        promptBg.color = new Color(0, 0, 0, 0.7f);
        promptGO.SetActive(false);

        // ── MobileInputController ──
        GameObject micGO = new GameObject("MobileInputController");
        MobileInputController mic = micGO.AddComponent<MobileInputController>();
        SerializedObject soMIC = new SerializedObject(mic);
        soMIC.FindProperty("joystickBg").objectReferenceValue = jBgRect;
        soMIC.FindProperty("joystickHandle").objectReferenceValue = jHandleRect;
        soMIC.FindProperty("joystickRadius").floatValue = 60f;
        soMIC.FindProperty("interactButton").objectReferenceValue = interactBtn.GetComponent<UnityEngine.UI.Button>();
        soMIC.FindProperty("jumpButton").objectReferenceValue = jumpBtn.GetComponent<UnityEngine.UI.Button>();
        soMIC.FindProperty("runButton").objectReferenceValue = runBtn.GetComponent<UnityEngine.UI.Button>();
        soMIC.ApplyModifiedProperties();

        // Gan InteractionPrompt vao PlayerInteraction
        PlayerInteraction pi = GameObject.FindFirstObjectByType<PlayerInteraction>();
        if (pi != null)
        {
            SerializedObject soPI = new SerializedObject(pi);
            soPI.FindProperty("interactionPromptUI").objectReferenceValue = promptGO;
            soPI.ApplyModifiedProperties();
        }

        Debug.Log("[Setup] Mobile input created.");
    }

    private static void SetupLighting()
    {
        // Tim Directional Light co san
        Light dirLight = GameObject.FindFirstObjectByType<Light>();
        if (dirLight != null)
        {
            dirLight.color = new Color(0.9f, 0.95f, 1f);
            dirLight.intensity = 1.2f;
            dirLight.transform.rotation = Quaternion.Euler(50, -30, 0);
        }

        // Ambient light: sci-fi blue
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.05f, 0.08f, 0.15f);
        Debug.Log("[Setup] Lighting configured.");
    }

    // ───────────────────────────────────────────────
    // Step 5: Build Settings
    // ───────────────────────────────────────────────
    private static void AddScenesToBuildSettings()
    {
        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene("Assets/_Project/Scenes/MainMenu.unity", true),
            new EditorBuildSettingsScene("Assets/_Project/Scenes/Lab.unity", true),
        };
        EditorBuildSettings.scenes = scenes;
        Debug.Log("[Setup] Build Settings updated: MainMenu(0), Lab(1).");
    }

    // ───────────────────────────────────────────────
    // UI Helpers
    // ───────────────────────────────────────────────
    private static GameObject CreateUIPanel(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.05f, 0.05f, 0.1f, 0.85f);
        return go;
    }

    private static GameObject CreateUIButton(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(300, 60);
        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.1f, 0.4f, 0.8f, 0.9f);
        UnityEngine.UI.Button btn = go.AddComponent<UnityEngine.UI.Button>();

        // Label text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        UnityEngine.UI.Text txt = textGO.AddComponent<UnityEngine.UI.Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 22;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = Color.white;

        return go;
    }

    private static GameObject CreateUIText(Transform parent, string name, string content)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.AddComponent<RectTransform>();
        UnityEngine.UI.Text txt = go.AddComponent<UnityEngine.UI.Text>();
        txt.text = content;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 32;
        txt.fontStyle = FontStyle.Bold;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.color = new Color(0.4f, 0.8f, 1f);
        var ol = go.AddComponent<UnityEngine.UI.Outline>();
        ol.effectColor = new Color(0, 0, 0, 0.85f);
        ol.effectDistance = new Vector2(1.5f, -1.5f);
        return go;
    }

    private static GameObject CreateMobileButton(Transform parent, string name, string label, Vector2 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchoredPosition = pos;
        rt.sizeDelta = new Vector2(100, 100);
        UnityEngine.UI.Image img = go.AddComponent<UnityEngine.UI.Image>();
        img.color = new Color(0.2f, 0.5f, 0.9f, 0.7f);
        go.AddComponent<UnityEngine.UI.Button>();

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        RectTransform trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        UnityEngine.UI.Text txt = textGO.AddComponent<UnityEngine.UI.Text>();
        txt.text = label;
        txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        txt.fontSize = 20;
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
