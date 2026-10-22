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
            sizeDelta.x,
            Mathf.Max(sizeDelta.y, 34f)
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
        background.color = new Color(0.055f, 0.095f, 0.17f, 1f);
        background.raycastTarget = true;

        Outline outline = inputObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.24f, 0.39f, 0.62f, 0.95f);
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
            originalText.fontSize,
            Color.white
        );
        inputText.gameObject.name = "Input Text";
        inputText.fontStyle = FontStyles.Bold;
        inputText.alignment = TextAlignmentOptions.MidlineLeft;
        inputText.overflowMode = TextOverflowModes.Ellipsis;

        TMP_Text placeholder = CreateInputText(
            viewport,
            "Enter player name",
            Mathf.Min(originalText.fontSize, 20f),
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
        text.fontSize = Mathf.Clamp(fontSize, 18f, 24f);
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

        playerTypeDropdown.ClearOptions();

        playerTypeDropdown.AddOptions(
            new List<string>
            {
                "Human",
                "Bot"
            }
        );

        playerTypeDropdown.value = 0;

        playerTypeDropdown.RefreshShownValue();
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
                buttonImage.color =
                    displayColor;
            }
        }

        if (playerColorImage != null)
        {
            playerColorImage.color =
                displayColor;
        }
    }
}