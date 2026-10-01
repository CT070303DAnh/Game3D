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
    // Scene names
    // ───────────────────────────────────────────────
    [Header("Scene Names")]
    [SerializeField] private string mainMenuScene = "MainMenu";
    [SerializeField] private string gameplayScene = "Lab";

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
        // Neu bat dau Play truc tiep trong gameplay scene (khong qua MainMenu),
        // tu dong set phase sang Playing de PlayerController hoat dong ngay.
        string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        if (CurrentPhase == GamePhase.MainMenu && currentScene == gameplayScene)
        {
            CurrentPhase = GamePhase.Playing;
            Time.timeScale = 1f;
            OnGameStart?.Invoke();
            Debug.Log("[GameManager] Auto-started: detected direct play in gameplay scene.");
        }
    }

    // ───────────────────────────────────────────────
    // Public API
    // ───────────────────────────────────────────────

    public void StartGame()
    {
        CurrentPhase = GamePhase.Playing;
        Time.timeScale = 1f;
        SceneManager.LoadScene(gameplayScene);
        OnGameStart?.Invoke();
        Debug.Log("[GameManager] Game started.");
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
        SceneManager.LoadScene(gameplayScene);
        Debug.Log("[GameManager] Restarting game.");
    }

    public void QuitGame()
    {
        Debug.Log("[GameManager] Quitting application.");
        Application.Quit();
    }

    public bool IsPlaying => CurrentPhase == GamePhase.Playing;
}
