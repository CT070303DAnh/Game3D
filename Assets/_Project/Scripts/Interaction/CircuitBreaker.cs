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

    public static bool IsPowerOn => Instance != null ? Instance.isSwitchedOn : (GameState.Instance != null && GameState.Instance.PowerRestored);

    public bool CanInteract => !isSwitchedOn && !isAnimating;

    public string InteractPromptText
    {
        get
        {
            if (isSwitchedOn) return "Cầu Dao Điện Tổng (Đã cấp điện)";
            bool hasFuse = GameState.Instance != null && GameState.Instance.FuseCollected;
            return hasFuse ? "Lắp Cầu Chì & Đóng Cầu Dao Điện [E]" : "Cầu Dao Điện Tổng (Thiếu Cầu Chì) [E]";
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
            bc.size = new Vector3(1.4f, 1.6f, 1.0f);
            bc.isTrigger = true;
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
            NotificationUI.ShowMessage("CẢNH BÁO: Tủ điện bị thiếu CẦU CHÌ (Fuse)! Hãy tìm Cầu Chì trong phòng để lắp vào.");
            return;
        }

        StartCoroutine(SwitchOnRoutine());
    }

    private IEnumerator SwitchOnRoutine()
    {
        isAnimating = true;
        isSwitchedOn = true;

        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0.8f;
        }
        audio.pitch = 0.9f;

        // Gat can cau dao xuong
        if (switchLever != null)
        {
            float elapsed = 0f;
            float duration = 0.6f;
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

        // Tia lua dien
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

        NotificationUI.ShowMessage("✓ ĐÃ ĐÓNG CẦU DAO TỔNG! Nguồn điện Thang Máy đã được phục hồi.");
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
