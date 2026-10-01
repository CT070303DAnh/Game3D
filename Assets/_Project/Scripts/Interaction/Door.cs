using UnityEngine;
using System.Collections;

/// <summary>
/// Door: Cua co the mo/dong, co the khoa.
/// Implement IInteractable. Ho tro nhieu dieu kien mo cua.
/// Attach vao: Door GameObject
/// Required: Animator, Collider (Interactable layer), AudioSource
/// Animator params: IsOpen (bool)
/// </summary>
[RequireComponent(typeof(Animator))]
public class Door : MonoBehaviour, IInteractable
{
    public enum DoorRequirement { None, SecurityCard, LaboratoryKey, PowerRestored, ExitCode }
    public enum DoorState { Closed, Opening, Open, Closing }

    [Header("Door Settings")]
    [SerializeField] private DoorRequirement requirement = DoorRequirement.None;
    [SerializeField] private bool isLocked = false;
    [SerializeField] private float autoCloseDelay = 0f; // 0 = khong tu dong dong

    [Header("Messages")]
    [SerializeField] private string lockedMessage = "Door is locked";
    [SerializeField] private string requirementMessage = "Access denied";
    [SerializeField] private string openMessage = "Open Door";

    [Header("Audio")]
    [SerializeField] private AudioClip openSound;
    [SerializeField] private AudioClip closeSound;
    [SerializeField] private AudioClip lockedSound;

    [Header("VFX")]
    [SerializeField] private GameObject unlockVFX;

    private Animator animator;
    private AudioSource audioSource;
    private DoorState currentState = DoorState.Closed;
    private static readonly int HashIsOpen = Animator.StringToHash("IsOpen");

    public DoorState State => currentState;
    public bool IsOpen => currentState == DoorState.Open;

    // IInteractable
    public bool CanInteract => currentState == DoorState.Closed || currentState == DoorState.Open;
    public string InteractPromptText
    {
        get
        {
            if (isLocked) return lockedMessage;
            if (!MeetsRequirement()) return requirementMessage;
            return IsOpen ? "Close Door" : openMessage;
        }
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void OnEnable()
    {
        // Lang nghe event khi power restored de mo cua may tinh
        GameState.OnPowerRestored += OnPowerRestored;
        GameState.OnExitUnlocked += OnExitUnlocked;
    }

    private void OnDisable()
    {
        GameState.OnPowerRestored -= OnPowerRestored;
        GameState.OnExitUnlocked -= OnExitUnlocked;
    }

    public void Interact()
    {
        if (!CanInteract) return;

        if (isLocked)
        {
            PlaySound(lockedSound);
            NotificationUI.ShowMessage(lockedMessage);
            return;
        }

        if (!MeetsRequirement())
        {
            PlaySound(lockedSound);
            NotificationUI.ShowMessage(requirementMessage);
            return;
        }

        if (IsOpen) CloseDoor();
        else OpenDoor();
    }

    public void OpenDoor()
    {
        if (currentState == DoorState.Open || currentState == DoorState.Opening) return;
        currentState = DoorState.Opening;
        if (animator.runtimeAnimatorController != null)
            animator.SetBool(HashIsOpen, true);
        else
            // Fallback khong co animator: xoay cua
            StartCoroutine(RotateDoorOpen());
        PlaySound(openSound);

        if (autoCloseDelay > 0)
            StartCoroutine(AutoClose());

        currentState = DoorState.Open;
        Debug.Log($"[Door] {name} opened.");
    }

    public void CloseDoor()
    {
        if (currentState == DoorState.Closed) return;
        currentState = DoorState.Closing;
        if (animator.runtimeAnimatorController != null)
            animator.SetBool(HashIsOpen, false);
        else
            StartCoroutine(RotateDoorClose());
        PlaySound(closeSound);
        currentState = DoorState.Closed;
    }

    public void Unlock()
    {
        isLocked = false;
        if (unlockVFX != null) Instantiate(unlockVFX, transform.position, Quaternion.identity);
        Debug.Log($"[Door] {name} unlocked.");
    }

    private bool MeetsRequirement()
    {
        if (GameState.Instance == null) return true;
        return requirement switch
        {
            DoorRequirement.SecurityCard  => GameState.Instance.SecurityCardCollected,
            DoorRequirement.LaboratoryKey => GameState.Instance.LaboratoryKeyCollected,
            DoorRequirement.PowerRestored => GameState.Instance.PowerRestored,
            DoorRequirement.ExitCode      => GameState.Instance.ExitUnlocked,
            _                             => true,
        };
    }

    private void OnPowerRestored()
    {
        if (requirement == DoorRequirement.PowerRestored)
            OpenDoor();
    }

    private void OnExitUnlocked()
    {
        if (requirement == DoorRequirement.ExitCode)
            Unlock();
    }

    private IEnumerator AutoClose()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        CloseDoor();
    }

    private IEnumerator RotateDoorOpen()
    {
        float elapsed = 0f; float duration = 0.8f;
        Quaternion from = transform.localRotation;
        Quaternion to = from * Quaternion.Euler(0, 90, 0);
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Slerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime; yield return null;
        }
        transform.localRotation = to;
    }

    private IEnumerator RotateDoorClose()
    {
        float elapsed = 0f; float duration = 0.8f;
        Quaternion from = transform.localRotation;
        Quaternion to = Quaternion.identity;
        while (elapsed < duration)
        {
            transform.localRotation = Quaternion.Slerp(from, to, elapsed / duration);
            elapsed += Time.deltaTime; yield return null;
        }
        transform.localRotation = to;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
