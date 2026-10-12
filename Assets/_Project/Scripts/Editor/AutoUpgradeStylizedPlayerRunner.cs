#if UNITY_EDITOR
using UnityEditor;

[InitializeOnLoad]
public static class AutoUpgradeStylizedPlayerRunner
{
    static AutoUpgradeStylizedPlayerRunner()
    {
        EditorApplication.delayCall += RunOnce;
    }

    private static void RunOnce()
    {
        string key = "StylizedPlayer_Upgraded_v1";
        if (EditorPrefs.GetBool(key, false)) return;
        EditorPrefs.SetBool(key, true);

        PlayerStylizedUpgrader.UpgradeAllScenes(false);
    }
}
#endif
