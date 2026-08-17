using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LandingActionPanel : MonoBehaviour
{
    [Header("Card")]
    [SerializeField] private GameObject cardRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text propertyNameText;
    [SerializeField] private TMP_Text propertyTypeText;
    [SerializeField] private TMP_Text propertyTypeBarText;
    [SerializeField] private TMP_Text purchasePriceText;

    [Header("Sections")]
    [SerializeField] private GameObject informationAreaObject;

    [Header("Buttons")]
    [SerializeField] private Button buyButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button closeXButton;

    private BoardSpace selectedSpace;

    private void Awake()
    {
        AutoFindReferences();
        WireButtons();
        HidePanel();

        Debug.Log("LandingActionPanel: Initialized.");
    }

    private void AutoFindReferences()
    {
        cardRoot =
            FindObject("PopupBackground", cardRoot);

        propertyNameText =
            FindTMP("PropertyNameText", propertyNameText);

        propertyTypeText =
            FindTMP("PropertyTypeText", propertyTypeText);

        propertyTypeBarText =
            FindTMP("PropertyTypeBar", propertyTypeBarText);

        purchasePriceText =
            FindTMP("PurchasePriceText", purchasePriceText);

        informationAreaObject =
            FindObject("InformationArea", informationAreaObject);

        buyButton =
            FindButton("BuyButton", buyButton);

        declineButton =
            FindButton("DeclineButton", declineButton);

        closeButton =
            FindButton("CloseButton", closeButton);

        closeXButton =
            FindButton("CloseXButton", closeXButton);
    }

    private void WireButtons()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveAllListeners();
            buyButton.onClick.AddListener(Buy);
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(Decline);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(Decline);
        }

        if (closeXButton != null)
        {
            closeXButton.onClick.RemoveAllListeners();
            closeXButton.onClick.AddListener(Decline);
        }
    }

    public void ShowLanding(BoardSpace space)
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

        selectedSpace = space;

        ShowPanel();

        if (propertyNameText != null)
            propertyNameText.text =
                space.SpaceName.ToUpperInvariant();

        string type =
            GetTypeDisplay(space.SpaceType);

        if (propertyTypeText != null)
            propertyTypeText.text = type;

        if (propertyTypeBarText != null)
            propertyTypeBarText.text = type;

        if (informationAreaObject != null)
            informationAreaObject.SetActive(true);

        bool canBuy =
            space.CanBePurchased();

        if (purchasePriceText != null)
        {
            purchasePriceText.text =
                $"${space.PurchasePrice:N0}M";
        }

        if (buyButton != null)
        {
            buyButton.gameObject.SetActive(canBuy);
            buyButton.interactable =
                canBuy &&
                player.Money >= space.PurchasePrice;

            SetButtonText(
                buyButton,
                "BUY"
            );
        }

        if (declineButton != null)
        {
            declineButton.gameObject.SetActive(true);
            declineButton.interactable = true;

            SetButtonText(
                declineButton,
                canBuy ? "DECLINE" : "CONTINUE"
            );
        }

        if (closeButton != null)
            closeButton.gameObject.SetActive(false);

        if (closeXButton != null)
            closeXButton.gameObject.SetActive(false);

        Debug.Log(
            $"LandingActionPanel: Showing action for " +
            $"{space.SpaceName}."
        );
    }

    private void Buy()
    {
        if (selectedSpace == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        if (!gameManager.BuySpace(selectedSpace))
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

    public void HidePanel()
    {
        // Keep root active so FindObjectsByType can find it.
        gameObject.SetActive(true);

        if (cardRoot != null)
            cardRoot.SetActive(false);

        selectedSpace = null;
    }

    private void ShowPanel()
    {
        gameObject.SetActive(true);

        if (cardRoot != null)
            cardRoot.SetActive(true);
    }

    private void SetButtonText(
        Button button,
        string text)
    {
        if (button == null)
            return;

        TMP_Text label =
            button.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
            label.text = text;
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
}