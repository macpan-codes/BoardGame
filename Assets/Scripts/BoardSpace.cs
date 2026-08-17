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

    public int MortgageValue =>
        mortgageValue;

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

        if (IsRailroad)
            return GetRailroadRent();

        if (IsUtility)
            return baseRent;

        if (hotel)
        {
            return hotelRent > 0
                ? hotelRent
                : baseRent;
        }

        int rent;

        switch (houses)
        {
            case 1:
                rent =
                    rentLevel1 > 0
                        ? rentLevel1
                        : baseRent;
                break;

            case 2:
                rent =
                    rentLevel2 > 0
                        ? rentLevel2
                        : baseRent;
                break;

            case 3:
                rent =
                    rentLevel3 > 0
                        ? rentLevel3
                        : baseRent;
                break;

            case 4:
                rent =
                    rentLevel4 > 0
                        ? rentLevel4
                        : baseRent;
                break;

            default:
                rent = baseRent;
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
        return IsOwned &&
               !mortgaged &&
               mortgageValue > 0 &&
               houses == 0 &&
               !hotel;
    }

    public bool Mortgage()
    {
        if (!CanMortgage())
            return false;

        if (owner == null)
            return false;

        mortgaged = true;

        owner.AddMoney(
            mortgageValue
        );

        RefreshBuildingVisual();

        Debug.Log(
            $"{owner.PlayerName} mortgaged " +
            $"{SpaceName} for " +
            $"${mortgageValue:N0}M."
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
                mortgageValue * 1.10f
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