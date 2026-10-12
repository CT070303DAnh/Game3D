#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class AutoUpgradeLevel3Runner
{
    static AutoUpgradeLevel3Runner()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        string key = "FirstAidUpgraded_Done_v2";
        if (EditorPrefs.GetBool(key, false)) return;
        EditorPrefs.SetBool(key, true);

        SceneQuickSwitcher.EnsureLevel3Upgraded(false);
    }
}
#endif
