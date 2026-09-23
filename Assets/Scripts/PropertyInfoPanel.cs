using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime controller for the compact Property Info card.
///
/// This script only controls data/state.
/// PropertyInfoPanelSetup builds the visual hierarchy in the Editor.
///
/// Dynamic data:
/// - Property name
/// - Purchase price
/// - Owner + owner color
/// - Rent table
/// - House/hotel costs
/// - Mortgage value
/// - Manage Property availability
/// </summary>
public class PropertyInfoPanel : MonoBehaviour
{
    [Header("ROOT")]
    [SerializeField] private GameObject cardRoot;

    [Header("HEADER")]
    [SerializeField] private TMP_Text propertyNameText;
    [SerializeField] private TMP_Text headerPriceText;
    [SerializeField] private TMP_Text propertyTypeBarText;

    [Header("OWNER")]
    [SerializeField] private GameObject ownerIndicator;
    [SerializeField] private Image ownerIndicatorImage;
    [SerializeField] private TMP_Text ownerText;

    [Header("INFO VALUES")]
    [SerializeField] private TMP_Text purchasePriceInfoText;
    [SerializeField] private TMP_Text baseRentText;
    [SerializeField] private TMP_Text oneHouseRentText;
    [SerializeField] private TMP_Text twoHouseRentText;
    [SerializeField] private TMP_Text threeHouseRentText;
    [SerializeField] private TMP_Text fourHouseRentText;
    [SerializeField] private TMP_Text hotelRentText;
    [SerializeField] private TMP_Text houseCostText;
    [SerializeField] private TMP_Text hotelCostText;
    [SerializeField] private TMP_Text mortgageValueText;

    [Header("ACTIONS")]
    [SerializeField] private Button managePropertyButton;
    [SerializeField] private Button closeXButton;

    [Header("COMPATIBILITY")]
    [SerializeField] private TMP_Text propertyTypeText;
    [SerializeField] private Image propertyImage;
    [SerializeField] private Button closeButton;
    [SerializeField] private GameObject headerObject;
    [SerializeField] private GameObject typeBarObject;
    [SerializeField] private GameObject informationAreaObject;
    [SerializeField] private GameObject closeButtonAreaObject;

    [Header("PANELS")]
    [SerializeField] private PropertyManagementPanel propertyManagementPanel;
    [SerializeField] private LandingActionPanel landingActionPanel;

    private BoardSpace selectedBoardSpace;

    public BoardSpace CurrentSpace =>
        selectedBoardSpace;

    private void Awake()
    {
        AutoFindReferences();
        FindManagementPanel();
        FindLandingActionPanel();
        WireButtons();
        HidePanel();
    }

    private void AutoFindReferences()
    {
        cardRoot =
            FindObject(
                "PopupBackground",
                cardRoot
            );

        propertyNameText =
            FindTMP(
                "PropertyNameText",
                propertyNameText
            );

        headerPriceText =
            FindTMP(
                "HeaderPriceText",
                headerPriceText
            );

        propertyTypeBarText =
            FindTMP(
                "PropertyTypeBarText",
                propertyTypeBarText
            );

        propertyTypeText =
            FindTMP(
                "PropertyTypeText",
                propertyTypeText
            );

        ownerIndicator =
            FindObject(
                "OwnerIndicator",
                ownerIndicator
            );

        ownerText =
            FindTMP(
                "OwnerText",
                ownerText
            );

        if (ownerIndicatorImage == null &&
            ownerIndicator != null)
        {
            ownerIndicatorImage =
                ownerIndicator.GetComponent<Image>();

            if (ownerIndicatorImage == null)
            {
                ownerIndicatorImage =
                    ownerIndicator.GetComponentInChildren<Image>(
                        true
                    );
            }
        }

        purchasePriceInfoText =
            FindTMP(
                "PurchasePriceInfoText",
                purchasePriceInfoText
            );

        baseRentText =
            FindTMP(
                "BaseRentText",
                baseRentText
            );

        oneHouseRentText =
            FindTMP(
                "OneHouseRentText",
                oneHouseRentText
            );

        twoHouseRentText =
            FindTMP(
                "TwoHouseRentText",
                twoHouseRentText
            );

        threeHouseRentText =
            FindTMP(
                "ThreeHouseRentText",
                threeHouseRentText
            );

        fourHouseRentText =
            FindTMP(
                "FourHouseRentText",
                fourHouseRentText
            );

        hotelRentText =
            FindTMP(
                "HotelRentText",
                hotelRentText
            );

        houseCostText =
            FindTMP(
                "HouseCostText",
                houseCostText
            );

        hotelCostText =
            FindTMP(
                "HotelCostText",
                hotelCostText
            );

        mortgageValueText =
            FindTMP(
                "MortgageValueText",
                mortgageValueText
            );

        managePropertyButton =
            FindButton(
                "ManagePropertyButton",
                managePropertyButton
            );

        closeXButton =
            FindButton(
                "CloseXButton",
                closeXButton
            );

        closeButton =
            FindButton(
                "CloseButton",
                closeButton
            );

        headerObject =
            FindObject(
                "Header",
                headerObject
            );

        typeBarObject =
            FindObject(
                "PropertyTypeBar",
                typeBarObject
            );

        informationAreaObject =
            FindObject(
                "InformationArea",
                informationAreaObject
            );

        closeButtonAreaObject =
            FindObject(
                "ActionArea",
                closeButtonAreaObject
            );
    }

