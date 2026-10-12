using UnityEngine;
using System.Collections;

/// <summary>
/// CircuitBreaker: Cau dao dien tong cho he thong Thang May trong Man 2.
/// - Yeu cau: Nguoi choi phai tim thay Cau Chi (Fuse) moi dong duoc cau dao.
/// - Khi bat: Cap dien tong cho nut goi thang may va buong thang may.
/// </summary>
public class CircuitBreaker : MonoBehaviour, IInteractable
{
    public static CircuitBreaker Instance { get; private set; }

    [Header("Components")]
    [SerializeField] private Transform switchLever;
    [SerializeField] private Light statusLight;
    [SerializeField] private ParticleSystem sparkVFX;

    private bool isSwitchedOn = false;
    private bool isAnimating = false;

    public static bool IsPowerOn => (Instance != null && Instance.isSwitchedOn) || (GameState.Instance != null && GameState.Instance.PowerRestored);

    public bool CanInteract => !isSwitchedOn && !isAnimating;

    public string InteractPromptText
    {
        get
        {
            if (isSwitchedOn) return "Cầu Dao Điện Tổng [ĐÃ CẤP ĐIỆN]";
            bool hasFuse = GameState.Instance != null && GameState.Instance.FuseCollected;
            return hasFuse ? "Lắp Cầu Chì & Đóng Cầu Dao Điện [E]" : "Cầu Dao Điện Tổng (Cần Cầu Chì) [E]";
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        var col = GetComponent<Collider>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(2.0f, 2.0f, 1.6f);
            bc.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        UpdateVisual();
    }

    public void SetupReferences(Transform lever, Light light, ParticleSystem sparks)
    {
        switchLever = lever;
        statusLight = light;
        sparkVFX = sparks;
    }

    public void Interact()
    {
        if (isSwitchedOn || isAnimating) return;

        bool hasFuse = GameState.Instance != null && GameState.Instance.FuseCollected;
        if (!hasFuse)
        {
            GameAudioManager.PlayTerminalError(transform.position);
            NotificationUI.ShowMessage("❌ THIẾU CẦU CHÌ! Hãy tìm Cầu Chì Cao Áp (Power Fuse) trong phòng để lắp vào.");
            AudioSource audio = GetComponent<AudioSource>();
            if (audio == null)
            {
                audio = gameObject.AddComponent<AudioSource>();
                audio.spatialBlend = 0.8f;
            }
            audio.pitch = 0.6f;
            audio.Play();
            return;
        }

        StartCoroutine(SwitchOnRoutine());
    }

    public void ForcePowerOn()
    {
        if (isSwitchedOn) return;
        isSwitchedOn = true;
        if (switchLever != null) switchLever.localRotation *= Quaternion.Euler(60f, 0, 0);
        if (sparkVFX != null) sparkVFX.Play();
        GameState.Instance?.SetPowerRestored();
        UpdateVisual();
        ObjectiveManager.Instance?.CompleteObjective("power");
    }

    private IEnumerator SwitchOnRoutine()
    {
        isAnimating = true;
        isSwitchedOn = true;

        GameAudioManager.PlaySwitchLever(transform.position);

        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0.8f;
        }
        audio.pitch = 0.9f;

        // Gạt cần cầu dao xuống (0.5 giây)
        if (switchLever != null)
        {
            float elapsed = 0f;
            float duration = 0.5f;
            Quaternion startRot = switchLever.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(60f, 0, 0);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                switchLever.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
                yield return null;
            }
            switchLever.localRotation = endRot;
        }

        // Tia lửa điện
        if (sparkVFX != null)
        {
            sparkVFX.Play();
        }

        if (GameState.Instance != null)
        {
            GameState.Instance.SetPowerRestored();
        }

        UpdateVisual();
        isAnimating = false;

        NotificationUI.ShowMessage("✓ ĐÃ LẮP CẦU CHÌ & BẬT CẦU DAO TỔNG! Nguồn điện Thang Máy đã được phục hồi.");
        ObjectiveManager.Instance?.CompleteObjective("power");
    }

    private void UpdateVisual()
    {
        if (statusLight != null)
        {
            statusLight.color = isSwitchedOn ? Color.green : Color.red;
            statusLight.intensity = isSwitchedOn ? 2.5f : 1.2f;
        }
    }
}
