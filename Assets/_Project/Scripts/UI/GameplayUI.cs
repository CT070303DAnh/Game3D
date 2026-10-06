using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// GameplayUI: Quan ly HUD gameplay (HP bar, objective text, win/gameover screen, crosshair).
/// Attach vao: GameplayCanvas / GameplayUI GameObject
/// </summary>
public class GameplayUI : MonoBehaviour
{
    public static GameplayUI Instance { get; private set; }

    [Header("HP")]
    [SerializeField] private Slider hpBar;
    [SerializeField] private Text hpText;
    [SerializeField] private Image hpFillImage;

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

    private GameObject crosshairGO;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        AccessCodeUI.EnsureEventSystem();

        Canvas c = GetComponentInParent<Canvas>();
        if (c == null) c = FindFirstObjectByType<Canvas>();
        if (c != null && c.GetComponent<GraphicRaycaster>() == null)
        {
            c.gameObject.AddComponent<GraphicRaycaster>();
        }

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

        // Dam bao visual thanh mau luon hoat dong dep mat
        EnsureHealthBar();
        EnsureCrosshair();
        EnsureInventoryUI();
    }

    private void Update()
    {
        // An tam ngam Crosshair khi mo cac menu/modal/inventory
        if (crosshairGO != null)
        {
            bool modalOpen = AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen || InventoryUI.IsOpen ||
                (winPanel != null && winPanel.activeSelf) || (gameOverPanel != null && gameOverPanel.activeSelf);
            crosshairGO.SetActive(!modalOpen);
        }
    }

    /// <summary>
    /// Tu dong thiet lap day du cac layer visual cua thanh mau (Background, FillArea, Fill, Text)
    /// neu scene chua co hoac bi thieu fillRect.
    /// </summary>
    public void EnsureHealthBar()
    {
        if (hpBar == null)
        {
            hpBar = GetComponentInChildren<Slider>(true);
        }

        if (hpBar != null)
        {
            // Dam bao panel cha chua thanh mau dang bat
            if (hpBar.transform.parent != null)
            {
                var parentGO = hpBar.transform.parent.gameObject;
                if (!parentGO.activeSelf) parentGO.SetActive(true);

                // Dong bo kich thuoc va vi tri HPPanel chuan nhu Man 2
                var pnlR = parentGO.GetComponent<RectTransform>();
                if (pnlR != null)
                {
                    pnlR.anchorMin = new Vector2(0f, 1f);
                    pnlR.anchorMax = new Vector2(0f, 1f);
                    pnlR.pivot = new Vector2(0f, 1f);
                    pnlR.anchoredPosition = new Vector2(20f, -20f);
                    pnlR.sizeDelta = new Vector2(290f, 62f);
                }

                var pnlImg = parentGO.GetComponent<Image>();
                if (pnlImg != null)
                {
                    pnlImg.color = new Color(0.06f, 0.08f, 0.12f, 0.92f);
                }
            }

            // Kiem tra Background cua Slider (mau do tham den nhu Man 2)
            Image bg = hpBar.GetComponent<Image>();
            if (bg == null)
            {
                bg = hpBar.gameObject.AddComponent<Image>();
            }
            bg.color = new Color(0.18f, 0.08f, 0.08f, 0.95f);

            // Kiem tra FillArea & Fill
            Transform fa = hpBar.transform.Find("FillArea") ?? hpBar.transform.Find("Fill Area");
            if (fa == null)
            {
                var faGO = new GameObject("FillArea");
                faGO.transform.SetParent(hpBar.transform, false);
                var faR = faGO.AddComponent<RectTransform>();
                faR.anchorMin = Vector2.zero;
                faR.anchorMax = Vector2.one;
                faR.offsetMin = new Vector2(2, 2);
                faR.offsetMax = new Vector2(-2, -2);
                fa = faGO.transform;
            }
            else
            {
                var faR = fa.GetComponent<RectTransform>();
                if (faR != null)
                {
                    faR.offsetMin = new Vector2(2, 2);
                    faR.offsetMax = new Vector2(-2, -2);
                }
            }

            Transform fi = fa.Find("Fill");
            if (fi == null)
            {
                var fiGO = new GameObject("Fill");
                fiGO.transform.SetParent(fa, false);
                var fiR = fiGO.AddComponent<RectTransform>();
                fiR.anchorMin = Vector2.zero;
                fiR.anchorMax = Vector2.one;
                fiR.offsetMin = Vector2.zero;
                fiR.offsetMax = Vector2.zero;
                hpFillImage = fiGO.AddComponent<Image>();
                hpFillImage.color = new Color(0.2f, 0.9f, 0.35f);
                fi = fiGO.transform;
            }
            else
            {
                hpFillImage = fi.GetComponent<Image>();
            }

            hpBar.fillRect = fi.GetComponent<RectTransform>();

            // Can chinh lai vi tri thanh mau nam gon trong HPPanel nhu Man 2
            RectTransform slR = hpBar.GetComponent<RectTransform>();
            slR.anchorMin = new Vector2(0f, 0f);
            slR.anchorMax = new Vector2(1f, 0f);
            slR.pivot = new Vector2(0.5f, 0f);
            slR.anchoredPosition = new Vector2(0, 10);
            slR.sizeDelta = new Vector2(-24, 20);
        }

        // Kiem tra HPText chuan font, size, outline nhu Man 2
        if (hpText == null)
        {
            hpText = GetComponentInChildren<Text>(true);
        }
        if (hpText != null)
        {
            RectTransform txtR = hpText.GetComponent<RectTransform>();
            txtR.anchorMin = new Vector2(0.5f, 1f);
            txtR.anchorMax = new Vector2(0.5f, 1f);
            txtR.pivot = new Vector2(0.5f, 1f);
            txtR.anchoredPosition = new Vector2(0, -6);
            txtR.sizeDelta = new Vector2(260, 24);
            hpText.fontSize = 15;
            hpText.fontStyle = FontStyle.Bold;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;

            var ol = hpText.GetComponent<Outline>();
            if (ol == null) ol = hpText.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.9f);
            ol.effectDistance = new Vector2(1.5f, -1.5f);
        }

        // Cap nhat ngay gia tri HP hien tai
        PlayerHealth ph = FindFirstObjectByType<PlayerHealth>();
        if (ph != null)
        {
            UpdateHP(ph.CurrentHealth, ph.MaxHealth);
        }
        else
        {
            UpdateHP(100, 100);
        }
    }

    public void EnsureInventoryUI()
    {
        var inv = GetComponent<InventoryUI>();
        if (inv == null) inv = FindFirstObjectByType<InventoryUI>();
        if (inv == null) inv = gameObject.AddComponent<InventoryUI>();
        inv.EnsureInventoryPanel();
    }

    private void EnsureCrosshair()
    {
        if (crosshairGO == null)
        {
            crosshairGO = new GameObject("HUD_Crosshair");
            crosshairGO.transform.SetParent(transform, false);
            var rt = crosshairGO.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(8, 8);
            rt.anchoredPosition = Vector2.zero;

            var img = crosshairGO.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0.9f);
            img.raycastTarget = false;

            var ol = crosshairGO.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.8f);
            ol.effectDistance = new Vector2(1f, -1f);
        }
    }

    private void UpdateHP(int current, int max)
    {
        float pct = max > 0 ? (float)current / max : 0f;
        if (hpBar != null) hpBar.value = pct;
        if (hpText != null) hpText.text = $"HP: {current} / {max}";

        if (hpFillImage != null)
        {
            if (pct > 0.55f)
                hpFillImage.color = new Color(0.2f, 0.92f, 0.38f); // Xanh la tuoi
            else if (pct > 0.25f)
                hpFillImage.color = new Color(1.0f, 0.72f, 0.1f);  // Vang cam canh bao
            else
                hpFillImage.color = new Color(0.95f, 0.2f, 0.2f);  // Do nguy hiem
        }
    }

    private void UpdateObjective(ObjectiveManager.Objective obj)
    {
        if (objectiveText == null) return;
        objectiveText.text = obj != null ? $"OBJECTIVE\n{obj.description}" : "OBJECTIVE\nEscape!";
    }

    private void ShowWin()
    {
        AccessCodeUI.ForceClose();
        TerminalUI.ForceClose();
        ElevatorKeypadUI.ForceClose();
        if (winPanel != null) winPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    private void ShowGameOver()
    {
        AccessCodeUI.ForceClose();
        TerminalUI.ForceClose();
        ElevatorKeypadUI.ForceClose();
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 1f;
    }

    public void SetPromptVisible(bool visible)
    {
        if (interactionPromptPanel != null)
        {
            if (visible && (AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen))
                return;
            interactionPromptPanel.SetActive(visible);
        }
    }

    private void ShowPrompt(IInteractable interactable)
    {
        if (interactionPromptPanel == null) return;
        if (AccessCodeUI.IsOpen || TerminalUI.IsOpen || ElevatorKeypadUI.IsOpen) return;

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
