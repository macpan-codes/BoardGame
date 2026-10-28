using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using TMPro;

/// <summary>Standalone placeholder for future multiplayer; no network dependencies.</summary>
public sealed class MultiplayerComingSoonScreen : MonoBehaviour
{
    private static readonly Color Background = new Color(0.11f, 0.14f, 0.08f);
    private static readonly Color Gold = new Color(0.86f, 0.72f, 0.45f);
    private static readonly Color Cream = new Color(0.95f, 0.89f, 0.73f);
    private static readonly Color Olive = new Color(0.24f, 0.30f, 0.17f);

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    private static void Anchor(RectTransform r, Vector2 min, Vector2 max, Vector2 bottomLeft, Vector2 topRight)
    {
        r.anchorMin = min; r.anchorMax = max;
        r.offsetMin = bottomLeft; r.offsetMax = topRight;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string value, float fontSize, Color color)
    {
        RectTransform r = CreateRect("Text", parent);
        Anchor(r, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        var text = r.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.enableWordWrapping = true;
        text.raycastTarget = false;
        text.enableAutoSizing = true;
        text.fontSizeMin = 20;
        text.fontSizeMax = fontSize;
        return text;
    }

    private void Awake()
    {
        // UI-only scenes have no camera; the existing DontDestroyOnLoad music
        // player needs a listener here. Never create another AudioSource.
        if (FindFirstObjectByType<AudioListener>() == null)
            new GameObject("MultiplayerAudioListener", typeof(AudioListener));
        if (FindFirstObjectByType<EventSystem>() == null)
            new GameObject("MultiplayerEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasObject = new GameObject("MultiplayerCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080, 1920);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = .5f;

        RectTransform bg = CreateRect("OliveBackground", canvasObject.transform);
        Anchor(bg, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        bg.gameObject.AddComponent<Image>().color = Background;

        RectTransform frame = CreateRect("AntiqueGoldFrame", bg);
        Anchor(frame, new Vector2(.07f,.22f), new Vector2(.93f,.82f), Vector2.zero, Vector2.zero);
        frame.gameObject.AddComponent<Image>().color = Gold;

        RectTransform inner = CreateRect("OlivePanel", frame);
        Anchor(inner, Vector2.zero, Vector2.one, new Vector2(5,5), new Vector2(-5,-5));
        inner.gameObject.AddComponent<Image>().color = Olive;

        RectTransform title = CreateRect("Title", inner);
        Anchor(title, new Vector2(.05f,.65f), new Vector2(.95f,.91f), Vector2.zero, Vector2.zero);
        MakeText(title,"MULTIPLAYER",74,Gold);

        RectTransform notice = CreateRect("DevelopmentNotice", inner);
        Anchor(notice,new Vector2(.06f,.29f),new Vector2(.94f,.65f),Vector2.zero,Vector2.zero);
        MakeText(notice,"UNDER DEVELOPMENT\n\nThis feature is not available yet.\nFor now, enjoy the local board game with your friends!",43,Cream);

        RectTransform button = CreateRect("BackButton", inner);
        Anchor(button,new Vector2(.23f,.09f),new Vector2(.77f,.24f),Vector2.zero,Vector2.zero);
        button.gameObject.AddComponent<Image>().color = Background;
        button.gameObject.AddComponent<Button>().onClick.AddListener(
            () => SceneManager.LoadScene("MainMenu"));
        MakeText(button,"‹  BACK TO MENU",42,Gold);
    }
}
