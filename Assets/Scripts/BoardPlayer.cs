using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BoardPlayer : MonoBehaviour
{
    [Header("Player Identity")]
    [SerializeField] private int playerNumber = 1;
    [SerializeField] private string playerName = "Player";

    public int PlayerNumber => playerNumber;

    public string PlayerName =>
        string.IsNullOrWhiteSpace(playerName)
            ? $"Player {playerNumber}"
            : playerName;

    [Header("Player Appearance")]
    [SerializeField] private Color tokenColor = Color.red;

    public Color TokenColor =>
        tokenColor;

    [Header("Player Type")]
    [SerializeField] private bool isBot;

    public bool IsBot =>
        isBot;

    [Header("Board")]
    [SerializeField] private BoardGenerator boardGenerator;
    [SerializeField] private int startingSpaceIndex = 0;
    [SerializeField] private int currentSpaceIndex = 0;

    private const int BoardSize = 40;
    private const int GoIndex = 0;
    private const int JailIndex = 10;

    [Header("Movement")]
    [SerializeField]
    [Min(0.01f)]
    private float moveDurationPerSpace = 0.15f;

    [SerializeField] private bool isMoving;

    [Header("Money")]
    [SerializeField]
    [Min(0)]
    private int startingMoney = 1500;

    [SerializeField]
    [Min(0)]
    private int money = 1500;

    [Header("Jail")]
    [SerializeField] private bool inJail;
    [SerializeField] private int jailTurnsRemaining;

    [Header("State")]
    [SerializeField] private bool bankrupt;
    [SerializeField] private bool initialized;

    public int CurrentSpaceIndex => currentSpaceIndex;
    public int Money => money;
    public bool IsMoving => isMoving;
    public bool IsBankrupt => bankrupt;
    public bool IsInitialized => initialized;
    public bool IsInJail => inJail;
    public int JailTurnsRemaining => jailTurnsRemaining;

    private void Start()
    {
        // GameManager is the only initialization authority.
    }

    // ============================================================
    // CONFIGURATION
    // ============================================================

    public void SetPlayerNumber(int number)
    {
        playerNumber =
            Mathf.Max(1, number);

        if (string.IsNullOrWhiteSpace(playerName) ||
            playerName == "Player")
        {
            playerName =
                $"Player {playerNumber}";
        }
    }

    public void SetPlayerName(string newName)
    {
        playerName =
            string.IsNullOrWhiteSpace(newName)
                ? $"Player {playerNumber}"
                : newName.Trim();
    }

    public void SetTokenColor(Color newColor)
    {
        tokenColor =
            newColor;

        ApplyTokenColor();
    }

    public void SetIsBot(bool value)
    {
        isBot = value;
    }

    // ============================================================
    // TOKEN COLOR
    // ============================================================

    private void ApplyTokenColor()
    {
        SpriteRenderer[] renderers =
            GetComponentsInChildren<SpriteRenderer>(
                true
            );

        foreach (SpriteRenderer renderer in renderers)
        {
            if (renderer == null)
                continue;

            renderer.color =
                tokenColor;
        }

        Image[] images =
            GetComponentsInChildren<Image>(
                true
            );

        foreach (Image image in images)
        {
            if (image == null)
                continue;

            image.color =
                tokenColor;
        }
    }

    // ============================================================
    // RESET FOR NEW GAME
    // ============================================================

    public void ResetForNewGame()
    {
        currentSpaceIndex =
            Mathf.Clamp(
                startingSpaceIndex,
                0,
                BoardSize - 1
            );

        isMoving = false;

        money = startingMoney;

        inJail = false;
        jailTurnsRemaining = 0;

        bankrupt = false;
        initialized = false;
    }

    // ============================================================
    // INITIALIZATION
    // ============================================================

    public void Initialize()
    {
        if (initialized)
            return;

        if (boardGenerator == null)
        {
            boardGenerator =
                FindFirstObjectByType<BoardGenerator>();
        }

        currentSpaceIndex =
            Mathf.Clamp(
                startingSpaceIndex,
                0,
                BoardSize - 1
            );

        if (money <= 0)
            money = startingMoney;

        isMoving = false;
        bankrupt = false;
        inJail = false;
        jailTurnsRemaining = 0;
        initialized = true;

        SnapToCurrentSpace();

        ApplyTokenColor();

        Debug.Log(
            $"{PlayerName} initialized at Space " +
            $"{currentSpaceIndex} with ${money:N0}M. " +
            $"Type: {(isBot ? "BOT" : "HUMAN")}."
        );
    }

    // ============================================================
    // MOVEMENT
    // ============================================================

    public void MoveBySteps(int steps)
    {
        if (steps <= 0 ||
            isMoving ||
            bankrupt)
        {
            return;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null ||
            !gameManager.IsPlayerTurn(this))
        {
            return;
        }

        StartCoroutine(
            MoveRoutine(steps)
        );
    }

    private IEnumerator MoveRoutine(int steps)
    {
        isMoving = true;

        if (boardGenerator == null)
        {
            boardGenerator =
                FindFirstObjectByType<BoardGenerator>();
        }

        if (boardGenerator == null)
        {
            isMoving = false;
            yield break;
        }

        for (int i = 0; i < steps; i++)
        {
            int previousIndex =
                currentSpaceIndex;

            bool passedGo =
                currentSpaceIndex + 1 >= BoardSize;

            currentSpaceIndex =
                passedGo
                    ? GoIndex
                    : currentSpaceIndex + 1;

            BoardSpace space =
                boardGenerator.GetSpace(
                    currentSpaceIndex
                );

            if (space != null)
            {
                yield return MoveTokenTo(
                    space.transform
                );
            }

            if (passedGo)
            {
                GameManager gameManager =
                    FindFirstObjectByType<GameManager>();

                if (gameManager != null)
                {
                    bool paid =
                        gameManager.PayPlayer(200);

                    if (paid)
                    {
                        GameNotificationUI.Show(
                            $"{PlayerName} RECEIVED $200M FOR PASSING GO"
                        );
                    }
                }
            }

            Debug.Log(
                $"{PlayerName}: " +
                $"{previousIndex} -> {currentSpaceIndex}"
            );
        }

        isMoving = false;

        LandOnCurrentSpace();
    }

    private IEnumerator MoveTokenTo(
        Transform target)
    {
        if (target == null)
            yield break;

        transform.SetParent(null, true);

        Vector3 startPosition =
            transform.position;

        Vector3 targetPosition =
            target.position;

        float duration =
            Mathf.Max(
                0.01f,
                moveDurationPerSpace
            );

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float progress =
                Mathf.Clamp01(
                    elapsed / duration
                );

            progress =
                progress * progress *
                (3f - 2f * progress);

            transform.position =
                Vector3.Lerp(
                    startPosition,
                    targetPosition,
                    progress
                );

            yield return null;
        }

        transform.position =
            targetPosition;

        transform.SetParent(
            target,
            true
        );
    }

    public void SnapToCurrentSpace()
    {
        if (boardGenerator == null)
        {
            boardGenerator =
                FindFirstObjectByType<BoardGenerator>();
        }

        if (boardGenerator == null)
            return;

        BoardSpace space =
            boardGenerator.GetSpace(
                currentSpaceIndex
            );

        if (space == null)
            return;

        transform.position =
            space.transform.position;

        transform.SetParent(
            space.transform,
            true
        );
    }

    // ============================================================
    // LANDING
    // ============================================================

    public void LandOnCurrentSpace()
    {
        if (bankrupt)
            return;

        if (boardGenerator == null)
        {
            boardGenerator =
                FindFirstObjectByType<BoardGenerator>();
        }

        if (boardGenerator == null)
            return;

        BoardSpace space =
            boardGenerator.GetSpace(
                currentSpaceIndex
            );

        if (space == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        Debug.Log(
            $"{PlayerName} landed on " +
            $"{space.SpaceName}."
        );

        bool purchasable =
            space.SpaceType == BoardSpaceType.Property ||
            space.SpaceType == BoardSpaceType.Airport ||
            space.SpaceType == BoardSpaceType.Utility;

        if (purchasable)
        {
            if (!space.IsOwned)
            {
                gameManager.BeginPlayerAction();

                GameNotificationUI.Show(
                    $"{PlayerName} LANDED ON " +
                    $"{space.SpaceName.ToUpperInvariant()}"
                );

                OpenLandingAction(space);

                return;
            }

            if (space.Owner != this)
            {
                int moneyBefore =
                    Money;

                bool rentPaid =
                    gameManager.PayRent(space);

                if (rentPaid)
                {
                    int rentPaidAmount =
                        moneyBefore - Money;

                    GameNotificationUI.Show(
                        $"{PlayerName} PAID " +
                        $"${rentPaidAmount:N0}M RENT"
                    );
                }

                FinishLanding();

                return;
            }

            if (space.IsProperty)
            {
                space.RegisterOwnerLanding(this);

                GameNotificationUI.Show(
                    $"{PlayerName} LANDED ON THEIR OWN " +
                    $"PROPERTY: {space.SpaceName.ToUpperInvariant()}"
                );

                gameManager.BeginPlayerAction();

                OpenPropertyManagementAction(space);

                return;
            }

            GameNotificationUI.Show(
                $"{PlayerName} LANDED ON THEIR OWN " +
                $"SPACE: {space.SpaceName.ToUpperInvariant()}"
            );

            FinishLanding();

            return;
        }

        if (space.SpaceType == BoardSpaceType.Tax)
        {
            long tax =
                space.SpaceName
                    .ToUpperInvariant()
                    .Contains("INCOME")
                    ? 200
                    : 100;

            bool paid =
                gameManager.PlayerPaysBank(tax);

            if (!paid)
            {
                GameNotificationUI.Show(
                    $"{PlayerName} CANNOT PAY " +
                    $"${tax:N0}M TAX"
                );

                gameManager.HandleBankruptcy(
                    this
                );

                return;
            }

            GameNotificationUI.Show(
                $"{PlayerName} PAID " +
                $"${tax:N0}M TAX"
            );

            FinishLanding();
            return;
        }

        if (space.SpaceType ==
            BoardSpaceType.GoToJail)
        {
            SendToJail();

            GameNotificationUI.Show(
                $"{PlayerName} WAS SENT TO JAIL"
            );

            FinishLanding();
            return;
        }

        if (space.SpaceType ==
            BoardSpaceType.Chance)
        {
            gameManager.BeginPlayerAction();

            GameNotificationUI.Show(
                "CHANCE"
            );

            CardManager cardManager =
                FindFirstObjectByType<CardManager>();

            if (cardManager != null)
            {
                cardManager.DrawChanceCard(
                    this
                );
            }
            else
            {
                Debug.LogError(
                    "BoardPlayer: CardManager not found."
                );

                gameManager.EndPlayerAction();
                FinishLanding();
            }

            return;
        }

        if (space.SpaceType ==
            BoardSpaceType.CommunityChest)
        {
            gameManager.BeginPlayerAction();

            GameNotificationUI.Show(
                "COMMUNITY CHEST"
            );

            CardManager cardManager =
                FindFirstObjectByType<CardManager>();

            if (cardManager != null)
            {
                cardManager.DrawCommunityChestCard(
                    this
                );
            }
            else
            {
                Debug.LogError(
                    "BoardPlayer: CardManager not found."
                );

                gameManager.EndPlayerAction();
                FinishLanding();
            }

            return;
        }

        FinishLanding();
    }

    private void OpenLandingAction(
        BoardSpace space)
    {
        LandingActionPanel[] panels =
            FindObjectsByType<LandingActionPanel>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        if (panels == null ||
            panels.Length == 0)
        {
            Debug.LogError(
                "BoardPlayer: LandingActionPanel does not exist."
            );

            return;
        }

        panels[0].ShowLanding(space);
    }

    private void OpenPropertyManagementAction(
        BoardSpace space)
    {
        PropertyManagementPanel management =
            FindFirstObjectByType<PropertyManagementPanel>(
                FindObjectsInactive.Include
            );

        if (management == null)
        {
            Debug.LogError(
                "BoardPlayer: PropertyManagementPanel not found."
            );

            GameManager gameManager =
                FindFirstObjectByType<GameManager>();

            if (gameManager != null)
            {
                gameManager.EndPlayerAction();
                FinishLanding();
            }

            return;
        }

        management.ShowForLanding(space);
    }

    private void FinishLanding()
    {
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        if (gameManager.WaitingForPlayerAction)
            return;

        gameManager.EndTurn();
    }

    // ============================================================
    // JAIL
    // ============================================================

    public void SendToJail()
    {
        currentSpaceIndex =
            JailIndex;

        inJail = true;
        jailTurnsRemaining = 3;

        SnapToCurrentSpace();

        Debug.Log(
            $"{PlayerName} was sent to Jail."
        );
    }

    public void LeaveJail()
    {
        inJail = false;
        jailTurnsRemaining = 0;
    }

    public void DecreaseJailTurn()
    {
        if (!inJail)
            return;

        jailTurnsRemaining =
            Mathf.Max(
                0,
                jailTurnsRemaining - 1
            );

        if (jailTurnsRemaining == 0)
            LeaveJail();
    }

    // ============================================================
    // MONEY
    // ============================================================

    public void AddMoney(int amount)
    {
        if (amount <= 0 ||
            bankrupt)
        {
            return;
        }

        long result =
            (long)money + amount;

        money =
            result > int.MaxValue
                ? int.MaxValue
                : (int)result;

        Debug.Log(
            $"{PlayerName} balance: " +
            $"${money:N0}M."
        );
    }

    public bool RemoveMoney(int amount)
    {
        if (amount <= 0 ||
            bankrupt)
        {
            return false;
        }

        if (money < amount)
            return false;

        money -= amount;

        Debug.Log(
            $"{PlayerName} balance: " +
            $"${money:N0}M."
        );

        if (money <= 0)
        {
            bankrupt = true;

            GameNotificationUI.Show(
                $"{PlayerName} IS BANKRUPT"
            );

            GameManager gameManager =
                FindFirstObjectByType<GameManager>();

            if (gameManager != null)
                gameManager.CheckForWinner();
        }

        return true;
    }

    public void SetMoney(int amount)
    {
        money =
            Mathf.Max(
                0,
                amount
            );

        bankrupt =
            money <= 0;
    }
}