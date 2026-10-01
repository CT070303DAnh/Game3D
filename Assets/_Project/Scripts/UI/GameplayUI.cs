using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameplayUI: Quan ly HUD gameplay (HP bar, objective text, win/gameover screen).
/// Attach vao: GameplayUI GameObject
/// </summary>
public class GameplayUI : MonoBehaviour
{
    public static GameplayUI Instance { get; private set; }

    [Header("HP")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private Text hpText;

    [Header("Objective")]
    [SerializeField] private Text objectiveText;
    [SerializeField] private GameObject objectivePanel;

    [Header("Win Screen")]
    [SerializeField] private GameObject winPanel;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Button mainMenuButton;

    [Header("Game Over Screen")]
    [SerializeField] private GameObject gameOverPanel;
    [SerializeField] private Button retryButton;
    [SerializeField] private Button goMenuButton;

    [Header("Interaction Prompt")]
    [SerializeField] private GameObject interactionPromptPanel;
    [SerializeField] private Text interactionPromptText;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Auto ensure EventSystem exists
        AccessCodeUI.EnsureEventSystem();

        // Auto ensure GraphicRaycaster on canvas
        Canvas c = GetComponentInParent<Canvas>();
        if (c == null) c = FindFirstObjectByType<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null)
        {
            c.gameObject.AddComponent<GraphicRaycaster>();
        }

        // Tat raycastTarget tren prompt panel va text de khong chan click chuot vao cac nut phia sau
        if (interactionPromptPanel != null)
        {
            var img = interactionPromptPanel.GetComponent<Image>();
            if (img != null) img.raycastTarget = false;
        }
        if (interactionPromptText != null)
        {
            interactionPromptText.raycastTarget = false;
        }
    }

    private void OnEnable()
    {
        PlayerHealth.OnHealthChanged += UpdateHP;
        PlayerHealth.OnPlayerDied += ShowGameOver;
        GameManager.OnGameWin += ShowWin;
        ObjectiveManager.OnCurrentObjectiveChanged += UpdateObjective;
        PlayerInteraction.OnInteractableFound += ShowPrompt;
        PlayerInteraction.OnInteractableLost += HidePrompt;
    }

    private void OnDisable()
    {
        PlayerHealth.OnHealthChanged -= UpdateHP;
        PlayerHealth.OnPlayerDied -= ShowGameOver;
        GameManager.OnGameWin -= ShowWin;
        ObjectiveManager.OnCurrentObjectiveChanged -= UpdateObjective;
        PlayerInteraction.OnInteractableFound -= ShowPrompt;
        PlayerInteraction.OnInteractableLost -= HidePrompt;
    }

    private void Start()
    {
        if (winPanel != null) winPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);

        // Buttons
        if (playAgainButton != null) playAgainButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (mainMenuButton != null)  mainMenuButton.onClick.AddListener(() => GameManager.Instance?.LoadMainMenu());
        if (retryButton != null)     retryButton.onClick.AddListener(() => GameManager.Instance?.RestartGame());
        if (goMenuButton != null)    goMenuButton.onClick.AddListener(() => GameManager.Instance?.LoadMainMenu());
    }

    private void UpdateHP(int current, int max)
    {
        if (hpBar != null) hpBar.value = (float)current / max;
        if (hpText != null) hpText.text = $"HP: {current}/{max}";
    }

    private void UpdateObjective(ObjectiveManager.Objective obj)
    {
        if (objectiveText == null) return;
        objectiveText.text = obj != null ? $"OBJECTIVE\n{obj.description}" : "OBJECTIVE\nEscape!";
    }

    private void ShowWin()
    {
        // Dong tat ca panel dang mo truoc
        AccessCodeUI.ForceClose();
        TerminalUI.ForceClose();
        if (winPanel != null) winPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    private void ShowGameOver()
    {
        // Dong tat ca panel dang mo truoc
        AccessCodeUI.ForceClose();
        TerminalUI.ForceClose();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    public void SetPromptVisible(bool visible)
    {
        if (interactionPromptPanel != null)
        {
            if (visible && (AccessCodeUI.IsOpen || TerminalUI.IsOpen))
                return; // Khong hien prompt neu modal dang mo
            interactionPromptPanel.SetActive(visible);
        }
    }

    private void ShowPrompt(IInteractable interactable)
    {
        if (interactionPromptPanel == null) return;
        if (AccessCodeUI.IsOpen || TerminalUI.IsOpen) return; // Tranh de len panel

        interactionPromptPanel.SetActive(true);
        if (interactionPromptText != null)
            interactionPromptText.text = interactable.InteractPromptText;
    }

    private void HidePrompt()
    {
        if (interactionPromptPanel != null) interactionPromptPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        if (playAgainButton != null) playAgainButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null) mainMenuButton.onClick.RemoveAllListeners();
        if (retryButton != null) retryButton.onClick.RemoveAllListeners();
        if (goMenuButton != null) goMenuButton.onClick.RemoveAllListeners();
    }
}
