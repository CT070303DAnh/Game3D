using UnityEngine;
using System.Collections;

/// <summary>
/// HelipadRadarConsole: Ban dieu khien tram Radar va Cong vom san do trong Man 3 (Level 3: Helipad).
/// - Nguoi choi leo len Thap Dieu Khien va tuong tac [E].
/// - Kich hoat dia Radar tren noc xoay tron, phat tin hieu dan duong va ha hang rao bao ve san do.
/// </summary>
public class HelipadRadarConsole : MonoBehaviour, IInteractable
{
    public static HelipadRadarConsole Instance { get; private set; }

    [Header("Console Visuals")]
    [SerializeField] private Light screenLight;
    [SerializeField] private Transform radarDishTransform;
    [SerializeField] private float radarSpinSpeed = 90f;
    [SerializeField] private Transform[] blastGateDoors; // Rào chắn sân đỗ hạ xuống

    private bool isActivated = false;
    private bool isAnimating = false;

    public bool CanInteract => !isActivated && !isAnimating;

    public string InteractPromptText => isActivated ? "Hệ thống Radar & Dẫn đường [ĐANG HOẠT ĐỘNG]" : "Kích hoạt Trạm Radar & Mở Cổng Vòm Sân Đỗ [E]";

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        var col = GetComponent<Collider>();
        if (col == null)
        {
            var bc = gameObject.AddComponent<BoxCollider>();
            bc.size = new Vector3(1.4f, 1.6f, 1.2f);
            bc.isTrigger = true;
        }
    }

    private void Start()
    {
        UpdateVisual();
    }

    private void Update()
    {
        // Khi da kich hoat, dia radar tren noc thap lien tuc xoay tron
        if (isActivated && radarDishTransform != null)
        {
            radarDishTransform.Rotate(Vector3.up, radarSpinSpeed * Time.deltaTime, Space.Self);
        }
    }

    public void SetupReferences(Light light, Transform dish, Transform[] gates)
    {
        screenLight = light;
        radarDishTransform = dish;
        blastGateDoors = gates;
    }

    public void Interact()
    {
        if (isActivated || isAnimating) return;
        StartCoroutine(ActivateRoutine());
    }

    private IEnumerator ActivateRoutine()
    {
        isAnimating = true;
        isActivated = true;

        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0.8f;
        }
        audio.pitch = 1.1f;

        // Am thanh khoi dong he thong
        NotificationUI.ShowMessage("ĐANG KẾT NỐI TÍN HIỆU ĐIỀU HÀNH KHÔNG LƯU...");
        yield return new WaitForSeconds(0.8f);

        UpdateVisual();

        // Ha hang rao bao ve / Cong vom san do
        if (blastGateDoors != null && blastGateDoors.Length > 0)
        {
            float elapsed = 0f;
            float duration = 1.5f;
            Vector3[] startPositions = new Vector3[blastGateDoors.Length];
            Vector3[] targetPositions = new Vector3[blastGateDoors.Length];

            for (int i = 0; i < blastGateDoors.Length; i++)
            {
                if (blastGateDoors[i] != null)
                {
                    startPositions[i] = blastGateDoors[i].position;
                    targetPositions[i] = startPositions[i] + Vector3.down * 4.5f; // Ha xuong duoi san
                }
            }

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                for (int i = 0; i < blastGateDoors.Length; i++)
                {
                    if (blastGateDoors[i] != null)
                    {
                        blastGateDoors[i].position = Vector3.Lerp(startPositions[i], targetPositions[i], t);
                    }
                }
                yield return null;
            }
        }

        isAnimating = false;

        if (GameState.Instance != null)
        {
            GameState.Instance.SetRadarActivated();
            GameState.Instance.SetDomeGateOpened();
        }

        NotificationUI.ShowMessage("✓ TRẠM RADAR ĐÃ BẬT & CỔNG VÒM SÂN ĐỖ ĐÃ MỞ! Trực thăng đã sẵn sàng cất cánh.");
        ObjectiveManager.Instance?.CompleteObjective("radar");
        ObjectiveManager.Instance?.CompleteObjective("dome_gate");
    }

    private void UpdateVisual()
    {
        if (screenLight != null)
        {
            screenLight.color = isActivated ? Color.green : Color.red;
            screenLight.intensity = isActivated ? 2.5f : 1.2f;
        }
    }
}
