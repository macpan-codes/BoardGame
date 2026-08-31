using System.Collections.Generic;
using UnityEngine;

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

    [Header("Dice")]
    [SerializeField] private int lastDiceRoll;
    [SerializeField] private int consecutiveDoubles;

    [Header("Bank")]
    [SerializeField] private long bankMoney = 20580;

    [Header("Game State")]
    [SerializeField] private bool gameStarted;
    [SerializeField] private bool gameOver;

    private bool playersConfigured;

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

    public long BankMoney =>
        bankMoney;

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
        InitializeGame();
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

        playersConfigured = true;

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

        for (int i = 0; i < players.Length; i++)
        {
            if (players[i] == null)
                continue;

            players[i].SetPlayerNumber(
                i + 1
            );

            players[i].Initialize();
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
        if (gameOver)
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

        if (player.IsBankrupt)
        {
            MoveToNextPlayer();
            return;
        }

        isTurnActive = true;
        waitingForPlayerAction = false;

        currentPhase =
            GamePhase.WaitingToRoll;

        lastDiceRoll = 0;

        Debug.Log(
            $"TURN STARTED: {player.PlayerName}"
        );

        GameNotificationUI.Show(
            $"{player.PlayerName}'S TURN"
        );
    }

    public void EndTurn()
    {
        if (gameOver ||
            !isTurnActive)
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

        isTurnActive = false;

        currentPhase =
            GamePhase.TurnEnded;

        if (CurrentPlayer != null)
        {
            Debug.Log(
                $"TURN ENDED: " +
                $"{CurrentPlayer.PlayerName}"
            );
        }

        MoveToNextPlayer();
    }

    private void MoveToNextPlayer()
    {
        consecutiveDoubles = 0;

        if (players == null ||
            players.Length == 0)
        {
            return;
        }

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
            players[currentPlayerIndex].IsBankrupt
        );

        StartTurn();
    }

    public bool IsPlayerTurn(
        BoardPlayer player)
    {
        return player != null &&
               player == CurrentPlayer &&
               isTurnActive &&
               !gameOver;
    }

    // ============================================================
    // PLAYER ACTION
    // ============================================================

    public void BeginPlayerAction()
    {
        if (gameOver)
            return;

        waitingForPlayerAction = true;

        currentPhase =
            GamePhase.WaitingForPlayerAction;
    }

    public void EndPlayerAction()
    {
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
        lastDiceRoll =
            Mathf.Clamp(total, 2, 12);

        currentPhase =
            GamePhase.Moving;
    }

    public void RegisterDouble()
    {
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
    // BANK -> PLAYER
    // ============================================================

    public bool PayPlayer(
        long amount)
    {
        if (amount <= 0)
            return false;

        BoardPlayer player =
            CurrentPlayer;

        if (player == null)
            return false;

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
        BoardPlayer player =
            CurrentPlayer;

        if (player == null)
            return false;

        if (amount <= 0)
            return true;

        int safeAmount =
            amount > int.MaxValue
                ? int.MaxValue
                : (int)amount;

        if (!player.RemoveMoney(
                safeAmount))
        {
            return false;
        }

        AddBankMoney(amount);

        return true;
    }

    // ============================================================
    // SPECIFIC PLAYER BANK TRANSACTIONS
    // ============================================================

    public bool PayPlayerForSpecificPlayer(
        BoardPlayer player,
        long amount)
    {
        if (player == null ||
            amount <= 0)
        {
            return false;
        }

        if (bankMoney < amount)
            return false;

        bankMoney -= amount;

        int safeAmount =
            amount > int.MaxValue
                ? int.MaxValue
                : (int)amount;

        player.AddMoney(
            safeAmount
        );

        return true;
    }

    public bool PlayerPaysBankForPlayer(
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

        if (!player.RemoveMoney(
                safeAmount))
        {
            return false;
        }

        bankMoney += amount;

        return true;
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

        if (!IsPlayerTurn(buyer))
            return false;

        if (!boardSpace.CanBePurchased())
            return false;

        int price =
            boardSpace.PurchasePrice;

        if (buyer.Money < price)
            return false;

        if (!buyer.RemoveMoney(price))
            return false;

        AddBankMoney(price);

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

        if (payer.Money < rent)
        {
            HandleBankruptcy(
                payer,
                owner
            );

            return false;
        }

        if (!payer.RemoveMoney(rent))
            return false;

        owner.AddMoney(rent);

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

        player.SetMoney(0);

        GameNotificationUI.Show(
            $"{player.PlayerName} IS BANKRUPT"
        );

        CheckForWinner();

        if (!gameOver)
        {
            MoveToNextPlayer();
        }
    }

    // ============================================================
    // WINNER
    // ============================================================

    public void CheckForWinner()
    {
        if (players == null)
            return;

        BoardPlayer remainingPlayer =
            null;

        int alivePlayers = 0;

        foreach (BoardPlayer player in players)
        {
            if (player == null ||
                player.IsBankrupt)
            {
                continue;
            }

            alivePlayers++;
            remainingPlayer = player;
        }

        if (alivePlayers == 1 &&
            remainingPlayer != null)
        {
            gameOver = true;
            isTurnActive = false;
            waitingForPlayerAction = false;

            currentPhase =
                GamePhase.GameOver;

            Debug.Log(
                $"GAME OVER: " +
                $"{remainingPlayer.PlayerName} wins!"
            );

            GameNotificationUI.Show(
                $"{remainingPlayer.PlayerName.ToUpperInvariant()} WINS!"
            );
        }
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

        if (!CanBuildHouse(space))
            return false;

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

        if (!CanBuildHotel(space))
            return false;

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