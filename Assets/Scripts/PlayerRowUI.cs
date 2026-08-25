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

        UpdateName();
        SetupDropdown();
        ApplyColor();
    }

    // ============================================================
    // NAME
    // ============================================================

    private void UpdateName()
    {
        if (playerNameText == null)
            return;

        playerNameText.text =
            $"PLAYER {playerNumber}";
    }

    public string GetPlayerName()
    {
        return $"Player {playerNumber}";
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