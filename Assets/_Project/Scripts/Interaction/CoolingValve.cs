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
    public string InteractPromptText => isClosed ? "Van khí đã được đóng kín" : "Vặn đóng van xả khí độc [E]";

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
    }

    public void Interact()
    {
        if (isClosed || isSpinning) return;
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
}

