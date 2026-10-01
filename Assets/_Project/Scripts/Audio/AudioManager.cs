using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// AudioManager: Quan ly tat ca am thanh tap trung.
/// Ho tro: BGM, SFX (non-spatial), Ambient.
/// Static API: AudioManager.PlaySFX("pickup"), AudioManager.PlayBGM("main_theme")
/// Attach vao: AudioManager GameObject (DontDestroyOnLoad)
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class SoundEntry
    {
        public string id;
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
        [Range(0.8f, 1.2f)] public float pitch = 1f;
        public bool loop = false;
    }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioSource ambientSource;

    [Header("Sound Library")]
    [SerializeField] private SoundEntry[] sounds;

    [Header("Volume")]
    [SerializeField, Range(0,1)] private float masterVolume = 1f;
    [SerializeField, Range(0,1)] private float bgmVolume = 0.6f;
    [SerializeField, Range(0,1)] private float sfxVolume = 1f;

    private Dictionary<string, SoundEntry> soundDict = new Dictionary<string, SoundEntry>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        // Build lookup dict
        foreach (var s in sounds)
            if (!string.IsNullOrEmpty(s.id))
                soundDict[s.id] = s;

        ApplyVolumes();
    }

    // ── Public Static API ──────────────────────────

    public static void PlaySFX(string id)
    {
        if (Instance == null) return;
        if (!Instance.soundDict.TryGetValue(id, out SoundEntry s)) return;
        if (s.clip == null) return;
        Instance.sfxSource.pitch = s.pitch + Random.Range(-0.05f, 0.05f);
        Instance.sfxSource.PlayOneShot(s.clip, s.volume * Instance.sfxVolume * Instance.masterVolume);
    }

    public static void PlayBGM(string id)
    {
        if (Instance == null) return;
        if (!Instance.soundDict.TryGetValue(id, out SoundEntry s)) return;
        if (s.clip == null) return;
        Instance.bgmSource.clip = s.clip;
        Instance.bgmSource.loop = true;
        Instance.bgmSource.volume = s.volume * Instance.bgmVolume * Instance.masterVolume;
        Instance.bgmSource.Play();
    }

    public static void PlayAmbient(string id)
    {
        if (Instance == null) return;
        if (!Instance.soundDict.TryGetValue(id, out SoundEntry s)) return;
        if (s.clip == null) return;
        Instance.ambientSource.clip = s.clip;
        Instance.ambientSource.loop = true;
        Instance.ambientSource.volume = s.volume * Instance.masterVolume;
        Instance.ambientSource.Play();
    }

    public static void StopBGM() => Instance?.bgmSource.Stop();
    public static void StopAmbient() => Instance?.ambientSource.Stop();

    public static void SetMasterVolume(float v) { if (Instance != null) { Instance.masterVolume = v; Instance.ApplyVolumes(); } }
    public static void SetBGMVolume(float v) { if (Instance != null) { Instance.bgmVolume = v; Instance.bgmSource.volume = v * Instance.masterVolume; } }
    public static void SetSFXVolume(float v) { if (Instance != null) Instance.sfxVolume = v; }

    private void ApplyVolumes()
    {
        if (bgmSource != null) bgmSource.volume = bgmVolume * masterVolume;
        if (ambientSource != null) ambientSource.volume = masterVolume;
    }
}
