using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// GameAudioManager: Hệ thống quản lý toàn diện âm thanh sống động cho cả 3 màn chơi:
/// 1. Nhạc nền (BGM / Ambient Sci-Fi & Horror) tự động đồng bộ theo từng màn:
///    - Màn 1 (Lab): Âm hưởng viễn tưởng bí ẩn, drone phòng thí nghiệm căng thẳng.
///    - Màn 2 (Reactor): Tiếng máy móc công nghiệp nặng, nhịp tim/bass trầm dập dồn, áp lực khí độc.
///    - Màn 3 (Helipad): Tiếng gió tầng cao rít mạnh, âm hưởng cao trào sinh tồn.
/// 2. Hiệu ứng âm thanh (SFX) phong phú:
///    - Nhặt Item (Thẻ, Cờ lê, Cầu chì, Chìa khóa, Pin, Đĩa mềm...).
///    - Robot bắn Laser, Robot giật điện (-5 HP), Robot phát hiện cảnh báo.
///    - Người chơi bị thương (Player Hurt).
///    - Thao tác Cửa: Mở trượt cơ khí, Cửa khóa từ chối, Mở khóa thành công.
///    - Thao tác Cỗ Máy: Cầu dao điện, Máy phát điện nổ máy, Van khí độc xả & vặn, Terminal & Keypad click.
///    - Mở / Đóng Túi Đồ [TAB].
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    [Header("Volume Controls")]
    [Range(0f, 1f)] public float masterVolume = 1.0f;
    [Range(0f, 1f)] public float bgmVolume = 0.55f;
    [Range(0f, 1f)] public float sfxVolume = 0.90f;

    private AudioSource bgmSource;
    private AudioSource sfx2DSource;
    private Dictionary<string, AudioClip> clipCache = new Dictionary<string, AudioClip>();
    private string currentBgmId = "";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void AutoInit()
    {
        EnsureInstance();
    }

    public static GameAudioManager EnsureInstance()
    {
        if (Instance != null) return Instance;

        var existing = FindFirstObjectByType<GameAudioManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        var go = new GameObject("GameAudioManager_Auto");
        Instance = go.AddComponent<GameAudioManager>();
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

        SetupAudioSources();
        PreloadAllClips();
    }

    private void Start()
    {
        CheckAndPlaySceneBGM(SceneManager.GetActiveScene().name);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        CheckAndPlaySceneBGM(scene.name);
    }

    private void SetupAudioSources()
    {
        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.loop = true;
            bgmSource.playOnAwake = false;
            bgmSource.spatialBlend = 0f; // 2D Full Stereo
            bgmSource.volume = bgmVolume * masterVolume;
        }

        if (sfx2DSource == null)
        {
            sfx2DSource = gameObject.AddComponent<AudioSource>();
            sfx2DSource.loop = false;
            sfx2DSource.playOnAwake = false;
            sfx2DSource.spatialBlend = 0f;
            sfx2DSource.volume = sfxVolume * masterVolume;
        }
    }

    private void PreloadAllClips()
    {
        string[] allClipNames = new string[]
        {
            "bgm_lab", "bgm_reactor", "bgm_helipad",
            "sfx_pickup", "sfx_robot_laser", "sfx_robot_shock", "sfx_robot_alert", "sfx_player_hurt",
            "sfx_door_open", "sfx_door_locked", "sfx_door_unlock",
            "sfx_switch_lever", "sfx_generator_start", "sfx_valve_turn",
            "sfx_terminal_key", "sfx_terminal_success", "sfx_terminal_error",
            "sfx_inventory_open", "sfx_inventory_close", "sfx_mission_passed"
        };

        foreach (var cName in allClipNames)
        {
            var clip = Resources.Load<AudioClip>("Audio/" + cName);
            if (clip != null)
            {
                clipCache[cName] = clip;
            }
        }
    }

    public AudioClip GetClip(string clipId)
    {
        if (string.IsNullOrEmpty(clipId)) return null;
        if (clipCache.TryGetValue(clipId, out AudioClip clip)) return clip;

        clip = Resources.Load<AudioClip>("Audio/" + clipId);
        if (clip != null)
        {
            clipCache[clipId] = clip;
            return clip;
        }

        return null;
    }

    public void CheckAndPlaySceneBGM(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        string targetBgm = "";
        if (sceneName.Contains("Lab") || sceneName.Contains("Level1"))
        {
            targetBgm = "bgm_lab";
        }
        else if (sceneName.Contains("Reactor") || sceneName.Contains("Level2"))
        {
            targetBgm = "bgm_reactor";
        }
        else if (sceneName.Contains("Helipad") || sceneName.Contains("Level3") || sceneName.Contains("Rooftop"))
        {
            targetBgm = "bgm_helipad";
        }

        if (!string.IsNullOrEmpty(targetBgm) && targetBgm != currentBgmId)
        {
            PlayBGM(targetBgm);
        }
    }

    public void PlayBGM(string bgmId)
    {
        AudioClip clip = GetClip(bgmId);
        if (clip == null) return;

        currentBgmId = bgmId;
        if (bgmSource != null)
        {
            bgmSource.clip = clip;
            bgmSource.volume = bgmVolume * masterVolume;
            bgmSource.loop = true;
            bgmSource.Play();
        }
    }

    public void PlaySound2D(string clipId, float volMultiplier = 1f, float pitch = 1f)
    {
        AudioClip clip = GetClip(clipId);
        if (clip == null) return;

        if (sfx2DSource != null)
        {
            sfx2DSource.pitch = pitch;
            sfx2DSource.PlayOneShot(clip, volMultiplier * sfxVolume * masterVolume);
        }
    }

    public void PlaySound3D(string clipId, Vector3 position, float spatialBlend = 0.85f, float maxDistance = 25f, float volMultiplier = 1f, float pitch = 1f)
    {
        AudioClip clip = GetClip(clipId);
        if (clip == null) return;

        GameObject tempGO = new GameObject("TempAudio3D_" + clipId);
        tempGO.transform.position = position;

        AudioSource aSource = tempGO.AddComponent<AudioSource>();
        aSource.clip = clip;
        aSource.volume = volMultiplier * sfxVolume * masterVolume;
        aSource.pitch = pitch;
        aSource.spatialBlend = spatialBlend;
        aSource.minDistance = 2.0f;
        aSource.maxDistance = maxDistance;
        aSource.rolloffMode = AudioRolloffMode.Linear;
        aSource.Play();

        Destroy(tempGO, clip.length / Mathf.Max(0.1f, pitch) + 0.2f);
    }

    // ── STATIC CONVENIENCE METHODS ──────────────────────────────────────────

    /// <summary>Âm thanh nhặt Item (Thẻ, Cờ Lê, Cầu Chì, Chìa Khóa, Pin, Đĩa Mềm...)</summary>
    public static void PlayPickup()
    {
        EnsureInstance().PlaySound2D("sfx_pickup", 1.0f, Random.Range(0.96f, 1.04f));
    }

    /// <summary>Âm thanh Robot bắn tia Laser đỏ</summary>
    public static void PlayRobotLaser(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        float pitch = Random.Range(0.95f, 1.05f);
        if (pos.HasValue)
            inst.PlaySound3D("sfx_robot_laser", pos.Value, 0.75f, 35f, 1.0f, pitch);
        else
            inst.PlaySound2D("sfx_robot_laser", 0.9f, pitch);
    }

    /// <summary>Âm thanh Robot phóng tia sét giật điện (-5 HP)</summary>
    public static void PlayRobotShock(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        float pitch = Random.Range(0.92f, 1.08f);
        if (pos.HasValue)
            inst.PlaySound3D("sfx_robot_shock", pos.Value, 0.8f, 25f, 1.1f, pitch);
        else
            inst.PlaySound2D("sfx_robot_shock", 1.0f, pitch);
    }

    /// <summary>Âm thanh Robot phát hiện / Gầm cảnh báo</summary>
    public static void PlayRobotAlert(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_robot_alert", pos.Value, 0.8f, 40f, 1.0f, 1.0f);
        else
            inst.PlaySound2D("sfx_robot_alert", 0.9f, 1.0f);
    }

    /// <summary>Âm thanh người chơi trúng đòn đau (Player Hurt)</summary>
    public static void PlayPlayerHurt()
    {
        EnsureInstance().PlaySound2D("sfx_player_hurt", 1.1f, Random.Range(0.94f, 1.06f));
    }

    /// <summary>Âm thanh Cửa mở trượt thủy lực / cơ khí</summary>
    public static void PlayDoorOpen(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_door_open", pos.Value, 0.85f, 30f, 1.0f, 1.0f);
        else
            inst.PlaySound2D("sfx_door_open", 1.0f, 1.0f);
    }

    /// <summary>Âm thanh Cửa bị khóa (Từ chối truy cập)</summary>
    public static void PlayDoorLocked(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_door_locked", pos.Value, 0.85f, 20f, 0.9f, 1.0f);
        else
            inst.PlaySound2D("sfx_door_locked", 0.9f, 1.0f);
    }

    /// <summary>Âm thanh Mở khóa Cửa thành công (Chime điện tử)</summary>
    public static void PlayDoorUnlock(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_door_unlock", pos.Value, 0.8f, 25f, 1.0f, 1.0f);
        else
            inst.PlaySound2D("sfx_door_unlock", 1.0f, 1.0f);
    }

    /// <summary>Âm thanh Cầu Dao Điện (Sập cần gạt kim loại + nẹt lửa điện)</summary>
    public static void PlaySwitchLever(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_switch_lever", pos.Value, 0.8f, 25f, 1.0f, 1.0f);
        else
            inst.PlaySound2D("sfx_switch_lever", 1.0f, 1.0f);
    }

    /// <summary>Âm thanh Máy Phát Điện khởi động tuabin nổ máy</summary>
    public static void PlayGeneratorStart(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_generator_start", pos.Value, 0.85f, 35f, 1.1f, 1.0f);
        else
            inst.PlaySound2D("sfx_generator_start", 1.0f, 1.0f);
    }

    /// <summary>Âm thanh Van xả khí độc (Két kim loại, rít xả khí và khóa van)</summary>
    public static void PlayValveTurn(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_valve_turn", pos.Value, 0.8f, 25f, 1.0f, 1.0f);
        else
            inst.PlaySound2D("sfx_valve_turn", 1.0f, 1.0f);
    }

    /// <summary>Âm thanh Gõ phím số trên Terminal / Bàn phím Thang Máy</summary>
    public static void PlayTerminalKey(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        float pitch = Random.Range(0.96f, 1.06f);
        if (pos.HasValue)
            inst.PlaySound3D("sfx_terminal_key", pos.Value, 0.7f, 15f, 0.8f, pitch);
        else
            inst.PlaySound2D("sfx_terminal_key", 0.75f, pitch);
    }

    /// <summary>Âm thanh Giải mã Terminal thành công / Khớp mật khẩu</summary>
    public static void PlayTerminalSuccess(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_terminal_success", pos.Value, 0.7f, 20f, 1.0f, 1.0f);
        else
            inst.PlaySound2D("sfx_terminal_success", 1.0f, 1.0f);
    }

    /// <summary>Âm thanh Nhập sai mã Terminal / Bàn phím</summary>
    public static void PlayTerminalError(Vector3? pos = null)
    {
        var inst = EnsureInstance();
        if (pos.HasValue)
            inst.PlaySound3D("sfx_terminal_error", pos.Value, 0.7f, 20f, 0.9f, 1.0f);
        else
            inst.PlaySound2D("sfx_terminal_error", 0.9f, 1.0f);
    }

    /// <summary>Âm thanh Mở / Đóng Túi Đồ [TAB]</summary>
    public static void PlayInventoryToggle(bool open)
    {
        EnsureInstance().PlaySound2D(open ? "sfx_inventory_open" : "sfx_inventory_close", 0.85f, 1.0f);
    }

    /// <summary>Âm thanh Chiến thắng MISSION PASSED</summary>
    public static void PlayMissionPassed()
    {
        EnsureInstance().PlaySound2D("sfx_mission_passed", 1.0f, 1.0f);
    }
}
