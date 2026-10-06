using UnityEngine;
using System.Collections;

/// <summary>
/// Generator: May phat dien. Require Fuse truoc khi bat.
/// Khi ON: phat OnPowerRestored event -> bat den, bat terminal, kich hoat robot.
/// Attach vao: Generator GameObject (Interactable layer)
/// </summary>
public class Generator : MonoBehaviour, IInteractable
{
    public enum GeneratorState { Off, InsertingFuse, Starting, On }

    [Header("Generator")]
    [SerializeField] private float startupDuration = 2.5f;

    [Header("Lights to enable on power")]
    [SerializeField] private Light[] roomLights;
    [SerializeField] private Color roomLightColor = new Color(0.4f, 0.8f, 1f);

    [Header("VFX")]
    [SerializeField] private ParticleSystem electricSparks;
    [SerializeField] private Light generatorGlow;

    [Header("Audio")]
    [SerializeField] private AudioClip insertFuseSound;
    [SerializeField] private AudioClip startupSound;
    [SerializeField] private AudioClip humLoop;

    private AudioSource audioSource;
    private GeneratorState state = GeneratorState.Off;

    public GeneratorState State => state;
    public bool CanInteract => state == GeneratorState.Off;
    public string InteractPromptText
    {
        get
        {
            if (state == GeneratorState.On) return "Máy Phát Điện [ĐÃ KHÔI PHỤC ĐIỆN]";
            if (GameState.Instance != null && !GameState.Instance.FuseCollected)
                return "Máy Phát Điện (Cần Cầu Chì - Fuse) [E]";
            return "Lắp Cầu Chì & Khởi Động Máy Phát Điện [E]";
        }
    }

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
    }

    private void Start()
    {
        // Tat het den khi chua co dien
        SetRoomLights(false);
        if (generatorGlow != null) generatorGlow.enabled = false;
    }

    public void Interact()
    {
        if (state != GeneratorState.Off) return;

        if (GameState.Instance == null || !GameState.Instance.FuseCollected)
        {
            NotificationUI.ShowMessage("CẢNH BÁO: Máy phát điện bị thiếu CẦU CHÌ! Hãy tìm Cầu Chì trong phòng kho để lắp vào.");
            return;
        }

        StartCoroutine(StartupSequence());
    }

    private IEnumerator StartupSequence()
    {
        // Phase 1: Insert fuse
        state = GeneratorState.InsertingFuse;
        NotificationUI.ShowMessage("Đang lắp Cầu Chì vào máy phát điện...");
        PlaySound(insertFuseSound);
        if (electricSparks != null) electricSparks.Play();
        yield return new WaitForSeconds(1f);

        // Phase 2: Starting
        state = GeneratorState.Starting;
        NotificationUI.ShowMessage("Máy phát điện đang nổ máy...");
        PlaySound(startupSound);
        yield return new WaitForSeconds(startupDuration);

        // Phase 3: ON
        state = GeneratorState.On;
        OnGeneratorOn();
    }

    private void OnGeneratorOn()
    {
        // Bat den trong phong
        SetRoomLights(true);

        // Glow tren generator
        if (generatorGlow != null) { generatorGlow.enabled = true; generatorGlow.color = new Color(0.2f, 1f, 0.4f); }

        // Am thanh hum
        if (humLoop != null && audioSource != null)
        {
            audioSource.clip = humLoop;
            audioSource.loop = true;
            audioSource.Play();
        }

        // Phat event power restored
        if (GameState.Instance != null)
            GameState.Instance.SetPowerRestored();

        NotificationUI.ShowMessage("⚡ ĐÃ LẮP CẦU CHÌ & BẬT ĐIỆN! CẢNH BÁO: ROBOT AN NINH ĐÃ THỨC TỈNH!");
        Debug.Log("[Generator] Power restored!");
    }

    private void SetRoomLights(bool on)
    {
        foreach (Light l in roomLights)
        {
            if (l == null) continue;
            l.enabled = on;
            if (on) l.color = roomLightColor;
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
            audioSource.PlayOneShot(clip);
    }
}
