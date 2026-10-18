using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controls editable scene UI created by MatchEndUIBuilder.
/// Gameplay rules remain in GameManager; this component only displays UI and
/// forwards player actions to the existing GameManager API.
/// </summary>
public sealed class MatchEndUI : MonoBehaviour
{
    [Header("Shared")]
    [SerializeField] private GameObject overlay;
    [SerializeField] private Button menuButton;
    [SerializeField] private TMP_Text menuButtonLabel;

    [Header("Main Menu")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button menuVoteButton;
    [SerializeField] private Button menuLeaveButton;
    [SerializeField] private Button menuCloseButton;

    [Header("Vote Panel")]
    [SerializeField] private GameObject votePanel;
    [SerializeField] private Transform voteRows;
    [SerializeField] private GameObject voteRowTemplate;
    [SerializeField] private Button voteCancelButton;

    [Header("Leave Player Panel")]
    [SerializeField] private GameObject leavePlayerPanel;
    [SerializeField] private Transform leavePlayerRows;
    [SerializeField] private GameObject leavePlayerRowTemplate;
    [SerializeField] private Button leavePlayerBackButton;

    [Header("Leave Confirmation")]
    [SerializeField] private GameObject leaveConfirmPanel;
    [SerializeField] private TMP_Text leaveConfirmText;
    [SerializeField] private Button leaveConfirmYesButton;
    [SerializeField] private Button leaveConfirmCancelButton;

    [Header("Final Results")]
    [SerializeField] private GameObject finalResultsPanel;
    [SerializeField] private TMP_Text finalWinnerText;
    [SerializeField] private TMP_Text finalReasonText;
    [SerializeField] private Transform resultRows;
    [SerializeField] private GameObject resultRowTemplate;
    [SerializeField] private TMP_Text valuationNoteText;
    [SerializeField] private Button newMatchButton;
    [SerializeField] private Button closeResultsButton;

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
    private readonly HashSet<BoardPlayer> yesVotes = new HashSet<BoardPlayer>();
    private readonly List<BoardPlayer> voteOrder = new List<BoardPlayer>();
    private int currentVoteIndex;
    private TMP_Text voteInstructionText;
    private BoardPlayer selectedLeavingPlayer;
    private BoardPlayer storedWinner;
    private string storedReason;

    public BoardPlayer CurrentVotePlayer =>
        currentVoteIndex >= 0 && currentVoteIndex < voteOrder.Count
            ? voteOrder[currentVoteIndex]
            : null;

    public int VoteCountReceived =>
        yesVotes.Count;

    public int VotePlayerCount =>
        voteOrder.Count;

    private void Awake()
    {
        gameManager = GetComponent<GameManager>();
        ResolveMissingReferences();
        voteInstructionText = FindChildText(
            votePanel != null ? votePanel.transform : null,
            "VoteInstruction"
        );
        BindButtons();
        HideAllPanels();
    }

    /// <summary>
    /// Recovers common missing scene references without recreating or
    /// restyling the user's manually designed UI. Button labels are used
    /// as the reliable fallback when an Inspector slot was left empty
    /// or accidentally pointed at another button.
    /// </summary>
    private void ResolveMissingReferences()
    {
        if (overlay == null)
        {
            GameObject foundOverlay = GameObject.Find("MatchEndOverlay");
            if (foundOverlay != null)
                overlay = foundOverlay;
        }

        if (leaveConfirmPanel == null && overlay != null)
            leaveConfirmPanel = FindPanelByName(overlay.transform, "LeaveConfirmPanel");

        if (leaveConfirmPanel != null)
        {
            Button yesByLabel = FindButtonByNormalizedLabel(
                leaveConfirmPanel.transform,
                "YESLEAVE"
            );

            if (yesByLabel != null)
                leaveConfirmYesButton = yesByLabel;

            Button cancelByLabel = FindButtonByNormalizedLabel(
                leaveConfirmPanel.transform,
                "CANCEL"
            );

            if (cancelByLabel != null)
                leaveConfirmCancelButton = cancelByLabel;

            if (leaveConfirmText == null)
                leaveConfirmText = FindFirstTextOutsideButtons(
                    leaveConfirmPanel.transform
                );
        }

        if (leaveConfirmYesButton == null)
            Debug.LogError(
                "MatchEndUI: Could not find the YES, LEAVE Button. " +
                "Assign it to Leave Confirm Yes Button on MatchEndUI, " +
                "or make sure the button's visible text is 'YES, LEAVE'.",
                this
            );

        if (leaveConfirmCancelButton == null)
            Debug.LogWarning(
                "MatchEndUI: Could not find the Leave Confirm Cancel Button. " +
                "Assign it in the MatchEndUI Inspector.",
                this
            );
    }

    private static GameObject FindPanelByName(Transform root, string targetName)
    {
        if (root == null)
            return null;

        Transform[] descendants = root.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in descendants)
        {
            if (string.Equals(child.name, targetName, StringComparison.OrdinalIgnoreCase))
                return child.gameObject;
        }

        return null;
    }