    private void WireButtons()
    {
        if (closeXButton != null)
        {
            closeXButton.onClick.RemoveAllListeners();
            closeXButton.onClick.AddListener(
                HidePanel
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(
                HidePanel
            );
        }

        if (managePropertyButton != null)
        {
            managePropertyButton.onClick.RemoveAllListeners();
            managePropertyButton.onClick.AddListener(
                OpenManagementPanel
            );
        }
    }

    public void ShowSpace(
        BoardSpace space)
    {
        if (space == null)
            return;

        selectedBoardSpace =
            space;

        if (landingActionPanel != null)
        {
            landingActionPanel.HidePanel();
        }

        ShowPanel();
        RefreshContent();
    }

    public void RefreshIfVisible()
    {
        if (selectedBoardSpace == null)
            return;

        bool visible =
            cardRoot != null
                ? cardRoot.activeSelf
                : gameObject.activeSelf;

        if (!visible)
            return;

        RefreshContent();
    }

    private void RefreshContent()
    {
        if (selectedBoardSpace == null)
            return;

        BoardSpace space =
            selectedBoardSpace;

        if (propertyNameText != null)
        {
            propertyNameText.text =
                space.SpaceName;
        }

        bool purchasable =
            space.SpaceType == BoardSpaceType.Property ||
            space.SpaceType == BoardSpaceType.Airport ||
            space.SpaceType == BoardSpaceType.Utility;

        if (headerPriceText != null)
        {
            headerPriceText.text =
                purchasable
                    ? Money(
                        space.PurchasePrice
                    )
                    : string.Empty;
        }

        string type =
            GetTypeDisplay(
                space.SpaceType
            );

        if (propertyTypeBarText != null)
        {
            propertyTypeBarText.text =
                type;
        }

        if (propertyTypeText != null)
        {
            propertyTypeText.text =
                type;
        }

        UpdateOwner(space);

        HideAllInfoRows();

        if (space.SpaceType ==
            BoardSpaceType.Property)
        {
            ShowPropertyInformation(
                space
            );
        }
        else
        {
            ShowSpecialInformation(
                space
            );
        }

        UpdateManagementButton(
            space
        );
    }

    private void UpdateOwner(
        BoardSpace space)
    {
        bool owned =
            space != null &&
            space.IsOwned &&
            space.Owner != null;

        if (ownerIndicator != null)
        {
            ownerIndicator.SetActive(
                true
            );
        }

        if (ownerText != null)
        {
            ownerText.text =
                owned
                    ? space.Owner.PlayerName
                    : "UNOWNED";
        }

        if (ownerIndicatorImage != null)
        {
            ownerIndicatorImage.color =
                owned
                    ? space.Owner.TokenColor
                    : new Color(
                        0.35f,
                        0.40f,
                        0.48f,
                        1f
                    );
        }
    }

    private void ShowPropertyInformation(
        BoardSpace space)
    {
        PropertyEconomy economy =
            PropertyEconomy.FromPurchasePrice(
                space.PurchasePrice
            );

        ShowValue(
            purchasePriceInfoText,
            Money(
                space.PurchasePrice
            )
        );

        ShowValue(
            baseRentText,
            Money(
                economy.baseRent
            )
        );

        ShowValue(
            oneHouseRentText,
            Money(
                economy.houseRent
            )
        );

        ShowValue(
            twoHouseRentText,
            Money(
                economy.twoHouseRent
            )
        );

        ShowValue(
            threeHouseRentText,
            Money(
                economy.threeHouseRent
            )
        );

        ShowValue(
            fourHouseRentText,
            Money(
                economy.fourHouseRent
            )
        );

        ShowValue(
            hotelRentText,
            Money(
                economy.hotelRent
            )
        );

        ShowValue(
            houseCostText,
            Money(
                economy.houseCost
            )
        );

        ShowValue(
            hotelCostText,
            Money(
                economy.hotelCost
            )
        );

        // IMPORTANT:
        // Use BoardSpace.MortgageValue so the display always matches
        // the exact value used by the mortgage/recovery gameplay.
        ShowValue(
            mortgageValueText,
            Money(
                space.MortgageValue
            )
        );
    }

    private void ShowSpecialInformation(
        BoardSpace space)
    {
        switch (space.SpaceType)
        {
            case BoardSpaceType.Airport:

                ShowValue(
                    purchasePriceInfoText,
                    Money(
                        space.PurchasePrice
                    )
                );

                ShowValue(
                    baseRentText,
                    Money(
                        space.GetRent()
                    )
                );

                HideValue(
                    oneHouseRentText
                );

                HideValue(
                    twoHouseRentText
                );

                HideValue(
                    threeHouseRentText
                );

                HideValue(
                    fourHouseRentText
                );

                HideValue(
                    hotelRentText
                );

                HideValue(
                    houseCostText
                );

                HideValue(
                    hotelCostText
                );

                HideValue(
                    mortgageValueText
                );

                break;

            case BoardSpaceType.Utility:

                ShowValue(
                    purchasePriceInfoText,
                    Money(
                        space.PurchasePrice
                    )
                );

                ShowValue(
                    baseRentText,
                    "DICE × 4 / × 10"
                );

                HideValue(
                    oneHouseRentText
                );

                HideValue(
                    twoHouseRentText
                );

                HideValue(
                    threeHouseRentText
                );

                HideValue(
                    fourHouseRentText
                );

                HideValue(
                    hotelRentText
                );

                HideValue(
                    houseCostText
                );

                HideValue(
                    hotelCostText
                );

                HideValue(
                    mortgageValueText
                );

                break;

            case BoardSpaceType.Tax:

                HideValue(
                    purchasePriceInfoText
                );

                ShowValue(
                    baseRentText,
                    space.SpaceName
                        .ToUpperInvariant()
                        .Contains("INCOME")
                        ? "$200M"
                        : "$100M"
                );

                HideAllNonTaxValues();

                break;

            case BoardSpaceType.Chance:

                HideValue(
                    purchasePriceInfoText
                );

                ShowValue(
                    baseRentText,
                    "DRAW A CHANCE CARD"
                );

                HideAllNonTaxValues();

                break;

            case BoardSpaceType.CommunityChest:

                HideValue(
                    purchasePriceInfoText
                );

                ShowValue(
                    baseRentText,
                    "DRAW A COMMUNITY CHEST CARD"
                );

                HideAllNonTaxValues();

                break;

            case BoardSpaceType.Jail:
            case BoardSpaceType.GoToJail:
            case BoardSpaceType.FreeParking:

                HideValue(
                    purchasePriceInfoText
                );

                ShowValue(
                    baseRentText,
                    GetTypeDisplay(
                        space.SpaceType
                    )
                );

                HideAllNonTaxValues();

                break;

            default:
                break;
        }
    }

    private void HideAllNonTaxValues()
    {
        HideValue(oneHouseRentText);
        HideValue(twoHouseRentText);
        HideValue(threeHouseRentText);
        HideValue(fourHouseRentText);
        HideValue(hotelRentText);
        HideValue(houseCostText);
        HideValue(hotelCostText);
        HideValue(mortgageValueText);
    }

    private void UpdateManagementButton(
        BoardSpace space)
    {
        if (managePropertyButton == null)
            return;

        GameManager gm =
            FindFirstObjectByType<GameManager>();

        BoardPlayer currentPlayer =
            gm != null
                ? gm.CurrentPlayer
                : null;

        bool show =
            space != null &&
            space.SpaceType ==
                BoardSpaceType.Property &&
            space.Owner != null &&
            space.Owner == currentPlayer;

        managePropertyButton.gameObject.SetActive(
            show
        );

        managePropertyButton.interactable =
            show;
    }

    private void ShowValue(
        TMP_Text text,
        string value)
    {
        if (text == null)
            return;

        text.text =
            value ?? string.Empty;

        SetRowActive(
            text,
            true
        );
    }

    private void HideValue(
        TMP_Text text)
    {
        if (text == null)
            return;

        SetRowActive(
            text,
            false
        );
    }

    private void HideAllInfoRows()
    {
        HideValue(
            purchasePriceInfoText
        );

        HideValue(
            baseRentText
        );

        HideValue(
            oneHouseRentText
        );

        HideValue(
            twoHouseRentText
        );

        HideValue(
            threeHouseRentText
        );

        HideValue(
            fourHouseRentText
        );

        HideValue(
            hotelRentText
        );

        HideValue(
            houseCostText
        );

        HideValue(
            hotelCostText
        );

        HideValue(
            mortgageValueText
        );
    }

    private void SetRowActive(
        TMP_Text text,
        bool active)
    {
        Transform row =
            text.transform.parent;

        if (row != null)
        {
            row.gameObject.SetActive(
                active
            );
        }
    }

    private void OpenManagementPanel()
    {
        if (selectedBoardSpace == null ||
            propertyManagementPanel == null)
        {
            return;
        }

        GameManager gm =
            FindFirstObjectByType<GameManager>();

        if (gm == null ||
            gm.CurrentPlayer !=
                selectedBoardSpace.Owner)
        {
            return;
        }

        propertyManagementPanel.Show(
            selectedBoardSpace
        );

        HidePanel();
    }

    public void HidePanel()
    {
        selectedBoardSpace = null;

        if (cardRoot != null)
        {
            cardRoot.SetActive(
                false
            );
        }

        if (managePropertyButton != null)
        {
            managePropertyButton.gameObject.SetActive(
                false
            );
        }
    }

    private void ShowPanel()
    {
        gameObject.SetActive(
            true
        );

        if (cardRoot != null)
        {
            cardRoot.SetActive(
                true
            );
        }

        if (headerObject != null)
        {
            headerObject.SetActive(
                true
            );
        }

        if (typeBarObject != null)
        {
            typeBarObject.SetActive(
                true
            );
        }

        if (informationAreaObject != null)
        {
            informationAreaObject.SetActive(
                true
            );
        }

        if (closeButtonAreaObject != null)
        {
            closeButtonAreaObject.SetActive(
                true
            );
        }
    }

    private void FindManagementPanel()
    {
        if (propertyManagementPanel == null)
        {
            propertyManagementPanel =
                FindFirstObjectByType<PropertyManagementPanel>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void FindLandingActionPanel()
    {
        if (landingActionPanel == null)
        {
            landingActionPanel =
                FindFirstObjectByType<LandingActionPanel>(
                    FindObjectsInactive.Include
                );
        }
    }

    private string GetTypeDisplay(
        BoardSpaceType type)
    {
        switch (type)
        {
            case BoardSpaceType.Property:
                return "PROPERTY";

            case BoardSpaceType.Airport:
                return "AIRPORT";

            case BoardSpaceType.Utility:
                return "UTILITY";

            case BoardSpaceType.Chance:
                return "CHANCE";

            case BoardSpaceType.CommunityChest:
                return "COMMUNITY CHEST";

            case BoardSpaceType.Tax:
                return "TAX";

            case BoardSpaceType.Jail:
                return "JAIL";

            case BoardSpaceType.GoToJail:
                return "GO TO JAIL";

            case BoardSpaceType.FreeParking:
                return "FREE PARKING";

            default:
                return "GO";
        }
    }

    private static string Money(
        int value)
    {
        return $"${value:N0}M";
    }

    private TMP_Text FindTMP(
        string name,
        TMP_Text current)
    {
        if (current != null)
            return current;

        Transform target =
            FindChildRecursive(
                transform,
                name
            );

        return target != null
            ? target.GetComponent<TMP_Text>()
            : null;
    }

    private Button FindButton(
        string name,
        Button current)
    {
        if (current != null)
            return current;

        Transform target =
            FindChildRecursive(
                transform,
                name
            );

        return target != null
            ? target.GetComponent<Button>()
            : null;
    }

    private GameObject FindObject(
        string name,
        GameObject current)
    {
        if (current != null)
            return current;

        Transform target =
            FindChildRecursive(
                transform,
                name
            );

        return target != null
            ? target.gameObject
            : null;
    }

    private Transform FindChildRecursive(
        Transform parent,
        string name)
    {
        if (parent == null)
            return null;

        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;

            Transform result =
                FindChildRecursive(
                    child,
                    name
                );

            if (result != null)
                return result;
        }

        return null;
    }
}
