using UnityEngine;

/// <summary>Centralized gameplay sound effects loaded from Resources/Audio.</summary>
public sealed class GameSoundEffects : MonoBehaviour
{
    private const string CardKey = "Audio/chance and comunity2";
    private const string TaxKey = "Audio/taxslot";
    private const string WinKey = "Audio/win";
    private static GameSoundEffects instance;
    private AudioSource cardSource;
    private AudioSource effectsSource;
    private AudioClip cardClip;
    private AudioClip taxClip;
    private AudioClip winClip;
    private bool winPlayed;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (instance != null) return;
        var host = new GameObject("GameSoundEffects");
        instance = host.AddComponent<GameSoundEffects>();
        DontDestroyOnLoad(host);
    }

    private void Awake()
    {
        if (instance != null && instance != this) { Destroy(gameObject); return; }
        instance = this;
        DontDestroyOnLoad(gameObject);
        cardSource = gameObject.AddComponent<AudioSource>();
        effectsSource = gameObject.AddComponent<AudioSource>();
        cardSource.playOnAwake = false; cardSource.spatialBlend = 0f;
        effectsSource.playOnAwake = false; effectsSource.spatialBlend = 0f;
        cardClip = Resources.Load<AudioClip>(CardKey);
        taxClip = Resources.Load<AudioClip>(TaxKey);
        winClip = Resources.Load<AudioClip>(WinKey);
        WarnIfMissing(cardClip, CardKey); WarnIfMissing(taxClip, TaxKey); WarnIfMissing(winClip, WinKey);
    }

    public static void StartCardSound()
    {
        if (instance == null || instance.cardClip == null) return;
        instance.cardSource.Stop(); instance.cardSource.clip = instance.cardClip;
        instance.cardSource.loop = true; instance.cardSource.Play();
    }
    public static void StopCardSound()
    {
        if (instance == null) return;
        instance.cardSource.Stop(); instance.cardSource.loop = false; instance.cardSource.clip = null;
    }
    public static void PlayTaxSound()
    {
        if (instance != null && instance.taxClip != null) instance.effectsSource.PlayOneShot(instance.taxClip);
    }
    public static void PlayWinSoundOnce()
    {
        if (instance == null || instance.winPlayed || instance.winClip == null) return;
        instance.winPlayed = true; instance.effectsSource.PlayOneShot(instance.winClip);
    }
    private static void WarnIfMissing(AudioClip clip, string key)
    {
        if (clip == null) Debug.LogWarning("GameSoundEffects: Missing Resources audio clip " + key + ". Copy the matching MP3 into Assets/Resources/Audio/.");
    }
}