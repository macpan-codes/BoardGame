#if UNITY_EDITOR || DEVELOPMENT_BUILD

using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RentTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;

    [Header("Keyboard")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F5;
    [SerializeField] private bool openOnStart = true;

    [Header("Utility")]
    [Range(2, 12)]
    [SerializeField] private int utilityDiceRoll = 7;

    private readonly List<BoardSpace> rentableSpaces =
        new List<BoardSpace>();

    private GameObject canvasObject;
    private GameObject panel;

    private RectTransform propertyListContent;

    private TMP_Text propertyText;
    private TMP_Text rentText;
    private TMP_Text setupText;
    private TMP_Text resultText;

    private int selectedPropertyIndex;
    private int selectedBuyerIndex;
    private int selectedOwnerIndex;
    private int selectedPayerIndex;

    private bool busy;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindGameManager();
    }

    private void Start()
    {
        BuildUI();
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
    // PROPERTY LIST
    // ============================================================

    public void RefreshProperties()
    {
        rentableSpaces.Clear();

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
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
                a.BoardIndex.CompareTo(
                    b.BoardIndex
                )
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

    public void SelectProperty(
        int index)
    {
        if (index < 0 ||
            index >= rentableSpaces.Count)
        {
            return;
        }

        selectedPropertyIndex =
            index;

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
    // MOVE BUYER
    // ============================================================

    public void MoveBuyerHere()
    {
        BoardSpace property =
            SelectedSpace;

        BoardPlayer buyer =
            SelectedPlayer(
                selectedBuyerIndex
            );

        if (!PrepareMovement(
                buyer,
                property,
                "BUYER"))
        {
            return;
        }

        busy = true;

        SetResult(
            $"MOVING {buyer.PlayerName}\n" +
            $"→ {property.SpaceName}"
        );

        buyer.MoveToSpaceForRentTest(
            property.BoardIndex,
            false
        );

        StartCoroutine(
            WaitForMovement(
                buyer,
                null
            )
        );
    }

    // ============================================================
    // MOVE OWNER
    // ============================================================

    public void MoveOwnerHere()
    {
        BoardSpace property =
            SelectedSpace;

        if (property == null)
        {
            SetResult(
                "MOVE OWNER FAILED\n" +
                "Select a property."
            );

            return;
        }

        BoardPlayer owner =
            property.Owner;

        if (owner == null)
        {
            SetResult(
                "MOVE OWNER FAILED\n" +
                "The selected property has no owner."
            );

            return;
        }

        if (!PrepareMovement(
                owner,
                property,
                "OWNER"))
        {
            return;
        }

        selectedOwnerIndex =
            GetPlayerIndex(owner);

        busy = true;

        SetResult(
            $"MOVING OWNER\n" +
            $"{owner.PlayerName}\n" +
            $"→ {property.SpaceName}"
        );

        owner.MoveToSpaceForRentTest(
            property.BoardIndex,
            true
        );

        StartCoroutine(
            WaitForMovement(
                owner,
                null
            )
        );
    }

    // ============================================================
    // PURCHASE
    // ============================================================

    public void PurchaseSelected()
    {
        FindGameManager();

        BoardSpace property =
            SelectedSpace;

        BoardPlayer buyer =
            SelectedPlayer(
                selectedBuyerIndex
            );

        if (property == null ||
            buyer == null ||
            gameManager == null)
        {
            SetResult(
                "PURCHASE FAILED\n" +
                "Missing property, buyer, or GameManager."
            );

            return;
        }

        if (busy)
        {
            SetResult(
                "WAIT\n" +
                "Player is still moving."
            );

            return;
        }

        if (gameManager.CurrentPlayer != buyer)
        {
            SetResult(
                $"PURCHASE BLOCKED\n" +
                $"Current player: {GetPlayerName(gameManager.CurrentPlayer)}\n" +
                $"Buyer: {buyer.PlayerName}\n\n" +
                "Click MOVE BUYER first."
            );

            return;
        }

        if (buyer.CurrentSpaceIndex !=
            property.BoardIndex)
        {
            SetResult(
                "PURCHASE BLOCKED\n" +
                "Buyer is not standing on the property."
            );

            return;
        }

        int before =
            buyer.Money;

        bool success =
            gameManager.BuySpace(
                property
            );

        int after =
            buyer.Money;

        if (success)
        {
            selectedOwnerIndex =
                selectedBuyerIndex;

            selectedPayerIndex =
                FindDifferentPlayer(
                    selectedOwnerIndex
                );

            SetResult(
                $"PURCHASE SUCCESS\n" +
                $"{buyer.PlayerName} → {property.SpaceName}\n" +
                $"${before:N0}M → ${after:N0}M"
            );
        }
        else
        {
            SetResult(
                "PURCHASE FAILED\n" +
                "The real game purchase rules rejected it."
            );
        }

        RefreshStatus();
    }

    // ============================================================
    // HOUSE
    // ============================================================

    public void AddHouse()
    {
        FindGameManager();

        BoardSpace property =
            SelectedSpace;

        if (property == null ||
            gameManager == null)
        {
            SetResult(
                "HOUSE FAILED\n" +
                "Select a property."
            );

            return;
        }

        BoardPlayer owner =
            property.Owner;

        if (owner == null)
        {
            SetResult(
                "HOUSE FAILED\n" +
                "Property has no owner."
            );

            return;
        }

        if (gameManager.CurrentPlayer != owner)
        {
            SetResult(
                "HOUSE BLOCKED\n" +
                $"Current player: " +
                $"{GetPlayerName(gameManager.CurrentPlayer)}\n" +
                $"Owner: {owner.PlayerName}\n\n" +
                "Click MOVE OWNER first."
            );

            return;
        }

        if (owner.CurrentSpaceIndex !=
            property.BoardIndex)
        {
            SetResult(
                "HOUSE BLOCKED\n" +
                "Owner is not standing on the property."
            );

            return;
        }

        int before =
            property.Houses;

        bool success =
            gameManager.BuildHouse(
                property
            );

        int after =
            property.Houses;

        SetResult(
            success
                ? $"HOUSE BUILT\n" +
                  $"{property.SpaceName}\n" +
                  $"{before}/4 → {after}/4"
                : "HOUSE BLOCKED\n" +
                  "The real building rules rejected it."
        );

        RefreshStatus();
    }

    // ============================================================
    // HOTEL
    // ============================================================

    public void AddHotel()
    {
        FindGameManager();

        BoardSpace property =
            SelectedSpace;

        if (property == null ||
            gameManager == null)
        {
            SetResult(
                "HOTEL FAILED\n" +
                "Select a property."
            );

            return;
        }

        BoardPlayer owner =
            property.Owner;

        if (owner == null)
        {
            SetResult(
                "HOTEL FAILED\n" +
                "Property has no owner."
            );

            return;
        }

        if (gameManager.CurrentPlayer != owner ||
            owner.CurrentSpaceIndex != property.BoardIndex)
        {
            SetResult(
                "HOTEL BLOCKED\n" +
                "Owner must be standing on the property."
            );

            return;
        }

        bool before =
            property.HasHotel;

        bool success =
            gameManager.BuildHotel(
                property
            );

        bool after =
            property.HasHotel;

        SetResult(
            success
                ? $"HOTEL BUILT\n" +
                  $"{property.SpaceName}\n" +
                  $"Hotel: {(before ? "YES" : "NO")} → {(after ? "YES" : "NO")}"
                : "HOTEL BLOCKED\n" +
                  "The real building rules rejected it."
        );

        RefreshStatus();
    }

    // ============================================================
    // REAL RENT TEST
    // ============================================================

    public void MovePayerAndTestRent()
    {
        FindGameManager();

        BoardSpace property =
            SelectedSpace;

        BoardPlayer payer =
            SelectedPlayer(
                selectedPayerIndex
            );

        if (property == null ||
            payer == null ||
            gameManager == null)
        {
            SetResult(
                "RENT FAILED\n" +
                "Missing property, payer, or GameManager."
            );

            return;
        }

        if (property.Owner == null)
        {
            SetResult(
                "RENT BLOCKED\n" +
                "Property is owned by the BANK."
            );

            return;
        }

        if (property.Owner == payer)
        {
            SetResult(
                "RENT BLOCKED\n" +
                "Payer cannot be the owner."
            );

            return;
        }

        if (busy)
        {
            SetResult(
                "WAIT\n" +
                "Player is still moving."
            );

            return;
        }

        if (!gameManager.BeginRentTesterTurn(
                payer))
        {
            SetResult(
                "RENT FAILED\n" +
                "Could not make payer the current player."
            );

            return;
        }

        if (property.IsUtility)
        {
            gameManager.SetLastDiceRoll(
                utilityDiceRoll
            );
        }

        int payerBefore =
            payer.Money;

        int ownerBefore =
            property.Owner.Money;

        int rentBefore =
            GetActualRent(
                property
            );

        busy = true;

        SetResult(
            $"MOVING PAYER\n" +
            $"{payer.PlayerName}\n" +
            $"→ {property.SpaceName}\n\n" +
            $"Expected rent: ${rentBefore:N0}M"
        );

        StartCoroutine(
            MovePayerForRent(
                payer,
                property,
                payerBefore,
                ownerBefore
            )
        );
    }

    private IEnumerator MovePayerForRent(
        BoardPlayer payer,
        BoardSpace property,
        int payerBefore,
        int ownerBefore)
    {
        payer.MoveToSpaceForRentTest(
            property.BoardIndex,
            true
        );

        while (payer != null &&
               payer.IsMoving)
        {
            yield return null;
        }

        busy = false;

        int payerAfter =
            payer != null
                ? payer.Money
                : payerBefore;

        int ownerAfter =
            property.Owner != null
                ? property.Owner.Money
                : ownerBefore;

        int paid =
            payerBefore -
            payerAfter;

        int received =
            ownerAfter -
            ownerBefore;

        SetResult(
            $"RENT TEST COMPLETE\n\n" +
            $"{payer.PlayerName} → {property.Owner.PlayerName}\n" +
            $"PAID: ${paid:N0}M\n" +
            $"RECEIVED: ${received:N0}M\n\n" +
            $"TRANSFER: {(paid == received ? "PASS" : "CHECK")}"
        );

        RefreshStatus();
    }

    // ============================================================
    // MOVEMENT HELPERS
    // ============================================================

    private bool PrepareMovement(
        BoardPlayer player,
        BoardSpace property,
        string role)
    {
        FindGameManager();

        if (gameManager == null ||
            player == null ||
            property == null)
        {
            SetResult(
                $"MOVE {role} FAILED\n" +
                "Missing required reference."
            );

            return false;
        }

        if (busy)
        {
            SetResult(
                "WAIT\n" +
                "Another tester movement is running."
            );

            return false;
        }

        if (!gameManager.BeginRentTesterTurn(
                player))
        {
            SetResult(
                $"MOVE {role} FAILED\n" +
                "Could not make player the current player."
            );

            return false;
        }

        return true;
    }

    private IEnumerator WaitForMovement(
        BoardPlayer player,
        string unused)
    {
        while (player != null &&
               player.IsMoving)
        {
            yield return null;
        }

        busy = false;

        RefreshStatus();
    }

    // ============================================================
    // STATUS UI
    // ============================================================

    public void RefreshStatus()
    {
        FindGameManager();

        BoardSpace property =
            SelectedSpace;

        if (propertyText != null)
        {
            propertyText.text =
                BuildPropertyText(
                    property
                );
        }

        if (rentText != null)
        {
            rentText.text =
                BuildRentText(
                    property
                );
        }

        if (setupText != null)
        {
            setupText.text =
                BuildSetupText(
                    property
                );
        }
    }

    private string BuildPropertyText(
        BoardSpace property)
    {
        if (property == null)
            return "NO PROPERTY SELECTED";

        string owner =
            property.Owner != null
                ? property.Owner.PlayerName
                : "BANK";

        return
            $"{property.SpaceName}\n" +
            $"TYPE: {GetTypeName(property)}\n" +
            $"PRICE: ${property.PurchasePrice:N0}M\n" +
            $"OWNER: {owner}\n" +
            $"CURRENT RENT: ${GetActualRent(property):N0}M\n" +
            $"HOUSES: {property.Houses}/4\n" +
            $"HOTEL: {(property.HasHotel ? "YES" : "NO")}\n" +
            $"MORTGAGED: {(property.IsMortgaged ? "YES" : "NO")}";
    }

    private string BuildRentText(
        BoardSpace property)
    {
        if (property == null)
            return "RENT";

        if (property.IsUtility)
        {
            return
                $"UTILITY RENT\n" +
                $"DICE: {utilityDiceRoll}\n" +
                $"CURRENT: " +
                $"${property.GetUtilityRent(utilityDiceRoll):N0}M";
        }

        if (property.IsRailroad)
        {
            return
                "AIRPORT RENT\n" +
                $"CURRENT: ${property.GetRent():N0}M";
        }

        PropertyEconomy economy =
            PropertyEconomy.FromPurchasePrice(
                property.PurchasePrice
            );

        return
            "RENT LEVELS\n" +
            $"BASE: ${economy.baseRent:N0}M\n" +
            $"1 HOUSE: ${economy.houseRent:N0}M\n" +
            $"2 HOUSES: ${economy.twoHouseRent:N0}M\n" +
            $"3 HOUSES: ${economy.threeHouseRent:N0}M\n" +
            $"4 HOUSES: ${economy.fourHouseRent:N0}M\n" +
            $"HOTEL: ${economy.hotelRent:N0}M\n\n" +
            $"CURRENT: ${property.GetRent():N0}M";
    }

    private string BuildSetupText(
        BoardSpace property)
    {
        BoardPlayer buyer =
            SelectedPlayer(
                selectedBuyerIndex
            );

        BoardPlayer payer =
            SelectedPlayer(
                selectedPayerIndex
            );

        BoardPlayer owner =
            property != null
                ? property.Owner
                : SelectedPlayer(
                    selectedOwnerIndex
                );

        BoardPlayer current =
            gameManager != null
                ? gameManager.CurrentPlayer
                : null;

        return
            $"BUYER: {GetPlayerName(buyer)}\n" +
            $"OWNER: {GetPlayerName(owner)}\n" +
            $"PAYER: {GetPlayerName(payer)}\n" +
            $"CURRENT: {GetPlayerName(current)}";
    }

    // ============================================================
    // LIST / PLAYERS
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

        int safe =
            Mathf.Clamp(
                index,
                0,
                gameManager.Players.Length - 1
            );

        return gameManager.Players[safe];
    }

    private int GetPlayerIndex(
        BoardPlayer player)
    {
        if (gameManager == null ||
            gameManager.Players == null)
        {
            return 0;
        }

        for (int i = 0;
             i < gameManager.Players.Length;
             i++)
        {
            if (gameManager.Players[i] == player)
                return i;
        }

        return 0;
    }

    private int FindDifferentPlayer(
        int ownerIndex)
    {
        if (gameManager == null ||
            gameManager.Players == null ||
            gameManager.Players.Length < 2)
        {
            return ownerIndex;
        }

        return
            (ownerIndex + 1) %
            gameManager.Players.Length;
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

    private int GetActualRent(
        BoardSpace property)
    {
        if (property == null)
            return 0;

        if (property.IsUtility)
        {
            return property.GetUtilityRent(
                utilityDiceRoll
            );
        }

        return property.GetRent();
    }

    private string GetTypeName(
        BoardSpace property)
    {
        if (property == null)
            return "UNKNOWN";

        if (property.IsRailroad)
            return "AIRPORT";

        if (property.IsUtility)
            return "UTILITY";

        return "PROPERTY";
    }

    // ============================================================
    // UI CREATION
    // ============================================================

    private void BuildUI()
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
            CreateFixedPanel(
                canvas.transform,
                "RentTesterPanel",
                new Vector2(
                    0.5f,
                    0.5f
                ),
                new Vector2(
                    0.5f,
                    0.5f
                ),
                new Vector2(
                    1250f,
                    760f
                ),
                new Color(
                    0.025f,
                    0.045f,
                    0.075f,
                    0.99f
                )
            );

        BuildHeader();
        BuildPropertyList();
        BuildInformation();
        BuildBottom();
    }

    private void BuildHeader()
    {
        GameObject header =
            CreateAnchoredPanel(
                panel.transform,
                "Header",
                new Vector2(
                    0.02f,
                    0.90f
                ),
                new Vector2(
                    0.98f,
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
                Color.white,
                Vector2.zero,
                Vector2.one
            );

        title.fontStyle =
            FontStyles.Bold;
    }

    private void BuildPropertyList()
    {
        GameObject left =
            CreateAnchoredPanel(
                panel.transform,
                "Properties",
                new Vector2(
                    0.02f,
                    0.12f
                ),
                new Vector2(
                    0.30f,
                    0.88f
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
                0.04f,
                0.92f
            ),
            new Vector2(
                0.96f,
                0.99f
            )
        );

        GameObject viewport =
            new GameObject(
                "Viewport",
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

        viewport.GetComponent<Image>().color =
            new Color(
                0.02f,
                0.035f,
                0.055f,
                1f
            );

        viewport.GetComponent<Mask>()
            .showMaskGraphic =
            true;

        GameObject content =
            new GameObject(
                "Content",
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
                7,
                7,
                7,
                7
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

        content.GetComponent<ContentSizeFitter>()
            .verticalFit =
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

        propertyListContent =
            contentRect;
    }

    private void BuildInformation()
    {
        GameObject right =
            CreateAnchoredPanel(
                panel.transform,
                "Information",
                new Vector2(
                    0.32f,
                    0.12f
                ),
                new Vector2(
                    0.98f,
                    0.88f
                ),
                new Color(
                    0.045f,
                    0.075f,
                    0.12f,
                    1f
                )
            );

        // Property
        GameObject property =
            CreateAnchoredPanel(
                right.transform,
                "PropertyInfo",
                new Vector2(
                    0.02f,
                    0.52f
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
            property.transform,
            "PROPERTY",
            20f,
            TextAlignmentOptions.Center,
            Color.white,
            new Vector2(
                0.05f,
                0.88f
            ),
            new Vector2(
                0.95f,
                0.98f
            )
        );

        propertyText =
            CreateAnchoredText(
                property.transform,
                "",
                18f,
                TextAlignmentOptions.TopLeft,
                Color.white,
                new Vector2(
                    0.08f,
                    0.08f
                ),
                new Vector2(
                    0.92f,
                    0.87f
                )
            );

        // Rent
        GameObject rent =
            CreateAnchoredPanel(
                right.transform,
                "RentInfo",
                new Vector2(
                    0.51f,
                    0.52f
                ),
                new Vector2(
                    0.98f,
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
            rent.transform,
            "RENT",
            20f,
            TextAlignmentOptions.Center,
            Color.white,
            new Vector2(
                0.05f,
                0.88f
            ),
            new Vector2(
                0.95f,
                0.98f
            )
        );

        rentText =
            CreateAnchoredText(
                rent.transform,
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
                    0.87f
                )
            );

        // Setup
        GameObject setup =
            CreateAnchoredPanel(
                right.transform,
                "Setup",
                new Vector2(
                    0.02f,
                    0.05f
                ),
                new Vector2(
                    0.98f,
                    0.48f
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
                0.86f
            ),
            new Vector2(
                0.97f,
                0.98f
            )
        );

        setupText =
            CreateAnchoredText(
                setup.transform,
                "",
                15f,
                TextAlignmentOptions.TopLeft,
                Color.white,
                new Vector2(
                    0.04f,
                    0.43f
                ),
                new Vector2(
                    0.32f,
                    0.82f
                )
            );

        CreateAnchoredButton(
            setup.transform,
            "BUYER →",
            15f,
            new Vector2(
                0.34f,
                0.64f
            ),
            new Vector2(
                0.45f,
                0.81f
            )
        ).onClick.AddListener(
            NextBuyer
        );

        CreateAnchoredButton(
            setup.transform,
            "MOVE BUYER",
            15f,
            new Vector2(
                0.47f,
                0.64f
            ),
            new Vector2(
                0.61f,
                0.81f
            )
        ).onClick.AddListener(
            MoveBuyerHere
        );

        CreateAnchoredButton(
            setup.transform,
            "PURCHASE",
            15f,
            new Vector2(
                0.63f,
                0.64f
            ),
            new Vector2(
                0.76f,
                0.81f
            )
        ).onClick.AddListener(
            PurchaseSelected
        );

        CreateAnchoredButton(
            setup.transform,
            "MOVE OWNER",
            15f,
            new Vector2(
                0.34f,
                0.42f
            ),
            new Vector2(
                0.48f,
                0.59f
            )
        ).onClick.AddListener(
            MoveOwnerHere
        );

        CreateAnchoredButton(
            setup.transform,
            "ADD HOUSE",
            15f,
            new Vector2(
                0.50f,
                0.42f
            ),
            new Vector2(
                0.64f,
                0.59f
            )
        ).onClick.AddListener(
            AddHouse
        );

        CreateAnchoredButton(
            setup.transform,
            "ADD HOTEL",
            15f,
            new Vector2(
                0.66f,
                0.42f
            ),
            new Vector2(
                0.80f,
                0.59f
            )
        ).onClick.AddListener(
            AddHotel
        );

        CreateAnchoredButton(
            setup.transform,
            "OWNER →",
            15f,
            new Vector2(
                0.34f,
                0.20f
            ),
            new Vector2(
                0.45f,
                0.37f
            )
        ).onClick.AddListener(
            NextOwner
        );

        CreateAnchoredButton(
            setup.transform,
            "PAYER →",
            15f,
            new Vector2(
                0.47f,
                0.20f
            ),
            new Vector2(
                0.58f,
                0.37f
            )
        ).onClick.AddListener(
            NextPayer
        );

        CreateAnchoredButton(
            setup.transform,
            "DICE −",
            15f,
            new Vector2(
                0.60f,
                0.20f
            ),
            new Vector2(
                0.69f,
                0.37f
            )
        ).onClick.AddListener(
            DecreaseUtilityDice
        );

        CreateAnchoredButton(
            setup.transform,
            "DICE +",
            15f,
            new Vector2(
                0.71f,
                0.20f
            ),
            new Vector2(
                0.80f,
                0.37f
            )
        ).onClick.AddListener(
            IncreaseUtilityDice
        );

        CreateAnchoredButton(
            setup.transform,
            "RENT → MOVE",
            15f,
            new Vector2(
                0.82f,
                0.20f
            ),
            new Vector2(
                0.97f,
                0.37f
            )
        ).onClick.AddListener(
            MovePayerAndTestRent
        );
    }

    private void BuildBottom()
    {
        GameObject bottom =
            CreateAnchoredPanel(
                panel.transform,
                "Bottom",
                new Vector2(
                    0.02f,
                    0.02f
                ),
                new Vector2(
                    0.98f,
                    0.095f
                ),
                new Color(
                    0.02f,
                    0.035f,
                    0.055f,
                    1f
                )
            );

        resultText =
            CreateAnchoredText(
                bottom.transform,
                "No test yet.",
                15f,
                TextAlignmentOptions.Left,
                Color.white,
                new Vector2(
                    0.01f,
                    0.08f
                ),
                new Vector2(
                    0.78f,
                    0.92f
                )
            );

        CreateAnchoredButton(
            bottom.transform,
            "REFRESH",
            14f,
            new Vector2(
                0.79f,
                0.12f
            ),
            new Vector2(
                0.85f,
                0.88f
            )
        ).onClick.AddListener(
            RefreshProperties
        );

        CreateAnchoredButton(
            bottom.transform,
            "RESET",
            14f,
            new Vector2(
                0.86f,
                0.12f
            ),
            new Vector2(
                0.92f,
                0.88f
            )
        ).onClick.AddListener(
            ResetTestView
        );

        CreateAnchoredButton(
            bottom.transform,
            "F5 CLOSE",
            14f,
            new Vector2(
                0.93f,
                0.12f
            ),
            new Vector2(
                0.99f,
                0.88f
            )
        ).onClick.AddListener(
            () => SetVisible(false)
        );
    }

    // ============================================================
    // UI HELPERS
    // ============================================================

    private GameObject CreateFixedPanel(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 pivot,
        Vector2 size,
        Color color)
    {
        GameObject obj =
            new GameObject(
                name,
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
            anchor;

        rect.anchorMax =
            anchor;

        rect.pivot =
            pivot;

        rect.anchoredPosition =
            Vector2.zero;

        rect.sizeDelta =
            size;

        obj.GetComponent<Image>().color =
            color;

        return obj;
    }

    private GameObject CreateAnchoredPanel(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject obj =
            new GameObject(
                name,
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
        Vector2 anchorMin,
        Vector2 anchorMax)
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

        rect.anchorMin =
            anchorMin;

        rect.anchorMax =
            anchorMax;

        rect.offsetMin =
            Vector2.zero;

        rect.offsetMax =
            Vector2.zero;

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

        button.colors =
            colors;

        TMP_Text text =
            CreateAnchoredText(
                obj.transform,
                label,
                fontSize,
                TextAlignmentOptions.Center,
                Color.white,
                Vector2.zero,
                Vector2.one
            );

        text.fontStyle =
            FontStyles.Bold;

        return button;
    }

    // ============================================================
    // MISSING HELPERS
    // ============================================================

    private void FindGameManager()
    {
        if (gameManager != null)
            return;

        gameManager =
            FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include
            );
    }

    private bool IsVisible()
    {
        return panel != null &&
               panel.activeSelf;
    }

    private void SetVisible(bool visible)
    {
        if (panel == null)
            return;

        panel.SetActive(visible);

        if (visible)
            RefreshStatus();
    }

    private void SetResult(string message)
    {
        if (resultText == null)
            return;

        resultText.text =
            message ?? string.Empty;
    }

    private string GetPlayerName(BoardPlayer player)
    {
        return player != null
            ? player.PlayerName
            : "NONE";
    }

    private void DecreaseUtilityDice()
    {
        utilityDiceRoll =
            Mathf.Clamp(
                utilityDiceRoll - 1,
                2,
                12
            );

        RefreshStatus();
    }

    private void IncreaseUtilityDice()
    {
        utilityDiceRoll =
            Mathf.Clamp(
                utilityDiceRoll + 1,
                2,
                12
            );

        RefreshStatus();
    }

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

    private Button CreateButton(
        Transform parent,
        string label,
        float fontSize,
        float height)
    {
        GameObject buttonObject =
            new GameObject(
                label + "Button",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        buttonObject.transform.SetParent(
            parent,
            false
        );

        LayoutElement layout =
            buttonObject.AddComponent<LayoutElement>();

        layout.preferredHeight =
            height;

        Image image =
            buttonObject.GetComponent<Image>();

        image.color =
            new Color(
                0.10f,
                0.30f,
                0.58f,
                1f
            );

        Button button =
            buttonObject.GetComponent<Button>();

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

        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform)
            );

        textObject.transform.SetParent(
            buttonObject.transform,
            false
        );

        TextMeshProUGUI text =
            textObject.AddComponent<TextMeshProUGUI>();

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

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget =
            false;

        RectTransform textRect =
            text.rectTransform;

        textRect.anchorMin =
            Vector2.zero;

        textRect.anchorMax =
            Vector2.one;

        textRect.offsetMin =
            Vector2.zero;

        textRect.offsetMax =
            Vector2.zero;

        return button;
    }

    private void ResetTestView()
    {
        selectedPropertyIndex = 0;
        selectedBuyerIndex = 0;
        selectedOwnerIndex = 0;
        selectedPayerIndex = 0;

        utilityDiceRoll = 7;
        busy = false;

        SetResult(
            "RESET\n" +
            "Tester view reset."
        );

        RefreshStatus();
    }

}

#endif