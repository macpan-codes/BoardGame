using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChanceCardTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CardManager cardManager;

    [Header("Keyboard")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F8;

    [Header("Window")]
    [SerializeField] private bool openOnStart = true;
    [SerializeField] private Vector2 windowSize = new Vector2(900f, 800f);

    [Header("Colors")]
    [SerializeField] private Color overlayColor = new Color(0f, 0f, 0f, 0.68f);
    [SerializeField] private Color windowColor = new Color(0.055f, 0.075f, 0.11f, 0.98f);
    [SerializeField] private Color headerColor = new Color(0.09f, 0.12f, 0.18f, 1f);
    [SerializeField] private Color rowColor = new Color(0.10f, 0.13f, 0.19f, 1f);
    [SerializeField] private Color rowAltColor = new Color(0.075f, 0.10f, 0.15f, 1f);
    [SerializeField] private Color previewColor = new Color(0.12f, 0.34f, 0.58f, 1f);
    [SerializeField] private Color testColor = new Color(0.12f, 0.52f, 0.30f, 1f);
    [SerializeField] private Color closeColor = new Color(0.35f, 0.14f, 0.18f, 1f);
    [SerializeField] private Color whiteText = new Color(0.94f, 0.97f, 1f, 1f);
    [SerializeField] private Color mutedText = new Color(0.68f, 0.73f, 0.82f, 1f);

    private Canvas canvas;
    private GameObject overlay;
    private GameObject window;
    private Transform contentRoot;
    private TMP_Text statusText;
    private TMP_Text cardCountText;

    private readonly List<GameObject> rows = new List<GameObject>();

    private void Awake()
    {
        if (cardManager == null)
            cardManager = FindFirstObjectByType<CardManager>();
    }

    private void Start()
    {
        BuildTesterUI();
        RefreshCards();
        SetVisible(openOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            SetVisible(!IsVisible());
    }

    public void Toggle()
    {
        SetVisible(!IsVisible());
    }

    public void RefreshCards()
    {
        if (contentRoot == null || cardManager == null)
        {
            SetStatus("CardManager not found. Assign it in the Inspector.");
            return;
        }

        foreach (GameObject row in rows)
        {
            if (row != null)
                Destroy(row);
        }
        rows.Clear();

        int count = cardManager.ChanceCardCount;

        if (cardCountText != null)
            cardCountText.text = $"{count} CARDS";

        for (int i = 0; i < count; i++)
        {
            if (!cardManager.TryGetChanceCardPreview(i, out CardManager.CardPreview preview))
                continue;

            CreateRow(i, preview);
        }

        SetStatus("Preview shows the real ChanceCardUI. Test executes the real CardManager effect.");
    }

    private void CreateRow(int index, CardManager.CardPreview preview)
    {
        GameObject row = new GameObject(
            $"ChanceCard_{index + 1}",
            typeof(RectTransform),
            typeof(Image)
        );

        row.transform.SetParent(contentRoot, false);
        rows.Add(row);

        RectTransform rect = row.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(0f, 82f);

        Image background = row.GetComponent<Image>();
        background.color = index % 2 == 0 ? rowColor : rowAltColor;
        background.raycastTarget = true;

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(14, 14, 10, 10);
        layout.spacing = 12f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        TMP_Text number = CreateText(row.transform, $"#{index + 1}", 22f, TextAlignmentOptions.Center, whiteText);
        LayoutElement numberLayout = number.gameObject.AddComponent<LayoutElement>();
        numberLayout.minWidth = 55f;
        numberLayout.preferredWidth = 55f;

        GameObject titleHolder = new GameObject("TitleHolder", typeof(RectTransform));
        titleHolder.transform.SetParent(row.transform, false);
        LayoutElement titleHolderLayout = titleHolder.AddComponent<LayoutElement>();
        titleHolderLayout.flexibleWidth = 1f;
        titleHolderLayout.minWidth = 220f;

        VerticalLayoutGroup titleLayout = titleHolder.AddComponent<VerticalLayoutGroup>();
        titleLayout.childAlignment = TextAnchor.MiddleLeft;
        titleLayout.spacing = 2f;
        titleLayout.childControlWidth = true;
        titleLayout.childControlHeight = true;
        titleLayout.childForceExpandWidth = true;
        titleLayout.childForceExpandHeight = false;

        TMP_Text title = CreateText(titleHolder.transform, preview.title, 20f, TextAlignmentOptions.Left, whiteText);
        LayoutElement titleElement = title.gameObject.AddComponent<LayoutElement>();
        titleElement.preferredHeight = 28f;

        TMP_Text description = CreateText(titleHolder.transform, preview.description, 13f, TextAlignmentOptions.Left, mutedText);
        description.maxVisibleLines = 2;
        description.overflowMode = TextOverflowModes.Ellipsis;
        LayoutElement descriptionElement = description.gameObject.AddComponent<LayoutElement>();
        descriptionElement.preferredHeight = 32f;

        Button previewButton = CreateButton(row.transform, "PREVIEW", 125f, 50f, previewColor);
        previewButton.onClick.AddListener(() => Preview(index));

        Button testButton = CreateButton(row.transform, "TEST", 105f, 50f, testColor);
        testButton.onClick.AddListener(() => Test(index));
    }

    private TMP_Text CreateText(
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Color textColor)
    {
        GameObject obj = new GameObject("Text", typeof(RectTransform));
        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value ?? string.Empty;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = textColor;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        return text;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        float width,
        float height,
        Color normalColor)
    {
        GameObject obj = new GameObject(
            label + "Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );

        obj.transform.SetParent(parent, false);

        LayoutElement layout = obj.AddComponent<LayoutElement>();
        layout.minWidth = width;
        layout.preferredWidth = width;
        layout.minHeight = height;
        layout.preferredHeight = height;

        Image image = obj.GetComponent<Image>();
        image.color = normalColor;
        image.raycastTarget = true;

        Button button = obj.GetComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = normalColor;
        colors.highlightedColor = LerpColor(normalColor, Color.white, 0.20f);
        colors.pressedColor = LerpColor(normalColor, Color.black, 0.20f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(normalColor.r, normalColor.g, normalColor.b, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        TMP_Text text = CreateText(obj.transform, label, 16f, TextAlignmentOptions.Center, Color.white);
        text.fontStyle = FontStyles.Bold;

        return button;
    }

    private Color LerpColor(Color a, Color b, float t)
    {
        return Color.Lerp(a, b, Mathf.Clamp01(t));
    }

    private void BuildTesterUI()
    {
        canvas = CreateCanvas();

        overlay = new GameObject(
            "ChanceCardTesterOverlay",
            typeof(RectTransform),
            typeof(Image)
        );
        overlay.transform.SetParent(canvas.transform, false);

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.anchorMin = Vector2.zero;
        overlayRect.anchorMax = Vector2.one;
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;

        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = overlayColor;

        window = new GameObject(
            "ChanceCardTesterWindow",
            typeof(RectTransform),
            typeof(Image)
        );
        window.transform.SetParent(canvas.transform, false);

        RectTransform windowRect = window.GetComponent<RectTransform>();
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.sizeDelta = windowSize;

        Image windowImage = window.GetComponent<Image>();
        windowImage.color = windowColor;

        VerticalLayoutGroup windowLayout = window.AddComponent<VerticalLayoutGroup>();
        windowLayout.padding = new RectOffset(20, 20, 20, 20);
        windowLayout.spacing = 10f;
        windowLayout.childAlignment = TextAnchor.UpperCenter;
        windowLayout.childControlWidth = true;
        windowLayout.childControlHeight = true;
        windowLayout.childForceExpandWidth = true;
        windowLayout.childForceExpandHeight = false;

        GameObject header = new GameObject(
            "Header",
            typeof(RectTransform),
            typeof(Image)
        );
        header.transform.SetParent(window.transform, false);
        LayoutElement headerLayout = header.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 82f;

        Image headerImage = header.GetComponent<Image>();
        headerImage.color = headerColor;

        VerticalLayoutGroup headerGroup = header.AddComponent<VerticalLayoutGroup>();
        headerGroup.padding = new RectOffset(16, 16, 10, 10);
        headerGroup.spacing = 2f;
        headerGroup.childAlignment = TextAnchor.MiddleLeft;
        headerGroup.childControlWidth = true;
        headerGroup.childControlHeight = true;
        headerGroup.childForceExpandWidth = true;
        headerGroup.childForceExpandHeight = false;

        TMP_Text title = CreateText(header.transform, "CHANCE CARD TESTER", 30f, TextAlignmentOptions.Left, whiteText);
        title.fontStyle = FontStyles.Bold;
        LayoutElement titleLayout = title.gameObject.AddComponent<LayoutElement>();
        titleLayout.preferredHeight = 38f;

        TMP_Text subtitle = CreateText(
            header.transform,
            "Use the REAL ChanceCardUI and REAL CardManager logic to preview or execute any card.",
            13f,
            TextAlignmentOptions.Left,
            mutedText
        );
        LayoutElement subtitleLayout = subtitle.gameObject.AddComponent<LayoutElement>();
        subtitleLayout.preferredHeight = 24f;

        cardCountText = CreateText(header.transform, "0 CARDS", 12f, TextAlignmentOptions.Right, mutedText);
        LayoutElement countLayout = cardCountText.gameObject.AddComponent<LayoutElement>();
        countLayout.preferredHeight = 18f;

        ScrollRect scroll = CreateScrollArea(window.transform);

        statusText = CreateText(
            window.transform,
            "",
            13f,
            TextAlignmentOptions.Left,
            mutedText
        );
        LayoutElement statusLayout = statusText.gameObject.AddComponent<LayoutElement>();
        statusLayout.minHeight = 40f;
        statusLayout.preferredHeight = 40f;

        Button close = CreateButton(
            window.transform,
            "CLOSE TESTER  •  F8",
            240f,
            48f,
            closeColor
        );
        close.onClick.AddListener(() => SetVisible(false));
    }

    private ScrollRect CreateScrollArea(Transform parent)
    {
        GameObject viewport = new GameObject(
            "Viewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Mask)
        );
        viewport.transform.SetParent(parent, false);

        LayoutElement viewportLayout = viewport.AddComponent<LayoutElement>();
        viewportLayout.flexibleHeight = 1f;
        viewportLayout.minHeight = 200f;

        Image viewportImage = viewport.GetComponent<Image>();
        viewportImage.color = new Color(0.02f, 0.03f, 0.05f, 0.85f);

        Mask mask = viewport.GetComponent<Mask>();
        mask.showMaskGraphic = true;

        GameObject content = new GameObject(
            "Content",
            typeof(RectTransform),
            typeof(VerticalLayoutGroup),
            typeof(ContentSizeFitter)
        );
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = content.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.offsetMin = Vector2.zero;
        contentRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup contentLayout = content.GetComponent<VerticalLayoutGroup>();
        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(4, 4, 4, 4);
        contentLayout.childAlignment = TextAnchor.UpperCenter;
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.viewport = viewport.GetComponent<RectTransform>();
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 45f;
        scroll.inertia = true;

        contentRoot = content.transform;
        return scroll;
    }

    private Canvas CreateCanvas()
    {
        GameObject canvasObject = new GameObject(
            "ChanceCardTesterCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas createdCanvas = canvasObject.GetComponent<Canvas>();
        createdCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        createdCanvas.sortingOrder = 500;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        return createdCanvas;
    }

    private void Preview(int index)
    {
        if (cardManager == null)
        {
            SetStatus("CardManager not found.");
            return;
        }

        SetVisible(false);
        cardManager.PreviewChanceCard(index);
        SetStatus($"Previewing card #{index + 1}.");
    }

    private void Test(int index)
    {
        if (cardManager == null)
        {
            SetStatus("CardManager not found.");
            return;
        }

        SetVisible(false);

        bool started = cardManager.TestChanceCard(index);

        SetStatus(
            started
                ? $"Testing card #{index + 1} through the real CardManager."
                : $"Could not start test for card #{index + 1}. Check the Unity Console."
        );
    }

    private bool IsVisible()
    {
        return window != null && window.activeSelf;
    }

    private void SetVisible(bool visible)
    {
        if (overlay != null)
            overlay.SetActive(visible);

        if (window != null)
            window.SetActive(visible);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message;
    }
}
