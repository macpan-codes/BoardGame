using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PropertyManagementPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Text")]
    [SerializeField] private TMP_Text propertyNameText;
    [SerializeField] private TMP_Text ownerText;
    [SerializeField] private TMP_Text groupText;
    [SerializeField] private TMP_Text housesText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button houseButton;
    [SerializeField] private Button hotelButton;
    [SerializeField] private Button mortgageButton;
    [SerializeField] private Button unmortgageButton;
    [SerializeField] private Button closeButton;

    private BoardSpace selectedSpace;
    private GameManager gameManager;

    // True when this panel was opened because the player
    // landed on their own property.
    private bool actionOpenedFromLanding;

    private void Awake()
    {
        gameManager =
            FindFirstObjectByType<GameManager>();

        WireButtons();

        Hide();
    }

    // ============================================================
    // BUTTON WIRING
    // ============================================================

    private void WireButtons()
    {
        if (houseButton != null)
        {
            houseButton.onClick.RemoveAllListeners();
            houseButton.onClick.AddListener(BuildHouse);
        }

        if (hotelButton != null)
        {
            hotelButton.onClick.RemoveAllListeners();
            hotelButton.onClick.AddListener(BuildHotel);
        }

        if (mortgageButton != null)
        {
            mortgageButton.onClick.RemoveAllListeners();
            mortgageButton.onClick.AddListener(Mortgage);
        }

        if (unmortgageButton != null)
        {
            unmortgageButton.onClick.RemoveAllListeners();
            unmortgageButton.onClick.AddListener(Unmortgage);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseAction);
        }
    }

    // ============================================================
    // NORMAL SHOW
    // ============================================================

    public void Show(BoardSpace space)
    {
        ShowInternal(space, false);
    }

    // ============================================================
    // SHOW FROM LANDING
    // ============================================================

    public void ShowForLanding(BoardSpace space)
    {
        ShowInternal(space, true);
    }

    private void ShowInternal(
        BoardSpace space,
        bool fromLanding)
    {
        if (space == null)
            return;

        gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        BoardPlayer player =
            gameManager.CurrentPlayer;

        if (player == null)
            return;

        // The property must belong to the active player.
        if (space.Owner != player)
        {
            Hide();
            return;
        }

        selectedSpace = space;
        actionOpenedFromLanding = fromLanding;

        if (panel != null)
            panel.SetActive(true);

        Refresh();
    }

    // ============================================================
    // REFRESH
    // ============================================================

    private void Refresh()
    {
        if (selectedSpace == null)
            return;

        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null)
            return;

        // --------------------------------------------------------
        // PROPERTY NAME
        // --------------------------------------------------------

        if (propertyNameText != null)
        {
            propertyNameText.text =
                selectedSpace.SpaceName;
        }

        // --------------------------------------------------------
        // OWNER
        // --------------------------------------------------------

        if (ownerText != null)
        {
            ownerText.text =
                selectedSpace.Owner != null
                    ? $"Owner: {selectedSpace.Owner.PlayerName}"
                    : "Owner: Bank";
        }

        // --------------------------------------------------------
        // GROUP
        // --------------------------------------------------------

        if (groupText != null)
        {
            if (selectedSpace.PropertyGroup != null)
            {
                groupText.text =
                    $"Group: " +
                    $"{selectedSpace.PropertyGroup.GroupName}";
            }
            else
            {
                groupText.text =
                    "Group: None";
            }
        }

        // --------------------------------------------------------
        // BUILDINGS
        // --------------------------------------------------------

        if (housesText != null)
        {
            if (selectedSpace.HasHotel)
            {
                housesText.text = "HOTEL";
            }
            else
            {
                housesText.text =
                    $"Houses: {selectedSpace.Houses}/4";
            }
        }

        // --------------------------------------------------------
        // STATUS
        // --------------------------------------------------------

        if (statusText != null)
        {
            statusText.text =
                GetStatusText();
        }

        // --------------------------------------------------------
        // BUTTONS
        // --------------------------------------------------------

        BoardPlayer currentPlayer =
            gameManager.CurrentPlayer;

        bool ownedByCurrentPlayer =
            selectedSpace.Owner == currentPlayer;

        if (!ownedByCurrentPlayer)
        {
            DisableAllButtons();
            return;
        }

        if (houseButton != null)
        {
            houseButton.interactable =
                gameManager.CanBuildHouse(
                    selectedSpace
                );
        }

        if (hotelButton != null)
        {
            hotelButton.interactable =
                gameManager.CanBuildHotel(
                    selectedSpace
                );
        }

        if (mortgageButton != null)
        {
            mortgageButton.interactable =
                selectedSpace.CanMortgage();
        }

        if (unmortgageButton != null)
        {
            unmortgageButton.interactable =
                selectedSpace.CanUnmortgage();
        }
    }

    // ============================================================
    // STATUS TEXT
    // ============================================================

    private string GetStatusText()
    {
        if (selectedSpace == null)
            return "";

        if (selectedSpace.IsMortgaged)
            return "MORTGAGED";

        if (!selectedSpace.IsProperty)
            return "SPECIAL PROPERTY";

        if (selectedSpace.HasHotel)
            return "HOTEL BUILT";

        int landings =
            selectedSpace.OwnerLandingCount;

        int houses =
            selectedSpace.Houses;

        int maximumHouses =
            selectedSpace.MaximumAllowedHouses;

        // Player can build another house after landing.
        if (houses < maximumHouses)
        {
            return
                $"LANDINGS: {landings} | " +
                $"NEXT: HOUSE {houses + 1}";
        }

        // Four houses reached and hotel landing unlocked.
        if (houses >= 4 &&
            landings >= 6)
        {
            return "HOTEL AVAILABLE";
        }

        return
            $"LANDINGS: {landings} | " +
            $"HOUSES: {houses}/4";
    }

    // ============================================================
    // BUILD HOUSE
    // ============================================================

    private void BuildHouse()
    {
        if (selectedSpace == null)
            return;

        if (gameManager == null)
            return;

        if (!gameManager.BuildHouse(selectedSpace))
            return;

        GameNotificationUI.Show(
            $"HOUSE BUILT ON " +
            $"{selectedSpace.SpaceName.ToUpperInvariant()}"
        );

        Refresh();

        // A landing action is completed after one
        // development action.
        if (actionOpenedFromLanding)
        {
            CompleteLandingAction();
        }
    }

    // ============================================================
    // BUILD HOTEL
    // ============================================================

    private void BuildHotel()
    {
        if (selectedSpace == null)
            return;

        if (gameManager == null)
            return;

        if (!gameManager.BuildHotel(selectedSpace))
            return;

        GameNotificationUI.Show(
            $"HOTEL BUILT ON " +
            $"{selectedSpace.SpaceName.ToUpperInvariant()}"
        );

        Refresh();

        if (actionOpenedFromLanding)
        {
            CompleteLandingAction();
        }
    }

    // ============================================================
    // MORTGAGE
    // ============================================================

    private void Mortgage()
    {
        if (selectedSpace == null)
            return;

        if (gameManager == null)
            return;

        if (!gameManager.MortgageProperty(
                selectedSpace))
        {
            return;
        }

        GameNotificationUI.Show(
            $"{selectedSpace.SpaceName.ToUpperInvariant()} " +
            "MORTGAGED"
        );

        Refresh();

        if (actionOpenedFromLanding)
        {
            CompleteLandingAction();
        }
    }

    // ============================================================
    // UNMORTGAGE
    // ============================================================

    private void Unmortgage()
    {
        if (selectedSpace == null)
            return;

        if (gameManager == null)
            return;

        if (!gameManager.UnmortgageProperty(
                selectedSpace))
        {
            return;
        }

        GameNotificationUI.Show(
            $"{selectedSpace.SpaceName.ToUpperInvariant()} " +
            "UNMORTGAGED"
        );

        Refresh();

        if (actionOpenedFromLanding)
        {
            CompleteLandingAction();
        }
    }

    // ============================================================
    // LANDING ACTION COMPLETE
    // ============================================================

    private void CompleteLandingAction()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }

        actionOpenedFromLanding = false;

        Hide();

        if (gameManager == null)
            return;

        if (gameManager.WaitingForPlayerAction)
        {
            gameManager.EndPlayerAction();
        }

        // Let GameManager decide whether this ends the turn
        // normally or gives the player another roll because
        // of doubles.
        gameManager.EndTurn();
    }

    // ============================================================
    // CLOSE
    // ============================================================

    private void CloseAction()
    {
        if (actionOpenedFromLanding)
        {
            CompleteLandingAction();
            return;
        }

        Hide();
    }

    // ============================================================
    // DISABLE BUTTONS
    // ============================================================

    private void DisableAllButtons()
    {
        if (houseButton != null)
            houseButton.interactable = false;

        if (hotelButton != null)
            hotelButton.interactable = false;

        if (mortgageButton != null)
            mortgageButton.interactable = false;

        if (unmortgageButton != null)
            unmortgageButton.interactable = false;
    }

    // ============================================================
    // HIDE
    // ============================================================

    public void Hide()
    {
        selectedSpace = null;
        actionOpenedFromLanding = false;

        if (panel != null)
            panel.SetActive(false);
    }

    public void RefreshIfVisible()
    {
        if (selectedSpace == null)
            return;

        if (panel == null ||
            !panel.activeSelf)
        {
            return;
        }

        Refresh();
    }
}