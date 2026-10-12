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

    [Header("Leaderboard & Player Name")]
    [SerializeField] private InputField playerNameInput;
    [SerializeField] private Button leaderboardButton;

    private void Start()
    {
        // Dam bao settings panel dong khi vao menu
        if (settingsPanel != null) settingsPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);

        EnsureLeaderboardElements();
        RelayoutMainMenu();
        SetupButtons();
    }

    private void EnsureLeaderboardElements()
    {
        Canvas canvas = GetComponentInParent<Canvas>() ?? FindFirstObjectByType<Canvas>();
        Transform root = (mainPanel != null) ? mainPanel.transform : (canvas != null ? canvas.transform : transform);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. Ô Nhập Tên Người Chơi
        if (playerNameInput == null)
        {
            var existingInput = root.Find("PlayerNameContainer/PlayerNameInput")?.GetComponent<InputField>();
            if (existingInput != null) playerNameInput = existingInput;
        }

        if (playerNameInput == null)
        {
            var nameBoxGO = new GameObject("PlayerNameContainer");
            nameBoxGO.transform.SetParent(root, false);
            var nameBoxR = nameBoxGO.AddComponent<RectTransform>();
            nameBoxR.anchorMin = new Vector2(0.5f, 0.5f);
            nameBoxR.anchorMax = new Vector2(0.5f, 0.5f);
            nameBoxR.pivot = new Vector2(0.5f, 0.5f);
            nameBoxR.anchoredPosition = new Vector2(0f, 95f);
            nameBoxR.sizeDelta = new Vector2(340f, 60f);

            // Label tiêu đề
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(nameBoxGO.transform, false);
            var lblR = lblGO.AddComponent<RectTransform>();
            lblR.anchoredPosition = new Vector2(0f, 18f);
            lblR.sizeDelta = new Vector2(340f, 22f);
            var lblTxt = lblGO.AddComponent<Text>();
            lblTxt.text = "👤 NHẬP TÊN ĐỂ GHI DANH RANK:";
            lblTxt.font = font;
            lblTxt.fontSize = 13;
            lblTxt.fontStyle = FontStyle.Bold;
            lblTxt.alignment = TextAnchor.MiddleCenter;
            lblTxt.color = new Color(0.35f, 0.9f, 1f);

            // InputField GO
            var inputGO = new GameObject("PlayerNameInput");
            inputGO.transform.SetParent(nameBoxGO.transform, false);
            var inputR = inputGO.AddComponent<RectTransform>();
            inputR.anchoredPosition = new Vector2(0f, -10f);
            inputR.sizeDelta = new Vector2(340f, 36f);

            var inputImg = inputGO.AddComponent<Image>();
            inputImg.color = new Color(0.06f, 0.12f, 0.22f, 0.95f);

            var inputOl = inputGO.AddComponent<Outline>();
            inputOl.effectColor = new Color(0.2f, 0.7f, 1f, 0.9f);
            inputOl.effectDistance = new Vector2(1.5f, -1.5f);

            // Text hiển thị
            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(inputGO.transform, false);
            var txtR = txtGO.AddComponent<RectTransform>();
            txtR.anchorMin = Vector2.zero;
            txtR.anchorMax = Vector2.one;
            txtR.offsetMin = new Vector2(10f, 0f);
            txtR.offsetMax = new Vector2(-10f, 0f);
            var txt = txtGO.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 15;
            txt.fontStyle = FontStyle.Bold;
            txt.color = Color.yellow;
            txt.alignment = TextAnchor.MiddleCenter;

            // Placeholder Text
            var phGO = new GameObject("Placeholder");
            phGO.transform.SetParent(inputGO.transform, false);
            var phR = phGO.AddComponent<RectTransform>();
            phR.anchorMin = Vector2.zero;
            phR.anchorMax = Vector2.one;
            phR.offsetMin = new Vector2(10f, 0f);
            phR.offsetMax = new Vector2(-10f, 0f);
            var phTxt = phGO.AddComponent<Text>();
            phTxt.text = "Nhập tên của bạn...";
            phTxt.font = font;
            phTxt.fontSize = 15;
            phTxt.fontStyle = FontStyle.Italic;
            phTxt.color = new Color(0.6f, 0.7f, 0.8f, 0.6f);
            phTxt.alignment = TextAnchor.MiddleCenter;

            playerNameInput = inputGO.AddComponent<InputField>();
            playerNameInput.textComponent = txt;
            playerNameInput.placeholder = phTxt;
            playerNameInput.characterLimit = 16;
        }

        // Tự động load tên đã lưu
        if (playerNameInput != null && LeaderboardManager.Instance != null)
        {
            playerNameInput.text = LeaderboardManager.Instance.CurrentPlayerName;
            playerNameInput.onValueChanged.AddListener((val) =>
            {
                LeaderboardManager.Instance.CurrentPlayerName = val;
            });
        }

        // 2. Nút BẢNG XẾP HẠNG
        if (leaderboardButton == null)
        {
            var existingBtn = root.Find("LeaderboardButton")?.GetComponent<Button>();
            if (existingBtn != null) leaderboardButton = existingBtn;
        }

        if (leaderboardButton == null)
        {
            var btnGO = new GameObject("LeaderboardButton");
            btnGO.transform.SetParent(root, false);
            var btnR = btnGO.AddComponent<RectTransform>();
            btnR.anchorMin = new Vector2(0.5f, 0.5f);
            btnR.anchorMax = new Vector2(0.5f, 0.5f);
            btnR.pivot = new Vector2(0.5f, 0.5f);
            btnR.anchoredPosition = new Vector2(0f, -35f);
            btnR.sizeDelta = new Vector2(340f, 48f);

            var btnImg = btnGO.AddComponent<Image>();
            btnImg.color = new Color(0.72f, 0.52f, 0.08f, 0.95f);

            var btnOl = btnGO.AddComponent<Outline>();
            btnOl.effectColor = new Color(1f, 0.85f, 0.2f, 0.95f);
            btnOl.effectDistance = new Vector2(1.5f, -1.5f);

            leaderboardButton = btnGO.AddComponent<Button>();

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txtR = txtGO.AddComponent<RectTransform>();
            txtR.anchorMin = Vector2.zero;
            txtR.anchorMax = Vector2.one;
            txtR.offsetMin = Vector2.zero;
            txtR.offsetMax = Vector2.zero;

            var txt = txtGO.AddComponent<Text>();
            txt.text = "🏆 BẢNG XẾP HẠNG (RANK)";
            txt.font = font;
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
            txt.raycastTarget = false;
        }

        if (leaderboardButton != null)
        {
            leaderboardButton.onClick.RemoveAllListeners();
            leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        }
    }

    private void RelayoutMainMenu()
    {
        Transform root = (mainPanel != null) ? mainPanel.transform : transform;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 1. TitleText
        var titleTrans = root.Find("TitleText");
        if (titleTrans != null)
        {
            var titleR = titleTrans.GetComponent<RectTransform>();
            if (titleR != null)
            {
                titleR.anchoredPosition = new Vector2(0f, 165f);
                titleR.sizeDelta = new Vector2(500f, 60f);
            }
            var titleTxt = titleTrans.GetComponent<Text>();
            if (titleTxt != null)
            {
                titleTxt.text = "ESCAPE THE LAB";
                titleTxt.fontSize = 36;
                titleTxt.fontStyle = FontStyle.Bold;
                titleTxt.color = new Color(0.2f, 0.85f, 1f);
                titleTxt.alignment = TextAnchor.MiddleCenter;
            }
        }

        // 2. PlayButton
        if (playButton != null)
        {
            var playR = playButton.GetComponent<RectTransform>();
            if (playR != null)
            {
                playR.anchoredPosition = new Vector2(0f, 25f);
                playR.sizeDelta = new Vector2(340f, 48f);
            }
            FormatMenuButton(playButton, new Color(0.12f, 0.42f, 0.82f), new Color(0.25f, 0.75f, 1f), "[ PLAY ]", font);
        }

        // 3. LeaderboardButton
        if (leaderboardButton != null)
        {
            var lbR = leaderboardButton.GetComponent<RectTransform>();
            if (lbR != null)
            {
                lbR.anchoredPosition = new Vector2(0f, -35f);
                lbR.sizeDelta = new Vector2(340f, 48f);
            }
            FormatMenuButton(leaderboardButton, new Color(0.72f, 0.52f, 0.08f), new Color(1f, 0.85f, 0.2f), "🏆 BẢNG XẾP HẠNG (RANK)", font);
        }

        // 4. SettingsButton
        if (settingsButton != null)
        {
            var setR = settingsButton.GetComponent<RectTransform>();
            if (setR != null)
            {
                setR.anchoredPosition = new Vector2(0f, -95f);
                setR.sizeDelta = new Vector2(340f, 48f);
            }
            FormatMenuButton(settingsButton, new Color(0.15f, 0.35f, 0.65f), new Color(0.35f, 0.7f, 1f), "⚙ CÀI ĐẶT (SETTINGS)", font);
        }

        // 5. ExitButton
        if (exitButton != null)
        {
            var exitR = exitButton.GetComponent<RectTransform>();
            if (exitR != null)
            {
                exitR.anchoredPosition = new Vector2(0f, -155f);
                exitR.sizeDelta = new Vector2(340f, 48f);
            }
            FormatMenuButton(exitButton, new Color(0.25f, 0.12f, 0.18f), new Color(0.85f, 0.25f, 0.35f), "[ EXIT ]", font);
        }
    }

    private void FormatMenuButton(Button btn, Color bgColor, Color outlineColor, string labelText, Font font)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = bgColor;

        var ol = btn.GetComponent<Outline>();
        if (ol == null) ol = btn.gameObject.AddComponent<Outline>();
        ol.effectColor = outlineColor;
        ol.effectDistance = new Vector2(1.5f, -1.5f);

        var txt = btn.GetComponentInChildren<Text>();
        if (txt != null)
        {
            txt.text = labelText;
            txt.font = font;
            txt.fontSize = 17;
            txt.fontStyle = FontStyle.Bold;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.white;
        }
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

    private void OnLeaderboardClicked()
    {
        LeaderboardUI.ShowLeaderboard();
    }

    private void OnPlayClicked()
    {
        // Lưu tên người chơi trước khi bắt đầu
        if (playerNameInput != null && !string.IsNullOrWhiteSpace(playerNameInput.text))
        {
            LeaderboardManager.Instance.CurrentPlayerName = playerNameInput.text.Trim();
        }

        Debug.Log($"[MainMenuUI] Bắt đầu chơi với tên: {LeaderboardManager.Instance?.CurrentPlayerName}");
        LeaderboardManager.StartNewRun();

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
        if (leaderboardButton != null) leaderboardButton.onClick.RemoveAllListeners();
        if (settingsButton != null) settingsButton.onClick.RemoveAllListeners();
        if (exitButton != null) exitButton.onClick.RemoveAllListeners();
        if (closeSettingsButton != null) closeSettingsButton.onClick.RemoveAllListeners();
    }
}
