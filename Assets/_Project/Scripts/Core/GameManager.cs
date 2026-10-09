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

    public static readonly string[] RequiredLevelScenes = new string[] { "Lab", "Level2_Reactor", "Level3_Helipad" };

    public void EnsureLevelScenesValid()
    {
        if (levelScenes == null || levelScenes.Length < 3 || string.IsNullOrEmpty(levelScenes[0]))
        {
            levelScenes = (string[])RequiredLevelScenes.Clone();
        }
        else
        {
            bool hasHelipad = false;
            for (int i = 0; i < levelScenes.Length; i++)
            {
                if (levelScenes[i] == "Level3_Helipad") { hasHelipad = true; break; }
            }
            if (!hasHelipad)
            {
                var list = new System.Collections.Generic.List<string>(levelScenes);
                list.Add("Level3_Helipad");
                levelScenes = list.ToArray();
            }
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

        EnsureLevelScenesValid();
    }

    private void Start()
    {
        EnsureLevelScenesValid();

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
        EnsureLevelScenesValid();
        currentLevelIndex = 0;
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        string startScene = levelScenes.Length > 0 ? levelScenes[0] : gameplayScene;

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(startScene, onComplete: () => {
                OnGameStart?.Invoke();
            });
        }
        else
        {
            SceneManager.LoadScene(startScene);
            OnGameStart?.Invoke();
        }
        Debug.Log("[GameManager] Game started at Level 1.");
    }

    public void LoadLevelByName(string targetScene)
    {
        EnsureLevelScenesValid();
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] == targetScene)
            {
                currentLevelIndex = i;
                break;
            }
        }

        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        Debug.Log($"[GameManager] Loading Level by name: {targetScene} (Index {currentLevelIndex + 1})");

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetScene, onComplete: () => {
                OnGameStart?.Invoke();
            });
        }
        else
        {
            SceneManager.LoadScene(targetScene);
            OnGameStart?.Invoke();
        }
    }

    public void LoadNextLevel()
    {
        EnsureLevelScenesValid();

        // Đồng bộ lại currentLevelIndex dựa trên scene thực tế đang chạy
        string activeScene = SceneManager.GetActiveScene().name;
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] == activeScene)
            {
                currentLevelIndex = i;
                break;
            }
        }

        int nextIndex = currentLevelIndex + 1;
        if (nextIndex < levelScenes.Length)
        {
            currentLevelIndex = nextIndex;
            CurrentPhase = GamePhase.Playing;
            Time.timeScale = 1f;
            string targetScene = levelScenes[currentLevelIndex];
            Debug.Log($"[GameManager] Loading Level {currentLevelIndex + 1}: {targetScene}");

            if (SceneTransitionManager.Instance != null)
            {
                SceneTransitionManager.Instance.TransitionToScene(targetScene, onComplete: () => {
                    OnGameStart?.Invoke();
                });
            }
            else
            {
                SceneManager.LoadScene(targetScene);
                OnGameStart?.Invoke();
            }
        }
        else
        {
            // Da hoan thanh tat ca cac man -> Chien thang toan dien!
            TriggerWin();
        }
    }

    public void LoadLevel(int levelIdx)
    {
        EnsureLevelScenesValid();
        currentLevelIndex = Mathf.Clamp(levelIdx, 0, levelScenes.Length - 1);
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        string targetScene = levelScenes[currentLevelIndex];

        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(targetScene, onComplete: () => {
                OnGameStart?.Invoke();
            });
        }
        else
        {
            SceneManager.LoadScene(targetScene);
            OnGameStart?.Invoke();
        }
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

        // Chơi lại ĐÚNG màn vừa thua: luôn dùng scene đang mở
        string sceneToReload = SceneManager.GetActiveScene().name;
        for (int i = 0; i < levelScenes.Length; i++)
        {
            if (levelScenes[i] == sceneToReload) { currentLevelIndex = i; break; }
        }

        Debug.Log($"[GameManager] Restarting level scene: {sceneToReload}.");
        if (SceneTransitionManager.Instance != null)
        {
            SceneTransitionManager.Instance.TransitionToScene(sceneToReload, isRestart: true, onComplete: () => {
                OnGameStart?.Invoke();
            });
        }
        else
        {
            SceneManager.LoadScene(sceneToReload);
            OnGameStart?.Invoke();
        }
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting application.");
        Application.Quit();
    }

    public bool IsPlaying => CurrentPhase == GamePhase.Playing;
}
