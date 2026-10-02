using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager: Singleton quan ly vong doi game, chuyen scene va trang thai win/lose.
/// Khong chua gameplay logic — chi dieu phoi cap cao.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ───────────────────────────────────────────────
    // Singleton
    // ───────────────────────────────────────────────
    // (Instance da duoc chuyen xuong duoi de Auto-Spawn)

    // ───────────────────────────────────────────────
    // Scene names & Level Progression
    // ───────────────────────────────────────────────
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameplayScene = "Lab";
    [SerializeField] private string[] levelScenes = new string[] { "Lab", "Level2_Reactor", "Level3_Helipad" };
    [SerializeField] private int currentLevelIndex = 0;

    public int CurrentLevelIndex => currentLevelIndex;
    public string CurrentLevelScene => (currentLevelIndex >= 0 && currentLevelIndex < levelScenes.Length) ? levelScenes[currentLevelIndex] : gameplayScene;

    // ───────────────────────────────────────────────
    // Game State
    // ───────────────────────────────────────────────
    public enum GamePhase { MainMenu, Playing, Paused, Win, GameOver }
    public GamePhase CurrentPhase { get; private set; } = GamePhase.MainMenu;

    // ───────────────────────────────────────────────
    // Events — cac he thong khac lang nghe
    // ───────────────────────────────────────────────
    public static event System.Action OnGameStart;
    public static event System.Action OnGameWin;
    public static event System.Action OnGameOver;
    public static event System.Action OnGamePaused;
    public static event System.Action OnGameResumed;

    private static GameManager _instance;
    public static GameManager Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameManager_AutoSpawned");
                    _instance = go.AddComponent<GameManager>();
                }
            }
            return _instance;
        }
    }

    // ───────────────────────────────────────────────
    // Unity Lifecycle
    // ───────────────────────────────────────────────
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Phat hien scene hien tai neu bat dau Play truc tiep
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] == currentScene)
            {
                currentLevelIndex = i;
                break;
            }
        }

        if (CurrentPhase == GamePhase.MainMenu && currentScene != mainMenuScene)
        {
            CurrentPhase = GamePhase.Playing;
            Time.timeScale = 1f;
            OnGameStart?.Invoke();
            Debug.Log($"[GameManager] Auto-started in scene: {currentScene} (Level {currentLevelIndex + 1})");
        }
    }

    // ───────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────

    public void StartGame()
    {
        currentLevelIndex = 0;
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelScenes.Length > 0 ? levelScenes[0] : gameplayScene);
        OnGameStart?.Invoke();
        Debug.Log("[GameManager] Game started at Level 1.");
    }

    public void LoadNextLevel()
    {
        currentLevelIndex++;
        if (currentLevelIndex < levelScenes.Length)
        {
            CurrentPhase = GamePhase.Playing;
            Time.timeScale = 1f;
            Debug.Log($"[GameManager] Loading Level {currentLevelIndex + 1}: {levelScenes[currentLevelIndex]}");
            SceneManager.LoadScene(levelScenes[currentLevelIndex]);
            OnGameStart?.Invoke();
        }
        else
        {
            // Da hoan thanh tat ca cac man -> Chien thang toan dien!
            TriggerWin();
        }
    }

    public void LoadLevel(int levelIdx)
    {
        currentLevelIndex = Mathf.Clamp(levelIdx, 0, levelScenes.Length - 1);
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene(levelScenes[currentLevelIndex]);
        OnGameStart?.Invoke();
    }

    public void TriggerWin()
    {
        if (CurrentPhase == GamePhase.Win) return;
        CurrentPhase = GamePhase.Win;
        Debug.Log("[GameManager] Player escaped! WIN.");
        OnGameWin?.Invoke();
    }

    public void TriggerGameOver()
    {
        if (CurrentPhase == GamePhase.GameOver) return;
        CurrentPhase = GamePhase.GameOver;
        Debug.Log("[GameManager] Game over.");
        OnGameOver?.Invoke();
    }

    public void PauseGame()
    {
        if (CurrentPhase != GamePhase.Playing) return;
        CurrentPhase = GamePhase.Paused;
        Time.timeScale = 0f;
        OnGamePaused?.Invoke();
        Debug.Log("[GameManager] Game paused.");
    }

    public void ResumeGame()
    {
        if (CurrentPhase != GamePhase.Paused) return;
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        OnGameResumed?.Invoke();
        Debug.Log("[GameManager] Game resumed.");
    }

    public void LoadMainMenu()
    {
        CurrentPhase = GamePhase.MainMenu;
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuScene);
        Debug.Log("[GameManager] Returning to Main Menu.");
    }

    public void RestartGame()
    {
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        string sceneToReload = (currentLevelIndex >= 0 && currentLevelIndex < levelScenes.Length)
            ? levelScenes[currentLevelIndex]
            : SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(sceneToReload);
        Debug.Log($"[GameManager] Restarting level scene: {sceneToReload}.");
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting application.");
        Application.Quit();
    }

    public bool IsPlaying => CurrentPhase == GamePhase.Playing;
}
