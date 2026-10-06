using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;
using Unity.AI.Navigation;

namespace EscapeTheLab.EditorTools
{
    public static class AssetUpgradeTools
    {
        private const string PBR_PREFAB_PATH = "Assets/SciFiWarriorPBRHPPolyart/Prefabs/PBRCharacter.prefab";

        [MenuItem("EscapeTheLab/🤖 Nâng Cấp Robot Sang Chiến Binh Sci-Fi PBR 3D", priority = 20)]
        public static void UpgradeRobotsInCurrentScene()
        {
            // 1. Nâng cấp shader vật liệu sang URP trước để không bị hồng tím
            FixAllShadersToURP();

            // 2. Tải Prefab Robot PBR
            var robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PBR_PREFAB_PATH);
            if (robotPrefab == null)
            {
                // Thử tìm bất kỳ prefab character nào trong SciFiWarrior
                string[] guids = AssetDatabase.FindAssets("t:Prefab PBRCharacter", new[] { "Assets/SciFiWarriorPBRHPPolyart" });
                if (guids.Length > 0)
                {
                    string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                    robotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                }
            }

            if (robotPrefab == null)
            {
                EditorUtility.DisplayDialog("Lỗi", 
                    "Không tìm thấy Prefab PBRCharacter tại 'Assets/SciFiWarriorPBRHPPolyart/Prefabs/PBRCharacter.prefab'!\nHãy kiểm tra xem gói đã được import chưa.", 
                    "OK");
                return;
            }

