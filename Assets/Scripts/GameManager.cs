using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    [Header("Players")]
    [SerializeField] private BoardPlayer[] players;
    [SerializeField] private int currentPlayerIndex = 0;

    [Header("Turn State")]
    [SerializeField] private bool isTurnActive;
    [SerializeField] private bool waitingForPlayerAction;

    [SerializeField]
    private GamePhase currentPhase = GamePhase.WaitingToRoll;

    private Coroutine botTurnRoutine;
    private readonly Dictionary<string, float> botTradeOfferCooldownUntil =
        new Dictionary<string, float>();
    // Prevent bots from repeatedly swapping the same two assets back and forth.
    private readonly HashSet<string> botTradePairsCompletedThisMatch =
        new HashSet<string>();

    [Header("Bot Timing")]
    [SerializeField, Min(0f)] private float botThinkBeforeRoll = 0.9f;
    [SerializeField, Min(0f)] private float botThinkBeforeAction = 1.2f;
    [SerializeField, Min(0f)] private float botThinkBeforeEndingTurn = 0.6f;

    [Header("Dice")]
    [SerializeField] private int lastDiceRoll;
    [SerializeField] private int consecutiveDoubles;

    [Header("Bank")]
    [SerializeField] private long bankMoney = 20580;

    [Header("Game State")]
    [SerializeField] private bool gameStarted;
    [SerializeField] private bool gameOver;
    [SerializeField] private bool endGameVoteInProgress;
    private int completedTurnsThisMatch;

    // ============================================================
    // CHANCE / TURN HISTORY
    // ============================================================

    // Balance recorded at the beginning of each player's turn.
    private readonly Dictionary<BoardPlayer, int> turnStartBalances =
        new Dictionary<BoardPlayer, int>();

    // Stores whether the player lost money on their last completed
    // turns. Maximum of the most recent 2 turns is kept.
    private readonly Dictionary<BoardPlayer, Queue<bool>> turnLossHistory =
        new Dictionary<BoardPlayer, Queue<bool>>();

    // ============================================================
    // CHANCE EFFECTS
    // ============================================================

    // These are accessed by CardManager.
    // The actual effect storage remains in CardManager.
    // GameManager only handles the game-wide transactions.

    // ============================================================
    // PROPERTIES
    // ============================================================

    public BoardPlayer CurrentPlayer
    {
        get
        {
            if (players == null ||
                players.Length == 0)
            {
                return null;
            }

            if (currentPlayerIndex < 0 ||
                currentPlayerIndex >= players.Length)
            {
                return null;
            }

            return players[currentPlayerIndex];
        }
    }

    public BoardPlayer[] Players =>
        players;

    public int CurrentPlayerIndex =>
        currentPlayerIndex;

    public int CurrentPlayerNumber =>
        CurrentPlayer != null
            ? CurrentPlayer.PlayerNumber
            : currentPlayerIndex + 1;

    public bool IsTurnActive =>
        isTurnActive;

    public bool WaitingForPlayerAction =>
        waitingForPlayerAction;

    public int LastDiceRoll =>
        lastDiceRoll;

    public int ConsecutiveDoubles =>
        consecutiveDoubles;

    public GamePhase CurrentPhase =>
        currentPhase;

    public bool GameStarted =>
        gameStarted;

    public bool GameOver =>
        gameOver;

    public bool IsEndGameVoteInProgress =>
        endGameVoteInProgress;

    public long BankMoney =>
        bankMoney;

    public bool IsPlayerActive(
        BoardPlayer player)
    {
        return player != null &&
               player.IsActive;
    }

    public int ActivePlayerCount =>
        GetActivePlayerCount();

    public bool IsMarketFrozen(
        BoardPlayer player)
    {
        if (player == null)
            return false;

        CommunityChestCardManager communityChest =
            FindFirstObjectByType<CommunityChestCardManager>(
                FindObjectsInactive.Include
            );

        return communityChest != null &&
               communityChest.IsMarketFrozen(player);
    }

    public bool IsFinancialRecoveryActive(
        BoardPlayer player)
    {
        FinancialRecoveryManager recovery =
            GetFinancialRecoveryManager();

        return recovery != null &&
               recovery.IsRecovering(player);
    }

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        players =
            FindExistingPlayers();

        ConfigurePlayersFromLocalSettings();

        RemoveNullPlayers();
        SortPlayers();

        currentPlayerIndex = 0;
    }

    private void Start()
    {
        EnsureMatchEndUI();
        InitializeGame();
    }

    private MatchEndUI EnsureMatchEndUI()
    {
        if (!Application.isPlaying)
            return null;

        MatchEndUI matchEndUI = GetComponent<MatchEndUI>();
        if (matchEndUI == null)
            matchEndUI = gameObject.AddComponent<MatchEndUI>();

        return matchEndUI;
    }

    // ============================================================
    // PLAYER DISCOVERY
    // ============================================================

    private BoardPlayer[] FindExistingPlayers()
    {
        return FindObjectsByType<BoardPlayer>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None
        );
    }

    // ============================================================
    // LOCAL GAME CONFIGURATION
    // ============================================================

    private void ConfigurePlayersFromLocalSettings()
    {
        if (LocalGameSettings.Players == null ||
            LocalGameSettings.Players.Count < 2)
        {
            Debug.Log(
                "GameManager: No local player configuration found. " +
                "Using scene players."
            );

            return;
        }

        List<BoardPlayer> existing =
            new List<BoardPlayer>(
                FindExistingPlayers()
            );

        if (existing.Count == 0)
        {
            Debug.LogError(
                "GameManager: No BoardPlayer objects exist " +
                "in MainScene to use as templates."
            );

            return;
        }

        BoardPlayer template =
            existing[0];

        int requestedCount =
            Mathf.Clamp(
                LocalGameSettings.Players.Count,
                2,
                6
            );

        // --------------------------------------------------------
        // CREATE ADDITIONAL PLAYER OBJECTS
        // --------------------------------------------------------

        while (existing.Count < requestedCount)
        {
            BoardPlayer clone =
                Instantiate(
                    template,
                    template.transform.parent
                );

            clone.name =
                $"Player_{existing.Count + 1}";

            clone.gameObject.SetActive(true);

            existing.Add(clone);
        }

        // --------------------------------------------------------
        // DISABLE EXCESS PLAYERS
        // --------------------------------------------------------

        for (int i = 0; i < existing.Count; i++)
        {
            if (existing[i] == null)
                continue;

            if (i >= requestedCount)
            {
                existing[i].gameObject.SetActive(false);
            }
        }

        // --------------------------------------------------------
        // APPLY CONFIGURATION
        // --------------------------------------------------------

        List<BoardPlayer> configured =
            new List<BoardPlayer>();

        for (int i = 0;
             i < requestedCount;
             i++)
        {
            BoardPlayer player =
                existing[i];

            if (player == null)
                continue;

            LocalGameSettings.PlayerConfig config =
                LocalGameSettings.GetPlayer(i);

            if (config == null)
                continue;

            player.gameObject.SetActive(true);

            player.ResetForNewGame();

            player.SetPlayerNumber(
                i + 1
            );

            player.SetPlayerName(
                config.playerName
            );

            player.SetTokenColor(
                config.color
            );

            player.SetIsBot(
                config.isBot
            );

            configured.Add(
                player
            );
        }

        players =
            configured.ToArray();

        Debug.Log(
            $"GameManager: Configured " +
            $"{players.Length} local players."
        );

        foreach (BoardPlayer player in players)
        {
            Debug.Log(
                $"PLAYER CONFIG: " +
                $"{player.PlayerNumber} | " +
                $"{player.PlayerName} | " +
                $"{(player.IsBot ? "BOT" : "HUMAN")}"
            );
        }
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public void InitializeGame()
    {
        if (players == null ||
            players.Length < 2)
        {
            Debug.LogError(
                "GameManager: At least 2 players are required."
            );

            return;
        }

        RemoveNullPlayers();
        SortPlayers();

        turnStartBalances.Clear();
        turnLossHistory.Clear();
        botTradeOfferCooldownUntil.Clear();
        botTradePairsCompletedThisMatch.Clear();

        for (int i = 0;
             i < players.Length;
             i++)
        {
            if (players[i] == null)
                continue;

            players[i].SetPlayerNumber(
                i + 1
            );

            players[i].Initialize();

            turnStartBalances[players[i]] =
                players[i].Money;

            turnLossHistory[players[i]] =
                new Queue<bool>();
        }

        currentPlayerIndex = 0;

        lastDiceRoll = 0;
        consecutiveDoubles = 0;

        waitingForPlayerAction = false;
        isTurnActive = false;

        currentPhase =
            GamePhase.WaitingToRoll;

        gameStarted = true;
        gameOver = false;
        endGameVoteInProgress = false;

        GetFinancialRecoveryManager().ResetForNewGame();
        completedTurnsThisMatch = 0;
        BotLearningSystem.BeginMatch(players);

        StartTurn();
    }

    private void RemoveNullPlayers()
    {
        if (players == null)
            return;

        int count = 0;

        foreach (BoardPlayer player in players)
        {
            if (player != null)
                count++;
        }

        if (count == players.Length)
            return;

        BoardPlayer[] validPlayers =
            new BoardPlayer[count];

        int index = 0;

        foreach (BoardPlayer player in players)
        {
            if (player != null)
            {
                validPlayers[index] =
                    player;

                index++;
            }
        }

        players =
            validPlayers;
    }

    private void SortPlayers()
    {
        if (players == null)
            return;

        System.Array.Sort(
            players,
            (a, b) =>
            {
                if (a == null && b == null)
                    return 0;

                if (a == null)
                    return 1;

                if (b == null)
                    return -1;

                return a.PlayerNumber.CompareTo(
                    b.PlayerNumber
                );
            }
        );
    }

    // ============================================================
    // TURN SYSTEM
    // ============================================================

    public void StartTurn()
    {
        if (gameOver || endGameVoteInProgress)
            return;

        BoardPlayer player =
            CurrentPlayer;

        if (player == null)
        {
            Debug.LogError(
                "GameManager: Current player is missing."
            );

            return;
        }

        if (!player.IsActive)
        {
            MoveToNextPlayer();
            return;
        }

        isTurnActive = true;

        waitingForPlayerAction = false;

        currentPhase =
            GamePhase.WaitingToRoll;

        lastDiceRoll = 0;

        // Record the balance at the exact beginning
        // of this player's turn.
        turnStartBalances[player] =
            player.Money;
        BotLearningSystem.BeginTurn(
            player,
            players,
            FindObjectsByType<BoardSpace>(FindObjectsSortMode.None)
        );

        Debug.Log(
            $"TURN STARTED: {player.PlayerName}"
        );

        GameNotificationUI.Show(
            $"{player.PlayerName}'S TURN"
        );

        // Deliver a queued trade request only to its recipient, when their
        // turn actually starts. Sending an offer does not end the sender's turn.
        TradeManager tradeManager =
            FindFirstObjectByType<TradeManager>(
                FindObjectsInactive.Include
            );

        if (tradeManager != null)
            tradeManager.NotifyReceiverTurnStarted(player);


        // Bot turns must be driven by code rather than the human dice button.
        if (botTurnRoutine != null)
        {
            StopCoroutine(botTurnRoutine);
            botTurnRoutine = null;
        }

        if (player.IsBot)
            botTurnRoutine = StartCoroutine(RunBotTurn(player));
    }

    private IEnumerator RunBotTurn(BoardPlayer bot)
    {
        // Let the turn UI and queued notifications settle before acting.
        yield return new WaitForSeconds(0.6f);

        // Give a bot time to review a trade request before deciding.
        EvaluateIncomingBotTrade(bot);
        yield return new WaitForSeconds(0.7f);

        DiceManager diceManager = FindFirstObjectByType<DiceManager>();
        if (diceManager == null)
        {
            Debug.LogError("GameManager: Bot cannot act because DiceManager was not found.");
            botTurnRoutine = null;
            yield break;
        }

        float unresolvedActionSince = -1f;
        while (!gameOver && !endGameVoteInProgress &&
               CurrentPlayer == bot && bot != null && bot.IsActive)
        {
            if (bot.IsMoving || diceManager.IsRolling)
            {
                yield return null;
                continue;
            }

            if (!waitingForPlayerAction)
                unresolvedActionSince = -1f;

            if (waitingForPlayerAction)
            {
                // Give the landing/action panel time to be visible, like a human
                // reading the options before choosing.
                if (unresolvedActionSince < 0f)
                    unresolvedActionSince = Time.time;

                yield return new WaitForSeconds(botThinkBeforeAction);

                if (CurrentPlayer != bot || !waitingForPlayerAction)
                    continue;

                LandingActionPanel landingPanel =
                    FindFirstObjectByType<LandingActionPanel>(FindObjectsInactive.Include);
                if (landingPanel != null && landingPanel.CurrentSpace != null)
                {
                    BoardSpace space = landingPanel.CurrentSpace;
                    bool purchased = ShouldBotBuySpace(bot, space) &&
                        !IsMarketFrozen(bot) && BuySpace(space);
                    if (purchased)
                        BotLearningSystem.RecordAction(bot, BotLearningSystem.ActionType.Purchase);

                    // Develop owned property when the return is worth the cost.
                    if (!purchased && space.Owner == bot)
                        TryBotDevelopment(bot, space);

                    unresolvedActionSince = -1f;
                    landingPanel.ResolveForBot(purchased);
                    yield return null;
                    continue;
                }

                PropertyManagementPanel propertyPanel =
                    FindFirstObjectByType<PropertyManagementPanel>(FindObjectsInactive.Include);
                if (propertyPanel != null && propertyPanel.ResolveForBot())
                {
                    unresolvedActionSince = -1f;
                    yield return null;
                    continue;
                }

                // Card and target panels normally resolve themselves. Keep checking instead
                // of killing the entire bot coroutine if a panel is still animating.
                if (Time.time - unresolvedActionSince < 12f)
                {
                    yield return new WaitForSeconds(0.75f);
                    continue;
                }

                // Last-resort watchdog: an invisible or unsupported modal must not freeze
                // every player after a bot landing. This only runs after all known handlers
                // have had time to finish their action.
                Debug.LogWarning("GameManager: Bot action watchdog released an unresolved action after 12 seconds.");
                unresolvedActionSince = -1f;
                EndPlayerAction();
                if (CurrentPlayer == bot && currentPhase == GamePhase.WaitingToFinishTurn)
                    EndTurn();
                yield return null;
                continue;
            }

            if (currentPhase == GamePhase.WaitingToRoll)
            {
                // Offer a group-completing trade only when this exact proposal is not
                // on cooldown; rejected deals should not repeat every turn.
                TryBotOfferGroupCompletingTrade(bot);

                // If cash is dangerously low, raise funds from a legal mortgage
                // before taking another risky turn.
                TryBotCashRecovery(bot);
                TryBotPortfolioDevelopment(bot);

                // Pause before each roll, including extra rolls after doubles.
                yield return new WaitForSeconds(botThinkBeforeRoll);

                if (CurrentPlayer == bot &&
                    isTurnActive &&
                    currentPhase == GamePhase.WaitingToRoll &&
                    !waitingForPlayerAction &&
                    !bot.IsMoving &&
                    !diceManager.IsRolling)
                {
                    diceManager.RollDice();
                }

                yield return null;
                continue;
            }

            if (currentPhase == GamePhase.WaitingToFinishTurn)
            {
                yield return new WaitForSeconds(botThinkBeforeEndingTurn);

                if (CurrentPlayer == bot &&
                    currentPhase == GamePhase.WaitingToFinishTurn &&
                    !waitingForPlayerAction)
                {
                    EndTurn();
                }

                yield return null;
                continue;
            }

            yield return null;
        }

        botTurnRoutine = null;
    }


    // ============================================================
    // BOT ECONOMY / STRATEGY
    // ============================================================

    private bool ShouldBotBuySpace(BoardPlayer bot, BoardSpace space)
    {
        if (bot == null || space == null || !space.CanBePurchased())
            return false;

        int price = space.PurchasePrice;
        if (price <= 0 || bot.Money < price)
            return false;

        // Preserve enough liquidity for rent, fees and unexpected cards.
        int reserve = Mathf.Max(100, GetBotCashReserve(bot) +
            BotLearningSystem.GetCashReserveAdjustment(
                BotLearningSystem.ActionType.Purchase, 200));
        int cashAfterPurchase = bot.Money - price;

        int ownedInCategory = 0;
        int activeOpponents = 0;
        foreach (BoardPlayer other in players)
        {
            if (other != null && other.IsActive && other != bot)
                activeOpponents++;
        }

        BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
        if (space.IsProperty && space.PropertyGroup != null)
        {
            foreach (BoardSpace candidate in allSpaces)
            {
                if (candidate != null && candidate.PropertyGroup == space.PropertyGroup &&
                    candidate.Owner == bot)
                    ownedInCategory++;
            }

            int groupSize = space.GetPropertyGroupSize();
            bool completesGroup = ownedInCategory + 1 >= groupSize;
            if (completesGroup)
                return cashAfterPurchase >= Mathf.Max(100, reserve / 2);

            // Prefer properties that bring the bot closer to a monopoly,
            // but don't spend nearly all available cash on a low-impact asset.
            return cashAfterPurchase >= reserve &&
                   (cashAfterPurchase >= price / 2 ||
                    ownedInCategory > 0 ||
                    activeOpponents <= 1);
        }

        // Airports and utilities scale in value as the collection grows.
        if (space.IsRailroad)
        {
            int ownedRailroads = 0;
            foreach (BoardSpace candidate in allSpaces)
                if (candidate != null && candidate.IsRailroad && candidate.Owner == bot)
                    ownedRailroads++;

            return cashAfterPurchase >= reserve / 2 &&
                   (ownedRailroads > 0 || cashAfterPurchase >= reserve);
        }

        if (space.IsUtility)
        {
            int ownedUtilities = 0;
            foreach (BoardSpace candidate in allSpaces)
                if (candidate != null && candidate.IsUtility && candidate.Owner == bot)
                    ownedUtilities++;

            return cashAfterPurchase >= reserve &&
                   (ownedUtilities > 0 || cashAfterPurchase >= price);
        }

        return cashAfterPurchase >= reserve;
    }

    private int GetBotCashReserve(BoardPlayer bot)
    {
        if (bot == null) return 0;

        // Increase the reserve as opponents develop their portfolios.
        int largestRent = 0;
        BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
        foreach (BoardSpace space in allSpaces)
        {
            if (space == null || space.Owner == null || space.Owner == bot ||
                !space.IsOwned || space.IsMortgaged)
                continue;

            largestRent = Mathf.Max(largestRent, space.GetRent());
        }

        return Mathf.Clamp(Mathf.Max(200, largestRent * 2), 200, 600);
    }

    private void TryBotDevelopment(BoardPlayer bot, BoardSpace space)
    {
        if (bot == null || space == null || bot != CurrentPlayer ||
            IsMarketFrozen(bot) || IsFinancialRecoveryActive(bot))
            return;

        int reserve = Mathf.Max(100, GetBotCashReserve(bot) +
            BotLearningSystem.GetCashReserveAdjustment(
                BotLearningSystem.ActionType.Development, 200));

        // Hotels are the best return when already unlocked, but keep a cash buffer.
        if (CanBuildHotel(space) && bot.Money - space.EffectiveHotelCost >= reserve)
        {
            if (BuildHotel(space))
                BotLearningSystem.RecordAction(bot, BotLearningSystem.ActionType.Development);
            return;
        }

        if (CanBuildHouse(space) && bot.Money - space.EffectiveHouseCost >= reserve &&
            BuildHouse(space))
        {
            BotLearningSystem.RecordAction(bot, BotLearningSystem.ActionType.Development);
        }
    }

    // Build across the bot's portfolio rather than only when it happens to
    // land on one of its own properties. At most one upgrade is made per roll
    // decision so development remains paced and leaves cash for rent/cards.
    private void TryBotPortfolioDevelopment(BoardPlayer bot)
    {
        if (bot == null || bot != CurrentPlayer || IsMarketFrozen(bot) ||
            IsFinancialRecoveryActive(bot))
            return;

        int reserve = Mathf.Max(100, GetBotCashReserve(bot) +
            BotLearningSystem.GetCashReserveAdjustment(
                BotLearningSystem.ActionType.Development, 200));
        BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);

        // Prefer a legal hotel upgrade, then a legal house upgrade.
        foreach (BoardSpace space in allSpaces)
        {
            if (space == null || space.Owner != bot || !space.IsProperty)
                continue;

            if (CanBuildHotel(space) &&
                bot.Money - space.EffectiveHotelCost >= reserve &&
                BuildHotel(space))
            {
                BotLearningSystem.RecordAction(bot, BotLearningSystem.ActionType.Development);
                return;
            }
        }

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null || space.Owner != bot || !space.IsProperty)
                continue;

            if (CanBuildHouse(space) &&
                bot.Money - space.EffectiveHouseCost >= reserve &&
                BuildHouse(space))
            {
                BotLearningSystem.RecordAction(bot, BotLearningSystem.ActionType.Development);
                return;
            }
        }
    }

    private void TryBotCashRecovery(BoardPlayer bot)
    {
        if (bot == null || bot != CurrentPlayer || IsFinancialRecoveryActive(bot) ||
            IsMarketFrozen(bot))
            return;

        int recoveryThreshold = Mathf.Max(100, GetBotCashReserve(bot) +
            Mathf.RoundToInt(BotLearningSystem.GetPreference(
                BotLearningSystem.ActionType.Mortgage) * 150f));
        if (bot.Money >= recoveryThreshold)
            return;

        BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
        BoardSpace best = null;
        int bestValue = int.MinValue;

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null || space.Owner != bot || !space.CanMortgage())
                continue;

            // Mortgage the least strategically valuable asset first.
            int value = space.MortgageValue;
            if (space.IsProperty && space.HasCompletePropertyGroup())
                value -= space.PurchasePrice / 2;

            if (value > bestValue)
            {
                bestValue = value;
                best = space;
            }
        }

        if (best != null && MortgageProperty(best))
        {
            BotLearningSystem.RecordAction(bot, BotLearningSystem.ActionType.Mortgage);
            GameNotificationUI.Show(
                bot.PlayerName.ToUpperInvariant() + " RAISED CASH TO PROTECT ITS FINANCES");
        }
    }

    private void EvaluateIncomingBotTrade(BoardPlayer player)
    {
        TradeManager tradeManager = FindFirstObjectByType<TradeManager>(FindObjectsInactive.Include);
        if (tradeManager == null || !tradeManager.HasPendingOffer ||
            player == null || !player.IsBot ||
            tradeManager.PendingOffer == null ||
            tradeManager.PendingOffer.Receiver != player)
            return;

        TradeOffer offer = tradeManager.PendingOffer;
        if (!tradeManager.ValidatePendingOffer(offer, out _))
        {
            tradeManager.DeclineOffer(player, offer);
            return;
        }

        // A completed asset-for-asset swap is not offered again in reverse.
        // Otherwise bots can undo each other's trades and burn turns on loops.
        if (HasCompletedBotTradePair(offer))
        {
            tradeManager.DeclineOffer(player, offer);
            Debug.Log("BOT TRADE: " + player.PlayerName +
                " declined a repeated swap to prevent trade cycling.");
            return;
        }

        long receivedValue = offer.OfferedMoney;
        long givenValue = offer.RequestedMoney;

        // The receiver gains the proposer's offered assets and gives up the
        // requested assets. Keep these sides correctly oriented or bots can
        // accept bad deals and reject genuinely beneficial ones.
        foreach (BoardSpace space in offer.OfferedProperties)
            receivedValue += EstimateBotPropertyValue(player, space);
        foreach (BoardSpace space in offer.RequestedProperties)
            givenValue += EstimateBotPropertyValue(player, space);

        // Don't accept a deal that leaves the bot unable to cover ordinary costs.
        long resultingCash = (long)player.Money +
            offer.OfferedMoney - offer.RequestedMoney;

        bool liquiditySafe = resultingCash >= 0 &&
            (resultingCash >= GetBotCashReserve(player) ||
             offer.RequestedMoney == 0);

        long tradeTolerance = Mathf.RoundToInt(
            BotLearningSystem.GetPreference(BotLearningSystem.ActionType.Trade) *
            Mathf.Max(100, player.Money / 10));
        if (receivedValue + tradeTolerance > givenValue && liquiditySafe)
        {
            tradeManager.AcceptOffer(player, offer);
            MarkBotTradePairsCompleted(offer);
            BotLearningSystem.RecordAction(player, BotLearningSystem.ActionType.Trade);
            Debug.Log("BOT TRADE: " + player.PlayerName + " accepted a value-positive offer.");
        }
        else
        {
            tradeManager.DeclineOffer(player, offer);
            Debug.Log("BOT TRADE: " + player.PlayerName + " declined an unfavorable offer.");
        }
    }

    private long EstimateBotPropertyValue(BoardPlayer bot, BoardSpace space)
    {
        if (space == null) return 0;
        long value = Mathf.Max(0, space.PurchasePrice);

        if (space.IsMortgaged) value = value * 3 / 4;
        if (space.IsProperty && space.PropertyGroup != null)
        {
            int owned = 0;
            BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
            foreach (BoardSpace candidate in allSpaces)
                if (candidate != null && candidate.PropertyGroup == space.PropertyGroup &&
                    candidate.Owner == bot)
                    owned++;

            int groupSize = space.GetPropertyGroupSize();
            if (space.Owner != bot && owned + 1 >= groupSize)
                value += space.PurchasePrice; // monopoly-completion premium
            if (space.Owner == bot && owned >= groupSize)
                value += space.PurchasePrice / 2; // penalty for breaking a completed group
        }

        return value;
    }

    private string GetBotTradePairKey(BoardSpace first, BoardSpace second)
    {
        if (first == null || second == null)
            return string.Empty;

        int firstId = first.GetInstanceID();
        int secondId = second.GetInstanceID();
        return firstId < secondId
            ? firstId + ":" + secondId
            : secondId + ":" + firstId;
    }

    private bool HasCompletedBotTradePair(TradeOffer offer)
    {
        if (offer == null)
            return false;

        foreach (BoardSpace offered in offer.OfferedProperties)
        {
            foreach (BoardSpace requested in offer.RequestedProperties)
            {
                if (botTradePairsCompletedThisMatch.Contains(
                    GetBotTradePairKey(offered, requested)))
                    return true;
            }
        }

        return false;
    }

    private void MarkBotTradePairsCompleted(TradeOffer offer)
    {
        if (offer == null)
            return;

        foreach (BoardSpace offered in offer.OfferedProperties)
        {
            foreach (BoardSpace requested in offer.RequestedProperties)
            {
                string key = GetBotTradePairKey(offered, requested);
                if (!string.IsNullOrEmpty(key))
                {
                    botTradePairsCompletedThisMatch.Add(key);
                    botTradeOfferCooldownUntil.Remove(key);
                }
            }
        }
    }

    private void TryBotOfferGroupCompletingTrade(BoardPlayer bot)
    {
        TradeManager tradeManager = FindFirstObjectByType<TradeManager>(FindObjectsInactive.Include);
        if (tradeManager == null || tradeManager.HasPendingOffer ||
            !tradeManager.CanOpenTrade(bot) || IsMarketFrozen(bot))
            return;

        BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
        foreach (BoardSpace target in allSpaces)
        {
            if (target == null || !target.IsProperty || target.PropertyGroup == null ||
                target.Owner == null || target.Owner == bot || target.IsMortgaged ||
                target.Houses > 0 || target.HasHotel)
                continue;

            int ownedInGroup = 0;
            foreach (BoardSpace candidate in allSpaces)
                if (candidate != null && candidate.PropertyGroup == target.PropertyGroup &&
                    candidate.Owner == bot)
                    ownedInGroup++;

            if (ownedInGroup == 0 || ownedInGroup + 1 < target.GetPropertyGroupSize())
                continue;

            BoardSpace offered = null;
            int bestDifference = int.MaxValue;
            foreach (BoardSpace candidate in allSpaces)
            {
                if (candidate == null || candidate.Owner != bot || candidate == target ||
                    !candidate.IsProperty || candidate.IsMortgaged ||
                    candidate.Houses > 0 || candidate.HasHotel)
                    continue;

                int difference = Mathf.Abs(candidate.PurchasePrice - target.PurchasePrice);
                if (difference < bestDifference)
                {
                    bestDifference = difference;
                    offered = candidate;
                }
            }

            if (offered == null || bestDifference > Mathf.Max(100, target.PurchasePrice / 2))
                continue;

            string tradeKey = GetBotTradePairKey(offered, target);
            if (botTradePairsCompletedThisMatch.Contains(tradeKey))
                continue;
            if (botTradeOfferCooldownUntil.TryGetValue(tradeKey, out float cooldownUntil) &&
                Time.time < cooldownUntil)
                continue;

            TradeOffer offer = new TradeOffer
            {
                Proposer = bot,
                Receiver = target.Owner,
                OfferedMoney = 0,
                RequestedMoney = 0
            };
            offer.OfferedProperties.Add(offered);
            offer.RequestedProperties.Add(target);

            if (tradeManager.SendOffer(offer))
            {
                // Sending an offer is not a completed trade, so do not train the
                // trade preference from it. Use an unordered pair key so the
                // reverse offer shares the same cooldown.
                botTradeOfferCooldownUntil[tradeKey] = Time.time + 180f;
                Debug.Log("BOT TRADE: " + bot.PlayerName + " offered " +
                    offered.SpaceName + " for " + target.SpaceName +
                    " (retry cooldown: 180 seconds).");
                return; // one strategic offer at a time
            }
        }
    }

    public void EndTurn()
    {
        if (gameOver || endGameVoteInProgress)
            return;

        if (!isTurnActive)
            return;

        if (IsFinancialRecoveryActive(CurrentPlayer))
        {
            return;
        }

        if (waitingForPlayerAction)
        {
            Debug.LogWarning(
                "GameManager: Cannot end turn while " +
                "player action is unresolved."
            );

            return;
        }

        if (CurrentPlayer != null &&
            CurrentPlayer.IsMoving)
        {
            return;
        }

        BoardPlayer endingPlayer =
            CurrentPlayer;

        // A double keeps the same turn alive, so do not
        // record the turn history yet.
        if (consecutiveDoubles == 0 &&
            endingPlayer != null)
        {
            completedTurnsThisMatch++;
            BotLearningSystem.CompleteTurn(
                endingPlayer,
                players,
                FindObjectsByType<BoardSpace>(FindObjectsSortMode.None)
            );
            RecordCompletedTurn(
                endingPlayer
            );
        }

        // --------------------------------------------------------
        // DOUBLE
        // --------------------------------------------------------

        if (consecutiveDoubles > 0)
        {
            isTurnActive = true;

            currentPhase =
                GamePhase.WaitingToRoll;

            Debug.Log(
                $"DOUBLE: {CurrentPlayer.PlayerName} " +
                "gets another roll."
            );

            GameNotificationUI.Show(
                $"{CurrentPlayer.PlayerName} GETS ANOTHER ROLL"
            );

            return;
        }

        // --------------------------------------------------------
        // END NORMAL TURN
        // --------------------------------------------------------

        isTurnActive = false;

        currentPhase =
            GamePhase.TurnEnded;

        if (endingPlayer != null)
        {
            Debug.Log(
                $"TURN ENDED: " +
                $"{endingPlayer.PlayerName}"
            );
        }

        CommunityChestCardManager communityChest =
            FindFirstObjectByType<CommunityChestCardManager>(
                FindObjectsInactive.Include
            );

        if (communityChest != null &&
            endingPlayer != null)
        {
            communityChest.AdvanceMarketFreezeTurn(
                endingPlayer
            );
        }

        // The limit applies only to all-bot training matches. Normal human or
        // mixed matches continue using the existing Monopoly end conditions.
        if (BotLearningSystem.IsTrainingMatch &&
            completedTurnsThisMatch >= BotLearningSystem.MaxTurnsPerMatch)
        {
            EndTrainingMatchAtTurnLimit();
            return;
        }

        MoveToNextPlayer();
    }

    private void RecordCompletedTurn(
        BoardPlayer player)
    {
        if (player == null)
            return;

        if (!turnStartBalances.TryGetValue(
                player,
                out int startBalance))
        {
            startBalance =
                player.Money;
        }

        bool lostMoney =
            player.Money < startBalance;

        if (!turnLossHistory.ContainsKey(player))
        {
            turnLossHistory[player] =
                new Queue<bool>();
        }

        Queue<bool> history =
            turnLossHistory[player];

        history.Enqueue(
            lostMoney
        );

        while (history.Count > 2)
        {
            history.Dequeue();
        }

        Debug.Log(
            $"TURN HISTORY: {player.PlayerName} | " +
            $"START ${startBalance:N0}M | " +
            $"END ${player.Money:N0}M | " +
            $"LOST MONEY: {lostMoney}"
        );
    }

    // Used by Chance #16.
    public bool LostMoneyOnLastTwoTurns(
        BoardPlayer player)
    {
        if (player == null)
            return false;

        if (!turnLossHistory.TryGetValue(
                player,
                out Queue<bool> history))
        {
            return false;
        }

        if (history.Count < 2)
            return false;

        bool[] records =
            history.ToArray();

        return
            records[records.Length - 2] &&
            records[records.Length - 1];
    }

    public int GetCompletedTurnCount(
        BoardPlayer player)
    {
        if (player == null)
            return 0;

        if (!turnLossHistory.TryGetValue(
                player,
                out Queue<bool> history))
        {
            return 0;
        }

        return history.Count;
    }

    private void MoveToNextPlayer()
    {
        if (gameOver || endGameVoteInProgress)
            return;

        consecutiveDoubles = 0;

        if (players == null ||
            players.Length == 0)
        {
            return;
        }

        CheckForWinner();

        if (gameOver)
            return;

        int safety =
            players.Length;

        do
        {
            currentPlayerIndex++;

            if (currentPlayerIndex >=
                players.Length)
            {
                currentPlayerIndex = 0;
            }

            safety--;

            if (safety <= 0)
            {
                CheckForWinner();
                return;
            }

        } while (
            players[currentPlayerIndex] == null ||
            !players[currentPlayerIndex].IsActive
        );

        StartTurn();
    }

    public bool IsPlayerTurn(
        BoardPlayer player)
    {
        return player != null &&
               player.IsActive &&
               player == CurrentPlayer &&
               isTurnActive &&
               !gameOver &&
               !endGameVoteInProgress;
    }

    // ============================================================
    // PLAYER ACTION
    // ============================================================

    public void BeginPlayerAction()
    {
        if (gameOver || endGameVoteInProgress)
            return;

        waitingForPlayerAction = true;

        currentPhase =
            GamePhase.WaitingForPlayerAction;
    }

    public void EndPlayerAction()
    {
        if (gameOver || endGameVoteInProgress)
            return;

        waitingForPlayerAction = false;

        currentPhase =
            GamePhase.WaitingToFinishTurn;
    }

    // ============================================================
    // DICE
    // ============================================================

    public void SetLastDiceRoll(
        int total)
    {
        if (gameOver || endGameVoteInProgress)
            return;

        lastDiceRoll =
            Mathf.Clamp(
                total,
                2,
                12
            );

        currentPhase =
            GamePhase.Moving;
    }

    public void RegisterDouble()
    {
        if (gameOver || endGameVoteInProgress)
            return;

        consecutiveDoubles++;

        Debug.Log(
            $"Doubles rolled. " +
            $"Consecutive doubles: " +
            $"{consecutiveDoubles}"
        );
    }

    public void ResetConsecutiveDoubles()
    {
        consecutiveDoubles = 0;
    }

    // ============================================================
    // BANK
    // ============================================================

    public void AddBankMoney(
        long amount)
    {
        if (amount <= 0)
            return;

        bankMoney += amount;
    }

    public bool RemoveBankMoney(
        long amount)
    {
        if (amount <= 0)
            return false;

        if (bankMoney < amount)
            return false;

        bankMoney -= amount;

        return true;
    }

    // ============================================================
    // BANK -> CURRENT PLAYER
    // ============================================================

    public bool PayPlayer(
        long amount)
    {
        return PaySpecificPlayer(
            CurrentPlayer,
            amount
        );
    }

    // ============================================================
    // BANK -> SPECIFIC PLAYER
    // ============================================================

    public bool PaySpecificPlayer(
        BoardPlayer player,
        long amount)
    {
        if (player == null ||
            amount <= 0 ||
            player.IsBankrupt)
        {
            return false;
        }

        if (!RemoveBankMoney(amount))
        {
            Debug.LogWarning(
                "GameManager: Bank does not have enough money."
            );

            return false;
        }

        int safeAmount =
            amount > int.MaxValue
                ? int.MaxValue
                : (int)amount;

        player.AddMoney(
            safeAmount
        );

        return true;
    }

    // ============================================================
    // PLAYER -> BANK
    // ============================================================

    public bool PlayerPaysBank(
        long amount)
    {
        return PlayerPaysBank(
            CurrentPlayer,
            amount
        );
    }

    public bool PlayerPaysBank(
        BoardPlayer player,
        long amount)
    {
        if (player == null)
            return false;

        if (amount <= 0)
            return true;

        int safeAmount =
            amount > int.MaxValue
                ? int.MaxValue
                : (int)amount;

        if (player.Money < safeAmount)
        {
            GetFinancialRecoveryManager()
                .BeginDebtResolution(
                    player,
                    null,
                    amount,
                    "BANK PAYMENT"
                );

            return false;
        }

        if (!player.RemoveMoney(
                safeAmount))
        {
            return false;
        }

        AddBankMoney(
            amount
        );

        return true;
    }

    // ============================================================
    // PLAYER -> PLAYER
    // ============================================================

    public bool TransferMoneyBetweenPlayers(
        BoardPlayer from,
        BoardPlayer to,
        long amount)
    {
        if (from == null ||
            to == null ||
            from == to ||
            amount <= 0 ||
            from.IsBankrupt ||
            to.IsBankrupt)
        {
            return false;
        }

        int safeAmount =
            amount > int.MaxValue
                ? int.MaxValue
                : (int)amount;

        if (!from.RemoveMoney(
                safeAmount))
        {
            return false;
        }

        to.AddMoney(
            safeAmount
        );

        Debug.Log(
            $"PLAYER TRANSFER: " +
            $"{from.PlayerName} -> " +
            $"{to.PlayerName} | " +
            $"${amount:N0}M"
        );

        return true;
    }

    // ============================================================
    // SPECIFIC PLAYER BANK TRANSACTIONS
    // ============================================================

    public bool PayPlayerForSpecificPlayer(
        BoardPlayer player,
        long amount)
    {
        return PaySpecificPlayer(
            player,
            amount
        );
    }

    public bool PlayerPaysBankForPlayer(
        BoardPlayer player,
        long amount)
    {
        return PlayerPaysBank(
            player,
            amount
        );
    }

    // ============================================================
    // PROPERTY PURCHASE
    // ============================================================

    public bool BuySpace(
        BoardSpace boardSpace)
    {
        if (boardSpace == null)
            return false;

        BoardPlayer buyer =
            CurrentPlayer;

        if (buyer == null)
            return false;

        if (IsFinancialRecoveryActive(buyer))
            return false;

        if (!IsPlayerTurn(buyer))
            return false;

        if (IsMarketFrozen(buyer))
        {
            GameNotificationUI.Show(
                $"{buyer.PlayerName.ToUpperInvariant()} " +
                "CANNOT BUY PROPERTY â€” MARKET FROZEN"
            );

            return false;
        }

        if (!boardSpace.CanBePurchased())
            return false;

        int price =
            boardSpace.PurchasePrice;

        if (buyer.Money < price)
            return false;

        if (!buyer.RemoveMoney(
                price))
        {
            return false;
        }

        AddBankMoney(
            price
        );

        boardSpace.SetOwner(
            buyer
        );

        if (boardSpace.IsProperty)
        {
            boardSpace.RegisterOwnerLanding(
                buyer
            );
        }

        Debug.Log(
            $"PROPERTY PURCHASED: " +
            $"{buyer.PlayerName} -> " +
            $"{boardSpace.SpaceName} " +
            $"for ${price:N0}M"
        );

        GameNotificationUI.Show(
            $"{buyer.PlayerName} PURCHASED " +
            $"{boardSpace.SpaceName.ToUpperInvariant()}"
        );

        return true;
    }

    // ============================================================
    // RENT
    // ============================================================

    public bool PayRent(
        BoardSpace boardSpace)
    {
        if (boardSpace == null)
            return false;

        BoardPlayer payer =
            CurrentPlayer;

        BoardPlayer owner =
            boardSpace.Owner;

        if (payer == null ||
            owner == null)
        {
            return false;
        }

        if (payer == owner)
            return true;

        CardManager cardManager =
            FindFirstObjectByType<CardManager>();

        // --------------------------------------------------------
        // CHANCE — NEXT PROPERTY RENT FREE
        // --------------------------------------------------------

        if (cardManager != null &&
            cardManager.ConsumeNextRentFree(
                payer
            ))
        {
            Debug.Log(
                $"CHANCE RENT SHIELD: " +
                $"{payer.PlayerName} pays $0 rent for " +
                $"{boardSpace.SpaceName}."
            );

            GameNotificationUI.Show(
                $"{payer.PlayerName.ToUpperInvariant()} " +
                $"PAID $0 RENT FOR " +
                $"{boardSpace.SpaceName.ToUpperInvariant()} " +
                "• CHANCE RENT SHIELD"
            );

            return true;
        }

        // --------------------------------------------------------
        // CALCULATE BASE RENT
        // --------------------------------------------------------

        int rent;

        if (boardSpace.IsUtility)
        {
            rent =
                boardSpace.GetUtilityRent(
                    lastDiceRoll
                );
        }
        else
        {
            rent =
                boardSpace.GetRent();
        }

        if (rent <= 0)
            return true;

        // --------------------------------------------------------
        // CHANCE — DOUBLE NEXT RENT
        // --------------------------------------------------------

        if (cardManager != null &&
            cardManager.HasDoubledNextRent(
                boardSpace
            ))
        {
            rent =
                SafeMultiplyRent(
                    rent,
                    2
                );

            cardManager.ConsumeDoubledNextRent(
                boardSpace
            );

            GameNotificationUI.Show(
                $"{boardSpace.SpaceName.ToUpperInvariant()} " +
                $"RENT DOUBLED TO ${rent:N0}M"
            );
        }

        // --------------------------------------------------------
        // CHANCE — BANK PAYS NEXT RENT
        // --------------------------------------------------------

        if (cardManager != null &&
            cardManager.HasBankPaysNextRent(
                payer
            ))
        {
            // Do not consume the protection if the bank cannot
            // actually cover the payment.
            if (bankMoney >= rent)
            {
                cardManager.ConsumeBankPaysNextRent(
                    payer
                );

                RemoveBankMoney(
                    rent
                );

                owner.AddMoney(
                    rent
                );

                Debug.Log(
                    $"CHANCE BANK RENT COVER: " +
                    $"Bank paid ${rent:N0}M to " +
                    $"{owner.PlayerName} " +
                    $"for {boardSpace.SpaceName}."
                );

                GameNotificationUI.Show(
                    $"{owner.PlayerName.ToUpperInvariant()} " +
                    $"RECEIVED ${rent:N0}M RENT FROM THE BANK " +
                    $"FOR {boardSpace.SpaceName.ToUpperInvariant()}"
                );

                return true;
            }

            Debug.LogWarning(
                "GameManager: Bank cannot cover the protected rent."
            );
        }

        // --------------------------------------------------------
        // NORMAL RENT
        // --------------------------------------------------------

        if (payer.Money < rent)
        {
            GetFinancialRecoveryManager()
                .BeginDebtResolution(
                    payer,
                    owner,
                    rent,
                    $"RENT FOR {boardSpace.SpaceName}"
                );

            return false;
        }

        if (!payer.RemoveMoney(
                rent))
        {
            return false;
        }

        owner.AddMoney(
            rent
        );

        Debug.Log(
            $"RENT: {payer.PlayerName} paid " +
            $"${rent:N0}M to {owner.PlayerName} " +
            $"for {boardSpace.SpaceName}"
        );

        GameNotificationUI.Show(
            $"{payer.PlayerName.ToUpperInvariant()} PAID " +
            $"${rent:N0}M RENT TO " +
            $"{owner.PlayerName.ToUpperInvariant()} " +
            $"FOR {boardSpace.SpaceName.ToUpperInvariant()}"
        );

        return true;
    }

    private int SafeMultiplyRent(
        int rent,
        int multiplier)
    {
        long result =
            (long)rent * multiplier;

        return result > int.MaxValue
            ? int.MaxValue
            : (int)result;
    }

    // ============================================================
    // BANKRUPTCY
    // ============================================================

    public void HandleBankruptcy(
        BoardPlayer player,
        BoardPlayer creditor = null)
    {
        if (player == null ||
            player.IsBankrupt)
        {
            return;
        }

        FinancialRecoveryManager recovery =
            GetFinancialRecoveryManager();

        if (recovery.IsRecovering(player))
            return;

        // Compatibility entry point for older callers that only report
        // a zero-balance player. A player is eliminated only when there
        // is no remaining recoverable asset value.
        if (player.Money <= 0 &&
            recovery.GetTotalRecoveryValue(player) <= 0)
        {
            recovery.EliminateNow(
                player,
                creditor,
                "UNRESOLVED DEBT"
            );
        }
    }

    private FinancialRecoveryManager GetFinancialRecoveryManager()
    {
        FinancialRecoveryManager recovery =
            FindFirstObjectByType<FinancialRecoveryManager>(
                FindObjectsInactive.Include
            );

        if (recovery == null)
        {
            recovery =
                gameObject.AddComponent<FinancialRecoveryManager>();
        }

        return recovery;
    }

    // ============================================================
    // WINNER
    // ============================================================

    public int GetActivePlayerCount()
    {
        if (players == null)
            return 0;

        int count = 0;

        foreach (BoardPlayer player in players)
        {
            if (player != null &&
                player.IsActive)
            {
                count++;
            }
        }

        return count;
    }

    public BoardPlayer[] GetActivePlayers()
    {
        if (players == null)
            return System.Array.Empty<BoardPlayer>();

        List<BoardPlayer> active =
            new List<BoardPlayer>();

        foreach (BoardPlayer player in players)
        {
            if (player != null &&
                player.IsActive)
            {
                active.Add(player);
            }
        }

        return active.ToArray();
    }

    public void CheckForWinner()
    {
        // The recovery batch runner deliberately eliminates test players
        // to verify asset settlement. Suppress only the winner UI/state
        // transition for the duration of that isolated, auto-restored batch.
        if (FinancialRecoveryTester.IsBatchTestRunning)
            return;

        if (players == null)
            return;

        BoardPlayer remainingPlayer =
            null;

        int activePlayers =
            0;

        foreach (BoardPlayer player in players)
        {
            if (player == null ||
                !player.IsActive)
            {
                continue;
            }

            activePlayers++;
            remainingPlayer = player;
        }

        if (activePlayers != 1 ||
            remainingPlayer == null)
        {
            return;
        }

        if (gameOver)
            return;

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == remainingPlayer)
            {
                currentPlayerIndex = i;
                break;
            }
        }

        bool restartSelfPlay = BotLearningSystem.AutoRestartEnabled &&
            BotLearningSystem.IsTrainingMatch;
        gameOver = true;
        BotLearningSystem.CompleteMatch(remainingPlayer, players, completedTurnsThisMatch);
        endGameVoteInProgress = false;
        isTurnActive = false;
        waitingForPlayerAction = false;

        currentPhase =
            GamePhase.GameOver;

        Debug.Log(
            "GAME OVER: " +
            remainingPlayer.PlayerName +
            " wins! Active players remaining: 1"
        );

        GameNotificationUI.Show(
            remainingPlayer.PlayerName.ToUpperInvariant() +
            " WINS!"
        );

        MatchEndUI matchEndUI = EnsureMatchEndUI();
        if (matchEndUI != null)
        {
            matchEndUI.ShowFinalResults(
                this,
                remainingPlayer,
                "MATCH WON BY ELIMINATION"
            );
        }

        if (restartSelfPlay)
            StartCoroutine(RestartSelfPlayMatch());
    }

    private void EndTrainingMatchAtTurnLimit()
    {
        if (gameOver || !BotLearningSystem.IsTrainingMatch)
            return;

        BoardSpace[] spaces = FindObjectsByType<BoardSpace>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        BoardPlayer leader = BotLearningSystem.GetHighestNetWorthPlayer(players, spaces);
        if (leader == null)
        {
            Debug.LogWarning("BOT LEARNING: Turn limit reached, but no player could be ranked.");
            return;
        }

        bool restartSelfPlay = BotLearningSystem.AutoRestartEnabled;
        long leaderNetWorth = BotLearningSystem.GetEstimatedNetWorth(leader, spaces);

        // This is an evaluation stop, not a bankruptcy win. It does not alter
        // player assets or mark anyone bankrupt; it only closes this training run.
        gameOver = true;
        endGameVoteInProgress = false;
        isTurnActive = false;
        waitingForPlayerAction = false;
        currentPhase = GamePhase.GameOver;

        BotLearningSystem.CompleteMatch(
            leader,
            players,
            completedTurnsThisMatch,
            true);

        string reason = "TURN LIMIT REACHED — TOP NET WORTH: " +
            leader.PlayerName.ToUpperInvariant() + " ($" +
            leaderNetWorth.ToString("N0") +
            "). EVALUATION ONLY — NOT A BANKRUPTCY WIN.";

        Debug.Log("BOT LEARNING: " + reason +
            " | Completed turns: " + completedTurnsThisMatch +
            " | " + BotLearningSystem.GetPolicySummary());

        MatchEndUI matchEndUI = EnsureMatchEndUI();
        if (matchEndUI != null)
            matchEndUI.ShowFinalResults(this, null, reason);
        else
            GameNotificationUI.Show("BOT EVALUATION COMPLETE");

        if (restartSelfPlay)
            StartCoroutine(RestartSelfPlayMatch());
    }

    private IEnumerator RestartSelfPlayMatch()
    {
        Debug.Log("BOT LEARNING: next self-play match starts in 3 seconds.");
        yield return new WaitForSeconds(3f);
        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.IsValid() && !string.IsNullOrEmpty(activeScene.name))
            SceneManager.LoadScene(activeScene.name);
    }

    public bool StartEndGameVote()
    {
        if (gameOver ||
            endGameVoteInProgress ||
            GetActivePlayerCount() < 2 ||
            waitingForPlayerAction ||
            (CurrentPlayer != null && CurrentPlayer.IsMoving))
        {
            return false;
        }

        endGameVoteInProgress = true;
        Debug.Log("END-GAME VOTE STARTED: unanimous YES votes required.");
        return true;
    }

    public void CancelEndGameVote()
    {
        if (!endGameVoteInProgress || gameOver)
            return;

        endGameVoteInProgress = false;
        Debug.Log("END-GAME VOTE CANCELLED: match resumes.");
    }

    public bool EndGameByUnanimousVote()
    {
        if (gameOver || !endGameVoteInProgress || GetActivePlayerCount() < 2)
            return false;

        endGameVoteInProgress = false;
        gameOver = true;
        isTurnActive = false;
        waitingForPlayerAction = false;
        currentPhase = GamePhase.GameOver;

        Debug.Log("GAME OVER: active players unanimously voted to end the match.");
        GameNotificationUI.Show("MATCH ENDED BY UNANIMOUS VOTE");

        MatchEndUI matchEndUI = EnsureMatchEndUI();
        if (matchEndUI != null)
        {
            matchEndUI.ShowFinalResults(
                this,
                null,
                "ENDED BY UNANIMOUS VOTE"
            );
        }

        return true;
    }

    public bool LeaveGame(BoardPlayer player)
    {
        if (gameOver || endGameVoteInProgress || player == null ||
            !player.IsActive || GetActivePlayerCount() <= 1)
        {
            GameNotificationUI.Show("THE LAST ACTIVE PLAYER CANNOT LEAVE THE MATCH");
            return false;
        }

        if (player != CurrentPlayer)
        {
            GameNotificationUI.Show("ONLY THE CURRENT PLAYER CAN LEAVE THIS LOCAL MATCH");
            Debug.LogWarning("GameManager: Leave request rejected because the requested player is not the current player.");
            return false;
        }

        bool leavingCurrentPlayer = player == CurrentPlayer;

        // Clear any pending recovery/card action before removing the player.
        // This prevents a coroutine or recovery Update from acting on them later.
        FinancialRecoveryManager recovery =
            FindFirstObjectByType<FinancialRecoveryManager>(
                FindObjectsInactive.Include
            );

        if (recovery != null)
            recovery.CancelRecoveryForVoluntaryLeave(player);

        CardManager cardManager =
            FindFirstObjectByType<CardManager>(
                FindObjectsInactive.Include
            );

        if (cardManager != null)
            cardManager.CancelForPlayerLeaving(player);

        TradeManager tradeManager =
            FindFirstObjectByType<TradeManager>(
                FindObjectsInactive.Include
            );

        if (tradeManager != null)
            tradeManager.CancelPendingOfferForPlayer(player);

        LandingActionPanel[] landingPanels =
            FindObjectsByType<LandingActionPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (LandingActionPanel panel in landingPanels)
        {
            if (panel != null)
                panel.HidePanel();
        }

        PropertyManagementPanel[] managementPanels =
            FindObjectsByType<PropertyManagementPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (PropertyManagementPanel panel in managementPanels)
        {
            if (panel != null)
                panel.Hide();
        }

        BoardSpace[] spaces = FindObjectsByType<BoardSpace>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (BoardSpace space in spaces)
        {
            if (space != null && space.Owner == player)
                space.ResetProperty();
        }

        player.MarkLeftGame();
        Debug.Log("PLAYER LEFT MATCH: " + player.PlayerName + ". Owned property was returned to the bank.");

        if (leavingCurrentPlayer)
        {
            isTurnActive = false;
            waitingForPlayerAction = false;
            lastDiceRoll = 0;
            consecutiveDoubles = 0;
            currentPhase = GamePhase.TurnEnded;
        }

        CheckForWinner();

        if (leavingCurrentPlayer && !gameOver)
            MoveToNextPlayer();

        return true;
    }

    // ============================================================
    // PROPERTY DEVELOPMENT
    // ============================================================

    private bool IsPlayerStandingOn(
        BoardPlayer player,
        BoardSpace space)
    {
        if (player == null ||
            space == null)
        {
            return false;
        }

        BoardGenerator boardGenerator =
            FindFirstObjectByType<BoardGenerator>();

        if (boardGenerator == null)
            return false;

        BoardSpace currentSpace =
            boardGenerator.GetSpace(
                player.CurrentSpaceIndex
            );

        return currentSpace == space;
    }

    // ============================================================
    // HOUSE
    // ============================================================

    public bool CanBuildHouse(
        BoardSpace space)
    {
        BoardPlayer player =
            CurrentPlayer;

        if (player == null ||
            space == null)
        {
            return false;
        }

        if (!IsPlayerTurn(player))
            return false;

        // Market Freeze disables ordinary property development.
        if (IsMarketFrozen(player))
            return false;

        if (!IsPlayerStandingOn(
                player,
                space))
        {
            return false;
        }

        return space.CanBuildAnotherHouseFor(
            player
        );
    }

    public bool BuildHouse(
        BoardSpace space)
    {
        BoardPlayer player =
            CurrentPlayer;

        if (IsFinancialRecoveryActive(player))
            return false;

        if (!CanBuildHouse(space))
            return false;

        if (IsMarketFrozen(player))
        {
            GameNotificationUI.Show(
                $"{player.PlayerName.ToUpperInvariant()} " +
                "CANNOT BUILD â€” MARKET FROZEN"
            );

            return false;
        }

        if (!space.AddHouse(player))
            return false;

        Debug.Log(
            $"HOUSE BUILT: " +
            $"{player.PlayerName} -> " +
            $"{space.SpaceName} " +
            $"({space.Houses}/4)"
        );

        GameNotificationUI.Show(
            $"{player.PlayerName} BUILT HOUSE " +
            $"{space.Houses}/4 ON " +
            $"{space.SpaceName.ToUpperInvariant()}"
        );

        return true;
    }

    // ============================================================
    // HOTEL
    // ============================================================

    public bool CanBuildHotel(
        BoardSpace space)
    {
        BoardPlayer player =
            CurrentPlayer;

        if (player == null ||
            space == null)
        {
            return false;
        }

        if (!IsPlayerTurn(player))
            return false;

        // Market Freeze disables ordinary property development.
        if (IsMarketFrozen(player))
            return false;

        if (!IsPlayerStandingOn(
                player,
                space))
        {
            return false;
        }

        return space.CanBuildHotelFor(
            player
        );
    }

    public bool BuildHotel(
        BoardSpace space)
    {
        BoardPlayer player =
            CurrentPlayer;

        if (IsFinancialRecoveryActive(player))
            return false;

        if (!CanBuildHotel(space))
            return false;

        if (IsMarketFrozen(player))
        {
            GameNotificationUI.Show(
                $"{player.PlayerName.ToUpperInvariant()} " +
                "CANNOT BUILD â€” MARKET FROZEN"
            );

            return false;
        }

        if (!space.AddHotel(player))
            return false;

        Debug.Log(
            $"HOTEL BUILT: " +
            $"{player.PlayerName} -> " +
            $"{space.SpaceName}"
        );

        GameNotificationUI.Show(
            $"{player.PlayerName} BUILT HOTEL ON " +
            $"{space.SpaceName.ToUpperInvariant()}"
        );

        return true;
    }

    // ============================================================
    // MORTGAGE
    // ============================================================

    public bool MortgageProperty(
        BoardSpace space)
    {
        BoardPlayer player =
            CurrentPlayer;

        if (player == null ||
            space == null)
        {
            return false;
        }

        if (IsMarketFrozen(player) &&
            !IsFinancialRecoveryActive(player))
        {
            GameNotificationUI.Show(
                $"{player.PlayerName.ToUpperInvariant()} " +
                "CANNOT MORTGAGE â€” MARKET FROZEN"
            );

            return false;
        }

        if (space.Owner != player)
            return false;

        bool success =
            space.Mortgage();

        if (success)
        {
            Debug.Log(
                $"MORTGAGED: " +
                $"{player.PlayerName} -> " +
                $"{space.SpaceName}"
            );
        }

        return success;
    }

    public bool UnmortgageProperty(
        BoardSpace space)
    {
        BoardPlayer player =
            CurrentPlayer;

        if (player == null ||
            space == null)
        {
            return false;
        }

        if (IsFinancialRecoveryActive(player))
            return false;

        if (IsMarketFrozen(player))
        {
            GameNotificationUI.Show(
                $"{player.PlayerName.ToUpperInvariant()} " +
                "CANNOT UNMORTGAGE â€” MARKET FROZEN"
            );

            return false;
        }

        if (space.Owner != player)
            return false;

        bool success =
            space.Unmortgage();

        if (success)
        {
            Debug.Log(
                $"UNMORTGAGED: " +
                $"{player.PlayerName} -> " +
                $"{space.SpaceName}"
            );
        }

        return success;
    }




#if UNITY_EDITOR || DEVELOPMENT_BUILD

// ============================================================
// RENT TESTER SUPPORT — DEVELOPMENT ONLY
// ============================================================

public bool BeginRentTesterTurn(BoardPlayer player)
{
    if (player == null)
        return false;

    if (players == null ||
        players.Length == 0)
    {
        return false;
    }

    int index =
        System.Array.IndexOf(
            players,
            player
        );

    if (index < 0)
        return false;

    currentPlayerIndex = index;

    // The tester needs a valid active turn so that
    // the real BuySpace / BuildHouse / BuildHotel /
    // MoveBySteps validation accepts this player.
    isTurnActive = true;

    waitingForPlayerAction = false;

    currentPhase =
        GamePhase.WaitingToRoll;

    consecutiveDoubles = 0;
    lastDiceRoll = 0;

    return true;
}

#endif


}

// ================================================================
// GAME PHASE
// ================================================================

public enum GamePhase
{
    WaitingToRoll,
    Moving,
    WaitingForPlayerAction,
    WaitingToFinishTurn,
    TurnEnded,
    GameOver
}
