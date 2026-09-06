using System;
using System.Collections.Generic;
using UnityEngine;

public class CommunityChestCardManager : MonoBehaviour
{
    private enum Effect
    {
        CommunityGrant,
        TaxRefund,
        BirthdayCollection,
        PropertyTax,
        LuxuryExpense,
        BuildingGrant,
        MaintenanceBonus,
        BankSupport,
        MarketFreeze,
        EmergencyFund,
        GiveToPoor,
        CommunityDonation,
        PropertyAssessment,
        BankPenalty,
        PublicServiceReward,
        CommunityRescue,
        ForcedPropertyPayment,
        JailRelief,
        CharityReward,
        CommunityJackpot
    }

    [Serializable]
    private class Card
    {
        public string title;
        [TextArea(2, 5)] public string description;
        public Effect effect;
        public int amount;
        public float weight = 1f;
    }

    [Header("UI")]
    [SerializeField] private CommunityChestCardUI ui;

    [Header("Deck")]
    [SerializeField] private List<Card> cards = new List<Card>();

    private BoardPlayer activePlayer;
    private Card activeCard;

    private readonly List<BoardSpace> propertyTargets =
        new List<BoardSpace>();

    private int propertyTargetIndex;
    private bool waitingForPropertyTarget;

    // Player purchase / development restrictions from Market Freeze.
    private readonly Dictionary<BoardPlayer, int> marketFreezeTurns =
        new Dictionary<BoardPlayer, int>();

    public bool IsMarketFrozen(BoardPlayer player)
    {
        if (player == null)
            return false;

        return marketFreezeTurns.TryGetValue(
            player,
            out int turns
        ) && turns > 0;
    }

    public int MarketFreezeTurns(BoardPlayer player)
    {
        if (player == null)
            return 0;

        return marketFreezeTurns.TryGetValue(
            player,
            out int turns
        ) ? turns : 0;
    }

    public int CardCount
    {
        get
        {
            EnsureCards();
            return cards.Count;
        }
    }

    private void Awake()
    {
        EnsureCards();

        if (ui == null)
        {
            ui =
                FindFirstObjectByType<CommunityChestCardUI>(
                    FindObjectsInactive.Include
                );
        }

        if (ui == null)
        {
            Debug.LogError(
                "CommunityChestCardManager: CommunityChestCardUI not found. " +
                "Attach CommunityChestCardUI to the duplicated CommunityChestCard hierarchy."
            );
            return;
        }

        ui.EnsureBuilt();
        ui.HideImmediate();
    }

    public bool TryGetPreview(
        int index,
        out string title,
        out string description)
    {
        EnsureCards();

        title = string.Empty;
        description = string.Empty;

        if (index < 0 ||
            index >= cards.Count ||
            cards[index] == null)
        {
            return false;
        }

        title = cards[index].title;
        description = cards[index].description;

        return true;
    }

    public void Draw(BoardPlayer player)
    {
        if (player == null)
            return;

        EnsureCards();

        GameManager gm =
            FindFirstObjectByType<GameManager>();

        if (gm == null)
            return;

        if (cards.Count == 0)
        {
            FinishTurn(gm);
            return;
        }

        activePlayer = player;
        activeCard = SelectWeightedCard();

        ResetState();

        ui.ShowCard(
            activeCard.title,
            activeCard.description
        );

        Resolve(
            gm
        );
    }

    public void TestCard(int index)
    {
        EnsureCards();

        GameManager gm =
            FindFirstObjectByType<GameManager>();

        if (gm == null)
            return;

        if (index < 0 ||
            index >= cards.Count)
        {
            Debug.LogWarning(
                $"Community Chest tester: invalid card index {index}."
            );
            return;
        }

        BoardPlayer player =
            gm.CurrentPlayer;

        if (player == null)
            return;

        if (!gm.WaitingForPlayerAction)
            gm.BeginPlayerAction();

        activePlayer = player;
        activeCard = cards[index];

        ResetState();

        Debug.Log(
            $"COMMUNITY CHEST TEST: #{index + 1} - " +
            $"{activeCard.title}"
        );

        ui.ShowCard(
            activeCard.title,
            activeCard.description
        );

        Resolve(
            gm
        );
    }

    public void NextPropertyTarget()
    {
        if (!waitingForPropertyTarget ||
            propertyTargets.Count == 0)
        {
            return;
        }

        propertyTargetIndex =
            (propertyTargetIndex + 1) %
            propertyTargets.Count;

        ShowCurrentPropertyTarget();
    }

