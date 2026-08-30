using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBalanceRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image colorIndicator;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text balanceText;

    private BoardPlayer player;

    private string lastName;
    private int lastBalance;
    private Color lastColor;
    private bool lastBankrupt;

    public BoardPlayer Player =>
        player;

    // ============================================================
    // SETUP
    // ============================================================

    public void Setup(BoardPlayer boardPlayer)
    {
        player = boardPlayer;

        ForceRefresh();
    }

    // ============================================================
    // REFRESH
    // ============================================================

    public void Refresh()
    {
        if (player == null)
        {
            Clear();
            return;
        }

        string currentName =
            player.PlayerName;

        int currentBalance =
            player.Money;

        Color currentColor =
            player.TokenColor;

        bool currentBankrupt =
            player.IsBankrupt;

        bool changed =
            currentName != lastName ||
            currentBalance != lastBalance ||
            !ApproximatelySameColor(
                currentColor,
                lastColor
            ) ||
            currentBankrupt != lastBankrupt;

        if (!changed)
            return;

        UpdateVisuals(
            currentName,
            currentBalance,
            currentColor,
            currentBankrupt
        );
    }

    // ============================================================
    // FORCE REFRESH
    // ============================================================

    private void ForceRefresh()
    {
        lastName = null;
        lastBalance = int.MinValue;
        lastColor = Color.clear;
        lastBankrupt = false;

        Refresh();
    }

    // ============================================================
    // VISUALS
    // ============================================================

    private void UpdateVisuals(
        string playerName,
        int balance,
        Color playerColor,
        bool bankrupt)
    {
        if (playerNameText != null)
        {
            playerNameText.text =
                bankrupt
                    ? $"{playerName}  •  BANKRUPT"
                    : playerName;
        }

        if (balanceText != null)
        {
            balanceText.text =
                bankrupt
                    ? "$0"
                    : $"${balance:N0}M";
        }

        if (colorIndicator != null)
        {
            colorIndicator.color =
                playerColor;
        }

        lastName = playerName;
        lastBalance = balance;
        lastColor = playerColor;
        lastBankrupt = bankrupt;
    }

    // ============================================================
    // CLEAR
    // ============================================================

    private void Clear()
    {
        player = null;

        if (playerNameText != null)
            playerNameText.text = string.Empty;

        if (balanceText != null)
            balanceText.text = string.Empty;

        if (colorIndicator != null)
            colorIndicator.enabled = false;
    }

    // ============================================================
    // COLOR COMPARISON
    // ============================================================

    private bool ApproximatelySameColor(
        Color a,
        Color b)
    {
        return Mathf.Abs(a.r - b.r) < 0.001f &&
               Mathf.Abs(a.g - b.g) < 0.001f &&
               Mathf.Abs(a.b - b.b) < 0.001f &&
               Mathf.Abs(a.a - b.a) < 0.001f;
    }
}