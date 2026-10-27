using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CurrentPlayerHUD : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text balanceText;
    [SerializeField] private Image colorIndicator;

    private static Sprite circleIndicatorSprite;
    private BoardPlayer currentPlayer;

    private string lastName;
    private int lastBalance;
    private Color lastColor;
    private bool lastBankrupt;

    // ============================================================
    // SET PLAYER
    // ============================================================

    public void SetPlayer(BoardPlayer player)
    {
        currentPlayer = player;

        ForceRefresh();
    }

    // ============================================================
    // REFRESH
    // ============================================================

    public void Refresh()
    {
        if (currentPlayer == null)
        {
            Clear();
            return;
        }

        string playerName =
            currentPlayer.PlayerName;

        int balance =
            currentPlayer.Money;

        Color playerColor =
            currentPlayer.TokenColor;

        bool bankrupt =
            currentPlayer.IsBankrupt;

        bool changed =
            playerName != lastName ||
            balance != lastBalance ||
            !ApproximatelySameColor(
                playerColor,
                lastColor
            ) ||
            bankrupt != lastBankrupt;

        if (!changed)
            return;

        if (playerNameText != null)
        {
            playerNameText.text =
                bankrupt
                    ? $"{playerName}\nBANKRUPT"
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
            EnsureCircularIndicator();
            colorIndicator.color =
                playerColor;

            colorIndicator.enabled =
                true;
        }

        lastName = playerName;
        lastBalance = balance;
        lastColor = playerColor;
        lastBankrupt = bankrupt;
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
    // CLEAR
    // ============================================================

    private void Clear()
    {
        currentPlayer = null;

        if (playerNameText != null)
            playerNameText.text = "WAITING...";

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