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
    }

    private void Update()
    {
        // Bob + rotate de noi bat
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime, Space.World);
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
            case ItemType.EngineerNote:
            case ItemType.Wrench:
                // Thong bao da duoc hien thi qua pickupMessage
                break;
        }
    }
}
