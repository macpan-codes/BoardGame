using UnityEngine;
using UnityEngine.UI;

public class BoardSpace : MonoBehaviour
{
    [Header("Basic Information")]
    [SerializeField] private string spaceName = "Space";
    [SerializeField] private BoardSpaceType spaceType;
    [SerializeField] private int boardIndex = -1;

    [Header("Property")]
    [SerializeField] private int purchasePrice;
    [SerializeField] private int baseRent;
    [SerializeField] private BoardPlayer owner;
    [SerializeField] private bool isOwned;
    [SerializeField] private PropertyGroup propertyGroup;

    [Header("Rent")]
    [SerializeField] private int rentLevel1;
    [SerializeField] private int rentLevel2;
    [SerializeField] private int rentLevel3;
    [SerializeField] private int rentLevel4;
    [SerializeField] private int hotelRent;

    [Header("Railroad / Utility")]
    [SerializeField] private int railroadRent = 25;
    [SerializeField] private int utilityMultiplierOne = 4;
    [SerializeField] private int utilityMultiplierTwo = 10;

    [Header("Buildings")]
    [SerializeField] private int houseCost;
    [SerializeField] private int hotelCost;

    [SerializeField]
    [Range(0, 4)]
    private int houses;

    [SerializeField] private bool hotel;

    [Header("Mortgage")]
    [SerializeField] private bool mortgaged;
    [SerializeField] private int mortgageValue;

    [Header("Ownership UI")]
    [SerializeField] private PropertyOwnershipMarker ownershipMarker;

    [Header("Custom Development")]
    [SerializeField]
    private int ownerLandingCount;

    [Header("Building Visual")]
    [SerializeField] private bool showBuildingVisual = true;

    [SerializeField]
    private RectTransform buildingVisualContainer;

    private PropertyBuildingVisual buildingVisual;

    // ============================================================
    // PUBLIC PROPERTIES
    // ============================================================

    public string SpaceName =>
        string.IsNullOrWhiteSpace(spaceName)
            ? gameObject.name
            : spaceName;

    public BoardSpaceType SpaceType =>
        spaceType;

    public int BoardIndex =>
        boardIndex;

    public int PurchasePrice =>
        purchasePrice;

    public int BaseRent =>
        baseRent;

    public BoardPlayer Owner =>
        owner;

    public bool IsOwned =>
        isOwned && owner != null;

    public PropertyGroup PropertyGroup =>
        propertyGroup;

    public int Houses =>
        houses;

    public bool HasHotel =>
        hotel;

    public bool IsMortgaged =>
        mortgaged;

    /// <summary>
    /// Effective mortgage value.
    ///
    /// Inspector value > 0 = explicit per-property override.
    /// Inspector value = 0 = use shared PropertyEconomy formula.
    /// </summary>
    public int MortgageValue =>
        mortgageValue > 0
            ? mortgageValue
            : PropertyEconomy
                .FromPurchasePrice(purchasePrice)
                .mortgageValue;

    public int EffectiveMortgageValue =>
        MortgageValue;

    public int HouseCost =>
        houseCost;

    public int HotelCost =>
        hotelCost;

    public int OwnerLandingCount =>
        ownerLandingCount;

    public bool IsProperty =>
        spaceType == BoardSpaceType.Property;

    public bool IsRailroad =>
        spaceType == BoardSpaceType.Airport;

    public bool IsUtility =>
        spaceType == BoardSpaceType.Utility;

    public int EffectiveHouseCost =>
        houseCost > 0
            ? houseCost
            : PropertyEconomy
                .FromPurchasePrice(purchasePrice)
                .houseCost;

    public int EffectiveHotelCost =>
        hotelCost > 0
            ? hotelCost
            : PropertyEconomy
                .FromPurchasePrice(purchasePrice)
                .hotelCost;

    // ============================================================
    // CUSTOM DEVELOPMENT
    //
    // 1st landing = purchase
    // 2nd landing = 1 house
    // 3rd landing = 2 houses
    // 4th landing = 3 houses
    // 5th landing = 4 houses
    // 6th landing = hotel
    //
    // Property groups affect RENT only.
    // ============================================================

