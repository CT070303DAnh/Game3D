#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Animations;
using System.IO;

/// <summary>
/// Tool tự động nâng cấp nhân vật người chơi sang "Stylized Player Character"
/// trên toàn bộ các Scene (Lab, Level2_Reactor, Level3_Helipad).
/// </summary>
public static class PlayerStylizedUpgrader
{
    private const string SCENE_LAB = "Assets/_Project/Scenes/Lab.unity";
    private const string SCENE_REACTOR = "Assets/_Project/Scenes/Level2_Reactor.unity";
    private const string SCENE_HELIPAD = "Assets/_Project/Scenes/Level3_Helipad.unity";

    private const string PREFAB_PLAYER = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Prefab/Player.prefab";
    private const string FBX_PLAYER = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Models/Player.fbx";
    private const string MAT_PLAYER = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Texturas/Materials/Models_MAT/Player_MAT.mat";

    private const string ANIM_IDLE = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Animations/AnimationsClips/Player/Idle.fbx";
    private const string ANIM_WALK = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Animations/AnimationsClips/Player/Walking.fbx";
    private const string ANIM_RUN = "Assets/StylizedCore/StylizedWoodMonsters/URP/AnimationGallery/Animations/AnimationsClips/Player/Running.fbx";

    private const string CONTROLLER_PATH = "Assets/_Project/Animations/Player/PlayerAnimator.controller";

    [MenuItem("EscapeTheLab/🧍 Áp Dụng Nhân Vật Stylized Player Character", false, 10)]
    public static void UpgradeAllScenesMenu()
    {
        UpgradeAllScenes(true);
    }

