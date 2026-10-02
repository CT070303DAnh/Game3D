using UnityEngine;

/// <summary>
/// ElevatorCallButton: Nut bam mo cua thang may hang hoa ben ngoai buong thang.
/// - Ban dau cua thang may dong kin.
/// - Nguoi choi phai:
///   1. Dong het 3 van xa khi doc (ToxicGasManager.IsAirSafe)
///   2. Lap cau chi va bat Cau Dao Dien Tong (CircuitBreaker.IsPowerOn)
/// - Sau do bam nut nay, cua thang may se truot mo de buoc vao trong.
/// </summary>
public class ElevatorCallButton : MonoBehaviour, IInteractable
{
    [Header("Components")]
    [SerializeField] private Light buttonLight;
    [SerializeField] private Renderer buttonRenderer;

    private bool isDoorOpened = false;

    public bool CanInteract => !isDoorOpened;

    public string InteractPromptText => isDoorOpened ? "Cửa thang máy đã mở" : "Bấm nút mở cửa Thang Máy [E]";

    public void SetupReferences(Light light, Renderer rend)
    {
        buttonLight = light;
        buttonRenderer = rend;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(0.8f, 0.8f, 0.6f);
            bc.isTrigger = true;
        }
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void Interact()
    {
        if (isDoorOpened) return;

        // 1. Kiem tra van xa khi
        if (ToxicGasManager.OpenValvesCount > 0)
        {
            NotificationUI.ShowMessage($"CẢNH BÁO: Áp suất khí độc quá cao ({ToxicGasManager.OpenValvesCount}/3 van còn xả)! Hãy đóng hết 3 van trước khi gọi thang.");
            return;
        }

        // 2. Kiem tra cau dao dien tong
        if (!CircuitBreaker.IsPowerOn)
        {
            NotificationUI.ShowMessage("CẢNH BÁO: Thang máy chưa có điện! Hãy tìm CẦU CHÌ và bật CẦU DAO ĐIỆN TỔNG.");
            return;
        }

        // Du dieu kien -> Mo cua thang may
        isDoorOpened = true;
        UpdateVisual();

        if (ElevatorController.Instance != null)
        {
            ElevatorController.Instance.OpenElevatorDoor();
        }

        NotificationUI.ShowMessage("✓ CỬA THANG MÁY ĐÃ MỞ! Hãy bước vào buồng thang máy.");
        ObjectiveManager.Instance?.CompleteObjective("call_elevator");
    }

    private void UpdateVisual()
    {
        Color col = isDoorOpened ? Color.green : (CircuitBreaker.IsPowerOn && ToxicGasManager.IsAirSafe ? Color.cyan : Color.red);
        if (buttonLight != null)
        {
            buttonLight.color = col;
            buttonLight.intensity = isDoorOpened ? 2.5f : 1.2f;
        }

        if (buttonRenderer != null && buttonRenderer.material != null)
        {
            buttonRenderer.material.color = col;
            buttonRenderer.material.EnableKeyword("_EMISSION");
            buttonRenderer.material.SetColor("_EmissionColor", col * 1.5f);
        }
    }
}
