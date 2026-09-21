using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime controller for the compact Landing Action card.
///
/// The visual hierarchy is built by LandingActionPanelSetup.
/// This script supplies live game data and preserves the existing
/// Buy / Decline / turn-continuation logic.
///
/// Dynamic data:
/// - Landed space name
/// - Purchase price
/// - Space type
/// - Current owner
/// - Base rent
/// - Development rent information for properties
/// - Buy / Frozen / Decline / Continue states
/// </summary>
public class LandingActionPanel : MonoBehaviour
{
    [Header("ROOT")]
    [SerializeField] private GameObject cardRoot;

    [Header("HEADER")]
    [SerializeField] private TMP_Text propertyNameText;
    [SerializeField] private TMP_Text headerPriceText;
    [SerializeField] private TMP_Text propertyTypeBarText;

    // Compatibility with the older hierarchy.
    [SerializeField] private TMP_Text propertyTypeText;
    [SerializeField] private TMP_Text purchasePriceText;

    [Header("OWNER")]
    [SerializeField] private GameObject ownerIndicator;
    [SerializeField] private Image ownerIndicatorImage;
    [SerializeField] private TMP_Text ownerText;

    [Header("INFORMATION")]
    [SerializeField] private GameObject informationAreaObject;
    [SerializeField] private TMP_Text purchasePriceInfoText;
    [SerializeField] private TMP_Text baseRentText;
    [SerializeField] private TMP_Text oneHouseRentText;
    [SerializeField] private TMP_Text twoHouseRentText;
    [SerializeField] private TMP_Text threeHouseRentText;
    [SerializeField] private TMP_Text fourHouseRentText;
    [SerializeField] private TMP_Text hotelRentText;

