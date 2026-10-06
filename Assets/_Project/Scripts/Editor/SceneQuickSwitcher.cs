#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// SceneQuickSwitcher: Menu tiện ích cho phép chuyển nhanh giữa các Scene trong Unity Editor.
/// </summary>
public static class SceneQuickSwitcher
{
    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/1️⃣ Màn 1: Phòng Thí Nghiệm (Lab)", false, 1)]
    public static void OpenLevel1()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Lab.unity");
        }
    }

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/2️⃣ Màn 2: Khu Lò Phản Ứng (Reactor)", false, 2)]
    public static void OpenLevel2()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level2_Reactor.unity");
        }
    }

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/3️⃣ Màn 3: Sân Đỗ Trực Thăng (Helipad)", false, 3)]
    public static void OpenLevel3()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/Level3_Helipad.unity");
        }
    }

    [MenuItem("EscapeTheLab/🎮 Mở Scene Chơi/🏠 Menu Chính (MainMenu)", false, 4)]
    public static void OpenMainMenu()
    {
        if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            EditorSceneManager.OpenScene("Assets/_Project/Scenes/MainMenu.unity");
        }
    }
}
#endif
