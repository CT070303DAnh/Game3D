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

    public string InteractPromptText
    {
        get
        {
            if (isDoorOpened) return "Cửa thang máy đã mở";
            if (ToxicGasManager.OpenValvesCount > 0) return $"Nút gọi Thang Máy [E] (CÒN {ToxicGasManager.OpenValvesCount} VAN KHÍ)";
            if (!CircuitBreaker.IsPowerOn) return "Nút gọi Thang Máy [E] (CHƯA CÓ ĐIỆN - CẦN BẬT CẦU DAO)";
            bool hasCard = GameState.Instance != null && GameState.Instance.EngineerCardCollected;
            if (!hasCard) return "Nút gọi Thang Máy [E] (CẦN THẺ KỸ SƯ)";
            return "Quẹt Thẻ Kỹ Sư & Mở Cửa Thang Máy [E]";
        }
    }

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
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage($"CẢNH BÁO: Còn {ToxicGasManager.OpenValvesCount} van đang xả khí độc! Hãy dùng Cờ Lê đóng hết các van trước khi gọi thang.");
            return;
        }

        // 2. Kiem tra nguon dien cau dao tong
        if (!CircuitBreaker.IsPowerOn)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ THANG MÁY CHƯA CÓ ĐIỆN! Hãy tìm CẦU CHÌ và bật CẦU DAO ĐIỆN TỔNG (Phòng Bên Phải) trước khi gọi thang.");
            return;
        }

        // 3. Kiem tra The Ky Su
        bool hasCard = GameState.Instance != null && GameState.Instance.EngineerCardCollected;
        if (!hasCard)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ TRUY CẬP BỊ TỪ CHỐI! Cần có 'Thẻ Kỹ Sư' (Engineer Card) để quẹt mở cửa Thang Máy.");
            return;
        }

        // Đủ toàn bộ 3 điều kiện -> Mở cửa thang máy!
        isDoorOpened = true;
        UpdateVisual();

        GameAudioManager.PlayDoorUnlock(transform.position);

        if (ElevatorController.Instance != null)
        {
            ElevatorController.Instance.OpenElevatorDoor();
        }

        NotificationUI.ShowMessage("✓ XÁC THỰC THẺ KỸ SƯ THÀNH CÔNG! CỬA THANG MÁY ĐÃ MỞ!");
        ObjectiveManager.Instance?.CompleteObjective("engineer_card");
        ObjectiveManager.Instance?.CompleteObjective("wall_numbers");
        ObjectiveManager.Instance?.CompleteObjective("call_elevator");
    }

    private void UpdateVisual()
    {
        bool hasCard = GameState.Instance != null && GameState.Instance.EngineerCardCollected;
        bool allReady = CircuitBreaker.IsPowerOn && ToxicGasManager.IsAirSafe && hasCard;
        Color col = isDoorOpened ? Color.green : (allReady ? Color.cyan : Color.red);
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