    private void Resolve(GameManager gm)
    {
        switch (activeCard.effect)
        {
            case Effect.CommunityGrant:
                BankReward(
                    gm,
                    activeCard.amount,
                    "COMMUNITY GRANT"
                );
                break;

            case Effect.TaxRefund:
                BankReward(
                    gm,
                    activeCard.amount,
                    "TAX REFUND"
                );
                break;

            case Effect.BirthdayCollection:
                CollectFromEveryOtherPlayer(
                    gm,
                    activeCard.amount
                );
                break;

            case Effect.PropertyTax:
                PayPerProperty(
                    gm,
                    activeCard.amount
                );
                break;

            case Effect.LuxuryExpense:
                PayBank(
                    gm,
                    activeCard.amount,
                    "LUXURY EXPENSE"
                );
                break;

            case Effect.BuildingGrant:
                PrepareBuildingGrant();
                break;

            case Effect.MaintenanceBonus:
                MaintenanceBonus(gm);
                break;

            case Effect.BankSupport:
                BankSupport(gm);
                break;

            case Effect.MarketFreeze:
                ApplyMarketFreeze(gm);
                break;

            case Effect.EmergencyFund:
                EmergencyFund(gm);
                break;

            case Effect.GiveToPoor:
                GiveToPoor(gm);
                break;

            case Effect.CommunityDonation:
                PayPerCompleteSet(gm);
                break;

            case Effect.PropertyAssessment:
                PayPerDevelopedProperty(gm);
                break;

            case Effect.BankPenalty:
                PayPercentage(
                    gm,
                    5
                );
                break;

            case Effect.PublicServiceReward:
                PublicServiceReward(gm);
                break;

            case Effect.CommunityRescue:
                CommunityRescue(gm);
                break;

            case Effect.ForcedPropertyPayment:
                PrepareForcedPropertyPayment();
                break;

            case Effect.JailRelief:
                JailRelief(gm);
                break;

            case Effect.CharityReward:
                CharityReward(gm);
                break;

            case Effect.CommunityJackpot:
                CommunityJackpot(gm);
                break;
        }
    }

    private void BankReward(
        GameManager gm,
        int amount,
        string label)
    {
        bool paid =
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                amount
            );

        ui.ShowMoneyEffect(
            label,
            paid
                ? $"+${amount:N0}M"
                : "$0",
            paid
                ? "THE BANK"
                : "BANK UNABLE TO PAY"
        );

