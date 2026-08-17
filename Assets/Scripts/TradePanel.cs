using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Outgoing trade UI: player selection, offer building, and send.
/// Does not modify turn state.
/// </summary>
public class TradePanel : MonoBehaviour
{
    private enum TradeView
    {
        PlayerSelection,
        OfferBuilder,
        PropertyPicker
    }

    // ============================================================
    // PANEL
    // ============================================================

    [Header("Panel")]
    [SerializeField] private GameObject panel;
    [SerializeField] private GameObject playerSelectionSection;
    [SerializeField] private GameObject offerBuilderSection;
    [SerializeField] private GameObject propertyPickerSection;

    // ============================================================
    // PLAYER INFO
    // ============================================================

    [Header("Player Info")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text currentPlayerText;
    [SerializeField] private TMP_Text selectedPlayerText;
    [SerializeField] private TMP_Text statusText;

    // ============================================================
    // PLAYER SELECTION
    // ============================================================

    [Header("Player Selection")]
    [SerializeField] private Transform playerButtonContainer;
    [SerializeField] private Button playerButtonPrefab;

    [Header("Offer Builder")]
    [SerializeField] private TMP_Text offerProposerLabelText;
    [SerializeField] private TMP_Text offerReceiverLabelText;
    [SerializeField] private TMP_InputField offeredMoneyInput;
    [SerializeField] private TMP_InputField requestedMoneyInput;
    [SerializeField] private Button offeredMoneyMinusButton;
    [SerializeField] private Button offeredMoneyPlusButton;
    [SerializeField] private Button requestedMoneyMinusButton;
    [SerializeField] private Button requestedMoneyPlusButton;
    [SerializeField] private Transform offeredPropertyChipContainer;
    [SerializeField] private Transform requestedPropertyChipContainer;
    [SerializeField] private Button selectOfferedPropertyButton;
    [SerializeField] private Button selectRequestedPropertyButton;
    [SerializeField] private TMP_Text summaryText;
    [SerializeField] private Transform propertyPickerListContainer;
    [SerializeField] private TMP_Text propertyPickerTitleText;
    [SerializeField] private Button propertyPickerConfirmButton;
    [SerializeField] private Button propertyPickerCancelButton;

    // ============================================================
    // BUTTONS
    // ============================================================

    [Header("Buttons")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Button sendTradeButton;
    [SerializeField] private Button closeButton;

    [Header("Prefabs")]
    [SerializeField] private Button propertyChipPrefab;
    [SerializeField] private Toggle propertyPickerTogglePrefab;

    // ============================================================
    // STATE
    // ============================================================

    private GameManager gameManager;
    private TradeManager tradeManager;

    private BoardPlayer currentPlayer;
    private BoardPlayer selectedPlayer;

    private TradeView currentView =
        TradeView.PlayerSelection;

    private bool pickingOfferedProperties;
    private readonly List<BoardSpace> draftOfferedProperties =
        new List<BoardSpace>();
    private readonly List<BoardSpace> draftRequestedProperties =
        new List<BoardSpace>();
    private readonly Dictionary<BoardSpace, Toggle> pickerToggles =
        new Dictionary<BoardSpace, Toggle>();

    private const int MoneyStep = 50;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        gameManager =
            FindFirstObjectByType<GameManager>();

        tradeManager =
            FindFirstObjectByType<TradeManager>();

        WireButtons();
        Hide();
    }

    private void OnEnable()
    {
        SubscribeTradeEvents(true);
    }

    private void OnDisable()
    {
        SubscribeTradeEvents(false);
    }

    // ============================================================
    // EVENTS
    // ============================================================

    private void SubscribeTradeEvents(bool subscribe)
    {
        if (tradeManager == null)
        {
            tradeManager =
                FindFirstObjectByType<TradeManager>();
        }

        if (tradeManager == null)
            return;

        if (subscribe)
        {
            tradeManager.OnOfferSent += HandleOfferSent;
            tradeManager.OnOfferCompleted += HandleTradeFinished;
            tradeManager.OnOfferDeclined += HandleTradeFinished;
            tradeManager.OnOfferCancelled += HandleTradeFinished;
            tradeManager.OnOfferFailed += HandleOfferFailed;
        }
        else
        {
            tradeManager.OnOfferSent -= HandleOfferSent;
            tradeManager.OnOfferCompleted -= HandleTradeFinished;
            tradeManager.OnOfferDeclined -= HandleTradeFinished;
            tradeManager.OnOfferCancelled -= HandleTradeFinished;
            tradeManager.OnOfferFailed -= HandleOfferFailed;
        }
    }

    private void HandleOfferSent(TradeOffer offer)
    {
        Hide();
    }

    private void HandleTradeFinished(TradeOffer offer)
    {
        if (panel != null &&
            panel.activeSelf)
        {
            Refresh();
        }
    }

    private void HandleOfferFailed(
        TradeOffer offer,
        string error)
    {
        if (statusText != null &&
            panel != null &&
            panel.activeSelf)
        {
            statusText.text = error;
        }
    }

    // ============================================================
    // BUTTON WIRING
    // ============================================================

    private void WireButtons()
    {
        BindButton(
            continueButton,
            ContinueToOfferBuilder
        );

        BindButton(
            backButton,
            BackToPlayerSelection
        );

        BindButton(
            sendTradeButton,
            SendTrade
        );

        BindButton(
            closeButton,
            Hide
        );

        BindButton(
            selectOfferedPropertyButton,
            () => OpenPropertyPicker(true)
        );

        BindButton(
            selectRequestedPropertyButton,
            () => OpenPropertyPicker(false)
        );

        BindButton(
            propertyPickerConfirmButton,
            ConfirmPropertyPicker
        );

        BindButton(
            propertyPickerCancelButton,
            CancelPropertyPicker
        );

        BindButton(
            offeredMoneyMinusButton,
            () => AdjustMoney(true, -MoneyStep)
        );

        BindButton(
            offeredMoneyPlusButton,
            () => AdjustMoney(true, MoneyStep)
        );

        BindButton(
            requestedMoneyMinusButton,
            () => AdjustMoney(false, -MoneyStep)
        );

        BindButton(
            requestedMoneyPlusButton,
            () => AdjustMoney(false, MoneyStep)
        );

        if (offeredMoneyInput != null)
        {
            offeredMoneyInput.onValueChanged.RemoveAllListeners();
            offeredMoneyInput.onValueChanged.AddListener(
                _ => RefreshOfferBuilder()
            );

            offeredMoneyInput.onEndEdit.RemoveAllListeners();
            offeredMoneyInput.onEndEdit.AddListener(
                _ => ClampMoneyInput(true)
            );
        }

        if (requestedMoneyInput != null)
        {
            requestedMoneyInput.onValueChanged.RemoveAllListeners();
            requestedMoneyInput.onValueChanged.AddListener(
                _ => RefreshOfferBuilder()
            );

            requestedMoneyInput.onEndEdit.RemoveAllListeners();
            requestedMoneyInput.onEndEdit.AddListener(
                _ => ClampMoneyInput(false)
            );
        }
    }

    private void BindButton(
        Button button,
        UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    // ============================================================
    // SHOW / HIDE
    // ============================================================

    public void Show()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }

        if (tradeManager == null)
        {
            tradeManager =
                FindFirstObjectByType<TradeManager>();
        }

        if (gameManager == null)
        {
            Debug.LogError(
                "TradePanel: GameManager not found."
            );

            return;
        }

        if (gameManager.GameOver)
        {
            Hide();
            return;
        }

        currentPlayer =
            gameManager.CurrentPlayer;

        if (currentPlayer == null)
        {
            Hide();
            return;
        }

        if (tradeManager != null &&
            !tradeManager.CanOpenTrade(currentPlayer))
        {
            GameNotificationUI.Show(
                "TRADING IS NOT AVAILABLE RIGHT NOW"
            );

            Hide();
            return;
        }

        if (currentPlayer.IsBankrupt)
        {
            Hide();
            return;
        }

        selectedPlayer = null;
        ResetDraftOffer();
        currentView = TradeView.PlayerSelection;

        if (panel != null)
            panel.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        selectedPlayer = null;
        currentPlayer = null;
        currentView = TradeView.PlayerSelection;

        ClearPlayerButtons();
        ClearPropertyChips();
        ClearPropertyPicker();

        if (panel != null)
            panel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void RefreshAfterTrade()
    {
        if (panel == null ||
            !panel.activeSelf)
        {
            return;
        }

        Refresh();
    }

    // ============================================================
    // REFRESH
    // ============================================================

    private void Refresh()
    {
        UpdateViewVisibility();

        if (headerText != null)
        {
            headerText.text =
                currentView == TradeView.PropertyPicker
                    ? "SELECT PROPERTIES"
                    : "TRADE";
        }

        switch (currentView)
        {
            case TradeView.PlayerSelection:
                RefreshPlayerSelection();
                break;

            case TradeView.OfferBuilder:
                RefreshOfferBuilder();
                break;

            case TradeView.PropertyPicker:
                RefreshPropertyPicker();
                break;
        }
    }

    private void UpdateViewVisibility()
    {
        if (playerSelectionSection != null)
        {
            playerSelectionSection.SetActive(
                currentView == TradeView.PlayerSelection
            );
        }

        if (offerBuilderSection != null)
        {
            offerBuilderSection.SetActive(
                currentView == TradeView.OfferBuilder
            );
        }

        if (propertyPickerSection != null)
        {
            propertyPickerSection.SetActive(
                currentView == TradeView.PropertyPicker
            );
        }
    }

    private void RefreshPlayerSelection()
    {
        if (currentPlayerText != null &&
            currentPlayer != null)
        {
            currentPlayerText.text =
                $"Trading as: {currentPlayer.PlayerName}\n" +
                $"Balance: {TradeManager.FormatMoney(currentPlayer.Money)}";
        }

        if (selectedPlayerText != null)
        {
            selectedPlayerText.text =
                selectedPlayer != null
                    ? $"Trade with: {selectedPlayer.PlayerName}\n" +
                      $"Balance: {TradeManager.FormatMoney(selectedPlayer.Money)}"
                    : "Select a player";
        }

        if (statusText != null)
        {
            statusText.text =
                selectedPlayer != null
                    ? $"Ready to build an offer for {selectedPlayer.PlayerName}."
                    : "Choose a player to trade with.";
        }

        if (continueButton != null)
        {
            continueButton.interactable =
                selectedPlayer != null;
        }

        RebuildPlayerButtons();
    }

    private void RefreshOfferBuilder()
    {
        if (currentPlayer == null ||
            selectedPlayer == null)
        {
            return;
        }

        if (offerProposerLabelText != null)
        {
            offerProposerLabelText.text =
                $"{currentPlayer.PlayerName.ToUpperInvariant()} OFFERS";
        }

        if (offerReceiverLabelText != null)
        {
            offerReceiverLabelText.text =
                $"{selectedPlayer.PlayerName.ToUpperInvariant()} GIVES";
        }

        if (statusText != null)
        {
            statusText.text =
                "Build your trade offer, then send it.";
        }

        RebuildPropertyChips(
            draftOfferedProperties,
            offeredPropertyChipContainer,
            true
        );

        RebuildPropertyChips(
            draftRequestedProperties,
            requestedPropertyChipContainer,
            false
        );

        UpdateSummary();
        UpdateSendButtonState();
    }

    private void UpdateSummary()
    {
        if (summaryText == null ||
            currentPlayer == null ||
            selectedPlayer == null)
        {
            return;
        }

        long offeredMoney =
            ParseMoneyInput(offeredMoneyInput);

        long requestedMoney =
            ParseMoneyInput(requestedMoneyInput);

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        builder.AppendLine("TRADE SUMMARY");
        builder.AppendLine();
        builder.AppendLine(
            $"{currentPlayer.PlayerName.ToUpperInvariant()} GIVES:"
        );

        AppendAssetLines(
            builder,
            offeredMoney,
            draftOfferedProperties
        );

        builder.AppendLine();
        builder.AppendLine(
            $"{selectedPlayer.PlayerName.ToUpperInvariant()} GIVES:"
        );

        AppendAssetLines(
            builder,
            requestedMoney,
            draftRequestedProperties
        );

        summaryText.text = builder.ToString();
    }

    private void AppendAssetLines(
        System.Text.StringBuilder builder,
        long money,
        List<BoardSpace> properties)
    {
        bool hasAssets = false;

        if (money > 0)
        {
            builder.AppendLine(
                TradeManager.FormatMoney(money)
            );

            hasAssets = true;
        }

        foreach (BoardSpace property in properties)
        {
            builder.AppendLine(
                TradeManager.DescribeProperty(property)
            );

            hasAssets = true;
        }

        if (!hasAssets)
        {
            builder.AppendLine("Nothing");
        }
    }

    private void UpdateSendButtonState()
    {
        if (sendTradeButton == null)
            return;

        TradeOffer draft = BuildDraftOffer();

        bool valid =
            draft != null &&
            draft.HasAnyAssets &&
            (tradeManager == null ||
             tradeManager.ValidateOffer(
                 draft,
                 out _));

        sendTradeButton.interactable = valid;
    }

    // ============================================================
    // PLAYER SELECTION
    // ============================================================

    private void RebuildPlayerButtons()
    {
        ClearPlayerButtons();

        if (playerButtonContainer == null ||
            playerButtonPrefab == null ||
            gameManager == null ||
            currentPlayer == null)
        {
            return;
        }

        foreach (BoardPlayer player in gameManager.Players)
        {
            if (player == null)
                continue;

            if (player == currentPlayer)
                continue;

            if (player.IsBankrupt)
                continue;

            Button button =
                Instantiate(
                    playerButtonPrefab,
                    playerButtonContainer
                );

            TMP_Text text =
                button.GetComponentInChildren<TMP_Text>(
                    true
                );

            if (text != null)
            {
                text.text =
                    $"{player.PlayerName}\n" +
                    TradeManager.FormatMoney(player.Money);
            }

            BoardPlayer targetPlayer = player;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(
                () => SelectPlayer(targetPlayer)
            );
        }
    }

    private void SelectPlayer(BoardPlayer player)
    {
        if (player == null ||
            currentPlayer == null)
        {
            return;
        }

        if (tradeManager != null &&
            !tradeManager.CanTradeWith(
                currentPlayer,
                player))
        {
            return;
        }

        selectedPlayer = player;

        GameNotificationUI.Show(
            $"{currentPlayer.PlayerName.ToUpperInvariant()} " +
            $"SELECTED {selectedPlayer.PlayerName.ToUpperInvariant()} " +
            "FOR TRADE"
        );

        Refresh();
    }

    private void ContinueToOfferBuilder()
    {
        if (currentPlayer == null ||
            selectedPlayer == null)
        {
            return;
        }

        ResetDraftOffer();
        currentView = TradeView.OfferBuilder;
        Refresh();
    }

    private void BackToPlayerSelection()
    {
        currentView = TradeView.PlayerSelection;
        Refresh();
    }

    // ============================================================
    // MONEY
    // ============================================================

    private void AdjustMoney(
        bool offeredSide,
        int delta)
    {
        TMP_InputField input =
            offeredSide
                ? offeredMoneyInput
                : requestedMoneyInput;

        BoardPlayer owner =
            offeredSide
                ? currentPlayer
                : selectedPlayer;

        long current =
            ParseMoneyInput(input);

        long next =
            current + delta;

        if (next < 0)
            next = 0;

        if (owner != null &&
            next > owner.Money)
        {
            next = owner.Money;
        }

        SetMoneyInput(input, next);
        RefreshOfferBuilder();
    }

    private void ClampMoneyInput(bool offeredSide)
    {
        TMP_InputField input =
            offeredSide
                ? offeredMoneyInput
                : requestedMoneyInput;

        BoardPlayer owner =
            offeredSide
                ? currentPlayer
                : selectedPlayer;

        long amount =
            ParseMoneyInput(input);

        if (amount < 0)
            amount = 0;

        if (owner != null &&
            amount > owner.Money)
        {
            amount = owner.Money;
        }

        SetMoneyInput(input, amount);
        RefreshOfferBuilder();
    }

    private long ParseMoneyInput(
        TMP_InputField input)
    {
        if (input == null ||
            string.IsNullOrWhiteSpace(input.text))
        {
            return 0;
        }

        string cleaned =
            input.text
                .Replace("$", "")
                .Replace("M", "")
                .Replace(",", "")
                .Trim();

        if (long.TryParse(
                cleaned,
                out long value))
        {
            return Mathf.Max(0, (int)value);
        }

        return 0;
    }

    private void SetMoneyInput(
        TMP_InputField input,
        long amount)
    {
        if (input == null)
            return;

        input.SetTextWithoutNotify(
            amount > 0
                ? amount.ToString("N0")
                : "0"
        );
    }

    // ============================================================
    // PROPERTY CHIPS
    // ============================================================

    private void RebuildPropertyChips(
        List<BoardSpace> properties,
        Transform container,
        bool offeredSide)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }

