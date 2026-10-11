using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// LeaderboardManager: Quản lý Bảng Xếp Hạng (Ranking) và Bộ đếm thời gian hoàn thành thử thách.
/// - Lưu tên người chơi hiện tại qua PlayerPrefs.
/// - Tự động tính thời gian từ lúc bắt đầu Màn 1 đến khi hoàn thành Màn 3 thoát hiểm.
/// - Chỉ khi người chơi CHIẾN THẮNG (MISSION PASSED) mới được ghi danh trên Bảng Xếp Hạng.
/// - Tự động sắp xếp thành tích theo thời gian hoàn thành ngắn nhất.
/// </summary>
public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

    private const string PREF_PLAYER_NAME = "EscapeTheLab_PlayerName";
    private const string PREF_LEADERBOARD = "EscapeTheLab_Leaderboard_JSON";

    [System.Serializable]
    public class RankRecord
    {
        public string playerName;
        public float timeSeconds;
        public string formattedTime;
        public string dateString;
    }

    [System.Serializable]
    public class LeaderboardContainer
    {
        public List<RankRecord> records = new List<RankRecord>();
    }

    private float currentRunTime = 0f;
    private bool isTimerRunning = false;
    private LeaderboardContainer cachedData;

    public float CurrentRunTime => currentRunTime;
    public bool IsTimerRunning => isTimerRunning;

    public string CurrentPlayerName
    {
        get => PlayerPrefs.GetString(PREF_PLAYER_NAME, "Chiến Binh");
        set
        {
            string cleanName = string.IsNullOrWhiteSpace(value) ? "Chiến Binh" : value.Trim();
            PlayerPrefs.SetString(PREF_PLAYER_NAME, cleanName);
            PlayerPrefs.Save();
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        EnsureInstance();
    }

    public static LeaderboardManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        var found = FindFirstObjectByType<LeaderboardManager>();
        if (found != null)
        {
            Instance = found;
            return Instance;
        }

        var go = new GameObject("LeaderboardManager_Auto");
        Instance = go.AddComponent<LeaderboardManager>();
        DontDestroyOnLoad(go);
        return Instance;
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadData();
    }

    private void Update()
    {
        if (isTimerRunning && Time.timeScale > 0)
        {
            currentRunTime += Time.unscaledDeltaTime;
        }
    }

    /// <summary>Bắt đầu lượt chơi mới từ Màn 1 -> Reset bộ đếm thời gian</summary>
    public static void StartNewRun()
    {
        var mgr = EnsureInstance();
        mgr.currentRunTime = 0f;
        mgr.isTimerRunning = true;
        Debug.Log("[LeaderboardManager] Bắt đầu lượt chơi mới. Timer kích hoạt!");
    }

    /// <summary>Đảm bảo timer đang chạy nếu người chơi đang trong màn</summary>
    public static void EnsureTimerRunning()
    {
        var mgr = EnsureInstance();
        if (!mgr.isTimerRunning)
        {
            mgr.isTimerRunning = true;
            Debug.Log("[LeaderboardManager] Timer tiếp tục đếm: " + FormatTime(mgr.currentRunTime));
        }
    }

    public static void PauseTimer()
    {
        if (Instance != null) Instance.isTimerRunning = false;
    }

    public static void ResumeTimer()
    {
        if (Instance != null) Instance.isTimerRunning = true;
    }

    public static void StopTimer()
    {
        if (Instance != null) Instance.isTimerRunning = false;
    }

    /// <summary>
    /// Ghi nhận thành tích khi người chơi THẮNG GAME (Mission Passed).
    /// Trả về thứ hạng đạt được (1-indexed: #1, #2, #3...).
    /// </summary>
    public static int RecordVictory(string customName = null)
    {
        var mgr = EnsureInstance();
        mgr.isTimerRunning = false;

        string pName = !string.IsNullOrWhiteSpace(customName) ? customName.Trim() : mgr.CurrentPlayerName;
        float elapsed = mgr.currentRunTime;
        if (elapsed <= 0.05f) elapsed = 1.0f; // Phòng trường hợp test nhanh

        string formatted = FormatTime(elapsed);
        string dateNow = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

        var record = new RankRecord
        {
            playerName = pName,
            timeSeconds = elapsed,
            formattedTime = formatted,
            dateString = dateNow
        };

        mgr.cachedData.records.Add(record);

        // Sắp xếp tăng dần theo thời gian (nhanh nhất đứng đầu)
        mgr.cachedData.records.Sort((a, b) => a.timeSeconds.CompareTo(b.timeSeconds));

        // Giữ lại tối đa 30 kỷ lục tốt nhất
        if (mgr.cachedData.records.Count > 30)
        {
            mgr.cachedData.records.RemoveRange(30, mgr.cachedData.records.Count - 30);
        }

        mgr.SaveData();

        // Tìm thứ hạng vừa đạt được
        int rankIndex = mgr.cachedData.records.IndexOf(record);
        int finalRank = (rankIndex >= 0) ? rankIndex + 1 : 1;

        Debug.Log($"[LeaderboardManager] ĐÃ GHI DANH KỶ LỤC: {pName} - {formatted} (Hạng #{finalRank})");
        return finalRank;
    }

    public static List<RankRecord> GetRecords()
    {
        var mgr = EnsureInstance();
        return mgr.cachedData.records;
    }

    public static void ClearAllRecords()
    {
        var mgr = EnsureInstance();
        mgr.cachedData.records.Clear();
        mgr.SaveData();
        Debug.Log("[LeaderboardManager] Đã làm trống Bảng Xếp Hạng.");
    }

    public static string FormatTime(float timeInSeconds)
    {
        if (timeInSeconds < 0f) timeInSeconds = 0f;
        int minutes = Mathf.FloorToInt(timeInSeconds / 60f);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60f);
        int milliseconds = Mathf.FloorToInt((timeInSeconds * 100f) % 100f);
        return string.Format("{0:00}:{1:00}.{2:00}", minutes, seconds, milliseconds);
    }

    private void LoadData()
    {
        string json = PlayerPrefs.GetString(PREF_LEADERBOARD, "");
        if (!string.IsNullOrEmpty(json))
        {
            try
            {
                cachedData = JsonUtility.FromJson<LeaderboardContainer>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[LeaderboardManager] Lỗi đọc JSON Rank: " + ex);
            }
        }

        if (cachedData == null)
        {
            cachedData = new LeaderboardContainer();
        }
    }

    private void SaveData()
    {
        if (cachedData == null) return;
        string json = JsonUtility.ToJson(cachedData);
        PlayerPrefs.SetString(PREF_LEADERBOARD, json);
        PlayerPrefs.Save();
    }
}
