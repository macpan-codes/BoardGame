using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Automatically adds the shared button-click sound to Unity UI Buttons,
/// including buttons created at runtime and buttons in newly loaded scenes.
/// Put buttonclick.mp3 at Assets/Resources/Audio/buttonclick.mp3.
/// </summary>
public sealed class ButtonClickSound : MonoBehaviour
{
    private static ButtonClickSound runner;
    private static AudioClip clickClip;
    private readonly HashSet<Button> hookedButtons = new HashSet<Button>();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install()
    {
        if (runner != null)
            return;

        GameObject host = new GameObject("ButtonClickSound_AutoBinder");
        DontDestroyOnLoad(host);
        runner = host.AddComponent<ButtonClickSound>();
    }

    private void Awake()
    {
        if (clickClip == null)
            clickClip = Resources.Load<AudioClip>("Audio/buttonclick");

        StartCoroutine(BindButtonsRoutine());
    }

    private IEnumerator BindButtonsRoutine()
    {
        var wait = new WaitForSecondsRealtime(0.25f);
        while (true)
        {
            Button[] buttons = FindObjectsByType<Button>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            foreach (Button button in buttons)
            {
                if (button == null || !hookedButtons.Add(button))
                    continue;

                ButtonClickSoundHook hook = button.GetComponent<ButtonClickSoundHook>();
                if (hook == null)
                    hook = button.gameObject.AddComponent<ButtonClickSoundHook>();

                hook.Initialize(clickClip);
            }

            hookedButtons.RemoveWhere(button => button == null);
            yield return wait;
        }
    }
}

public sealed class ButtonClickSoundHook : MonoBehaviour
{
    private AudioSource audioSource;
    private AudioClip clip;
    private Button button;

    public void Initialize(AudioClip sharedClip)
    {
        clip = sharedClip;
        button = GetComponent<Button>();

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        audioSource.playOnAwake = false;
        audioSource.loop = false;
        audioSource.spatialBlend = 0f;

        if (button != null)
        {
            button.onClick.RemoveListener(PlayClick);
            button.onClick.AddListener(PlayClick);
        }
    }

    private void PlayClick()
    {
        if (clip == null)
            clip = Resources.Load<AudioClip>("Audio/buttonclick");

        if (clip == null || audioSource == null)
            return;

        audioSource.PlayOneShot(clip);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(PlayClick);
    }
}