            // 3. Tìm tất cả Robot trong Scene
            var allRobots = Object.FindObjectsByType<RobotAI>(FindObjectsSortMode.None);
            if (allRobots.Length == 0)
            {
                // Thử tìm theo tên GameObject
                var rList = new List<GameObject>();
                foreach (var name in new[] { "Robot", "Robot_1", "Robot_2" })
                {
                    var go = GameObject.Find(name);
                    if (go != null) rList.Add(go);
                }

                if (rList.Count == 0)
                {
                    EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy Robot nào trong Scene hiện tại!", "OK");
                    return;
                }

                foreach (var r in rList)
                {
                    ApplyPBRModelToRobot(r, robotPrefab);
                }
            }
            else
            {
                foreach (var ai in allRobots)
                {
                    ApplyPBRModelToRobot(ai.gameObject, robotPrefab);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("Hoàn tất!", 
                $"Đã nâng cấp thành công Robot trong Scene sang mô hình 3D Sci-Fi Warrior PBR chất lượng cao!\n\nNhớ nhấn Ctrl + S để lưu Scene.", 
                "Tuyệt vời");
        }

        [MenuItem("EscapeTheLab/🚁 1-Click Nâng Cấp Trực Thăng 3D Quân Sự (OH-58D Kiowa)", priority = 21)]
        public static void UpgradeHelicopterInCurrentScene()
        {
            FixAllShadersToURP();

            var heliPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Helicopter/Resources/Models/HelicopterModel/OH-58D.fbx");
            if (heliPrefab == null)
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file mô hình OH-58D.fbx tại 'Assets/Helicopter/Resources/Models/HelicopterModel/OH-58D.fbx'!", "OK");
                return;
            }

            var heliRoot = GameObject.Find("RescueHelicopter");
            if (heliRoot == null)
            {
                EditorUtility.DisplayDialog("Thông báo", "Không tìm thấy GameObject 'RescueHelicopter' trong Scene hiện tại!\nHãy mở Màn 3 (Level 3: Helipad) qua menu: EscapeTheLab > Mở Scene Chơi > Màn 3.", "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(heliRoot, "Upgrade Helicopter Model");

            // 1. Dọn dẹp triệt để TẤT CẢ các bộ phận khối hình học primitive cũ của trực thăng
            // Xóa mọi child GameObject trừ DownwashDust và HeliSign
            var childrenToDestroy = new List<GameObject>();
            for (int i = 0; i < heliRoot.transform.childCount; i++)
            {
                var child = heliRoot.transform.GetChild(i).gameObject;
                string cName = child.name;
                if (cName == "DownwashDust" || cName == "HeliSign")
                {
                    continue;
                }
                childrenToDestroy.Add(child);
            }

            foreach (var go in childrenToDestroy)
            {
                Undo.DestroyObjectImmediate(go);
            }

            // 2. Instantiate mô hình trực thăng 3D quân sự OH-58D với kích thước lớn chuẩn tỉ lệ
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(heliPrefab, heliRoot.transform);
            inst.name = "Model_OH58D";
            inst.transform.localPosition = new Vector3(0, 0.05f, 0);
            inst.transform.localRotation = Quaternion.Euler(0, 180f, 0); // Quay đầu đón người chơi
            inst.transform.localScale = Vector3.one * 2.3f; // Tăng kích cỡ to gấp 2.3 lần chuẩn tỉ lệ quân sự thực tế

            // Xóa AudioListener / Camera trên model nếu có
            foreach (var cam in inst.GetComponentsInChildren<Camera>(true)) Object.DestroyImmediate(cam.gameObject);
            foreach (var al in inst.GetComponentsInChildren<AudioListener>(true)) Object.DestroyImmediate(al);

            // 3. Tìm kiếm cánh quạt chính và cánh quạt đuôi (Main Rotor & Tail Rotor)
            Transform mainRotorNode = null;
            Transform tailRotorNode = null;

            var childTransforms = inst.GetComponentsInChildren<Transform>(true);
            foreach (var t in childTransforms)
            {
                if (t == inst.transform) continue;
                string lower = t.name.ToLower();
                if (lower.Contains("rotar") || lower == "rotar")
                {
                    mainRotorNode = t;
                }
                else if (lower.Contains("screw") || lower.Contains("tailrot") || lower.Contains("backrot"))
                {
                    tailRotorNode = t;
                }
                else if (lower.Contains("rotor") || lower.Contains("blade") || lower.Contains("propeller"))
                {
                    if (lower.Contains("tail") || lower.Contains("rear") || lower.Contains("back") || lower.Contains("sub"))
                        tailRotorNode = t;
                    else if (mainRotorNode == null)
                        mainRotorNode = t;
                }
            }

            // Fallback nếu tên node trong FBX là dạng ID
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

            // 4. Tạo hoặc định vị lại Đèn Pha Rọi Sàn (SearchLight) ở mũi máy bay
            var slGO = heliRoot.transform.Find("SearchLight");
            Light searchLightComp = null;
            if (slGO == null)
            {
                var newSL = new GameObject("SearchLight");
                newSL.transform.SetParent(heliRoot.transform, false);
                searchLightComp = newSL.AddComponent<Light>();
                searchLightComp.type = LightType.Spot;
                searchLightComp.color = new Color(0.95f, 0.98f, 1f);
                searchLightComp.range = 35f;
                searchLightComp.spotAngle = 65f;
                searchLightComp.intensity = 3.5f;
                slGO = newSL.transform;
            }
            else
            {
                searchLightComp = slGO.GetComponent<Light>();
            }
            slGO.localPosition = new Vector3(0, 1.3f, 4.0f);
            slGO.localRotation = Quaternion.Euler(30f, 0, 0);

            // 5. Cấu hình HelicopterController
            var ctrl = heliRoot.GetComponent<HelicopterController>();
            if (ctrl != null)
            {
                var so = new SerializedObject(ctrl);
                if (mainRotorNode != null) so.FindProperty("mainRotor").objectReferenceValue = mainRotorNode;
                if (tailRotorNode != null) so.FindProperty("tailRotor").objectReferenceValue = tailRotorNode;
                if (searchLightComp != null) so.FindProperty("searchLight").objectReferenceValue = searchLightComp;

                var dustGO = heliRoot.transform.Find("DownwashDust");
                if (dustGO != null) so.FindProperty("downwashDust").objectReferenceValue = dustGO.GetComponent<ParticleSystem>();

                // Gán âm thanh trực thăng
                var audioClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Helicopter/Resources/Audio/Helicopter.wav");
                var aud = heliRoot.GetComponent<AudioSource>();
                if (aud == null) aud = heliRoot.AddComponent<AudioSource>();
                if (audioClip != null)
                {
                    aud.clip = audioClip;
                    aud.loop = true;
                    aud.playOnAwake = false;
                    aud.spatialBlend = 1f;
                    aud.minDistance = 5f;
                    aud.maxDistance = 60f;
                }

                so.ApplyModifiedProperties();
            }

            // 6. Cập nhật BoxCollider bao quanh máy bay to lớn để người chơi dễ tương tác
            var col = heliRoot.GetComponent<BoxCollider>();
            if (col == null) col = heliRoot.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.center = new Vector3(0, 2.2f, 0);
            col.size = new Vector3(8.0f, 5.5f, 15.0f);

            // 7. Định vị lại biển báo hướng dẫn đặt phía trước mũi máy bay
            var sign = heliRoot.transform.Find("HeliSign");
            if (sign != null)
            {
                sign.localPosition = new Vector3(0, 0.45f, 5.8f);
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            EditorUtility.DisplayDialog("Thành công!", 
                "Đã nâng cấp trực thăng cứu hộ sang mô hình 3D quân sự OH-58D Kiowa to lớn & sạch sẽ!\n\n" +
                "• Đã xóa sạch 100% tất cả các khối primitive và đuôi cũ còn sót lại\n" +
                "• Mô hình trực thăng được phóng to gấp 2.3 lần chuẩn tỉ lệ quân sự thực tế\n" +
                "• Càng đáp tiếp đất hoàn hảo trên sân bay Helipad\n" +
                "• Nâng cấp vật liệu URP Lit sắc nét\n" +
                "• Đã kết nối âm thanh Helicopter.wav và cơ chế cất cánh tẩu thoát\n\n" +
                "Nhớ nhấn Ctrl + S để lưu Scene lại!", 
                "Tuyệt vời");
        }

        private static void ApplyPBRModelToRobot(GameObject robot, GameObject prefab)
        {
            Undo.RegisterFullObjectHierarchyUndo(robot, "Upgrade Robot to PBR");

            // 1. Tắt MeshRenderer của con Capsule ban đầu (giữ lại Collider để phát hiện va chạm)
            var mr = robot.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // 2. Xóa các khối primitive hình hộp thô sơ cũ
            string[] primitivePartNames = { "RobotVisor", "HeadRadarMast", "HeadRadarDish", "RobotBlasterGun" };
            foreach (var partName in primitivePartNames)
            {
                var part = robot.transform.Find(partName);
                if (part != null)
                {
                    Undo.DestroyObjectImmediate(part.gameObject);
                }
            }

            // 3. Xóa model PBR cũ nếu đã từng gắn trước đó (để tránh trùng lặp khi chạy lại)
            var oldModel = robot.transform.Find("Model_PBRCharacter");
            if (oldModel != null)
            {
                Undo.DestroyObjectImmediate(oldModel.gameObject);
            }

            // 4. Instantiate model 3D mới làm con của Robot
            var modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, robot.transform);
            modelInstance.name = "Model_PBRCharacter";
            modelInstance.transform.localPosition = new Vector3(0, -1.0f, 0); // Đưa chân chạm đất (Capsule cao 2m, tâm ở 0)
            modelInstance.transform.localRotation = Quaternion.identity;
            modelInstance.transform.localScale = Vector3.one;

            // Xóa AudioListener trên model nếu có (tránh lỗi duplicate audio listener)
            var listeners = modelInstance.GetComponentsInChildren<AudioListener>(true);
            foreach (var l in listeners) Object.DestroyImmediate(l);

            // 5. Cấu hình Animator
            var modelAnimator = modelInstance.GetComponent<Animator>();
            if (modelAnimator == null) modelAnimator = modelInstance.GetComponentInChildren<Animator>();

            // 6. Tìm nòng súng để gắn tia Laser & Muzzle Flash
            Transform muzzleTransform = null;
            var rifle = modelInstance.transform.Find("AssaultRifle");
            if (rifle == null)
            {
                // Thử tìm đệ quy
                foreach (var t in modelInstance.GetComponentsInChildren<Transform>(true))
                {
                    if (t.name.Contains("AssaultRifle") || t.name.Contains("Rifle") || t.name.Contains("Gun"))
                    {
                        rifle = t;
                        break;
                    }
                }
            }

            if (rifle != null)
            {
                var muzzleGO = rifle.Find("PBR_Muzzle")?.gameObject;
                if (muzzleGO == null)
                {
                    muzzleGO = new GameObject("PBR_Muzzle");
                    muzzleGO.transform.SetParent(rifle, false);
                    muzzleGO.transform.localPosition = new Vector3(0, 0.05f, 0.65f); // Đầu nòng súng
                    muzzleGO.transform.localRotation = Quaternion.identity;
                }
                muzzleTransform = muzzleGO.transform;
            }

            // 7. Cập nhật vị trí Mắt (EyePoint) & Đèn rọi (SensorLight) lên độ cao đầu người (1.6m)
            var eye = robot.transform.Find("EyePoint");
            if (eye != null)
            {
                eye.localPosition = new Vector3(0, 1.6f, 0.35f);
            }

            var sensor = robot.transform.Find("SensorLight");
            if (sensor != null)
            {
                sensor.localPosition = new Vector3(0, 1.6f, 0.35f);
            }

            // 8. Liên kết lại vào RobotAI
            var ai = robot.GetComponent<RobotAI>();
            if (ai != null)
            {
                var so = new SerializedObject(ai);
                if (muzzleTransform != null)
                {
                    so.FindProperty("gunMuzzle").objectReferenceValue = muzzleTransform;

                    // Tạo hoặc gán lại LineRenderer & Muzzle Flash Light nếu cần
                    var lineRend = robot.GetComponentInChildren<LineRenderer>(true);
                    if (lineRend != null) so.FindProperty("laserBeamLine").objectReferenceValue = lineRend;

                    var muzzleFlash = muzzleTransform.GetComponentInChildren<Light>(true);
                    if (muzzleFlash == null)
                    {
                        var flashGO = new GameObject("MuzzleFlashLight");
                        flashGO.transform.SetParent(muzzleTransform, false);
                        var fl = flashGO.AddComponent<Light>();
                        fl.type = LightType.Point;
                        fl.color = new Color(1f, 0.35f, 0.1f);
                        fl.intensity = 4.0f;
                        fl.range = 3.5f;
                        fl.enabled = false;
                        so.FindProperty("muzzleLight").objectReferenceValue = fl;
                    }
                    else
                    {
                        so.FindProperty("muzzleLight").objectReferenceValue = muzzleFlash;
                    }
                }
                so.ApplyModifiedProperties();
            }

            Debug.Log($"[AssetUpgradeTools] ✅ Đã nâng cấp Robot '{robot.name}' sang mô hình Sci-Fi Warrior PBR thành công!");
        }

        [MenuItem("EscapeTheLab/🎨 Sửa Lỗi Vật Liệu Hồng (Convert Toàn Bộ Materials Sang URP Lit)", priority = 21)]
        public static void FixAllShadersToURP()
        {
            var urpLitShader = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLitShader == null)
            {
                Debug.LogError("[AssetUpgradeTools] Không tìm thấy shader 'Universal Render Pipeline/Lit'!");
                return;
            }

            string[] searchFolders = {
                "Assets/SciFiWarriorPBRHPPolyart",
                "Assets/Sci-Fi Styled Modular Pack",
                "Assets/Helicopter",
                "Assets/Phoenix3D"
            };

            int convertedCount = 0;
            string[] matGuids = AssetDatabase.FindAssets("t:Material", searchFolders);

            foreach (var guid in matGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null) continue;

                // Nếu shader là Standard cũ hoặc Error/Hidden/InternalError
                if (mat.shader == null || mat.shader.name.Contains("Standard") || mat.shader.name.Contains("Error"))
                {
                    ConvertMaterialToURPLit(mat, urpLitShader);
                    EditorUtility.SetDirty(mat);
                    convertedCount++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"[AssetUpgradeTools] ✨ Đã chuyển đổi {convertedCount} vật liệu sang Universal Render Pipeline/Lit!");
        }

        private static void ConvertMaterialToURPLit(Material mat, Shader urpLitShader)
        {
            // Lưu lại các texture và màu gốc
            Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
            Color mainColor = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
            Texture bumpMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
            Texture metallicMap = mat.HasProperty("_MetallicGlossMap") ? mat.GetTexture("_MetallicGlossMap") : null;
            Texture occlusionMap = mat.HasProperty("_OcclusionMap") ? mat.GetTexture("_OcclusionMap") : null;
            Texture emissionMap = mat.HasProperty("_EmissionMap") ? mat.GetTexture("_EmissionMap") : null;
            Color emissionColor = mat.HasProperty("_EmissionColor") ? mat.GetColor("_EmissionColor") : Color.black;
            float glossiness = mat.HasProperty("_Glossiness") ? mat.GetFloat("_Glossiness") : 0.5f;
            float metallic = mat.HasProperty("_Metallic") ? mat.GetFloat("_Metallic") : 0f;

            // Gán shader URP Lit
            mat.shader = urpLitShader;

            // Ánh xạ sang các thuộc tính chuẩn URP Lit
            if (mainTex != null) mat.SetTexture("_BaseMap", mainTex);
            mat.SetColor("_BaseColor", mainColor);

            if (bumpMap != null)
            {
                mat.SetTexture("_BumpMap", bumpMap);
                mat.EnableKeyword("_NORMALMAP");
            }

            if (metallicMap != null)
            {
                mat.SetTexture("_MetallicGlossMap", metallicMap);
                mat.EnableKeyword("_METALLICSPECGLOSSMAP");
            }
            else
            {
                mat.SetFloat("_Metallic", metallic);
            }

            mat.SetFloat("_Smoothness", glossiness);

            if (occlusionMap != null)
            {
                mat.SetTexture("_OcclusionMap", occlusionMap);
            }

            if (emissionMap != null || emissionColor.maxColorComponent > 0.01f)
            {
                if (emissionMap != null) mat.SetTexture("_EmissionMap", emissionMap);
                mat.SetColor("_EmissionColor", emissionColor);
                mat.EnableKeyword("_EMISSION");
            }
        }

        [MenuItem("EscapeTheLab/📦 1-Click Nâng Cấp Toàn Bộ Đạo Cụ (Máy Phát Điện, Pin, Cầu Chì, Terminal, Thùng)", priority = 22)]
        public static void UpgradeAllPropsInCurrentScene()
        {
            FixAllShadersToURP();

            int generatorCount = UpgradeGenerators();
            int batteryCount = UpgradeBatteries();
            int fuseCount = UpgradeFuses();
            int jetFuelCount = UpgradeJetFuel();
            int consoleCount = UpgradeTerminalsAndConsoles();
            int crateCount = UpgradeCrates();

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            string summary = "Đã nâng cấp đạo cụ thành công bằng các Model Sci-Fi PBR:\n" +
                $"⚡ Máy phát điện (Generator): {generatorCount}\n" +
                $"🔋 Pin năng lượng (Battery): {batteryCount}\n" +
                $"🔌 Cầu chì / Lõi tụ điện (Capacitor/Fuse): {fuseCount}\n" +
                $"🛢️ Thùng nhiên liệu máy bay (JetFuel): {jetFuelCount}\n" +
                $"💻 Trạm máy tính & Console (Terminal): {consoleCount}\n" +
                $"📦 Thùng hàng công nghiệp Sci-Fi: {crateCount}\n\n" +
                "Nhớ nhấn Ctrl + S để lưu Scene lại!";

            EditorUtility.DisplayDialog("Hoàn tất nâng cấp đạo cụ!", summary, "Tuyệt vời");
        }

        private static int UpgradeGenerators()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab");
            if (prefab == null) return 0;

            int count = 0;
            var allGenerators = Object.FindObjectsByType<Generator>(FindObjectsSortMode.None);
            var targets = new List<GameObject>();
            foreach (var g in allGenerators) targets.Add(g.gameObject);

            foreach (var name in new[] { "Generator", "Generator_Machine", "EmergencyGenerator" })
            {
                var go = GameObject.Find(name);
                if (go != null && !targets.Contains(go)) targets.Add(go);
            }

            foreach (var go in targets)
            {
                Undo.RegisterFullObjectHierarchyUndo(go, "Upgrade Generator");
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;

                // Xóa mô hình cũ nếu có
                var oldM = go.transform.Find("Model_SciFiGenerator");
                if (oldM != null) Undo.DestroyObjectImmediate(oldM.gameObject);

                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, go.transform);
                inst.name = "Model_SciFiGenerator";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one * 0.95f;
                count++;
            }
            return count;
        }

        private static int UpgradeBatteries()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery.prefab");
            if (prefab == null) return 0;

