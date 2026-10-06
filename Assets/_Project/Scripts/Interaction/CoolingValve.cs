using UnityEngine;
using System.Collections;

/// <summary>
/// CoolingValve: Van lam mat xa khi doc trong Man 2.
/// - Ban dau: Van dang mo xa khi doc man manh, hoi nuoc phut ra, nguoi choi cham vao bi tru mau.
/// - Tuong tac [E]: Nguoi choi van dong van, tat luong hoi nuoc, giam muc do ngat khi toan man.
/// </summary>
public class CoolingValve : MonoBehaviour, IInteractable
{
    [Header("Valve Components")]
    [SerializeField] private Transform wheelTransform;
    [SerializeField] private Light statusLight;
    [SerializeField] private ParticleSystem steamEffect;
    [SerializeField] private GameObject gasDamageTrigger;

    private bool isClosed = false;
    private bool isSpinning = false;

    public bool CanInteract => !isClosed && !isSpinning;
    public string InteractPromptText
    {
        get
        {
            if (isClosed) return "Van khí đã được đóng kín";
            bool hasWrench = GameState.Instance != null && GameState.Instance.WrenchCollected;
            return hasWrench ? "Dùng Cờ Lê vặn đóng van khí [E]" : "Cần Cờ Lê (Wrench) để vặn van xả khí [E]";
        }
    }

    public void SetupReferences(Transform wheel, Light light, ParticleSystem steam, GameObject dmgTrigger)
    {
        wheelTransform = wheel;
        statusLight = light;
        steamEffect = steam;
        gasDamageTrigger = dmgTrigger;
    }

    private void Awake()
    {
        var col = GetComponent<Collider>();
        if (col == null)
        {
            var sc = gameObject.AddComponent<SphereCollider>();
            sc.radius = 1.8f;
            sc.isTrigger = true;
        }
    }

    private void Start()
    {
        UpdateVisual();
        EnsureRealisticSteamVisuals();
    }

    public void Interact()
    {
        if (isClosed || isSpinning) return;

        bool hasWrench = GameState.Instance != null && GameState.Instance.WrenchCollected;
        if (!hasWrench)
        {
            NotificationUI.ShowMessage("❌ BẠN CẦN CỜ LÊ ĐỂ VẶN VAN! Hãy tìm Cờ Lê tại Kho Phụ Tùng (Phía Bắc).");
            AudioSource errAudio = GetComponent<AudioSource>();
            if (errAudio == null)
            {
                errAudio = gameObject.AddComponent<AudioSource>();
                errAudio.spatialBlend = 0.8f;
            }
            errAudio.pitch = 0.6f;
            errAudio.Play();
            return;
        }

        StartCoroutine(CloseValveRoutine());
    }

    private IEnumerator CloseValveRoutine()
    {
        isSpinning = true;
        isClosed = true;

        // Am thanh van van
        AudioSource audio = GetComponent<AudioSource>();
        if (audio == null)
        {
            audio = gameObject.AddComponent<AudioSource>();
            audio.spatialBlend = 0.8f;
        }
        audio.pitch = 1.0f;

        // Xoay tay quay van dong chat
        if (wheelTransform != null)
        {
            float elapsed = 0f;
            float duration = 1.0f;
            Quaternion startRot = wheelTransform.localRotation;
            Quaternion endRot = startRot * Quaternion.Euler(0, 0, -720f);

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                wheelTransform.localRotation = Quaternion.Slerp(startRot, endRot, elapsed / duration);
                yield return null;
            }
            wheelTransform.localRotation = endRot;
        }
        else
        {
            yield return new WaitForSeconds(0.4f);
        }

        // Tat luong hoi nuoc phut ra
        if (steamEffect != null)
        {
            steamEffect.Stop();
        }

        // Tat vung sat thuong hoi nong
        if (gasDamageTrigger != null)
        {
            gasDamageTrigger.SetActive(false);
        }

        UpdateVisual();
        isSpinning = false;

