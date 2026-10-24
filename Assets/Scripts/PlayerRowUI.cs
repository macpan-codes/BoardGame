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
    [SerializeField, Range(18f, 36f)] private float nameFontSize = 30f;
    [SerializeField, Min(24f)] private float colorSwatchSize = 34f;
    [SerializeField, Min(80f)] private float playerTypeWidth = 112f;
    [SerializeField, Min(32f)] private float playerTypeHeight = 40f;
    [SerializeField, Range(14f, 30f)] private float playerTypeFontSize = 22f;

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
        // Prefer the TMP_InputField authored in PlayerRow.prefab. Never create
        // a second runtime input: the prefab hierarchy remains the source of truth.
        if (playerNameInput == null)
            playerNameInput = GetComponentInChildren<TMP_InputField>(true);

        if (playerNameInput != null)
        {
            playerNameText = playerNameInput.textComponent;
            playerNameInput.interactable = true;
            playerNameInput.readOnly = false;
            playerNameInput.contentType = TMP_InputField.ContentType.Standard;
            playerNameInput.lineType = TMP_InputField.LineType.SingleLine;
            playerNameInput.characterLimit = 16;
            playerNameInput.onFocusSelectAll = true;
            return;
        }

        Debug.LogError(
            "PlayerRowUI: PlayerRow prefab needs one authored TMP_InputField child. " +
            "Add it to the prefab and assign its Text Component; no runtime input will be generated.",
            this
        );
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
        if (playerTypeDropdown == null)
            return;

        // Keep the dropdown's visual design editable in the prefab Inspector.
        // This method only maintains the options required by game setup.
        if (playerTypeDropdown.options.Count != 2 ||
            playerTypeDropdown.options[0].text != "Human" ||
            playerTypeDropdown.options[1].text != "Bot")
        {
            playerTypeDropdown.ClearOptions();
            playerTypeDropdown.AddOptions(new List<string> { "Human", "Bot" });
        }

        playerTypeDropdown.RefreshShownValue();
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