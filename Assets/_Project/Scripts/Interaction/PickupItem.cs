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
    [SerializeField] private ItemType itemType = ItemType.SecurityCard;
    [SerializeField] private string itemName = "Vật phẩm";
    [SerializeField] [TextArea(2, 4)] private string pickupMessage = "";
    [SerializeField] private int healAmount = 35;

    [Header("VFX")]
    [SerializeField] private GameObject pickupVFX;
    [SerializeField] private float bobSpeed = 1.8f;
    [SerializeField] private float bobAmount = 0.18f;
    [SerializeField] private float rotateSpeed = 65f;

    private Vector3 startPos;
    private bool collected;

    // IInteractable
    public string InteractPromptText => $"Nhặt {itemName} [E]";
    public bool CanInteract => !collected;

    private void Awake()
    {
        AutoDetectItemTypeFromName();

        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 2.5f;
            sc.isTrigger = true;
        }
        else
        {
            col.isTrigger = true;
            if (col is SphereCollider sc)
            {
                sc.radius = Mathf.Max(sc.radius, 2.5f);
            }
        }
    }

    public void AutoDetectItemTypeFromName()
    {
        string n = gameObject.name.ToLower();
        if (n.Contains("wrench") || n.Contains("cole") || n.Contains("crank") || n.Contains("spanner"))
        {
            itemType = ItemType.Wrench;
            itemName = "Cờ Lê Sửa Chữa (Wrench)";
            if (string.IsNullOrEmpty(pickupMessage) || pickupMessage.Contains("Vật phẩm") || pickupMessage.Contains("Pin"))
            {
                pickupMessage = "✓ ĐÃ NHẶT CỜ LÊ SỬA CHỮA! Hãy dùng Cờ Lê để vặn khóa 3 van xả khí độc.";
            }
        }
        else if (n.Contains("card") || n.Contains("thebao"))
        {
            itemType = ItemType.SecurityCard;
            itemName = "Thẻ Bảo Mật";
            if (string.IsNullOrEmpty(pickupMessage) || pickupMessage.Contains("Vật phẩm"))
            {
                pickupMessage = "✓ ĐÃ NHẶT THẺ BẢO MẬT! Dùng để mở Cửa Thoát Hiểm.";
            }
        }
        else if (n.Contains("fuse") || n.Contains("cauchi"))
        {
            itemType = ItemType.Fuse;
            itemName = "Cầu Chì Cao Áp (Power Fuse)";
            if (string.IsNullOrEmpty(pickupMessage) || pickupMessage.Contains("Vật phẩm"))
            {
                pickupMessage = "✓ ĐÃ NHẶT CẦU CHÌ CAO ÁP! Dùng để lắp vào Cầu Dao Điện Tổng.";
            }
        }
        else if (n.Contains("fuel") || n.Contains("nhienlieu") || n.Contains("xang"))
        {
            itemType = ItemType.JetFuel;
            itemName = "Bình Nhiên Liệu Trực Thăng";
        }
        else if (n.Contains("key") && !n.Contains("card") && !n.Contains("pad"))
        {
            itemType = ItemType.LaboratoryKey;
            itemName = "Chìa Khóa Phòng Lab";
        }
        else if (n.Contains("battery") || n.Contains("pin"))
        {
            itemType = ItemType.Battery;
            itemName = "Pin Năng Lượng (+HP)";
        }
    }

    private void Start()
    {
        startPos = transform.position;
        EnsurePSXItemVisuals();
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

    public void EnsurePSXItemVisuals()
    {
        var existingRoot = transform.Find("PSX_ModelRoot");
        if (existingRoot != null)
        {
            // Nếu đã tồn tại nhưng kích thước cũ bé hơn 4.5f, xóa đi để áp dụng kích thước gấp đôi mới
            if (existingRoot.localScale.x < 4.5f)
            {
                DestroyImmediate(existingRoot.gameObject);
                var oldLbl = transform.Find("Item3DLabel");
                if (oldLbl != null) DestroyImmediate(oldLbl.gameObject);
            }
            else
            {
                return;
            }
        }

        string modelResourceName = null;
        Vector3 localScale = Vector3.one;
        Quaternion localRot = Quaternion.identity;

        AutoDetectItemTypeFromName();

        switch (itemType)
        {
            case ItemType.SecurityCard:
                modelResourceName = "ItemModels/Model_SecurityCard";
                localScale = Vector3.one * 7.0f; // Tăng gấp đôi
                localRot = Quaternion.Euler(20f, 0f, 15f);
                break;
            case ItemType.Wrench:
                modelResourceName = "ItemModels/Model_Wrench";
                localScale = Vector3.one * 6.4f; // Tăng gấp đôi
                localRot = Quaternion.Euler(30f, 45f, 0f);
                break;
            case ItemType.Fuse:
                modelResourceName = "ItemModels/Model_Fuse";
                localScale = Vector3.one * 6.0f; // Tăng gấp đôi
                localRot = Quaternion.Euler(0f, 0f, 0f);
                break;
            case ItemType.Battery:
                modelResourceName = "ItemModels/Model_Battery";
                localScale = Vector3.one * 5.6f; // Tăng gấp đôi
                localRot = Quaternion.Euler(0f, 0f, 0f);
                break;
            case ItemType.LaboratoryKey:
                modelResourceName = "ItemModels/Model_LaboratoryKey";
                localScale = Vector3.one * 7.2f; // Tăng gấp đôi
                localRot = Quaternion.Euler(25f, 30f, 0f);
                break;
            case ItemType.AccessCode:
            case ItemType.EngineerNote:
                modelResourceName = "ItemModels/Model_AccessCode";
                localScale = Vector3.one * 6.0f; // Tăng gấp đôi
                localRot = Quaternion.Euler(30f, 15f, 0f);
                break;
            case ItemType.JetFuel:
                modelResourceName = "ItemModels/Model_Battery";
                localScale = Vector3.one * 6.4f;
                localRot = Quaternion.Euler(0f, 0f, 0f);
                break;
        }

        GameObject prefab = !string.IsNullOrEmpty(modelResourceName) ? Resources.Load<GameObject>(modelResourceName) : null;
        if (prefab != null)
        {
            var mr = GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            var oldWrench = transform.Find("WrenchModel");
            if (oldWrench != null) Destroy(oldWrench.gameObject);

            var root = new GameObject("PSX_ModelRoot");
            root.transform.SetParent(transform, false);
            root.transform.localPosition = Vector3.zero;
            root.transform.localRotation = localRot;
            root.transform.localScale = localScale;

            var inst = Instantiate(prefab, root.transform);
            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            foreach (var c in inst.GetComponentsInChildren<Collider>())
            {
                Destroy(c);
            }

            Shader urpLit = Shader.Find("Universal Render Pipeline/Lit");
            if (urpLit != null)
            {
                foreach (var rend in inst.GetComponentsInChildren<Renderer>())
                {
                    foreach (var mat in rend.materials)
                    {
                        if (mat.shader != urpLit)
                        {
                            Texture tex = mat.mainTexture;
                            Color col = mat.HasProperty("_Color") ? mat.color : Color.white;
                            mat.shader = urpLit;
                            if (tex != null) mat.SetTexture("_BaseMap", tex);
                            mat.SetColor("_BaseColor", col);
                        }
                    }
                }
            }

            if (GetComponentInChildren<Light>() == null)
            {
                var lGO = new GameObject("ItemGlowLight");
                lGO.transform.SetParent(transform, false);
                lGO.transform.localPosition = new Vector3(0, 0.5f, 0);
                var l = lGO.AddComponent<Light>();
                l.type = LightType.Point;
                l.range = 6.5f;
                l.intensity = 3.2f;
                l.color = GetGlowColorForItem(itemType);
            }

            if (transform.Find("Item3DLabel") == null)
            {
                var lblGO = new GameObject("Item3DLabel");
                lblGO.transform.SetParent(transform, false);
                lblGO.transform.localPosition = new Vector3(0, 1.35f, 0); // Đẩy chữ lên cao hơn để không đè vào model
                lblGO.transform.localRotation = Quaternion.Euler(0, 180f, 0);
                lblGO.transform.localScale = Vector3.one * 0.026f;
                var tm = lblGO.AddComponent<TextMesh>();
                tm.text = $"[ {itemName} ]";
                tm.fontSize = 32;
                tm.fontStyle = FontStyle.Bold;
                tm.alignment = TextAlignment.Center;
                tm.anchor = TextAnchor.MiddleCenter;
                tm.color = GetGlowColorForItem(itemType);
            }
        }
        else if (itemType == ItemType.Wrench)
        {
            EnsureFallbackWrenchVisuals();
        }
    }

    private Color GetGlowColorForItem(ItemType type)
    {
        switch (type)
        {
            case ItemType.SecurityCard: return new Color(0.2f, 0.9f, 1f);
            case ItemType.Wrench: return new Color(1f, 0.75f, 0.2f);
            case ItemType.Fuse: return new Color(1f, 0.85f, 0.1f);
            case ItemType.Battery: return new Color(0.3f, 1f, 0.4f);
            case ItemType.LaboratoryKey: return new Color(0.9f, 0.8f, 0.3f);
            case ItemType.AccessCode:
            case ItemType.EngineerNote: return new Color(0.3f, 0.7f, 1f);
            default: return Color.white;
        }
    }

    private void EnsureFallbackWrenchVisuals()
    {
        if (transform.Find("WrenchModel") != null) return;
        var mr = GetComponent<MeshRenderer>();
        if (mr != null) mr.enabled = false;

        Shader litShader = Shader.Find("Universal Render Pipeline/Lit");
        if (litShader == null) litShader = Shader.Find("Standard");
        Material wrenchMat = new Material(litShader);
        wrenchMat.color = new Color(0.88f, 0.90f, 0.94f);

        var modelRoot = new GameObject("WrenchModel");
        modelRoot.transform.SetParent(transform, false);
        modelRoot.transform.localRotation = Quaternion.Euler(20f, 45f, 0f);

        var handle = GameObject.CreatePrimitive(PrimitiveType.Cube);
        handle.name = "Handle";
        handle.transform.SetParent(modelRoot.transform, false);
        handle.transform.localPosition = Vector3.zero;
        handle.transform.localScale = new Vector3(0.08f, 0.55f, 0.04f);
        handle.GetComponent<Renderer>().material = wrenchMat;
        Destroy(handle.GetComponent<Collider>());
    }
}
