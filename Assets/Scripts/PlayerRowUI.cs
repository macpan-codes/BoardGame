using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Dropdown playerTypeDropdown;
    [SerializeField] private Button playerColorButton;

    private int playerNumber;
    private Color playerColor = Color.white;

    public void Setup(int number, Color color)
    {
        playerNumber = number;
        playerColor = color;

        if (playerNameText != null)
        {
            playerNameText.text =
                $"PLAYER {number}";
        }

        if (playerTypeDropdown != null)
        {
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

        ApplyColor();
    }

    public string GetPlayerName()
    {
        return $"Player {playerNumber}";
    }

    public bool IsBot()
    {
        return playerTypeDropdown != null &&
               playerTypeDropdown.value == 1;
    }

    public Color GetColor()
    {
        return playerColor;
    }

    public void SetColor(Color color)
    {
        playerColor = color;
        ApplyColor();
    }

    private void ApplyColor()
    {
        if (playerColorButton == null)
            return;

        Image image =
            playerColorButton.GetComponent<Image>();

        if (image != null)
        {
            image.color = playerColor;
        }
    }
}   