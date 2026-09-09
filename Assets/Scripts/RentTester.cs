#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Development-only Rent System Tester.
///
/// IMPORTANT:
/// - Uses the real GameManager.BuySpace().
/// - Uses the real GameManager.PayRent().
/// - Uses the real GameManager.BuildHouse()/BuildHotel().
/// - Reads rent from the real BoardSpace rent system.
/// - Does not directly modify ownership, money, houses, hotel, or mortgage state.
/// </summary>
public class RentTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Keyboard")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F5;
    [SerializeField] private bool openOnStart;

    [Header("Utility Test")]
    [Range(2, 12)]
    [SerializeField] private int utilityDiceRoll = 7;

    // ============================================================
    // DATA
    // ============================================================

    private readonly List<BoardSpace> rentableSpaces =
        new List<BoardSpace>();

    private GameObject canvasObject;
    private GameObject panel;

    private RectTransform propertyListContent;

    private TMP_Text selectedPropertyText;
    private TMP_Text rentReferenceText;
    private TMP_Text playerSetupText;
    private TMP_Text resultText;
    private TMP_Text logText;

    private int selectedPropertyIndex;
    private int selectedBuyerIndex;
    private int selectedOwnerIndex;
    private int selectedPayerIndex;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindGameManager();
    }

    private void Start()
    {
        BuildTesterUI();
        RefreshProperties();
        SetVisible(openOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisible(!IsVisible());
        }
    }

    // ============================================================
    // PROPERTY DISCOVERY
    // ============================================================

    public void RefreshProperties()
    {
        rentableSpaces.Clear();

        BoardSpace[] allSpaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in allSpaces)
        {
            if (space == null)
                continue;

            if (!space.IsProperty &&
                !space.IsRailroad &&
                !space.IsUtility)
            {
                continue;
            }

            rentableSpaces.Add(space);
        }

        rentableSpaces.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(b.BoardIndex)
        );

        selectedPropertyIndex =
            Mathf.Clamp(
                selectedPropertyIndex,
                0,
                Mathf.Max(
                    0,
                    rentableSpaces.Count - 1
                )
            );

        RebuildPropertyList();
        RefreshStatus();
    }

    // ============================================================
    // PROPERTY SELECTION
    // ============================================================

    public void SelectProperty(int index)
    {
        if (index < 0 ||
            index >= rentableSpaces.Count)
        {
            return;
        }

        selectedPropertyIndex = index;

        RefreshStatus();
    }

    // ============================================================
    // PLAYER SELECTION
    // ============================================================

    public void NextBuyer()
    {
        selectedBuyerIndex =
            NextPlayerIndex(
                selectedBuyerIndex
            );

        RefreshStatus();
    }

    public void NextOwner()
    {
        selectedOwnerIndex =
            NextPlayerIndex(
                selectedOwnerIndex
            );

        RefreshStatus();
    }

    public void NextPayer()
    {
        selectedPayerIndex =
            NextPlayerIndex(
                selectedPayerIndex
            );

        RefreshStatus();
    }

    // ============================================================
    // UTILITY DICE
    // ============================================================

    public void DecreaseUtilityDice()
    {
        utilityDiceRoll =
            Mathf.Clamp(
                utilityDiceRoll - 1,
                2,
                12
            );

        RefreshStatus();
    }

    public void IncreaseUtilityDice()
    {
        utilityDiceRoll =
            Mathf.Clamp(
                utilityDiceRoll + 1,
                2,
                12
            );

        RefreshStatus();
    }

    // ============================================================
    // PURCHASE TEST
    // ============================================================

    public void PurchaseSelected()
    {
        FindGameManager();

        BoardSpace space =
            SelectedSpace;

        BoardPlayer buyer =
            SelectedPlayer(
                selectedBuyerIndex
            );

        if (gameManager == null ||
            space == null ||
            buyer == null)
        {
            SetResult(
                "PURCHASE FAILED\n" +
                "Missing GameManager, property, or buyer."
            );

            return;
        }

        if (buyer != gameManager.CurrentPlayer)
        {
            SetResult(
                "PURCHASE BLOCKED\n" +
                "The real GameManager only allows the current player to buy."
            );

            return;
        }

        int balanceBefore =
            buyer.Money;

        bool success =
            gameManager.BuySpace(
                space
            );

        int balanceAfter =
            buyer.Money;

        if (success)
        {
            selectedOwnerIndex =
                selectedBuyerIndex;

            if (gameManager.Players != null &&
                gameManager.Players.Length > 1)
            {
                selectedPayerIndex =
                    (selectedOwnerIndex + 1) %
                    gameManager.Players.Length;
            }
        }

        SetResult(
            success
                ? "PURCHASE SUCCESS\n" +
                  $"{space.SpaceName}\n" +
                  $"{buyer.PlayerName} now owns this property.\n" +
                  $"Balance: ${balanceBefore:N0}M → ${balanceAfter:N0}M"
                : "PURCHASE FAILED\n" +
                  "The real purchase rules rejected the transaction."
        );

        AppendLog(
            "PURCHASE TEST\n" +
            $"Property: {space.SpaceName}\n" +
            $"Buyer: {buyer.PlayerName}\n" +
            $"Balance: ${balanceBefore:N0}M → ${balanceAfter:N0}M\n" +
            $"RESULT: {(success ? "SUCCESS" : "FAILED")}"
        );

        RefreshStatus();
    }

    // ============================================================
    // HOUSE TEST
    // ============================================================

    public void AddHouseToSelected()
    {
        FindGameManager();

        BoardSpace space =
            SelectedSpace;

        if (gameManager == null ||
            space == null)
        {
            SetResult(
                "HOUSE FAILED\n" +
                "Missing GameManager or property."
            );

            return;
        }

        if (!space.IsProperty)
        {
            SetResult(
                "HOUSE BLOCKED\n" +
                "Houses only apply to normal properties."
            );

            return;
        }

        int before =
            space.Houses;

        bool success =
            gameManager.BuildHouse(
                space
            );

        int after =
            space.Houses;

        SetResult(
            success
                ? $"HOUSE BUILT\n" +
                  $"{space.SpaceName}\n" +
                  $"{before} houses → {after} houses"
                : "HOUSE BLOCKED\n" +
                  "The real building rules rejected the action."
        );

        AppendLog(
            "HOUSE TEST\n" +
            $"Property: {space.SpaceName}\n" +
            $"Houses: {before} → {after}\n" +
            $"RESULT: {(success ? "SUCCESS" : "BLOCKED")}"
        );

        RefreshStatus();
    }

    // ============================================================
    // HOTEL TEST
    // ============================================================

    public void AddHotelToSelected()
    {
        FindGameManager();

        BoardSpace space =
            SelectedSpace;

        if (gameManager == null ||
            space == null)
        {
            SetResult(
                "HOTEL FAILED\n" +
                "Missing GameManager or property."
            );

            return;
        }

        if (!space.IsProperty)
        {
            SetResult(
                "HOTEL BLOCKED\n" +
                "Hotels only apply to normal properties."
            );

            return;
        }

        bool before =
            space.HasHotel;

        bool success =
            gameManager.BuildHotel(
                space
            );

        bool after =
            space.HasHotel;

        SetResult(
            success
                ? $"HOTEL BUILT\n" +
                  $"{space.SpaceName}\n" +
                  $"Hotel: {(before ? "YES" : "NO")} → {(after ? "YES" : "NO")}"
                : "HOTEL BLOCKED\n" +
                  "The real building rules rejected the action."
        );

        AppendLog(
            "HOTEL TEST\n" +
            $"Property: {space.SpaceName}\n" +
            $"Hotel: {before} → {after}\n" +
            $"RESULT: {(success ? "SUCCESS" : "BLOCKED")}"
        );

        RefreshStatus();
    }

    // ============================================================
    // RENT TEST
    // ============================================================

    public void TestRent()
    {
        FindGameManager();

        BoardSpace space =
            SelectedSpace;

        if (gameManager == null ||
            space == null)
        {
            SetResult(
                "RENT TEST FAILED\n" +
                "Missing GameManager or property."
            );

            return;
        }

        BoardPlayer actualPayer =
            gameManager.CurrentPlayer;

        BoardPlayer actualOwner =
            space.Owner;

        BoardPlayer selectedOwner =
            SelectedPlayer(
                selectedOwnerIndex
            );

        BoardPlayer selectedPayer =
            SelectedPlayer(
                selectedPayerIndex
            );

        if (actualOwner == null)
        {
            SetResult(
                "RENT BLOCKED\n" +
                "This property is owned by the BANK.\n" +
                "Purchase it first."
            );

            return;
        }

        if (selectedOwner != actualOwner)
        {
            SetResult(
                "RENT BLOCKED\n" +
                $"Selected owner: {GetPlayerName(selectedOwner)}\n" +
                $"Actual owner: {actualOwner.PlayerName}"
            );

            return;
        }

        if (actualPayer == null)
        {
            SetResult(
                "RENT BLOCKED\n" +
                "No current player."
            );

            return;
        }

        if (selectedPayer != actualPayer)
        {
            SetResult(
                "RENT BLOCKED\n" +
                $"Selected payer: {GetPlayerName(selectedPayer)}\n" +
                $"Current player: {actualPayer.PlayerName}"
            );

            return;
        }

        if (actualPayer == actualOwner)
        {
            SetResult(
                "RENT BLOCKED\n" +
                "Owner and payer must be different."
            );

            return;
        }

        if (space.IsUtility)
        {
            gameManager.SetLastDiceRoll(
                utilityDiceRoll
            );
        }

        int rentBefore =
            GetActualRentValue(
                space
            );

        int payerBefore =
            actualPayer.Money;

        int ownerBefore =
            actualOwner.Money;

        bool success =
            gameManager.PayRent(
                space
            );

        int payerAfter =
            actualPayer.Money;

        int ownerAfter =
            actualOwner.Money;

        int paid =
            payerBefore -
            payerAfter;

        int received =
            ownerAfter -
            ownerBefore;

        bool normalTransfer =
            paid > 0 &&
            paid == received;

        bool zeroRent =
            rentBefore <= 0 &&
            paid == 0 &&
            received == 0 &&
            success;

        bool blockedOrFailed =
            !success;

        bool pass =
            normalTransfer ||
            zeroRent ||
            blockedOrFailed;

        string transaction;

        if (normalTransfer)
        {
            transaction = "PLAYER → PLAYER";
        }
        else if (success &&
                 paid == 0 &&
                 received > 0)
        {
            transaction = "SPECIAL / BANK → OWNER";
        }
        else if (zeroRent)
        {
            transaction = "NO MONEY TRANSFER";
        }
        else
        {
            transaction = "FAILED / BANKRUPTCY";
        }

        SetResult(
            $"RENT TEST {(pass ? "PASS" : "FAIL")}\n\n" +
            $"{actualPayer.PlayerName} → {actualOwner.PlayerName}\n" +
            $"Rent: ${rentBefore:N0}M\n" +
            $"Paid: ${paid:N0}M\n" +
            $"Received: ${received:N0}M\n" +
            $"Result: {transaction}"
        );

        AppendLog(
            "RENT TEST\n" +
            $"Property: {space.SpaceName}\n" +
            $"Type: {GetTypeName(space)}\n" +
            $"Houses: {space.Houses}/4\n" +
            $"Hotel: {(space.HasHotel ? "YES" : "NO")}\n" +
            $"Mortgaged: {(space.IsMortgaged ? "YES" : "NO")}\n" +
            $"Rent: ${rentBefore:N0}M\n" +
            $"Payer: {actualPayer.PlayerName}\n" +
            $"Payer balance: ${payerBefore:N0}M → ${payerAfter:N0}M\n" +
            $"Owner: {actualOwner.PlayerName}\n" +
            $"Owner balance: ${ownerBefore:N0}M → ${ownerAfter:N0}M\n" +
            $"Actual paid: ${paid:N0}M\n" +
            $"Actual received: ${received:N0}M\n" +
            $"Transaction: {transaction}\n" +
            $"PayRent(): {(success ? "SUCCESS" : "FAILED")}\n" +
            $"RESULT: {(pass ? "PASS" : "FAIL")}"
        );

        RefreshStatus();
    }

    // ============================================================
    // STATUS
    // ============================================================

    public void RefreshStatus()
    {
        FindGameManager();

        BoardSpace space =
            SelectedSpace;

        if (selectedPropertyText != null)
        {
            selectedPropertyText.text =
                BuildPropertyText(
                    space
                );
        }

        if (rentReferenceText != null)
        {
            rentReferenceText.text =
                BuildRentReferenceText(
                    space
                );
        }

        if (playerSetupText != null)
        {
            playerSetupText.text =
                BuildPlayerSetupText();
        }
    }

    private string BuildPropertyText(
        BoardSpace space)
    {
        if (space == null)
        {
            return
                "NO PROPERTY SELECTED";
        }

        string owner =
            space.Owner != null
                ? space.Owner.PlayerName
                : "BANK";

        string group =
            space.PropertyGroup != null
                ? space.PropertyGroup.GroupName
                : "NONE";

        string type =
            GetTypeName(
                space
            );

        string text =
            $"{space.SpaceName}\n" +
            $"TYPE: {type}\n" +
            $"PRICE: ${space.PurchasePrice:N0}M\n" +
            $"CURRENT RENT: ${GetActualRentValue(space):N0}M\n" +
            $"OWNER: {owner}\n" +
            $"HOUSES: {space.Houses}/4\n" +
            $"HOTEL: {(space.HasHotel ? "YES" : "NO")}\n" +
            $"MORTGAGED: {(space.IsMortgaged ? "YES" : "NO")}\n" +
            $"GROUP: {group}";

        if (space.IsRailroad)
        {
            text +=
                $"\nOWNED AIRPORTS: " +
                $"{CountOwnedSpaces(space.Owner, true, false)}";
        }

        if (space.IsUtility)
        {
            text +=
                $"\nUTILITY DICE: {utilityDiceRoll}\n" +
                $"OWNED UTILITIES: " +
                $"{CountOwnedSpaces(space.Owner, false, true)}";
        }

        return text;
    }

    private string BuildRentReferenceText(
        BoardSpace space)
    {
        if (space == null)
            return "RENT";

        if (space.IsUtility)
        {
            return
                "RENT\n" +
                $"DICE ROLL: {utilityDiceRoll}\n" +
                $"UTILITY RENT: " +
                $"${space.GetUtilityRent(utilityDiceRoll):N0}M\n\n" +
                "Uses the real utility rent system.";
        }

        if (space.IsRailroad)
        {
            return
                "RENT\n" +
                $"AIRPORT RENT: ${space.GetRent():N0}M\n\n" +
                "Uses the real airport rent system.";
        }

        PropertyEconomy economy =
            PropertyEconomy.FromPurchasePrice(
                space.PurchasePrice
            );

        return
            "RENT REFERENCE\n" +
            $"BASE: ${economy.baseRent:N0}M\n" +
            $"1 HOUSE: ${economy.houseRent:N0}M\n" +
            $"2 HOUSES: ${economy.twoHouseRent:N0}M\n" +
            $"3 HOUSES: ${economy.threeHouseRent:N0}M\n" +
            $"4 HOUSES: ${economy.fourHouseRent:N0}M\n" +
            $"HOTEL: ${economy.hotelRent:N0}M\n\n" +
            $"CURRENT REAL RENT: " +
            $"${space.GetRent():N0}M";
    }

    private string BuildPlayerSetupText()
    {
        BoardPlayer buyer =
            SelectedPlayer(
                selectedBuyerIndex
            );

        BoardPlayer owner =
            SelectedPlayer(
                selectedOwnerIndex
            );

        BoardPlayer payer =
            SelectedPlayer(
                selectedPayerIndex
            );

        string current =
            gameManager != null &&
            gameManager.CurrentPlayer != null
                ? gameManager.CurrentPlayer.PlayerName
                : "NONE";

        return
            "PLAYERS\n" +
            $"BUYER: {GetPlayerName(buyer)}\n" +
            $"OWNER: {GetPlayerName(owner)}\n" +
            $"PAYER: {GetPlayerName(payer)}\n" +
            $"CURRENT PLAYER: {current}\n\n" +
            "Purchase and rent always use the real game APIs.";
    }

    // ============================================================
    // HELPERS
    // ============================================================

    private BoardSpace SelectedSpace
    {
        get
        {
            if (selectedPropertyIndex < 0 ||
                selectedPropertyIndex >= rentableSpaces.Count)
            {
                return null;
            }

            return rentableSpaces[
                selectedPropertyIndex
            ];
        }
    }

    private BoardPlayer SelectedPlayer(
        int index)
    {
        if (gameManager == null ||
            gameManager.Players == null ||
            gameManager.Players.Length == 0)
        {
            return null;
        }

        int safeIndex =
            Mathf.Clamp(
                index,
                0,
                gameManager.Players.Length - 1
            );

        return gameManager.Players[
            safeIndex
        ];
    }

    private int NextPlayerIndex(
        int currentIndex)
    {
        if (gameManager == null ||
            gameManager.Players == null ||
            gameManager.Players.Length == 0)
        {
            return 0;
        }

        return
            (currentIndex + 1) %
            gameManager.Players.Length;
    }

    private int GetActualRentValue(
        BoardSpace space)
    {
        if (space == null)
            return 0;

        if (space.IsUtility)
        {
            return space.GetUtilityRent(
                utilityDiceRoll
            );
        }

        return space.GetRent();
    }

    private int CountOwnedSpaces(
        BoardPlayer owner,
        bool airports,
        bool utilities)
    {
        if (owner == null)
            return 0;

        int count = 0;

        foreach (BoardSpace space in rentableSpaces)
        {
            if (space == null ||
                space.Owner != owner)
            {
                continue;
            }

            if ((airports && space.IsRailroad) ||
                (utilities && space.IsUtility))
            {
                count++;
            }
        }

        return count;
    }

    private string GetTypeName(
        BoardSpace space)
    {
        if (space == null)
            return "UNKNOWN";

        if (space.IsRailroad)
            return "AIRPORT";

        if (space.IsUtility)
            return "UTILITY";

        return "PROPERTY";
    }

    private string GetPlayerName(
        BoardPlayer player)
    {
        return player != null
            ? player.PlayerName
            : "NONE";
    }

    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>(
                    FindObjectsInactive.Include
                );
        }
    }

    // ============================================================
    // PROPERTY LIST UI
    // ============================================================

    private void RebuildPropertyList()
    {
        if (propertyListContent == null)
            return;

        for (int i =
             propertyListContent.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                propertyListContent.GetChild(i).gameObject
            );
        }

        for (int i = 0;
             i < rentableSpaces.Count;
             i++)
        {
            BoardSpace space =
                rentableSpaces[i];

            int index = i;

            Button button =
                CreateButton(
                    propertyListContent,
                    $"{space.BoardIndex}  {space.SpaceName}",
                    17f,
                    42f
                );

            button.onClick.AddListener(
                () =>
                    SelectProperty(index)
            );
        }
    }

    // ============================================================
    // MAIN UI
    // ============================================================

    private void BuildTesterUI()
    {
        canvasObject =
            new GameObject(
                "RentTesterCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)
            );

        Canvas canvas =
            canvasObject.GetComponent<Canvas>();

        canvas.renderMode =
            RenderMode.ScreenSpaceOverlay;

        canvas.sortingOrder =
            610;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();

        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;

        scaler.referenceResolution =
            new Vector2(
                1920f,
                1080f
            );

        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;

        scaler.matchWidthOrHeight =
            0.5f;

        panel =
            new GameObject(
                "RentTesterPanel",
                typeof(RectTransform),
                typeof(Image)
            );

        panel.transform.SetParent(
            canvas.transform,
            false
        );

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();

        panelRect.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        panelRect.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        panelRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        panelRect.anchoredPosition =
            Vector2.zero;

        panelRect.sizeDelta =
            new Vector2(
                1200f,
                760f
            );

        panel.GetComponent<Image>().color =
            new Color(
                0.025f,
                0.045f,
                0.075f,
                0.99f
            );

        BuildHeader(panel.transform);
        BuildLeftPanel(panel.transform);
        BuildRightPanel(panel.transform);
        BuildBottomBar(panel.transform);
    }

    // ============================================================
    // HEADER
    // ============================================================

    private void BuildHeader(
        Transform parent)
    {
        GameObject header =
            CreateFixedPanel(
                parent,
                "Header",
                new Vector2(
                    0.03f,
                    0.89f
                ),
                new Vector2(
                    0.97f,
                    0.97f
                ),
                new Color(
                    0.07f,
                    0.13f,
                    0.22f,
                    1f
                )
            );

        TMP_Text title =
            CreateAnchoredText(
                header.transform,
                "RENT SYSTEM TESTER",
                30f,
                TextAlignmentOptions.Center,
                Color.white
            );

        AnchorFull(
            title.rectTransform
        );

        title.fontStyle =
            FontStyles.Bold;
    }

    // ============================================================
    // LEFT PANEL
    // ============================================================

    private void BuildLeftPanel(
        Transform parent)
    {
        GameObject left =
            CreateFixedPanel(
                parent,
                "PropertyPanel",
                new Vector2(
                    0.03f,
                    0.12f
                ),
                new Vector2(
                    0.33f,
                    0.87f
                ),
                new Color(
                    0.045f,
                    0.075f,
                    0.12f,
                    1f
                )
            );

        CreateAnchoredText(
            left.transform,
            "PROPERTIES",
            21f,
            TextAlignmentOptions.Center,
            Color.white,
            new Vector2(
                0.03f,
                0.92f
            ),
            new Vector2(
                0.97f,
                0.99f
            )
        );

        GameObject viewport =
            new GameObject(
                "PropertyViewport",
                typeof(RectTransform),
                typeof(Image),
                typeof(Mask),
                typeof(ScrollRect)
            );

        viewport.transform.SetParent(
            left.transform,
            false
        );

        RectTransform viewportRect =
            viewport.GetComponent<RectTransform>();

        viewportRect.anchorMin =
            new Vector2(
                0.03f,
                0.03f
            );

        viewportRect.anchorMax =
            new Vector2(
                0.97f,
                0.91f
            );

        viewportRect.offsetMin =
            Vector2.zero;

        viewportRect.offsetMax =
            Vector2.zero;

        Image image =
            viewport.GetComponent<Image>();

        image.color =
            new Color(
                0.02f,
                0.035f,
                0.055f,
                1f
            );

        Mask mask =
            viewport.GetComponent<Mask>();

        mask.showMaskGraphic =
            true;

        GameObject content =
            new GameObject(
                "PropertyContent",
                typeof(RectTransform),
                typeof(VerticalLayoutGroup),
                typeof(ContentSizeFitter)
            );

        content.transform.SetParent(
            viewport.transform,
            false
        );

        RectTransform contentRect =
            content.GetComponent<RectTransform>();

        contentRect.anchorMin =
            new Vector2(
                0f,
                1f
            );

        contentRect.anchorMax =
            new Vector2(
                1f,
                1f
            );

        contentRect.pivot =
            new Vector2(
                0.5f,
                1f
            );

        contentRect.offsetMin =
            Vector2.zero;

        contentRect.offsetMax =
            Vector2.zero;

        VerticalLayoutGroup layout =
            content.GetComponent<VerticalLayoutGroup>();

        layout.padding =
            new RectOffset(
                8,
                8,
                8,
                8
            );

        layout.spacing =
            5f;

        layout.childAlignment =
            TextAnchor.UpperCenter;

        layout.childControlWidth =
            true;

        layout.childControlHeight =
            true;

        layout.childForceExpandWidth =
            true;

        layout.childForceExpandHeight =
            false;

        ContentSizeFitter fitter =
            content.GetComponent<ContentSizeFitter>();

        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll =
            viewport.GetComponent<ScrollRect>();

        scroll.viewport =
            viewportRect;

        scroll.content =
            contentRect;

        scroll.horizontal =
            false;

        scroll.vertical =
            true;

        scroll.movementType =
            ScrollRect.MovementType.Clamped;

        scroll.scrollSensitivity =
            40f;

        propertyListContent =
            content.GetComponent<RectTransform>();
    }

    // ============================================================
    // RIGHT PANEL
    // ============================================================

    private void BuildRightPanel(
        Transform parent)
    {
        GameObject right =
            CreateFixedPanel(
                parent,
                "InformationPanel",
                new Vector2(
                    0.35f,
                    0.12f
                ),
                new Vector2(
                    0.97f,
                    0.87f
                ),
                new Color(
                    0.045f,
                    0.075f,
                    0.12f,
                    1f
                )
            );

        // --------------------------------------------------------
        // Property information
        // --------------------------------------------------------

        GameObject propertyInfo =
            CreateFixedPanel(
                right.transform,
                "PropertyInfo",
                new Vector2(
                    0.025f,
                    0.57f
                ),
                new Vector2(
                    0.49f,
                    0.97f
                ),
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    1f
                )
            );

        CreateAnchoredText(
            propertyInfo.transform,
            "PROPERTY",
            20f,
            TextAlignmentOptions.Center,
            Color.white,
            new Vector2(
                0.05f,
                0.87f
            ),
            new Vector2(
                0.95f,
                0.97f
            )
        );

        selectedPropertyText =
            CreateAnchoredText(
                propertyInfo.transform,
                "",
                18f,
                TextAlignmentOptions.TopLeft,
                Color.white,
                new Vector2(
                    0.07f,
                    0.08f
                ),
                new Vector2(
                    0.93f,
                    0.86f
                )
            );

        // --------------------------------------------------------
        // Rent reference
        // --------------------------------------------------------

        GameObject rentInfo =
            CreateFixedPanel(
                right.transform,
                "RentInfo",
                new Vector2(
                    0.51f,
                    0.57f
                ),
                new Vector2(
                    0.975f,
                    0.97f
                ),
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    1f
                )
            );

        CreateAnchoredText(
            rentInfo.transform,
            "RENT",
            20f,
            TextAlignmentOptions.Center,
            Color.white,
            new Vector2(
                0.05f,
                0.87f
            ),
            new Vector2(
                0.95f,
                0.97f
            )
        );

        rentReferenceText =
            CreateAnchoredText(
                rentInfo.transform,
                "",
                17f,
                TextAlignmentOptions.TopLeft,
                Color.white,
                new Vector2(
                    0.08f,
                    0.08f
                ),
                new Vector2(
                    0.92f,
                    0.86f
                )
            );

        // --------------------------------------------------------
        // Player setup
        // --------------------------------------------------------

        GameObject setup =
            CreateFixedPanel(
                right.transform,
                "PlayerSetup",
                new Vector2(
                    0.025f,
                    0.30f
                ),
                new Vector2(
                    0.975f,
                    0.55f
                ),
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    1f
                )
            );

        CreateAnchoredText(
            setup.transform,
            "TEST SETUP",
            19f,
            TextAlignmentOptions.Center,
            Color.white,
            new Vector2(
                0.03f,
                0.78f
            ),
            new Vector2(
                0.97f,
                0.96f
            )
        );

        playerSetupText =
            CreateAnchoredText(
                setup.transform,
                "",
                16f,
                TextAlignmentOptions.TopLeft,
                Color.white,
                new Vector2(
                    0.04f,
                    0.08f
                ),
                new Vector2(
                    0.28f,
                    0.76f
                )
            );

        Button nextBuyer =
            CreateAnchoredButton(
                setup.transform,
                "BUYER  →",
                17f,
                new Vector2(
                    0.31f,
                    0.50f
                ),
                new Vector2(
                    0.47f,
                    0.72f
                )
            );

        nextBuyer.onClick.AddListener(
            NextBuyer
        );

        Button purchase =
            CreateAnchoredButton(
                setup.transform,
                "PURCHASE",
                17f,
                new Vector2(
                    0.50f,
                    0.50f
                ),
                new Vector2(
                    0.70f,
                    0.72f
                )
            );

        purchase.onClick.AddListener(
            PurchaseSelected
        );

        Button nextOwner =
            CreateAnchoredButton(
                setup.transform,
                "OWNER  →",
                17f,
                new Vector2(
                    0.31f,
                    0.24f
                ),
                new Vector2(
                    0.47f,
                    0.46f
                )
            );

        nextOwner.onClick.AddListener(
            NextOwner
        );

        Button nextPayer =
            CreateAnchoredButton(
                setup.transform,
                "PAYER  →",
                17f,
                new Vector2(
                    0.50f,
                    0.24f
                ),
                new Vector2(
                    0.70f,
                    0.46f
                )
            );

        nextPayer.onClick.AddListener(
            NextPayer
        );

        // --------------------------------------------------------
        // Development / rent controls
        // --------------------------------------------------------

        Button addHouse =
            CreateAnchoredButton(
                setup.transform,
                "ADD HOUSE",
                16f,
                new Vector2(
                    0.72f,
                    0.50f
                ),
                new Vector2(
                    0.86f,
                    0.72f
                )
            );

        addHouse.onClick.AddListener(
            AddHouseToSelected
        );

        Button addHotel =
            CreateAnchoredButton(
                setup.transform,
                "ADD HOTEL",
                16f,
                new Vector2(
                    0.87f,
                    0.50f
                ),
                new Vector2(
                    0.99f,
                    0.72f
                )
            );

        addHotel.onClick.AddListener(
            AddHotelToSelected
        );

        Button diceDown =
            CreateAnchoredButton(
                setup.transform,
                "DICE −",
                16f,
                new Vector2(
                    0.72f,
                    0.24f
                ),
                new Vector2(
                    0.82f,
                    0.46f
                )
            );

        diceDown.onClick.AddListener(
            DecreaseUtilityDice
        );

        Button diceUp =
            CreateAnchoredButton(
                setup.transform,
                "DICE +",
                16f,
                new Vector2(
                    0.83f,
                    0.24f
                ),
                new Vector2(
                    0.93f,
                    0.46f
                )
            );

        diceUp.onClick.AddListener(
            IncreaseUtilityDice
        );

        Button testRent =
            CreateAnchoredButton(
                setup.transform,
                "TEST RENT",
                16f,
                new Vector2(
                    0.72f,
                    0.08f
                ),
                new Vector2(
                    0.93f,
                    0.20f
                )
            );

        testRent.onClick.AddListener(
            TestRent
        );
    }

    // ============================================================
    // RESULT AREA
    // ============================================================

    private void BuildBottomBar(
        Transform parent)
    {
        GameObject result =
            CreateFixedPanel(
                parent,
                "ResultPanel",
                new Vector2(
                    0.35f,
                    0.02f
                ),
                new Vector2(
                    0.72f,
                    0.095f
                ),
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    1f
                )
            );

        CreateAnchoredText(
            result.transform,
            "RESULT",
            17f,
            TextAlignmentOptions.Left,
            Color.white,
            new Vector2(
                0.02f,
                0.10f
            ),
            new Vector2(
                0.14f,
                0.90f
            )
        );

        resultText =
            CreateAnchoredText(
                result.transform,
                "No test run yet.",
                15f,
                TextAlignmentOptions.Left,
                Color.white,
                new Vector2(
                    0.15f,
                    0.08f
                ),
                new Vector2(
                    0.98f,
                    0.92f
                )
            );

        GameObject log =
            CreateFixedPanel(
                parent,
                "LogPanel",
                new Vector2(
                    0.73f,
                    0.02f
                ),
                new Vector2(
                    0.97f,
                    0.095f
                ),
                new Color(
                    0.015f,
                    0.025f,
                    0.045f,
                    1f
                )
            );

        logText =
            CreateAnchoredText(
                log.transform,
                "Log: no tests yet.",
                12f,
                TextAlignmentOptions.TopLeft,
                new Color(
                    0.75f,
                    0.80f,
                    0.88f,
                    1f
                ),
                new Vector2(
                    0.04f,
                    0.08f
                ),
                new Vector2(
                    0.96f,
                    0.92f
                )
            );

        CreateAnchoredButton(
            parent,
            "REFRESH",
            15f,
            new Vector2(
                0.03f,
                0.035f
            ),
            new Vector2(
                0.13f,
                0.085f
            )
        ).onClick.AddListener(
            RefreshProperties
        );

        CreateAnchoredButton(
            parent,
            "RESET",
            15f,
            new Vector2(
                0.14f,
                0.035f
            ),
            new Vector2(
                0.24f,
                0.085f
            )
        ).onClick.AddListener(
            ResetTestView
        );

        CreateAnchoredButton(
            parent,
            "CLOSE  (F5)",
            15f,
            new Vector2(
                0.25f,
                0.035f
            ),
            new Vector2(
                0.33f,
                0.085f
            )
        ).onClick.AddListener(
            () => SetVisible(false)
        );
    }

    // ============================================================
    // FIXED UI HELPERS
    // ============================================================

    private GameObject CreateFixedPanel(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        obj.GetComponent<Image>().color =
            color;

        return obj;
    }

    private TMP_Text CreateAnchoredText(
        Transform parent,
        string value,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color,
        Vector2 anchorMin = default,
        Vector2 anchorMax = default)
    {
        GameObject obj =
            new GameObject(
                "Text",
                typeof(RectTransform)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        text.text =
            value ?? string.Empty;

        text.fontSize =
            fontSize;

        text.color =
            color;

        text.alignment =
            alignment;

        text.enableAutoSizing =
            false;

        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget =
            false;

        RectTransform rect =
            text.rectTransform;

        if (anchorMin == default &&
            anchorMax == default)
        {
            AnchorFull(rect);
        }
        else
        {
            rect.anchorMin =
                anchorMin;

            rect.anchorMax =
                anchorMax;

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;
        }

        return text;
    }

    private Button CreateAnchoredButton(
        Transform parent,
        string label,
        float fontSize,
        Vector2 anchorMin,
        Vector2 anchorMax)
    {
        GameObject obj =
            new GameObject(
                label + "Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

        Image image =
            obj.GetComponent<Image>();

        image.color =
            new Color(
                0.10f,
                0.30f,
                0.58f,
                1f
            );

        Button button =
            obj.GetComponent<Button>();

        ColorBlock colors =
            button.colors;

        colors.normalColor =
            new Color(
                0.10f,
                0.30f,
                0.58f,
                1f
            );

        colors.highlightedColor =
            new Color(
                0.17f,
                0.42f,
                0.72f,
                1f
            );

        colors.pressedColor =
            new Color(
                0.07f,
                0.20f,
                0.40f,
                1f
            );

        colors.disabledColor =
            new Color(
                0.10f,
                0.15f,
                0.22f,
                0.55f
            );

        colors.fadeDuration =
            0.08f;

        button.colors =
            colors;

        TMP_Text text =
            CreateAnchoredText(
                obj.transform,
                label,
                fontSize,
                TextAlignmentOptions.Center,
                Color.white
            );

        text.fontStyle =
            FontStyles.Bold;

        return button;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        float fontSize,
        float height)
    {
        GameObject obj =
            new GameObject(
                label + "Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        LayoutElement layout =
            obj.AddComponent<LayoutElement>();

        layout.preferredHeight =
            height;

        Image image =
            obj.GetComponent<Image>();

        image.color =
            new Color(
                0.10f,
                0.30f,
                0.58f,
                1f
            );

        Button button =
            obj.GetComponent<Button>();

        TMP_Text text =
            new GameObject(
                "Text",
                typeof(RectTransform)
            ).AddComponent<TextMeshProUGUI>();

        text.transform.SetParent(
            obj.transform,
            false
        );

        text.text =
            label;

        text.fontSize =
            fontSize;

        text.fontStyle =
            FontStyles.Bold;

        text.color =
            Color.white;

        text.alignment =
            TextAlignmentOptions.Center;

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.raycastTarget =
            false;

        AnchorFull(
            text.rectTransform
        );

        return button;
    }

    private void AnchorFull(
        RectTransform rect)
    {
        rect.anchorMin =
            Vector2.zero;

        rect.anchorMax =
            Vector2.one;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;
    }

    // ============================================================
    // RESULT / LOG
    // ============================================================

    private void SetResult(
        string message)
    {
        if (resultText != null)
        {
            resultText.text =
                message;
        }
    }

    private void AppendLog(
        string entry)
    {
        if (logText == null)
            return;

        logText.text =
            entry +
            "\n\n" +
            logText.text;
    }

    // ============================================================
    // VISIBILITY
    // ============================================================

    private bool IsVisible()
    {
        return panel != null &&
               panel.activeSelf;
    }

    private void SetVisible(
        bool visible)
    {
        if (panel != null)
        {
            panel.SetActive(
                visible
            );
        }

        if (visible)
        {
            RefreshStatus();
        }
    }

    // ============================================================
    // RESET
    // ============================================================

    public void ResetTestView()
    {
        selectedPropertyIndex = 0;
        selectedBuyerIndex = 0;
        selectedOwnerIndex = 0;
        selectedPayerIndex = 0;
        utilityDiceRoll = 7;

        SetResult(
            "RESET\nNo gameplay state was changed."
        );

        if (logText != null)
        {
            logText.text =
                "Log: reset.";
        }

        RefreshStatus();
    }
}

#endif