        // Thong bao toi he thong ngat khi
        ToxicGasManager.OnValveClosed();
    }

    private void UpdateVisual()
    {
        if (statusLight != null)
        {
            // Do = dang xa khi (nguy hiem) | Xanh = da dong van (an toan)
            statusLight.color = isClosed ? Color.green : Color.red;
            statusLight.intensity = isClosed ? 2.0f : 1.5f;
        }
    }

    public static void ResetValveProgress()
    {
        // Hook cho reset tien do van
    }

    public static int totalValvesCount => 3;
    public static int turnedValvesCount => 3 - ToxicGasManager.OpenValvesCount;
    public static bool AllValvesTurned => ToxicGasManager.IsAirSafe;

    private static Texture2D _cachedSoftPuffTex;
    private static Texture2D CreateSoftPuffTexture()
    {
        if (_cachedSoftPuffTex != null) return _cachedSoftPuffTex;
        int size = 128;
        _cachedSoftPuffTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        _cachedSoftPuffTex.wrapMode = TextureWrapMode.Clamp;
        Vector2 center = new Vector2(size * 0.5f, size * 0.5f);
        float radius = size * 0.48f;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), center) / radius;
                float alpha = Mathf.Clamp01(1f - dist);
                alpha = alpha * alpha * (3f - 2f * alpha);
                _cachedSoftPuffTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        _cachedSoftPuffTex.Apply();
        return _cachedSoftPuffTex;
    }

    private void EnsureRealisticSteamVisuals()
    {
        if (steamEffect == null)
        {
            var steamChild = transform.Find("SteamJet");
            if (steamChild != null) steamEffect = steamChild.GetComponent<ParticleSystem>();
        }
        if (steamEffect == null) return;

        var psr = steamEffect.GetComponent<ParticleSystemRenderer>();
        if (psr == null) psr = steamEffect.gameObject.AddComponent<ParticleSystemRenderer>();

        bool needsRealisticMaterial = false;
        if (psr.sharedMaterial == null || psr.sharedMaterial.shader == null)
        {
            needsRealisticMaterial = true;
        }
        else
        {
            string sName = psr.sharedMaterial.shader.name;
            if (!sName.Contains("Universal Render Pipeline") || sName.Contains("Error") || psr.sharedMaterial.name.Contains("Default-Particle"))
            {
                needsRealisticMaterial = true;
            }
        }

        if (needsRealisticMaterial)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Unlit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

            Material mat = new Material(shader);
            mat.name = "M_Steam_Realistic_Runtime";
            mat.SetFloat("_Surface", 1);
            mat.SetFloat("_Blend", 0);
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.renderQueue = 3000;
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetColor("_BaseColor", new Color(0.92f, 0.96f, 1f, 0.45f));

            Texture2D softTex = CreateSoftPuffTexture();
            mat.SetTexture("_BaseMap", softTex);
            mat.mainTexture = softTex;

            psr.material = mat;
        }

        psr.renderMode = ParticleSystemRenderMode.Billboard;
        psr.alignment = ParticleSystemRenderSpace.View;

        var main = steamEffect.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.2f, 1.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(4.5f, 6.0f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.35f, 0.70f);
        main.startColor = new Color(0.92f, 0.96f, 1.0f, 0.50f);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, 360f * Mathf.Deg2Rad);
        main.gravityModifier = -0.04f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        var sizeModule = steamEffect.sizeOverLifetime;
        sizeModule.enabled = true;
        AnimationCurve sizeCurve = new AnimationCurve();
        sizeCurve.AddKey(0f, 0.35f);
        sizeCurve.AddKey(0.35f, 1.15f);
        sizeCurve.AddKey(1f, 2.3f);
        sizeModule.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorModule = steamEffect.colorOverLifetime;
        colorModule.enabled = true;
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] {
                new GradientColorKey(new Color(0.95f, 0.98f, 1.0f), 0f),
                new GradientColorKey(new Color(0.88f, 0.93f, 0.98f), 1f)
            },
            new GradientAlphaKey[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.55f, 0.12f),
                new GradientAlphaKey(0.40f, 0.60f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorModule.color = grad;

        var rotModule = steamEffect.rotationOverLifetime;
        rotModule.enabled = true;
        rotModule.z = new ParticleSystem.MinMaxCurve(-45f * Mathf.Deg2Rad, 45f * Mathf.Deg2Rad);
    }
}

