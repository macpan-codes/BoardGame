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
    private CanvasGroup visibilityGroup;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        tradeManager =
            FindFirstObjectByType<TradeManager>();

        EnsureVisibilityGroup();
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

    /// <summary>
    /// Ensures this controller is active and subscribed before TradeManager
    /// raises OnOfferReceived. The popup remains visually hidden until the event.
    /// </summary>
    public bool PrepareForOfferDelivery()
    {
        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        if (!enabled)
            enabled = true;

        if (!gameObject.activeInHierarchy || !isActiveAndEnabled)
            return false;

        if (tradeManager == null)
        {
            tradeManager =
                FindFirstObjectByType<TradeManager>(
                    FindObjectsInactive.Include
                );
        }

        EnsureVisibilityGroup();
        SetVisible(false);
        return true;
    }

    private void HandleOfferClosed(TradeOffer offer)
    {
        Hide();
    }

    private void HandleOfferFailed(
        TradeOffer offer,
        string error)
    {
        if (currentOffer == null || offer != currentOffer)
            return;

        ShowFailureAndAllowDismiss(error);
    }

    private void ShowFailureAndAllowDismiss(string message)
    {
        if (statusText != null)
            statusText.text = message;

        if (acceptButton != null)
            acceptButton.interactable = false;

        if (declineButton != null)
        {
            declineButton.interactable = true;
            declineButton.onClick.RemoveAllListeners();
            declineButton.onClick.AddListener(Hide);

            TMP_Text label =
                declineButton.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.text = "CLOSE";
        }
    }

    private void RestoreDeclineAction()
    {
        if (declineButton == null)
            return;

        declineButton.interactable = true;
        declineButton.onClick.RemoveAllListeners();
        declineButton.onClick.AddListener(DeclineTrade);

        TMP_Text label =
            declineButton.GetComponentInChildren<TMP_Text>(true);

        if (label != null)
            label.text = "DECLINE";
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

        // The old CLOSE button performed a decline anyway. Hide it so the
        // recipient has only explicit ACCEPT and DECLINE actions.
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.gameObject.SetActive(false);
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

        if (!gameObject.activeSelf)
            gameObject.SetActive(true);

        currentOffer = offer;
        RestoreDeclineAction();
        SetVisible(true);
        Refresh();
    }

    public void Hide()
    {
        currentOffer = null;
        SetVisible(false);
    }

    private void EnsureVisibilityGroup()
    {
        if (visibilityGroup != null)
            return;

        visibilityGroup = GetComponent<CanvasGroup>();

        if (visibilityGroup == null)
            visibilityGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private void SetVisible(bool visible)
    {
        EnsureVisibilityGroup();

        // Keep this GameObject enabled so OnEnable remains subscribed to
        // trade events. CanvasGroup hides it and releases raycasts instead.
        if (panel != null && panel != gameObject)
            panel.SetActive(visible);

        visibilityGroup.alpha = visible ? 1f : 0f;
        visibilityGroup.interactable = visible;
        visibilityGroup.blocksRaycasts = visible;
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
                $"HAND THE CONTROLS TO {receiver.PlayerName.ToUpperInvariant()}.\n" +
                $"{proposer.PlayerName} wants to trade with {receiver.PlayerName}.";
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
                $"{receiver.PlayerName.ToUpperInvariant()} GIVES:\n" +
                BuildAssetList(
                    currentOffer.RequestedMoney,
                    currentOffer.RequestedProperties
                );
        }

        if (statusText != null)
        {
            statusText.text =
                $"Review the offer. {receiver.PlayerName}, accept or decline.";
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

        if (!tradeManager.AcceptOffer(currentOffer.Receiver, currentOffer))
        {
            if (tradeManager.HasPendingOffer &&
                tradeManager.PendingOffer == currentOffer)
            {
                Refresh();
            }
            else
            {
                ShowFailureAndAllowDismiss(
                    !string.IsNullOrWhiteSpace(currentOffer.FailureReason)
                        ? currentOffer.FailureReason
                        : "This trade request is no longer available."
                );
            }
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

        tradeManager.DeclineOffer(
            currentOffer != null ? currentOffer.Receiver : null,
            currentOffer
        );
        Hide();
    }
}
