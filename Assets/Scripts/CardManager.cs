using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    // ============================================================
    // CARD PREVIEW
    // ============================================================

    [Serializable]
    public class CardPreview
    {
        public string title;
        public string description;

        public CardPreview(string title, string description)
        {
            this.title = title;
            this.description = description;
        }
    }

    // ============================================================
    // EFFECT TYPES
    // ============================================================

    private enum CardEffect
    {
        CollectFromEveryPlayer,
        ReceiveMoney,
        FreeUpgrade,
        FreeProperty,
        ReceiveMoneyAndRentProtection,
        MoveToGoAndCollect,
        DoubleNextRent,
        DiscountedPropertyPurchase50,
        BankPaysNextRent,
        MoneyPerProperty,
        MoveToAnyPropertyNoRent,
        GetOutOfJailFree,
        MoneyOrFreeHouse,
        MoneyPerCompleteSet,
        ConditionalPropertyCountReward,
        LossStreakReward,
        MoveForwardAndCollect,
        TakeMoneyFromPlayer,
        NearestPropertyPurchase75,
        LuckyBreak
    }

    [Serializable]
    private class CardDefinition
    {
        public string title;

        [TextArea(2, 5)]
        public string description;

        [Min(0.01f)]
        public float weight = 1f;

        public CardEffect effect;

        // All money values are whole millions in the existing game economy.
        public int amount;
        public int secondaryAmount;
        public int movement;

        [Range(1, 100)]
        public int percentage = 100;
    }

    // ============================================================
    // DECKS
    // ============================================================

    [Header("Chance Cards")]
    [SerializeField]
    private List<CardDefinition> chanceCards =
        new List<CardDefinition>();

    [Header("Community Chest Cards")]
    [SerializeField]
    private List<CardDefinition> communityChestCards =
        new List<CardDefinition>();

    [Header("Built-in Chance Deck")]
    [SerializeField]
    private int chanceDeckVersion = 3;

    private const int CurrentChanceDeckVersion = 3;

    // ============================================================
    // UI
    // ============================================================

    [Header("Chance UI")]
    [SerializeField]
    private ChanceCardUI chanceCardUI;

    // ============================================================
    // ACTIVE CARD STATE
    // ============================================================

    private BoardPlayer activePlayer;
    private CardDefinition activeCard;
    private List<CardDefinition> activeDeck;

    private bool waitingForChoice;
    private bool waitingForPlayerTarget;
    private bool waitingForPropertyTarget;

    private readonly List<BoardPlayer> playerTargets =
        new List<BoardPlayer>();

    private readonly List<BoardSpace> propertyTargets =
        new List<BoardSpace>();

    private int targetIndex;

    private BoardPlayer selectedPlayerTarget;
    private BoardSpace selectedPropertyTarget;

    // ============================================================
    // PERSISTENT CHANCE EFFECTS
    // ============================================================

    private readonly HashSet<BoardPlayer> rentFreePlayers =
        new HashSet<BoardPlayer>();

    private readonly HashSet<BoardPlayer> bankRentPlayers =
        new HashSet<BoardPlayer>();

    private readonly HashSet<BoardSpace> doubledRentProperties =
        new HashSet<BoardSpace>();

    private readonly Dictionary<BoardPlayer, int> jailFreeCards =
        new Dictionary<BoardPlayer, int>();

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        EnsureChanceCards();

        if (chanceCardUI == null)
        {
            chanceCardUI =
                FindFirstObjectByType<ChanceCardUI>(
                    FindObjectsInactive.Include
                );
        }

        if (chanceCardUI == null)
        {
            Debug.LogError(
                "CardManager: ChanceCardUI could not be found. " +
                "Assign it in the Inspector or place ChanceCardUI in the scene."
            );
            return;
        }

        chanceCardUI.Initialize(this);
        chanceCardUI.HideImmediate();
    }

    // ============================================================
    // DRAW PUBLIC API
    // ============================================================

    public void DrawChanceCard(BoardPlayer player)
    {
        DrawCard(player, chanceCards, "CHANCE");
    }

    // Community Chest remains separate. Its visual system will be built later.
    public void DrawCommunityChestCard(BoardPlayer player)
    {
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (communityChestCards == null ||
            communityChestCards.Count == 0)
        {
            Debug.LogWarning(
                "CardManager: Community Chest is not configured yet."
            );

            if (gameManager != null)
            {
                gameManager.EndPlayerAction();
                gameManager.EndTurn();
            }

            return;
        }

        Debug.LogWarning(
            "CardManager: Community Chest deck exists, but its UI is not implemented yet."
        );

        if (gameManager != null)
        {
            gameManager.EndPlayerAction();
            gameManager.EndTurn();
        }
    }

    private void DrawCard(
        BoardPlayer player,
        List<CardDefinition> deck,
        string deckName)
    {
        if (player == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogError("CardManager: GameManager not found.");
            return;
        }

        if (chanceCardUI == null)
        {
            Debug.LogError("CardManager: ChanceCardUI not found.");
            gameManager.EndPlayerAction();
            gameManager.EndTurn();
            return;
        }

        if (deck == null || deck.Count == 0)
        {
            Debug.LogWarning($"{deckName}: No cards available.");
            gameManager.EndPlayerAction();
            gameManager.EndTurn();
            return;
        }

        CardDefinition selected =
            SelectWeightedCard(deck);

        if (selected == null)
        {
            Debug.LogError($"{deckName}: Could not select a card.");
            gameManager.EndPlayerAction();
            gameManager.EndTurn();
            return;
        }

        activePlayer = player;
        activeCard = selected;
        activeDeck = deck;

        waitingForChoice = false;
        waitingForPlayerTarget = false;
        waitingForPropertyTarget = false;

        playerTargets.Clear();
        propertyTargets.Clear();

        selectedPlayerTarget = null;
        selectedPropertyTarget = null;
        targetIndex = 0;

        Debug.Log(
            $"{deckName}: {selected.title} - {selected.description}"
        );

        List<CardPreview> previews =
            BuildRollPreviews(
                deck,
                selected,
                7
            );

        CardPreview finalPreview =
            new CardPreview(
                selected.title,
                selected.description
            );

        chanceCardUI.BeginCardReveal(
            player,
            deckName,
            previews,
            finalPreview,
            () => ResolveCard(
                player,
                selected,
                gameManager
            )
        );
    }

    // ============================================================
    // WEIGHTED CARD SELECTION
    // ============================================================

    private CardDefinition SelectWeightedCard(
        List<CardDefinition> deck)
    {
        float totalWeight = 0f;

        foreach (CardDefinition card in deck)
        {
            if (card == null)
                continue;

            totalWeight += Mathf.Max(0.01f, card.weight);
        }

        if (totalWeight <= 0f)
            return null;

        float roll =
            UnityEngine.Random.Range(0f, totalWeight);

        float cumulative = 0f;

        foreach (CardDefinition card in deck)
        {
            if (card == null)
                continue;

            cumulative += Mathf.Max(0.01f, card.weight);

            if (roll < cumulative)
                return card;
        }

        for (int i = deck.Count - 1; i >= 0; i--)
        {
            if (deck[i] != null)
                return deck[i];
        }

        return null;
    }

    private List<CardPreview> BuildRollPreviews(
        List<CardDefinition> deck,
        CardDefinition finalCard,
        int count)
    {
        List<CardPreview> previews =
            new List<CardPreview>();

        List<CardDefinition> candidates =
            new List<CardDefinition>();

        foreach (CardDefinition card in deck)
        {
            if (card == null || card == finalCard)
                continue;

            candidates.Add(card);
        }

        if (candidates.Count == 0)
            return previews;

        int safeCount = Mathf.Max(1, count);

        for (int i = 0; i < safeCount; i++)
        {
            CardDefinition preview =
                candidates[
                    UnityEngine.Random.Range(
                        0,
                        candidates.Count
                    )
                ];

            previews.Add(
                new CardPreview(
                    preview.title,
                    preview.description
                )
            );
        }

        return previews;
    }

    // ============================================================
    // CARD RESOLUTION
    // ============================================================

    private void ResolveCard(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        switch (card.effect)
        {
            case CardEffect.CollectFromEveryPlayer:
                ResolveCollectFromEveryPlayer(player, card, gameManager);
                break;

            case CardEffect.ReceiveMoney:
                ResolveReceiveMoney(player, card, gameManager);
                break;

            case CardEffect.FreeUpgrade:
                ResolveFreeUpgrade(player);
                break;

            case CardEffect.FreeProperty:
                PreparePropertyTarget(player, card, true);
                break;

            case CardEffect.ReceiveMoneyAndRentProtection:
                ResolveReceiveMoneyAndProtection(player, card, gameManager);
                break;

            case CardEffect.MoveToGoAndCollect:
                ResolveMoveToGoAndCollect(player, card, gameManager);
                break;

            case CardEffect.DoubleNextRent:
                PreparePropertyTarget(player, card, false);
                break;

            case CardEffect.DiscountedPropertyPurchase50:
                PreparePropertyPurchaseTarget(player, 50);
                break;

            case CardEffect.BankPaysNextRent:
                ResolveBankPaysNextRent(player);
                break;

            case CardEffect.MoneyPerProperty:
                ResolveMoneyPerProperty(player, card, gameManager);
                break;

            case CardEffect.MoveToAnyPropertyNoRent:
                PrepareAnyPropertyTarget(player, card);
                break;

            case CardEffect.GetOutOfJailFree:
                ResolveGetOutOfJailFree(player);
                break;

            case CardEffect.MoneyOrFreeHouse:
                ResolveMoneyOrFreeHouse(player);
                break;

            case CardEffect.MoneyPerCompleteSet:
                ResolveMoneyPerCompleteSet(player, card, gameManager);
                break;

            case CardEffect.ConditionalPropertyCountReward:
                ResolvePropertyCountReward(player, card, gameManager);
                break;

            case CardEffect.LossStreakReward:
                ResolveLossStreakReward(player, card, gameManager);
                break;

            case CardEffect.MoveForwardAndCollect:
                ResolveMoveForwardAndCollect(player, card, gameManager);
                break;

            case CardEffect.TakeMoneyFromPlayer:
                PreparePlayerTarget(player, card);
                break;

            case CardEffect.NearestPropertyPurchase75:
                ResolveNearestPropertyPurchase75(player, card, gameManager);
                break;

            case CardEffect.LuckyBreak:
                ResolveLuckyBreak(player, card, gameManager);
                break;
        }
    }

    // ============================================================
    // #1 — COLLECT FROM EVERY OTHER PLAYER
    // ============================================================

    private void ResolveCollectFromEveryPlayer(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        int totalReceived = 0;

        chanceCardUI.ShowPlayerEffect(
            "COLLECT FROM EVERY OTHER PLAYER",
            "EVERY OTHER PLAYER → YOU",
            $"${card.amount:N0}M EACH"
        );

        chanceCardUI.ShowPlayerList(
            "PLAYER COLLECTION",
            "$0M"
        );

        BoardPlayer[] players = gameManager.Players;

        if (players != null)
        {
            foreach (BoardPlayer other in players)
            {
                if (other == null ||
                    other == player ||
                    other.IsBankrupt)
                {
                    continue;
                }

                int payable =
                    Mathf.Min(
                        Mathf.Max(0, card.amount),
                        Mathf.Max(0, other.Money)
                    );

                if (payable <= 0)
                {
                    chanceCardUI.AddPlayerListRow(
                        other.PlayerName,
                        "$0M"
                    );
                    continue;
                }

                if (!gameManager.TransferMoneyBetweenPlayers(
                        other,
                        player,
                        payable))
                {
                    continue;
                }

                totalReceived += payable;

                chanceCardUI.AddPlayerListRow(
                    other.PlayerName,
                    $"-${payable:N0}M"
                );
            }
        }

        chanceCardUI.ShowPlayerList(
            "COLLECTION COMPLETE",
            $"+${totalReceived:N0}M"
        );

        chanceCardUI.ShowMoneyEffect(
            "PLAYER COLLECTION",
            $"+${totalReceived:N0}M",
            "FROM OTHER PLAYERS",
            "Each eligible player paid up to the card amount.",
            $"${card.amount:N0}M EACH"
        );

        chanceCardUI.ShowResult(
            $"+${totalReceived:N0}M",
            "Money was collected from every eligible player.",
            "EFFECT APPLIED"
        );

        EnableClose();
    }

    // ============================================================
    // #2 — BANK REWARD
    // ============================================================

    private void ResolveReceiveMoney(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        bool paid =
            gameManager.PaySpecificPlayer(
                player,
                card.amount
            );

        chanceCardUI.ShowMoneyEffect(
            "BANK REWARD",
            paid ? $"+${card.amount:N0}M" : "$0",
            "FROM THE BANK",
            paid
                ? "The bank transferred the reward to your balance."
                : "The bank could not provide the reward.",
            ""
        );

        chanceCardUI.ShowResult(
            paid
                ? $"+${card.amount:N0}M"
                : "NO REWARD",
            paid
                ? "Money received from the bank."
                : "The bank could not make the payment.",
            paid
                ? "REWARD RECEIVED"
                : "NO EFFECT"
        );

        EnableClose();
    }

    // ============================================================
    // #3 — FREE UPGRADE
    // ============================================================

    private void ResolveFreeUpgrade(
        BoardPlayer player)
    {
        propertyTargets.Clear();

        foreach (BoardSpace property in GetOwnedProperties(player))
        {
            if (property == null)
                continue;

            if (!property.IsProperty ||
                property.IsMortgaged ||
                property.HasHotel ||
                property.Houses >= 4)
            {
                continue;
            }

            propertyTargets.Add(property);
        }

        if (propertyTargets.Count == 0)
        {
            chanceCardUI.ShowResult(
                "NO ELIGIBLE PROPERTY",
                "None of your properties can receive another house.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        waitingForPropertyTarget = true;
        selectedPropertyTarget = null;
        targetIndex = 0;

        ShowCurrentPropertyTarget(
            "SELECT PROPERTY FOR FREE UPGRADE"
        );

        chanceCardUI.SetCloseInteractable(false);
    }

    // ============================================================
    // #4 — FREE PROPERTY
    // ============================================================

    private void ResolveFreeProperty(
        BoardPlayer player,
        BoardSpace property)
    {
        if (property == null ||
            !property.CanBePurchased())
        {
            ShowPropertyFailure(
                "PROPERTY IS NO LONGER AVAILABLE"
            );
            return;
        }

        property.SetOwner(player);

        if (property.IsProperty)
            property.RegisterOwnerLanding(player);

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            GetPropertyInfo(property),
            "CLAIMED FOR FREE",
            "TARGET PROPERTY",
            "NO PURCHASE COST"
        );

        chanceCardUI.ShowResult(
            "PROPERTY CLAIMED",
            $"{player.PlayerName} received {property.SpaceName} for free.",
            "EFFECT APPLIED"
        );

        EnableClose();
    }

    // ============================================================
    // #5 — MONEY + NEXT RENT FREE
    // ============================================================

    private void ResolveReceiveMoneyAndProtection(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        bool paid =
            gameManager.PaySpecificPlayer(
                player,
                card.amount
            );

        GiveNextRentFree(player);

        chanceCardUI.ShowMoneyEffect(
            "BANK REWARD",
            paid ? $"+${card.amount:N0}M" : "$0",
            "FROM THE BANK",
            paid
                ? "Your reward has been added."
                : "The bank could not provide the reward.",
            ""
        );

        chanceCardUI.ShowSpecialEffect(
            "YOUR NEXT PROPERTY RENT IS FREE",
            "NEXT PROPERTY LANDING",
            "The next rent you owe on another player's property will be reduced to $0.",
            "ACTIVE"
        );

        chanceCardUI.ShowResult(
            paid ? $"+${card.amount:N0}M" : "RENT SHIELD",
            paid
                ? "Reward received and rent protection activated."
                : "Rent protection activated.",
            "EFFECT APPLIED"
        );

        EnableClose();
    }

    // ============================================================
    // #6 — GO + REWARD
    // ============================================================

    private void ResolveMoveToGoAndCollect(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        int steps =
            CalculateForwardSteps(
                player.CurrentSpaceIndex,
                0
            );

        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {player.CurrentSpaceIndex}",
            "GO",
            steps > 0
                ? $"MOVE {steps} SPACES"
                : "ALREADY ON GO",
            "Collect $2,000,000 after arriving."
        );

        chanceCardUI.SetCloseInteractable(false);

        if (steps <= 0)
        {
            bool paid =
                gameManager.PaySpecificPlayer(
                    player,
                    card.amount
                );

            chanceCardUI.ShowResult(
                paid ? $"+${card.amount:N0}M" : "GO",
                paid
                    ? "You were already on GO and received the card reward."
                    : "The bank could not provide the reward.",
                paid ? "EFFECT APPLIED" : "PARTIAL EFFECT"
            );

            EnableClose();
            return;
        }

        player.MoveBySteps(steps);

        StartCoroutine(
            WaitForMovementAndComplete(
                player,
                () =>
                {
                    bool paid =
                        gameManager.PaySpecificPlayer(
                            player,
                            card.amount
                        );

                    chanceCardUI.ShowResult(
                        paid ? $"+${card.amount:N0}M" : "GO REACHED",
                        paid
                            ? "You reached GO and received the reward."
                            : "You reached GO, but the bank could not pay.",
                        paid ? "EFFECT APPLIED" : "PARTIAL EFFECT"
                    );

                    EnableClose();
                }
            )
        );
    }

    // ============================================================
    // #7 — DOUBLE NEXT RENT
    // ============================================================

    private void ResolveDoubleNextRent(
        BoardPlayer player,
        BoardSpace property)
    {
        if (property == null ||
            property.Owner != player ||
            !property.IsProperty)
        {
            ShowPropertyFailure(
                "You must select one of your own properties."
            );
            return;
        }

        DoubleNextRent(property);

        int currentRent = property.GetRent();
        int doubledRent = SafeMultiply(currentRent, 2);

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            GetPropertyInfo(property),
            "NEXT RENT WILL BE DOUBLED",
            "SELECTED PROPERTY",
            $"${currentRent:N0}M → ${doubledRent:N0}M"
        );

        chanceCardUI.ShowResult(
            "RENT BOOST ACTIVE",
            $"The next rent collected on {property.SpaceName} will be doubled.",
            "EFFECT APPLIED"
        );

        EnableClose();
    }

    // ============================================================
    // #8 — 50% PROPERTY PURCHASE
    // ============================================================

    private void ResolveDiscountedPropertyPurchase(
        BoardPlayer player,
        BoardSpace property,
        int percentage,
        GameManager gameManager)
    {
        if (property == null ||
            !property.CanBePurchased())
        {
            ShowPropertyFailure(
                "PROPERTY IS NO LONGER AVAILABLE"
            );
            return;
        }

        int originalPrice = property.PurchasePrice;
        int discountedPrice =
            Mathf.FloorToInt(
                originalPrice * percentage / 100f
            );

        if (player.Money < discountedPrice)
        {
            chanceCardUI.ShowResult(
                "CANNOT AFFORD",
                $"The discounted price is ${discountedPrice:N0}M.",
                "NO PURCHASE"
            );

            EnableClose();
            return;
        }

        int steps =
            CalculateForwardSteps(
                player.CurrentSpaceIndex,
                property.BoardIndex
            );

        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {player.CurrentSpaceIndex}",
            property.SpaceName,
            steps > 0 ? $"MOVE {steps} SPACES" : "ALREADY THERE",
            "PURCHASE AT 50% PRICE"
        );

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            $"Original: ${originalPrice:N0}M\n" +
            $"Discounted: ${discountedPrice:N0}M",
            "50% PURCHASE",
            "TARGET PROPERTY",
            $"PAY ${discountedPrice:N0}M"
        );

        chanceCardUI.SetCloseInteractable(false);

        if (steps > 0)
        {
            player.MoveBySteps(steps);

            StartCoroutine(
                WaitForMovementAndComplete(
                    player,
                    () =>
                    {
                        CompleteDiscountedPropertyPurchase(
                            player,
                            property,
                            discountedPrice,
                            gameManager,
                            percentage
                        );
                    }
                )
            );

            return;
        }

        CompleteDiscountedPropertyPurchase(
            player,
            property,
            discountedPrice,
            gameManager,
            percentage
        );
    }

    private void CompleteDiscountedPropertyPurchase(
        BoardPlayer player,
        BoardSpace property,
        int price,
        GameManager gameManager,
        int percentage)
    {
        if (property == null ||
            !property.CanBePurchased())
        {
            ShowPropertyFailure(
                "PROPERTY IS NO LONGER AVAILABLE"
            );
            return;
        }

        if (!player.RemoveMoney(price))
        {
            chanceCardUI.ShowResult(
                "PURCHASE FAILED",
                "You no longer have enough money.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        gameManager.AddBankMoney(price);

        property.SetOwner(player);

        if (property.IsProperty)
            property.RegisterOwnerLanding(player);

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            $"Original: ${property.PurchasePrice:N0}M\n" +
            $"Paid: ${price:N0}M",
            $"{percentage}% PURCHASE",
            "PROPERTY ACQUIRED",
            "Purchase completed."
        );

        chanceCardUI.ShowResult(
            "PROPERTY PURCHASED",
            $"{property.SpaceName} was purchased for ${price:N0}M.",
            "EFFECT APPLIED"
        );

        EnableClose();
    }

    // ============================================================
    // #9 — BANK PAYS NEXT RENT
    // ============================================================

    private void ResolveBankPaysNextRent(
        BoardPlayer player)
    {
        GiveBankPaysNextRent(player);

        chanceCardUI.ShowSpecialEffect(
            "THE BANK WILL PAY YOUR NEXT RENT",
            "NEXT RENT PAYMENT",
            "When you next owe rent, the bank will cover it for you.",
            "ACTIVE"
        );

        chanceCardUI.ShowResult(
            "RENT PROTECTION ACTIVE",
            "The bank will cover your next rent payment.",
            "EFFECT APPLIED"
        );

        EnableClose();
    }

    // ============================================================
    // #10 — MONEY PER PROPERTY
    // ============================================================

    private void ResolveMoneyPerProperty(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        int propertyCount =
            GetOwnedProperties(player).Count;

        int total =
            SafeMultiply(
                propertyCount,
                card.amount
            );

        bool paid =
            total > 0 &&
            gameManager.PaySpecificPlayer(
                player,
                total
            );

        chanceCardUI.ShowConditionEffect(
            $"YOU OWN {propertyCount} PROPERTIES",
            $"${card.amount:N0}M × {propertyCount} properties",
            propertyCount > 0
                ? "CONDITION MET"
                : "NO PROPERTIES",
            paid ? $"+${total:N0}M" : "$0"
        );

        chanceCardUI.ShowMoneyEffect(
            "PROPERTY DIVIDEND",
            paid ? $"+${total:N0}M" : "$0",
            "FROM THE BANK",
            "Reward calculated from your number of properties.",
            $"${card.amount:N0}M × {propertyCount}"
        );

        chanceCardUI.ShowResult(
            paid ? $"+${total:N0}M" : "NO REWARD",
            paid
                ? "Property-based reward received."
                : "No property-based reward was available.",
            paid ? "REWARD RECEIVED" : "NO EFFECT"
        );

        EnableClose();
    }

    // ============================================================
    // #11 — MOVE TO ANY PROPERTY, NO RENT
    // ============================================================

    private void PrepareAnyPropertyTarget(
        BoardPlayer player,
        CardDefinition card)
    {
        propertyTargets.Clear();

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            if (!space.IsProperty)
                continue;

            propertyTargets.Add(space);
        }

        propertyTargets.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(b.BoardIndex)
        );

        if (propertyTargets.Count == 0)
        {
            chanceCardUI.ShowResult(
                "NO PROPERTY",
                "No property spaces are available.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        waitingForPropertyTarget = true;
        selectedPropertyTarget = null;
        targetIndex = 0;

        ShowCurrentPropertyTarget(
            "SELECT DESTINATION PROPERTY"
        );

        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {player.CurrentSpaceIndex}",
            propertyTargets[targetIndex].SpaceName,
            "SELECT DESTINATION",
            "NO RENT WILL BE PAID"
        );

        chanceCardUI.SetCloseInteractable(false);
    }

    private void ResolveMoveToAnyPropertyNoRent(
        BoardPlayer player,
        BoardSpace property)
    {
        if (property == null)
        {
            ShowPropertyFailure("Destination property is missing.");
            return;
        }

        int steps =
            CalculateForwardSteps(
                player.CurrentSpaceIndex,
                property.BoardIndex
            );

        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {player.CurrentSpaceIndex}",
            property.SpaceName,
            steps > 0 ? $"MOVE {steps} SPACES" : "ALREADY THERE",
            "NO RENT WILL BE PAID"
        );

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            GetPropertyInfo(property),
            "DESTINATION SELECTED",
            "MOVE TO PROPERTY",
            "NO RENT"
        );

        chanceCardUI.SetCloseInteractable(false);

        if (steps <= 0)
        {
            chanceCardUI.ShowResult(
                "DESTINATION SELECTED",
                $"You are already on {property.SpaceName}.",
                "EFFECT APPLIED"
            );

            EnableClose();
            return;
        }

        player.MoveBySteps(steps);

        StartCoroutine(
            WaitForMovementAndComplete(
                player,
                () =>
                {
                    chanceCardUI.ShowResult(
                        "MOVED",
                        $"You moved to {property.SpaceName}. No rent was paid by this card.",
                        "EFFECT APPLIED"
                    );

                    EnableClose();
                }
            )
        );
    }

    // ============================================================
    // #12 — GET OUT OF JAIL FREE
    // ============================================================

    private void ResolveGetOutOfJailFree(
        BoardPlayer player)
    {
        GiveGetOutOfJailFree(player);

        int count =
            GetGetOutOfJailFreeCount(player);

        chanceCardUI.ShowSpecialEffect(
            "GET OUT OF JAIL FREE",
            "UNTIL USED",
            "Keep this card and use it when you need to leave Jail.",
            $"HELD: {count}"
        );

        chanceCardUI.ShowResult(
            "CARD RECEIVED",
            $"You now hold {count} Get Out Of Jail Free " +
            (count == 1 ? "card." : "cards."),
            "CARD STORED"
        );

        EnableClose();
    }

    // ============================================================
    // #13 — $2M OR FREE HOUSE
    // ============================================================

    private void ResolveMoneyOrFreeHouse(
        BoardPlayer player)
    {
        waitingForChoice = true;

        chanceCardUI.ShowSpecialEffect(
            "CHOOSE ONE REWARD",
            "ONE CHOICE",
            "Choose the cash reward or receive one free house.",
            "WAITING FOR CHOICE"
        );

        chanceCardUI.ShowChoice(
            "CHOOSE ONE",
            "$2,000,000",
            "FREE HOUSE"
        );

        chanceCardUI.SetChoiceInteractable(true, true);
        chanceCardUI.SetCloseInteractable(false);
    }

    public void SelectChoice(int choice)
    {
        if (!waitingForChoice ||
            activePlayer == null ||
            activeCard == null)
        {
            return;
        }

        waitingForChoice = false;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        if (choice == 0)
        {
            bool paid =
                gameManager.PaySpecificPlayer(
                    activePlayer,
                    activeCard.amount
                );

            chanceCardUI.ShowResult(
                paid ? $"+${activeCard.amount:N0}M" : "NO REWARD",
                paid
                    ? "You chose the cash reward."
                    : "The bank could not provide the cash reward.",
                paid ? "CHOICE APPLIED" : "NO EFFECT"
            );

            EnableClose();
            return;
        }

        chanceCardUI.ShowSpecialEffect(
            "FREE HOUSE",
            "ONE PROPERTY",
            "Now choose which eligible property receives the free house.",
            "SELECT PROPERTY"
        );

        ResolveFreeUpgrade(activePlayer);
    }

    // ============================================================
    // #14 — MONEY PER COMPLETE SET
    // ============================================================

    private void ResolveMoneyPerCompleteSet(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        int completedSets =
            CountCompletePropertySets(player);

        int total =
            SafeMultiply(
                completedSets,
                card.amount
            );

        bool paid =
            total > 0 &&
            gameManager.PaySpecificPlayer(
                player,
                total
            );

        chanceCardUI.ShowConditionEffect(
            $"YOU OWN {completedSets} COMPLETE COLOR SETS",
            $"${card.amount:N0}M × {completedSets} sets",
            completedSets > 0
                ? "CONDITION MET"
                : "NO COMPLETE SETS",
            paid ? $"+${total:N0}M" : "$0"
        );

        chanceCardUI.ShowResult(
            paid ? $"+${total:N0}M" : "NO REWARD",
            "Reward calculated from your complete color sets.",
            paid ? "REWARD RECEIVED" : "NO EFFECT"
        );

        EnableClose();
    }

    // ============================================================
    // #15 — 4+ PROPERTIES
    // ============================================================

    private void ResolvePropertyCountReward(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        int propertyCount =
            GetOwnedProperties(player).Count;

        bool qualified =
            propertyCount >= 4;

        bool paid =
            qualified &&
            gameManager.PaySpecificPlayer(
                player,
                card.amount
            );

        chanceCardUI.ShowConditionEffect(
            $"YOU OWN {propertyCount} PROPERTIES",
            $"Required: 4+\nCurrent: {propertyCount}",
            qualified
                ? "CONDITION MET"
                : "CONDITION NOT MET",
            paid ? $"+${card.amount:N0}M" : "$0"
        );

        chanceCardUI.ShowResult(
            paid ? $"+${card.amount:N0}M" : "NO REWARD",
            qualified
                ? "You qualified for the property milestone reward."
                : "You need at least four properties.",
            paid ? "REWARD RECEIVED" : "CONDITION FAILED"
        );

        EnableClose();
    }

    // ============================================================
    // #16 — LAST 2 TURNS LOST MONEY
    // ============================================================

    private void ResolveLossStreakReward(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        bool qualified =
            gameManager.LostMoneyOnLastTwoTurns(player);

        bool paid =
            qualified &&
            gameManager.PaySpecificPlayer(
                player,
                card.amount
            );

        chanceCardUI.ShowConditionEffect(
            qualified
                ? "YOU LOST MONEY ON YOUR LAST 2 TURNS"
                : "THE TWO-TURN LOSS CONDITION WAS NOT MET",
            $"Required reward: ${card.amount:N0}M",
            qualified
                ? "CONDITION MET"
                : "CONDITION NOT MET",
            paid ? $"+${card.amount:N0}M" : "$0"
        );

        chanceCardUI.ShowResult(
            paid ? $"+${card.amount:N0}M" : "NO REWARD",
            qualified
                ? "Your two-turn losing streak activated the recovery reward."
                : "You did not lose money on both of your previous completed turns.",
            paid ? "REWARD RECEIVED" : "CONDITION FAILED"
        );

        EnableClose();
    }

    // ============================================================
    // #17 — MOVE FORWARD 5 + $1M
    // ============================================================

    private void ResolveMoveForwardAndCollect(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {player.CurrentSpaceIndex}",
            $"SPACE +{card.movement}",
            $"MOVE {card.movement} SPACES",
            "Collect $1,000,000 after movement."
        );

        chanceCardUI.SetCloseInteractable(false);

        player.MoveBySteps(card.movement);

        StartCoroutine(
            WaitForMovementAndComplete(
                player,
                () =>
                {
                    bool paid =
                        gameManager.PaySpecificPlayer(
                            player,
                            card.amount
                        );

                    chanceCardUI.ShowResult(
                        paid ? $"+${card.amount:N0}M" : "MOVE COMPLETE",
                        paid
                            ? $"Moved forward {card.movement} spaces and received the reward."
                            : "Movement completed, but the bank could not pay.",
                        paid ? "EFFECT APPLIED" : "PARTIAL EFFECT"
                    );

                    EnableClose();
                }
            )
        );
    }

    // ============================================================
    // #18 — TAKE $1M FROM A PLAYER
    // ============================================================

    private void PreparePlayerTarget(
        BoardPlayer player,
        CardDefinition card)
    {
        playerTargets.Clear();

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        foreach (BoardPlayer other in gameManager.Players)
        {
            if (other == null ||
                other == player ||
                other.IsBankrupt)
            {
                continue;
            }

            playerTargets.Add(other);
        }

        if (playerTargets.Count == 0)
        {
            chanceCardUI.ShowResult(
                "NO VALID PLAYER",
                "There is nobody else available to target.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        waitingForPlayerTarget = true;
        selectedPlayerTarget = null;
        targetIndex = 0;

        ShowCurrentPlayerTarget();

        chanceCardUI.SetCloseInteractable(false);
    }

    public void NextTarget()
    {
        if (waitingForPlayerTarget)
        {
            if (playerTargets.Count <= 1)
                return;

            targetIndex++;

            if (targetIndex >= playerTargets.Count)
                targetIndex = 0;

            selectedPlayerTarget = null;
            ShowCurrentPlayerTarget();
            return;
        }

        if (!waitingForPropertyTarget)
            return;

        if (propertyTargets.Count <= 1)
            return;

        targetIndex++;

        if (targetIndex >= propertyTargets.Count)
            targetIndex = 0;

        selectedPropertyTarget = null;

        if (activeCard != null &&
            activeCard.effect ==
                CardEffect.DiscountedPropertyPurchase50)
        {
            ShowCurrentPropertyPurchaseTarget(50);
        }
        else
        {
            ShowCurrentPropertyTarget(
                GetPropertyTargetHeader()
            );
        }
    }

    public void TargetPlayerButtonPressed()
    {
        if (!waitingForPlayerTarget ||
            playerTargets.Count == 0)
        {
            return;
        }

        if (selectedPlayerTarget == null)
        {
            selectedPlayerTarget =
                playerTargets[targetIndex];

            ShowCurrentPlayerTarget(true);
            return;
        }

        ExecutePlayerTransfer(
            selectedPlayerTarget
        );
    }

    private void ShowCurrentPlayerTarget(
        bool confirmed = false)
    {
        BoardPlayer target =
            playerTargets[targetIndex];

        chanceCardUI.ShowPlayerTarget(
            target.PlayerName,
            $"Balance: ${target.Money:N0}M",
            confirmed ? "CONFIRM" : "SELECT"
        );

        chanceCardUI.SetTargetNextButtonVisible(
            playerTargets.Count > 1
        );

        chanceCardUI.ShowPlayerEffect(
            "TAKE $1M FROM ONE PLAYER",
            $"{target.PlayerName} → YOU",
            "$1M"
        );
    }

    private void ExecutePlayerTransfer(
        BoardPlayer target)
    {
        if (target == null ||
            activePlayer == null ||
            activeCard == null)
        {
            return;
        }

        int amount =
            Mathf.Min(
                Mathf.Max(0, activeCard.amount),
                Mathf.Max(0, target.Money)
            );

        if (amount <= 0)
        {
            chanceCardUI.ShowResult(
                "NO PAYMENT",
                $"{target.PlayerName} cannot pay anything right now.",
                "NO EFFECT"
            );

            waitingForPlayerTarget = false;
            selectedPlayerTarget = null;
            EnableClose();
            return;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        bool transferred =
            gameManager != null &&
            gameManager.TransferMoneyBetweenPlayers(
                target,
                activePlayer,
                amount
            );

        chanceCardUI.ShowMoneyEffect(
            "PLAYER TRANSFER",
            transferred ? $"+${amount:N0}M" : "$0",
            transferred
                ? $"FROM {target.PlayerName.ToUpperInvariant()}"
                : "TRANSFER FAILED",
            "Money was transferred directly between the two players.",
            ""
        );

        chanceCardUI.ShowResult(
            transferred ? $"+${amount:N0}M" : "TRANSFER FAILED",
            transferred
                ? $"{target.PlayerName} paid {activePlayer.PlayerName}."
                : "The money transfer could not be completed.",
            transferred
                ? "TRANSFER COMPLETE"
                : "NO EFFECT"
        );

        waitingForPlayerTarget = false;
        selectedPlayerTarget = null;

        EnableClose();
    }

    // ============================================================
    // #19 — NEAREST UNOWNED PROPERTY + 75%
    // ============================================================

    private void ResolveNearestPropertyPurchase75(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        BoardSpace nearest =
            FindNearestUnownedProperty(player);

        if (nearest == null)
        {
            chanceCardUI.ShowResult(
                "NO UNOWNED PROPERTY",
                "There are no unowned properties available.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        int originalPrice = nearest.PurchasePrice;
        int discountedPrice =
            Mathf.CeilToInt(
                originalPrice * 0.75f
            );

        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {player.CurrentSpaceIndex}",
            nearest.SpaceName,
            "MOVE TO PROPERTY",
            "PURCHASE AT 75% PRICE"
        );

        chanceCardUI.ShowPropertyEffect(
            nearest.SpaceName,
            $"Original: ${originalPrice:N0}M\n" +
            $"Purchase: ${discountedPrice:N0}M",
            "75% PURCHASE",
            "NEAREST UNOWNED PROPERTY",
            $"PAY ${discountedPrice:N0}M"
        );

        chanceCardUI.SetCloseInteractable(false);

        if (player.Money < discountedPrice)
        {
            chanceCardUI.ShowResult(
                "CANNOT AFFORD",
                $"You need ${discountedPrice:N0}M for the property.",
                "NO PURCHASE"
            );

            EnableClose();
            return;
        }

        int steps =
            CalculateForwardSteps(
                player.CurrentSpaceIndex,
                nearest.BoardIndex
            );

        if (steps > 0)
        {
            player.MoveBySteps(steps);

            StartCoroutine(
                WaitForMovementAndComplete(
                    player,
                    () =>
                    {
                        CompleteDiscountedPropertyPurchase(
                            player,
                            nearest,
                            discountedPrice,
                            gameManager,
                            75
                        );
                    }
                )
            );

            return;
        }

        CompleteDiscountedPropertyPurchase(
            player,
            nearest,
            discountedPrice,
            gameManager,
            75
        );
    }

    // ============================================================
    // CHOICE / PROPERTY TARGET SUPPORT
    // ============================================================

    public void TargetPropertyButtonPressed()
    {
        if (!waitingForPropertyTarget ||
            propertyTargets.Count == 0)
        {
            return;
        }

        if (selectedPropertyTarget == null)
        {
            selectedPropertyTarget =
                propertyTargets[targetIndex];

            chanceCardUI.ShowPropertyTarget(
                selectedPropertyTarget.SpaceName,
                GetPropertyTargetInfo(selectedPropertyTarget),
                "CONFIRM"
            );

            chanceCardUI.ShowPropertyEffect(
                selectedPropertyTarget.SpaceName,
                GetPropertyInfo(selectedPropertyTarget),
                GetPropertyActionText(),
                "SELECTED PROPERTY",
                ""
            );

            if (activeCard != null &&
                (activeCard.effect ==
                     CardEffect.MoveToAnyPropertyNoRent ||
                 activeCard.effect ==
                     CardEffect.DiscountedPropertyPurchase50))
            {
                chanceCardUI.ShowMovementEffect(
                    $"CURRENT\nSPACE {activePlayer.CurrentSpaceIndex}",
                    selectedPropertyTarget.SpaceName,
                    "DESTINATION SELECTED",
                    activeCard.effect == CardEffect.MoveToAnyPropertyNoRent
                        ? "NO RENT WILL BE PAID"
                        : "PURCHASE AT 50% PRICE"
                );
            }

            return;
        }

        BoardSpace property = selectedPropertyTarget;

        selectedPropertyTarget = null;
        waitingForPropertyTarget = false;

        CompletePropertySelection(property);
    }

    private void ShowCurrentPropertyTarget(
        string header)
    {
        if (propertyTargets.Count == 0)
            return;

        BoardSpace property =
            propertyTargets[targetIndex];

        chanceCardUI.ShowPropertyTarget(
            property.SpaceName,
            GetPropertyTargetInfo(property),
            "SELECT"
        );

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            GetPropertyInfo(property),
            header,
            "TARGET PROPERTY",
            ""
        );

        chanceCardUI.SetTargetNextButtonVisible(
            propertyTargets.Count > 1
        );
    }

    private void ShowCurrentPropertyPurchaseTarget(
        int percentage)
    {
        if (propertyTargets.Count == 0)
            return;

        BoardSpace property =
            propertyTargets[targetIndex];

        int discountedPrice =
            Mathf.FloorToInt(
                property.PurchasePrice *
                percentage /
                100f
            );

        chanceCardUI.ShowPropertyTarget(
            property.SpaceName,
            $"Value: ${property.PurchasePrice:N0}M\n" +
            $"Purchase: ${discountedPrice:N0}M",
            "SELECT"
        );

        chanceCardUI.ShowPropertyEffect(
            property.SpaceName,
            $"Original: ${property.PurchasePrice:N0}M\n" +
            $"Discounted: ${discountedPrice:N0}M",
            $"{percentage}% PURCHASE",
            "TARGET PROPERTY",
            $"PAY ${discountedPrice:N0}M"
        );

        chanceCardUI.ShowMovementEffect(
            $"CURRENT\nSPACE {activePlayer.CurrentSpaceIndex}",
            property.SpaceName,
            "SELECT DESTINATION",
            $"PURCHASE AT {percentage}% PRICE"
        );

        chanceCardUI.SetTargetNextButtonVisible(
            propertyTargets.Count > 1
        );
    }

    private void CompletePropertySelection(
        BoardSpace property)
    {
        if (property == null ||
            activePlayer == null ||
            activeCard == null)
        {
            return;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        switch (activeCard.effect)
        {
            case CardEffect.FreeUpgrade:

                if (property.Owner != activePlayer ||
                    !property.IsProperty ||
                    property.IsMortgaged ||
                    property.HasHotel ||
                    property.Houses >= 4)
                {
                    ShowPropertyFailure(
                        "This property cannot receive another house."
                    );
                    return;
                }

                if (property.AddFreeHouse(activePlayer))
                {
                    chanceCardUI.ShowPropertyEffect(
                        property.SpaceName,
                        GetPropertyInfo(property),
                        "FREE HOUSE",
                        "SELECTED PROPERTY",
                        "NO BUILDING COST"
                    );

                    chanceCardUI.ShowResult(
                        "FREE UPGRADE",
                        $"A free house was added to {property.SpaceName}.",
                        "EFFECT APPLIED"
                    );
                }
                else
                {
                    chanceCardUI.ShowResult(
                        "UPGRADE FAILED",
                        "The free house could not be added to this property.",
                        "NO EFFECT"
                    );
                }

                EnableClose();
                break;

            case CardEffect.FreeProperty:
                ResolveFreeProperty(
                    activePlayer,
                    property
                );
                break;

            case CardEffect.DoubleNextRent:
                ResolveDoubleNextRent(
                    activePlayer,
                    property
                );
                break;

            case CardEffect.DiscountedPropertyPurchase50:
                if (gameManager == null)
                {
                    ShowPropertyFailure("GameManager could not be found.");
                    return;
                }

                ResolveDiscountedPropertyPurchase(
                    activePlayer,
                    property,
                    50,
                    gameManager
                );
                break;

            case CardEffect.MoveToAnyPropertyNoRent:
                ResolveMoveToAnyPropertyNoRent(
                    activePlayer,
                    property
                );
                break;

            default:
                chanceCardUI.ShowResult(
                    "NO PROPERTY ACTION",
                    "This card does not use property selection.",
                    "NO EFFECT"
                );

                EnableClose();
                break;
        }
    }

    // ============================================================
    // TARGET PREPARATION
    // ============================================================

    private void PreparePropertyTarget(
        BoardPlayer player,
        CardDefinition card,
        bool freeProperty)
    {
        propertyTargets.Clear();

        propertyTargets.AddRange(
            freeProperty
                ? GetUnownedProperties()
                : GetOwnedProperties(player)
        );

        if (card.effect == CardEffect.FreeUpgrade)
        {
            propertyTargets.RemoveAll(
                property =>
                    property == null ||
                    property.IsMortgaged ||
                    property.HasHotel ||
                    property.Houses >= 4
            );
        }

        if (card.effect == CardEffect.DoubleNextRent)
        {
            propertyTargets.RemoveAll(
                property =>
                    property == null ||
                    property.Owner != player ||
                    !property.IsProperty
            );
        }

        if (propertyTargets.Count == 0)
        {
            chanceCardUI.ShowResult(
                "NO VALID PROPERTY",
                "There is no eligible property for this card.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        waitingForPropertyTarget = true;
        selectedPropertyTarget = null;
        targetIndex = 0;

        ShowCurrentPropertyTarget(
            freeProperty
                ? "SELECT UNOWNED PROPERTY"
                : GetPropertyTargetHeader()
        );

        chanceCardUI.SetCloseInteractable(false);
    }

    private void PreparePropertyPurchaseTarget(
        BoardPlayer player,
        int percentage)
    {
        propertyTargets.Clear();
        propertyTargets.AddRange(GetUnownedProperties());

        if (propertyTargets.Count == 0)
        {
            chanceCardUI.ShowResult(
                "NO UNOWNED PROPERTY",
                "There are no unowned properties available.",
                "NO EFFECT"
            );

            EnableClose();
            return;
        }

        waitingForPropertyTarget = true;
        selectedPropertyTarget = null;
        targetIndex = 0;

        ShowCurrentPropertyPurchaseTarget(percentage);
        chanceCardUI.SetCloseInteractable(false);
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private List<BoardSpace> GetOwnedProperties(
        BoardPlayer player)
    {
        List<BoardSpace> result =
            new List<BoardSpace>();

        if (player == null)
            return result;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null ||
                !space.IsProperty ||
                space.Owner != player)
            {
                continue;
            }

            result.Add(space);
        }

        result.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(b.BoardIndex)
        );

        return result;
    }

    private List<BoardSpace> GetUnownedProperties()
    {
        List<BoardSpace> result =
            new List<BoardSpace>();

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null ||
                !space.IsProperty ||
                !space.CanBePurchased())
            {
                continue;
            }

            result.Add(space);
        }

        result.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(b.BoardIndex)
        );

        return result;
    }

    private int CountCompletePropertySets(
        BoardPlayer player)
    {
        HashSet<PropertyGroup> groups =
            new HashSet<PropertyGroup>();

        foreach (BoardSpace property in GetOwnedProperties(player))
        {
            if (property.PropertyGroup != null)
                groups.Add(property.PropertyGroup);
        }

        int completed = 0;

        BoardSpace[] allSpaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (PropertyGroup group in groups)
        {
            bool complete = true;
            bool found = false;

            foreach (BoardSpace space in allSpaces)
            {
                if (space == null ||
                    !space.IsProperty ||
                    space.PropertyGroup != group)
                {
                    continue;
                }

                found = true;

                if (space.Owner != player)
                {
                    complete = false;
                    break;
                }
            }

            if (found && complete)
                completed++;
        }

        return completed;
    }

    private BoardSpace FindNearestUnownedProperty(
        BoardPlayer player)
    {
        BoardSpace best = null;
        int bestDistance = int.MaxValue;

        foreach (BoardSpace property in GetUnownedProperties())
        {
            int distance =
                CalculateForwardSteps(
                    player.CurrentSpaceIndex,
                    property.BoardIndex
                );

            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = property;
            }
        }

        return best;
    }

    private BoardPlayer FindLowestBalancePlayer(
        BoardPlayer[] players)
    {
        BoardPlayer result = null;

        if (players == null)
            return null;

        foreach (BoardPlayer player in players)
        {
            if (player == null ||
                player.IsBankrupt)
            {
                continue;
            }

            if (result == null ||
                player.Money < result.Money)
            {
                result = player;
            }
        }

        return result;
    }

    private int CalculateForwardSteps(
        int current,
        int target)
    {
        const int boardSize = 40;

        if (target >= current)
            return target - current;

        return boardSize - current + target;
    }

    private int SafeMultiply(
        int a,
        int b)
    {
        long result =
            (long)a * b;

        if (result > int.MaxValue)
            return int.MaxValue;

        if (result < int.MinValue)
            return int.MinValue;

        return (int)result;
    }

    private void ShowPropertyFailure(
        string message)
    {
        chanceCardUI.ShowResult(
            "PROPERTY ACTION FAILED",
            message,
            "NO EFFECT"
        );

        waitingForPropertyTarget = false;
        selectedPropertyTarget = null;
        EnableClose();
    }

    private void EnableClose()
    {
        if (chanceCardUI != null)
            chanceCardUI.SetCloseInteractable(true);
    }

    private string GetPropertyTargetHeader()
    {
        if (activeCard == null)
            return "SELECT PROPERTY";

        switch (activeCard.effect)
        {
            case CardEffect.FreeProperty:
                return "SELECT UNOWNED PROPERTY";

            case CardEffect.FreeUpgrade:
                return "SELECT PROPERTY FOR FREE UPGRADE";

            case CardEffect.DoubleNextRent:
                return "SELECT PROPERTY FOR RENT BOOST";

            case CardEffect.MoveToAnyPropertyNoRent:
                return "SELECT DESTINATION PROPERTY";

            default:
                return "SELECT PROPERTY";
        }
    }

    private string GetPropertyActionText()
    {
        if (activeCard == null)
            return "PROPERTY ACTION";

        switch (activeCard.effect)
        {
            case CardEffect.FreeProperty:
                return "CLAIM FOR FREE";

            case CardEffect.FreeUpgrade:
                return "FREE HOUSE";

            case CardEffect.DoubleNextRent:
                return "DOUBLE NEXT RENT";

            case CardEffect.DiscountedPropertyPurchase50:
                return "50% PURCHASE";

            case CardEffect.MoveToAnyPropertyNoRent:
                return "NO-RENT DESTINATION";

            default:
                return "PROPERTY ACTION";
        }
    }

    private string GetPropertyTargetInfo(
        BoardSpace property)
    {
        if (property == null)
            return string.Empty;

        return
            $"Value: ${property.PurchasePrice:N0}M\n" +
            $"Rent: ${property.GetRent():N0}M";
    }

    private string GetPropertyInfo(
        BoardSpace property)
    {
        if (property == null)
            return string.Empty;

        string owner =
            property.Owner != null
                ? property.Owner.PlayerName
                : "BANK";

        return
            $"Value: ${property.PurchasePrice:N0}M\n" +
            $"Owner: {owner}\n" +
            $"Houses: {property.Houses}";
    }

    private IEnumerator WaitForMovementAndComplete(
        BoardPlayer player,
        Action callback)
    {
        yield return new WaitUntil(
            () =>
                player == null ||
                !player.IsMoving
        );

        callback?.Invoke();
    }

    // ============================================================
    // PERSISTENT EFFECT API
    // ============================================================

    public void GiveNextRentFree(
        BoardPlayer player)
    {
        if (player == null)
            return;

        rentFreePlayers.Add(player);

        Debug.Log(
            $"{player.PlayerName}: NEXT PROPERTY RENT FREE"
        );
    }

    public bool HasNextRentFree(
        BoardPlayer player)
    {
        return player != null &&
               rentFreePlayers.Contains(player);
    }

    public bool ConsumeNextRentFree(
        BoardPlayer player)
    {
        if (!HasNextRentFree(player))
            return false;

        rentFreePlayers.Remove(player);
        return true;
    }

    public void GiveBankPaysNextRent(
        BoardPlayer player)
    {
        if (player == null)
            return;

        bankRentPlayers.Add(player);

        Debug.Log(
            $"{player.PlayerName}: BANK PAYS NEXT RENT"
        );
    }

    public bool HasBankPaysNextRent(
        BoardPlayer player)
    {
        return player != null &&
               bankRentPlayers.Contains(player);
    }

    public bool ConsumeBankPaysNextRent(
        BoardPlayer player)
    {
        if (!HasBankPaysNextRent(player))
            return false;

        bankRentPlayers.Remove(player);
        return true;
    }

    public void DoubleNextRent(
        BoardSpace property)
    {
        if (property == null)
            return;

        doubledRentProperties.Add(property);

        Debug.Log(
            $"{property.SpaceName}: NEXT RENT DOUBLED"
        );
    }

    public bool HasDoubledNextRent(
        BoardSpace property)
    {
        return property != null &&
               doubledRentProperties.Contains(property);
    }

    public bool ConsumeDoubledNextRent(
        BoardSpace property)
    {
        if (!HasDoubledNextRent(property))
            return false;

        doubledRentProperties.Remove(property);
        return true;
    }

    // ============================================================
    // GET OUT OF JAIL FREE API
    // ============================================================

    public void GiveGetOutOfJailFree(
        BoardPlayer player)
    {
        if (player == null)
            return;

        if (!jailFreeCards.ContainsKey(player))
            jailFreeCards[player] = 0;

        jailFreeCards[player]++;

        Debug.Log(
            $"{player.PlayerName}: " +
            $"Get Out Of Jail Free cards = " +
            jailFreeCards[player]
        );
    }

    public int GetGetOutOfJailFreeCount(
        BoardPlayer player)
    {
        if (player == null)
            return 0;

        return jailFreeCards.TryGetValue(
            player,
            out int count
        )
            ? count
            : 0;
    }

    public bool ConsumeGetOutOfJailFree(
        BoardPlayer player)
    {
        if (player == null)
            return false;

        if (!jailFreeCards.TryGetValue(
                player,
                out int count))
        {
            return false;
        }

        if (count <= 0)
            return false;

        count--;

        if (count == 0)
            jailFreeCards.Remove(player);
        else
            jailFreeCards[player] = count;

        return true;
    }

    // ============================================================
    // CLOSE / RESET
    // ============================================================

    public void CloseCard()
    {
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (chanceCardUI != null)
            chanceCardUI.HideImmediate();

        activePlayer = null;
        activeCard = null;
        activeDeck = null;

        waitingForChoice = false;
        waitingForPlayerTarget = false;
        waitingForPropertyTarget = false;

        playerTargets.Clear();
        propertyTargets.Clear();

        selectedPlayerTarget = null;
        selectedPropertyTarget = null;
        targetIndex = 0;

        if (gameManager != null)
        {
            gameManager.EndPlayerAction();
            gameManager.EndTurn();
        }
    }


    private void ResolveLuckyBreak(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        if (player == null ||
            card == null ||
            gameManager == null)
        {
            return;
        }

        BoardPlayer lowestBalancePlayer =
            FindLowestBalancePlayer(
                gameManager.Players
            );

        bool isLowestBalance =
            lowestBalancePlayer == player;

        int rewardAmount =
            isLowestBalance
                ? card.amount
                : card.secondaryAmount;

        bool received =
            rewardAmount > 0 &&
            gameManager.PayPlayer(
                rewardAmount
            );

        // --------------------------------------------------------
        // CONDITION DISPLAY
        // --------------------------------------------------------

        chanceCardUI.ShowConditionEffect(
            isLowestBalance
                ? "YOU HAVE THE LOWEST CASH BALANCE"
                : "YOU DO NOT HAVE THE LOWEST CASH BALANCE",

            lowestBalancePlayer != null
                ? $"Lowest balance: " +
                $"{lowestBalancePlayer.PlayerName} " +
                $"(${lowestBalancePlayer.Money:N0}M)"
                : "Lowest balance player could not be determined.",

            isLowestBalance
                ? "FULL REWARD"
                : "STANDARD REWARD",

            received
                ? $"+${rewardAmount:N0}M"
                : "$0"
        );

        // --------------------------------------------------------
        // MONEY DISPLAY
        // --------------------------------------------------------

        chanceCardUI.ShowMoneyEffect(
            "LUCKY BREAK",
            received
                ? $"+${rewardAmount:N0}M"
                : "$0",
            "FROM THE BANK",
            isLowestBalance
                ? "You had the lowest cash balance, so you received the full reward."
                : "You received the standard Lucky Break reward.",
            isLowestBalance
                ? "LOWEST BALANCE BONUS"
                : "STANDARD REWARD"
        );

        // --------------------------------------------------------
        // RESULT
        // --------------------------------------------------------

        chanceCardUI.ShowResult(
            received
                ? $"+${rewardAmount:N0}M"
                : "NO REWARD",

            isLowestBalance
                ? $"You were the player with the lowest cash balance and received the full ${rewardAmount:N0}M reward."
                : $"You received the standard ${rewardAmount:N0}M reward.",

            received
                ? "EFFECT APPLIED"
                : "BANK PAYMENT FAILED"
        );
    }


    // ============================================================
    // DEFAULT CHANCE DECK
    // ============================================================

    private void EnsureChanceCards()
    {
        if (chanceCards == null)
        {
            chanceCards =
                new List<CardDefinition>();
        }

        // Generate a clean built-in deck if the serialized deck is from
        // an older version or has the wrong number of cards.
        if (chanceCards.Count != 20 ||
            chanceDeckVersion != CurrentChanceDeckVersion)
        {
            CreateChanceCards();
            chanceDeckVersion = CurrentChanceDeckVersion;
        }
    }

    private void CreateChanceCards()
    {
        chanceCards.Clear();

        // Money is represented in millions in the existing economy.
        // The old $500K values were normalized to $1M because BoardPlayer
        // currently stores money as an integer number of millions.

        // 1
        chanceCards.Add(new CardDefinition
        {
            title = "Player Collection",
            description = "Collect $2,000,000 from every other player.",
            effect = CardEffect.CollectFromEveryPlayer,
            amount = 2,
            weight = 5f
        });

        // 2
        chanceCards.Add(new CardDefinition
        {
            title = "Bank Reward",
            description = "Collect $5,000,000 from the bank.",
            effect = CardEffect.ReceiveMoney,
            amount = 5,
            weight = 8f
        });

        // 3
        chanceCards.Add(new CardDefinition
        {
            title = "Free Upgrade",
            description = "Upgrade one property for free.",
            effect = CardEffect.FreeUpgrade,
            weight = 4f
        });

        // 4
        chanceCards.Add(new CardDefinition
        {
            title = "Property Gift",
            description = "Claim one unowned property for free.",
            effect = CardEffect.FreeProperty,
            weight = 1f
        });

        // 5
        chanceCards.Add(new CardDefinition
        {
            title = "Rent Shield",
            description =
                "Receive $1,000,000 and pay no rent on your next property landing.",
            effect = CardEffect.ReceiveMoneyAndRentProtection,
            amount = 1,
            weight = 5f
        });

        // 6
        chanceCards.Add(new CardDefinition
        {
            title = "Return To GO",
            description =
                "Move directly to GO and collect $2,000,000.",
            effect = CardEffect.MoveToGoAndCollect,
            amount = 2,
            weight = 3f
        });

        // 7
        chanceCards.Add(new CardDefinition
        {
            title = "Rent Surge",
            description =
                "Choose one property you own. Its next rent is doubled.",
            effect = CardEffect.DoubleNextRent,
            weight = 4f
        });

        // 8
        chanceCards.Add(new CardDefinition
        {
            title = "Property Bargain",
            description =
                "Move to any unowned property and buy it for 50% of its price.",
            effect = CardEffect.DiscountedPropertyPurchase50,
            percentage = 50,
            weight = 2f
        });

        // 9
        chanceCards.Add(new CardDefinition
        {
            title = "Bank Rent Cover",
            description = "The bank pays your next rent.",
            effect = CardEffect.BankPaysNextRent,
            weight = 4f
        });

        // 10
        chanceCards.Add(new CardDefinition
        {
            title = "Property Dividend",
            description =
                "Receive $1,000,000 for every property you own.",
            effect = CardEffect.MoneyPerProperty,
            amount = 1,
            weight = 5f
        });

        // 11
        chanceCards.Add(new CardDefinition
        {
            title = "Free Ride",
            description =
                "Move to any property on the board without paying rent.",
            effect = CardEffect.MoveToAnyPropertyNoRent,
            weight = 3f
        });

        // 12
        chanceCards.Add(new CardDefinition
        {
            title = "Get Out Of Jail Free",
            description = "Keep this card until needed.",
            effect = CardEffect.GetOutOfJailFree,
            weight = 1f
        });

        // 13
        chanceCards.Add(new CardDefinition
        {
            title = "Choose Your Reward",
            description =
                "Choose $2,000,000 or one free house.",
            effect = CardEffect.MoneyOrFreeHouse,
            amount = 2,
            weight = 4f
        });

        // 14
        chanceCards.Add(new CardDefinition
        {
            title = "Set Bonus",
            description =
                "Collect $3,000,000 for every complete color set you own.",
            effect = CardEffect.MoneyPerCompleteSet,
            amount = 3,
            weight = 2f
        });

        // 15
        chanceCards.Add(new CardDefinition
        {
            title = "Property Milestone",
            description =
                "If you own 4 or more properties, collect $4,000,000.",
            effect = CardEffect.ConditionalPropertyCountReward,
            amount = 4,
            weight = 3f
        });

        // 16
        chanceCards.Add(new CardDefinition
        {
            title = "Rough Streak",
            description =
                "If your cash balance decreased during each of your last 2 turns, collect $3,000,000.",
            effect = CardEffect.LossStreakReward,
            amount = 3,
            weight = 3f
        });

        // 17
        chanceCards.Add(new CardDefinition
        {
            title = "Forward Bonus",
            description =
                "Move forward 5 spaces and collect $1,000,000.",
            effect = CardEffect.MoveForwardAndCollect,
            amount = 1,
            movement = 5,
            weight = 6f
        });

        // 18
        chanceCards.Add(new CardDefinition
        {
            title = "Player Transfer",
            description =
                "Choose another player and take $1,000,000 from them.",
            effect = CardEffect.TakeMoneyFromPlayer,
            amount = 1,
            weight = 3f
        });

        // 19
        chanceCards.Add(new CardDefinition
        {
            title = "Property Opportunity",
            description =
                "Move to the nearest unowned property and buy it for 75% of its price.",
            effect = CardEffect.NearestPropertyPurchase75,
            percentage = 75,
            weight = 2f
        });

        // 20
        chanceCards.Add(new CardDefinition
        {
            title = "Lucky Break",
            description =
                "Receive $5,000,000 if you have the lowest cash balance; otherwise receive $1,000,000.",
            effect = CardEffect.LuckyBreak,
            amount = 5,
            secondaryAmount = 1,
            weight = 3f
        });
    }
}
