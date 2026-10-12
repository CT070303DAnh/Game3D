using UnityEngine;

/// <summary>
/// WallCodeClue: Con số phát quang in trên tường trong Màn 2.
/// Hiển thị số và ký hiệu thứ tự trên tường dưới dạng chữ phát quang Neon.
/// Người chơi tự quan sát bằng mắt trong phòng để giải mã, không có nút tương tác [E] và không hiện gợi ý pop-up.
/// </summary>
public class WallCodeClue : MonoBehaviour
{
    [Header("Clue Data")]
    public string digit = "2";
    public int orderIndex = 1; // 1, 2, hoac 3
    public string orderLabel = "①";

    [Header("Visual Components")]
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private Light glowLight;

    private void Awake()
    {
        // Loại bỏ mọi collider tương tác nếu có để PlayerInteraction không bắt trúng
        var cols = GetComponentsInChildren<Collider>(true);
        foreach (var c in cols)
        {
            if (c != null && c.isTrigger)
            {
                Destroy(c);
            }
        }
    }

    public void SetupClue(string num, int order, string label, Color neonCol)
    {
        digit = num;
        orderIndex = order;
        orderLabel = label;

        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMesh>();

        if (textMesh != null)
        {
            textMesh.text = $"{orderLabel}\n{digit}";
            textMesh.color = neonCol;
            textMesh.fontSize = 72;
            textMesh.alignment = TextAlignment.Center;
            textMesh.anchor = TextAnchor.MiddleCenter;
        }

        if (glowLight == null)
            glowLight = GetComponentInChildren<Light>();

        if (glowLight != null)
        {
            glowLight.color = neonCol;
            glowLight.range = 3.5f;
            glowLight.intensity = 1.8f;
        }
    }
}

