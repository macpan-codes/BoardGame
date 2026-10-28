using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shared runtime audio-settings panel used by the main menu and gameplay menu.
/// Audio preferences are stored locally so they persist between sessions.
/// </summary>
public sealed class GameAudioSettingsUI : MonoBehaviour
{
    private const string MasterVolumeKey = "BoardGame.MasterVolume";
    private const string MutedKey = "BoardGame.AudioMuted";
    private const string BackgroundMusicVolumeKey = "BoardGame.BackgroundMusicVolume";

    private static GameAudioSettingsUI instance;

    private GameObject overlay;
    private GameObject panel;
    private Slider masterVolumeSlider;
    private TMP_Text volumeValueText;
    private Slider backgroundMusicSlider;
    private TMP_Text backgroundMusicValueText;
    private Button muteButton;
    private TMP_Text muteButtonText;

    private static float SavedMasterVolume =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(MasterVolumeKey, 1f));

    private static bool SavedMuted =>
        PlayerPrefs.GetInt(MutedKey, 0) == 1;

    private static float SavedBackgroundMusicVolume =>
        Mathf.Clamp01(PlayerPrefs.GetFloat(BackgroundMusicVolumeKey, 0.3f));

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedAudioSettings()
    {
        AudioListener.volume = SavedMuted ? 0f : SavedMasterVolume;
    }

    public static void Open()
    {
        GameAudioSettingsUI existing =
            FindFirstObjectByType<GameAudioSettingsUI>(
                FindObjectsInactive.Include
            );

        if (existing != null)
        {
            existing.Show();
            return;
        }

        GameObject root = new GameObject(
            "GameAudioSettingsUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        GameAudioSettingsUI settings =
            root.AddComponent<GameAudioSettingsUI>();

        settings.Show();
    }

    public static void CloseOpenSettings()
    {
        GameAudioSettingsUI existing =
            FindFirstObjectByType<GameAudioSettingsUI>(
                FindObjectsInactive.Include
            );

        if (existing != null)
            existing.Close();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        if (transform.Find("AudioSettingsOverlay") != null)
            BindExistingUI();
        else
            BuildUI();
        if (overlay != null)
            overlay.SetActive(false);
        ApplySavedAudioSettings();
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void BuildUI()
    {
        Canvas canvas = GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5000;

        CanvasScaler scaler = GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = GetComponent<RectTransform>();
        Stretch(canvasRect);

        overlay = CreateImage(
            "AudioSettingsOverlay",
            transform,
            new Color(0.005f, 0.012f, 0.028f, 0.84f)
        );
        Stretch(overlay.GetComponent<RectTransform>());

        panel = CreateImage(
            "AudioSettingsPanel",
            overlay.transform,
            new Color(0.035f, 0.055f, 0.10f, 0.99f)
        );

        RectTransform panelRect = panel.GetComponent<RectTransform>();
        AnchorCenter(panelRect, Vector2.zero, new Vector2(640f, 600f));

        Outline panelOutline = panel.AddComponent<Outline>();
        panelOutline.effectColor = new Color(0.96f, 0.74f, 0.30f, 0.95f);
        panelOutline.effectDistance = new Vector2(2f, 2f);
        panelOutline.useGraphicAlpha = false;

        CreateText(
            panel.transform,
            "SETTINGS",
            34f,
            new Color(0.96f, 0.74f, 0.30f, 1f),
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f),
            new Vector2(0.95f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -54f),
            new Vector2(0f, 52f),
            FontStyles.Bold
        );

        CreateText(
            panel.transform,
            "Adjust the game audio.",
            19f,
            new Color(0.69f, 0.73f, 0.80f, 1f),
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f),
            new Vector2(0.95f, 1f),
            new Vector2(0.5f, 1f),
            new Vector2(0f, -98f),
            new Vector2(0f, 34f)
        );

        volumeValueText = CreateText(
            panel.transform,
            "MASTER VOLUME  100%",
            22f,
            Color.white,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.73f),
            new Vector2(0.92f, 0.83f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            FontStyles.Bold
        );

        masterVolumeSlider = CreateVolumeSlider(
            panel.transform,
            new Vector2(0.5f, 0.66f),
            new Vector2(440f, 42f)
        );

        masterVolumeSlider.SetValueWithoutNotify(SavedMasterVolume);
        masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);

        backgroundMusicValueText = CreateText(
            panel.transform,
            "BACKGROUND MUSIC  30%",
            22f,
            Color.white,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.48f),
            new Vector2(0.92f, 0.58f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            FontStyles.Bold
        );

        backgroundMusicSlider = CreateVolumeSlider(
            panel.transform,
            new Vector2(0.5f, 0.42f),
            new Vector2(440f, 42f)
        );
        backgroundMusicSlider.gameObject.name = "BackgroundMusicVolumeSlider";
        backgroundMusicSlider.SetValueWithoutNotify(SavedBackgroundMusicVolume);
        backgroundMusicSlider.onValueChanged.AddListener(OnBackgroundMusicVolumeChanged);

        muteButton = CreateButton(
            panel.transform,
            "MuteAudioButton",
            "MUTE AUDIO: OFF",
            new Vector2(0.5f, 0.25f),
            new Vector2(320f, 58f),
            new Color(0.16f, 0.22f, 0.32f, 1f),
            out muteButtonText
        );
        muteButton.onClick.AddListener(ToggleMute);

        CreateButton(
            panel.transform,
            "CloseAudioSettingsButton",
            "BACK TO GAME",
            new Vector2(0.5f, 0.10f),
            new Vector2(250f, 54f),
            new Color(0.14f, 0.42f, 0.31f, 1f),
            out _
        ).onClick.AddListener(Close);

        RefreshLabels();
        overlay.SetActive(false);
    }

    /// <summary>Builds the editable hierarchy when invoked by the Unity Editor tooling.</summary>
    public void BuildUIForEditor()
    {
        if (transform.Find("AudioSettingsOverlay") == null)
            BuildUI();
    }

    private void BindExistingUI()
    {
        Transform overlayTransform = transform.Find("AudioSettingsOverlay");
        overlay = overlayTransform != null ? overlayTransform.gameObject : null;
        Transform panelTransform = overlayTransform != null
            ? overlayTransform.Find("AudioSettingsPanel")
            : null;
        panel = panelTransform != null ? panelTransform.gameObject : null;

        if (panel == null)
        {
            Debug.LogError("GameAudioSettingsUI: Editable Settings panel is missing.", this);
            return;
        }

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.text.StartsWith("MASTER VOLUME", System.StringComparison.OrdinalIgnoreCase))
                volumeValueText = text;
            else if (text.text.StartsWith("BACKGROUND MUSIC", System.StringComparison.OrdinalIgnoreCase))
                backgroundMusicValueText = text;
            else if (text.text.StartsWith("MUTE AUDIO", System.StringComparison.OrdinalIgnoreCase))
                muteButtonText = text;
        }

        Transform sliderTransform = panel.transform.Find("MasterVolumeSlider");
        masterVolumeSlider = sliderTransform != null ? sliderTransform.GetComponent<Slider>() : null;
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.SetValueWithoutNotify(SavedMasterVolume);
            masterVolumeSlider.onValueChanged.RemoveListener(OnVolumeChanged);
            masterVolumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        }

        Transform musicSliderTransform = panel.transform.Find("BackgroundMusicVolumeSlider");
        backgroundMusicSlider = musicSliderTransform != null ? musicSliderTransform.GetComponent<Slider>() : null;
        if (backgroundMusicSlider != null)
        {
            backgroundMusicSlider.SetValueWithoutNotify(SavedBackgroundMusicVolume);
            backgroundMusicSlider.onValueChanged.RemoveListener(OnBackgroundMusicVolumeChanged);
            backgroundMusicSlider.onValueChanged.AddListener(OnBackgroundMusicVolumeChanged);
        }

        Transform muteTransform = panel.transform.Find("MuteAudioButton");
        muteButton = muteTransform != null ? muteTransform.GetComponent<Button>() : null;
        if (muteButton != null)
        {
            muteButton.onClick.RemoveListener(ToggleMute);
            muteButton.onClick.AddListener(ToggleMute);
        }

        Transform closeTransform = panel.transform.Find("CloseAudioSettingsButton");
        Button closeButton = closeTransform != null ? closeTransform.GetComponent<Button>() : null;
        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(Close);
            closeButton.onClick.AddListener(Close);
        }

        RefreshLabels();
    }

    private Slider CreateVolumeSlider(
        Transform parent,
        Vector2 anchor,
        Vector2 size)
    {
        GameObject sliderObject = new GameObject(
            "MasterVolumeSlider",
            typeof(RectTransform),
            typeof(Slider)
        );
        sliderObject.transform.SetParent(parent, false);

        RectTransform sliderRect = sliderObject.GetComponent<RectTransform>();
        sliderRect.anchorMin = anchor;
        sliderRect.anchorMax = anchor;
        sliderRect.pivot = new Vector2(0.5f, 0.5f);
        sliderRect.anchoredPosition = Vector2.zero;
        sliderRect.sizeDelta = size;

        GameObject track = CreateImage(
            "Track",
            sliderObject.transform,
            new Color(0.10f, 0.14f, 0.21f, 1f)
        );
        RectTransform trackRect = track.GetComponent<RectTransform>();
        trackRect.anchorMin = new Vector2(0f, 0.35f);
        trackRect.anchorMax = new Vector2(1f, 0.65f);
        trackRect.offsetMin = new Vector2(12f, 0f);
        trackRect.offsetMax = new Vector2(-12f, 0f);

        GameObject fillArea = new GameObject("Fill Area", typeof(RectTransform));
        fillArea.transform.SetParent(sliderObject.transform, false);
        RectTransform fillAreaRect = fillArea.GetComponent<RectTransform>();
        fillAreaRect.anchorMin = new Vector2(0f, 0.35f);
        fillAreaRect.anchorMax = new Vector2(1f, 0.65f);
        fillAreaRect.offsetMin = new Vector2(12f, 0f);
        fillAreaRect.offsetMax = new Vector2(-12f, 0f);

        GameObject fill = CreateImage(
            "Fill",
            fillArea.transform,
            new Color(0.96f, 0.74f, 0.30f, 1f)
        );
        RectTransform fillRect = fill.GetComponent<RectTransform>();
        fillRect.anchorMin = Vector2.zero;
        fillRect.anchorMax = Vector2.one;
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;

        GameObject handleArea = new GameObject(
            "Handle Slide Area",
            typeof(RectTransform)
        );
        handleArea.transform.SetParent(sliderObject.transform, false);
        RectTransform handleAreaRect = handleArea.GetComponent<RectTransform>();
        handleAreaRect.anchorMin = Vector2.zero;
        handleAreaRect.anchorMax = Vector2.one;
        handleAreaRect.offsetMin = new Vector2(12f, 0f);
        handleAreaRect.offsetMax = new Vector2(-12f, 0f);

        GameObject handle = CreateImage(
            "Handle",
            handleArea.transform,
            Color.white
        );
        RectTransform handleRect = handle.GetComponent<RectTransform>();
        handleRect.anchorMin = new Vector2(0.5f, 0.5f);
        handleRect.anchorMax = new Vector2(0.5f, 0.5f);
        handleRect.pivot = new Vector2(0.5f, 0.5f);
        handleRect.sizeDelta = new Vector2(26f, 34f);

        Slider slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.wholeNumbers = false;
        slider.direction = Slider.Direction.LeftToRight;
        slider.fillRect = fillRect;
        slider.handleRect = handleRect;
        slider.targetGraphic = handle.GetComponent<Image>();

        return slider;
    }

    private Button CreateButton(
        Transform parent,
        string objectName,
        string label,
        Vector2 anchor,
        Vector2 size,
        Color color,
        out TMP_Text buttonText)
    {
        GameObject buttonObject = CreateImage(
            objectName,
            parent,
            color
        );

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        button.transition = Selectable.Transition.ColorTint;

        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.14f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.14f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(color.r, color.g, color.b, 0.5f);
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        buttonText = CreateText(
            buttonObject.transform,
            label,
            20f,
            Color.white,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero,
            FontStyles.Bold
        );

        return button;
    }

    private TMP_Text CreateText(
        Transform parent,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        FontStyles style = FontStyles.Normal)
    {
        GameObject textObject = new GameObject(
            value.Replace(" ", "") + "Text",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        return text;
    }

    private GameObject CreateImage(
        string objectName,
        Transform parent,
        Color color)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image)
        );
        imageObject.transform.SetParent(parent, false);

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        return imageObject;
    }

    private void OnVolumeChanged(float value)
    {
        SetMasterVolume(value);
        RefreshLabels();
    }

    private void OnBackgroundMusicVolumeChanged(float value)
    {
        PlayerPrefs.SetFloat(BackgroundMusicVolumeKey, Mathf.Clamp01(value));
        PlayerPrefs.Save();
        BackgroundMusicController.SetVolume(value);
        RefreshLabels();
    }

    private void ToggleMute()
    {
        SetMuted(!SavedMuted);
        RefreshLabels();
    }

    private void RefreshLabels()
    {
        if (volumeValueText != null)
        {
            volumeValueText.text =
                "MASTER VOLUME  " +
                Mathf.RoundToInt(SavedMasterVolume * 100f) +
                "%";
        }

        if (backgroundMusicValueText != null)
        {
            backgroundMusicValueText.text =
                "BACKGROUND MUSIC  " +
                Mathf.RoundToInt(SavedBackgroundMusicVolume * 100f) +
                "%";
        }

        BackgroundMusicController.SetVolume(SavedBackgroundMusicVolume);

        if (muteButtonText != null)
        {
            muteButtonText.text =
                SavedMuted
                    ? "MUTE AUDIO: ON"
                    : "MUTE AUDIO: OFF";
        }
    }

    public static void SetMasterVolume(float value)
    {
        PlayerPrefs.SetFloat(
            MasterVolumeKey,
            Mathf.Clamp01(value)
        );

        ApplySavedAudioSettings();
    }

    public static void SetMuted(bool muted)
    {
        PlayerPrefs.SetInt(
            MutedKey,
            muted ? 1 : 0
        );

        ApplySavedAudioSettings();
    }

    public static float GetMasterVolume()
    {
        return SavedMasterVolume;
    }

    public static bool IsMuted()
    {
        return SavedMuted;
    }

    private void Show()
    {
        if (overlay != null)
            overlay.SetActive(true);
    }

    private void Close()
    {
        PlayerPrefs.Save();
        if (overlay != null)
            overlay.SetActive(false);
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void AnchorCenter(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
