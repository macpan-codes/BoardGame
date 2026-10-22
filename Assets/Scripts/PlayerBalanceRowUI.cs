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
    private bool lastLeftGame;

    private Color originalPlayerNameColor = Color.white;
    private Color originalBalanceColor = Color.white;
    private bool hasCachedTextColors;

    public BoardPlayer Player =>
        player;

    // ============================================================
    // SETUP
    // ============================================================

    public void Setup(BoardPlayer boardPlayer)
    {
        player = boardPlayer;

        CacheTextColors();
        ForceRefresh();
    }

    private void Awake()
    {
        CacheTextColors();
    }

    private void CacheTextColors()
    {
        if (hasCachedTextColors ||
            (playerNameText == null && balanceText == null))
        {
            return;
        }

        if (playerNameText != null)
            originalPlayerNameColor = playerNameText.color;

        if (balanceText != null)
            originalBalanceColor = balanceText.color;

        hasCachedTextColors = true;
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

        bool currentLeftGame =
            player.HasLeftGame;

        bool changed =
            currentName != lastName ||
            currentBalance != lastBalance ||
            !ApproximatelySameColor(
                currentColor,
                lastColor
            ) ||
            currentBankrupt != lastBankrupt ||
            currentLeftGame != lastLeftGame;

        if (!changed)
            return;

        UpdateVisuals(
            currentName,
            currentBalance,
            currentColor,
            currentBankrupt,
            currentLeftGame
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
        lastLeftGame = false;

        Refresh();
    }

    // ============================================================
    // VISUALS
    // ============================================================

    private void UpdateVisuals(
        string playerName,
        int balance,
        Color playerColor,
        bool bankrupt,
        bool leftGame)
    {
        if (playerNameText != null)
        {
            // Keep the row compact. A left-game status suffix pushes into
            // the balance column, so identify a departed player by dimming
            // their row instead of adding more text.
            playerNameText.text =
                bankrupt
                    ? $"{playerName}  •  BANKRUPT"
                    : playerName;

            playerNameText.color =
                leftGame
                    ? GetMutedColor(originalPlayerNameColor)
                    : originalPlayerNameColor;
        }

        if (balanceText != null)
        {
            balanceText.text =
                leftGame || bankrupt
                    ? "$0"
                    : $"${balance:N0}M";

            balanceText.color =
                leftGame
                    ? GetMutedColor(originalBalanceColor)
                    : originalBalanceColor;
        }

        if (colorIndicator != null)
        {
            colorIndicator.color =
                leftGame
                    ? GetMutedColor(playerColor)
                    : playerColor;
        }

        lastName = playerName;
        lastBalance = balance;
        lastColor = playerColor;
        lastBankrupt = bankrupt;
        lastLeftGame = leftGame;
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

    private Color GetMutedColor(Color original)
    {
        Color muted = Color.Lerp(original, Color.gray, 0.55f);
        muted.a = original.a * 0.55f;
        return muted;
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