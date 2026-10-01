using UnityEngine;

/// <summary>
/// PlayerInteraction: Kiem tra IInteractable gan player, hien thi prompt va goi Interact().
/// Khong hard-code tung loai object — chi lam viec voi IInteractable interface.
///
/// Attach vao: Player GameObject
/// Phu thuoc: IMovementInput (de doc InteractPressed)
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    // ───────────────────────────────────────────────
    // Inspector
    // ───────────────────────────────────────────────
    [Header("Interaction Settings")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableMask;

    [Header("UI Prompt (optional)")]
    [SerializeField] private GameObject interactionPromptUI; // GameObject chua text "Press E"

    // ───────────────────────────────────────────────
    // Runtime State
    // ───────────────────────────────────────────────
    private IInteractable currentInteractable;
    private IMovementInput input;

    // ───────────────────────────────────────────────
    // Events
    // ───────────────────────────────────────────────
    public static event System.Action<IInteractable> OnInteractableFound;
    public static event System.Action OnInteractableLost;

    // ───────────────────────────────────────────────
    // Unity Lifecycle
    // ───────────────────────────────────────────────
    private void Start()
    {
        input = FindFirstObjectByType<MobileInputController>();
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(false);
    }

    private void Update()
    {
        if (GameManager.Instance != null && !GameManager.Instance.IsPlaying) return;

        DetectInteractable();
        HandleInteractInput();
    }

    // ───────────────────────────────────────────────
    // Detection — Sphere check quanh player
    // ───────────────────────────────────────────────
    private void DetectInteractable()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactRange, interactableMask);
        IInteractable nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider col in hits)
        {
            IInteractable interactable = col.GetComponent<IInteractable>();
            if (interactable == null) continue;

            float dist = Vector3.Distance(transform.position, col.transform.position);
            if (dist < nearestDist)
            {
                nearest = interactable;
                nearestDist = dist;
            }
        }

        // Cap nhat currentInteractable
        if (nearest != currentInteractable)
        {
            currentInteractable = nearest;

            if (currentInteractable != null)
            {
                ShowPrompt(true);
                OnInteractableFound?.Invoke(currentInteractable);
            }
            else
            {
                ShowPrompt(false);
                OnInteractableLost?.Invoke();
            }
        }
    }

    // ───────────────────────────────────────────────
    // Input Handler
    // ───────────────────────────────────────────────
    private void HandleInteractInput()
    {
        if (currentInteractable == null) return;
        if (input == null) return;

        if (input.InteractPressed)
        {
            Debug.Log($"[PlayerInteraction] Interacting with: {currentInteractable}");
            currentInteractable.Interact();
        }
    }

    // ───────────────────────────────────────────────
    // UI
    // ───────────────────────────────────────────────
    private void ShowPrompt(bool show)
    {
        if (interactionPromptUI != null)
            interactionPromptUI.SetActive(show);
    }

    // ───────────────────────────────────────────────
    // Debug
    // ───────────────────────────────────────────────
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = currentInteractable != null ? Color.green : Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRange);
    }
}
