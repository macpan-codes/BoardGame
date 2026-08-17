using System.Collections;
using TMPro;
using UnityEngine;

public class GameNotificationUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject panel;
    [SerializeField] private TMP_Text messageText;

    [Header("Timing")]
    [SerializeField] private float displayDuration = 1.8f;

    private Coroutine hideRoutine;

    private static GameNotificationUI instance;

    public static GameNotificationUI Instance => instance;

    private void Awake()
    {
        instance = this;

        if (panel == null)
            panel = gameObject;

        HideImmediate();
    }

    public static void Show(string message)
    {
        if (Instance == null)
            return;

        Instance.ShowInternal(message);
    }

    private void ShowInternal(string message)
    {
        if (messageText != null)
            messageText.text = message;

        if (panel != null)
            panel.SetActive(true);

        if (hideRoutine != null)
            StopCoroutine(hideRoutine);

        hideRoutine = StartCoroutine(
            HideAfterDelay()
        );
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(
            Mathf.Max(0.1f, displayDuration)
        );

        HideImmediate();
        hideRoutine = null;
    }

    public void HideImmediate()
    {
        if (panel != null)
            panel.SetActive(false);
    }
}