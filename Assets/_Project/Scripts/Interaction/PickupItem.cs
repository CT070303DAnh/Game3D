using UnityEngine;

/// <summary>
/// PickupItem: Vat pham co the nhat len.
/// Implement IInteractable. Khi Interact() -> cap nhat GameState -> hien thong bao -> tu huy.
/// Attach vao: Item GameObject
/// </summary>
public class PickupItem : MonoBehaviour, IInteractable
{
    public enum ItemType { SecurityCard, Fuse, Battery, AccessCode, LaboratoryKey, EngineerNote, Medkit, Wrench, JetFuel }

    [Header("Item Settings")]
    [SerializeField] private ItemType itemType = ItemType.Battery;
    [SerializeField] private string itemName = "Vật phẩm";
    [SerializeField] [TextArea(2, 4)] private string pickupMessage = "";
    [SerializeField] private int healAmount = 35;

    [Header("VFX")]
    [SerializeField] private GameObject pickupVFX;
    [SerializeField] private float bobSpeed = 1.8f;
    [SerializeField] private float bobAmount = 0.12f;
    [SerializeField] private float rotateSpeed = 65f;

    private Vector3 startPos;
    private bool collected;

    // IInteractable
    public string InteractPromptText => $"Nhặt {itemName} [E]";
    public bool CanInteract => !collected;

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 1.0f;
            sc.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
        }
    }

    private void Start()
    {
        startPos = transform.position;
        if (itemType == ItemType.Wrench)
        {
            EnsureWrenchVisuals();
        }
    }

    private void Update()
    {
        // Bob + rotate de noi bat
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;
        if (other.CompareTag("Player") || other.GetComponent<PlayerHealth>() != null || other.GetComponent<CharacterController>() != null)
        {
            Interact();
        }
    }

    public void Interact()
    {
        if (collected) return;
        collected = true;

        ApplyToGameState();

        string msg = !string.IsNullOrEmpty(pickupMessage) ? pickupMessage : $"ĐÃ NHẶT: {itemName}";
        NotificationUI.ShowMessage(msg);

        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, Quaternion.identity);

        Debug.Log($"[PickupItem] Collected: {itemName}");
        Destroy(gameObject);
    }

    private void ApplyToGameState()
    {
        switch (itemType)
        {
            case ItemType.SecurityCard:
                GameState.Instance?.CollectSecurityCard();
                break;
            case ItemType.Fuse:
                GameState.Instance?.CollectFuse();
                break;
            case ItemType.Battery:
            case ItemType.Medkit:
                GameState.Instance?.CollectBattery();
                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                if (ph != null) ph.Heal(healAmount);
                break;
            case ItemType.AccessCode:
                GameState.Instance?.SetAccessCodeFound();
                break;
            case ItemType.LaboratoryKey:
                GameState.Instance?.CollectLaboratoryKey();
                break;
            case ItemType.JetFuel:
                GameState.Instance?.CollectJetFuel();
                break;
            case ItemType.Wrench:
                GameState.Instance?.CollectWrench();
                break;
            case ItemType.EngineerNote:
                // Thong bao da duoc hien thi qua pickupMessage
                break;
        }
    }

    private void EnsureWrenchVisuals()
    {
        if (transform.Find("WrenchModel") != null) return;

        // An mesh mac dinh cua Capsule hoac Cube neu co
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");
        Material wrenchMat = new Material(litShader);
        wrenchMat.color = new Color(0.88f, 0.90f, 0.94f);
        wrenchMat.SetFloat("_Metallic", 0.92f);
        wrenchMat.SetFloat("_Smoothness", 0.82f);

        var modelRoot = new GameObject("WrenchModel");
        modelRoot.transform.SetParent(transform, false);
        modelRoot.transform.localRotation = Quaternion.Euler(20f, 45f, 0f);

        // Can co le
        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(modelRoot.transform, false);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.08f, 0.55f, 0.04f);
        handle.GetComponent<Renderer>().material = wrenchMat;
        Destroy(handle.GetComponent<Collider>());

        // Dau ngam co le
        var headBase = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        headBase.name = "JawHead";
        headBase.transform.SetParent(modelRoot.transform, false);
        headBase.transform.localPosition = new Vector3(0, 0.32f, 0);
        headBase.transform.localScale = new Vector3(0.24f, 0.04f, 0.24f);
        headBase.GetComponent<Renderer>().material = wrenchMat;
        Destroy(headBase.GetComponent<Collider>());

        var jawL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jawL.name = "Jaw_L";
        jawL.transform.SetParent(modelRoot.transform, false);
        jawL.transform.localPosition = new Vector3(-0.09f, 0.44f, 0);
        jawL.transform.localScale = new Vector3(0.06f, 0.18f, 0.04f);
        jawL.GetComponent<Renderer>().material = wrenchMat;
        Destroy(jawL.GetComponent<Collider>());

        var jawR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jawR.name = "Jaw_R";
        jawR.transform.SetParent(modelRoot.transform, false);
        jawR.transform.localPosition = new Vector3(0.09f, 0.44f, 0);
        jawR.transform.localScale = new Vector3(0.06f, 0.18f, 0.04f);
        jawR.GetComponent<Renderer>().material = wrenchMat;
        Destroy(jawR.GetComponent<Collider>());

        // Vong chu O o duoi can
        var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        ring.name = "RingEnd";
        ring.transform.SetParent(modelRoot.transform, false);
        ring.transform.localPosition = new Vector3(0, -0.32f, 0);
        ring.transform.localScale = new Vector3(0.18f, 0.038f, 0.18f);
        ring.GetComponent<Renderer>().material = wrenchMat;
        Destroy(ring.GetComponent<Collider>());

        // Den chieu sang toa quang cho Co Le
        if (GetComponentInChildren<Light>() == null)
        {
            var lightGO = new GameObject("WrenchLight");
            lightGO.transform.SetParent(transform, false);
            lightGO.transform.localPosition = new Vector3(0, 0.2f, 0);
            var l = lightGO.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1.0f, 0.85f, 0.3f);
            l.range = 3.5f;
            l.intensity = 2.2f;
        }

        // Bien 3D danh dau Co Le
        if (transform.Find("WrenchLabel") == null)
        {
            var signGO = new GameObject("WrenchLabel");
            signGO.transform.SetParent(transform, false);
            signGO.transform.localPosition = new Vector3(0, 0.7f, 0);
            signGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
            signGO.transform.localScale = Vector3.one * 0.022f;
            var tm = signGO.AddComponent<TextMesh>();
            tm.text = "[ CỜ LÊ SỬA CHỮA ]";
            tm.fontSize = 32;
            tm.alignment = TextAlignment.Center;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.color = Color.yellow;
        }
    }
}