    public int MaximumAllowedHouses =>
        Mathf.Clamp(
            ownerLandingCount - 1,
            0,
            4
        );

    public bool CanBuildAnotherHouseFor(
        BoardPlayer player)
    {
        if (player == null)
            return false;

        if (owner != player)
            return false;

        if (!IsProperty)
            return false;

        if (mortgaged)
            return false;

        if (hotel)
            return false;

        if (houses >= 4)
            return false;

        if (player.CurrentSpaceIndex != boardIndex)
            return false;

        return houses < MaximumAllowedHouses;
    }

    public bool CanBuildHotelFor(
        BoardPlayer player)
    {
        if (player == null)
            return false;

        if (owner != player)
            return false;

        if (!IsProperty)
            return false;

        if (mortgaged)
            return false;

        if (hotel)
            return false;

        if (player.CurrentSpaceIndex != boardIndex)
            return false;

        if (houses < 4)
            return false;

        return ownerLandingCount >= 6;
    }

    public void RegisterOwnerLanding(
        BoardPlayer player)
    {
        if (player == null)
            return;

        if (owner != player)
            return;

        if (!IsProperty)
            return;

        if (mortgaged)
            return;

        ownerLandingCount++;

        RefreshBuildingVisual();

        Debug.Log(
            $"{player.PlayerName} landed on " +
            $"{SpaceName}. " +
            $"Development landing count: " +
            $"{ownerLandingCount}. " +
            $"Maximum houses: " +
            $"{MaximumAllowedHouses}/4"
        );
    }

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (ownershipMarker == null)
        {
            ownershipMarker =
                GetComponentInChildren<PropertyOwnershipMarker>(
                    true
                );
        }

        RefreshOwnershipMarker();