    private static Button FindButtonByNormalizedLabel(Transform root, string normalizedTarget)
    {
        if (root == null)
            return null;

        Button[] buttons = root.GetComponentsInChildren<Button>(true);
        foreach (Button button in buttons)
        {
            if (button == null)
                continue;

            string label = GetButtonText(button);
            if (NormalizeButtonLabel(label) == normalizedTarget)
                return button;

            string objectName = NormalizeButtonLabel(button.gameObject.name);
            if (objectName == normalizedTarget)
                return button;
        }

        return null;
    }

    private static string GetButtonText(Button button)
    {
        TMP_Text tmp = button.GetComponentInChildren<TMP_Text>(true);
        if (tmp != null)
            return tmp.text;

        Text legacyText = button.GetComponentInChildren<Text>(true);
        return legacyText != null ? legacyText.text : string.Empty;
    }

    private static string NormalizeButtonLabel(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] chars = value
            .ToUpperInvariant()
            .Where(char.IsLetterOrDigit)
            .ToArray();

        return new string(chars);
    }

    private static TMP_Text FindFirstTextOutsideButtons(Transform root)
    {
        if (root == null)
            return null;

        TMP_Text[] texts = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (TMP_Text text in texts)
        {
            if (text != null && text.GetComponentInParent<Button>() == null)
                return text;
        }

        return null;
    }

    private void BindButtons()
    {
        Bind(menuButton, ShowEndGameMenu);
        Bind(menuVoteButton, BeginVote);
        Bind(menuLeaveButton, ShowLeavePlayerSelection);
        Bind(menuCloseButton, HideOverlay);
        Bind(voteCancelButton, CancelVote);
        Bind(leavePlayerBackButton, ShowEndGameMenu);
        Bind(leaveConfirmYesButton, ConfirmLeave);
        Bind(leaveConfirmCancelButton, ShowLeavePlayerSelection);
        Bind(newMatchButton, RestartCurrentScene);
        Bind(closeResultsButton, CloseResults);
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void ShowEndGameMenu()
    {
        if (gameManager == null)
            return;

        if (gameManager.GameOver)
        {
            ShowFinalResults(gameManager, storedWinner, storedReason);
            return;
        }

        ShowOnly(mainMenuPanel);
    }

    public void ShowFinalResults(GameManager manager, BoardPlayer naturalWinner, string reason)
    {
        if (manager != null)
            gameManager = manager;

        if (gameManager == null || finalResultsPanel == null)
        {
            Debug.LogError("MatchEndUI: Final Results UI references are missing. Use Board Game > UI > Create Editable Match-End UI.");
            return;
        }

        storedReason = reason;
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

        storedWinner = winner;

        if (winner != null)
            results = results
                .OrderBy(r => r.Player == winner ? 0 : r.Player.IsActive ? 1 : 2)
                .ThenByDescending(r => r.NetWorth)
                .ThenBy(r => r.Player.PlayerNumber)
                .ToArray();

        if (finalWinnerText != null)
            finalWinnerText.text = winner != null
                ? "★  " + winner.PlayerName.ToUpperInvariant() + " WINS  ★"
                : "FINAL RESULTS";

        if (finalReasonText != null)
            finalReasonText.text = string.IsNullOrWhiteSpace(reason)
                ? "FINAL PLAYER STANDING"
                : reason.ToUpperInvariant();

        if (valuationNoteText != null)
            valuationNoteText.text =
                "NET WORTH = CASH + PROPERTY VALUE + BUILDING COST − MORTGAGE PAYOFF (110% OF MORTGAGE VALUE)";

        ClearDynamicRows(resultRows, resultRowTemplate);
        if (resultRows == null || resultRowTemplate == null)
        {
            Debug.LogError("MatchEndUI: ResultRows or ResultRowTemplate is not assigned.");
            return;
        }

        for (int i = 0; i < results.Length; i++)
        {
            PlayerResult result = results[i];
            GameObject row = Instantiate(resultRowTemplate, resultRows);
            row.name = "ResultRow_" + (i + 1);
            row.SetActive(true);

            TMP_Text summary = row.GetComponentInChildren<TMP_Text>(true);
            if (summary != null)
            {
                string status = result.Player == winner ? "WINNER" : result.Status;
                summary.text =
                    "#" + (i + 1) + "  " + result.Player.PlayerName + "  [" + status + "]\n" +
                    "CASH " + FormatMoney(result.Cash) +
                    "  |  PROPERTIES " + result.PropertyCount + " / " + FormatMoney(result.PropertyValue) +
                    "  |  BUILDINGS " + FormatMoney(result.BuildingValue) +
                    "  |  MORTGAGE -" + FormatMoney(result.MortgageLiability) +
                    "  |  NET WORTH " + FormatMoney(result.NetWorth);
            }

            Image background = row.GetComponent<Image>();
            if (background != null && result.Player == winner)
                background.color = new Color(0.30f, 0.24f, 0.10f, 1f);
        }

        ShowOnly(finalResultsPanel);
        SetMenuButtonLabel("FINAL RESULTS");
    }

    private void BeginVote()
    {
        yesVotes.Clear();
        voteOrder.Clear();
        currentVoteIndex = 0;

        if (gameManager == null || !gameManager.StartEndGameVote())
        {
            GameNotificationUI.Show("THE END-GAME VOTE CANNOT START RIGHT NOW");
            HideOverlay();
            return;
        }

        BuildVoteOrder();

        if (voteOrder.Count < 2)
        {
            CancelVote();
            return;
        }

        ShowVotePanel();
    }

    private void BuildVoteOrder()
    {
        voteOrder.Clear();

        if (gameManager == null)
            return;

        BoardPlayer[] players = gameManager.Players;
        if (players == null || players.Length == 0)
            return;

        // Start with the player whose turn it is, then continue in turn order.
        int startIndex = System.Array.IndexOf(
            players,
            gameManager.CurrentPlayer
        );

        if (startIndex < 0)
            startIndex = 0;

        for (int offset = 0; offset < players.Length; offset++)
        {
            int index = (startIndex + offset) % players.Length;
            BoardPlayer player = players[index];

            if (player != null && player.IsActive)
                voteOrder.Add(player);
        }
    }

    private void ShowVotePanel()
    {
        if (gameManager == null || votePanel == null || voteRows == null || voteRowTemplate == null)
        {
            Debug.LogError("MatchEndUI: Vote UI references are missing. The scene design was not changed.");
            return;
        }

        ClearDynamicRows(voteRows, voteRowTemplate);

        // Inactive participants cannot cast a vote. Move forward safely if
        // a participant was eliminated by a future extension during voting.
        while (currentVoteIndex < voteOrder.Count &&
               (voteOrder[currentVoteIndex] == null ||
                !voteOrder[currentVoteIndex].IsActive ||
                yesVotes.Contains(voteOrder[currentVoteIndex])))
        {
            currentVoteIndex++;
        }

        int requiredVotes = gameManager.ActivePlayerCount;
        if (yesVotes.Count >= requiredVotes)
        {
            gameManager.EndGameByUnanimousVote();
            return;
        }

        if (currentVoteIndex >= voteOrder.Count)
        {
            CancelVote();
            return;
        }

        BoardPlayer voter = voteOrder[currentVoteIndex];

        if (voteInstructionText != null)
        {
            voteInstructionText.text =
                voter.PlayerName.ToUpperInvariant() +
                ", DO YOU AGREE TO END THE MATCH?";
        }

        // Show exactly one vote row at a time. No participant can be selected
        // from a roster to cast a vote on somebody else's behalf.
        GameObject row = Instantiate(voteRowTemplate, voteRows);
        row.name = "CurrentVote_" + voter.PlayerNumber;
        row.SetActive(true);

        TMP_Text nameText = FindChildText(row.transform, "VoterName");
        if (nameText != null)
            nameText.text = voter.PlayerName + " — YOUR VOTE";

        Button yesButton = FindChildButton(row.transform, "YesButton");
        Button noButton = FindChildButton(row.transform, "NoButton");

        if (yesButton != null)
        {
            TMP_Text label = yesButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "YES";

            yesButton.interactable = true;
            yesButton.onClick.RemoveAllListeners();
            yesButton.onClick.AddListener(() => RegisterYesVote(voter));
        }
        else
        {
            Debug.LogError("MatchEndUI: Vote row template is missing a child named YesButton.");
        }

        if (noButton != null)
        {
            TMP_Text label = noButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "NO";

            noButton.interactable = true;
            noButton.onClick.RemoveAllListeners();
            noButton.onClick.AddListener(CancelVote);
        }
        else
        {
            Debug.LogError("MatchEndUI: Vote row template is missing a child named NoButton.");
        }

        ShowOnly(votePanel);
    }

    private void RegisterYesVote(BoardPlayer player)
    {
        if (gameManager == null ||
            !gameManager.IsEndGameVoteInProgress ||
            player == null ||
            !player.IsActive ||
            player != CurrentVotePlayer)
        {
            return;
        }

        if (!yesVotes.Add(player))
            return;

        currentVoteIndex++;

        if (yesVotes.Count >= gameManager.ActivePlayerCount)
        {
            gameManager.EndGameByUnanimousVote();
            return;
        }

        ShowVotePanel();
    }

    private void CancelVote()
    {
        yesVotes.Clear();
        voteOrder.Clear();
        currentVoteIndex = 0;

        if (gameManager != null)
            gameManager.CancelEndGameVote();

        HideOverlay();
        GameNotificationUI.Show("END-GAME VOTE REJECTED — MATCH RESUMES");
    }

    private void ShowLeavePlayerSelection()
    {
        if (gameManager == null || leavePlayerPanel == null ||
            leavePlayerRows == null || leavePlayerRowTemplate == null)
        {
            Debug.LogError("MatchEndUI: Leave Game UI references are missing. The scene design was not changed.");
            return;
        }

        // This is a local, turn-based multiplayer game. There is no separate
        // authenticated client per player, so the current player is the only
        // participant this screen can safely allow to leave.
        BoardPlayer player = gameManager.CurrentPlayer;
        if (player == null || !player.IsActive)
        {
            GameNotificationUI.Show("NO ACTIVE CURRENT PLAYER CAN LEAVE RIGHT NOW");
            HideOverlay();
            return;
        }

        ClearDynamicRows(leavePlayerRows, leavePlayerRowTemplate);

        GameObject row = Instantiate(leavePlayerRowTemplate, leavePlayerRows);
        row.name = "LeaveCurrentPlayer_" + player.PlayerNumber;
        row.SetActive(true);

        TMP_Text label = row.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "LEAVE AS " + player.PlayerName.ToUpperInvariant();

        Button button = row.GetComponent<Button>();
        if (button == null)
            button = row.GetComponentInChildren<Button>(true);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowLeaveConfirmation(player));
        }
        else
        {
            Debug.LogError("MatchEndUI: Leave-player row template needs a Button component.");
        }

        ShowOnly(leavePlayerPanel);
    }

    private void ShowLeaveConfirmation(BoardPlayer player)
    {
        if (player == null || !player.IsActive || leaveConfirmPanel == null)
            return;

        selectedLeavingPlayer = player;
        if (leaveConfirmText != null)
        {
            leaveConfirmText.text =
                "ARE YOU SURE " + player.PlayerName.ToUpperInvariant() + " WANTS TO LEAVE?\n\n" +
                "ALL OWNED PROPERTIES, BUILDINGS, AND MORTGAGES WILL BE CLEARED.";
        }

        ShowOnly(leaveConfirmPanel);
    }

    private void ConfirmLeave()
    {
        Debug.Log("MatchEndUI: YES, LEAVE was clicked.", this);

        if (gameManager == null)
        {
            Debug.LogError("MatchEndUI: GameManager reference is missing.", this);
            GameNotificationUI.Show("COULD NOT LEAVE: GAME MANAGER IS MISSING");
            return;
        }

        if (selectedLeavingPlayer == null)
        {
            Debug.LogError(
                "MatchEndUI: YES, LEAVE was clicked but no leaving player was selected.",
                this
            );
            GameNotificationUI.Show("PLEASE SELECT LEAVE GAME AGAIN");
            ShowEndGameMenu();
            return;
        }

        bool left = gameManager.LeaveGame(selectedLeavingPlayer);
        selectedLeavingPlayer = null;

        if (!left)
        {
            ShowEndGameMenu();
            return;
        }

        if (gameManager.GameOver)
            return;

        HideOverlay();
        GameNotificationUI.Show("PLAYER LEFT THE MATCH — PROPERTIES RETURNED TO THE BANK");
    }

    private void RestartCurrentScene()
    {
        if (gameManager != null && gameManager.IsEndGameVoteInProgress)
            gameManager.CancelEndGameVote();

        Scene scene = SceneManager.GetActiveScene();
        if (scene.buildIndex >= 0)
            SceneManager.LoadScene(scene.buildIndex);
        else
            SceneManager.LoadScene(scene.name);
    }

    private void CloseResults()
    {
        HideOverlay();
        SetMenuButtonLabel("FINAL RESULTS");
    }

    private void ShowOnly(GameObject panel)
    {
        if (overlay == null)
        {
            Debug.LogError("MatchEndUI: Overlay reference is not assigned. Use Board Game > UI > Create Editable Match-End UI.");
            return;
        }

        overlay.SetActive(true);
        SetPanelActive(mainMenuPanel, panel == mainMenuPanel);
        SetPanelActive(votePanel, panel == votePanel);
        SetPanelActive(leavePlayerPanel, panel == leavePlayerPanel);
        SetPanelActive(leaveConfirmPanel, panel == leaveConfirmPanel);
        SetPanelActive(finalResultsPanel, panel == finalResultsPanel);
    }

    private void HideOverlay()
    {
        if (overlay != null)
            overlay.SetActive(false);
    }

    private void HideAllPanels()
    {
        if (overlay != null)
            overlay.SetActive(false);

        SetPanelActive(mainMenuPanel, false);
        SetPanelActive(votePanel, false);
        SetPanelActive(leavePlayerPanel, false);
        SetPanelActive(leaveConfirmPanel, false);
        SetPanelActive(finalResultsPanel, false);

        if (voteRowTemplate != null)
            voteRowTemplate.SetActive(false);
        if (leavePlayerRowTemplate != null)
            leavePlayerRowTemplate.SetActive(false);
        if (resultRowTemplate != null)
            resultRowTemplate.SetActive(false);

        if (menuButton != null)
            menuButton.gameObject.SetActive(true);
    }

    private static void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
            panel.SetActive(active);
    }

    private static void ClearDynamicRows(Transform container, GameObject template)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Transform child = container.GetChild(i);
            if (template != null && child.gameObject == template)
                continue;

            Destroy(child.gameObject);
        }
    }

    private static TMP_Text FindChildText(Transform parent, string childName)
    {
        Transform child = FindDeepChild(parent, childName);
        return child != null ? child.GetComponent<TMP_Text>() : null;
    }

    private static Button FindChildButton(Transform parent, string childName)
    {
        Transform child = FindDeepChild(parent, childName);
        return child != null ? child.GetComponent<Button>() : null;
    }

    private static Transform FindDeepChild(Transform parent, string childName)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == childName)
                return child;

            Transform nested = FindDeepChild(child, childName);
            if (nested != null)
                return nested;
        }

        return null;
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
                Cash = player.IsActive ? player.Money : 0L
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
                    result.MortgageLiability += (long)Math.Ceiling(space.MortgageValue * 1.10d);
            }

            result.NetWorth = result.Cash + result.PropertyValue + result.BuildingValue - result.MortgageLiability;
            results.Add(result);
        }

        return results
            .OrderByDescending(r => r.Player.IsActive)
            .ThenByDescending(r => r.NetWorth)
            .ThenBy(r => r.Player.PlayerNumber)
            .ToArray();
    }

    private string GetStatus(BoardPlayer player)
    {
        if (player.HasLeftGame)
            return "LEFT GAME";

        if (player.IsBankrupt)
            return "BANKRUPT";

        return "ACTIVE";
    }

    private static TMP_Text GetButtonLabel(Button button)
    {
        return button != null ? button.GetComponentInChildren<TMP_Text>(true) : null;
    }

    private static string FormatMoney(long value)
    {
        return "$" + value.ToString("N0");
    }

    private void SetMenuButtonLabel(string label)
    {
        if (menuButtonLabel != null)
            menuButtonLabel.text = label;
        else
        {
            TMP_Text text = GetButtonLabel(menuButton);
            if (text != null)
                text.text = label;
        }
    }
}
