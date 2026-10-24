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
                // Preserve the prefab-authored swatch size so it stays editable in Unity.
            }
        }

        if (playerColorImage != null)
        {
            playerColorImage.color =
                displayColor;
        }
    }
}