        Finish(
            gm,
            paid
                ? $"+${amount:N0}M"
                : "NO REWARD",
            paid
                ? "Money was added to your balance."
                : "The bank does not have enough funds."
        );
    }

    private void PayBank(
        GameManager gm,
        int amount,
        string label)
    {
        bool paid =
            gm.PlayerPaysBankForPlayer(
                activePlayer,
                amount
            );

        ui.ShowPaymentEffect(
            label,
            paid
                ? $"-${amount:N0}M"
                : "PAYMENT FAILED",
            paid
                ? "TO THE BANK"
                : "INSUFFICIENT FUNDS"
        );

        if (paid)
        {
            Finish(
                gm,
                $"-${amount:N0}M",
                $"You paid ${amount:N0}M to the bank."
            );
        }
        else
        {
            gm.HandleBankruptcy(
                activePlayer
            );

            Finish(
                gm,
                "BANKRUPT",
                "You could not afford the payment."
            );
        }
    }

    private void CollectFromEveryOtherPlayer(
        GameManager gm,
        int amount)
    {
        int total =
            0;

        foreach (BoardPlayer player
                 in gm.Players)
        {
            if (player == null ||
                player == activePlayer ||
                player.IsBankrupt)
            {
                continue;
            }

            int payment =
                Mathf.Min(
                    amount,
                    player.Money
                );

            if (payment <= 0)
                continue;

            if (!player.RemoveMoney(payment))
                continue;

            activePlayer.AddMoney(payment);
            total += payment;
        }

        ui.ShowPlayerEffect(
            "BIRTHDAY COLLECTION",
            "EVERY OTHER PLAYER → YOU",
            $"+${total:N0}M",
            "Each active player contributed up to the card amount."
        );

        // Use the existing TargetArea as a clean collection summary,
        // instead of putting player details inside PlayerEffect.
        List<string> contributions =
            new List<string>();

        foreach (BoardPlayer participant in gm.Players)
        {
            if (participant == null ||
                participant == activePlayer ||
                participant.IsBankrupt)
            {
                continue;
            }

            int contribution =
                Mathf.Min(
                    amount,
                    participant.Money
                );

            if (contribution > 0)
            {
                contributions.Add(
                    $"{participant.PlayerName}: +${contribution:N0}M"
                );
            }
            else
            {
                contributions.Add(
                    $"{participant.PlayerName}: $0"
                );
            }
        }

        string contributionSummary =
            contributions.Count > 0
                ? string.Join("\n", contributions)
                : "No other active players could contribute.";

        ui.ShowPlayerTargetInfo(
            "COLLECTION SUMMARY",
            contributionSummary,
            "AUTOMATIC COLLECTION"
        );

        Finish(
            gm,
            $"+${total:N0}M",
            "Birthday collection completed."
        );
    }

    private void PayPerProperty(
        GameManager gm,
        int perProperty)
    {
        int count =
            CountNormalProperties(
                activePlayer
            );

        int total =
            SafeMultiply(
                count,
                perProperty
            );

        bool paid =
            total == 0 ||
            gm.PlayerPaysBankForPlayer(
                activePlayer,
                total
            );

        ui.ShowPaymentEffect(
            "PROPERTY TAX",
            paid
                ? $"-${total:N0}M"
                : "PAYMENT FAILED",
            $"You own {count} properties. ${perProperty:N0}M × {count} = ${total:N0}M."
        );

        if (paid)
        {
            Finish(
                gm,
                total == 0
                    ? "NO PAYMENT"
                    : $"-${total:N0}M",
                "Property tax has been applied."
            );
        }
        else
        {
            gm.HandleBankruptcy(
                activePlayer
            );

            Finish(
                gm,
                "BANKRUPT",
                "You could not pay the property tax."
            );
        }
    }

    private void PrepareBuildingGrant()
    {
        propertyTargets.Clear();

        foreach (BoardSpace space
                 in FindObjectsByType<BoardSpace>(
                     FindObjectsSortMode.None
                 ))
        {
            if (space == null ||
                !space.IsProperty ||
                space.Owner != activePlayer)
            {
                continue;
            }

            if (space.IsMortgaged ||
                space.HasHotel ||
                space.Houses >= 4)
            {
                continue;
            }

            propertyTargets.Add(
                space
            );
        }

        if (propertyTargets.Count == 0)
        {
            GameManager gm =
                FindFirstObjectByType<GameManager>();

            ui.ShowResult(
                "NO ELIGIBLE PROPERTY",
                "You do not own a property that can receive a free house.",
                "NO EFFECT"
            );

            FinishTurn(
                gm
            );

            return;
        }

        propertyTargets.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(
                    b.BoardIndex
                )
        );

        waitingForPropertyTarget = true;
        propertyTargetIndex = 0;

        ShowCurrentPropertyTarget();
    }

    private void ShowCurrentPropertyTarget()
    {
        if (propertyTargets.Count == 0)
            return;

        BoardSpace target =
            propertyTargets[
                propertyTargetIndex
            ];

        ui.ShowSpecialEffect(
            "BUILDING GRANT",
            "ONE FREE HOUSE",
            "Choose any eligible property you own. You do not need to be standing on it.",
            "SELECT PROPERTY"
        );

        ui.ShowTarget(
            "TARGET PROPERTY",
            target.SpaceName,
            $"Owner: {activePlayer.PlayerName}\nHouses: {target.Houses}/4\nValue: ${target.PurchasePrice:N0}M",
            "SELECT",
            propertyTargets.Count > 1,
            ConfirmPropertyTarget,
            NextPropertyTarget
        );
    }

    private void ConfirmPropertyTarget()
    {
        if (!waitingForPropertyTarget ||
            propertyTargets.Count == 0)
        {
            return;
        }

        BoardSpace target =
            propertyTargets[
                propertyTargetIndex
            ];

        waitingForPropertyTarget = false;

        GameManager gm =
            FindFirstObjectByType<GameManager>();

        if (target.AddFreeHouse(activePlayer))
        {
            ui.ShowPropertyEffect(
                target.SpaceName,
                $"Houses: {target.Houses}/4",
                "FREE HOUSE",
                "No building cost."
            );

            Finish(
                gm,
                "FREE HOUSE",
                $"A free house was added to {target.SpaceName}."
            );
        }
        else
        {
            Finish(
                gm,
                "UPGRADE FAILED",
                $"The free house could not be added to {target.SpaceName}."
            );
        }
    }

    private void MaintenanceBonus(
        GameManager gm)
    {
        int developed =
            0;

        foreach (BoardSpace space
                 in GetOwnedNormal())
        {
            if (space.Houses > 0 ||
                space.HasHotel)
            {
                developed++;
            }
        }

        int total =
            SafeMultiply(
                developed,
                activeCard.amount
            );

        bool paid =
            total > 0 &&
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                total
            );

        ui.ShowMoneyEffect(
            "MAINTENANCE BONUS",
            paid
                ? $"+${total:N0}M"
                : "$0",
            $"${activeCard.amount:N0}M × {developed} developed properties."
        );

        Finish(
            gm,
            paid
                ? $"+${total:N0}M"
                : "NO REWARD",
            "Maintenance bonus resolved."
        );
    }

    private void BankSupport(
        GameManager gm)
    {
        bool qualifies =
            activePlayer.Money < 2000;

        int reward =
            qualifies
                ? 30
                : 5;

        bool paid =
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                reward
            );

        ui.ShowConditionEffect(
            qualifies
                ? "BALANCE BELOW $2,000M"
                : "BALANCE $2,000M OR MORE",
            qualifies
                ? "Full support applies."
                : "Standard support applies.",
            qualifies
                ? "FULL SUPPORT"
                : "STANDARD SUPPORT",
            paid
                ? $"+${reward:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"+${reward:N0}M"
                : "NO REWARD",
            "Bank Support resolved."
        );
    }

    private void ApplyMarketFreeze(
        GameManager gm)
    {
        marketFreezeTurns[
            activePlayer
        ] = 2;

        ui.ShowSpecialEffect(
            "MARKET FREEZE",
            "2 TURNS",
            "You cannot buy, sell, mortgage, or trade property for your next 2 turns.",
            "ACTIVE"
        );

        Finish(
            gm,
            "MARKET FREEZE",
            "Property transactions are restricted for 2 turns."
        );
    }

    private void EmergencyFund(
        GameManager gm)
    {
        bool qualifies =
            activePlayer.Money < 1000;

        int reward =
            qualifies
                ? 20
                : 0;

        bool paid =
            reward > 0 &&
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                reward
            );

        ui.ShowConditionEffect(
            qualifies
                ? "BALANCE BELOW $1,000M"
                : "BALANCE $1,000M OR MORE",
            qualifies
                ? "Emergency support is activated."
                : "No emergency payment applies.",
            qualifies
                ? "ELIGIBLE"
                : "NOT ELIGIBLE",
            paid
                ? $"+${reward:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"+${reward:N0}M"
                : "NO REWARD",
            qualifies
                ? "Emergency Fund resolved."
                : "You did not qualify."
        );
    }

    private void GiveToPoor(
        GameManager gm)
    {
        BoardPlayer poorest = null;

        foreach (BoardPlayer p
                 in gm.Players)
        {
            if (p == null ||
                p == activePlayer ||
                p.IsBankrupt)
            {
                continue;
            }

            if (poorest == null ||
                p.Money < poorest.Money)
            {
                poorest = p;
            }
        }

        if (poorest == null)
        {
            ui.ShowResult(
                "NO TARGET PLAYER",
                "There is no other active player to receive the donation.",
                "NO EFFECT"
            );

            FinishTurn(gm);
            return;
        }

        int donation =
            Mathf.Min(
                activeCard.amount,
                activePlayer.Money
            );

        if (donation <= 0)
        {
            ui.ShowPlayerEffect(
                "COMMUNITY DONATION",
                $"{activePlayer.PlayerName} → {poorest.PlayerName}",
                "$0",
                "You cannot afford the donation."
            );

            Finish(
                gm,
                "NO DONATION",
                "You do not have enough cash."
            );

            return;
        }

        if (!activePlayer.RemoveMoney(donation))
        {
            Finish(
                gm,
                "NO DONATION",
                "The donation could not be completed."
            );

            return;
        }

        poorest.AddMoney(
            donation
        );

        ui.ShowPlayerEffect(
            "COMMUNITY DONATION",
            $"{activePlayer.PlayerName} → {poorest.PlayerName}",
            $"-${donation:N0}M",
            "The donation goes to the player with the lowest cash balance."
        );

        ui.ShowPlayerTargetInfo(
            poorest.PlayerName,
            $"Balance after receiving: ${poorest.Money:N0}M",
            "DONATION RECEIVED"
        );

        Finish(
            gm,
            $"-${donation:N0}M",
            $"{poorest.PlayerName} received the donation."
        );
    }

    private void PayPerCompleteSet(
        GameManager gm)
    {
        int sets =
            CountCompleteSets(
                activePlayer
            );

        int total =
            SafeMultiply(
                sets,
                activeCard.amount
            );

        bool paid =
            total == 0 ||
            gm.PlayerPaysBankForPlayer(
                activePlayer,
                total
            );

        ui.ShowConditionEffect(
            $"COMPLETE SETS: {sets}",
            $"${activeCard.amount:N0}M × {sets} sets",
            paid
                ? "ASSESSMENT COMPLETE"
                : "PAYMENT FAILED",
            paid
                ? $"-${total:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"-${total:N0}M"
                : "PAYMENT FAILED",
            "Community Donation resolved."
        );
    }

    private void PayPerDevelopedProperty(
        GameManager gm)
    {
        int developed =
            0;

        foreach (BoardSpace space
                 in GetOwnedNormal())
        {
            if (space.Houses > 0 ||
                space.HasHotel)
            {
                developed++;
            }
        }

        int total =
            SafeMultiply(
                developed,
                activeCard.amount
            );

        bool paid =
            total == 0 ||
            gm.PlayerPaysBankForPlayer(
                activePlayer,
                total
            );

        ui.ShowConditionEffect(
            $"DEVELOPED PROPERTIES: {developed}",
            $"${activeCard.amount:N0}M × {developed}",
            paid
                ? "ASSESSMENT COMPLETE"
                : "PAYMENT FAILED",
            paid
                ? $"-${total:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"-${total:N0}M"
                : "PAYMENT FAILED",
            "Property Assessment resolved."
        );
    }

    private void PayPercentage(
        GameManager gm,
        int percentage)
    {
        int amount =
            Mathf.CeilToInt(
                activePlayer.Money *
                (percentage / 100f)
            );

        bool paid =
            amount == 0 ||
            gm.PlayerPaysBankForPlayer(
                activePlayer,
                amount
            );

        ui.ShowPaymentEffect(
            "BANK PENALTY",
            paid
                ? $"-${amount:N0}M"
                : "PAYMENT FAILED",
            $"{percentage}% of your current cash balance."
        );

        Finish(
            gm,
            paid
                ? $"-${amount:N0}M"
                : "PAYMENT FAILED",
            "Bank penalty resolved."
        );
    }

    private void PublicServiceReward(
        GameManager gm)
    {
        int count =
            0;

        foreach (BoardSpace space
                 in FindObjectsByType<BoardSpace>(
                     FindObjectsSortMode.None
                 ))
        {
            if (space == null ||
                space.Owner != activePlayer)
            {
                continue;
            }

            if (space.SpaceType == BoardSpaceType.Airport ||
                space.SpaceType == BoardSpaceType.Utility)
            {
                count++;
            }
        }

        int rewardPerUnit =
            5;

        int total =
            SafeMultiply(
                count,
                rewardPerUnit
            );

        bool paid =
            total > 0 &&
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                total
            );

        ui.ShowMoneyEffect(
            "PUBLIC SERVICE REWARD",
            paid
                ? $"+${total:N0}M"
                : "$0",
            $"${rewardPerUnit:N0}M × {count} airports/utilities."
        );

        Finish(
            gm,
            paid
                ? $"+${total:N0}M"
                : "NO REWARD",
            "Public Service Reward resolved."
        );
    }

    private void CommunityRescue(
        GameManager gm)
    {
        int highestOther =
            0;

        foreach (BoardPlayer p
                 in gm.Players)
        {
            if (p == null ||
                p == activePlayer ||
                p.IsBankrupt)
            {
                continue;
            }

            highestOther =
                Mathf.Max(
                    highestOther,
                    p.Money
                );
        }

        bool qualifies =
            activePlayer.Money < highestOther;

        int reward =
            qualifies
                ? 10
                : 0;

        bool paid =
            reward > 0 &&
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                reward
            );

        ui.ShowConditionEffect(
            qualifies
                ? "ANOTHER PLAYER HAS MORE CASH"
                : "NO OTHER PLAYER HAS MORE CASH",
            qualifies
                ? "Community rescue activated."
                : "No rescue payment applies.",
            qualifies
                ? "RESCUE"
                : "NO RESCUE",
            paid
                ? $"+${reward:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"+${reward:N0}M"
                : "NO REWARD",
            "Community Rescue resolved."
        );
    }

    private void PrepareForcedPropertyPayment()
    {
        propertyTargets.Clear();

        foreach (BoardSpace space
                 in FindObjectsByType<BoardSpace>(
                     FindObjectsSortMode.None
                 ))
        {
            if (space == null ||
                !space.IsProperty ||
                space.Owner != activePlayer)
            {
                continue;
            }

            propertyTargets.Add(
                space
            );
        }

        propertyTargets.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(
                    b.BoardIndex
                )
        );

        if (propertyTargets.Count == 0)
        {
            GameManager gm =
                FindFirstObjectByType<GameManager>();

            ui.ShowResult(
                "NO PROPERTY",
                "You do not own a property to assess.",
                "NO EFFECT"
            );

            FinishTurn(gm);
            return;
        }

        waitingForPropertyTarget = true;
        propertyTargetIndex = 0;

        ShowCurrentForcedPropertyTarget();
    }

    private void ShowCurrentForcedPropertyTarget()
    {
        BoardSpace target =
            propertyTargets[
                propertyTargetIndex
            ];

        int payment =
            Mathf.CeilToInt(
                target.PurchasePrice *
                0.25f
            );

        ui.ShowPaymentEffect(
            "PROPERTY ASSESSMENT",
            $"-${payment:N0}M",
            $"{target.SpaceName}: 25% of ${target.PurchasePrice:N0}M."
        );

        ui.ShowTarget(
            "TARGET PROPERTY",
            target.SpaceName,
            $"Value: ${target.PurchasePrice:N0}M\nAssessment: ${payment:N0}M",
            "SELECT",
            propertyTargets.Count > 1,
            ConfirmForcedPropertyPayment,
            () =>
            {
                propertyTargetIndex =
                    (propertyTargetIndex + 1) %
                    propertyTargets.Count;

                ShowCurrentForcedPropertyTarget();
            }
        );
    }

    private void ConfirmForcedPropertyPayment()
    {
        if (!waitingForPropertyTarget ||
            propertyTargets.Count == 0)
        {
            return;
        }

        BoardSpace property =
            propertyTargets[
                propertyTargetIndex
            ];

        waitingForPropertyTarget = false;

        GameManager gm =
            FindFirstObjectByType<GameManager>();

        int payment =
            Mathf.CeilToInt(
                property.PurchasePrice *
                0.25f
            );

        bool paid =
            gm.PlayerPaysBankForPlayer(
                activePlayer,
                payment
            );

        ui.ShowPropertyEffect(
            property.SpaceName,
            $"Value: ${property.PurchasePrice:N0}M",
            "25% ASSESSMENT",
            paid
                ? $"-${payment:N0}M paid to the bank."
                : "Payment failed."
        );

        Finish(
            gm,
            paid
                ? $"-${payment:N0}M"
                : "PAYMENT FAILED",
            paid
                ? $"You paid 25% of {property.SpaceName}'s purchase value."
                : "You could not afford the assessment."
        );
    }

    private void JailRelief(
        GameManager gm)
    {
        if (activePlayer.IsInJail)
        {
            activePlayer.LeaveJail();

            ui.ShowSpecialEffect(
                "JAIL RELIEF",
                "IMMEDIATE",
                "You were released from Jail.",
                "ACTIVE"
            );

            Finish(
                gm,
                "OUT OF JAIL",
                "You have been released from Jail."
            );

            return;
        }

        bool paid =
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                5
            );

        ui.ShowSpecialEffect(
            "JAIL RELIEF",
            "NOT IN JAIL",
            "You were not in Jail, so a relief payment was granted.",
            paid
                ? "+$5M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? "+$5M"
                : "NO REWARD",
            "Jail Relief resolved."
        );
    }

    private void CharityReward(
        GameManager gm)
    {
        int count =
            CountNormalProperties(
                activePlayer
            );

        int reward =
            count <= 3
                ? 20
                : 5;

        bool paid =
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                reward
            );

        ui.ShowConditionEffect(
            $"PROPERTIES: {count}",
            count <= 3
                ? "3 or fewer properties = full charity."
                : "More than 3 properties = standard charity.",
            count <= 3
                ? "FULL CHARITY"
                : "STANDARD CHARITY",
            paid
                ? $"+${reward:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"+${reward:N0}M"
                : "NO REWARD",
            "Charity Reward resolved."
        );
    }

    private void CommunityJackpot(
        GameManager gm)
    {
        BoardPlayer lowest =
            null;

        foreach (BoardPlayer p
                 in gm.Players)
        {
            if (p == null ||
                p.IsBankrupt)
            {
                continue;
            }

            if (lowest == null ||
                p.Money < lowest.Money)
            {
                lowest = p;
            }
        }

        bool qualifies =
            lowest == activePlayer;

        int reward =
            qualifies
                ? activeCard.amount
                : 10;

        bool paid =
            gm.PayPlayerForSpecificPlayer(
                activePlayer,
                reward
            );

        ui.ShowConditionEffect(
            qualifies
                ? "LOWEST CASH BALANCE"
                : "STANDARD OUTCOME",
            qualifies
                ? $"Jackpot reward: ${activeCard.amount:N0}M."
                : "Standard reward applies.",
            qualifies
                ? "JACKPOT"
                : "STANDARD",
            paid
                ? $"+${reward:N0}M"
                : "$0"
        );

        Finish(
            gm,
            paid
                ? $"+${reward:N0}M"
                : "NO REWARD",
            "Community Jackpot resolved."
        );
    }

    private void Finish(
        GameManager gm,
        string main,
        string detail)
    {
        if (gm == null)
            return;

        ui.ShowResult(
            main,
            detail,
            "EFFECT APPLIED"
        );

        gm.EndPlayerAction();
        gm.EndTurn();

    }

    private void FinishTurn(
        GameManager gm)
    {
        if (gm == null)
            return;

        gm.EndPlayerAction();
        gm.EndTurn();

    }

    private void AdvanceMarketFreezeTurn(
        BoardPlayer player)
    {
        if (player == null)
            return;

        if (!marketFreezeTurns.TryGetValue(
                player,
                out int turns))
        {
            return;
        }

        turns--;

        if (turns <= 0)
        {
            marketFreezeTurns.Remove(
                player
            );
        }
        else
        {
            marketFreezeTurns[player] =
                turns;
        }
    }

    private void ResetState()
    {
        waitingForPropertyTarget = false;
        propertyTargetIndex = 0;
        propertyTargets.Clear();
    }

    private Card SelectWeightedCard()
    {
        float total =
            0f;

        foreach (Card card
                 in cards)
        {
            if (card == null)
                continue;

            total +=
                Mathf.Max(
                    0.01f,
                    card.weight
                );
        }

        float roll =
            UnityEngine.Random.Range(
                0f,
                total
            );

        float running =
            0f;

        foreach (Card card
                 in cards)
        {
            if (card == null)
                continue;

            running +=
                Mathf.Max(
                    0.01f,
                    card.weight
                );

            if (roll <= running)
                return card;
        }

        return cards[
            cards.Count - 1
        ];
    }

    private List<BoardSpace> GetOwnedNormal()
    {
        List<BoardSpace> result =
            new List<BoardSpace>();

        foreach (BoardSpace space
                 in FindObjectsByType<BoardSpace>(
                     FindObjectsSortMode.None
                 ))
        {
            if (space != null &&
                space.IsProperty &&
                space.Owner == activePlayer)
            {
                result.Add(
                    space
                );
            }
        }

        return result;
    }

    private int CountNormalProperties(
        BoardPlayer player)
    {
        int count = 0;

        foreach (BoardSpace space
                 in FindObjectsByType<BoardSpace>(
                     FindObjectsSortMode.None
                 ))
        {
            if (space != null &&
                space.IsProperty &&
                space.Owner == player)
            {
                count++;
            }
        }

        return count;
    }

    private int CountCompleteSets(
        BoardPlayer player)
    {
        HashSet<PropertyGroup> groups =
            new HashSet<PropertyGroup>();

        foreach (BoardSpace space
                 in GetOwnedNormal())
        {
            if (space.PropertyGroup != null)
            {
                groups.Add(
                    space.PropertyGroup
                );
            }
        }

        int complete =
            0;

        foreach (PropertyGroup group
                 in groups)
        {
            bool groupComplete =
                true;

            foreach (BoardSpace space
                     in FindObjectsByType<BoardSpace>(
                         FindObjectsSortMode.None
                     ))
            {
                if (space == null ||
                    !space.IsProperty ||
                    space.PropertyGroup != group)
                {
                    continue;
                }

                if (space.Owner != player)
                {
                    groupComplete =
                        false;
                    break;
                }
            }

            if (groupComplete)
                complete++;
        }

        return complete;
    }

    private int SafeMultiply(
        int a,
        int b)
    {
        long value =
            (long)a *
            b;

        return value > int.MaxValue
            ? int.MaxValue
            : value < int.MinValue
                ? int.MinValue
                : (int)value;
    }

    private void EnsureCards()
    {
        if (cards != null && cards.Count == 20)
            return;

        cards = new List<Card>
        {
            new Card
            {
                title = "Community Grant",
                description = "Receive $30M from the bank.",
                effect = Effect.CommunityGrant,
                amount = 30,
                weight = 8f
            },
            new Card
            {
                title = "Tax Refund",
                description = "Receive $20M from the bank.",
                effect = Effect.TaxRefund,
                amount = 20,
                weight = 7f
            },
            new Card
            {
                title = "Birthday Collection",
                description = "Collect $10M from every other player.",
                effect = Effect.BirthdayCollection,
                amount = 10,
                weight = 4f
            },
            new Card
            {
                title = "Property Tax",
                description = "Pay $5M for every property you own.",
                effect = Effect.PropertyTax,
                amount = 5,
                weight = 5f
            },
            new Card
            {
                title = "Luxury Expense",
                description = "Pay $20M to the bank.",
                effect = Effect.LuxuryExpense,
                amount = 20,
                weight = 5f
            },
            new Card
            {
                title = "Building Grant",
                description = "Choose one eligible property you own and receive one free house.",
                effect = Effect.BuildingGrant,
                weight = 3f
            },
            new Card
            {
                title = "Maintenance Bonus",
                description = "Receive $5M for every property with at least one house or hotel.",
                effect = Effect.MaintenanceBonus,
                amount = 5,
                weight = 4f
            },
            new Card
            {
                title = "Bank Support",
                description = "If your balance is below $2,000M, receive $30M. Otherwise, receive $5M.",
                effect = Effect.BankSupport,
                weight = 3f
            },
            new Card
            {
                title = "Market Freeze",
                description = "You cannot buy, sell, mortgage, or trade property for your next 2 turns.",
                effect = Effect.MarketFreeze,
                weight = 3f
            },
            new Card
            {
                title = "Emergency Fund",
                description = "If your balance is below $1,000M, receive $20M.",
                effect = Effect.EmergencyFund,
                weight = 3f
            },
            new Card
            {
                title = "Give to the Poor",
                description = "Give $10M to the player with the lowest cash balance.",
                effect = Effect.GiveToPoor,
                amount = 10,
                weight = 4f
            },
            new Card
            {
                title = "Community Donation",
                description = "Pay $10M for every complete color set you own.",
                effect = Effect.CommunityDonation,
                amount = 10,
                weight = 3f
            },
            new Card
            {
                title = "Property Assessment",
                description = "Pay $10M for every developed property you own.",
                effect = Effect.PropertyAssessment,
                amount = 10,
                weight = 3f
            },
            new Card
            {
                title = "Bank Penalty",
                description = "Pay 5% of your current cash balance to the bank.",
                effect = Effect.BankPenalty,
                weight = 4f
            },
            new Card
            {
                title = "Public Service Reward",
                description = "Receive $5M for every airport or utility you own.",
                effect = Effect.PublicServiceReward,
                amount = 5,
                weight = 3f
            },
            new Card
            {
                title = "Community Rescue",
                description = "If another player has more cash than you, receive $10M from the bank.",
                effect = Effect.CommunityRescue,
                amount = 10,
                weight = 5f
            },
            new Card
            {
                title = "Forced Property Payment",
                description = "Choose one property you own and pay 10% of its purchase price to the bank.",
                effect = Effect.ForcedPropertyPayment,
                weight = 2f
            },
            new Card
            {
                title = "Jail Relief",
                description = "If you are in Jail, leave immediately. Otherwise, receive $5M.",
                effect = Effect.JailRelief,
                amount = 5,
                weight = 2f
            },
            new Card
            {
                title = "Charity Reward",
                description = "If you own 3 or fewer properties, receive $20M. Otherwise, receive $5M.",
                effect = Effect.CharityReward,
                weight = 4f
            },
            new Card
            {
                title = "Community Jackpot",
                description = "Receive $50M if you have the lowest cash balance. Otherwise, receive $10M.",
                effect = Effect.CommunityJackpot,
                amount = 50,
                weight = 1f
            }
        };
    }
}
