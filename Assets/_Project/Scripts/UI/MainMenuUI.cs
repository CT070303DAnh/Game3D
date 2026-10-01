using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MainMenuUI: Quan ly Main Menu scene.
/// Attach vao: MainMenuUI GameObject trong MainMenu scene.
/// Chua GameManager trong scene rieng hoac DontDestroyOnLoad.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button exitButton;

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject mainPanel;

    [Header("Settings")]
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider sfxVolumeSlider;
    [SerializeField] private Button closeSettingsButton;

    private void Start()
    {
        // Dam bao settings panel dong khi vao menu
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);

        SetupButtons();
    }

    private void SetupButtons()
    {
        if (playButton != null)
            playButton.onClick.AddListener(OnPlayClicked);

        if (settingsButton != null)
            settingsButton.onClick.AddListener(OnSettingsClicked);

        if (exitButton != null)
            exitButton.onClick.AddListener(OnExitClicked);

        if (closeSettingsButton != null)
            closeSettingsButton.onClick.AddListener(OnCloseSettings);
    }

    private void OnPlayClicked()
    {
        Debug.Log("[MainMenuUI] Play clicked.");
        if (GameManager.Instance != null)
            GameManager.Instance.StartGame();
        else
            Debug.LogError("[MainMenuUI] GameManager not found! Make sure GameManager exists in scene.");
    }

    private void OnSettingsClicked()
    {
        if (mainPanel != null) mainPanel.SetActive(false);
        if (settingsPanel != null) settingsPanel.SetActive(true);
    }

    private void OnCloseSettings()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
    }

    private void OnExitClicked()
    {
        Debug.Log("[MainMenuUI] Exit clicked.");
        if (GameManager.Instance != null)
            GameManager.Instance.QuitGame();
        else
            Application.Quit();
    }

    private void OnDestroy()
    {
        // Cleanup listeners de tranh memory leak
        if (playButton != null) playButton.onClick.RemoveAllListeners();
        if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
        if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveAllListeners();
    }
}
