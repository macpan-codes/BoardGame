using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertyInfoPanel : MonoBehaviour
{
    // ============================================================
    // STATE
    // ============================================================

    private BoardSpace selectedBoardSpace;

    public BoardSpace CurrentSpace =>
        selectedBoardSpace;

    // ============================================================
    // CARD ROOT
    // ============================================================

    [Header("Card Root")]
    [SerializeField] private GameObject cardRoot;

    // ============================================================
    // MANAGEMENT PANEL
    // ============================================================

    [Header("Management Panel")]
    [SerializeField]
    private PropertyManagementPanel propertyManagementPanel;

    // ============================================================
    // OWNERSHIP
    // ============================================================

    [Header("Ownership")]
    [SerializeField] private GameObject ownerIndicator;
    [SerializeField] private TMP_Text ownerText;

    // ============================================================
    // PROPERTY
    // ============================================================

    [Header("Property")]
    [SerializeField] private TMP_Text propertyNameText;
    [SerializeField] private TMP_Text propertyTypeText;
    [SerializeField] private TMP_Text propertyTypeBarText;
    [SerializeField] private Image propertyImage;

    // ============================================================
    // INFORMATION
    // ============================================================

    [Header("Information")]
    [SerializeField] private TMP_Text purchasePriceText;
    [SerializeField] private TMP_Text baseRentText;
    [SerializeField] private TMP_Text oneHouseRentText;
    [SerializeField] private TMP_Text twoHouseRentText;
    [SerializeField] private TMP_Text threeHouseRentText;
    [SerializeField] private TMP_Text fourHouseRentText;
    [SerializeField] private TMP_Text hotelRentText;
    [SerializeField] private TMP_Text houseCostText;
    [SerializeField] private TMP_Text hotelCostText;
    [SerializeField] private TMP_Text mortgageValueText;

    // ============================================================
    // CARD SECTIONS
    // ============================================================

    [Header("Card Sections")]
    [SerializeField] private GameObject headerObject;
    [SerializeField] private GameObject typeBarObject;
    [SerializeField] private GameObject informationAreaObject;
    [SerializeField] private GameObject closeButtonAreaObject;

    // ============================================================
    // BUTTONS
    // ============================================================

    [Header("Buttons")]
    [SerializeField] private Button managePropertyButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button closeXButton;

    // ============================================================
    // LANDING ACTION
    // ============================================================

    [Header("Landing Action")]
    [SerializeField] private LandingActionPanel landingActionPanel;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        AutoFindReferences();
        FindManagementPanel();
        FindLandingActionPanel();
        WireButtons();
        HidePanel();

        Debug.Log(
            "PropertyInfoPanel: Initialized successfully."
        );
    }

    // ============================================================
    // FIND REFERENCES
    // ============================================================

    private void AutoFindReferences()
    {
        cardRoot =
            FindObject(
                "PopupBackground",
                cardRoot
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

        propertyNameText =
            FindTMP(
                "PropertyNameText",
                propertyNameText
            );

        propertyTypeText =
            FindTMP(
                "PropertyTypeText",
                propertyTypeText
            );

        propertyTypeBarText =
            FindTMP(
                "PropertyTypeBar",
                propertyTypeBarText
            );

        purchasePriceText =
            FindTMP(
                "PurchasePriceText",
                purchasePriceText
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
                "CloseButtonArea",
                closeButtonAreaObject
            );

        managePropertyButton =
            FindButton(
                "ManagePropertyButton",
                managePropertyButton
            );

        closeButton =
            FindButton(
                "CloseButton",
                closeButton
            );

        closeXButton =
            FindButton(
                "CloseXButton",
                closeXButton
            );

        if (propertyImage == null)
        {
            Transform image =
                FindChildRecursive(
                    transform,
                    "PropertyImage"
                );

            if (image != null)
            {
                propertyImage =
                    image.GetComponent<Image>();
            }
        }
    }

    private void FindManagementPanel()
    {
        if (propertyManagementPanel != null)
            return;

        propertyManagementPanel =
            FindFirstObjectByType<PropertyManagementPanel>(
                FindObjectsInactive.Include
            );

        if (propertyManagementPanel != null)
        {
            Debug.Log(
                "PropertyInfoPanel: PropertyManagementPanel found."
            );
        }
        else
        {
            Debug.LogWarning(
                "PropertyInfoPanel: PropertyManagementPanel could not be found."
            );
        }
    }

    private void FindLandingActionPanel()
    {
        if (landingActionPanel != null)
            return;

        landingActionPanel =
            FindFirstObjectByType<LandingActionPanel>(
                FindObjectsInactive.Include
            );
    }

    // ============================================================
    // BUTTON WIRING
    // ============================================================

    private void WireButtons()
    {
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(
                HidePanel
            );
        }

        if (closeXButton != null)
        {
            closeXButton.onClick.RemoveAllListeners();
            closeXButton.onClick.AddListener(
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

    // ============================================================
    // PUBLIC SHOW
    // ============================================================

    public void ShowSpace(BoardSpace space)
    {
        if (space == null)
            return;

        selectedBoardSpace = space;

        if (landingActionPanel != null)
        {
            landingActionPanel.HidePanel();
        }

        ShowPanel();

        if (propertyNameText != null)
        {
            propertyNameText.text =
                space.SpaceName.ToUpperInvariant();
        }

        string type =
            GetTypeDisplay(
                space.SpaceType
            );

        if (propertyTypeText != null)
        {
            propertyTypeText.text = type;
        }

        if (propertyTypeBarText != null)
        {
            propertyTypeBarText.text = type;
        }

        ClearRows();

        if (space.SpaceType ==
            BoardSpaceType.Property)
        {
            ShowPropertyInformation(space);
        }
        else
        {
            ShowSpecialInformation(space);
        }

        UpdateOwner(space);
        UpdateManagementButton(space);
    }

    // ============================================================
    // MANAGEMENT BUTTON
    // ============================================================

    private void UpdateManagementButton(
        BoardSpace space)
    {
        if (managePropertyButton == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        BoardPlayer currentPlayer =
            gameManager != null
                ? gameManager.CurrentPlayer
                : null;

        bool showManagement =
            space != null &&
            space.IsOwned &&
            space.Owner != null &&
            currentPlayer != null &&
            space.Owner == currentPlayer &&
            space.SpaceType == BoardSpaceType.Property;

        managePropertyButton.gameObject.SetActive(
            showManagement
        );

        managePropertyButton.interactable =
            showManagement;
    }

    private void OpenManagementPanel()
    {
        if (selectedBoardSpace == null)
            return;

        if (!selectedBoardSpace.IsOwned)
        {
            return;
        }

        if (selectedBoardSpace.Owner == null)
        {
            return;
        }

        FindManagementPanel();

        if (propertyManagementPanel == null)
        {
            Debug.LogWarning(
                "PropertyInfoPanel: PropertyManagementPanel is missing."
            );

            return;
        }

        Debug.Log(
            $"PropertyInfoPanel: Opening management panel for " +
            $"{selectedBoardSpace.SpaceName}."
        );

        propertyManagementPanel.Show(
            selectedBoardSpace
        );

        HidePanel();
    }

    // ============================================================
    // HIDE
    // ============================================================

    public void HidePanel()
    {
        if (managePropertyButton != null)
        {
            managePropertyButton.gameObject.SetActive(
                false
            );
        }

        if (cardRoot != null)
        {
            cardRoot.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
        }

        if (ownerIndicator != null)
        {
            ownerIndicator.SetActive(false);
        }

        HideRows();

        selectedBoardSpace = null;
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

        ShowSpace(selectedBoardSpace);
    }

    // ============================================================
    // PROPERTY INFORMATION
    // ============================================================

    private void ShowPropertyInformation(
        BoardSpace space)
    {
        ShowRow(purchasePriceText);
        ShowRow(baseRentText);
        ShowRow(oneHouseRentText);
        ShowRow(twoHouseRentText);
        ShowRow(threeHouseRentText);
        ShowRow(fourHouseRentText);
        ShowRow(hotelRentText);
        ShowRow(houseCostText);
        ShowRow(hotelCostText);
        ShowRow(mortgageValueText);

        SetLabel(
            purchasePriceText,
            "PURCHASE PRICE"
        );

        SetLabel(
            baseRentText,
            "BASE RENT"
        );

        SetLabel(
            oneHouseRentText,
            "1 HOUSE"
        );

        SetLabel(
            twoHouseRentText,
            "2 HOUSES"
        );

        SetLabel(
            threeHouseRentText,
            "3 HOUSES"
        );

        SetLabel(
            fourHouseRentText,
            "4 HOUSES"
        );

        SetLabel(
            hotelRentText,
            "HOTEL"
        );

        SetLabel(
            houseCostText,
            "HOUSE COST"
        );

        SetLabel(
            hotelCostText,
            "HOTEL COST"
        );

        SetLabel(
            mortgageValueText,
            "MORTGAGE VALUE"
        );

        PropertyEconomy economy =
            PropertyEconomy.FromPurchasePrice(
                space.PurchasePrice
            );

        SetValue(
            purchasePriceText,
            Money(space.PurchasePrice)
        );

        SetValue(
            baseRentText,
            Money(economy.baseRent)
        );

        SetValue(
            oneHouseRentText,
            Money(economy.houseRent)
        );

        SetValue(
            twoHouseRentText,
            Money(economy.twoHouseRent)
        );

        SetValue(
            threeHouseRentText,
            Money(economy.threeHouseRent)
        );

        SetValue(
            fourHouseRentText,
            Money(economy.fourHouseRent)
        );

        SetValue(
            hotelRentText,
            Money(economy.hotelRent)
        );

        SetValue(
            houseCostText,
            Money(economy.houseCost)
        );

        SetValue(
            hotelCostText,
            Money(economy.hotelCost)
        );

        SetValue(
            mortgageValueText,
            Money(economy.mortgageValue)
        );
    }

    // ============================================================
    // SPECIAL INFORMATION
    // ============================================================

    private void ShowSpecialInformation(
        BoardSpace space)
    {
        switch (space.SpaceType)
        {
            case BoardSpaceType.Airport:

                ShowRow(purchasePriceText);
                ShowRow(baseRentText);

                SetLabel(
                    purchasePriceText,
                    "PURCHASE PRICE"
                );

                SetLabel(
                    baseRentText,
                    "BASE RENT"
                );

                SetValue(
                    purchasePriceText,
                    Money(space.PurchasePrice)
                );

                SetValue(
                    baseRentText,
                    Money(space.GetRent())
                );

                break;

            case BoardSpaceType.Utility:

                ShowRow(purchasePriceText);
                ShowRow(baseRentText);

                SetLabel(
                    purchasePriceText,
                    "PURCHASE PRICE"
                );

                SetLabel(
                    baseRentText,
                    "RENT"
                );

                SetValue(
                    purchasePriceText,
                    Money(space.PurchasePrice)
                );

                SetValue(
                    baseRentText,
                    "DICE × 4 / × 10"
                );

                break;

            case BoardSpaceType.Tax:

                ShowRow(baseRentText);

                SetLabel(
                    baseRentText,
                    "PAY BANK"
                );

                SetValue(
                    baseRentText,
                    space.SpaceName
                        .ToUpperInvariant()
                        .Contains("INCOME")
                        ? "$200M"
                        : "$100M"
                );

                break;

            case BoardSpaceType.Chance:

                ShowRow(baseRentText);

                SetLabel(
                    baseRentText,
                    "EFFECT"
                );

                SetValue(
                    baseRentText,
                    "DRAW A CHANCE CARD"
                );

                break;

            case BoardSpaceType.CommunityChest:

                ShowRow(baseRentText);

                SetLabel(
                    baseRentText,
                    "EFFECT"
                );

                SetValue(
                    baseRentText,
                    "DRAW A COMMUNITY CHEST CARD"
                );

                break;

            case BoardSpaceType.Jail:

                ShowRow(baseRentText);

                SetLabel(
                    baseRentText,
                    "STATUS"
                );

                SetValue(
                    baseRentText,
                    "JUST VISITING / JAIL"
                );

                break;

            case BoardSpaceType.GoToJail:

                ShowRow(baseRentText);

                SetLabel(
                    baseRentText,
                    "ACTION"
                );

                SetValue(
                    baseRentText,
                    "GO TO JAIL"
                );

                break;

            case BoardSpaceType.FreeParking:

                ShowRow(baseRentText);

                SetLabel(
                    baseRentText,
                    "EFFECT"
                );

                SetValue(
                    baseRentText,
                    "NO EFFECT"
                );

                break;
        }
    }

    // ============================================================
    // OWNER
    // ============================================================

    private void UpdateOwner(
        BoardSpace space)
    {
        if (ownerIndicator == null)
            return;

        if (!space.IsOwned ||
            space.Owner == null)
        {
            ownerIndicator.SetActive(false);

            if (ownerText != null)
            {
                ownerText.text = "";
            }

            return;
        }

        ownerIndicator.SetActive(true);

        if (ownerText != null)
        {
            ownerText.text =
                $"OWNER: {space.Owner.PlayerName}";
        }
    }

    // ============================================================
    // VISUALS
    // ============================================================

    private void ShowPanel()
    {
        gameObject.SetActive(true);

        if (cardRoot != null)
            cardRoot.SetActive(true);

        if (headerObject != null)
            headerObject.SetActive(true);

        if (typeBarObject != null)
            typeBarObject.SetActive(true);

        if (informationAreaObject != null)
            informationAreaObject.SetActive(true);

        if (closeButtonAreaObject != null)
            closeButtonAreaObject.SetActive(true);
    }

    // ============================================================
    // ROWS
    // ============================================================

    private void ClearRows()
    {
        HideRows();
    }

    private void HideRows()
    {
        HideRow(purchasePriceText);
        HideRow(baseRentText);
        HideRow(oneHouseRentText);
        HideRow(twoHouseRentText);
        HideRow(threeHouseRentText);
        HideRow(fourHouseRentText);
        HideRow(hotelRentText);
        HideRow(houseCostText);
        HideRow(hotelCostText);
        HideRow(mortgageValueText);
    }

    private void ShowRow(
        TMP_Text value)
    {
        if (value == null)
            return;

        if (value.transform.parent != null)
        {
            value.transform.parent.gameObject.SetActive(true);
        }
    }

    private void HideRow(
        TMP_Text value)
    {
        if (value == null)
            return;

        if (value.transform.parent != null)
        {
            value.transform.parent.gameObject.SetActive(false);
        }
    }

    private void SetValue(
        TMP_Text text,
        string value)
    {
        if (text != null)
        {
            text.text = value;
        }
    }

    private void SetLabel(
        TMP_Text valueText,
        string label)
    {
        if (valueText == null)
            return;

        Transform row =
            valueText.transform.parent;

        if (row == null)
            return;

        TMP_Text[] texts =
            row.GetComponentsInChildren<TMP_Text>(
                true
            );

        foreach (TMP_Text text in texts)
        {
            if (text == null ||
                text == valueText)
            {
                continue;
            }

            text.text = label;
            break;
        }
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private static string Money(
        int value)
    {
        return $"${value:N0}M";
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

    // ============================================================
    // AUTO-FIND HELPERS
    // ============================================================

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