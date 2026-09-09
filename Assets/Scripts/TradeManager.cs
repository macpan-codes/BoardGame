using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates, stores, and executes player-to-player trades.
/// Does not modify turn state — trading is overlay UI only.
/// </summary>
public class TradeManager : MonoBehaviour
{
    public static TradeManager Instance { get; private set; }

    private TradeOffer pendingOffer;

    public event Action<TradeOffer> OnOfferSent;
    public event Action<TradeOffer> OnOfferReceived;
    public event Action<TradeOffer> OnOfferCompleted;
    public event Action<TradeOffer> OnOfferDeclined;
    public event Action<TradeOffer> OnOfferCancelled;
    public event Action<TradeOffer, string> OnOfferFailed;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    // ============================================================
    // PROPERTIES
    // ============================================================

    public bool HasPendingOffer =>
        pendingOffer != null &&
        pendingOffer.Status == TradeOfferStatus.Pending;

    public TradeOffer PendingOffer =>
        pendingOffer;

    private bool IsMarketFrozen(BoardPlayer player)
    {
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        return gameManager != null &&
               gameManager.IsMarketFrozen(player);
    }

    // ============================================================
    // TRADE ACCESS
    // ============================================================

    public bool CanOpenTrade(BoardPlayer player)
    {
        if (player == null)
            return false;

        if (player.IsBankrupt)
            return false;

        if (IsMarketFrozen(player))
            return false;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null ||
            gameManager.GameOver)
        {
            return false;
        }

        if (!gameManager.IsPlayerTurn(player))
            return false;

        if (player.IsMoving)
            return false;

