using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// NotificationUI: Hien thi thong bao ngan (toast notification).
/// Static API: NotificationUI.ShowMessage("Text") tu bat ky script nao.
/// Attach vao: NotificationUI GameObject trong Canvas
/// </summary>
public class NotificationUI : MonoBehaviour
{
    public static NotificationUI Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameObject notificationPanel;
    [SerializeField] private Text messageText;

    [Header("Settings")]
    [SerializeField] private float displayDuration = 2.5f;
    [SerializeField] private float fadeSpeed = 3f;

    private CanvasGroup canvasGroup;
    private Coroutine showCoroutine;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        canvasGroup = notificationPanel?.GetComponent<CanvasGroup>();
        if (canvasGroup == null && notificationPanel != null)
            canvasGroup = notificationPanel.AddComponent<CanvasGroup>();

        if (notificationPanel != null) notificationPanel.SetActive(false);
    }

    /// <summary>Static shortcut de goi tu bat ky script nao.</summary>
    public static void ShowMessage(string message)
    {
        if (Instance != null) Instance.Show(message);
        else Debug.Log($"[Notification] {message}"); // Fallback
    }

    public void Show(string message)
    {
        if (showCoroutine != null) StopCoroutine(showCoroutine);
        showCoroutine = StartCoroutine(ShowRoutine(message));
    }

    private IEnumerator ShowRoutine(string message)
    {
        if (messageText != null) messageText.text = message;
        if (notificationPanel != null) notificationPanel.SetActive(true);

        // Fade in
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0;
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.deltaTime * fadeSpeed;
                yield return null;
            }
        }

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.deltaTime * fadeSpeed;
                yield return null;
            }
        }

        if (notificationPanel != null) notificationPanel.SetActive(false);
    }
}