    [Header("ACTIONS")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button closeXButton;

    private BoardSpace selectedSpace;

    public BoardSpace CurrentSpace =>
        selectedSpace;

    private void Awake()
    {
        AutoFindReferences();
        WireButtons();
        HidePanel();

        Debug.Log(
            "LandingActionPanel: Initialized."
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

        // Compatibility with the old generated hierarchy.
        if (propertyTypeBarText == null)
        {
            propertyTypeBarText =
                FindTMP(
                    "PropertyTypeBar",
                    propertyTypeBarText
                );
        }

        propertyTypeText =
            FindTMP(
                "PropertyTypeText",
                propertyTypeText
            );

        purchasePriceText =
            FindTMP(
                "PurchasePriceText",
                purchasePriceText
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

        informationAreaObject =
            FindObject(
                "InformationArea",
                informationAreaObject
            );

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

        buyButton =
            FindButton(
                "BuyButton",
                buyButton
            );

        declineButton =
            FindButton(
                "DeclineButton",
                declineButton
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
    }

    // ============================================================
    // BUTTON WIRING
    // ============================================================

    private void WireButtons()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(
                Buy
            );
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(
                Decline
            );
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(
                Decline
            );
        }

        if (closeXButton != null)
        {
            closeXButton.onClick.RemoveAllListeners();
            closeXButton.onClick.AddListener(
                Decline
            );
        }
    }

    // ============================================================
    // SHOW LANDING
    // ============================================================

    public void ShowLanding(
        BoardSpace space)
    {
        if (space == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        BoardPlayer player =
            gameManager.CurrentPlayer;

        if (player == null)
            return;

        selectedSpace =
            space;

        ShowPanel();

        RefreshContent(
            gameManager,
            player
        );
    }

    private void RefreshContent(
        GameManager gameManager,
        BoardPlayer player)
    {
        BoardSpace space =
            selectedSpace;

        if (space == null)
            return;

        // --------------------------------------------------------
        // NAME
        // --------------------------------------------------------

        if (propertyNameText != null)
        {
            propertyNameText.text =
                space.SpaceName;
        }

        // --------------------------------------------------------
        // TYPE
        // --------------------------------------------------------

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

        // --------------------------------------------------------
        // PURCHASE PRICE
        // --------------------------------------------------------

        bool purchasable =
            space.SpaceType ==
                BoardSpaceType.Property ||
            space.SpaceType ==
                BoardSpaceType.Airport ||
            space.SpaceType ==
                BoardSpaceType.Utility;

        string priceText =
            purchasable
                ? Money(
                    space.PurchasePrice
                )
                : string.Empty;

        if (headerPriceText != null)
        {
            headerPriceText.text =
                priceText;
        }

        if (purchasePriceText != null)
        {
            purchasePriceText.text =
                priceText;
        }

        // --------------------------------------------------------
        // OWNER
        // --------------------------------------------------------

        UpdateOwner(
            space
        );

        // --------------------------------------------------------
        // INFORMATION
        // --------------------------------------------------------

        UpdateInformation(
            space
        );

        // --------------------------------------------------------
        // BUY / DECLINE
        // --------------------------------------------------------

        bool canBuy =
            space.CanBePurchased();

        bool marketFrozen =
            gameManager.IsMarketFrozen(
                player
            );

        bool enoughMoney =
            player.Money >=
            space.PurchasePrice;

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(
                canBuy
            );

            buyButton.interactable =
                canBuy &&
                !marketFrozen &&
                enoughMoney;

            if (marketFrozen)
            {
                SetButtonText(
                    buyButton,
                    "FROZEN"
                );
            }
            else if (!enoughMoney)
            {
                SetButtonText(
                    buyButton,
                    "NOT ENOUGH"
                );
            }
            else
            {
                SetButtonText(
                    buyButton,
                    "BUY"
                );
            }
        }

        if (declineButton != null)
        {
            declineButton.gameObject.SetActive(
                true
            );

            declineButton.interactable =
                true;

            SetButtonText(
                declineButton,
                canBuy
                    ? "DECLINE"
                    : "CONTINUE"
            );
        }

        // The landing card uses the X as the visible close control.
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(
                false
            );
        }

        if (closeXButton != null)
        {
            closeXButton.gameObject.SetActive(
                true
            );
        }
    }

    // ============================================================
    // OWNER
    // ============================================================

    private void UpdateOwner(
        BoardSpace space)
    {
        bool owned =
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

    // ============================================================
    // INFORMATION
    // ============================================================

    private void UpdateInformation(
        BoardSpace space)
    {
        HideAllInfoRows();

        if (informationAreaObject != null)
        {
            informationAreaObject.SetActive(
                true
            );
        }

        switch (space.SpaceType)
        {
            case BoardSpaceType.Property:
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

                break;
            }

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

                break;

            default:

                ShowValue(
                    baseRentText,
                    GetSpecialActionText(
                        space.SpaceType
                    )
                );

                break;
        }
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
    }

    private void ShowValue(
        TMP_Text text,
        string value)
    {
        if (text == null)
            return;

        text.text =
            value ?? string.Empty;

        Transform row =
            text.transform.parent;

        if (row != null)
        {
            row.gameObject.SetActive(
                true
            );
        }
    }

    private void HideValue(
        TMP_Text text)
    {
        if (text == null)
            return;

        Transform row =
            text.transform.parent;

        if (row != null)
        {
            row.gameObject.SetActive(
                false
            );
        }
    }

    // ============================================================
    // BUY
    // ============================================================

    private void Buy()
    {
        if (selectedSpace == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        if (!gameManager.BuySpace(
                selectedSpace
            ))
        {
            Debug.LogWarning(
                "LandingActionPanel: Purchase failed."
            );

            return;
        }

        HidePanel();

        gameManager.EndPlayerAction();
        gameManager.EndTurn();
    }

    // ============================================================
    // DECLINE / CONTINUE
    // ============================================================

    private void Decline()
    {
        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        HidePanel();

        if (gameManager == null)
            return;

        gameManager.EndPlayerAction();
        gameManager.EndTurn();
    }

    // ============================================================
    // VISIBILITY
    // ============================================================

    public void HidePanel()
    {
        // Keep the component itself active so existing game logic
        // can still find this panel.
        gameObject.SetActive(
            true
        );

        if (cardRoot != null)
        {
            cardRoot.SetActive(
                false
            );
        }

        selectedSpace = null;
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
    }

    // ============================================================
    // BUTTON TEXT
    // ============================================================

    private void SetButtonText(
        Button button,
        string text)
    {
        if (button == null)
            return;

        TMP_Text label =
            button.GetComponentInChildren<TMP_Text>(
                true
            );

        if (label != null)
        {
            label.text =
                text;
        }
    }

    // ============================================================
    // SPECIAL SPACE TEXT
    // ============================================================

    private string GetSpecialActionText(
        BoardSpaceType type)
    {
        switch (type)
        {
            case BoardSpaceType.Chance:
                return "DRAW A CHANCE CARD";

            case BoardSpaceType.CommunityChest:
                return "DRAW A COMMUNITY CHEST CARD";

            case BoardSpaceType.Tax:
                return "PAY TAX";

            case BoardSpaceType.FreeParking:
                return "NO EFFECT";

            case BoardSpaceType.Jail:
                return "JUST VISITING / JAIL";

            case BoardSpaceType.GoToJail:
                return "GO TO JAIL";

            default:
                return type
                    .ToString()
                    .ToUpperInvariant();
        }
    }

    // ============================================================
    // TYPE DISPLAY
    // ============================================================

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
    // MONEY
    // ============================================================

    private static string Money(
        int value)
    {
        return $"${value:N0}M";
    }

    // ============================================================
    // FIND HELPERS
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