using UnityEngine;

/// <summary>
/// WallCodeClue: Con so phan quang in tren tuong trong Man 2.
/// Hien thi so (1, 4 hoac 2) va ky hieu thu tu (I, II, III / ①, ②, ③).
/// Cho phep player nhin thay ro tren tuong hoac bam E de xem chi tiet.
/// </summary>
public class WallCodeClue : MonoBehaviour, IInteractable
{
    [Header("Clue Data")]
    public string digit = "2";
    public int orderIndex = 1; // 1, 2, hoac 3
    public string orderLabel = "①";

    [Header("Visual Components")]
    [SerializeField] private TextMesh textMesh;
    [SerializeField] private Light glowLight;

    public bool CanInteract => true;
    public string InteractPromptText => $"Xem con số trên tường [E]";

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
            glowLight.range = 2.5f;
            glowLight.intensity = 1.2f;
        }
    }

    public void Interact()
    {
        NotificationUI.ShowMessage($"MANH MỐI MÃ THANG MÁY: Ký hiệu {orderLabel} là số [{digit}] (Vị trí thứ {orderIndex})");
    }
}
