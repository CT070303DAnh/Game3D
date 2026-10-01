using UnityEngine;

/// <summary>
/// Terminal: Man hinh may tinh de xem clue hoac giai puzzle.
/// Implement IInteractable. Hien thi clue text qua UI.
/// Attach vao: Terminal GameObject (Interactable layer)
/// </summary>
public class Terminal : MonoBehaviour, IInteractable
{
    [Header("Terminal Settings")]
    [SerializeField] private string terminalTitle = "TERMINAL";
    [SerializeField, TextArea(3,8)] private string clueText = "Clue goes here...";
    [SerializeField] private bool requiresPower = true;
    [SerializeField] private GameObject screenGlowObject; // Light/emission khi bat

    [Header("VFX")]
    [SerializeField] private Light screenLight;
    [SerializeField] private Color onColor = new Color(0f, 0.8f, 1f);
    [SerializeField] private Color offColor = Color.black;

    private bool isPowered;
    private bool isOn;

    // IInteractable
    public bool CanInteract => !requiresPower || isPowered;
    public string InteractPromptText => CanInteract ? $"Xem {terminalTitle}" : "Không có điện";

    private void OnEnable()
    {
        GameState.OnPowerRestored += OnPowerRestored;
    }

    private void OnDisable()
    {
        GameState.OnPowerRestored -= OnPowerRestored;
    }

    private void Start()
    {
        // Neu khong can power, bat ngay
        if (!requiresPower) TurnOn();
        else TurnOff();
    }

    public void Interact()
    {
        if (!CanInteract)
        {
            NotificationUI.ShowMessage("Terminal chưa có điện.");
            return;
        }
        // Hien thi clue qua UIManager
        TerminalUI.ShowTerminal(terminalTitle, clueText);
        Debug.Log($"[Terminal] Player read: {terminalTitle}");
    }

    private void OnPowerRestored()
    {
        isPowered = true;
        TurnOn();
    }

    public void TurnOn()
    {
        isOn = true;
        isPowered = true;
        if (screenLight != null) { screenLight.enabled = true; screenLight.color = onColor; }
        if (screenGlowObject != null) screenGlowObject.SetActive(true);
    }

    public void TurnOff()
    {
        isOn = false;
        if (screenLight != null) { screenLight.enabled = false; }
        if (screenGlowObject != null) screenGlowObject.SetActive(false);
    }
}