    public static void UpgradeAllScenes(bool showDialog)
    {
        Debug.Log("[PlayerStylizedUpgrader] Bắt đầu nâng cấp Stylized Player Character cho toàn bộ Scenes...");

        // 1. Tạo/Cấu hình Animator Controller chuẩn với Blend Tree (Idle, Walk, Run)
        var controller = SetupAnimatorController();
        if (controller == null)
        {
            Debug.LogError("[PlayerStylizedUpgrader] Không thể tạo/cấu hình AnimatorController!");
            return;
        }

        // 2. Lấy Avatar từ Player.fbx
        Avatar playerAvatar = GetPlayerAvatar();
        if (playerAvatar == null)
        {
            Debug.LogError("[PlayerStylizedUpgrader] Không tìm thấy Humanoid Avatar trong Player.fbx!");
            return;
        }

        // 3. Nâng cấp trên từng Scene
        string[] scenes = new string[] { SCENE_LAB, SCENE_REACTOR, SCENE_HELIPAD };
        int upgradedCount = 0;

        foreach (var scenePath in scenes)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogWarning($"[PlayerStylizedUpgrader] Không tìm thấy scene: {scenePath}");
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            bool success = UpgradePlayerInScene(controller, playerAvatar);
            if (success)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                upgradedCount++;
                Debug.Log($"[PlayerStylizedUpgrader] Đã nâng cấp & lưu Scene thành công: {scenePath}");
            }
        }

        AssetDatabase.SaveAssets();

        if (showDialog)
        {
            EditorUtility.DisplayDialog("Stylized Player Character",
                $"Đã áp dụng thành công mô hình Stylized Player Character và bộ Animation (Idle/Walk/Run) cho {upgradedCount} màn chơi!",
                "Tuyệt vời");
        }
    }

    public static AnimatorController SetupAnimatorController()
    {
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(CONTROLLER_PATH);
        if (controller == null)
        {
            string dir = Path.GetDirectoryName(CONTROLLER_PATH);
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            controller = AnimatorController.CreateAnimatorControllerAtPath(CONTROLLER_PATH);
        }

        // Đảm bảo đủ parameters
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "IsGrounded", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "Jump", AnimatorControllerParameterType.Trigger);

        // Load animation clips
        AnimationClip clipIdle = LoadClipFromFbx(ANIM_IDLE, "Idle");
        AnimationClip clipWalk = LoadClipFromFbx(ANIM_WALK, "Walking");
        AnimationClip clipRun = LoadClipFromFbx(ANIM_RUN, "Running");

        if (clipIdle == null || clipWalk == null || clipRun == null)
        {
            Debug.LogError($"[PlayerStylizedUpgrader] Thiếu Animation Clip! Idle: {clipIdle != null}, Walk: {clipWalk != null}, Run: {clipRun != null}");
            return null;
        }

        var rootStateMachine = controller.layers[0].stateMachine;

        // Tìm hoặc tạo Locomotion State
        AnimatorState locomotionState = null;
        foreach (var childState in rootStateMachine.states)
        {
            if (childState.state.name == "Locomotion" || childState.state.name == "Motion" || childState.state.name == "Blend Tree")
            {
                locomotionState = childState.state;
                break;
            }
        }

        BlendTree blendTree = null;
        if (locomotionState == null)
        {
            locomotionState = controller.CreateBlendTreeInController("Locomotion", out blendTree);
        }
        else
        {
            blendTree = locomotionState.motion as BlendTree;
            if (blendTree == null)
            {
                blendTree = new BlendTree { name = "LocomotionBlend" };
                AssetDatabase.AddObjectToAsset(blendTree, controller);
                locomotionState.motion = blendTree;
            }
        }

        locomotionState.name = "Locomotion";
        blendTree.blendType = BlendTreeType.Simple1D;
        blendTree.blendParameter = "Speed";
        blendTree.useAutomaticThresholds = false;

        // Xóa các motion cũ trong blend tree
        while (blendTree.children.Length > 0)
        {
            blendTree.RemoveChild(0);
        }

        // Thêm Idle (0.0), Walk (0.6), Run (1.0)
        blendTree.AddChild(clipIdle, 0.0f);
        blendTree.AddChild(clipWalk, 0.6f);
        blendTree.AddChild(clipRun, 1.0f);

        rootStateMachine.defaultState = locomotionState;

        EditorUtility.SetDirty(blendTree);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        Debug.Log("[PlayerStylizedUpgrader] Đã cấu hình xong Animator Controller với 1D Blend Tree!");
        return controller;
    }

    private static void EnsureParameter(AnimatorController controller, string paramName, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == paramName) return;
        }
        controller.AddParameter(paramName, type);
    }

    private static AnimationClip LoadClipFromFbx(string fbxPath, string preferredName)
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
        if (assets == null || assets.Length == 0) return null;

        AnimationClip fallback = null;
        foreach (var obj in assets)
        {
            if (obj is AnimationClip clip && !clip.name.Contains("__preview__"))
            {
                if (clip.name.Equals(preferredName, System.StringComparison.OrdinalIgnoreCase))
                    return clip;
                if (fallback == null) fallback = clip;
            }
        }
        return fallback;
    }

    public static Avatar GetPlayerAvatar()
    {
        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(FBX_PLAYER);
        foreach (var obj in assets)
        {
            if (obj is Avatar av) return av;
        }
        return null;
    }

    public static bool UpgradePlayerInScene(AnimatorController controller, Avatar avatar)
    {
        GameObject playerGO = GameObject.FindGameObjectWithTag("Player");
        if (playerGO == null) playerGO = GameObject.Find("Player");
        if (playerGO == null)
        {
            Debug.LogWarning("[PlayerStylizedUpgrader] Không tìm thấy Player GameObject trong scene!");
            return false;
        }

        // 1. Kiểm tra và dọn dẹp các model hiển thị cũ (nhưng giữ lại GroundCheck và Camera)
        Transform existingStylized = playerGO.transform.Find("Model_StylizedPlayer");
        if (existingStylized != null)
        {
            Object.DestroyImmediate(existingStylized.gameObject);
        }

        // Xóa các model visual con cũ (Model_PBRCharacter, Capsule mesh, etc.)
        for (int i = playerGO.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = playerGO.transform.GetChild(i);
            string cName = child.name.ToLower();
            if (cName.Contains("model") || cName.Contains("character") || cName.Contains("body") || cName.Contains("mesh"))
            {
                Debug.Log($"[PlayerStylizedUpgrader] Xóa model cũ: {child.name}");
                Object.DestroyImmediate(child.gameObject);
                continue;
            }

            // Nếu child có MeshRenderer / SkinnedMeshRenderer mà không phải Camera/Light/GroundCheck
            if ((child.GetComponent<MeshRenderer>() != null || child.GetComponent<SkinnedMeshRenderer>() != null) 
                && !cName.Contains("ground") && !cName.Contains("cam"))
            {
                Debug.Log($"[PlayerStylizedUpgrader] Xóa mesh con cũ: {child.name}");
                Object.DestroyImmediate(child.gameObject);
            }
        }

        // Xóa MeshFilter/MeshRenderer trên chính root Player nếu trước đó dùng Primitive Capsule
        var rootMF = playerGO.GetComponent<MeshFilter>();
        if (rootMF != null) Object.DestroyImmediate(rootMF);
        var rootMR = playerGO.GetComponent<MeshRenderer>();
        if (rootMR != null) Object.DestroyImmediate(rootMR);

        // 2. Instantiate Player.prefab mới làm con của Player
        GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PREFAB_PLAYER);
        if (playerPrefab == null)
        {
            Debug.LogError($"[PlayerStylizedUpgrader] Không tìm thấy prefab tại {PREFAB_PLAYER}");
            return false;
        }

        GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab, playerGO.transform);
        if (modelInstance != null && PrefabUtility.IsPartOfPrefabInstance(modelInstance))
        {
            PrefabUtility.UnpackPrefabInstance(modelInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        }
        modelInstance.name = "Model_StylizedPlayer";
        modelInstance.transform.localPosition = Vector3.zero;
        modelInstance.transform.localRotation = Quaternion.identity;
        modelInstance.transform.localScale = Vector3.one;

        // Xóa các script thừa (demo character controller, etc.) trên prefab con
        var childMonoBehaviours = modelInstance.GetComponentsInChildren<MonoBehaviour>(true);
        foreach (var mb in childMonoBehaviours)
        {
            if (mb != null)
            {
                string typeName = mb.GetType().Name;
                if (typeName != "PlayerController" && typeName != "PlayerHealth" && typeName != "PlayerInteraction")
                {
                    Object.DestroyImmediate(mb);
                }
            }
        }

        // Xóa Collider con thừa (CharacterController trên root đã quản lý va chạm)
        var childColliders = modelInstance.GetComponentsInChildren<Collider>(true);
        foreach (var col in childColliders)
        {
            Object.DestroyImmediate(col);
        }

        // Xóa Animator trên model con để Animator trên root làm chủ quản
        var childAnimators = modelInstance.GetComponentsInChildren<Animator>(true);
        foreach (var a in childAnimators)
        {
            Object.DestroyImmediate(a);
        }

        // Gán Material URP chuẩn cho SkinnedMeshRenderer nếu chưa có
        Material playerMat = AssetDatabase.LoadAssetAtPath<Material>(MAT_PLAYER);
        if (playerMat != null)
        {
            var smrs = modelInstance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
            foreach (var smr in smrs)
            {
                smr.sharedMaterial = playerMat;
            }
        }

        // 3. Cấu hình CharacterController trên root
        CharacterController cc = playerGO.GetComponent<CharacterController>();
        if (cc == null) cc = playerGO.AddComponent<CharacterController>();
        cc.height = 1.83f;
        cc.center = new Vector3(0f, 0.915f, 0f);
        cc.radius = 0.35f;
        cc.stepOffset = 0.3f;
        cc.slopeLimit = 45f;
        cc.minMoveDistance = 0.001f;

        // 4. Cấu hình Animator trên root Player
        Animator animator = playerGO.GetComponent<Animator>();
        if (animator == null) animator = playerGO.AddComponent<Animator>();
        animator.avatar = avatar;
        animator.runtimeAnimatorController = controller;
        animator.applyRootMotion = false;

        // 5. Cấu hình GroundCheck & PlayerController
        Transform groundCheck = playerGO.transform.Find("GroundCheck");
        if (groundCheck == null)
        {
            GameObject gcGO = new GameObject("GroundCheck");
            gcGO.transform.SetParent(playerGO.transform);
            gcGO.transform.localPosition = new Vector3(0f, 0.05f, 0f);
            groundCheck = gcGO.transform;
        }

        PlayerController pc = playerGO.GetComponent<PlayerController>();
        if (pc != null)
        {
            SerializedObject so = new SerializedObject(pc);
            var gcProp = so.FindProperty("groundCheck");
            if (gcProp != null) gcProp.objectReferenceValue = groundCheck;

            var walkProp = so.FindProperty("walkSpeed");
            if (walkProp != null) walkProp.floatValue = 3.5f;

            var runProp = so.FindProperty("runSpeed");
            if (runProp != null) runProp.floatValue = 5.5f;

            so.ApplyModifiedProperties();
        }

        EditorUtility.SetDirty(playerGO);
        return true;
    }
}
#endif
