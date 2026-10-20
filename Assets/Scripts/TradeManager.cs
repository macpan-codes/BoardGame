using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Validates, stores, and executes player-to-player trades.
/// Offers remain pending until the receiving player's turn begins.
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

        if (!player.IsActive)
            return false;

        if (HasPendingOffer)
            return false;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager != null &&
            gameManager.IsFinancialRecoveryActive(player))
        {
            return false;
        }

        if (IsMarketFrozen(player))
            return false;

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

        if (!proposer.IsActive ||
            !receiver.IsActive)
        {
            return false;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager != null &&
            (gameManager.IsFinancialRecoveryActive(proposer) ||
             gameManager.IsFinancialRecoveryActive(receiver)))
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

        // In the scene, the request controller may start inactive. Wake it
        // before raising OnOfferReceived so it can subscribe to the event.
        if (Application.isPlaying)
        {
            TradeRequestPanel requestPanel =
                FindFirstObjectByType<TradeRequestPanel>(
                    FindObjectsInactive.Include
                );

            if (requestPanel == null ||
                !requestPanel.PrepareForOfferDelivery(this))
            {
                error = "The incoming trade-request panel is unavailable.";
                NotifyFailed(offer, error);
                return false;
            }
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

        // Do not show the receiving-player popup yet. The request stays queued
        // while the proposer continues their normal turn.
        return true;
    }

    /// <summary>
    /// Delivers a queued offer only when its designated receiver's turn starts.
    /// </summary>
    public void NotifyReceiverTurnStarted(BoardPlayer currentPlayer)
    {
        if (!HasPendingOffer ||
            currentPlayer == null ||
            pendingOffer.Receiver != currentPlayer)
        {
            return;
        }

        if (Application.isPlaying)
        {
            TradeRequestPanel requestPanel =
                FindFirstObjectByType<TradeRequestPanel>(
                    FindObjectsInactive.Include
                );

            if (requestPanel != null)
            {
                requestPanel.PrepareForOfferDelivery(this);
            }
            else
            {
                Debug.LogError(
                    "TradeManager: No incoming trade-request panel was found " +
                    "when the receiver's turn started."
                );
            }
        }

        OnOfferReceived?.Invoke(pendingOffer);
    }

    [System.Obsolete("Pass the requesting player to cancel a trade safely.")]
    public void CancelPendingOffer()
    {
        GameNotificationUI.Show(
            "TRADE CANCELLATION REQUIRES THE REQUESTING PLAYER"
        );
    }

    public bool CancelPendingOffer(BoardPlayer requester)
    {
        if (!HasPendingOffer ||
            requester == null ||
            pendingOffer.Proposer != requester)
        {
            return false;
        }

        CancelPendingOfferInternal();
        return true;
    }

    private void CancelPendingOfferInternal()
    {
        if (!HasPendingOffer)
            return;

        TradeOffer cancelled = pendingOffer;
        cancelled.Status = TradeOfferStatus.Cancelled;

        ClearPendingOffer();

        OnOfferCancelled?.Invoke(cancelled);
    }

    public bool CancelPendingOfferForPlayer(BoardPlayer player)
    {
        if (player == null || !HasPendingOffer)
            return false;

        if (pendingOffer.Proposer != player &&
            pendingOffer.Receiver != player)
        {
            return false;
        }

        CancelPendingOfferInternal();

        Debug.Log(
            $"TRADE CANCELLED: Pending offer involving {player.PlayerName} " +
            "was cancelled because the player left the match."
        );

        GameNotificationUI.Show(
            $"{player.PlayerName.ToUpperInvariant()} LEFT THE MATCH — " +
            "PENDING TRADE CANCELLED"
        );

        return true;
    }

    [System.Obsolete("Pass the responding player to decline a trade safely.")]
    public bool DeclineOffer()
    {
        GameNotificationUI.Show(
            "TRADE RESPONSE FAILED — RECEIVING PLAYER REQUIRED"
        );
        return false;
    }

    public bool DeclineOffer(
        BoardPlayer responder,
        TradeOffer expectedOffer)
    {
        if (expectedOffer == null ||
            !ReferenceEquals(pendingOffer, expectedOffer))
        {
            GameNotificationUI.Show(
                "THIS TRADE REQUEST IS NO LONGER CURRENT"
            );
            return false;
        }

        return DeclineOffer(responder);
    }

    public bool DeclineOffer(BoardPlayer responder)
    {
        if (!CanRespondToPendingOffer(responder, out string responseError))
        {
            if (!string.IsNullOrWhiteSpace(responseError))
                GameNotificationUI.Show(responseError);
            return false;
        }

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

    [System.Obsolete("Pass the responding player to accept a trade safely.")]
    public bool AcceptOffer()
    {
        GameNotificationUI.Show(
            "TRADE RESPONSE FAILED — RECEIVING PLAYER REQUIRED"
        );
        return false;
    }

    public bool AcceptOffer(
        BoardPlayer responder,
        TradeOffer expectedOffer)
    {
        if (expectedOffer == null ||
            !ReferenceEquals(pendingOffer, expectedOffer))
        {
            GameNotificationUI.Show(
                "THIS TRADE REQUEST IS NO LONGER CURRENT"
            );
            return false;
        }

        return AcceptOffer(responder);
    }

    public bool AcceptOffer(BoardPlayer responder)
    {
        if (!CanRespondToPendingOffer(responder, out string responseError))
        {
            if (!string.IsNullOrWhiteSpace(responseError))
                GameNotificationUI.Show(responseError);
            return false;
        }

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

    private bool CanRespondToPendingOffer(
        BoardPlayer responder,
        out string error)
    {
        error = null;

        if (!HasPendingOffer)
        {
            error = "There is no pending trade offer.";
            return false;
        }

        if (responder == null)
        {
            error = "The responding player is missing.";
            return false;
        }

        if (responder != pendingOffer.Receiver)
        {
            error =
                $"Only {pendingOffer.Receiver.PlayerName} can respond to this offer.";
            return false;
        }

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

        if (!proposer.IsActive ||
            !receiver.IsActive)
        {
            error = "Bankrupt or left-game players cannot trade.";
            return false;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null || gameManager.GameOver)
        {
            error = "Trading is unavailable because the match is not active.";
            return false;
        }

        if (!gameManager.IsPlayerTurn(proposer))
        {
            error = "Only the current player can send a trade offer.";
            return false;
        }

        if (gameManager.IsFinancialRecoveryActive(proposer) ||
            gameManager.IsFinancialRecoveryActive(receiver))
        {
            error =
                "Trade is unavailable during financial recovery.";
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
                if (property.Houses > 0 || property.HasHotel)
                {
                    error =
                        $"{property.SpaceName} must have all buildings removed before it can be traded.";
                }
                else if (property.IsMortgaged)
                {
                    error =
                        $"{property.SpaceName} must be unmortgaged before it can be traded.";
                }
                else
                {
                    error =
                        $"{property.SpaceName} cannot be traded.";
                }

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
        public int PreviousOwnerLandingCount;
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

        int previousOwnerLandingCount =
            property.OwnerLandingCount;

        property.TransferOwnershipForTrade(to);

        if (property.Owner != to)
        {
            error =
                $"Ownership transfer failed for {property.SpaceName}.";
            return false;
        }

        transferred.Add(
            new TransferredProperty
            {
                Property = property,
                PreviousOwner = from,
                PreviousOwnerLandingCount = previousOwnerLandingCount
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
                    entry.PreviousOwner,
                    entry.PreviousOwnerLandingCount
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

        // Do not transfer developed or mortgaged properties under the current
        // rules; those states need separate sale/mortgage settlement first.
        if (property.Houses > 0 ||
            property.HasHotel ||
            property.IsMortgaged)
        {
            return false;
        }

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
