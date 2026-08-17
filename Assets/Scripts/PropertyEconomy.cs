using System;
using UnityEngine;

/// <summary>
/// Stores the economic information for a property.
/// Values are calculated from the purchase price for now.
/// Individual property data can replace these formulas later.
/// </summary>
[Serializable]
public class PropertyEconomy
{
    public int baseRent;
    public int houseRent;
    public int twoHouseRent;
    public int threeHouseRent;
    public int fourHouseRent;
    public int hotelRent;
    public int houseCost;
    public int hotelCost;
    public int mortgageValue;

    // --- Formula ratios (change these to adjust all properties at once) ---

    private const float BaseRentRatio = 0.10f;
    private const float OneHouseRentRatio = 0.20f;
    private const float TwoHouseRentRatio = 0.30f;
    private const float ThreeHouseRentRatio = 0.40f;
    private const float FourHouseRentRatio = 0.50f;
    private const float HotelRentRatio = 0.75f;
    private const float HouseCostRatio = 0.50f;
    private const float HotelCostRatio = 0.50f;
    private const float MortgageRatio = 0.50f;

    /// <summary>
    /// Creates prototype economic values based on the property's purchase price.
    /// </summary>
    public static PropertyEconomy FromPurchasePrice(int purchasePrice)
    {
        return new PropertyEconomy
        {
            baseRent = RoundPrice(purchasePrice * BaseRentRatio),
            houseRent = RoundPrice(purchasePrice * OneHouseRentRatio),
            twoHouseRent = RoundPrice(purchasePrice * TwoHouseRentRatio),
            threeHouseRent = RoundPrice(purchasePrice * ThreeHouseRentRatio),
            fourHouseRent = RoundPrice(purchasePrice * FourHouseRentRatio),
            hotelRent = RoundPrice(purchasePrice * HotelRentRatio),
            houseCost = RoundPrice(purchasePrice * HouseCostRatio),
            hotelCost = RoundPrice(purchasePrice * HotelCostRatio),
            mortgageValue = RoundPrice(purchasePrice * MortgageRatio)
        };
    }

    // Rounds to the nearest 5 for cleaner display values (e.g. 60 -> 6, not 6.0).
    private static int RoundPrice(float value)
    {
        return Mathf.RoundToInt(value / 5f) * 5;
    }
}
