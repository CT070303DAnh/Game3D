using UnityEngine;

/// <summary>
/// GameState: Luu tru trang thai tien trinh game (items da nhat, puzzle da giai, v.v.)
/// Day la nguon su that duy nhat cho progression logic.
/// Reset khi bat dau game moi.
/// </summary>
public class GameState : MonoBehaviour
{
    // ───────────────────────────────────────────────
    // Singleton
    // ───────────────────────────────────────────────
    // (Instance da duoc chuyen xuong duoi de Auto-Spawn)

    // ───────────────────────────────────────────────
    // Item Collection State
    // ───────────────────────────────────────────────
    [Header("Item State (Read-Only in Runtime)")]
    [SerializeField] private bool securityCardCollected;
    [SerializeField] private bool fuseCollected;
    [SerializeField] private bool batteryCollected;
    [SerializeField] private bool laboratoryKeyCollected;
    [SerializeField] private bool accessCodeFound;
    [SerializeField] private bool jetFuelCollected;

    // ───────────────────────────────────────────────
    // Puzzle / World State
    // ───────────────────────────────────────────────
    [Header("World State")]
    [SerializeField] private bool powerRestored;
    [SerializeField] private bool accessCodeSolved;
    [SerializeField] private bool exitUnlocked;
    [SerializeField] private bool radarActivated;
    [SerializeField] private bool domeGateOpened;
    [SerializeField] private bool helipadEscaped;

    // ───────────────────────────────────────────────
    // Events — phat khi state thay doi
    // ───────────────────────────────────────────────
    public static event System.Action OnSecurityCardCollected;
    public static event System.Action OnFuseCollected;
    public static event System.Action OnPowerRestored;
    public static event System.Action OnAccessCodeSolved;
    public static event System.Action OnExitUnlocked;
    public static event System.Action OnJetFuelCollected;
    public static event System.Action OnRadarActivated;
    public static event System.Action OnDomeGateOpened;
    public static event System.Action OnHelipadEscaped;

    // ───────────────────────────────────────────────
    // Properties (read-only tu ngoai)
    // ───────────────────────────────────────────────
    public bool SecurityCardCollected => securityCardCollected;
    public bool FuseCollected => fuseCollected;
    public bool BatteryCollected => batteryCollected;
    public bool LaboratoryKeyCollected => laboratoryKeyCollected;
    public bool AccessCodeFound => accessCodeFound;
    public bool JetFuelCollected => jetFuelCollected;
    public bool PowerRestored => powerRestored;
    public bool AccessCodeSolved => accessCodeSolved;
    public bool ExitUnlocked => exitUnlocked;
    public bool RadarActivated => radarActivated;
    public bool DomeGateOpened => domeGateOpened;
    public bool HelipadEscaped => helipadEscaped;

    private static GameState _instance;
    public static GameState Instance 
    { 
        get 
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GameState>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GameState_AutoSpawned");
                    _instance = go.AddComponent<GameState>();
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
        ResetState();
    }

    private void OnEnable()
    {
        GameManager.OnGameStart += ResetState;
    }

    private void OnDisable()
    {
        GameManager.OnGameStart -= ResetState;
    }

    // ───────────────────────────────────────────────
    // Reset
    // ───────────────────────────────────────────────
    private void ResetState()
    {
        securityCardCollected = false;
        fuseCollected = false;
        batteryCollected = false;
        laboratoryKeyCollected = false;
        accessCodeFound = false;
        powerRestored = false;
        accessCodeSolved = false;
        exitUnlocked = false;
        jetFuelCollected = false;
        radarActivated = false;
        domeGateOpened = false;
        helipadEscaped = false;
        Debug.Log("[GameState] State reset.");
    }

    // ───────────────────────────────────────────────
    // Public Setters — goi tu cac he thong khac
    // ───────────────────────────────────────────────

    public void CollectSecurityCard()
    {
        if (securityCardCollected) return;
        securityCardCollected = true;
        Debug.Log("[GameState] Security Card collected.");
        OnSecurityCardCollected?.Invoke();
        CheckExitCondition();
    }

    public void CollectFuse()
    {
        if (fuseCollected) return;
        fuseCollected = true;
        Debug.Log("[GameState] Fuse collected.");
        OnFuseCollected?.Invoke();
    }

    public void CollectBattery()
    {
        if (batteryCollected) return;
        batteryCollected = true;
        Debug.Log("[GameState] Battery collected.");
    }

    public void CollectLaboratoryKey()
    {
        if (laboratoryKeyCollected) return;
        laboratoryKeyCollected = true;
        Debug.Log("[GameState] Laboratory Key collected.");
    }

    public void SetAccessCodeFound()
    {
        if (accessCodeFound) return;
        accessCodeFound = true;
        Debug.Log("[GameState] Access Code found.");
    }

    public void SetPowerRestored()
    {
        if (powerRestored) return;
        powerRestored = true;
        Debug.Log("[GameState] Power restored!");
        OnPowerRestored?.Invoke();
    }

    public void RestorePower() => SetPowerRestored();

    public void SetAccessCodeSolved()
    {
        if (accessCodeSolved) return;
        accessCodeSolved = true;
        Debug.Log("[GameState] Access Code solved!");
        OnAccessCodeSolved?.Invoke();
        CheckExitCondition();
    }

    public void CollectJetFuel()
    {
        if (jetFuelCollected) return;
        jetFuelCollected = true;
        Debug.Log("[GameState] Jet Fuel collected.");
        OnJetFuelCollected?.Invoke();
    }

    public void SetRadarActivated()
    {
        if (radarActivated) return;
        radarActivated = true;
        Debug.Log("[GameState] Radar activated.");
        OnRadarActivated?.Invoke();
    }

    public void SetDomeGateOpened()
    {
        if (domeGateOpened) return;
        domeGateOpened = true;
        Debug.Log("[GameState] Dome Gate opened.");
        OnDomeGateOpened?.Invoke();
    }

    public void SetHelipadEscaped()
    {
        if (helipadEscaped) return;
        helipadEscaped = true;
        Debug.Log("[GameState] Helipad Escaped! Victory!");
        OnHelipadEscaped?.Invoke();
    }

    // ───────────────────────────────────────────────
    // Exit Condition Logic
    // SecurityCard + Fuse + PowerRestored + AccessCodeSolved = Unlock Exit
    // ───────────────────────────────────────────────
    private void CheckExitCondition()
    {
        if (exitUnlocked) return;
        if (securityCardCollected && powerRestored && accessCodeSolved)
        {
            exitUnlocked = true;
            Debug.Log("[GameState] EXIT UNLOCKED!");
            OnExitUnlocked?.Invoke();
        }
    }
}