        SetupBuildingVisual();
        RefreshBuildingVisual();
    }

    // ============================================================
    // BOARD INDEX
    // ============================================================

    public void SetBoardIndex(int index)
    {
        boardIndex = index;
    }

    // ============================================================
    // OWNERSHIP MARKER
    // ============================================================

    public void AssignOwnershipMarker(
        PropertyOwnershipMarker marker)
    {
        ownershipMarker = marker;
        RefreshOwnershipMarker();
    }

    public void RefreshOwnershipMarker()
    {
        if (ownershipMarker == null)
            return;

        if (IsOwned)
            ownershipMarker.SetOwner(owner);
        else
            ownershipMarker.Hide();
    }

    // ============================================================
    // RENT
    // ============================================================

    public int GetRent()
    {
        if (mortgaged)
            return 0;

        // Airports/railroads use the dedicated ownership-based rent table.
        if (IsRailroad)
            return GetRailroadRent();

        // Utilities are calculated from the dice roll in GetUtilityRent().
        // Keep this fallback for callers that ask for GetRent() directly.
        if (IsUtility)
        {
            return baseRent > 0
                ? baseRent
                : PropertyEconomy
                    .FromPurchasePrice(purchasePrice)
                    .baseRent;
        }

        // Use PropertyEconomy as the authoritative fallback whenever a
        // per-property rent field has not been explicitly configured.
        // This keeps ordinary rent, property-info displays, and Chance-card
        // calculations on the same economy model.
        PropertyEconomy economy =
            PropertyEconomy.FromPurchasePrice(
                purchasePrice
            );

        int calculatedBaseRent =
            baseRent > 0
                ? baseRent
                : economy.baseRent;

        if (hotel)
        {
            return hotelRent > 0
                ? hotelRent
                : economy.hotelRent;
        }

        int rent;

        switch (houses)
        {
            case 1:
                rent =
                    rentLevel1 > 0
                        ? rentLevel1
                        : economy.houseRent;
                break;

            case 2:
                rent =
                    rentLevel2 > 0
                        ? rentLevel2
                        : economy.twoHouseRent;
                break;

            case 3:
                rent =
                    rentLevel3 > 0
                        ? rentLevel3
                        : economy.threeHouseRent;
                break;

            case 4:
                rent =
                    rentLevel4 > 0
                        ? rentLevel4
                        : economy.fourHouseRent;
                break;

            default:
                rent = calculatedBaseRent;
                break;
        }

        // Complete group multiplier applies only
        // when there are no buildings.
        if (houses == 0 &&
            !hotel &&
            HasCompletePropertyGroup())
        {
            int multiplier =
                propertyGroup != null
                    ? propertyGroup.CompletedGroupRentMultiplier
                    : 2;

            rent *= Mathf.Max(1, multiplier);
        }

        return rent;
    }

    // ============================================================
    // RAILROADS / AIRPORTS
    // ============================================================

    private int GetRailroadRent()
    {
        if (!IsOwned)
            return railroadRent;

        int count =
            CountOwnedRailroads();

        switch (count)
        {
            case 1:
                return 25;

            case 2:
                return 50;

            case 3:
                return 100;

            case 4:
                return 200;

            default:
                return railroadRent;
        }
    }

    // ============================================================
    // UTILITIES
    // ============================================================

    public int GetUtilityRent(
        int diceRoll)
    {
        if (!IsUtility ||
            mortgaged)
        {
            return 0;
        }

        int count =
            CountOwnedUtilities();

        return count >= 2
            ? diceRoll * utilityMultiplierTwo
            : diceRoll * utilityMultiplierOne;
    }

    // ============================================================
    // PROPERTY GROUP
    // ============================================================

    public bool HasCompletePropertyGroup()
    {
        if (!IsProperty ||
            propertyGroup == null ||
            owner == null)
        {
            return false;
        }

        BoardSpace[] allSpaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        bool foundAny = false;

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null)
                continue;

            if (!space.IsProperty)
                continue;

            if (space.PropertyGroup != propertyGroup)
                continue;

            foundAny = true;

            if (space.Owner != owner)
                return false;
        }

        return foundAny;
    }

    public int GetPropertyGroupSize()
    {
        if (propertyGroup == null)
            return 0;

        BoardSpace[] allSpaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null)
                continue;

            if (!space.IsProperty)
                continue;

            if (space.PropertyGroup == propertyGroup)
                count++;
        }

        return count;
    }

    public int GetOwnedPropertyGroupCount()
    {
        if (propertyGroup == null ||
            owner == null)
        {
            return 0;
        }

        BoardSpace[] allSpaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null)
                continue;

            if (!space.IsProperty)
                continue;

            if (space.PropertyGroup != propertyGroup)
                continue;

            if (space.Owner == owner)
                count++;
        }

        return count;
    }

    // ============================================================
    // OWNERSHIP
    // ============================================================

    public void SetOwner(
        BoardPlayer newOwner)
    {
        bool ownershipChanged =
            owner != newOwner;

        owner = newOwner;
        isOwned = newOwner != null;

        if (ownershipChanged ||
            newOwner == null)
        {
            ownerLandingCount = 0;
            houses = 0;
            hotel = false;
            mortgaged = false;
        }

        RefreshOwnershipMarker();
        RefreshBuildingVisual();

        Debug.Log(
            $"OWNERSHIP CHANGED: " +
            $"{SpaceName} -> " +
            (owner != null
                ? owner.PlayerName
                : "BANK")
        );
    }


    public void TransferOwnershipForTrade(
        BoardPlayer newOwner)
    {
        if (newOwner == null)
            return;

        if (!IsOwned)
            return;

        BoardPlayer previousOwner =
            owner;

        owner = newOwner;
        isOwned = true;

        RefreshOwnershipMarker();
        RefreshBuildingVisual();

        Debug.Log(
            $"TRADE OWNERSHIP TRANSFER: " +
            $"{SpaceName} | " +
            $"{previousOwner.PlayerName} -> " +
            $"{newOwner.PlayerName} | " +
            $"Houses: {houses} | " +
            $"Hotel: {hotel} | " +
            $"Landings: {ownerLandingCount}"
        );
    }




    public void TransferOwnershipForBankruptcy(
        BoardPlayer newOwner)
    {
        if (newOwner == null ||
            !IsOwned)
        {
            return;
        }

        BoardPlayer previousOwner =
            owner;

        // Buildings do not transfer through bankruptcy. The mortgage flag
        // remains because the property itself is transferred as-is.
        houses = 0;
        hotel = false;
        ownerLandingCount = 0;

        owner = newOwner;
        isOwned = true;

        RefreshOwnershipMarker();
        RefreshBuildingVisual();

        Debug.Log(
            $"BANKRUPTCY PROPERTY TRANSFER: " +
            $"{SpaceName} | " +
            $"{previousOwner?.PlayerName ?? "UNKNOWN"} -> " +
            $"{newOwner.PlayerName} | " +
            $"Mortgage: {mortgaged}"
        );
    }

    public void ClearOwner()
    {
        owner = null;
        isOwned = false;

        ownerLandingCount = 0;
        houses = 0;
        hotel = false;
        mortgaged = false;

        RefreshOwnershipMarker();
        RefreshBuildingVisual();
    }

    // ============================================================
    // PURCHASE
    // ============================================================

    public bool CanBePurchased()
    {
        bool validType =
            spaceType == BoardSpaceType.Property ||
            spaceType == BoardSpaceType.Airport ||
            spaceType == BoardSpaceType.Utility;

        return validType &&
               !IsOwned &&
               purchasePrice > 0;
    }

    public bool Purchase(
        BoardPlayer player)
    {
        if (player == null)
            return false;

        if (!CanBePurchased())
            return false;

        if (player.IsBankrupt)
            return false;

        if (player.Money < purchasePrice)
            return false;

        if (!player.RemoveMoney(
                purchasePrice))
        {
            return false;
        }

        SetOwner(player);

        if (IsProperty)
            RegisterOwnerLanding(player);

        Debug.Log(
            $"{player.PlayerName} purchased " +
            $"{SpaceName} for " +
            $"${purchasePrice:N0}."
        );

        return true;
    }

    // ============================================================
    // BUILDINGS
    // ============================================================

    public bool CanBuildHouse()
    {
        if (owner == null)
            return false;

        return CanBuildAnotherHouseFor(owner);
    }

    public bool AddHouse()
    {
        if (owner == null)
            return false;

        BoardPlayer player = owner;

        if (!CanBuildAnotherHouseFor(player))
            return false;

        int cost =
            EffectiveHouseCost;

        if (cost <= 0)
            return false;

        if (player.Money < cost)
            return false;

        if (!player.RemoveMoney(cost))
            return false;

        houses++;

        RefreshBuildingVisual();

        Debug.Log(
            $"{player.PlayerName} built " +
            $"house #{houses} on " +
            $"{SpaceName} for " +
            $"${cost:N0}M."
        );

        return true;
    }

    public bool AddHouse(
        BoardPlayer player)
    {
        if (player == null ||
            owner != player)
        {
            return false;
        }

        return AddHouse();
    }


