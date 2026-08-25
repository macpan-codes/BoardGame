using System.Collections;
using TMPro;
using UnityEngine;

public class GameNotificationUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField]
    [Min(0.1f)]
    private float displayDuration = 2.5f;

    [SerializeField]
    private float fadeDuration = 0.15f;

    private Coroutine notificationRoutine;

    private static GameNotificationUI instance;

    public static GameNotificationUI Instance =>
        instance;

    private CanvasGroup canvasGroup;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        if (panel == null)
            panel = gameObject;

        canvasGroup =
            panel.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup =
                panel.AddComponent<CanvasGroup>();
        }

        HideImmediate();
    }

    // ============================================================
    // SHOW
    // ============================================================

    public static void Show(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        if (Instance == null)
        {
            Debug.LogWarning(
                $"GameNotificationUI: No active instance. " +
                $"Message was: {message}"
            );

            return;
        }

        Instance.ShowInternal(
            message
        );
    }

    private void ShowInternal(
        string message)
    {
        if (messageText == null)
        {
            Debug.LogWarning(
                "GameNotificationUI: MessageText is not assigned."
            );

            return;
        }

        if (panel == null)
        {
            Debug.LogWarning(
                "GameNotificationUI: Panel is not assigned."
            );

            return;
        }

        if (notificationRoutine != null)
        {
            StopCoroutine(
                notificationRoutine
            );
        }

        messageText.text =
            message;

        panel.SetActive(true);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
        }

        notificationRoutine =
            StartCoroutine(
                NotificationRoutine()
            );
    }

    // ============================================================
    // ROUTINE
    // ============================================================

    private IEnumerator NotificationRoutine()
    {
        yield return new WaitForSeconds(
            Mathf.Max(
                0.1f,
                displayDuration
            )
        );

        if (canvasGroup != null &&
            fadeDuration > 0f)
        {
            float elapsed = 0f;

            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;

                float progress =
                    Mathf.Clamp01(
                        elapsed / fadeDuration
                    );

                canvasGroup.alpha =
                    1f - progress;

                yield return null;
            }
        }

        HideImmediate();

        notificationRoutine = null;
    }

    // ============================================================
    // HIDE
    // ============================================================

    public void HideImmediate()
    {
        if (notificationRoutine != null)
        {
            StopCoroutine(
                notificationRoutine
            );

            notificationRoutine = null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }

        if (panel != null)
        {
            panel.SetActive(false);
        }
    }
}