        if (propertyChipPrefab == null)
            return;

        foreach (BoardSpace property in properties)
        {
            Button chip =
                Instantiate(
                    propertyChipPrefab,
                    container
                );

            TMP_Text text =
                chip.GetComponentInChildren<TMP_Text>(
                    true
                );

            if (text != null)
            {
                text.text =
                    TradeManager.DescribeProperty(property) +
                    "   ×";
            }

            BoardSpace captured = property;

            chip.onClick.RemoveAllListeners();
            chip.onClick.AddListener(
                () => RemovePropertyFromDraft(
                    captured,
                    offeredSide
                )
            );
        }
    }

    private void RemovePropertyFromDraft(
        BoardSpace property,
        bool offeredSide)
    {
        List<BoardSpace> list =
            offeredSide
                ? draftOfferedProperties
                : draftRequestedProperties;

        list.Remove(property);
        RefreshOfferBuilder();
    }

    private void ClearPropertyChips()
    {
        ClearContainer(offeredPropertyChipContainer);
        ClearContainer(requestedPropertyChipContainer);
    }

    // ============================================================
    // PROPERTY PICKER
    // ============================================================

    private void OpenPropertyPicker(bool offeredSide)
    {
        pickingOfferedProperties = offeredSide;
        currentView = TradeView.PropertyPicker;
        Refresh();
    }

    private void RefreshPropertyPicker()
    {
        ClearPropertyPicker();

        if (propertyPickerListContainer == null ||
            propertyPickerTogglePrefab == null)
        {
            return;
        }

        BoardPlayer owner =
            pickingOfferedProperties
                ? currentPlayer
                : selectedPlayer;

        List<BoardSpace> selectedList =
            pickingOfferedProperties
                ? draftOfferedProperties
                : draftRequestedProperties;

        if (propertyPickerTitleText != null &&
            owner != null)
        {
            propertyPickerTitleText.text =
                pickingOfferedProperties
                    ? $"Select properties offered by {owner.PlayerName}"
                    : $"Select properties requested from {owner.PlayerName}";
        }

        BoardSpace[] properties =
            TradeManager.GetTradablePropertiesFor(owner);

        foreach (BoardSpace property in properties)
        {
            Toggle toggle =
                Instantiate(
                    propertyPickerTogglePrefab,
                    propertyPickerListContainer
                );

            TMP_Text label =
                toggle.GetComponentInChildren<TMP_Text>(
                    true
                );

            if (label != null)
            {
                label.text =
                    BuildPropertyPickerLabel(property);
            }

            bool isSelected =
                selectedList.Contains(property);

            toggle.SetIsOnWithoutNotify(isSelected);
            pickerToggles[property] = toggle;
        }
    }

    private string BuildPropertyPickerLabel(
        BoardSpace property)
    {
        if (property == null)
            return "Unknown";

        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        builder.Append(property.SpaceName);

        if (property.Owner != null)
        {
            builder.Append(" | ");
            builder.Append(property.Owner.PlayerName);
        }

        builder.Append(" | ");
        builder.Append(
            TradeManager.FormatMoney(
                property.PurchasePrice
            )
        );

        if (property.HasHotel)
        {
            builder.Append(" | Hotel");
        }
        else if (property.Houses > 0)
        {
            builder.Append(" | ");
            builder.Append(property.Houses);
            builder.Append(
                property.Houses == 1
                    ? " House"
                    : " Houses"
            );
        }

        if (property.IsMortgaged)
        {
            builder.Append(" | Mortgaged");
        }

        return builder.ToString();
    }

    private void ConfirmPropertyPicker()
    {
        List<BoardSpace> targetList =
            pickingOfferedProperties
                ? draftOfferedProperties
                : draftRequestedProperties;

        targetList.Clear();

        foreach (KeyValuePair<BoardSpace, Toggle> pair in pickerToggles)
        {
            if (pair.Key == null ||
                pair.Value == null)
            {
                continue;
            }

            if (pair.Value.isOn)
            {
                targetList.Add(pair.Key);
            }
        }

        currentView = TradeView.OfferBuilder;
        Refresh();
    }

    private void CancelPropertyPicker()
    {
        currentView = TradeView.OfferBuilder;
        Refresh();
    }

    private void ClearPropertyPicker()
    {
        pickerToggles.Clear();
        ClearContainer(propertyPickerListContainer);
    }

    // ============================================================
    // SEND TRADE
    // ============================================================

    private void SendTrade()
    {
        if (tradeManager == null)
        {
            tradeManager =
                FindFirstObjectByType<TradeManager>();

            if (tradeManager == null)
            {
                Debug.LogError(
                    "TradePanel: TradeManager not found."
                );

                return;
            }
        }

        TradeOffer offer = BuildDraftOffer();

        if (offer == null)
            return;

        if (!tradeManager.ValidateOffer(
                offer,
                out string error))
        {
            if (statusText != null)
            {
                statusText.text = error;
            }

            GameNotificationUI.Show(
                "TRADE CANNOT BE SENT"
            );

            return;
        }

        if (!tradeManager.SendOffer(offer))
        {
            if (statusText != null &&
                !string.IsNullOrWhiteSpace(
                    offer.FailureReason))
            {
                statusText.text =
                    offer.FailureReason;
            }

            return;
        }
    }

    private TradeOffer BuildDraftOffer()
    {
        if (currentPlayer == null ||
            selectedPlayer == null)
        {
            return null;
        }

        TradeOffer offer = new TradeOffer
        {
            Proposer = currentPlayer,
            Receiver = selectedPlayer,
            OfferedMoney =
                ParseMoneyInput(offeredMoneyInput),
            RequestedMoney =
                ParseMoneyInput(requestedMoneyInput)
        };

        offer.OfferedProperties.AddRange(
            draftOfferedProperties
        );

        offer.RequestedProperties.AddRange(
            draftRequestedProperties
        );

        return offer;
    }

    private void ResetDraftOffer()
    {
        draftOfferedProperties.Clear();
        draftRequestedProperties.Clear();

        SetMoneyInput(offeredMoneyInput, 0);
        SetMoneyInput(requestedMoneyInput, 0);
    }

    // ============================================================
    // CLEAR HELPERS
    // ============================================================

    private void ClearPlayerButtons()
    {
        ClearContainer(playerButtonContainer);
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
            return;

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }

    // ============================================================
    // PUBLIC ACCESS
    // ============================================================

    public BoardPlayer SelectedPlayer =>
        selectedPlayer;

    public BoardPlayer CurrentPlayer =>
        currentPlayer;
}