        return true;
    }

    public bool CanTradeWith(
        BoardPlayer proposer,
        BoardPlayer receiver)
    {
        if (proposer == null ||
            receiver == null)
        {
            return false;
        }

        if (proposer == receiver)
            return false;

        if (proposer.IsBankrupt ||
            receiver.IsBankrupt)
        {
            return false;
        }

        if (IsMarketFrozen(proposer) ||
            IsMarketFrozen(receiver))
        {
            return false;
        }

        return true;
    }

    // ============================================================
    // SEND / CANCEL
    // ============================================================

    public bool SendOffer(TradeOffer offer)
    {
        if (offer == null)
            return false;

        if (HasPendingOffer)
        {
            NotifyFailed(
                offer,
                "Another trade is already pending."
            );

            return false;
        }

        if (!ValidateOffer(
                offer,
                out string error))
        {
            NotifyFailed(offer, error);
            return false;
        }

        offer.Status = TradeOfferStatus.Pending;
        pendingOffer = offer.Clone();

        Debug.Log(
            $"TRADE SENT: " +
            $"{offer.Proposer.PlayerName} -> " +
            $"{offer.Receiver.PlayerName}"
        );

        GameNotificationUI.Show(
            $"{offer.Proposer.PlayerName.ToUpperInvariant()} " +
            $"SENT A TRADE REQUEST TO " +
            $"{offer.Receiver.PlayerName.ToUpperInvariant()}"
        );

        OnOfferSent?.Invoke(pendingOffer);
        OnOfferReceived?.Invoke(pendingOffer);

        return true;
    }

    public void CancelPendingOffer()
    {
        if (!HasPendingOffer)
            return;

        TradeOffer cancelled = pendingOffer;
        cancelled.Status = TradeOfferStatus.Cancelled;

        ClearPendingOffer();

        OnOfferCancelled?.Invoke(cancelled);
    }

    public bool DeclineOffer()
    {
        if (!HasPendingOffer)
            return false;

        TradeOffer declined = pendingOffer;
        declined.Status = TradeOfferStatus.Declined;

        GameNotificationUI.Show(
            $"{declined.Receiver.PlayerName.ToUpperInvariant()} " +
            $"DECLINED THE TRADE"
        );

        ClearPendingOffer();

        OnOfferDeclined?.Invoke(declined);

        return true;
    }

    // ============================================================
    // ACCEPT
    // ============================================================

    public bool AcceptOffer()
    {
        if (!HasPendingOffer)
            return false;

        TradeOffer offer = pendingOffer;

        if (!ValidateOffer(
                offer,
                out string error))
        {
            offer.Status = TradeOfferStatus.Failed;
            offer.FailureReason = error;

            GameNotificationUI.Show(
                "TRADE FAILED — OFFER NO LONGER VALID"
            );

            NotifyFailed(offer, error);
            ClearPendingOffer();

            return false;
        }

        if (!ExecuteOffer(offer, out error))
        {
            offer.Status = TradeOfferStatus.Failed;
            offer.FailureReason = error;

            GameNotificationUI.Show(
                $"{offer.Proposer.PlayerName.ToUpperInvariant()} " +
                "TRADE FAILED"
            );

            NotifyFailed(offer, error);
            ClearPendingOffer();

            return false;
        }

        offer.Status = TradeOfferStatus.Completed;

        GameNotificationUI.Show(
            $"{offer.Proposer.PlayerName.ToUpperInvariant()} " +
            $"TRADED WITH " +
            $"{offer.Receiver.PlayerName.ToUpperInvariant()}"
        );

        ClearPendingOffer();

        RefreshAllGameUI();

        OnOfferCompleted?.Invoke(offer);

        return true;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    public bool ValidateOffer(
        TradeOffer offer,
        out string error)
    {
        error = null;

        if (offer == null)
        {
            error = "Trade offer is missing.";
            return false;
        }

        BoardPlayer proposer = offer.Proposer;
        BoardPlayer receiver = offer.Receiver;

        if (proposer == null ||
            receiver == null)
        {
            error = "Trade players are missing.";
            return false;
        }

        if (proposer == receiver)
        {
            error = "You cannot trade with yourself.";
            return false;
        }

        if (proposer.IsBankrupt ||
            receiver.IsBankrupt)
        {
            error = "Bankrupt players cannot trade.";
            return false;
        }

        if (IsMarketFrozen(proposer))
        {
            error =
                $"{proposer.PlayerName} cannot trade because " +
                "Market Freeze is active.";
            return false;
        }

        if (IsMarketFrozen(receiver))
        {
            error =
                $"{receiver.PlayerName} cannot trade because " +
                "Market Freeze is active.";
            return false;
        }

        if (offer.OfferedMoney < 0 ||
            offer.RequestedMoney < 0)
        {
            error = "Money amounts cannot be negative.";
            return false;
        }

        if (proposer.Money < offer.OfferedMoney)
        {
            error =
                $"{proposer.PlayerName} does not have enough money.";
            return false;
        }

        if (receiver.Money < offer.RequestedMoney)
        {
            error =
                $"{receiver.PlayerName} does not have enough money.";
            return false;
        }

        if (!ValidatePropertyList(
                offer.OfferedProperties,
                proposer,
                out error))
        {
            return false;
        }

        if (!ValidatePropertyList(
                offer.RequestedProperties,
                receiver,
                out error))
        {
            return false;
        }

        if (!offer.HasAnyAssets)
        {
            error = "The trade must include money or properties.";
            return false;
        }

        HashSet<BoardSpace> allSpaces =
            new HashSet<BoardSpace>();

        foreach (BoardSpace space in offer.OfferedProperties)
        {
            if (!allSpaces.Add(space))
            {
                error = "Duplicate property in offer.";
                return false;
            }
        }

        foreach (BoardSpace space in offer.RequestedProperties)
        {
            if (!allSpaces.Add(space))
            {
                error = "Duplicate property in request.";
                return false;
            }
        }

        return true;
    }

    private bool ValidatePropertyList(
        List<BoardSpace> properties,
        BoardPlayer expectedOwner,
        out string error)
    {
        error = null;

        if (properties == null)
            return true;

        HashSet<BoardSpace> seen =
            new HashSet<BoardSpace>();

        foreach (BoardSpace property in properties)
        {
            if (property == null)
            {
                error = "A property in the trade is missing.";
                return false;
            }

            if (!seen.Add(property))
            {
                error = "Duplicate property selected.";
                return false;
            }

            if (!IsTradableProperty(property))
            {
                error =
                    $"{property.SpaceName} cannot be traded.";
                return false;
            }

            if (property.Owner != expectedOwner)
            {
                error =
                    $"{expectedOwner.PlayerName} no longer owns " +
                    $"{property.SpaceName}.";
                return false;
            }
        }

        return true;
    }

    // ============================================================
    // EXECUTION
    // ============================================================

    private bool ExecuteOffer(
        TradeOffer offer,
        out string error)
    {
        error = null;

        BoardPlayer proposer = offer.Proposer;
        BoardPlayer receiver = offer.Receiver;

        int offeredMoney = SafeInt(offer.OfferedMoney);
        int requestedMoney = SafeInt(offer.RequestedMoney);

        bool proposerPaid = false;
        bool receiverPaid = false;

        List<TransferredProperty> transferred =
            new List<TransferredProperty>();

        // --------------------------------------------------------
        // REMOVE MONEY FIRST
        // --------------------------------------------------------

        if (offeredMoney > 0)
        {
            if (!proposer.RemoveMoney(offeredMoney))
            {
                error =
                    $"{proposer.PlayerName} cannot pay offered money.";
                return false;
            }

            proposerPaid = true;
        }

        if (requestedMoney > 0)
        {
            if (!receiver.RemoveMoney(requestedMoney))
            {
                if (proposerPaid)
                {
                    proposer.AddMoney(offeredMoney);
                }

                error =
                    $"{receiver.PlayerName} cannot pay requested money.";
                return false;
            }

            receiverPaid = true;
        }

        // --------------------------------------------------------
        // TRANSFER PROPERTIES
        // --------------------------------------------------------

        foreach (BoardSpace property in offer.OfferedProperties)
        {
            if (!TransferPropertySafe(
                    property,
                    proposer,
                    receiver,
                    transferred,
                    out error))
            {
                RollbackExecution(
                    proposer,
                    receiver,
                    offeredMoney,
                    requestedMoney,
                    proposerPaid,
                    receiverPaid,
                    transferred
                );

                return false;
            }
        }

        foreach (BoardSpace property in offer.RequestedProperties)
        {
            if (!TransferPropertySafe(
                    property,
                    receiver,
                    proposer,
                    transferred,
                    out error))
            {
                RollbackExecution(
                    proposer,
                    receiver,
                    offeredMoney,
                    requestedMoney,
                    proposerPaid,
                    receiverPaid,
                    transferred
                );

                return false;
            }
        }

        // --------------------------------------------------------
        // PAY MONEY
        // --------------------------------------------------------

        if (offeredMoney > 0)
        {
            receiver.AddMoney(offeredMoney);

            GameNotificationUI.Show(
                $"{receiver.PlayerName.ToUpperInvariant()} " +
                $"RECEIVED ${offeredMoney:N0}M FROM TRADE"
            );
        }

        if (requestedMoney > 0)
        {
            proposer.AddMoney(requestedMoney);

            GameNotificationUI.Show(
                $"{proposer.PlayerName.ToUpperInvariant()} " +
                $"RECEIVED ${requestedMoney:N0}M FROM TRADE"
            );
        }

        Debug.Log(
            $"TRADE COMPLETED: " +
            $"{proposer.PlayerName} <-> " +
            $"{receiver.PlayerName}"
        );

        return true;
    }

    private struct TransferredProperty
    {
        public BoardSpace Property;
        public BoardPlayer PreviousOwner;
    }

    private bool TransferPropertySafe(
        BoardSpace property,
        BoardPlayer from,
        BoardPlayer to,
        List<TransferredProperty> transferred,
        out string error)
    {
        error = null;

        if (property == null ||
            from == null ||
            to == null)
        {
            error = "Invalid property transfer.";
            return false;
        }

        if (property.Owner != from)
        {
            error =
                $"{from.PlayerName} no longer owns " +
                $"{property.SpaceName}.";
            return false;
        }

        property.TransferOwnershipForTrade(to);

        transferred.Add(
            new TransferredProperty
            {
                Property = property,
                PreviousOwner = from
            }
        );

        Debug.Log(
            $"PROPERTY TRADED: " +
            $"{property.SpaceName} | " +
            $"{from.PlayerName} -> {to.PlayerName}"
        );

        return true;
    }

    private void RollbackExecution(
        BoardPlayer proposer,
        BoardPlayer receiver,
        int offeredMoney,
        int requestedMoney,
        bool proposerPaid,
        bool receiverPaid,
        List<TransferredProperty> transferred)
    {
        for (int i = transferred.Count - 1; i >= 0; i--)
        {
            TransferredProperty entry =
                transferred[i];

            if (entry.Property != null &&
                entry.PreviousOwner != null)
            {
                entry.Property.TransferOwnershipForTrade(
                    entry.PreviousOwner
                );
            }
        }

        if (receiverPaid && requestedMoney > 0)
        {
            receiver.AddMoney(requestedMoney);
        }

        if (proposerPaid && offeredMoney > 0)
        {
            proposer.AddMoney(offeredMoney);
        }

        Debug.LogWarning(
            "TRADE ROLLED BACK: Transfer failed."
        );
    }

    // ============================================================
    // PROPERTY HELPERS
    // ============================================================

    public static bool IsTradableProperty(
        BoardSpace property)
    {
        if (property == null)
            return false;

        if (!property.IsOwned)
            return false;

        return
            property.IsProperty ||
            property.IsRailroad ||
            property.IsUtility;
    }

    public static BoardSpace[] GetTradablePropertiesFor(
        BoardPlayer player)
    {
        if (player == null)
            return Array.Empty<BoardSpace>();

        BoardSpace[] allSpaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );

        List<BoardSpace> owned =
            new List<BoardSpace>();

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null)
                continue;

            if (!IsTradableProperty(space))
                continue;

            if (space.Owner != player)
                continue;

            owned.Add(space);
        }

        owned.Sort(
            (a, b) =>
                string.Compare(
                    a.SpaceName,
                    b.SpaceName,
                    StringComparison.OrdinalIgnoreCase
                )
        );

        return owned.ToArray();
    }

    public static string FormatMoney(long amount)
    {
        return $"${amount:N0}M";
    }

    public static string DescribeProperty(
        BoardSpace property)
    {
        if (property == null)
            return "Unknown";

        string description =
            property.SpaceName;

        if (property.HasHotel)
        {
            description += " (Hotel)";
        }
        else if (property.Houses > 0)
        {
            description +=
                $" ({property.Houses} House" +
                (property.Houses > 1 ? "s" : "") +
                ")";
        }

        if (property.IsMortgaged)
        {
            description += " [Mortgaged]";
        }

        return description;
    }

    // ============================================================
    // UI REFRESH
    // ============================================================

    public void RefreshAllGameUI()
    {
        PropertyInfoPanel infoPanel =
            FindFirstObjectByType<PropertyInfoPanel>(
                FindObjectsInactive.Include
            );

        if (infoPanel != null)
        {
            infoPanel.RefreshIfVisible();
        }

        PropertyManagementPanel managementPanel =
            FindFirstObjectByType<PropertyManagementPanel>(
                FindObjectsInactive.Include
            );

        if (managementPanel != null)
        {
            managementPanel.RefreshIfVisible();
        }

        TradePanel tradePanel =
            FindFirstObjectByType<TradePanel>(
                FindObjectsInactive.Include
            );

        if (tradePanel != null)
        {
            tradePanel.RefreshAfterTrade();
        }
    }

    // ============================================================
    // CLEAR
    // ============================================================

    private void ClearPendingOffer()
    {
        pendingOffer = null;
    }

    private void NotifyFailed(
        TradeOffer offer,
        string error)
    {
        if (offer != null)
        {
            offer.Status = TradeOfferStatus.Failed;
            offer.FailureReason = error;
        }

        Debug.LogWarning(
            $"TRADE FAILED: {error}"
        );

        OnOfferFailed?.Invoke(offer, error);
    }

    private int SafeInt(long value)
    {
        if (value <= 0)
            return 0;

        return value > int.MaxValue
            ? int.MaxValue
            : (int)value;
    }
}
