using System.Collections.Generic;

/// <summary>
/// Data model for a player-to-player trade proposal.
/// Keeps trade state separate from UI state.
/// </summary>
public class TradeOffer
{
    public BoardPlayer Proposer { get; set; }
    public BoardPlayer Receiver { get; set; }

    public long OfferedMoney { get; set; }
    public long RequestedMoney { get; set; }

    public List<BoardSpace> OfferedProperties { get; }
    public List<BoardSpace> RequestedProperties { get; }

    public TradeOfferStatus Status { get; set; }
    public string FailureReason { get; set; }

    public TradeOffer()
    {
        OfferedProperties = new List<BoardSpace>();
        RequestedProperties = new List<BoardSpace>();
        Status = TradeOfferStatus.Pending;
    }

    public TradeOffer Clone()
    {
        TradeOffer copy = new TradeOffer
        {
            Proposer = Proposer,
            Receiver = Receiver,
            OfferedMoney = OfferedMoney,
            RequestedMoney = RequestedMoney,
            Status = Status,
            FailureReason = FailureReason
        };

        copy.OfferedProperties.AddRange(OfferedProperties);
        copy.RequestedProperties.AddRange(RequestedProperties);

        return copy;
    }

    public bool HasAnyAssets =>
        OfferedMoney > 0 ||
        RequestedMoney > 0 ||
        OfferedProperties.Count > 0 ||
        RequestedProperties.Count > 0;

    public bool HasOfferedAssets =>
        OfferedMoney > 0 ||
        OfferedProperties.Count > 0;

    public bool HasRequestedAssets =>
        RequestedMoney > 0 ||
        RequestedProperties.Count > 0;
}

public enum TradeOfferStatus
{
    Pending,
    Accepted,
    Declined,
    Cancelled,
    Failed,
    Completed
}
