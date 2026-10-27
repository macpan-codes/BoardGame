using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerBalanceRowUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image colorIndicator;
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text balanceText;

    private static Sprite circleIndicatorSprite;

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
            EnsureCircularIndicator();
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

    private void EnsureCircularIndicator()
    {
        if (colorIndicator == null)
            return;

        if (circleIndicatorSprite == null)
        {
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "HUDPlayerColorCircle";
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
            circleIndicatorSprite = Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }

        colorIndicator.sprite = circleIndicatorSprite;
        colorIndicator.type = Image.Type.Simple;
        colorIndicator.preserveAspect = true;
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