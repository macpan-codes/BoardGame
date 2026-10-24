using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField]
    private TMP_Text playerNameText;

    [SerializeField]
    private TMP_Dropdown playerTypeDropdown;

    [SerializeField]
    private Button playerColorButton;

    [SerializeField]
    private Image playerColorImage;

    [Header("Visual")]
    [SerializeField]
    private float colorButtonAlpha = 1f;

    [Header("Sizing Controls (adjust in Inspector)")]
    [SerializeField, Min(120f)] private float nameFieldWidth = 190f;
    [SerializeField, Min(32f)] private float nameFieldHeight = 42f;
    [SerializeField, Range(18f, 32f)] private float nameFontSize = 24f;
    [SerializeField, Min(24f)] private float colorSwatchSize = 34f;
    [SerializeField, Min(80f)] private float playerTypeWidth = 112f;
    [SerializeField, Min(32f)] private float playerTypeHeight = 40f;
    [SerializeField, Range(14f, 24f)] private float playerTypeFontSize = 18f;

    private int playerNumber;
    private TMP_InputField playerNameInput;

    private Color playerColor =
        Color.white;

    // ============================================================
    // SETUP
    // ============================================================

    public void Setup(
        int number,
        Color color,
        string initialName = null,
        bool initialBot = false)
    {
        playerNumber =
            Mathf.Max(
                1,
                number
            );

        playerColor =
            color;

        SetupNameInput();
        UpdateName();
        SetupDropdown();

        if (playerNameInput != null)
        {
            string nameToUse =
                string.IsNullOrWhiteSpace(initialName)
                    ? $"Player {playerNumber}"
                    : initialName.Trim();

            playerNameInput.SetTextWithoutNotify(nameToUse);
        }

        if (playerTypeDropdown != null)
        {
            playerTypeDropdown.SetValueWithoutNotify(
                initialBot ? 1 : 0
            );
            playerTypeDropdown.RefreshShownValue();
        }

        ApplyColor();
    }

    private void SetupNameInput()
    {
        if (playerNameInput != null ||
            playerNameText == null)
        {
            return;
        }

        // Preserve the original prefab label's sizing and font, but use a
        // fresh TMP text component for the input. This avoids the old label's
        // layout/state from interfering with live typing and caret rendering.
        TMP_Text originalText = playerNameText;
        RectTransform originalRect = originalText.rectTransform;

        Vector2 anchorMin = originalRect.anchorMin;
        Vector2 anchorMax = originalRect.anchorMax;
        Vector2 anchoredPosition = originalRect.anchoredPosition;
        Vector2 sizeDelta = originalRect.sizeDelta;
        Vector2 pivot = originalRect.pivot;

        GameObject inputObject = new GameObject(
            "PlayerNameInput",
            typeof(RectTransform),
            typeof(Image)
        );

        inputObject.SetActive(false);
        inputObject.transform.SetParent(transform, false);

        RectTransform inputRect = inputObject.GetComponent<RectTransform>();
        inputRect.anchorMin = anchorMin;
        inputRect.anchorMax = anchorMax;
        inputRect.anchoredPosition = anchoredPosition;
        inputRect.sizeDelta = new Vector2(
            Mathf.Max(sizeDelta.x, nameFieldWidth),
            Mathf.Max(sizeDelta.y, nameFieldHeight)
        );
        inputRect.pivot = pivot;

        Image background = inputObject.GetComponent<Image>();
        Image rowBackground = GetComponent<Image>();

        if (rowBackground != null && rowBackground.sprite != null)
        {
            background.sprite = rowBackground.sprite;
            background.type = rowBackground.type;
        }
        else
        {
            background.sprite = null;
            background.type = Image.Type.Simple;
        }

        // Navy is intentionally lighter than the row panel so the editable
        // field remains visibly distinct instead of looking pitch black.
        background.color = new Color(0.025f, 0.055f, 0.10f, 0.55f);
        background.sprite = GetRoundedUISprite();
        background.type = Image.Type.Sliced;
        background.raycastTarget = true;

        Outline outline = inputObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.62f, 0.48f, 0.25f, 0.72f);
        outline.effectDistance = new Vector2(1.25f, 1.25f);
        outline.useGraphicAlpha = false;

        GameObject viewportObject = new GameObject(
            "Text Area",
            typeof(RectTransform),
            typeof(RectMask2D)
        );
        viewportObject.transform.SetParent(inputObject.transform, false);

        RectTransform viewport = viewportObject.GetComponent<RectTransform>();
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(10f, 2f);
        viewport.offsetMax = new Vector2(-10f, -2f);

        TMP_Text inputText = CreateInputText(
            viewport,
            $"Player {playerNumber}",
            nameFontSize,
            Color.white
        );
        inputText.gameObject.name = "Input Text";
        inputText.fontStyle = FontStyles.Bold;
        inputText.alignment = TextAlignmentOptions.MidlineLeft;
        inputText.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text placeholder = CreateInputText(
            viewport,
            "Enter player name",
            Mathf.Min(nameFontSize - 2f, 22f),
            new Color(0.68f, 0.75f, 0.88f, 1f)
        );
        placeholder.gameObject.name = "Placeholder";
        placeholder.fontStyle = FontStyles.Italic;

        // The prefab label is no longer rendered; the fresh text object is
        // now the one TMP_InputField updates on every typed character.
        originalText.gameObject.SetActive(false);
        playerNameText = inputText;

        TMP_InputField inputField =
            inputObject.AddComponent<TMP_InputField>();

        inputField.targetGraphic = background;
        inputField.transition = Selectable.Transition.None;
        inputField.textViewport = viewport;
        inputField.textComponent = inputText;
        inputField.placeholder = placeholder;
        inputField.contentType = TMP_InputField.ContentType.Standard;
        inputField.lineType = TMP_InputField.LineType.SingleLine;
        inputField.characterLimit = 16;
        inputField.readOnly = false;
        inputField.interactable = true;
        inputField.onFocusSelectAll = true;
        inputField.customCaretColor = true;
        inputField.caretColor = Color.white;
        inputField.selectionColor = new Color(0.25f, 0.48f, 0.88f, 0.55f);

        inputField.SetTextWithoutNotify($"Player {playerNumber}");

        inputField.onSelect.AddListener(
            _ => SetInputFocus(outline, true)
        );
        inputField.onDeselect.AddListener(
            _ => SetInputFocus(outline, false)
        );

        // Set explicit sibling ordering so the placeholder is behind the
        // editable text and both remain inside the clipping viewport.
        placeholder.transform.SetAsFirstSibling();
        inputText.transform.SetAsLastSibling();

        playerNameInput = inputField;
        inputObject.SetActive(true);
    }

    private TMP_Text CreateInputText(
        Transform parent,
        string value,
        float fontSize,
        Color color)
    {
        GameObject textObject = new GameObject(
            "InputText",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = playerNameText.font;
        text.fontSize = Mathf.Clamp(fontSize, 18f, 32f);
        text.fontStyle = FontStyles.Normal;
        text.color = color;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableAutoSizing = false;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        return text;
    }

    private void SetInputFocus(Outline outline, bool focused)
    {
        if (outline == null)
            return;

        outline.effectColor = focused
            ? new Color(1f, 0.78f, 0.24f, 1f)
            : new Color(0.24f, 0.39f, 0.62f, 0.95f);
    }

    // ============================================================
    // NAME
    // ============================================================

    private void UpdateName()
    {
        if (playerNameText == null)
            return;

        // Preserve a readable visual label when input fields are unavailable.
        if (playerNameInput == null)
        {
            playerNameText.text =
                $"PLAYER {playerNumber}";
        }
    }

    public string GetPlayerName()
    {
        if (playerNameInput == null)
            return $"Player {playerNumber}";

        string enteredName =
            playerNameInput.text?.Trim();

        return string.IsNullOrWhiteSpace(enteredName)
            ? $"Player {playerNumber}"
            : enteredName;
    }

    // ============================================================
    // TYPE
    // ============================================================

    private void SetupDropdown()
    {
        if (playerTypeDropdown == null) return;
        playerTypeDropdown.ClearOptions();
        playerTypeDropdown.AddOptions(new List<string> { "Human", "Bot" });
        playerTypeDropdown.RefreshShownValue();
        RectTransform dropdownRect = playerTypeDropdown.transform as RectTransform;
        if (dropdownRect != null) dropdownRect.sizeDelta = new Vector2(playerTypeWidth, playerTypeHeight);
        Image dropdownImage = playerTypeDropdown.GetComponent<Image>();
        if (dropdownImage != null)
        {
            dropdownImage.sprite = GetRoundedUISprite();
            dropdownImage.type = Image.Type.Sliced;
            dropdownImage.color = new Color(0.035f, 0.075f, 0.13f, 0.98f);
        }
        if (playerTypeDropdown.captionText != null)
        {
            playerTypeDropdown.captionText.fontSize = playerTypeFontSize;
            playerTypeDropdown.captionText.color = new Color(0.97f, 0.94f, 0.86f, 1f);
            playerTypeDropdown.captionText.alignment = TextAlignmentOptions.Center;
            playerTypeDropdown.captionText.fontStyle = FontStyles.Bold;
        }
        if (playerTypeDropdown.template != null)
        {
            Image templateImage = playerTypeDropdown.template.GetComponent<Image>();
            if (templateImage != null)
            {
                templateImage.sprite = GetRoundedUISprite();
                templateImage.type = Image.Type.Sliced;
                templateImage.color = new Color(0.025f, 0.055f, 0.10f, 0.99f);
            }
            Outline outline = playerTypeDropdown.template.GetComponent<Outline>();
            if (outline == null) outline = playerTypeDropdown.template.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0.72f, 0.55f, 0.27f, 0.9f);
            outline.effectDistance = new Vector2(1f, 1f);
            Transform item = playerTypeDropdown.template.Find("Viewport/Content/Item");
            if (item != null)
            {
                TMP_Text itemText = item.Find("Item Label")?.GetComponent<TMP_Text>();
                if (itemText != null)
                {
                    itemText.fontSize = playerTypeFontSize;
                    itemText.color = new Color(0.97f, 0.94f, 0.86f, 1f);
                }
                Image itemImage = item.GetComponent<Image>();
                if (itemImage != null)
                {
                    itemImage.sprite = GetRoundedUISprite();
                    itemImage.type = Image.Type.Sliced;
                    itemImage.color = new Color(0.06f, 0.10f, 0.16f, 0.98f);
                }
            }
        }
    }

    private static Sprite GetRoundedUISprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "RoundedPlayerRowUI";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                const float radius = 13f;
                float cx = Mathf.Clamp(x, radius, size - 1 - radius);
                float cy = Mathf.Clamp(y, radius, size - 1 - radius);
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(radius + 0.5f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f),
            size, 0, SpriteMeshType.FullRect, new Vector4(16f, 16f, 16f, 16f));
    }

    private static Sprite GetCircleSprite()
    {
        const int size = 64;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "PlayerColorCircle";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f));
                float alpha = Mathf.Clamp01(32f - distance);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    public bool IsBot()
    {
        return playerTypeDropdown != null &&
               playerTypeDropdown.value == 1;
    }

    // ============================================================
    // COLOR
    // ============================================================

    public Color GetColor()
    {
        return playerColor;
    }

    public void SetColor(
        Color color)
    {
        playerColor =
            color;

        ApplyColor();
    }

    private void ApplyColor()
    {
        Color displayColor =
            playerColor;

        displayColor.a =
            Mathf.Clamp01(
                colorButtonAlpha
            );

        if (playerColorButton != null)
        {
            Image buttonImage =
                playerColorButton.GetComponent<Image>();

            if (buttonImage != null)
            {
                buttonImage.sprite = GetCircleSprite();
                buttonImage.type = Image.Type.Simple;
                buttonImage.color = displayColor;
                RectTransform colorRect = playerColorButton.transform as RectTransform;
                if (colorRect != null) colorRect.sizeDelta = new Vector2(colorSwatchSize, colorSwatchSize);
            }
        }

        if (playerColorImage != null)
        {
            playerColorImage.color =
                displayColor;
        }
    }
}