using UnityEngine;

/// <summary>
/// Persistent background music player. The music continues across scene loads
/// and loops until the application exits. Its volume is independent of effects.
/// </summary>
[DisallowMultipleComponent]
public sealed class BackgroundMusicController : MonoBehaviour
{
    private const string VolumeKey = "BoardGame.BackgroundMusicVolume";
    private static BackgroundMusicController instance;
    private AudioSource musicSource;

    public static float CurrentVolume =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 0.3f));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CreatePersistentPlayer()
    {
        if (instance != null)
            return;

        GameObject musicObject = new GameObject("BackgroundMusicController");
        instance = musicObject.AddComponent<BackgroundMusicController>();
        DontDestroyOnLoad(musicObject);
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        musicSource = GetComponent<AudioSource>();
        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = CurrentVolume;

        AudioClip music = Resources.Load<AudioClip>("Audio/background music1");
        if (music == null)
        {
            Debug.LogError(
                "BackgroundMusicController: Put background music1.mp3 at Assets/Resources/Audio/background music1.mp3.",
                this);
            return;
        }

        musicSource.clip = music;
        musicSource.Play();
    }

    public static void SetVolume(float volume)
    {
        float clamped = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(VolumeKey, clamped);

        if (instance != null && instance.musicSource != null)
            instance.musicSource.volume = clamped;
    }

    public static float GetVolume()
    {
        return CurrentVolume;
    }
}
