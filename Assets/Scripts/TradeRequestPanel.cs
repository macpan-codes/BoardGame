using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Incoming trade request UI for the receiving player.
/// </summary>
public class TradeRequestPanel : MonoBehaviour
{
    [Header("Panel")]
    [SerializeField] private GameObject panel;

    [Header("Text")]
    [SerializeField] private TMP_Text headerText;
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private TMP_Text offeredText;
    [SerializeField] private TMP_Text requestedText;
    [SerializeField] private TMP_Text statusText;

    [Header("Buttons")]
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button declineButton;
    [SerializeField] private Button closeButton;

    private TradeManager tradeManager;
    private TradeOffer currentOffer;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        tradeManager =
            FindFirstObjectByType<TradeManager>();

        WireButtons();
        Hide();
    }

    private void OnEnable()
    {
        Subscribe(true);
    }

    private void OnDisable()
    {
        Subscribe(false);
    }

    // ============================================================
    // EVENTS
    // ============================================================

    private void Subscribe(bool subscribe)
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
            tradeManager.OnOfferReceived += HandleOfferReceived;
            tradeManager.OnOfferCompleted += HandleOfferClosed;
            tradeManager.OnOfferDeclined += HandleOfferClosed;
            tradeManager.OnOfferCancelled += HandleOfferClosed;
            tradeManager.OnOfferFailed += HandleOfferFailed;
        }
        else
        {
            tradeManager.OnOfferReceived -= HandleOfferReceived;
            tradeManager.OnOfferCompleted -= HandleOfferClosed;
            tradeManager.OnOfferDeclined -= HandleOfferClosed;
            tradeManager.OnOfferCancelled -= HandleOfferClosed;
            tradeManager.OnOfferFailed -= HandleOfferFailed;
        }
    }

    private void HandleOfferReceived(TradeOffer offer)
    {
        ShowOffer(offer);
    }

    private void HandleOfferClosed(TradeOffer offer)
    {
        Hide();
    }

    private void HandleOfferFailed(
        TradeOffer offer,
        string error)
    {
        if (statusText != null)
        {
            statusText.text = error;
        }

        if (acceptButton != null)
        {
            acceptButton.interactable = false;
        }
    }

    // ============================================================
    // BUTTONS
    // ============================================================

    private void WireButtons()
    {
        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(AcceptTrade);
        }

        if (declineButton != null)
        {
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(DeclineTrade);
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(DeclineTrade);
        }
    }

    // ============================================================
    // SHOW / HIDE
    // ============================================================

    private void ShowOffer(TradeOffer offer)
    {
        if (offer == null)
        {
            Hide();
            return;
        }

        currentOffer = offer;

        if (panel != null)
            panel.SetActive(true);
        else
            gameObject.SetActive(true);

        Refresh();
    }

    public void Hide()
    {
        currentOffer = null;

        if (panel != null)
            panel.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    private void Refresh()
    {
        if (currentOffer == null)
            return;

        BoardPlayer proposer =
            currentOffer.Proposer;

        BoardPlayer receiver =
            currentOffer.Receiver;

        if (headerText != null)
        {
            headerText.text = "TRADE REQUEST";
        }

        if (messageText != null &&
            proposer != null &&
            receiver != null)
        {
            messageText.text =
                $"{proposer.PlayerName} wants to trade with " +
                $"{receiver.PlayerName}.";
        }

        if (offeredText != null &&
            proposer != null)
        {
            offeredText.text =
                $"{proposer.PlayerName.ToUpperInvariant()} OFFERS:\n" +
                BuildAssetList(
                    currentOffer.OfferedMoney,
                    currentOffer.OfferedProperties
                );
        }

        if (requestedText != null &&
            receiver != null)
        {
            requestedText.text =
                $"{proposer.PlayerName.ToUpperInvariant()} REQUESTS:\n" +
                BuildAssetList(
                    currentOffer.RequestedMoney,
                    currentOffer.RequestedProperties
                );
        }

        if (statusText != null)
        {
            statusText.text =
                "Review the offer, then accept or decline.";
        }

        if (acceptButton != null)
        {
            acceptButton.interactable =
                tradeManager != null &&
                tradeManager.ValidateOffer(
                    currentOffer,
                    out _
                );
        }
    }

    private string BuildAssetList(
        long money,
        System.Collections.Generic.List<BoardSpace> properties)
    {
        System.Text.StringBuilder builder =
            new System.Text.StringBuilder();

        bool hasAssets = false;

        if (money > 0)
        {
            builder.AppendLine(
                TradeManager.FormatMoney(money)
            );

            hasAssets = true;
        }

        if (properties != null)
        {
            foreach (BoardSpace property in properties)
            {
                builder.AppendLine(
                    TradeManager.DescribeProperty(property)
                );

                hasAssets = true;
            }
        }

        if (!hasAssets)
        {
            builder.AppendLine("Nothing");
        }

        return builder.ToString().TrimEnd();
    }

    // ============================================================
    // ACTIONS
    // ============================================================

    private void AcceptTrade()
    {
        if (tradeManager == null)
        {
            tradeManager =
                FindFirstObjectByType<TradeManager>();
        }

        if (tradeManager == null ||
            currentOffer == null)
        {
            return;
        }

        if (!tradeManager.AcceptOffer())
        {
            Refresh();
        }
    }

    private void DeclineTrade()
    {
        if (tradeManager == null)
        {
            tradeManager =
                FindFirstObjectByType<TradeManager>();
        }

        if (tradeManager == null)
            return;

        tradeManager.DeclineOffer();
        Hide();
    }
}