// ============================================================
// FREE HOUSE — CHANCE / SPECIAL CARD
// ============================================================

// ============================================================
// FREE HOUSE — CHANCE / SPECIAL CARD
// ============================================================

    public bool AddFreeHouse(BoardPlayer player)
    {
        if (player == null)
            return false;

        // The property must belong to the player.
        if (owner != player)
            return false;

        // Free house only applies to normal properties.
        if (!IsProperty)
            return false;

        // Cannot develop a mortgaged property.
        if (mortgaged)
            return false;

        // Cannot place a house on a hotel property.
        if (hotel)
            return false;

        // Maximum of four houses.
        if (houses >= 4)
            return false;

        // IMPORTANT:
        // This is a Chance Card reward.
        // It intentionally bypasses:
        //
        // 1. "Player must be standing on this property."
        // 2. "Property must have enough development landings."
        //
        // The player may therefore receive a free house on
        // ANY eligible property they own.

        houses++;

        RefreshBuildingVisual();

        Debug.Log(
            $"{player.PlayerName} received a FREE HOUSE on " +
            $"{SpaceName}. Houses: {houses}/4"
        );

        GameNotificationUI.Show(
            $"{player.PlayerName} RECEIVED A FREE HOUSE ON " +
            $"{SpaceName.ToUpperInvariant()}"
        );

        return true;
    }

    public bool CanBuildHotel()
    {
        if (owner == null)
            return false;

        return CanBuildHotelFor(owner);
    }

    public bool AddHotel()
    {
        if (owner == null)
            return false;

        BoardPlayer player = owner;

        if (!CanBuildHotelFor(player))
            return false;

        int cost =
            EffectiveHotelCost;

        if (cost <= 0)
            return false;

        if (player.Money < cost)
            return false;

        if (!player.RemoveMoney(cost))
            return false;

        hotel = true;
        houses = 0;

        RefreshBuildingVisual();

        Debug.Log(
            $"{player.PlayerName} built a HOTEL on " +
            $"{SpaceName} for " +
            $"${cost:N0}M."
        );

        return true;
    }

    public bool AddHotel(
        BoardPlayer player)
    {
        if (player == null ||
            owner != player)
        {
            return false;
        }

        return AddHotel();
    }

    // ============================================================
    // MORTGAGE
    // ============================================================

    public bool CanMortgage()
    {
        if (!IsOwned ||
            mortgaged ||
            MortgageValue <= 0)
        {
            return false;
        }

        if (houses == 0 &&
            !hotel)
        {
            return true;
        }

        // During Financial Recovery, the mortgage operation will first
        // liquidate houses/hotel so the player can use the property to
        // satisfy an urgent debt.
        return IsOwnerInFinancialRecovery();
    }

    public int GetRecoveryLiquidationValue()
    {
        if (!IsOwned ||
            mortgaged ||
            MortgageValue <= 0)
        {
            return 0;
        }

        long total = MortgageValue;

        int houseRefund =
            Mathf.CeilToInt(
                EffectiveHouseCost * 0.5f
            );

        int hotelRefund =
            Mathf.CeilToInt(
                EffectiveHotelCost * 0.5f
            );

        if (hotel)
        {
            total += hotelRefund;
            total += (long)houseRefund * 4;
        }
        else
        {
            total += (long)houseRefund * houses;
        }

        return total > int.MaxValue
            ? int.MaxValue
            : Mathf.Max(
                0,
                (int)total
            );
    }

    public bool Mortgage()
    {
        if (!CanMortgage())
            return false;

        if (owner == null)
            return false;

        bool recovery =
            IsOwnerInFinancialRecovery();

        // Emergency recovery: sell buildings first, then mortgage the
        // property. This keeps the existing PropertyManagementPanel as
        // the interaction surface without introducing a new recovery UI.
        if (recovery)
        {
            if (hotel)
                SellHotel();

            while (houses > 0)
            {
                if (!SellHouse())
                    break;
            }
        }

        if (houses > 0 ||
            hotel)
        {
            return false;
        }

        mortgaged = true;

        owner.AddMoney(
            MortgageValue
        );

        RefreshBuildingVisual();

        Debug.Log(
            $"{owner.PlayerName} mortgaged " +
            $"{SpaceName} for " +
            $"${MortgageValue:N0}M" +
            (recovery
                ? " during Financial Recovery."
                : ".")
        );

        return true;
    }

    public bool CanUnmortgage()
    {
        return IsOwned &&
               mortgaged;
    }

    public bool Unmortgage()
    {
        if (!CanUnmortgage())
            return false;

        if (owner == null)
            return false;

        int repayment =
            Mathf.CeilToInt(
                MortgageValue * 1.10f
            );

        if (owner.Money < repayment)
            return false;

        if (!owner.RemoveMoney(
                repayment))
        {
            return false;
        }

        mortgaged = false;

        Debug.Log(
            $"{owner.PlayerName} unmortgaged " +
            $"{SpaceName} for " +
            $"${repayment:N0}M."
        );

        return true;
    }

    // ============================================================
    // SELL BUILDINGS
    // ============================================================

    public bool SellHouse()
    {
        if (!IsOwned ||
            houses <= 0 ||
            EffectiveHouseCost <= 0)
        {
            return false;
        }

        int refund =
            Mathf.CeilToInt(
                EffectiveHouseCost * 0.5f
            );

        houses--;

        owner.AddMoney(refund);

        RefreshBuildingVisual();

        return true;
    }

    public bool SellHotel()
    {
        if (!IsOwned ||
            !hotel ||
            EffectiveHotelCost <= 0)
        {
            return false;
        }

        int refund =
            Mathf.CeilToInt(
                EffectiveHotelCost * 0.5f
            );

        hotel = false;
        houses = 4;

        owner.AddMoney(refund);

        RefreshBuildingVisual();

        return true;
    }

    // ============================================================
    // RAILROADS / UTILITIES
    // ============================================================

    private int CountOwnedRailroads()
    {
        if (owner == null)
            return 0;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            if (!space.IsRailroad)
                continue;

            if (space.Owner == owner)
                count++;
        }

        return count;
    }

    private int CountOwnedUtilities()
    {
        if (owner == null)
            return 0;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        int count = 0;

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            if (!space.IsUtility)
                continue;

            if (space.Owner == owner)
                count++;
        }

        return count;
    }

    // ============================================================
    // AUTOMATIC BUILDING VISUAL
    // ============================================================

    private void SetupBuildingVisual()
    {
        if (!showBuildingVisual ||
            !IsProperty)
        {
            return;
        }

        RectTransform spaceRect =
            GetComponent<RectTransform>();

        if (spaceRect == null)
        {
            Debug.LogWarning(
                $"BoardSpace: {SpaceName} does not have " +
                $"a RectTransform. Building visual skipped."
            );

            return;
        }

        // --------------------------------------------------------
        // FIND EXISTING BUILDING VISUAL
        // --------------------------------------------------------

        Transform existing =
            transform.Find("BuildingVisuals");

        if (existing != null)
        {
            buildingVisualContainer =
                existing.GetComponent<RectTransform>();

            buildingVisual =
                existing.GetComponent<PropertyBuildingVisual>();
        }

        // --------------------------------------------------------
        // CREATE BUILDING VISUAL CONTAINER
        // --------------------------------------------------------

        if (buildingVisualContainer == null)
        {
            GameObject root =
                new GameObject(
                    "BuildingVisuals",
                    typeof(RectTransform)
                );

            root.transform.SetParent(
                transform,
                false
            );

            buildingVisualContainer =
                root.GetComponent<RectTransform>();

            // Top-left of the property slot.
            buildingVisualContainer.anchorMin =
                new Vector2(0f, 1f);

            buildingVisualContainer.anchorMax =
                new Vector2(0f, 1f);

            buildingVisualContainer.pivot =
                new Vector2(0f, 1f);

            buildingVisualContainer.anchoredPosition =
                new Vector2(
                    3f,
                    -3f
                );

            buildingVisualContainer.sizeDelta =
                new Vector2(
                    Mathf.Max(
                        60f,
                        spaceRect.rect.width - 6f
                    ),
                    20f
                );
        }

        // --------------------------------------------------------
        // ADD VISUAL SCRIPT
        // --------------------------------------------------------

        if (buildingVisual == null)
        {
            buildingVisual =
                buildingVisualContainer
                    .gameObject
                    .GetComponent<PropertyBuildingVisual>();

            if (buildingVisual == null)
            {
                buildingVisual =
                    buildingVisualContainer
                        .gameObject
                        .AddComponent<PropertyBuildingVisual>();
            }
        }

        // Tell the visual exactly which container to use.
        buildingVisual.SetContainer(
            buildingVisualContainer
        );

        buildingVisual.Refresh();
    }

    public void RefreshBuildingVisual()
    {
        if (!showBuildingVisual ||
            !IsProperty)
        {
            return;
        }

        if (buildingVisual == null)
            SetupBuildingVisual();

        if (buildingVisual != null)
            buildingVisual.Refresh();
    }

    private bool IsOwnerInFinancialRecovery()
    {
        if (owner == null)
            return false;

        FinancialRecoveryManager recovery =
            FinancialRecoveryManager.Instance;

        if (recovery == null)
        {
            recovery =
                FindFirstObjectByType<FinancialRecoveryManager>(
                    FindObjectsInactive.Include
                );
        }

        return recovery != null &&
               recovery.IsRecovering(owner);
    }

    private void OnValidate()
    {
        if (purchasePrice <= 0)
            return;

        mortgageValue =
            PropertyEconomy
                .FromPurchasePrice(purchasePrice)
                .mortgageValue;
    }



    // ============================================================
    // RESET
    // ============================================================

    public void ResetProperty()
    {
        owner = null;
        isOwned = false;

        ownerLandingCount = 0;

        houses = 0;
        hotel = false;
        mortgaged = false;

        RefreshOwnershipMarker();
        RefreshBuildingVisual();
    }
}