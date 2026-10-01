using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// UISharpener: Tự động tối ưu độ sắc nét của toàn bộ UI trong game khi chạy.
/// Đảm bảo font chữ luôn được render ở độ phân giải 3x (chống vỡ, chống mờ).
/// </summary>
public static class UISharpener
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Init()
    {
        ApplySharpSettings();
        SceneManager.sceneLoaded += (scene, mode) => ApplySharpSettings();
    }

    public static void ApplySharpSettings()
    {
        // 1. Tăng mật độ điểm ảnh cho font lên 3x trên tất cả Canvas
        var scalers = Object.FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var scaler in scalers)
        {
            if (scaler != null)
            {
                scaler.dynamicPixelsPerUnit = 3f;
                scaler.referencePixelsPerUnit = 100f;
            }
        }

        // 2. Tự động thêm viền và làm đậm text
        var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (var t in texts)
        {
            if (t != null)
            {
                t.fontStyle = FontStyle.Bold;
                var outline = t.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = t.gameObject.AddComponent<Outline>();
                    outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
                    outline.effectDistance = new Vector2(1.5f, -1.5f);
                }
            }
        }
    }
}
