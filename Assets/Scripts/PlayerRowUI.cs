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
        Color color)
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
        ApplyColor();
    }

    private void SetupNameInput()
    {
        if (playerNameInput != null ||
            playerNameText == null)
        {
            return;
        }

        RectTransform originalRect =
            playerNameText.rectTransform;

        Vector2 anchorMin = originalRect.anchorMin;
        Vector2 anchorMax = originalRect.anchorMax;
        Vector2 anchoredPosition = originalRect.anchoredPosition;
        Vector2 sizeDelta = originalRect.sizeDelta;
        Vector2 pivot = originalRect.pivot;

        GameObject inputObject =
            new GameObject(
                "PlayerNameInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField)
            );

        // Configure the hierarchy before enabling TMP_InputField so it
        // never runs with missing text/viewport references.
        inputObject.SetActive(false);
        inputObject.transform.SetParent(
            transform,
            false
        );

        RectTransform inputRect =
            inputObject.GetComponent<RectTransform>();

        inputRect.anchorMin = anchorMin;
        inputRect.anchorMax = anchorMax;
        inputRect.anchoredPosition = anchoredPosition;
        inputRect.sizeDelta = sizeDelta;
        inputRect.pivot = pivot;

        Image background =
            inputObject.GetComponent<Image>();

        background.color =
            new Color(0.025f, 0.045f, 0.075f, 0.92f);

        background.raycastTarget = true;

        GameObject viewportObject =
            new GameObject(
                "Text Area",
                typeof(RectTransform),
                typeof(RectMask2D)
            );

        viewportObject.transform.SetParent(
            inputObject.transform,
            false
        );

        RectTransform viewport =
            viewportObject.GetComponent<RectTransform>();

        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.offsetMin = new Vector2(8f, 2f);
        viewport.offsetMax = new Vector2(-8f, -2f);

        TMP_Text placeholder =
            CreateInputText(
                viewport,
                "Enter player name",
                new Color(0.65f, 0.70f, 0.78f, 0.85f)
            );

        placeholder.fontStyle = FontStyles.Italic;
        placeholder.gameObject.name = "Placeholder";

        playerNameText.transform.SetParent(
            viewport,
            false
        );

        RectTransform textRect =
            playerNameText.rectTransform;

        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        // The placeholder must appear behind the input text.
        placeholder.transform.SetAsFirstSibling();
        playerNameText.transform.SetAsLastSibling();
        playerNameText.alignment = TextAlignmentOptions.Left;
        playerNameText.enableWordWrapping = false;
        playerNameText.overflowMode = TextOverflowModes.Ellipsis;
        playerNameText.raycastTarget = false;

        playerNameInput =
            inputObject.GetComponent<TMP_InputField>();

        playerNameInput.targetGraphic = background;
        playerNameInput.textViewport = viewport;
        playerNameInput.textComponent = playerNameText;
        playerNameInput.placeholder = placeholder;
        playerNameInput.contentType =
            TMP_InputField.ContentType.Standard;
        playerNameInput.lineType =
            TMP_InputField.LineType.SingleLine;
        playerNameInput.characterLimit = 16;
        playerNameInput.selectionColor =
            new Color(0.2f, 0.45f, 0.85f, 0.45f);

        playerNameInput.SetTextWithoutNotify(
            $"Player {playerNumber}"
        );

        inputObject.SetActive(true);
    }

    private TMP_Text CreateInputText(
        Transform parent,
        string value,
        Color color)
    {
        GameObject textObject =
            new GameObject(
                "InputText",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(TextMeshProUGUI)
            );

        textObject.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        text.text = value;
        text.font = playerNameText.font;
        text.fontSize = playerNameText.fontSize;
        text.fontStyle = playerNameText.fontStyle;
        text.color = color;
        text.alignment = TextAlignmentOptions.Left;
        text.enableWordWrapping = false;
        text.raycastTarget = false;

        return text;
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