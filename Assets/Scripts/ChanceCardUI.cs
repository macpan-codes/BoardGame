using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ChanceCardUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popup;
    [SerializeField] private RectTransform cardTransform;
    [SerializeField] private CanvasGroup popupCanvasGroup;


    [Header("Coroutine Runner")]
    [SerializeField] private ChanceCoroutineRunner coroutineRunner;


    [Header("Header")]
    [SerializeField] private TMP_Text chanceLabel;
    [SerializeField] private Button closeButtonTop;

    [Header("Main Card")]
    [SerializeField] private Image cardIcon;
    [SerializeField] private TMP_Text cardTitle;
    [SerializeField] private TMP_Text cardDescription;

    [Header("Effect Area")]
    [SerializeField] private GameObject effectArea;
    [SerializeField] private GameObject moneyEffect;
    [SerializeField] private GameObject conditionEffect;
    [SerializeField] private GameObject propertyEffect;
    [SerializeField] private GameObject movementEffect;
    [SerializeField] private GameObject playerEffect;
    [SerializeField] private GameObject specialEffect;

    [Header("Money Effect")]
    [SerializeField] private TMP_Text moneyEffectLabel;
    [SerializeField] private Image moneyIcon;
    [SerializeField] private TMP_Text moneyAmountText;
    [SerializeField] private TMP_Text moneySourceText;
    [SerializeField] private TMP_Text moneyDetailText;
    [SerializeField] private TMP_Text moneyBreakdownText;

    [Header("Condition Effect")]
    [SerializeField] private TMP_Text conditionHeader;
    [SerializeField] private Image conditionIcon;
    [SerializeField] private TMP_Text conditionText;
    [SerializeField] private TMP_Text conditionDetailsText;
    [SerializeField] private TMP_Text conditionStatusText;
    [SerializeField] private TMP_Text conditionResultText;

    [Header("Property Effect")]
    [SerializeField] private TMP_Text propertyEffectHeader;
    [SerializeField] private Image propertyIcon;
    [SerializeField] private TMP_Text propertyNameText;
    [SerializeField] private TMP_Text propertyInfoText;
    [SerializeField] private TMP_Text propertyActionText;
    [SerializeField] private TMP_Text propertyTargetText;
    [SerializeField] private Button selectPropertyButton;
    [SerializeField] private TMP_Text propertyResultText;

    [Header("Movement Effect")]
    [SerializeField] private TMP_Text movementEffectHeader;
    [SerializeField] private Image movementIcon;
    [SerializeField] private TMP_Text currentPositionText;
    [SerializeField] private Image movementArrow;
    [SerializeField] private TMP_Text destinationText;
    [SerializeField] private TMP_Text movementDistanceText;
    [SerializeField] private TMP_Text movementRuleText;

    [Header("Player Effect")]
    [SerializeField] private TMP_Text playerEffectHeader;
    [SerializeField] private Image playerInteractionIcon;
    [SerializeField] private TMP_Text interactionText;
    [SerializeField] private GameObject targetPlayer;
    [SerializeField] private Image playerAvatar;
    [SerializeField] private TMP_Text targetPlayerNameText;
    [SerializeField] private TMP_Text targetPlayerBalanceText;
    [SerializeField] private TMP_Text targetIndicator;
    [SerializeField] private TMP_Text transferAmountText;
    [SerializeField] private TMP_Text transferDirectionText;
    [SerializeField] private GameObject playerListEffect;
    [SerializeField] private TMP_Text listHeader;
    [SerializeField] private Transform playerRowContainer;
    [SerializeField] private TMP_Text totalAmountText;

    [Header("Special Effect")]
    [SerializeField] private TMP_Text specialEffectHeader;
    [SerializeField] private Image specialEffectIcon;
    [SerializeField] private TMP_Text specialEffectText;
    [SerializeField] private TMP_Text specialDurationText;
    [SerializeField] private TMP_Text specialDetailsText;
    [SerializeField] private TMP_Text specialStatusIndicator;

    [Header("Choice Area")]
    [SerializeField] private GameObject choiceArea;
    [SerializeField] private TMP_Text choiceLabel;
    [SerializeField] private Button choiceButtonA;
    [SerializeField] private Button choiceButtonB;
    [SerializeField] private TMP_Text choiceButtonAText;
    [SerializeField] private TMP_Text choiceButtonBText;
    [SerializeField] private TMP_Text orText;

    [Header("Target Area")]
    [SerializeField] private GameObject targetArea;
    [SerializeField] private TMP_Text targetHeader;
    [SerializeField] private Image targetIcon;
    [SerializeField] private TMP_Text targetNameText;
    [SerializeField] private TMP_Text targetInfoText;
    [Tooltip("Cycles through available targets.")]
    [SerializeField] private Button targetNextButton;
    [Tooltip("Confirms the currently displayed target.")]
    [SerializeField] private Button targetSelectButton;

    [Header("Result Area")]
    [SerializeField] private GameObject resultArea;
    [SerializeField] private TMP_Text resultHeader;
    [SerializeField] private Image resultIcon;
    [SerializeField] private TMP_Text resultMainText;
    [SerializeField] private TMP_Text resultDetailText;
    [SerializeField] private TMP_Text resultStatusText;

    [Header("Close")]
    [SerializeField] private Button closeButtonBottom;

    [Header("Animation")]
    [SerializeField] private float revealDuration = 0.30f;
    [SerializeField] private float revealScale = 0.92f;
    [SerializeField] private float cardRollDuration = 0.80f;
    [SerializeField] private float cardRollInterval = 0.07f;

    private Vector3 originalScale = Vector3.one;
    private CardManager cardManager;
    private BoardPlayer currentPlayer;
    private bool previewMode;

    private void Awake()
    {
        if (popup == null)
            popup = gameObject;

        if (cardTransform == null)
            cardTransform = transform as RectTransform;

        if (cardTransform != null)
            originalScale = cardTransform.localScale;

        // IMPORTANT: ChanceCardUI itself must remain ACTIVE at all times.
        // The popup is hidden with CanvasGroup instead of SetActive(false),
        // because disabling ChanceCard would prevent coroutines from running.
        EnsurePopupCanvasGroup();

        if (coroutineRunner == null)
        {
            coroutineRunner =
                FindFirstObjectByType<ChanceCoroutineRunner>(
                    FindObjectsInactive.Include
                );
        }

        WireButtons();
        HideImmediate();
    }

    private void WireButtons()
    {
        Wire(closeButtonTop, Close);
        Wire(closeButtonBottom, Close);

        Wire(choiceButtonA, () =>
        {
            if (cardManager != null)
                cardManager.SelectChoice(0);
        });

        Wire(choiceButtonB, () =>
        {
            if (cardManager != null)
                cardManager.SelectChoice(1);
        });

        Wire(selectPropertyButton, () =>
        {
            if (cardManager != null)
                cardManager.TargetPropertyButtonPressed();
        });

        Wire(targetNextButton, () =>
        {
            if (cardManager != null)
                cardManager.NextTarget();
        });

        // TargetSelectButton is rewired dynamically by
        // ShowPlayerTarget() or ShowPropertyTarget().
    }

    private void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    public void Initialize(CardManager manager)
    {
        cardManager = manager;
    }








    // ============================================================
    // CARD REVEAL
    // ============================================================

    public void BeginCardReveal(
        BoardPlayer player,
        string deckName,
        List<CardManager.CardPreview> previews,
        CardManager.CardPreview finalCard,
        Action onRevealFinished)
    {
        previewMode = false;
        currentPlayer = player;
        ResetAllSections();
        SetActive(true);
        SetCloseInteractable(false);

        if (chanceLabel != null)
            chanceLabel.text = deckName;

        if (coroutineRunner == null)
        {
            coroutineRunner =
                FindFirstObjectByType<ChanceCoroutineRunner>(
                    FindObjectsInactive.Include
                );
        }

        if (coroutineRunner == null)
        {
            Debug.LogError(
                "ChanceCardUI: ChanceCoroutineRunner not found."
            );

            return;
        }

        coroutineRunner.Run(
            CardRollRoutine(
                deckName,
                previews,
                finalCard,
                onRevealFinished
            )
        );
    }

    private IEnumerator CardRollRoutine(
        string deckName,
        List<CardManager.CardPreview> previews,
        CardManager.CardPreview finalCard,
        Action onRevealFinished)
    {
        if (previews != null && previews.Count > 0)
        {
            float elapsed = 0f;

            while (elapsed < cardRollDuration)
            {
                CardManager.CardPreview preview =
                    previews[UnityEngine.Random.Range(0, previews.Count)];

                SetMainCard(deckName, preview.title, preview.description);

                elapsed += cardRollInterval;
                yield return new WaitForSecondsRealtime(
                    Mathf.Max(0.01f, cardRollInterval)
                );
            }
        }

        if (finalCard != null)
            SetMainCard(deckName, finalCard.title, finalCard.description);
        else
            SetMainCard(deckName, string.Empty, string.Empty);

        if (cardTransform != null)
        {
            cardTransform.localScale = originalScale * revealScale;

            // ChanceCard can be inactive when the popup is hidden.
            // The coroutine is therefore hosted by ChanceCoroutineRunner,
            // which lives on the always-active Canvas.
            if (coroutineRunner == null)
            {
                Debug.LogError(
                    "ChanceCardUI: ChanceCoroutineRunner not found."
                );

                cardTransform.localScale = originalScale;
            }
            else
            {
                yield return RevealRoutine();
            }
        }

        SetCloseInteractable(true);
        onRevealFinished?.Invoke();
    }

    private IEnumerator RevealRoutine()
    {
        float elapsed = 0f;
        float duration = Mathf.Max(0.01f, revealDuration);
        Vector3 start = originalScale * revealScale;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            t = t * t * (3f - 2f * t);

            cardTransform.localScale = Vector3.Lerp(
                start,
                originalScale,
                t
            );

            yield return null;
        }

        cardTransform.localScale = originalScale;
    }

    private void SetMainCard(
        string deckName,
        string title,
        string description)
    {
        if (chanceLabel != null)
            chanceLabel.text = deckName;

        if (cardTitle != null)
            cardTitle.text = title ?? string.Empty;

        if (cardDescription != null)
            cardDescription.text = description ?? string.Empty;
    }

    // ============================================================
    // SIMPLE SHOW
    // ============================================================

    public void ShowCard(
        BoardPlayer player,
        string title,
        string description)
    {
        previewMode = false;
        currentPlayer = player;
        ResetAllSections();
        SetActive(true);
        SetMainCard("CHANCE", title, description);
        SetCloseInteractable(true);

        if (cardTransform != null)
        {
            cardTransform.localScale = originalScale * revealScale;
            // Do not call StartCoroutine here. ChanceCard may be inactive.
            // The always-active Canvas runner owns the animation coroutine.
            if (coroutineRunner != null)
            {
                coroutineRunner.Run(
                    RevealRoutine()
                );
            }
            else
            {
                Debug.LogError(
                    "ChanceCardUI: ChanceCoroutineRunner not found."
                );
            }
        }
    }

    // ============================================================
    // DEVELOPER PREVIEW
    // ============================================================

    public void ShowPreviewCard(
        string deckName,
        string title,
        string description)
    {
        previewMode = true;
        currentPlayer = null;

        ResetAllSections();
        SetActive(true);
        SetMainCard(
            deckName,
            title,
            description
        );
        SetCloseInteractable(true);

        if (cardTransform != null)
        {
            cardTransform.localScale =
                originalScale * revealScale;

            if (coroutineRunner != null)
            {
                coroutineRunner.Run(
                    RevealRoutine()
                );
            }
        }
    }

    // ============================================================
    // CLOSE
    // ============================================================

    private void Close()
    {
        if (previewMode)
        {
            HideImmediate();
            return;
        }

        if (cardManager != null)
            cardManager.CloseCard();
        else
            HideImmediate();
    }

    public void HideImmediate()
    {
        SetActive(false);

        if (cardTransform != null)
            cardTransform.localScale = originalScale;

        SetCloseInteractable(false);
        currentPlayer = null;
        previewMode = false;
    }

    private void EnsurePopupCanvasGroup()
    {
        if (popup == null)
            popup = gameObject;

        if (popupCanvasGroup == null && popup != null)
            popupCanvasGroup = popup.GetComponent<CanvasGroup>();

        if (popupCanvasGroup == null && popup != null)
            popupCanvasGroup = popup.AddComponent<CanvasGroup>();
    }

    private void SetActive(bool value)
    {
        EnsurePopupCanvasGroup();

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = value ? 1f : 0f;
            popupCanvasGroup.interactable = value;
            popupCanvasGroup.blocksRaycasts = value;
        }
        else if (popup != null)
        {
            // Fallback only if CanvasGroup cannot be created.
            // Normally the CanvasGroup path is always used.
            popup.SetActive(value);
        }
    }

    public void SetCloseInteractable(bool interactable)
    {
        if (closeButtonTop != null)
            closeButtonTop.interactable = interactable;

        if (closeButtonBottom != null)
            closeButtonBottom.interactable = interactable;
    }

    // ============================================================
    // RESET
    // ============================================================

    private void ResetAllSections()
    {
        SetSection(effectArea, false);
        SetSection(moneyEffect, false);
        SetSection(conditionEffect, false);
        SetSection(propertyEffect, false);
        SetSection(movementEffect, false);
        SetSection(playerEffect, false);
        SetSection(specialEffect, false);
        SetSection(choiceArea, false);
        SetSection(targetArea, false);
        SetSection(resultArea, false);

        if (selectPropertyButton != null)
            selectPropertyButton.gameObject.SetActive(false);

        if (targetNextButton != null)
            targetNextButton.gameObject.SetActive(false);

        if (targetSelectButton != null)
            targetSelectButton.gameObject.SetActive(false);

        ClearPlayerRows();
        ClearTexts();
    }

    private void SetSection(GameObject section, bool active)
    {
        if (section != null)
            section.SetActive(active);
    }

    private void ClearTexts()
    {
        SetText(moneyEffectLabel, string.Empty);
        SetText(moneyAmountText, string.Empty);
        SetText(moneySourceText, string.Empty);
        SetText(moneyDetailText, string.Empty);
        SetText(moneyBreakdownText, string.Empty);

        SetText(conditionHeader, string.Empty);
        SetText(conditionText, string.Empty);
        SetText(conditionDetailsText, string.Empty);
        SetText(conditionStatusText, string.Empty);
        SetText(conditionResultText, string.Empty);

        SetText(propertyEffectHeader, string.Empty);
        SetText(propertyNameText, string.Empty);
        SetText(propertyInfoText, string.Empty);
        SetText(propertyActionText, string.Empty);
        SetText(propertyTargetText, string.Empty);
        SetText(propertyResultText, string.Empty);

        SetText(movementEffectHeader, string.Empty);
        SetText(currentPositionText, string.Empty);
        SetText(destinationText, string.Empty);
        SetText(movementDistanceText, string.Empty);
        SetText(movementRuleText, string.Empty);

        SetText(playerEffectHeader, string.Empty);
        SetText(interactionText, string.Empty);
        SetText(targetPlayerNameText, string.Empty);
        SetText(targetPlayerBalanceText, string.Empty);
        SetText(targetIndicator, string.Empty);
        SetText(transferAmountText, string.Empty);
        SetText(transferDirectionText, string.Empty);
        SetText(listHeader, string.Empty);
        SetText(totalAmountText, string.Empty);

        SetText(specialEffectHeader, string.Empty);
        SetText(specialEffectText, string.Empty);
        SetText(specialDurationText, string.Empty);
        SetText(specialDetailsText, string.Empty);
        SetText(specialStatusIndicator, string.Empty);

        SetText(choiceLabel, string.Empty);
        SetText(choiceButtonAText, string.Empty);
        SetText(choiceButtonBText, string.Empty);
        SetText(orText, string.Empty);

        SetText(targetHeader, string.Empty);
        SetText(targetNameText, string.Empty);
        SetText(targetInfoText, string.Empty);

        SetText(resultHeader, string.Empty);
        SetText(resultMainText, string.Empty);
        SetText(resultDetailText, string.Empty);
        SetText(resultStatusText, string.Empty);
    }

    private void SetText(TMP_Text target, string value)
    {
        if (target != null)
            target.text = value;
    }

    // ============================================================
    // EFFECT AREA
    // ============================================================

    // Only ONE effect child is allowed to be visible at a time.
    // A card may have multiple mechanical outcomes, but they must be
    // represented inside one primary effect panel to avoid overlap.
    private void HideAllEffectSections()
    {
        SetSection(moneyEffect, false);
        SetSection(conditionEffect, false);
        SetSection(propertyEffect, false);
        SetSection(movementEffect, false);
        SetSection(playerEffect, false);
        SetSection(specialEffect, false);
    }

    public void ShowMoneyEffect(
        string label,
        string amount,
        string source,
        string detail,
        string breakdown = "")
    {
        HideAllEffectSections();
        SetSection(effectArea, true);
        SetSection(moneyEffect, true);

        SetText(moneyEffectLabel, label);
        SetText(moneyAmountText, amount);
        SetText(moneySourceText, source);
        SetText(moneyDetailText, detail);
        SetText(moneyBreakdownText, breakdown);
    }

    public void ShowConditionEffect(
        string condition,
        string details,
        string status,
        string result)
    {
        HideAllEffectSections();
        SetSection(effectArea, true);
        SetSection(conditionEffect, true);

        SetText(conditionHeader, "CONDITION");
        SetText(conditionText, condition);
        SetText(conditionDetailsText, details);
        SetText(conditionStatusText, status);
        SetText(conditionResultText, result);
    }

    public void ShowPropertyEffect(
        string propertyName,
        string propertyInfo,
        string action,
        string target,
        string result)
    {
        HideAllEffectSections();
        SetSection(effectArea, true);
        SetSection(propertyEffect, true);

        SetText(propertyEffectHeader, "PROPERTY EFFECT");
        SetText(propertyNameText, propertyName);
        SetText(propertyInfoText, propertyInfo);
        SetText(propertyActionText, action);
        SetText(propertyTargetText, target);
        SetText(propertyResultText, result);
    }

    public void ShowMovementEffect(
        string current,
        string destination,
        string distance,
        string rule)
    {
        HideAllEffectSections();
        SetSection(effectArea, true);
        SetSection(movementEffect, true);

        SetText(movementEffectHeader, "MOVEMENT");
        SetText(currentPositionText, current);
        SetText(destinationText, destination);
        SetText(movementDistanceText, distance);
        SetText(movementRuleText, rule);
    }

    public void ShowPlayerEffect(
        string interaction,
        string direction,
        string amount)
    {
        HideAllEffectSections();
        SetSection(effectArea, true);
        SetSection(playerEffect, true);

        SetText(playerEffectHeader, "PLAYER INTERACTION");
        SetText(interactionText, interaction);
        SetText(transferDirectionText, direction);
        SetText(transferAmountText, amount);
    }

    public void ShowSpecialEffect(
        string text,
        string duration,
        string details,
        string status)
    {
        HideAllEffectSections();
        SetSection(effectArea, true);
        SetSection(specialEffect, true);

        SetText(specialEffectHeader, "SPECIAL EFFECT");
        SetText(specialEffectText, text);
        SetText(specialDurationText, duration);
        SetText(specialDetailsText, details);
        SetText(specialStatusIndicator, status);
    }

    // ============================================================
    // CHOICES
    // ============================================================

    public void ShowChoice(
        string label,
        string optionA,
        string optionB)
    {
        SetSection(choiceArea, true);

        SetText(choiceLabel, label);
        SetText(choiceButtonAText, optionA);
        SetText(choiceButtonBText, optionB);

        if (choiceButtonA != null)
            choiceButtonA.gameObject.SetActive(true);

        if (choiceButtonB != null)
            choiceButtonB.gameObject.SetActive(true);

        if (orText != null)
        {
            orText.text = "OR";
            orText.gameObject.SetActive(true);
        }
    }

    public void SetChoiceInteractable(bool optionA, bool optionB)
    {
        if (choiceButtonA != null)
            choiceButtonA.interactable = optionA;

        if (choiceButtonB != null)
            choiceButtonB.interactable = optionB;
    }

    // ============================================================
    // TARGET AREA
    // ============================================================
    public void ShowPlayerTarget(
        string name,
        string info,
        string buttonText)
    {
        SetSection(
            targetArea,
            true
        );

        if (targetHeader != null)
            targetHeader.text = "TARGET PLAYER";

        if (targetNameText != null)
            targetNameText.text = name;

        if (targetInfoText != null)
            targetInfoText.text = info;

        // TargetArea owns the universal confirmation button.
        // Rewire it for player targeting.
        if (targetSelectButton != null)
        {
            targetSelectButton.gameObject.SetActive(true);
            SetButtonText(targetSelectButton, buttonText);

            targetSelectButton.onClick.RemoveAllListeners();
            targetSelectButton.onClick.AddListener(() =>
            {
                if (cardManager != null)
                    cardManager.TargetPlayerButtonPressed();
            });
        }

        // The old PropertyEffect selection button is not used for targeting.
        if (selectPropertyButton != null)
            selectPropertyButton.gameObject.SetActive(false);
    }

    public void ShowPropertyTarget(
        string name,
        string info,
        string buttonText)
    {
        SetSection(
            targetArea,
            true
        );

        if (targetHeader != null)
            targetHeader.text = "TARGET PROPERTY";

        if (targetNameText != null)
            targetNameText.text = name;

        if (targetInfoText != null)
            targetInfoText.text = info;

        // TargetArea owns the universal confirmation button.
        // Rewire it for property targeting.
        if (targetSelectButton != null)
        {
            targetSelectButton.gameObject.SetActive(true);
            SetButtonText(targetSelectButton, buttonText);

            targetSelectButton.onClick.RemoveAllListeners();
            targetSelectButton.onClick.AddListener(() =>
            {
                if (cardManager != null)
                    cardManager.TargetPropertyButtonPressed();
            });
        }

        // The PropertyEffect button is not used for targeting.
        if (selectPropertyButton != null)
            selectPropertyButton.gameObject.SetActive(false);
    }

    public void SetTargetNextButtonVisible(
        bool visible)
    {
        if (targetNextButton != null)
        {
            targetNextButton.gameObject.SetActive(
                visible
            );
        }
    }

    public void ConfigureTargetNavigation(
        bool showNext,
        bool showConfirm,
        string nextText = "NEXT",
        string confirmText = "SELECT")
    {
        if (targetNextButton != null)
        {
            targetNextButton.gameObject.SetActive(showNext);
            SetButtonText(targetNextButton, nextText);
        }

        if (targetSelectButton != null)
        {
            targetSelectButton.gameObject.SetActive(showConfirm);
            SetButtonText(targetSelectButton, confirmText);
        }
    }

    private void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text buttonText =
            button.GetComponentInChildren<TMP_Text>(true);

        if (buttonText != null)
            buttonText.text = value ?? string.Empty;
    }

    // ============================================================
    // RESULT AREA
    // ============================================================

    public void ShowResult(
        string mainText,
        string detail,
        string status)
    {
        // A final result replaces temporary selection UI.
        SetSection(choiceArea, false);
        SetSection(targetArea, false);

        if (targetNextButton != null)
            targetNextButton.gameObject.SetActive(false);

        if (targetSelectButton != null)
            targetSelectButton.gameObject.SetActive(false);

        if (selectPropertyButton != null)
            selectPropertyButton.gameObject.SetActive(false);

        SetSection(resultArea, true);
        SetText(resultHeader, "RESULT");
        SetText(resultMainText, mainText);
        SetText(resultDetailText, detail);
        SetText(resultStatusText, status);
    }

    // ============================================================
    // PLAYER LIST
    // ============================================================

    public void ShowPlayerList(string header, string total)
    {
        HideAllEffectSections();

        SetSection(effectArea, true);
        SetSection(playerEffect, true);
        SetSection(playerListEffect, true);

        SetText(listHeader, header);
        SetText(totalAmountText, total);
    }

    public void AddPlayerListRow(
        string playerName,
        string amountText)
    {
        if (playerRowContainer == null)
            return;

        GameObject row =
            new GameObject(
                "CardPlayerRow",
                typeof(RectTransform)
            );

        row.transform.SetParent(playerRowContainer, false);

        RectTransform rowRect =
            row.GetComponent<RectTransform>();

        rowRect.anchorMin = new Vector2(0f, 0.5f);
        rowRect.anchorMax = new Vector2(1f, 0.5f);
        rowRect.pivot = new Vector2(0.5f, 0.5f);
        rowRect.sizeDelta = new Vector2(0f, 30f);

        CreateRowText(
            row.transform,
            "Name",
            playerName,
            new Vector2(0f, 0f),
            new Vector2(0.55f, 1f),
            TextAlignmentOptions.Left
        );

        CreateRowText(
            row.transform,
            "Amount",
            amountText,
            new Vector2(0.55f, 0f),
            new Vector2(1f, 1f),
            TextAlignmentOptions.Right
        );
    }

    private void CreateRowText(
        Transform parent,
        string objectName,
        string value,
        Vector2 anchorMin,
        Vector2 anchorMax,
        TextAlignmentOptions alignment)
    {
        GameObject textObject =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        textObject.transform.SetParent(parent, false);

        TMP_Text text =
            textObject.AddComponent<TextMeshProUGUI>();

        text.text = value ?? string.Empty;
        text.alignment = alignment;

        RectTransform rect =
            textObject.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void ClearPlayerRows()
    {
        if (playerRowContainer == null)
            return;

        for (int i = playerRowContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(playerRowContainer.GetChild(i).gameObject);
        }
    }
}
