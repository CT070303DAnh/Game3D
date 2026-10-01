using UnityEngine;

/// <summary>
/// PickupItem: Vat pham co the nhat len.
/// Implement IInteractable. Khi Interact() -> cap nhat GameState -> tu huy.
/// Attach vao: Item GameObject
/// Required: Collider (set layer = Interactable)
/// </summary>
public class PickupItem : MonoBehaviour, IInteractable
{
    public enum ItemType { SecurityCard, Fuse, Battery, AccessCode, LaboratoryKey }

    [Header("Item Settings")]
    [SerializeField] private ItemType itemType;
    [SerializeField] private string itemName = "Item";
    [SerializeField] private int healAmount = 30; // Chi dung khi Battery

    [Header("VFX")]
    [SerializeField] private GameObject pickupVFX;
    [SerializeField] private float bobSpeed = 1.5f;
    [SerializeField] private float bobAmount = 0.15f;
    [SerializeField] private float rotateSpeed = 60f;

    private Vector3 startPos;
    private bool collected;

    // IInteractable
    public string InteractPromptText => $"Nhặt {itemName}";
    public bool CanInteract => !collected;

    private void Start()
    {
        startPos = transform.position;
    }

    private void Update()
    {
        // Bob + rotate de noi bat
        float newY = startPos.y + Mathf.Sin(Time.time * bobSpeed) * bobAmount;
        transform.position = new Vector3(transform.position.x, newY, transform.position.z);
        transform.Rotate(Vector3.up, rotateSpeed * Time.deltaTime);
    }

    public void Interact()
    {
        if (collected) return;
        collected = true;

        ApplyToGameState();

        if (pickupVFX != null)
            Instantiate(pickupVFX, transform.position, Quaternion.identity);

        Debug.Log($"[PickupItem] Collected: {itemName}");
        Destroy(gameObject);
    }

    private void ApplyToGameState()
    {
        if (GameState.Instance == null) return;
        switch (itemType)
        {
            case ItemType.SecurityCard: GameState.Instance.CollectSecurityCard(); break;
            case ItemType.Fuse:         GameState.Instance.CollectFuse();         break;
            case ItemType.Battery:      GameState.Instance.CollectBattery();
                // Hoi phuc HP
                PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
                if (ph != null) ph.Heal(healAmount);
                break;
            case ItemType.AccessCode:   GameState.Instance.SetAccessCodeFound();  break;
            case ItemType.LaboratoryKey:GameState.Instance.CollectLaboratoryKey();break;
        }
    }
}
