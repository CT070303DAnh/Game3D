#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using System.IO;

[InitializeOnLoad]
public static class AutoBuildLevel2Once
{
    private const string FLAG_PATH = "Temp/Level3_Built_Flag.txt";

    static AutoBuildLevel2Once()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        if (File.Exists(FLAG_PATH)) return;

        try
        {
            File.WriteAllText(FLAG_PATH, "built");
            string currentScene = EditorSceneManager.GetActiveScene().path;

            // 1. Tao & Cap nhat Man 3 (Level 3: Helipad Escape)
            Debug.Log("[AutoBuild] === XÂY DỰNG MÀN 3 (LEVEL 3: HELIPAD ESCAPE) ===");
            HelipadEnvironmentBuilder.BuildLevel3_Direct(false);
            Debug.Log("[AutoBuild] Màn 3 đã được khởi tạo hoàn tất 100%! Scene Level3_Helipad đang mở.");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[AutoBuild] Lỗi cập nhật: " + ex);
        }
    }
}
#endif
