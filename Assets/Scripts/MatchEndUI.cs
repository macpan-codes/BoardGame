using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Runtime match controls and final results screen.
/// Created by GameManager so no scene hierarchy setup is required.
/// </summary>
public sealed class MatchEndUI : MonoBehaviour
{
    private sealed class PlayerResult
    {
        public BoardPlayer Player;
        public string Status;
        public long Cash;
        public int PropertyCount;
        public long PropertyValue;
        public long BuildingValue;
        public long MortgageLiability;
        public long NetWorth;
    }

    private GameManager gameManager;
    private Canvas canvas;
    private GameObject overlay;
    private GameObject modal;
    private Button menuButton;
    private readonly HashSet<BoardPlayer> yesVotes = new HashSet<BoardPlayer>();

    private static readonly Color Navy = new Color(0.035f, 0.055f, 0.10f, 0.98f);
    private static readonly Color Gold = new Color(0.96f, 0.74f, 0.30f, 1f);
    private static readonly Color PanelColor = new Color(0.075f, 0.10f, 0.16f, 0.99f);
    private static readonly Color ButtonColor = new Color(0.16f, 0.22f, 0.32f, 1f);
    private static readonly Color Green = new Color(0.14f, 0.42f, 0.31f, 1f);
    private static readonly Color Red = new Color(0.55f, 0.20f, 0.20f, 1f);
    private static readonly Color White = new Color(0.95f, 0.96f, 0.98f, 1f);
    private static readonly Color Muted = new Color(0.69f, 0.73f, 0.80f, 1f);

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        BuildCanvas();
    }

    public void ShowEndGameMenu()
    {
        if (gameManager == null || gameManager.GameOver)
            return;

        ShowOverlay();
        BuildModal("GAME MENU", new Vector2(620f, 470f));

        AddText(modal.transform, "MenuSubtitle",
            "Choose an action for this match.",
            24, Muted, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -105f), new Vector2(0f, 45f));

        AddButton(modal.transform, "VOTE TO END MATCH",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -185f),
            new Vector2(430f, 58f), Green, BeginVote);

        AddButton(modal.transform, "LEAVE GAME",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -260f),
            new Vector2(430f, 58f), Red, ShowLeavePlayerSelection);

        AddButton(modal.transform, "CANCEL",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 28f),
            new Vector2(200f, 48f), ButtonColor, HideOverlay);
    }

    public void ShowFinalResults(
        GameManager manager,
        BoardPlayer naturalWinner,
        string reason)
    {
        if (manager != null)
            gameManager = manager;

        if (gameManager == null)
            return;

        PlayerResult[] results = CalculateResults(gameManager);
        if (results.Length == 0)
            return;

        BoardPlayer winner = naturalWinner;
        if (winner == null)
        {
            PlayerResult highestActive = results
                .Where(r => r.Player != null && r.Player.IsActive)
                .OrderByDescending(r => r.NetWorth)
                .ThenBy(r => r.Player.PlayerNumber)
                .FirstOrDefault();

            winner = highestActive != null ? highestActive.Player : null;
        }

        ShowOverlay();
        BuildModal("MATCH COMPLETE", new Vector2(1540f, 960f));

        AddText(modal.transform, "WinnerStars",
            "*     *     *",
            30, Gold, TextAlignmentOptions.Center,
            new Vector2(0.1f, 1f), new Vector2(0.9f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(0f, 38f));

        AddText(modal.transform, "WinnerName",
            winner != null ? winner.PlayerName.ToUpperInvariant() + " WINS" : "FINAL RESULTS",
            40, Gold, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(0f, 54f));

        AddText(modal.transform, "ResultReason",
            string.IsNullOrWhiteSpace(reason) ? "FINAL PLAYER STANDING" : reason.ToUpperInvariant(),
            19, Muted, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -132f), new Vector2(0f, 32f));

        AddText(modal.transform, "ReportHeader",
            "FINAL FINANCIAL REPORT",
            23, White, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(0f, 34f));

        for (int i = 0; i < results.Length; i++)
        {
            PlayerResult result = results[i];
            float y = -235f - i * 94f;
            bool isWinner = result.Player == winner;
            Color rowColor = isWinner
                ? new Color(0.22f, 0.20f, 0.12f, 1f)
                : new Color(0.10f, 0.13f, 0.19f, 1f);

            GameObject row = CreateImage(
                "ResultRow_" + (i + 1),
                modal.transform,
                rowColor,
                new Vector2(0.035f, 1f),
                new Vector2(0.965f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, y),
                new Vector2(0f, 82f)
            );

            string status = isWinner
                ? "WINNER"
                : result.Status;

            string lineOne =
                "#" + (i + 1) + "   " + result.Player.PlayerName +
                "     [" + status + "]";

            string lineTwo =
                "CASH " + FormatMoney(result.Cash) +
                "    |    PROPERTIES " + result.PropertyCount + " / " + FormatMoney(result.PropertyValue) +
                "    |    BUILDINGS " + FormatMoney(result.BuildingValue) +
                "    |    MORTGAGE -" + FormatMoney(result.MortgageLiability) +
                "    |    NET WORTH " + FormatMoney(result.NetWorth);

            AddText(row.transform, "PlayerSummary", lineOne + "\n" + lineTwo,
                17, isWinner ? Gold : White, TextAlignmentOptions.Left,
                new Vector2(0.025f, 0.08f), new Vector2(0.98f, 0.92f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        }

        AddText(modal.transform, "ValuationNote",
            "Net worth = cash + property value + building cost - mortgage payoff (110% of mortgage value).",
            15, Muted, TextAlignmentOptions.Center,
            new Vector2(0.04f, 0f), new Vector2(0.96f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 62f), new Vector2(0f, 28f));

        AddButton(modal.transform, "CLOSE REPORT",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 14f),
            new Vector2(220f, 44f), ButtonColor, HideOverlay);

        if (menuButton != null)
            menuButton.gameObject.SetActive(false);
    }

    private void ShowLeavePlayerSelection()
    {
        BoardPlayer[] activePlayers = gameManager.GetActivePlayers();
        if (activePlayers.Length == 0)
            return;

        ShowOverlay();
        BuildModal("LEAVE GAME", new Vector2(650f, Mathf.Clamp(190f + activePlayers.Length * 72f, 450f, 740f)));

        AddText(modal.transform, "LeaveInstruction",
            "Select the player who is leaving. Their properties return to the bank.",
            19, Muted, TextAlignmentOptions.Center,
            new Vector2(0.06f, 1f), new Vector2(0.94f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, 50f));

        for (int i = 0; i < activePlayers.Length; i++)
        {
            BoardPlayer player = activePlayers[i];
            AddButton(modal.transform, player.PlayerName + " — LEAVE",
                new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -155f - i * 64f),
                new Vector2(440f, 50f), Red,
                () => ShowLeaveConfirmation(player));
        }

        AddButton(modal.transform, "BACK",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 18f),
            new Vector2(180f, 44f), ButtonColor, ShowEndGameMenu);
    }

    private void ShowLeaveConfirmation(BoardPlayer player)
    {
        if (player == null || !player.IsActive)
        {
            ShowEndGameMenu();
            return;
        }

        ShowOverlay();
        BuildModal("CONFIRM LEAVE", new Vector2(650f, 430f));

        AddText(modal.transform, "LeaveWarning",
            "Are you sure " + player.PlayerName + " wants to leave?\nAll owned properties, buildings, and mortgages will be cleared.",
            23, White, TextAlignmentOptions.Center,
            new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(0f, 110f));

        AddButton(modal.transform, "YES, LEAVE",
            new Vector2(0.35f, 0f), new Vector2(0.35f, 0f),
            new Vector2(0.5f, 0f), new Vector2(-8f, 40f),
            new Vector2(205f, 52f), Red,
            () =>
            {
                bool left = gameManager.LeaveGame(player);
                if (left && gameManager.GameOver)
                    return;

                HideOverlay();
            });

        AddButton(modal.transform, "CANCEL",
            new Vector2(0.65f, 0f), new Vector2(0.65f, 0f),
            new Vector2(0.5f, 0f), new Vector2(8f, 40f),
            new Vector2(205f, 52f), ButtonColor, ShowLeavePlayerSelection);
    }

    private void BeginVote()
    {
        yesVotes.Clear();
        if (gameManager == null || !gameManager.StartEndGameVote())
        {
            GameNotificationUI.Show("THE END-GAME VOTE CANNOT START RIGHT NOW");
            HideOverlay();
            return;
        }

        ShowVotePanel();
    }

    private void ShowVotePanel()
    {
        BoardPlayer[] activePlayers = gameManager.GetActivePlayers();
        ShowOverlay();
        BuildModal("VOTE TO END MATCH", new Vector2(740f, Mathf.Clamp(250f + activePlayers.Length * 66f, 560f, 850f)));

        AddText(modal.transform, "VoteInstruction",
            "Every active player must vote YES. Any NO cancels the vote.",
            19, Muted, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, 44f));

        for (int i = 0; i < activePlayers.Length; i++)
        {
            BoardPlayer player = activePlayers[i];
            GameObject row = CreateImage(
                "VoteRow_" + player.PlayerNumber,
                modal.transform,
                new Color(0.10f, 0.13f, 0.19f, 1f),
                new Vector2(0.05f, 1f),
                new Vector2(0.95f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -150f - i * 66f),
                new Vector2(0f, 54f)
            );

            AddText(row.transform, "VoterName", player.PlayerName,
                20, White, TextAlignmentOptions.Left,
                new Vector2(0.04f, 0f), new Vector2(0.58f, 1f),
                new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

            AddButton(row.transform, yesVotes.Contains(player) ? "YES ✓" : "YES",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-112f, 0f),
                new Vector2(92f, 42f), Green,
                () => RegisterYesVote(player));

            AddButton(row.transform, "NO",
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(1f, 0.5f), new Vector2(-12f, 0f),
                new Vector2(82f, 42f), Red,
                CancelVote);
        }

        AddButton(modal.transform, "CANCEL VOTE",
            new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 20f),
            new Vector2(210f, 44f), ButtonColor, CancelVote);
    }

    private void RegisterYesVote(BoardPlayer player)
    {
        if (!gameManager.IsEndGameVoteInProgress || player == null || !player.IsActive)
        {
            CancelVote();
            return;
        }

        yesVotes.Add(player);

        int activeCount = gameManager.ActivePlayerCount;
        if (yesVotes.Count >= activeCount)
        {
            gameManager.EndGameByUnanimousVote();
            return;
        }

        ShowVotePanel();
    }

    private void CancelVote()
    {
        yesVotes.Clear();
        if (gameManager != null)
            gameManager.CancelEndGameVote();

        HideOverlay();
        GameNotificationUI.Show("END-GAME VOTE REJECTED — MATCH RESUMES");
    }

    private void BuildCanvas()
    {
        if (!Application.isPlaying || canvas != null)
            return;

        GameObject canvasObject = new GameObject(
            "MatchEndUICanvas",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        canvasObject.transform.SetParent(transform, false);

        canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject(
                "MatchEndEventSystem",
                typeof(EventSystem),
                typeof(StandaloneInputModule)
            );
        }

        menuButton = AddButton(
            canvas.transform,
            "GAME MENU",
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(-24f, -24f),
            new Vector2(185f, 54f),
            ButtonColor,
            ShowEndGameMenu
        );

        overlay = CreateImage(
            "MatchEndOverlay",
            canvas.transform,
            new Color(0.015f, 0.025f, 0.045f, 0.88f),
            Vector2.zero,
            Vector2.one,
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            Vector2.zero
        );

        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        overlayRect.offsetMin = Vector2.zero;
        overlayRect.offsetMax = Vector2.zero;
        overlay.SetActive(false);
    }

    private void ShowOverlay()
    {
        if (canvas == null)
            BuildCanvas();

        if (overlay != null)
            overlay.SetActive(true);
    }

    private void HideOverlay()
    {
        if (overlay != null)
            overlay.SetActive(false);

        if (menuButton != null && (gameManager == null || !gameManager.GameOver))
            menuButton.gameObject.SetActive(true);
    }

    private void BuildModal(string title, Vector2 size)
    {
        if (overlay == null)
            return;

        if (modal != null)
            Destroy(modal);

        modal = CreateImage(
            "MatchEndModal",
            overlay.transform,
            PanelColor,
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            size
        );

        AddText(modal.transform, "ModalTitle", title,
            32, Gold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0f, 48f));
    }

    private void AddText(
        Transform parent,
        string objectName,
        string value,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        GameObject textObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(TextMeshProUGUI)
        );

        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;
    }

    private Button AddButton(
        Transform parent,
        string label,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size,
        Color color,
        UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = CreateImage(
            "Button_" + label.Replace(" ", "_"),
            parent,
            color,
            anchorMin,
            anchorMax,
            pivot,
            position,
            size
        );

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = buttonObject.GetComponent<Image>();
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        button.onClick.AddListener(action);

        AddText(buttonObject.transform, "Label", label,
            19, White, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
            Vector2.zero, Vector2.zero);

        return button;
    }

    private GameObject CreateImage(
        string objectName,
        Transform parent,
        Color color,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 size)
    {
        GameObject imageObject = new GameObject(
            objectName,
            typeof(RectTransform),
            typeof(Image)
        );

        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = imageObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return imageObject;
    }

    private PlayerResult[] CalculateResults(GameManager manager)
    {
        BoardPlayer[] players = manager.Players ?? Array.Empty<BoardPlayer>();
        BoardSpace[] spaces = FindObjectsByType<BoardSpace>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        List<PlayerResult> results = new List<PlayerResult>();

        foreach (BoardPlayer player in players)
        {
            if (player == null)
                continue;

            PlayerResult result = new PlayerResult
            {
                Player = player,
                Status = GetStatus(player),
                Cash = player.IsBankrupt ? 0L : player.Money
            };

            foreach (BoardSpace space in spaces)
            {
                if (space == null || space.Owner != player || !space.IsOwned)
                    continue;

                result.PropertyCount++;
                result.PropertyValue += Math.Max(0, space.PurchasePrice);

                if (space.HasHotel)
                {
                    result.BuildingValue += Math.Max(0, space.EffectiveHotelCost);
                    result.BuildingValue += (long)Math.Max(0, space.EffectiveHouseCost) * 4L;
                }
                else
                {
                    result.BuildingValue += (long)Math.Max(0, space.EffectiveHouseCost) *
                        Math.Max(0, space.Houses);
                }

                if (space.IsMortgaged)
                {
                    result.MortgageLiability += (long)Math.Ceiling(space.MortgageValue * 1.10d);
                }
            }

            result.NetWorth =
                result.Cash +
                result.PropertyValue +
                result.BuildingValue -
                result.MortgageLiability;

            results.Add(result);
        }

        return results
            .OrderByDescending(r => r.NetWorth)
            .ThenBy(r => r.Player.PlayerNumber)
            .ToArray();
    }

    private string GetStatus(BoardPlayer player)
    {
        if (player == null)
            return "UNKNOWN";

        if (player.HasLeftGame)
            return "LEFT GAME";

        if (player.IsBankrupt)
            return "BANKRUPT";

        return "ACTIVE";
    }

    private string FormatMoney(long value)
    {
        return "$" + value.ToString("N0");
    }
}
