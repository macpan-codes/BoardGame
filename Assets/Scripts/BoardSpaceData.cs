using System;

/// <summary>
/// Holds the configuration for one board space.
/// This is plain data — it does not run any gameplay logic.
/// </summary>
[Serializable]
public struct BoardSpaceData
{
    public int index;
    public string displayName;
    public BoardSpaceType type;
    public int purchasePrice;

    public BoardSpaceData(int index, string displayName, BoardSpaceType type, int purchasePrice)
    {
        this.index = index;
        this.displayName = displayName;
        this.type = type;
        this.purchasePrice = purchasePrice;
    }
}
