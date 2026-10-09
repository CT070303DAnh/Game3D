using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// SceneTransitionManager: Hệ thống chuyển cảnh phong cách Sci-Fi Glitch / Nhiễu sóng điện tử.
/// - CRT Scanlines & Screen Tear Slices (nhiễu sọc màu Cyan, Hồng Neon, Trắng).
/// - Giao diện Terminal Cyber HUD giải mã dữ liệu khu vực (Decryption Sequence).
/// - Âm thanh Sci-Fi glitch, tiếng rè điện từ và tiếng bass trầm tương lai.
/// - Tải ngầm Scene mượt mà (Async Loading) với thanh tiến trình số hóa.
/// </summary>
public class SceneTransitionManager : MonoBehaviour
{
    private static SceneTransitionManager _instance;
    public static SceneTransitionManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<SceneTransitionManager>(FindObjectsInactive.Include);
                if (_instance == null)
                {
                    GameObject go = new GameObject("SceneTransitionManager_Auto");
                    _instance = go.AddComponent<SceneTransitionManager>();
                }
            }
            return _instance;
        }
    }

    [Header("Audio Settings")]
    [SerializeField] private AudioClip glitchSound;
    [SerializeField] private AudioClip warpCompleteSound;

    // UI Elements
    private Canvas transitionCanvas;
    private CanvasGroup canvasGroup;
    private RawImage scanlineImage;
    private RectTransform glitchBarsContainer;
    private RectTransform[] glitchBars;
    private Text terminalLogText;
    private Text sectorTitleText;
    private Text sectorSubtitleText;
    private Text progressText;
    private Image progressBarFill;
    private AudioSource audioSource;

    private static Texture2D scanlineTexture;
    private static AudioClip proceduralGlitchAudio;
    private static AudioClip proceduralWarpAudio;

    private bool isTransitioning = false;
    public bool IsTransitioning => isTransitioning;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        EnsureUIElements();
    }

    /// <summary>
    /// Chuyển cảnh sang scene mới với hiệu ứng Sci-Fi Glitch.
    /// </summary>
    public void TransitionToScene(string sceneName, string title = null, string subtitle = null, bool isRestart = false, System.Action onComplete = null)
    {
        if (isTransitioning) return;
        EnsureUIElements();
        StartCoroutine(GlitchTransitionRoutine(sceneName, title, subtitle, isRestart, onComplete));
    }

    private void GetSectorInfo(string sceneName, bool isRestart, out string title, out string subtitle, out string code)
    {
        string s = sceneName.ToLower();

        if (isRestart)
        {
            code = "SYS_REBOOT // MEMORY_PURGE";
            if (s.Contains("reactor") || s.Contains("level2"))
            {
                title = "SECTOR 02 // KHU LÒ PHẢN ỨNG";
                subtitle = "TÁI KHỞI ĐỘNG HỆ THỐNG: Khóa 3 van khí độc và kích hoạt thang máy!";
            }
            else if (s.Contains("helipad") || s.Contains("level3"))
            {
                title = "SECTOR 03 // SÂN ĐỖ TRỰC THĂNG";
                subtitle = "TÁI KHỞI ĐỘNG HỆ THỐNG: Tiếp nhiên liệu và chuẩn bị cất cánh!";
            }
            else
            {
                title = "SECTOR 01 // PHÒNG THÍ NGHIỆM";
                subtitle = "TÁI KHỞI ĐỘNG HỆ THỐNG: Tìm Thẻ Bảo Mật, Lắp Cầu Chì & Trốn Thoát!";
            }
            return;
        }

        if (s.Contains("reactor") || s.Contains("level2"))
        {
            code = "SECTOR_02 // HAZARD_LEVEL_4";
            title = "SECTOR 02 // KHU LÒ PHẢN ỨNG (REACTOR CORE)";
            subtitle = "CẢNH BÁO: Rò rỉ khí độc cấp 4! Dùng Cờ Lê khóa 3 van, đóng Cầu Dao & Gọi Thang Máy.";
        }
        else if (s.Contains("helipad") || s.Contains("level3"))
        {
            code = "SECTOR_03 // ROOFTOP_EXTRACTION";
            title = "SECTOR 03 // SÂN ĐỖ TRỰC THĂNG (ROOFTOP HELIPAD)";
            subtitle = "NHIỆM VỤ CUỐI: Tiếp nhiên liệu, Kích hoạt Đài Radar & Lên Trực Thăng Tẩu Thoát!";
        }
        else
        {
            code = "SECTOR_01 // SECURE_LAB";
            title = "SECTOR 01 // PHÒNG THÍ NGHIỆM (LABORATORY)";
            subtitle = "MỤC TIÊU: Tìm Thẻ Bảo Mật, Lắp Cầu Chì, Giải Mã Terminal và Trốn Thoát.";
        }
    }

    private IEnumerator GlitchTransitionRoutine(string sceneName, string customTitle, string customSubtitle, bool isRestart, System.Action onComplete)
    {
        isTransitioning = true;
        Time.timeScale = 1f;

        GetSectorInfo(sceneName, isRestart, out string defaultTitle, out string defaultSubtitle, out string codeName);
        string displayTitle = !string.IsNullOrEmpty(customTitle) ? customTitle : defaultTitle;
        string displaySubtitle = !string.IsNullOrEmpty(customSubtitle) ? customSubtitle : defaultSubtitle;

        transitionCanvas.gameObject.SetActive(true);
        canvasGroup.alpha = 1f;

        // Âm thanh Glitch rè điện tử
        PlaySound(glitchSound != null ? glitchSound : GetProceduralGlitchAudio());

        // ── PHASE 1: GLITCH BURST (Màn hình nhiễu loạn cực mạnh) ──
        float phase1Duration = 0.65f;
        float elapsed = 0f;

        terminalLogText.text = $"[⚠ ALERT: SECTOR TRANSIT]\n>>> CODE: {codeName}\n>>> DECRYPTING TELEMETRY FEED...";
        sectorTitleText.text = "";
        sectorSubtitleText.text = "";
        progressText.text = "SYNCING DATA [ 0% ]";
        if (progressBarFill != null) progressBarFill.fillAmount = 0f;

        while (elapsed < phase1Duration)
        {
            elapsed += Time.unscaledDeltaTime;
            RandomizeGlitchBars(intensity: 1.5f);
            yield return null;
        }

        // ── PHASE 2: TERMINAL HUD & ASYNC LOAD (Giải mã chữ & Tải Scene) ──
        sectorTitleText.text = displayTitle;
        sectorSubtitleText.text = displaySubtitle;

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false;

        float hudDuration = 1.35f;
        float hudElapsed = 0f;

        while (hudElapsed < hudDuration || asyncLoad.progress < 0.9f)
        {
            hudElapsed += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(Mathf.Min(hudElapsed / hudDuration, asyncLoad.progress / 0.9f));

            if (progressBarFill != null) progressBarFill.fillAmount = progress;
            int pct = Mathf.RoundToInt(progress * 100f);
            progressText.text = $"ESTABLISHING SECURE PROTOCOL [ {pct}% ]";

            // Nhiễu nhẹ liên tục trong lúc đọc dữ liệu
            RandomizeGlitchBars(intensity: 0.45f);
            yield return null;
        }

        // 100% Complete -> Phát âm thanh Warp hoàn tất
        progressText.text = "ACCESS GRANTED. ENTERING SECTOR [ 100% ]";
        if (progressBarFill != null) progressBarFill.fillAmount = 1f;
        PlaySound(warpCompleteSound != null ? warpCompleteSound : GetProceduralWarpAudio());

        // Tia chớp trắng glitch khi kích hoạt
        FlashGlitchBars(new Color(1f, 1f, 1f, 0.9f));
        yield return new WaitForSecondsRealtime(0.25f);

        // Kích hoạt Scene mới
        asyncLoad.allowSceneActivation = true;
        while (!asyncLoad.isDone)
        {
            yield return null;
        }

        // Đợi 1 frame để scene mới khởi tạo camera và ánh sáng
        yield return null;

        // ── PHASE 3: DISSIPATE GLITCH (Mờ dần trả lại góc nhìn cho người chơi) ──
        float fadeOutTime = 0.45f;
        float fadeElapsed = 0f;
        while (fadeElapsed < fadeOutTime)
        {
            fadeElapsed += Time.unscaledDeltaTime;
            float t = fadeElapsed / fadeOutTime;
            canvasGroup.alpha = 1f - t;
            RandomizeGlitchBars(intensity: (1f - t) * 0.8f);
            yield return null;
        }

        canvasGroup.alpha = 0f;
        transitionCanvas.gameObject.SetActive(false);
        isTransitioning = false;

        onComplete?.Invoke();
    }

    private void RandomizeGlitchBars(float intensity)
    {
        if (glitchBars == null) return;

        for (int i = 0; i < glitchBars.Length; i++)
        {
            var bar = glitchBars[i];
            if (bar == null) continue;

            bool show = Random.value < (0.6f * intensity);
            bar.gameObject.SetActive(show);
            if (!show) continue;

            float w = Random.Range(300f, 1920f);
            float h = Random.Range(4f, 45f) * intensity;
            bar.sizeDelta = new Vector2(w, h);

            float posX = Random.Range(-180f, 180f) * intensity;
            float posY = Random.Range(-520f, 520f);
            bar.anchoredPosition = new Vector2(posX, posY);

            var img = bar.GetComponent<Image>();
            if (img != null)
            {
                // Chọn ngẫu nhiên bảng màu Cyberpunk Sci-Fi
                float rCol = Random.value;
                Color col;
                if (rCol < 0.35f) col = new Color(0f, 0.95f, 1f, Random.Range(0.4f, 0.85f)); // Neon Cyan
                else if (rCol < 0.7f) col = new Color(1f, 0.05f, 0.45f, Random.Range(0.4f, 0.85f)); // Neon Magenta
                else if (rCol < 0.85f) col = new Color(1f, 0.85f, 0.1f, Random.Range(0.4f, 0.85f)); // Cyber Amber
                else col = new Color(1f, 1f, 1f, Random.Range(0.6f, 0.95f)); // White Glitch

                img.color = col;
            }
        }
    }

    private void FlashGlitchBars(Color flashCol)
    {
        if (glitchBars == null) return;
        foreach (var bar in glitchBars)
        {
            if (bar == null) continue;
            bar.gameObject.SetActive(true);
            bar.sizeDelta = new Vector2(1920f, Random.Range(10f, 60f));
            var img = bar.GetComponent<Image>();
            if (img != null) img.color = flashCol;
        }
    }

    // ───────────────────────────────────────────────
    // UI SETUP (Tự động tạo Canvas không cần kéo thả Inspector)
    // ───────────────────────────────────────────────

    public void EnsureUIElements()
    {
        if (transitionCanvas != null) return;

        // 1. Root Canvas
        GameObject canvasGO = new GameObject("SciFiTransitionCanvas");
        canvasGO.transform.SetParent(transform, false);
        transitionCanvas = canvasGO.AddComponent<Canvas>();
        transitionCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        transitionCanvas.sortingOrder = 99998;

        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasGO.AddComponent<GraphicRaycaster>();
        canvasGroup = canvasGO.AddComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = true;

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        // 2. Nền tối sâu (Deep Dark Background)
        GameObject bgGO = new GameObject("BackgroundDark");
        bgGO.transform.SetParent(canvasGO.transform, false);
        var bgR = bgGO.AddComponent<RectTransform>();
        bgR.anchorMin = Vector2.zero; bgR.anchorMax = Vector2.one;
        bgR.offsetMin = Vector2.zero; bgR.offsetMax = Vector2.zero;
        var bgImg = bgGO.AddComponent<Image>();
        bgImg.color = new Color(0.02f, 0.025f, 0.04f, 0.98f);

        // 3. CRT Scanline Grid Texture
        GameObject scanGO = new GameObject("CRTScanlines");
        scanGO.transform.SetParent(canvasGO.transform, false);
        var scanR = scanGO.AddComponent<RectTransform>();
        scanR.anchorMin = Vector2.zero; scanR.anchorMax = Vector2.one;
        scanR.offsetMin = Vector2.zero; scanR.offsetMax = Vector2.zero;
        scanlineImage = scanGO.AddComponent<RawImage>();
        scanlineImage.texture = GetScanlineTexture();
        scanlineImage.uvRect = new Rect(0f, 0f, 1920f, 270f);
        scanlineImage.color = new Color(1f, 1f, 1f, 0.18f);

        // 4. Glitch Slices Container
        GameObject glitchContGO = new GameObject("GlitchBarsContainer");
        glitchContGO.transform.SetParent(canvasGO.transform, false);
        glitchBarsContainer = glitchContGO.AddComponent<RectTransform>();
        glitchBarsContainer.anchorMin = new Vector2(0.5f, 0.5f);
        glitchBarsContainer.anchorMax = new Vector2(0.5f, 0.5f);
        glitchBarsContainer.sizeDelta = new Vector2(1920f, 1080f);

        int barCount = 10;
        glitchBars = new RectTransform[barCount];
        for (int i = 0; i < barCount; i++)
        {
            var bGO = new GameObject($"GlitchBar_{i}");
            bGO.transform.SetParent(glitchBarsContainer, false);
            glitchBars[i] = bGO.AddComponent<RectTransform>();
            glitchBars[i].anchorMin = new Vector2(0.5f, 0.5f);
            glitchBars[i].anchorMax = new Vector2(0.5f, 0.5f);
            var img = bGO.AddComponent<Image>();
            img.color = new Color(0f, 0.95f, 1f, 0.7f);
            bGO.SetActive(false);
        }

        // 5. Terminal HUD Dialog Box
        GameObject hudBoxGO = new GameObject("HUDBox");
        hudBoxGO.transform.SetParent(canvasGO.transform, false);
        var boxR = hudBoxGO.AddComponent<RectTransform>();
        boxR.anchorMin = new Vector2(0.5f, 0.5f);
        boxR.anchorMax = new Vector2(0.5f, 0.5f);
        boxR.sizeDelta = new Vector2(960f, 440f);

        var boxImg = hudBoxGO.AddComponent<Image>();
        boxImg.color = new Color(0.04f, 0.06f, 0.09f, 0.88f);
        var boxOl = hudBoxGO.AddComponent<Outline>();
        boxOl.effectColor = new Color(0f, 0.95f, 1f, 0.85f);
        boxOl.effectDistance = new Vector2(2f, -2f);

        // 6. Terminal Log Status Text (Trên)
        GameObject logGO = new GameObject("TerminalLogText");
        logGO.transform.SetParent(hudBoxGO.transform, false);
        var logR = logGO.AddComponent<RectTransform>();
        logR.anchorMin = new Vector2(0f, 1f); logR.anchorMax = new Vector2(1f, 1f);
        logR.pivot = new Vector2(0.5f, 1f);
        logR.anchoredPosition = new Vector2(0f, -25f);
        logR.sizeDelta = new Vector2(-60f, 75f);
        terminalLogText = logGO.AddComponent<Text>();
        terminalLogText.font = defaultFont;
        terminalLogText.fontSize = 15;
        terminalLogText.lineSpacing = 1.2f;
        terminalLogText.alignment = TextAnchor.UpperLeft;
        terminalLogText.color = new Color(0f, 0.95f, 1f, 0.95f);

        // 7. Sector Title Text (Giữa)
        GameObject titleGO = new GameObject("SectorTitleText");
        titleGO.transform.SetParent(hudBoxGO.transform, false);
        var titleR = titleGO.AddComponent<RectTransform>();
        titleR.anchorMin = new Vector2(0f, 0.5f); titleR.anchorMax = new Vector2(1f, 0.5f);
        titleR.pivot = new Vector2(0.5f, 0.5f);
        titleR.anchoredPosition = new Vector2(0f, 20f);
        titleR.sizeDelta = new Vector2(-60f, 70f);
        sectorTitleText = titleGO.AddComponent<Text>();
        sectorTitleText.font = defaultFont;
        sectorTitleText.fontSize = 32;
        sectorTitleText.fontStyle = FontStyle.Bold;
        sectorTitleText.alignment = TextAnchor.MiddleCenter;
        sectorTitleText.color = new Color(1f, 0.98f, 0.95f, 1f);
        var titleOl = titleGO.AddComponent<Outline>();
        titleOl.effectColor = new Color(1f, 0.05f, 0.45f, 0.85f);
        titleOl.effectDistance = new Vector2(2.5f, -2.5f);

        // 8. Sector Subtitle Text (Dưới Tiêu Đề)
        GameObject subGO = new GameObject("SectorSubtitleText");
        subGO.transform.SetParent(hudBoxGO.transform, false);
        var subR = subGO.AddComponent<RectTransform>();
        subR.anchorMin = new Vector2(0f, 0.5f); subR.anchorMax = new Vector2(1f, 0.5f);
        subR.pivot = new Vector2(0.5f, 0.5f);
        subR.anchoredPosition = new Vector2(0f, -40f);
        subR.sizeDelta = new Vector2(-60f, 50f);
        sectorSubtitleText = subGO.AddComponent<Text>();
        sectorSubtitleText.font = defaultFont;
        sectorSubtitleText.fontSize = 16;
        sectorSubtitleText.alignment = TextAnchor.MiddleCenter;
        sectorSubtitleText.color = new Color(0.9f, 0.8f, 0.4f, 0.95f);

        // 9. Cyber Loading Bar Background
        GameObject barBgGO = new GameObject("ProgressBarBg");
        barBgGO.transform.SetParent(hudBoxGO.transform, false);
        var barBgR = barBgGO.AddComponent<RectTransform>();
        barBgR.anchorMin = new Vector2(0.5f, 0f); barBgR.anchorMax = new Vector2(0.5f, 0f);
        barBgR.pivot = new Vector2(0.5f, 0f);
        barBgR.anchoredPosition = new Vector2(0f, 45f);
        barBgR.sizeDelta = new Vector2(800f, 18f);
        var barBgImg = barBgGO.AddComponent<Image>();
        barBgImg.color = new Color(0.1f, 0.15f, 0.2f, 0.8f);

        // 10. Cyber Loading Bar Fill
        GameObject barFillGO = new GameObject("ProgressBarFill");
        barFillGO.transform.SetParent(barBgGO.transform, false);
        var barFillR = barFillGO.AddComponent<RectTransform>();
        barFillR.anchorMin = Vector2.zero; barFillR.anchorMax = Vector2.one;
        barFillR.offsetMin = Vector2.zero; barFillR.offsetMax = Vector2.zero;
        progressBarFill = barFillGO.AddComponent<Image>();
        progressBarFill.type = Image.Type.Filled;
        progressBarFill.fillMethod = Image.FillMethod.Horizontal;
        progressBarFill.fillAmount = 0f;
        progressBarFill.color = new Color(0f, 0.95f, 1f, 1f);

        // 11. Progress Percent Text
        GameObject progTxtGO = new GameObject("ProgressText");
        progTxtGO.transform.SetParent(hudBoxGO.transform, false);
        var progR = progTxtGO.AddComponent<RectTransform>();
        progR.anchorMin = new Vector2(0.5f, 0f); progR.anchorMax = new Vector2(0.5f, 0f);
        progR.pivot = new Vector2(0.5f, 0f);
        progR.anchoredPosition = new Vector2(0f, 18f);
        progR.sizeDelta = new Vector2(800f, 24f);
        progressText = progTxtGO.AddComponent<Text>();
        progressText.font = defaultFont;
        progressText.fontSize = 14;
        progressText.fontStyle = FontStyle.Bold;
        progressText.alignment = TextAnchor.MiddleCenter;
        progressText.color = new Color(0f, 0.95f, 1f, 0.9f);

        canvasGroup.alpha = 0f;
        transitionCanvas.gameObject.SetActive(false);
    }

    private Texture2D GetScanlineTexture()
    {
        if (scanlineTexture != null) return scanlineTexture;
        scanlineTexture = new Texture2D(2, 4, TextureFormat.RGBA32, false);
        scanlineTexture.filterMode = FilterMode.Point;
        scanlineTexture.wrapMode = TextureWrapMode.Repeat;
        Color blackLine = new Color(0f, 0f, 0f, 0.75f);
        Color clearLine = new Color(0f, 0f, 0f, 0.05f);
        scanlineTexture.SetPixel(0, 0, blackLine);
        scanlineTexture.SetPixel(1, 0, blackLine);
        scanlineTexture.SetPixel(0, 1, blackLine);
        scanlineTexture.SetPixel(1, 1, blackLine);
        scanlineTexture.SetPixel(0, 2, clearLine);
        scanlineTexture.SetPixel(1, 2, clearLine);
        scanlineTexture.SetPixel(0, 3, clearLine);
        scanlineTexture.SetPixel(1, 3, clearLine);
        scanlineTexture.Apply();
        return scanlineTexture;
    }

    private void PlaySound(AudioClip clip)
    {
        if (clip != null && audioSource != null)
        {
            audioSource.PlayOneShot(clip, 0.9f);
        }
    }

    // ───────────────────────────────────────────────
    // PROCEDURAL AUDIO (Tạo âm thanh Sci-Fi Glitch bằng mã C#)
    // ───────────────────────────────────────────────

    public static AudioClip GetProceduralGlitchAudio()
    {
        if (proceduralGlitchAudio != null) return proceduralGlitchAudio;
        int sampleRate = 44100;
        float duration = 0.65f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        System.Random rand = new System.Random(99);

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Sin(Mathf.Clamp01(t / duration) * Mathf.PI);

            // Modulated digital buzz + noise bursts
            float freq = (i % 250 < 125) ? 140f : 440f;
            if (t > 0.3f && t < 0.45f) freq = 880f;
            float tone = Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 0.45f : -0.45f;
            float noise = ((float)rand.NextDouble() * 2f - 1f) * 0.4f;
            if (rand.NextDouble() > 0.88) noise *= 2.2f;

            samples[i] = Mathf.Clamp((tone + noise) * env, -1f, 1f);
        }

        proceduralGlitchAudio = AudioClip.Create("SciFi_Glitch_Procedural", sampleCount, 1, sampleRate, false);
        proceduralGlitchAudio.SetData(samples, 0);
        return proceduralGlitchAudio;
    }

    public static AudioClip GetProceduralWarpAudio()
    {
        if (proceduralWarpAudio != null) return proceduralWarpAudio;
        int sampleRate = 44100;
        float duration = 0.85f;
        int sampleCount = Mathf.FloorToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float t = (float)i / sampleRate;
            float env = Mathf.Exp(-t * 3.5f);

            // Deep sub-bass swell dropping from 120Hz down to 40Hz
            float freq = Mathf.Lerp(120f, 40f, t / duration);
            float sub = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.65f;
            // Cyber chime at 1200Hz
            float chime = Mathf.Sin(2f * Mathf.PI * 1200f * t) * Mathf.Exp(-t * 12f) * 0.35f;

            samples[i] = Mathf.Clamp((sub + chime) * env, -1f, 1f);
        }

        proceduralWarpAudio = AudioClip.Create("SciFi_Warp_Procedural", sampleCount, 1, sampleRate, false);
        proceduralWarpAudio.SetData(samples, 0);
        return proceduralWarpAudio;
    }
}