            int count = 0;
            var allPickups = Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
            foreach (var p in allPickups)
            {
                var so = new SerializedObject(p);
                var typeProp = so.FindProperty("itemType");
                if (typeProp != null && typeProp.enumValueIndex == (int)PickupItem.ItemType.Battery)
                {
                    Undo.RegisterFullObjectHierarchyUndo(p.gameObject, "Upgrade Battery");
                    var mr = p.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;

                    var oldM = p.transform.Find("Model_SciFiBattery");
                    if (oldM != null) Undo.DestroyObjectImmediate(oldM.gameObject);

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p.transform);
                    inst.name = "Model_SciFiBattery";
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one * 0.35f;
                    count++;
                }
            }
            return count;
        }

        private static int UpgradeFuses()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab");
            if (prefab == null) return 0;

            int count = 0;
            var allPickups = Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
            foreach (var p in allPickups)
            {
                var so = new SerializedObject(p);
                var typeProp = so.FindProperty("itemType");
                if (typeProp != null && typeProp.enumValueIndex == (int)PickupItem.ItemType.Fuse)
                {
                    Undo.RegisterFullObjectHierarchyUndo(p.gameObject, "Upgrade Fuse");
                    var mr = p.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;

                    var oldM = p.transform.Find("Model_SciFiFuse");
                    if (oldM != null) Undo.DestroyObjectImmediate(oldM.gameObject);

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p.transform);
                    inst.name = "Model_SciFiFuse";
                    inst.transform.localPosition = new Vector3(0, -0.1f, 0);
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one * 0.28f;
                    count++;
                }
            }
            return count;
        }

        private static int UpgradeJetFuel()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab");
            if (prefab == null)
                prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_big.prefab");
            if (prefab == null) return 0;

            int count = 0;
            var allPickups = Object.FindObjectsByType<PickupItem>(FindObjectsSortMode.None);
            foreach (var p in allPickups)
            {
                var so = new SerializedObject(p);
                var typeProp = so.FindProperty("itemType");
                if (typeProp != null && typeProp.enumValueIndex == (int)PickupItem.ItemType.JetFuel)
                {
                    Undo.RegisterFullObjectHierarchyUndo(p.gameObject, "Upgrade JetFuel");
                    var mr = p.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;

                    var oldM = p.transform.Find("Model_SciFiJetFuel");
                    if (oldM != null) Undo.DestroyObjectImmediate(oldM.gameObject);

                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, p.transform);
                    inst.name = "Model_SciFiJetFuel";
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one * 0.65f;
                    count++;
                }
            }
            return count;
        }

        private static int UpgradeTerminalsAndConsoles()
        {
            var stationPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab");
            var consolePrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console.prefab");
            if (stationPrefab == null && consolePrefab == null) return 0;

            int count = 0;
            var allTerminals = Object.FindObjectsByType<Terminal>(FindObjectsSortMode.None);
            var targets = new List<GameObject>();
            foreach (var t in allTerminals) targets.Add(t.gameObject);

            foreach (var name in new[] { "Terminal", "Terminal_1", "Terminal_2", "RadarControlConsole", "RadarStation" })
            {
                var go = GameObject.Find(name);
                if (go != null && !targets.Contains(go)) targets.Add(go);
            }

            foreach (var go in targets)
            {
                Undo.RegisterFullObjectHierarchyUndo(go, "Upgrade Terminal/Console");
                var mr = go.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;

                var oldM = go.transform.Find("Model_SciFiConsole");
                if (oldM != null) Undo.DestroyObjectImmediate(oldM.gameObject);

                var prefabToUse = go.name.Contains("Radar") ? (consolePrefab ?? stationPrefab) : (stationPrefab ?? consolePrefab);
                var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, go.transform);
                inst.name = "Model_SciFiConsole";
                inst.transform.localPosition = Vector3.zero;
                inst.transform.localRotation = Quaternion.identity;
                inst.transform.localScale = Vector3.one * 0.85f;
                count++;
            }
            return count;
        }

        private static int UpgradeCrates()
        {
            var bigPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab");
            var smallPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab");
            if (bigPrefab == null && smallPrefab == null) return 0;

            int count = 0;
            var allTransforms = Object.FindObjectsByType<Transform>(FindObjectsSortMode.None);
            foreach (var t in allTransforms)
            {
                string n = t.name.ToLower();
                if (n.StartsWith("crate") || n.StartsWith("woodencrate") || n.StartsWith("storagebox") || n.Contains("box_crate"))
                {
                    Undo.RegisterFullObjectHierarchyUndo(t.gameObject, "Upgrade Crate");
                    var mr = t.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;

                    var oldM = t.Find("Model_SciFiContainer");
                    if (oldM != null) Undo.DestroyObjectImmediate(oldM.gameObject);

                    var prefabToUse = (count % 2 == 0 && bigPrefab != null) ? bigPrefab : smallPrefab;
                    var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefabToUse, t);
                    inst.name = "Model_SciFiContainer";
                    inst.transform.localPosition = Vector3.zero;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;
                    count++;
                }
            }
            return count;
        }

        [MenuItem("EscapeTheLab/🧱 1-Click Thay Texture Tường P3D (Scene Hiện Tại)", priority = 22)]
        public static void UpgradeCurrentSceneBuildingTexturesP3D()
        {
            FixAllShadersToURP();

            var activeScene = EditorSceneManager.GetActiveScene();
            int count = ApplyP3DWallTexturesToScene(activeScene);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            EditorUtility.DisplayDialog("Hoàn tất!",
                $"Đã thay thế và nâng cấp thành công {count} bề mặt công trình/tường trong Scene '{activeScene.name}' sang texture P3D Outdoor Wall Tile PBR cao cấp!\n\n" +
                "• Tòa nhà sân đỗ trực thăng: Ốp tấm facade kiến trúc cao cấp (T11 Cool Gray Architectural Tile)\n" +
                "• Tháp điều khiển & Trạm radar: Ốp đá đen kỹ thuật quân sự (T16 Tactical Anthracite Tile)\n" +
                "• Kho tiếp liệu & Hầm nhiên liệu: Ốp bê tông khối công nghiệp chống nổ (T09 Heavy Industrial Block)\n" +
                "• Buồng thang máy: Ốp đá kiến trúc kiên cố (T07 Smooth Stone Panel)\n" +
                "• Lan can & Tường bao quanh: Ốp đá phiến đen bảo vệ (T04 Dark Slate Tile)\n" +
                "• Tự động tính toán UV Tiling chuẩn tỉ lệ thực tế, vân gạch sắc nét không bị vỡ/kéo giãn!\n\n" +
                "Scene đã được tự động lưu lại (Ctrl + S).",
                "Tuyệt vời");
        }

        [MenuItem("EscapeTheLab/🏰 1-Click Thay Texture Tường P3D (Cả 3 Màn: Lab, Reactor, Helipad)", priority = 23)]
        public static void UpgradeAllBuildingTexturesP3D()
        {
            FixAllShadersToURP();

            string[] scenePaths = new[]
            {
                "Assets/_Project/Scenes/Lab.unity",
                "Assets/_Project/Scenes/Level2_Reactor.unity",
                "Assets/_Project/Scenes/Level3_Helipad.unity"
            };

            int totalUpgraded = 0;
            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            foreach (var path in scenePaths)
            {
                if (!System.IO.File.Exists(path)) continue;
                var sc = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int count = ApplyP3DWallTexturesToScene(sc);
                EditorSceneManager.MarkSceneDirty(sc);
                EditorSceneManager.SaveScene(sc);
                totalUpgraded += count;
            }

            if (!string.IsNullOrEmpty(currentScenePath) && System.IO.File.Exists(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.DisplayDialog("Hoàn tất toàn diện!",
                $"Đã nâng cấp toàn bộ 3 Màn chơi ({totalUpgraded} bề mặt tường/công trình) sang texture P3D Outdoor Wall Tile PBR chất lượng cao nhất!\n\n" +
                "• Màn 1 (Lab): Gạch phòng lab vô trùng (T01), phòng máy (T09), server room (T16)\n" +
                "• Màn 2 (Reactor): Bê tông chống bức xạ (T09), nhà tuabin (T14), trạm điều hành (T01)\n" +
                "• Màn 3 (Helipad): Tòa nhà sân đỗ (T11), Tháp radar (T16), Kho xăng (T09), Buồng thang máy (T07)\n\n" +
                "Tất cả các Scene đã được lưu hoàn tất!",
                "Tuyệt vời");
        }

        public static int ApplyP3DWallTexturesToScene(Scene scene)
        {
            // 1. Đảm bảo thư mục lưu Applied Materials tồn tại
            string appliedDir = "Assets/Phoenix3D/Materials/Applied";
            if (!AssetDatabase.IsValidFolder("Assets/Phoenix3D/Materials/Applied"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Phoenix3D/Materials"))
                {
                    if (!AssetDatabase.IsValidFolder("Assets/Phoenix3D"))
                        AssetDatabase.CreateFolder("Assets", "Phoenix3D");
                    AssetDatabase.CreateFolder("Assets/Phoenix3D", "Materials");
                }
                AssetDatabase.CreateFolder("Assets/Phoenix3D/Materials", "Applied");
            }

            // 2. Tìm tất cả MeshRenderer trong Scene
            var allRenderers = Object.FindObjectsByType<MeshRenderer>(FindObjectsInactive.Include);
            int upgradedCount = 0;
            string sceneName = scene.name.ToLower();

            var matCache = new Dictionary<string, Material>();

            foreach (var mr in allRenderers)
            {
                if (mr == null) continue;
                var go = mr.gameObject;
                string goName = go.name.ToLower();
                Transform parentT = go.transform.parent;
                string parentName = parentT != null ? parentT.name.ToLower() : "";

                // Bỏ qua các đối tượng không phải tường/công trình
                if (goName.Contains("sign") || goName.Contains("text") || goName.Contains("light") || goName.Contains("canvas") ||
                    goName.Contains("rotor") || goName.Contains("blade") || goName.Contains("pickup") || goName.Contains("item") ||
                    goName.Contains("robot") || goName.Contains("player") || goName.Contains("console") || goName.Contains("drum") ||
                    goName.Contains("crate") || goName.Contains("terminal") || goName.Contains("radar") || goName.Contains("pod") ||
                    goName.Contains("glass") || goName.Contains("doorarch") || goName.Contains("stairstep") || goName.Contains("ramp"))
                {
                    continue;
                }

                // Bỏ qua nếu thuộc trực thăng
                if (parentName.Contains("helicopter") || goName.Contains("helicopter") || parentName.Contains("oh-58d"))
                {
                    continue;
                }

                // Nhận diện phân loại tường và công trình
                bool isWall = false;
                string wallCategory = "default";

                // ── Phân loại cho Màn 3 (Helipad) ──
                if (goName.Contains("building_maincore") || goName.Contains("building_basetrim") || goName.Contains("building_roofcornice") || parentName.Contains("helipadbuilding"))
                {
                    isWall = true;
                    wallCategory = "facade";
                }
                else if (parentName.Contains("controltower") || goName.StartsWith("towerwall") || goName.Contains("towerfloor2_core"))
                {
                    isWall = true;
                    wallCategory = "tower";
                }
                else if (parentName.Contains("fueldepot") || goName.Contains("depotwall") || goName.Contains("depotroof") || (parentName.Contains("fueldepot") && goName.StartsWith("wall_")))
                {
                    isWall = true;
                    wallCategory = "depot";
                }
                else if (parentName.Contains("elevatorarrival") || goName.StartsWith("elev_wall") || parentName.Contains("elevatorshaft"))
                {
                    isWall = true;
                    wallCategory = "elevator";
                }
                else if (goName.StartsWith("fence_") || goName.Contains("parapet") || goName.Contains("perimeterwall"))
                {
                    isWall = true;
                    wallCategory = "fence";
                }
                // ── Phân loại cho Màn 2 (Reactor) ──
                else if (sceneName.Contains("reactor") && (goName.Contains("reactorwall") || goName.Contains("safetywall") || parentName.Contains("reactor")))
                {
                    isWall = true;
                    wallCategory = "reactor";
                }
                else if (sceneName.Contains("reactor") && (parentName.Contains("turbine") || parentName.Contains("generator") || goName.Contains("turbine")))
                {
                    isWall = true;
                    wallCategory = "turbine";
                }
                // ── Phân loại cho Màn 1 (Lab) ──
                else if (sceneName.Contains("lab") && (parentName.Contains("roomb") || goName.Contains("server")))
                {
                    isWall = true;
                    wallCategory = "lab_server";
                }
                else if (sceneName.Contains("lab") && (parentName.Contains("rooma") || goName.Contains("machine")))
                {
                    isWall = true;
                    wallCategory = "lab_machine";
                }
                else if (sceneName.Contains("lab") && (parentName.Contains("roomc") || goName.Contains("vault") || goName.Contains("exit")))
                {
                    isWall = true;
                    wallCategory = "lab_vault";
                }
                else if (sceneName.Contains("lab") && (parentName.Contains("mainhall") || parentName.Contains("corridor") || goName.Contains("corridor")))
                {
                    isWall = true;
                    wallCategory = "lab_clean";
                }
                // ── Nhận diện chung cho các đối tượng có tên chứa "wall" ──
                else if (goName.StartsWith("wall_") || goName.EndsWith("_wall") || goName.Contains("wall_") || goName.Contains("_wall_"))
                {
                    isWall = true;
                    if (sceneName.Contains("helipad")) wallCategory = "facade";
                    else if (sceneName.Contains("reactor")) wallCategory = "reactor";
                    else wallCategory = "lab_clean";
                }

                if (!isWall) continue;

                // Chọn vật liệu P3D phù hợp nhất
                string baseMatName;
                float tileDensityX = 3.0f;
                float tileDensityY = 2.5f;

                switch (wallCategory)
                {
                    case "facade":
                        baseMatName = "Outdoor_Wall_T11"; // Tấm ốp kiến trúc xám hiện đại tòa nhà sân bay
                        tileDensityX = 3.2f;
                        tileDensityY = 2.4f;
                        break;
                    case "tower":
                        baseMatName = "Outdoor_Wall_T16"; // Đá đen xám quân sự kỹ thuật cao cho tháp radar
                        tileDensityX = 2.6f;
                        tileDensityY = 2.0f;
                        break;
                    case "depot":
                        baseMatName = "Outdoor_Wall_T09"; // Khối bê tông kiên cố công nghiệp chống nổ
                        tileDensityX = 2.5f;
                        tileDensityY = 2.2f;
                        break;
                    case "elevator":
                        baseMatName = "Outdoor_Wall_T07"; // Đá kiến trúc phẳng sang trọng buồng thang máy
                        tileDensityX = 2.8f;
                        tileDensityY = 2.2f;
                        break;
                    case "fence":
                        baseMatName = "Outdoor_Wall_T04"; // Đá hoa cương đen bảo vệ lan can tầng thượng
                        tileDensityX = 3.5f;
                        tileDensityY = 2.0f;
                        break;
                    case "reactor":
                        baseMatName = "Outdoor_Wall_T09"; // Bê tông cản bức xạ lò phản ứng
                        tileDensityX = 3.0f;
                        tileDensityY = 2.5f;
                        break;
                    case "turbine":
                        baseMatName = "Outdoor_Wall_T14"; // Gạch men công nghiệp nhà máy điện
                        tileDensityX = 3.0f;
                        tileDensityY = 2.5f;
                        break;
                    case "lab_server":
                        baseMatName = "Outdoor_Wall_T16"; // Đá đen cao cấp phòng máy chủ
                        tileDensityX = 2.8f;
                        tileDensityY = 2.5f;
                        break;
                    case "lab_machine":
                        baseMatName = "Outdoor_Wall_T09"; // Gạch chịu lực phòng máy móc
                        tileDensityX = 2.8f;
                        tileDensityY = 2.5f;
                        break;
                    case "lab_vault":
                        baseMatName = "Outdoor_Wall_T05"; // Khối đá đen kiên cố phòng thoát hiểm
                        tileDensityX = 2.8f;
                        tileDensityY = 2.5f;
                        break;
                    case "lab_clean":
                    default:
                        baseMatName = "Outdoor_Wall_T01"; // Gạch men trắng sứ vô trùng phòng thí nghiệm
                        tileDensityX = 3.0f;
                        tileDensityY = 2.5f;
                        break;
                }

                string baseMatPath = $"Assets/Phoenix3D/Materials/{baseMatName}.mat";
                var baseMat = AssetDatabase.LoadAssetAtPath<Material>(baseMatPath);
                if (baseMat == null) continue;

                // Tính toán UV Tiling tỉ lệ thực tế
                Vector3 scale = go.transform.lossyScale;
                float length = Mathf.Max(scale.x, scale.z);
                float height = scale.y;

                int tileX = Mathf.Clamp(Mathf.RoundToInt(length / tileDensityX), 1, 32);
                int tileY = Mathf.Clamp(Mathf.RoundToInt(height / tileDensityY), 1, 16);

                string cacheKey = $"{baseMatName}_{tileX}x{tileY}";
                Material appliedMat;

                if (!matCache.TryGetValue(cacheKey, out appliedMat))
                {
                    string appliedPath = $"{appliedDir}/{cacheKey}.mat";
                    appliedMat = AssetDatabase.LoadAssetAtPath<Material>(appliedPath);

                    if (appliedMat == null)
                    {
                        appliedMat = new Material(baseMat);
                        appliedMat.name = cacheKey;
                        Vector2 st = new Vector2(tileX, tileY);

                        if (appliedMat.HasProperty("_BaseMap")) appliedMat.SetTextureScale("_BaseMap", st);
                        if (appliedMat.HasProperty("_BumpMap")) appliedMat.SetTextureScale("_BumpMap", st);
                        if (appliedMat.HasProperty("_OcclusionMap")) appliedMat.SetTextureScale("_OcclusionMap", st);
                        if (appliedMat.HasProperty("_ParallaxMap")) appliedMat.SetTextureScale("_ParallaxMap", st);
                        if (appliedMat.HasProperty("_MainTex")) appliedMat.SetTextureScale("_MainTex", st);

                        AssetDatabase.CreateAsset(appliedMat, appliedPath);
                    }
                    matCache[cacheKey] = appliedMat;
                }

                Undo.RecordObject(mr, "Apply P3D Wall Material");
                mr.sharedMaterial = appliedMat;
                upgradedCount++;
            }

            AssetDatabase.SaveAssets();
            return upgradedCount;
        }

        [MenuItem("EscapeTheLab/🧪 1-Click Trang Hoàng & Nâng Cấp Đồ Vật Phòng Lab (Màn 1)", priority = 24)]
        public static void UpgradeAndDecorateLab()
        {
            FixAllShadersToURP();

            string labScenePath = "Assets/_Project/Scenes/Lab.unity";
            if (!System.IO.File.Exists(labScenePath))
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file Scene 'Assets/_Project/Scenes/Lab.unity'!", "OK");
                return;
            }

            var currentScene = EditorSceneManager.GetActiveScene();
            bool needRestore = false;
            string currentScenePath = currentScene.path;

            Scene labScene;
            if (currentScene.path != labScenePath)
            {
                labScene = EditorSceneManager.OpenScene(labScenePath, OpenSceneMode.Single);
                needRestore = true;
            }
            else
            {
                labScene = currentScene;
            }

            int wallCount = ApplyP3DWallTexturesToScene(labScene);
            int propCount = DecorateLabWithSciFiPropsInternal(labScene);

            EditorSceneManager.MarkSceneDirty(labScene);
            EditorSceneManager.SaveScene(labScene);

            if (needRestore && !string.IsNullOrEmpty(currentScenePath) && System.IO.File.Exists(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.DisplayDialog("Hoàn tất Màn 1!",
                $"Đã nâng cấp và trang hoàng toàn bộ Phòng Lab (Màn 1) thành công!\n\n" +
                $"• {wallCount} bức tường được ốp texture P3D sạch bóng chuẩn Cleanroom Lab.\n" +
                $"• {propCount} vật thể công nghệ cao Sci-Fi đã được lắp đặt:\n" +
                "  + Cột trụ chịu lực vòm trần (Structural Columns) tại 4 góc sảnh\n" +
                "  + Bàn nghiên cứu kính & Dãy bàn máy tính Terminal (Workstations)\n" +
                "  + Màn hình tường lớn & Bảng sơ đồ nhiệm vụ (Telemetry Screens)\n" +
                "  + Máy phát điện plasma, ắc quy năng lượng & tủ tụ điện cao áp (Room A)\n" +
                "  + Dãy máy chủ Server, bàn điều khiển Console & tủ tài liệu (Room B)\n" +
                "  + Hệ thống đèn LED neon xanh cyan âm tường dọc hành lang\n" +
                "  + Thùng container kỹ thuật bảo vệ linh kiện (Tech Cargo Crates)\n" +
                "  + Khung cửa trượt khí nén tự động (Airlock Doors)\n" +
                "• Đã tự động Bake lại NavMesh cho AI di chuyển mượt mà.\n\n" +
                "Scene đã được tự động lưu lại!", "Tuyệt vời");
        }

        [MenuItem("EscapeTheLab/⚡ 1-Click Trang Hoàng & Nâng Cấp Đồ Vật Lò Phản Ứng (Màn 2)", priority = 25)]
        public static void UpgradeAndDecorateReactor()
        {
            FixAllShadersToURP();

            string reactorScenePath = "Assets/_Project/Scenes/Level2_Reactor.unity";
            if (!System.IO.File.Exists(reactorScenePath))
            {
                EditorUtility.DisplayDialog("Lỗi", "Không tìm thấy file Scene 'Assets/_Project/Scenes/Level2_Reactor.unity'!", "OK");
                return;
            }

            var currentScene = EditorSceneManager.GetActiveScene();
            bool needRestore = false;
            string currentScenePath = currentScene.path;

            Scene reactorScene;
            if (currentScene.path != reactorScenePath)
            {
                reactorScene = EditorSceneManager.OpenScene(reactorScenePath, OpenSceneMode.Single);
                needRestore = true;
            }
            else
            {
                reactorScene = currentScene;
            }

            int wallCount = ApplyP3DWallTexturesToScene(reactorScene);
            int propCount = DecorateReactorWithSciFiPropsInternal(reactorScene);

            EditorSceneManager.MarkSceneDirty(reactorScene);
            EditorSceneManager.SaveScene(reactorScene);

            if (needRestore && !string.IsNullOrEmpty(currentScenePath) && System.IO.File.Exists(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.DisplayDialog("Hoàn tất Màn 2!",
                $"Đã nâng cấp và trang hoàng khu vực Lò Phản Ứng (Màn 2) thành công!\n\n" +
                $"• {wallCount} bức tường được ốp texture P3D bê tông chịu nhiệt & gạch chống bức xạ.\n" +
                $"• {propCount} máy móc thiết bị công nghiệp Sci-Fi được bổ sung:\n" +
                "  + Máy phát điện plasma khổng lồ & tủ tụ điện cao áp (Phòng Máy Phát)\n" +
                "  + Bàn điều hành vi tính & Màn hình hiển thị mạng lưới điện trung tâm\n" +
                "  + Thùng container kỹ thuật thay thế toàn bộ khối hộp gỗ thô cũ\n" +
                "  + Kệ sắt công nghiệp, tủ thiết bị & bảng sơ đồ điều hành\n" +
                "  + Cột trụ chịu lực và đèn neon dải dài phát sáng URP Lit\n" +
                "• Đã tự động Bake lại NavMesh cho AI di chuyển mượt mà.\n\n" +
                "Scene đã được tự động lưu lại!", "Tuyệt vời");
        }

        [MenuItem("EscapeTheLab/🌟 1-Click Nâng Cấp & Trang Hoàng Toàn Bộ Màn 1 + Màn 2", priority = 26)]
        public static void UpgradeAndDecorateBothScenes()
        {
            FixAllShadersToURP();

            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            // Màn 1
            var sc1 = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity", OpenSceneMode.Single);
            int w1 = ApplyP3DWallTexturesToScene(sc1);
            int p1 = DecorateLabWithSciFiPropsInternal(sc1);
            BrightenSceneLighting(sc1);
            EditorSceneManager.MarkSceneDirty(sc1);
            EditorSceneManager.SaveScene(sc1);

            // Màn 2
            var sc2 = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level2_Reactor.unity", OpenSceneMode.Single);
            int w2 = ApplyP3DWallTexturesToScene(sc2);
            int p2 = DecorateReactorWithSciFiPropsInternal(sc2);
            BrightenSceneLighting(sc2);
            EditorSceneManager.MarkSceneDirty(sc2);
            EditorSceneManager.SaveScene(sc2);

            // Màn 3
            if (System.IO.File.Exists("Assets/_Project/Scenes/Level3_Helipad.unity"))
            {
                var sc3 = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level3_Helipad.unity", OpenSceneMode.Single);
                BrightenSceneLighting(sc3);
                EditorSceneManager.MarkSceneDirty(sc3);
                EditorSceneManager.SaveScene(sc3);
            }

            if (!string.IsNullOrEmpty(currentScenePath) && System.IO.File.Exists(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.DisplayDialog("Hoàn tất đại tu toàn diện!",
                $"Đã hoàn tất nâng cấp và trang hoàng toàn bộ Màn 1 (Lab) & Màn 2 (Reactor)!\n\n" +
                $"• Tổng cộng {w1 + w2} bề mặt công trình/tường được ốp gạch/bê tông P3D sắc nét.\n" +
                $"• Tổng cộng {p1 + p2} máy móc, bàn vi tính, tủ, màn hình, đèn neon và container Sci-Fi được bố trí sống động.\n" +
                "• Ánh sáng được tăng cường rực rỡ, sắc nét, loại bỏ mọi góc tối đen mù mịt.\n" +
                "• Toàn bộ NavMesh AI đã được nướng lại chuẩn xác.\n\n" +
                "Tất cả Scene đã được lưu!", "Tuyệt vời");
        }

        [MenuItem("EscapeTheLab/💡 1-Click Tăng Sáng Toàn Diện (Cả 3 Màn: Lab, Reactor, Helipad)", priority = 30)]
        public static void BrightenAllScenesLighting()
        {
            string currentScenePath = EditorSceneManager.GetActiveScene().path;

            var sc1 = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity", OpenSceneMode.Single);
            BrightenSceneLighting(sc1);
            EditorSceneManager.MarkSceneDirty(sc1);
            EditorSceneManager.SaveScene(sc1);

            var sc2 = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level2_Reactor.unity", OpenSceneMode.Single);
            BrightenSceneLighting(sc2);
            EditorSceneManager.MarkSceneDirty(sc2);
            EditorSceneManager.SaveScene(sc2);

            if (System.IO.File.Exists("Assets/_Project/Scenes/Level3_Helipad.unity"))
            {
                var sc3 = EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level3_Helipad.unity", OpenSceneMode.Single);
                BrightenSceneLighting(sc3);
                EditorSceneManager.MarkSceneDirty(sc3);
                EditorSceneManager.SaveScene(sc3);
            }

            if (!string.IsNullOrEmpty(currentScenePath) && System.IO.File.Exists(currentScenePath))
            {
                EditorSceneManager.OpenScene(currentScenePath, OpenSceneMode.Single);
            }

            EditorUtility.DisplayDialog("Tăng Sáng Thành Công!",
                "Đã nâng cấp và tăng cường ánh sáng rực rỡ, rõ nét cho cả 3 màn:\n\n" +
                "• Màn 1 (Lab): Ánh sáng huỳnh quang trắng xanh sắc nét + Đèn rọi sảnh & bàn họp.\n" +
                "• Màn 2 (Reactor): Ánh sáng công nghiệp ấm áp, đèn trần cao áp 32m tỏa rộng khắp 4 phòng, không còn góc tối đen.\n" +
                "• Màn 3 (Helipad): Tăng cường đèn mặt trăng dịu mát + Đèn pha cao áp công suất lớn 75W rọi sáng sân đỗ và nóc nhà.\n" +
                "• Ambient Light môi trường được tăng mạnh, loại bỏ hoàn toàn vùng tối đen mù mịt!\n\n" +
                "Tất cả các màn đã được lưu!", "Tuyệt vời");
        }

        [MenuItem("EscapeTheLab/💡 1-Click Tăng Sáng Màn Hiện Tại", priority = 31)]
        public static void BrightenCurrentSceneLighting()
        {
            var activeScene = EditorSceneManager.GetActiveScene();
            BrightenSceneLighting(activeScene);
            EditorSceneManager.MarkSceneDirty(activeScene);
            EditorSceneManager.SaveScene(activeScene);

            EditorUtility.DisplayDialog("Tăng Sáng Thành Công!",
                $"Đã tăng cường độ sáng rõ nét cho scene hiện tại ({activeScene.name})!\n" +
                "Không còn góc tối đen, các chi tiết nội thất và tường hiện rõ tuyệt đẹp.", "OK");
        }

        public static void BrightenSceneLighting(Scene scene)
        {
            string sName = scene.name.ToLower();
            if (sName.Contains("lab") && !sName.Contains("reactor") && !sName.Contains("helipad"))
            {
                BrightenLabLightingInternal(scene);
            }
            else if (sName.Contains("reactor") || sName.Contains("level2"))
            {
                BrightenReactorLightingInternal(scene);
            }
            else if (sName.Contains("helipad") || sName.Contains("level3"))
            {
                BrightenHelipadLightingInternal(scene);
            }
        }

        public static void BrightenReactorLightingInternal(Scene scene)
        {
            // 1. Ambient Light công nghiệp hiện đại, rõ ràng, nhìn rõ mọi chi tiết
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.52f, 0.50f, 0.48f);

            // 2. Tạo hoặc kích hoạt Directional Fill Light để tán xạ ánh sáng tự nhiên
            var dirLightGO = GameObject.Find("Directional_Fill_Light");
            if (dirLightGO == null)
            {
                dirLightGO = new GameObject("Directional_Fill_Light");
            }
            var dirLight = dirLightGO.GetComponent<Light>();
            if (dirLight == null) dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.90f;
            dirLight.color = new Color(0.96f, 0.92f, 0.88f);
            dirLight.transform.rotation = Quaternion.Euler(55f, -35f, 0);
            dirLight.shadows = LightShadows.Soft;

            var envRoot = GameObject.Find("=== LEVEL 2: REACTOR ENVIRONMENT ===");
            if (envRoot == null) return;

            Transform lightsRoot = envRoot.transform.Find("Lights");
            if (lightsRoot != null)
            {
                Undo.DestroyObjectImmediate(lightsRoot.gameObject);
            }
            lightsRoot = new GameObject("Lights").transform;
            lightsRoot.SetParent(envRoot.transform, false);

            // A. Sảnh Lò Phản Ứng (26x26m) - 4 đèn cao áp hổ phách cực sáng + 1 đèn rọi lõi plasma
            SpawnCeilingLamp(lightsRoot, new Vector3(-7f, 5.2f, -7f), new Color(1f, 0.78f, 0.52f), 32f, 32f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 7f, 5.2f, -7f), new Color(1f, 0.78f, 0.52f), 32f, 32f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-7f, 5.2f,  7f), new Color(1f, 0.78f, 0.52f), 32f, 32f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 7f, 5.2f,  7f), new Color(1f, 0.78f, 0.52f), 32f, 32f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 0f, 5.5f,  0f), new Color(0.35f, 0.85f, 1f), 35f, 28f);

            // B. Phòng Nồi Hơi (West: X=-26, 16x18m) - 4 đèn phân bổ đều 4 góc phòng
            SpawnCeilingLamp(lightsRoot, new Vector3(-30f, 4.8f, -4f), new Color(1f, 0.65f, 0.35f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-22f, 4.8f, -4f), new Color(1f, 0.65f, 0.35f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-30f, 4.8f,  4f), new Color(1f, 0.65f, 0.35f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-22f, 4.8f,  4f), new Color(1f, 0.65f, 0.35f), 28f, 26f);

            // C. Phòng Máy Phát (East: X=26, 16x18m) - 4 đèn điện tử cyan phân bổ đều
            SpawnCeilingLamp(lightsRoot, new Vector3(30f, 4.8f, -4f), new Color(0.55f, 0.85f, 1f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(22f, 4.8f, -4f), new Color(0.55f, 0.85f, 1f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(30f, 4.8f,  4f), new Color(0.55f, 0.85f, 1f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(22f, 4.8f,  4f), new Color(0.55f, 0.85f, 1f), 28f, 26f);

            // D. Tiền Sảnh Kho & Thang Máy (North: Z=24, 20x14m) - 4 đèn trần trắng sáng
            SpawnCeilingLamp(lightsRoot, new Vector3(-5f, 4.8f, 21f), new Color(0.95f, 0.95f, 0.92f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 5f, 4.8f, 21f), new Color(0.95f, 0.95f, 0.92f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-5f, 4.8f, 27f), new Color(0.95f, 0.95f, 0.92f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 5f, 4.8f, 27f), new Color(0.95f, 0.95f, 0.92f), 28f, 26f);

            // E. Các cửa vòm chuyển tiếp (Airlock Portals)
            SpawnCeilingLamp(lightsRoot, new Vector3(-13f, 4.5f, 0f), new Color(1f, 0.85f, 0.6f), 20f, 18f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 13f, 4.5f, 0f), new Color(0.6f, 0.85f, 1f), 20f, 18f);
            SpawnCeilingLamp(lightsRoot, new Vector3(  0f, 4.5f, 13f), new Color(0.95f, 0.95f, 0.95f), 22f, 20f);
        }

        public static void BrightenLabLightingInternal(Scene scene)
        {
            // 1. Ambient Light trắng xanh phòng thí nghiệm hiện đại
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.50f, 0.53f, 0.58f);

            // 2. Directional Fill Light
            var dirLightGO = GameObject.Find("Directional_Fill_Light");
            if (dirLightGO == null)
            {
                dirLightGO = new GameObject("Directional_Fill_Light");
            }
            var dirLight = dirLightGO.GetComponent<Light>();
            if (dirLight == null) dirLight = dirLightGO.AddComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.90f;
            dirLight.color = new Color(0.92f, 0.95f, 1.0f);
            dirLight.transform.rotation = Quaternion.Euler(50f, -30f, 0);
            dirLight.shadows = LightShadows.Soft;

            var envRoot = GameObject.Find("=== LAB ENVIRONMENT ===");
            if (envRoot == null) return;

            Transform lightsRoot = envRoot.transform.Find("Lighting");
            if (lightsRoot != null)
            {
                Undo.DestroyObjectImmediate(lightsRoot.gameObject);
            }
            lightsRoot = new GameObject("Lighting").transform;
            lightsRoot.SetParent(envRoot.transform, false);

            // Sảnh chính (20x24m)
            SpawnCeilingLamp(lightsRoot, new Vector3(-6f, 4.6f, -6f), new Color(0.85f, 0.95f, 1f), 28f, 30f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 6f, 4.6f, -6f), new Color(0.85f, 0.95f, 1f), 28f, 30f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-6f, 4.6f,  6f), new Color(0.85f, 0.95f, 1f), 28f, 30f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 6f, 4.6f,  6f), new Color(0.85f, 0.95f, 1f), 28f, 30f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 0f, 4.6f, -3.5f), new Color(0.7f, 0.9f, 1f), 25f, 22f);

            // Phòng A (Máy móc / Năng lượng: X=-16, 12x16m)
            SpawnCeilingLamp(lightsRoot, new Vector3(-18f, 4.6f,  8f), new Color(0.75f, 1f, 0.8f), 25f, 24f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-14f, 4.6f,  8f), new Color(0.75f, 1f, 0.8f), 25f, 24f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-18f, 4.6f,  0f), new Color(0.75f, 1f, 0.8f), 25f, 24f);
            SpawnCeilingLamp(lightsRoot, new Vector3(-14f, 4.6f,  0f), new Color(0.75f, 1f, 0.8f), 25f, 24f);

            // Phòng B (Máy chủ: X=16, 12x16m)
            SpawnCeilingLamp(lightsRoot, new Vector3(14f, 4.6f,  8f), new Color(0.7f, 0.88f, 1f), 25f, 24f);
            SpawnCeilingLamp(lightsRoot, new Vector3(18f, 4.6f,  8f), new Color(0.7f, 0.88f, 1f), 25f, 24f);
            SpawnCeilingLamp(lightsRoot, new Vector3(14f, 4.6f,  0f), new Color(0.7f, 0.88f, 1f), 25f, 24f);
            SpawnCeilingLamp(lightsRoot, new Vector3(18f, 4.6f,  0f), new Color(0.7f, 0.88f, 1f), 25f, 24f);

            // Phòng C (Thoát hiểm: Z=20, 14x10m)
            SpawnCeilingLamp(lightsRoot, new Vector3(-3.5f, 4.6f, 20f), new Color(1f, 0.9f, 0.8f), 28f, 26f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 3.5f, 4.6f, 20f), new Color(1f, 0.9f, 0.8f), 28f, 26f);

            // Các hành lang
            SpawnCeilingLamp(lightsRoot, new Vector3(-8f, 4.6f,  4f), new Color(0.8f, 0.95f, 1f), 22f, 20f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 8f, 4.6f,  4f), new Color(0.8f, 0.95f, 1f), 22f, 20f);
            SpawnCeilingLamp(lightsRoot, new Vector3( 0f, 4.6f, 13.5f), new Color(0.8f, 0.95f, 1f), 22f, 20f);

            // Alert Light
            var alertGO = new GameObject("RobotAlertLight");
            alertGO.transform.SetParent(lightsRoot, false);
            alertGO.transform.position = new Vector3(0, 4.8f, 0);
            var al = alertGO.AddComponent<Light>();
            al.type = LightType.Point;
            al.color = Color.red;
            al.intensity = 0f;
            al.range = 30f;
        }

        public static void BrightenHelipadLightingInternal(Scene scene)
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.45f, 0.48f, 0.55f);

            var moon = GameObject.Find("MoonDirectionalLight");
            if (moon != null)
            {
                var ml = moon.GetComponent<Light>();
                if (ml != null)
                {
                    ml.intensity = 1.35f;
                    ml.color = new Color(0.7f, 0.82f, 1f);
                    ml.shadows = LightShadows.Soft;
                }
            }

            var envRoot = GameObject.Find("=== HELIPAD ENVIRONMENT ===");
            if (envRoot == null) return;

            // Tăng cường các đèn pha Floodlights
            var floodlights = envRoot.GetComponentsInChildren<Light>();
            foreach (var l in floodlights)
            {
                if (l.type == LightType.Spot)
                {
                    l.intensity = 75f;
                    l.range = 80f;
                }
            }

            // Đèn chiếu sáng sân dưới và chân cầu thang
            Transform extraLights = envRoot.transform.Find("BrightCourtyardLights");
            if (extraLights != null) Undo.DestroyObjectImmediate(extraLights.gameObject);
            extraLights = new GameObject("BrightCourtyardLights").transform;
            extraLights.SetParent(envRoot.transform, false);

            SpawnCeilingLamp(extraLights, new Vector3(-16f, 6f, -6f), new Color(0.9f, 0.95f, 1f), 35f, 35f);
            SpawnCeilingLamp(extraLights, new Vector3( 16f, 6f, -6f), new Color(0.9f, 0.95f, 1f), 35f, 35f);
            SpawnCeilingLamp(extraLights, new Vector3(-16f, 6f, 18f), new Color(0.9f, 0.95f, 1f), 35f, 35f);
            SpawnCeilingLamp(extraLights, new Vector3( 16f, 6f, 18f), new Color(0.9f, 0.95f, 1f), 35f, 35f);
        }

        [InitializeOnLoadMethod]
        private static void CheckAutoRunOnLoad()
        {
            string flagPath = "Temp/RunDecorateUpgrade.flag";
            if (System.IO.File.Exists(flagPath))
            {
                try { System.IO.File.Delete(flagPath); } catch {}
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("[AssetUpgradeTools] 🌟 Tự động thực hiện nâng cấp & trang hoàng cả 2 màn...");
                    UpgradeAndDecorateBothScenes();
                };
            }

            string flagLight = "Temp/RunBrightenLighting.flag";
            if (System.IO.File.Exists(flagLight))
            {
                try { System.IO.File.Delete(flagLight); } catch {}
                EditorApplication.delayCall += () =>
                {
                    Debug.Log("[AssetUpgradeTools] 💡 Tự động tăng sáng toàn diện cả 3 màn...");
                    BrightenAllScenesLighting();
                };
            }
        }

        public static int DecorateLabWithSciFiPropsInternal(Scene scene)
        {
            var envRoot = GameObject.Find("=== LAB ENVIRONMENT ===");
            if (envRoot == null) return 0;

            Undo.RegisterFullObjectHierarchyUndo(envRoot, "Decorate Lab with Sci-Fi Props");

            // 1. Dọn dẹp các khối primitive cũ
            var oldProps = envRoot.transform.Find("Props");
            if (oldProps != null)
            {
                List<GameObject> toDestroy = new List<GameObject>();
                for (int i = 0; i < oldProps.childCount; i++)
                {
                    var child = oldProps.GetChild(i);
                    string n = child.name.ToLower();
                    if (n.StartsWith("pillar") || n.StartsWith("workbench") || n.StartsWith("crate") || n.StartsWith("model_scificontainer"))
                    {
                        toDestroy.Add(child.gameObject);
                    }
                }
                foreach (var go in toDestroy)
                {
                    Undo.DestroyObjectImmediate(go);
                }
            }

            // 2. Tạo container SciFi_Props mới
            var oldSciFiProps = envRoot.transform.Find("SciFi_Props");
            if (oldSciFiProps != null) Undo.DestroyObjectImmediate(oldSciFiProps.gameObject);

            var propsRoot = new GameObject("SciFi_Props");
            propsRoot.transform.SetParent(envRoot.transform, false);
            int count = 0;

            // ── A. SẢNH CHÍNH (MAIN HALL) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(-8f, 0, -8f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(8f, 0, -8f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(-8f, 0, 8f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(8f, 0, 8f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;

            // Bàn họp kính & Máy chiếu ảnh 3 chiều Hologram
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/decorative_table_glass.prefab", propsRoot.transform, new Vector3(0, 0, -3.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/projector.prefab", propsRoot.transform, new Vector3(0, 0.85f, -3.5f), Quaternion.identity, Vector3.one * 0.7f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(-1.6f, 0, -3.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(1.6f, 0, -3.5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            // Trạm điều khiển máy tính trung tâm + Ghế
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", propsRoot.transform, new Vector3(0, 0, -6.5f), Quaternion.Euler(0, 180, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(0, 0, -5.3f), Quaternion.identity, Vector3.one) != null ? 1 : 0;

            // Màn hình lớn trung tâm & Bảng thông báo
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab", propsRoot.transform, new Vector3(0, 2.5f, 11.75f), Quaternion.Euler(0, 180, 0), new Vector3(1.2f, 1.2f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/bulletin_board_big.prefab", propsRoot.transform, new Vector3(9.75f, 2.2f, 0), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/bulletin_board_big.prefab", propsRoot.transform, new Vector3(-9.75f, 2.2f, 0), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;

            // Bồn nghiên cứu sinh học Bio-Hydroponic & Cây cảnh thí nghiệm
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/hydroponic.prefab", propsRoot.transform, new Vector3(-8f, 0, 9f), Quaternion.Euler(0, 45, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_plant.prefab", propsRoot.transform, new Vector3(8f, 0, 9f), Quaternion.Euler(0, -45, 0), Vector3.one) != null ? 1 : 0;

            // Máy bán nước tự động (Khu nghỉ ngơi nhân viên sảnh)
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/vending_machine.prefab", propsRoot.transform, new Vector3(8.5f, 0, -10.5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(-8.5f, 0, -10.5f), Quaternion.Euler(0, 25, 0), Vector3.one) != null ? 1 : 0;

            // Bảng sơ đồ công nghệ dán tường & Ống sàn góc tường
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_1.prefab", propsRoot.transform, new Vector3(9.85f, 2.2f, -5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_3.prefab", propsRoot.transform, new Vector3(-9.85f, 2.2f, -5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab", propsRoot.transform, new Vector3(-9.2f, 0, -11.2f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab", propsRoot.transform, new Vector3(9.2f, 0, -11.2f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            // Đèn LED neon sảnh chính
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(-9.8f, 2.5f, 5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(-9.8f, 2.5f, -5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(9.8f, 2.5f, 5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(9.8f, 2.5f, -5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            // ── B. ROOM A: MACHINE ROOM (PHÒNG MÁY MÓC / NĂNG LƯỢNG) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab", propsRoot.transform, new Vector3(-18f, 0, 8.5f), Quaternion.Euler(0, 180, 0), new Vector3(1.1f, 1.1f, 1.1f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Shield Core.prefab", propsRoot.transform, new Vector3(-20f, 0, 7f), Quaternion.identity, Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_big.prefab", propsRoot.transform, new Vector3(-13.5f, 0, 9.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_medium.prefab", propsRoot.transform, new Vector3(-13.5f, 0, 6.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab", propsRoot.transform, new Vector3(-19.5f, 0, 1.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab", propsRoot.transform, new Vector3(-21.6f, 0, 5.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/shelf.prefab", propsRoot.transform, new Vector3(-21.6f, 0, 2.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_plant_small.prefab", propsRoot.transform, new Vector3(-21.6f, 0.9f, 2.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(-19.5f, 0, -2.5f), Quaternion.Euler(0, 15, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab", propsRoot.transform, new Vector3(-17.5f, 0, -2.5f), Quaternion.Euler(0, -10, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_2.prefab", propsRoot.transform, new Vector3(-21.85f, 2.2f, -1f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_2_blue.prefab", propsRoot.transform, new Vector3(-21.8f, 2.5f, 4f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab", propsRoot.transform, new Vector3(-21.2f, 0, 11.2f), Quaternion.identity, Vector3.one) != null ? 1 : 0;

            // ── C. ROOM B: SERVER ROOM (PHÒNG MÁY CHỦ / AN NINH) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", propsRoot.transform, new Vector3(14f, 0, 8f), Quaternion.Euler(0, 180, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(14f, 0, 6.8f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", propsRoot.transform, new Vector3(18f, 0, 4f), Quaternion.Euler(0, 90, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(16.8f, 0, 4f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", propsRoot.transform, new Vector3(14f, 0, 0), Quaternion.identity, Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(14f, 0, 1.2f), Quaternion.Euler(0, 180, 0), Vector3.one) != null ? 1 : 0;

            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console.prefab", propsRoot.transform, new Vector3(18f, 0, 8f), Quaternion.Euler(0, 180, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/desk.prefab", propsRoot.transform, new Vector3(18.5f, 0, -1.5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_plant_desk.prefab", propsRoot.transform, new Vector3(18.5f, 0.85f, -1.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab", propsRoot.transform, new Vector3(16f, 2.5f, 11.75f), Quaternion.Euler(0, 180, 0), new Vector3(1.1f, 1.1f, 1.1f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab", propsRoot.transform, new Vector3(21.6f, 0, 2f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab", propsRoot.transform, new Vector3(21.6f, 0, 6f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_4.prefab", propsRoot.transform, new Vector3(21.85f, 2.2f, -1f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(21.8f, 2.5f, 4f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab", propsRoot.transform, new Vector3(21.2f, 0, 11.2f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;

            // ── D. ROOM C: EXIT ROOM (PHÒNG LỐI THOÁT) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/bulletin_board_big.prefab", propsRoot.transform, new Vector3(6.75f, 2.2f, 20f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(-5.5f, 0, 23.5f), Quaternion.Euler(0, 30, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab", propsRoot.transform, new Vector3(5.5f, 0, 23.5f), Quaternion.Euler(0, -25, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab", propsRoot.transform, new Vector3(-6.75f, 0, 20f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console_celing.prefab", propsRoot.transform, new Vector3(0, 4.3f, 21.5f), Quaternion.Euler(0, 180, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_5.prefab", propsRoot.transform, new Vector3(-6.85f, 2.2f, 22f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(0, 3.2f, 24.8f), Quaternion.Euler(0, 180, 0), Vector3.one) != null ? 1 : 0;

            // ── E. KHUNG CỬA TRƯỢT TẠI CÁC HÀNH LANG (LỐI ĐI MỞ) ──
            SpawnAirlockDoorFrame(propsRoot.transform, new Vector3(-6f, 0, 4f), Quaternion.Euler(0, 90, 0));
            SpawnAirlockDoorFrame(propsRoot.transform, new Vector3(6f, 0, 4f), Quaternion.Euler(0, -90, 0));
            SpawnAirlockDoorFrame(propsRoot.transform, new Vector3(0, 0, 12f), Quaternion.identity);

            // ── F. NÂNG CẤP HÌNH ẢNH 3D CHO CÁC ITEM & THIẾT BỊ ──
            UpgradeInteractableVisuals(scene);

            // 3. Bake lại NavMesh
            RebuildNavMeshInScene(envRoot);

            return count;
        }

        public static int DecorateReactorWithSciFiPropsInternal(Scene scene)
        {
            var envRoot = GameObject.Find("=== LEVEL 2: REACTOR ENVIRONMENT ===");
            if (envRoot == null) return 0;

            Undo.RegisterFullObjectHierarchyUndo(envRoot, "Decorate Reactor with Sci-Fi Props");

            var oldSciFiProps = envRoot.transform.Find("SciFi_Props");
            if (oldSciFiProps != null) Undo.DestroyObjectImmediate(oldSciFiProps.gameObject);

            var propsRoot = new GameObject("SciFi_Props");
            propsRoot.transform.SetParent(envRoot.transform, false);
            int count = 0;

            // ── A. PHÒNG MÁY PHÁT (ROOM_GENERATOR - EAST) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab", propsRoot.transform, new Vector3(29f, 0, 0), Quaternion.identity, new Vector3(1.35f, 1.35f, 1.35f)) != null ? 1 : 0;
            var oldTurbine = envRoot.transform.Find("Room_Generator/MainTurbine");
            if (oldTurbine != null)
            {
                var mr = oldTurbine.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", propsRoot.transform, new Vector3(23f, 0, 5.5f), Quaternion.Euler(0, 180, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(23f, 0, 4.3f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            var oldControlRack = envRoot.transform.Find("Room_Generator/ControlRack");
            if (oldControlRack != null)
            {
                var mr = oldControlRack.GetComponent<MeshRenderer>();
                if (mr != null) mr.enabled = false;
            }

            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_big.prefab", propsRoot.transform, new Vector3(32.5f, 0, -4.5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab", propsRoot.transform, new Vector3(32.5f, 0, 4.5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/shelf.prefab", propsRoot.transform, new Vector3(33.5f, 0, 0), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/vending_machine.prefab", propsRoot.transform, new Vector3(19.5f, 0, 5.5f), Quaternion.Euler(0, 180, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab", propsRoot.transform, new Vector3(26f, 2.8f, 7.8f), Quaternion.Euler(0, 180, 0), new Vector3(1.2f, 1.2f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(31f, 0, -7f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab", propsRoot.transform, new Vector3(21f, 0, -5.5f), Quaternion.Euler(0, 30, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_1.prefab", propsRoot.transform, new Vector3(33.75f, 2.2f, 2.5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            // ── B. PHÒNG NỒI HƠI (ROOM_BOILER - WEST) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab", propsRoot.transform, new Vector3(-29f, 0, 0), Quaternion.identity, new Vector3(1.15f, 1.15f, 1.15f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Capacitor.prefab", propsRoot.transform, new Vector3(-32.5f, 0, -4.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery_medium.prefab", propsRoot.transform, new Vector3(-28f, 0, -6.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Tables/desk.prefab", propsRoot.transform, new Vector3(-22f, 0, 5.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(-22f, 0, 4.3f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/shelf.prefab", propsRoot.transform, new Vector3(-33.5f, 0, 0), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab", propsRoot.transform, new Vector3(-32.5f, 0, 4.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(-21f, 0, -5.5f), Quaternion.Euler(0, -25, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/bulletin_board_big.prefab", propsRoot.transform, new Vector3(-33.75f, 2.2f, 2.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_2.prefab", propsRoot.transform, new Vector3(-26f, 2.5f, -8.85f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_2_blue.prefab", propsRoot.transform, new Vector3(-26f, 2.5f, 7.8f), Quaternion.Euler(0, 180, 0), Vector3.one) != null ? 1 : 0;

            // ── C. TIỀN SẢNH KHO HÀNG (ROOM_CARGOELEVATOR) ──
            foreach (var crateName in new[] { "Crate_1", "Crate_2", "Crate_3" })
            {
                var cr = envRoot.transform.Find($"Room_CargoElevator/{crateName}");
                if (cr != null)
                {
                    var mr = cr.GetComponent<MeshRenderer>();
                    if (mr != null) mr.enabled = false;
                }
            }
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(-6f, 0, 22f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_big.prefab", propsRoot.transform, new Vector3(-6f, 0, 25f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab", propsRoot.transform, new Vector3(6f, 0, 23f), Quaternion.Euler(0, 15, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/storage_container_small.prefab", propsRoot.transform, new Vector3(6f, 0, 26f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/shelf.prefab", propsRoot.transform, new Vector3(-8.8f, 0, 27f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/shelf.prefab", propsRoot.transform, new Vector3(8.8f, 0, 24f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/cabinet.prefab", propsRoot.transform, new Vector3(8.8f, 0, 27f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/pilot_seat.prefab", propsRoot.transform, new Vector3(-8.5f, 0, 19.5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/bulletin_board_big.prefab", propsRoot.transform, new Vector3(-9.8f, 2.2f, 24f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_3.prefab", propsRoot.transform, new Vector3(9.8f, 2.2f, 20f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            // ── D. SẢNH LÒ PHẢN ỨNG CHÍNH (ROOM_REACTORHALL) ──
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(-9f, 0, -9f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(9f, 0, -9f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(-9f, 0, 9f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Column/column_middle.prefab", propsRoot.transform, new Vector3(9f, 0, 9f), Quaternion.identity, new Vector3(1.2f, 1.25f, 1.2f)) != null ? 1 : 0;

            // Thiết bị ổn định lõi năng lượng Shield Core + Bàn điều khiển thứ cấp
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Shield Core.prefab", propsRoot.transform, new Vector3(0, 0, -10.5f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console.prefab", propsRoot.transform, new Vector3(-7f, 0, -10f), Quaternion.Euler(0, 45, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/console.prefab", propsRoot.transform, new Vector3(7f, 0, -10f), Quaternion.Euler(0, -45, 0), Vector3.one * 0.9f) != null ? 1 : 0;

            // Trạm phân tích đo đạc từ xa + Màn hình lớn hiển thị thông số Lò
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", propsRoot.transform, new Vector3(10.5f, 0, -6f), Quaternion.Euler(0, -90, 0), Vector3.one * 0.9f) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/decorative_chair.prefab", propsRoot.transform, new Vector3(9.3f, 0, -6f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/big_screen.prefab", propsRoot.transform, new Vector3(0, 3.2f, -12.75f), Quaternion.identity, new Vector3(1.25f, 1.25f, 1.25f)) != null ? 1 : 0;

            // Bảng cảnh báo bức xạ & Đường ống góc sảnh lò
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_5.prefab", propsRoot.transform, new Vector3(-12.85f, 2.5f, -6f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/Arts/art_6.prefab", propsRoot.transform, new Vector3(12.85f, 2.5f, -6f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab", propsRoot.transform, new Vector3(-12.2f, 0, -12.2f), Quaternion.identity, Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/floor_corner_ornament_pipes.prefab", propsRoot.transform, new Vector3(12.2f, 0, -12.2f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(-11.8f, 2.5f, 5f), Quaternion.Euler(0, 90, 0), Vector3.one) != null ? 1 : 0;
            count += SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Lights/light_wall_1_blue.prefab", propsRoot.transform, new Vector3(11.8f, 2.5f, 5f), Quaternion.Euler(0, -90, 0), Vector3.one) != null ? 1 : 0;

            // ── E. KHUNG CỬA TRƯỢT TẠI 3 LỐI VÀO PHÒNG LÒ PHẢN ỨNG ──
            SpawnAirlockDoorFrame(propsRoot.transform, new Vector3(-13f, 0, 0), Quaternion.Euler(0, 90, 0));
            SpawnAirlockDoorFrame(propsRoot.transform, new Vector3(13f, 0, 0), Quaternion.Euler(0, -90, 0));
            SpawnAirlockDoorFrame(propsRoot.transform, new Vector3(0, 0, 13f), Quaternion.identity);

            // ── F. NÂNG CẤP HÌNH ẢNH 3D CHO CÁC ITEM & THIẾT BỊ ──
            UpgradeInteractableVisuals(scene);

            // 3. Bake lại NavMesh
            RebuildNavMeshInScene(envRoot);

            return count;
        }

        private static void UpgradeInteractableVisuals(Scene scene)
        {
            var rootObjects = scene.GetRootGameObjects();

            // 1. Nâng cấp Pickup Items (Pin, Máy phát, Terminal)
            foreach (var root in rootObjects)
            {
                var pickups = root.GetComponentsInChildren<PickupItem>(true);
                foreach (var p in pickups)
                {
                    string pName = p.gameObject.name.ToLower();
                    if (pName.Contains("battery"))
                    {
                        Attach3DVisualToItem(p.gameObject, "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/Battery.prefab", Vector3.zero, Quaternion.identity, Vector3.one * 0.35f);
                    }
                }

                // 2. Nâng cấp Generator (Lab)
                var generators = root.GetComponentsInChildren<Generator>(true);
                foreach (var g in generators)
                {
                    Attach3DVisualToItem(g.gameObject, "Assets/Sci-Fi Styled Modular Pack/Prefabs/Machines/generator.prefab", Vector3.zero, Quaternion.identity, Vector3.one * 0.85f);
                }

                // 3. Nâng cấp Terminal (Lab)
                var terminals = root.GetComponentsInChildren<Terminal>(true);
                foreach (var t in terminals)
                {
                    Attach3DVisualToItem(t.gameObject, "Assets/Sci-Fi Styled Modular Pack/Prefabs/Decorative elements/computer_station.prefab", Vector3.zero, Quaternion.identity, Vector3.one * 0.85f);
                }
            }
        }

        private static void Attach3DVisualToItem(GameObject target, string prefabPath, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            if (target == null) return;
            var existingVisual = target.transform.Find("Visual_3D_Model");
            if (existingVisual != null) return; // Đã nâng cấp rồi

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (prefab == null) return;

            // Ẩn MeshRenderer dạng hình cầu/khối cube nguyên bản
            var rootMr = target.GetComponent<MeshRenderer>();
            if (rootMr != null) rootMr.enabled = false;

            // Tạo model con
            var visual = (GameObject)PrefabUtility.InstantiatePrefab(prefab, target.transform);
            visual.name = "Visual_3D_Model";
            visual.transform.localPosition = localPos;
            visual.transform.localRotation = localRot;
            visual.transform.localScale = localScale;

            // Tắt mọi Collider của model con để không cản trở việc nhặt hay player di chuyển
            var cols = visual.GetComponentsInChildren<Collider>(true);
            foreach (var c in cols)
            {
                c.enabled = false;
            }
        }

        private static GameObject SpawnSciFiProp(string relativePrefabPath, Transform parent, Vector3 localPos, Quaternion localRot, Vector3 localScale)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(relativePrefabPath);
            if (prefab == null) return null;
            var inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
            inst.transform.localPosition = localPos;
            inst.transform.localRotation = localRot;
            inst.transform.localScale = localScale;
            return inst;
        }

        private static void SpawnAirlockDoorFrame(Transform parent, Vector3 pos, Quaternion rot)
        {
            var door = SpawnSciFiProp("Assets/Sci-Fi Styled Modular Pack/Prefabs/Doors/door_1.prefab", parent, pos, rot, Vector3.one);
            if (door != null)
            {
                var dl = door.transform.Find("door_1_left");
                if (dl != null) dl.gameObject.SetActive(false);
                var dr = door.transform.Find("door_1_right");
                if (dr != null) dr.gameObject.SetActive(false);
            }
        }

        private static void SpawnCeilingLamp(Transform parent, Vector3 pos, Color col, float intensity, float range)
        {
            var go = new GameObject("CeilingLamp");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;

            var l = go.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = col;
            l.intensity = intensity;
            l.range = range;
            l.shadows = LightShadows.Soft;

            // Tạo chụp đèn phát sáng visual
            var tube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tube.name = "LampVisual";
            tube.transform.SetParent(go.transform, false);
            tube.transform.localPosition = Vector3.zero;
            tube.transform.localScale = new Vector3(1.2f, 0.08f, 0.25f);
            var colTube = tube.GetComponent<Collider>();
            if (colTube != null) Object.DestroyImmediate(colTube);

            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.color = col;
            mat.EnableKeyword("_EMISSION");
            mat.SetColor("_EmissionColor", col * 3.5f);
            tube.GetComponent<Renderer>().material = mat;
        }

        private static void RebuildNavMeshInScene(GameObject envRoot)
        {
            if (envRoot == null) return;
            var surface = envRoot.GetComponentInChildren<NavMeshSurface>();
            if (surface == null)
            {
                surface = envRoot.AddComponent<NavMeshSurface>();
                surface.collectObjects = CollectObjects.All;
            }
            surface.BuildNavMesh();
        }
    }
}

