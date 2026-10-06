using UnityEngine;

/// <summary>
/// CameraModeManager: Quan ly chuyen doi goc nhin Camera (First Person FPS <-> Third Person PUBG).
/// Mac dinh khi vao game: Goc nhin thu nhat (First Person).
/// Phim tat: Bam phim [V] de chuyen doi goc nhin linh hoat trong luc choi.
/// </summary>
public class CameraModeManager : MonoBehaviour
{
    public static CameraModeManager Instance { get; private set; }

    public enum CameraMode { FirstPerson, ThirdPersonPUBG }

    [Header("Camera Mode Settings")]
    [SerializeField] private CameraMode defaultMode = CameraMode.FirstPerson;
    [SerializeField] private KeyCode toggleKey = KeyCode.V;

    [Header("First Person Settings")]
    [SerializeField] private Vector3 eyeOffset = new Vector3(0f, 1.6f, 0.12f);
    [SerializeField] private float fovFirstPerson = 75f;

    [Header("Third Person Settings")]
    [SerializeField] private float fovThirdPerson = 70f;

    private CameraMode currentMode;
    private GameObject player;
    private Transform eyePoint;
    private Camera mainCam;
    private FirstPersonCamera fpc;
    private PubgCamera pubgCam;
    private GameObject pubgRig;
    private Renderer[] playerRenderers;

    public static CameraMode ActiveMode => Instance != null ? Instance.currentMode : CameraMode.FirstPerson;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        InitComponents();
        SetCameraMode(defaultMode);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleCameraMode();
        }
    }

    private void InitComponents()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) player = GameObject.Find("Player");

        if (player != null)
        {
            // Tim hoac tao EyePoint tren Player
            eyePoint = player.transform.Find("EyePoint");
            if (eyePoint == null)
            {
                var epGO = new GameObject("EyePoint");
                epGO.transform.SetParent(player.transform, false);
                epGO.transform.localPosition = eyeOffset;
                epGO.transform.localRotation = Quaternion.identity;
                eyePoint = epGO.transform;
            }

            playerRenderers = player.GetComponentsInChildren<Renderer>(true);
        }

        mainCam = Camera.main;
        if (mainCam == null)
        {
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            mainCam = camGO.AddComponent<Camera>();
            camGO.AddComponent<AudioListener>();
        }

        // Tim PUBG Rig
        pubgCam = Object.FindFirstObjectByType<PubgCamera>();
        pubgRig = GameObject.Find("CameraRig_PUBG");
        if (pubgCam != null && pubgRig == null) pubgRig = pubgCam.gameObject;

        // Tim/Tao FirstPersonCamera tren Main Camera
        fpc = mainCam.GetComponent<FirstPersonCamera>();
        if (fpc == null)
        {
            fpc = mainCam.gameObject.AddComponent<FirstPersonCamera>();
        }
        if (player != null)
        {
            fpc.SetPlayerBody(player.transform);
        }
    }

    public static void SetFirstPerson()
    {
        if (Instance != null) Instance.SetCameraMode(CameraMode.FirstPerson);
    }

    public static void SetThirdPerson()
    {
        if (Instance != null) Instance.SetCameraMode(CameraMode.ThirdPersonPUBG);
    }

    public static void TogglePerspective()
    {
        if (Instance != null) Instance.ToggleCameraMode();
    }

    public void ToggleCameraMode()
    {
        CameraMode target = currentMode == CameraMode.FirstPerson ? CameraMode.ThirdPersonPUBG : CameraMode.FirstPerson;
        SetCameraMode(target);
    }

    public void SetCameraMode(CameraMode mode)
    {
        currentMode = mode;
        if (mainCam == null) InitComponents();

        if (mode == CameraMode.FirstPerson)
        {
            ApplyFirstPerson();
        }
        else
        {
            ApplyThirdPerson();
        }
    }

    private void ApplyFirstPerson()
    {
        if (player == null || eyePoint == null || mainCam == null) return;

        // 1. Tat PubgCamera
        if (pubgCam != null) pubgCam.enabled = false;
        if (pubgRig != null) pubgRig.SetActive(false);

        // 2. Gan Main Camera vao EyePoint tren Player
        mainCam.transform.SetParent(eyePoint, false);
        mainCam.transform.localPosition = Vector3.zero;
        mainCam.transform.localRotation = Quaternion.identity;
        mainCam.fieldOfView = fovFirstPerson;
        mainCam.nearClipPlane = 0.05f;

        // 3. Bat FirstPersonCamera
        if (fpc == null) fpc = mainCam.GetComponent<FirstPersonCamera>() ?? mainCam.gameObject.AddComponent<FirstPersonCamera>();
        fpc.enabled = true;
        fpc.SetPlayerBody(player.transform);

        // 4. An Mesh than Player (chi do bong, khong che mat camera)
        if (playerRenderers != null)
        {
            foreach (var r in playerRenderers)
            {
                if (r != null && r.gameObject == player)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
                }
            }
        }

        // 5. PlayerController dung Camera.main -> khong can goi lai Start
        //    (SendMessage("Start") truoc day gay de quy vo han -> StackOverflow)

        NotificationUI.ShowMessage("CHẾ ĐỘ: GÓC NHÌN THỨ NHẤT (FPS) [Bấm V để đổi]");
        Debug.Log("[CameraModeManager] Switched to FIRST PERSON mode.");
    }

    private void ApplyThirdPerson()
    {
        if (player == null || mainCam == null) return;

        // 1. Tat FirstPersonCamera
        if (fpc != null) fpc.enabled = false;

        // 2. Kich hoat lai PUBG Camera Rig
        if (pubgRig == null)
        {
            pubgRig = new GameObject("CameraRig_PUBG");
            pubgCam = pubgRig.AddComponent<PubgCamera>();
        }
        pubgRig.SetActive(true);
        if (pubgCam == null) pubgCam = pubgRig.GetComponent<PubgCamera>() ?? pubgRig.AddComponent<PubgCamera>();
        pubgCam.enabled = true;

        // Gan target
        pubgCam.SetTarget(player.transform);

        // 3. Gan Main Camera vao CameraRig_PUBG
        mainCam.transform.SetParent(pubgRig.transform, false);
        mainCam.transform.localPosition = new Vector3(0, 0, -3.5f);
        mainCam.transform.localRotation = Quaternion.identity;
        mainCam.fieldOfView = fovThirdPerson;
        mainCam.nearClipPlane = 0.1f;

        // 4. Hien lai Renderer than Player
        if (playerRenderers != null)
        {
            foreach (var r in playerRenderers)
            {
                if (r != null && r.gameObject == player)
                {
                    r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;
                    r.enabled = true;
                }
            }
        }

        // 5. PlayerController dung Camera.main -> khong can goi lai Start

        NotificationUI.ShowMessage("CHẾ ĐỘ: GÓC NHÌN THỨ BA (PUBG TPS) [Bấm V để đổi]");
        Debug.Log("[CameraModeManager] Switched to THIRD PERSON PUBG mode.");
    }
}
