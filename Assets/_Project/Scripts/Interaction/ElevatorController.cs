using UnityEngine;
using System.Collections;

/// <summary>
/// ElevatorController: Dieu khien buong thang may hang hoa trong Man 2.
/// - Ban dau: Cua thang may DONG KIN.
/// - Khi duoc goi boi ElevatorCallButton ben ngoai: Cua truot mo de player buoc vao.
/// - Ben trong buong thang may co Ban phim Keypad: Nguoi choi nhap ma 214 de van hanh thang len Man 3.
/// </summary>
public class ElevatorController : MonoBehaviour, IInteractable
{
    public static ElevatorController Instance { get; private set; }

    [Header("Elevator Components")]
    [SerializeField] private Transform elevatorDoor;       // Canh cua thang may
    [SerializeField] private Vector3 doorClosedOffset = new Vector3(0, 2.1f, 30f);
    [SerializeField] private Vector3 doorOpenOffset = new Vector3(0, 6.2f, 30f);
    [SerializeField] private Light interiorLight;

    [Header("Puzzle Settings")]
    [SerializeField] private string targetCode = "214";

    private bool isDoorOpen = false;
    private bool isCodeSolved = false;
    private bool isMoving = false;

    public bool CanInteract => isDoorOpen && !isCodeSolved && !isMoving;

    public string InteractPromptText
    {
        get
        {
            if (isCodeSolved || isMoving) return "Thang máy đang vận hành...";
            if (!isDoorOpen) return "Thang máy (Chưa mở cửa)";
            return "Bảng điều khiển thang máy - Nhập mã [E]";
        }
    }

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        var col = GetComponent<Collider>();
        if (col == null)
        {
            var box = gameObject.AddComponent<BoxCollider>();
            box.size = new Vector3(1.4f, 1.8f, 1.4f);
            box.isTrigger = true;
        }
    }

    private void Start()
    {
        // BAN DAU CUA THANG MAY DONG KIN
        if (elevatorDoor != null)
        {
            elevatorDoor.position = doorClosedOffset;
        }

        isDoorOpen = false;
        UpdateVisual();
    }

    public void SetupDoor(Transform door, Vector3 closedPos, Vector3 openPos, Light light)
    {
        elevatorDoor = door;
        doorClosedOffset = closedPos;
        doorOpenOffset = openPos;
        interiorLight = light;
    }

    /// <summary>Goi tu ElevatorCallButton khi du dieu kien mo cua</summary>
    public void OpenElevatorDoor()
    {
        if (isDoorOpen || isMoving) return;
        StartCoroutine(OpenDoorRoutine());
    }

    private IEnumerator OpenDoorRoutine()
    {
        isDoorOpen = true;

        if (elevatorDoor != null)
        {
            float elapsed = 0f;
            float duration = 1.2f;
            Vector3 startPos = elevatorDoor.position;
            Vector3 endPos = doorOpenOffset;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                elevatorDoor.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            elevatorDoor.position = endPos;
        }

        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (interiorLight != null)
        {
            interiorLight.color = isDoorOpen ? new Color(0.2f, 0.9f, 1f) : new Color(0.8f, 0.3f, 0.1f);
            interiorLight.intensity = isDoorOpen ? 2.2f : 0.8f;
        }
    }

    public void Interact()
    {
        if (!isDoorOpen || isCodeSolved || isMoving) return;

        // Mo ban phim so de nhap ma 214
        ElevatorKeypadUI.ShowKeypad(this, targetCode);
    }

    /// <summary>Duoc goi tu ElevatorKeypadUI khi nguoi choi nhap dung ma "214"</summary>
    public void OnCodeCorrect()
    {
        if (isCodeSolved) return;
        isCodeSolved = true;

        if (interiorLight != null)
        {
            interiorLight.color = Color.green;
            interiorLight.intensity = 2.5f;
        }

        StartCoroutine(StartElevatorSequence());
    }

    private IEnumerator StartElevatorSequence()
    {
        isMoving = true;
        NotificationUI.ShowMessage("CỬA THANG MÁY ĐANG ĐÓNG...");

        // Dong cua thang may tu doorOpenOffset ve doorClosedOffset
        if (elevatorDoor != null)
        {
            float elapsed = 0f;
            float duration = 1.5f;
            Vector3 startPos = elevatorDoor.position;
            Vector3 endPos = doorClosedOffset;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                elevatorDoor.position = Vector3.Lerp(startPos, endPos, elapsed / duration);
                yield return null;
            }
            elevatorDoor.position = endPos;
        }

        NotificationUI.ShowMessage("THANG MÁY ĐANG DI CHUYỂN LÊN TẦNG THƯỢNG (MÀN 3)...");

        // Rung camera tao hieu ung di chuyen thang may
        Transform cam = Camera.main != null ? Camera.main.transform : null;
        float moveTime = 0f;
        float moveDuration = 2.5f;

        while (moveTime < moveDuration)
        {
            moveTime += Time.deltaTime;
            if (cam != null)
            {
                float shake = (Mathf.PerlinNoise(Time.time * 25f, 0f) - 0.5f) * 0.04f;
                cam.position += new Vector3(0, shake, 0);
            }
            yield return null;
        }

        // Chuyen sang Man 3!
        if (GameManager.Instance != null)
        {
            GameManager.Instance.LoadLevelByName("Level3_Helipad");
        }
        else if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene("Level3_Helipad");
        }
        else
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Level3_Helipad");
        }
    }
}
