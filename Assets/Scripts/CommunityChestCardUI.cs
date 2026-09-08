using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommunityChestCardUI : MonoBehaviour
{
    [Header("Root")]
    [SerializeField] private GameObject popup;
    [SerializeField] private RectTransform cardTransform;
    [SerializeField] private CanvasGroup popupCanvasGroup;

    [Header("Header")]
    [SerializeField] private TMP_Text communityChestLabel;
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
[SerializeField] private TMP_Text transferAmountText;
    [SerializeField] private TMP_Text transferDirectionText;
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
    [SerializeField] private Button targetNextButton;
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

    private Vector3 originalScale = Vector3.one;
    private bool referencesReady;

    private void Awake()
    {
        EnsureBuilt();
        WireButtons();
        HideImmediate();
    }

    public void EnsureBuilt()
    {
        if (referencesReady)
            return;

        if (popup == null)
        {
            Transform candidate = transform;
            while (candidate != null)
            {
                if (candidate.name == "CommunityChestPopup")
                {
                    popup = candidate.gameObject;
                    break;
                }
                candidate = candidate.parent;
            }
        }

        if (popup == null)
            popup = gameObject;

        if (cardTransform == null)
            cardTransform = transform as RectTransform;

        if (cardTransform != null)
            originalScale = cardTransform.localScale;

        EnsurePopupCanvasGroup();
        AutoWireHierarchy();
        referencesReady = true;
    }

    private void EnsurePopupCanvasGroup()
    {
        if (popupCanvasGroup == null && popup != null)
            popupCanvasGroup = popup.GetComponent<CanvasGroup>();

        if (popupCanvasGroup == null && popup != null)
            popupCanvasGroup = popup.AddComponent<CanvasGroup>();
    }

    private void AutoWireHierarchy()
    {
        communityChestLabel = FindText("CommunityChestHeader", communityChestLabel);
        if (communityChestLabel == null)
            communityChestLabel = FindText("ChanceHeader", communityChestLabel);

        closeButtonTop = FindComponent<Button>("CloseButton", closeButtonTop);
        if (closeButtonTop == null)
            closeButtonTop = FindComponent<Button>("CloseButtonTop", closeButtonTop);

        cardIcon = FindComponent<Image>("CardIcon", cardIcon);
        cardTitle = FindText("CardTitle", cardTitle);
        cardDescription = FindText("CardDescription", cardDescription);

        effectArea = FindObject("EffectArea", effectArea);
        moneyEffect = FindObject("MoneyEffect", moneyEffect);
        conditionEffect = FindObject("ConditionEffect", conditionEffect);
        propertyEffect = FindObject("PropertyEffect", propertyEffect);
        movementEffect = FindObject("MovementEffect", movementEffect);
        playerEffect = FindObject("PlayerEffect", playerEffect);
        specialEffect = FindObject("SpecialEffect", specialEffect);
        choiceArea = FindObject("ChoiceArea", choiceArea);
        targetArea = FindObject("TargetArea", targetArea);
        resultArea = FindObject("ResultArea", resultArea);

        moneyEffectLabel = FindText("MoneyEffectLabel", moneyEffectLabel);
        moneyIcon = FindComponent<Image>("MoneyIcon", moneyIcon);
        moneyAmountText = FindText("MoneyAmountText", moneyAmountText);
        moneySourceText = FindText("MoneySourceText", moneySourceText);
        moneyDetailText = FindText("MoneyDetailText", moneyDetailText);
        moneyBreakdownText = FindText("MoneyBreakdownText", moneyBreakdownText);

        conditionHeader = FindText("ConditionHeader", conditionHeader);
        conditionIcon = FindComponent<Image>("ConditionIcon", conditionIcon);
        conditionText = FindText("ConditionText", conditionText);
        conditionDetailsText = FindText("ConditionDetailsText", conditionDetailsText);
        conditionStatusText = FindText("ConditionStatusText", conditionStatusText);
        conditionResultText = FindText("ConditionResultText", conditionResultText);

        propertyEffectHeader = FindText("PropertyEffectHeader", propertyEffectHeader);
        propertyIcon = FindComponent<Image>("PropertyIcon", propertyIcon);
        propertyNameText = FindText("PropertyNameText", propertyNameText);
        propertyInfoText = FindText("PropertyInfoText", propertyInfoText);
        propertyActionText = FindText("PropertyActionText", propertyActionText);
        propertyTargetText = FindText("PropertyTargetText", propertyTargetText);
        selectPropertyButton = FindComponent<Button>("TargetSelectButton", selectPropertyButton);
        propertyResultText = FindText("PropertyResultText", propertyResultText);

        movementEffectHeader = FindText("MovementEffectHeader", movementEffectHeader);
        movementIcon = FindComponent<Image>("MovementIcon", movementIcon);
        currentPositionText = FindText("CurrentPositionText", currentPositionText);
        movementArrow = FindComponent<Image>("MovementArrow", movementArrow);
        destinationText = FindText("DestinationText", destinationText);
        movementDistanceText = FindText("MovementDistanceText", movementDistanceText);
        movementRuleText = FindText("MovementRuleText", movementRuleText);

        playerEffectHeader = FindText("PlayerEffectHeader", playerEffectHeader);
        playerInteractionIcon = FindComponent<Image>("PlayerInteractionIcon", playerInteractionIcon);
        interactionText = FindText("InteractionText", interactionText);
        transferAmountText = FindText("TransferAmountText", transferAmountText);
        transferDirectionText = FindText("TransferDirectionText", transferDirectionText);

        specialEffectHeader = FindText("SpecialEffectHeader", specialEffectHeader);
        specialEffectIcon = FindComponent<Image>("SpecialEffectIcon", specialEffectIcon);
        specialEffectText = FindText("SpecialEffectText", specialEffectText);
        specialDurationText = FindText("SpecialDurationText", specialDurationText);
        specialDetailsText = FindText("SpecialDetailsText", specialDetailsText);
        specialStatusIndicator = FindText("SpecialStatusIndicator", specialStatusIndicator);

        choiceLabel = FindText("ChoiceLabel", choiceLabel);
        choiceButtonA = FindComponent<Button>("ChoiceButtonA", choiceButtonA);
        choiceButtonB = FindComponent<Button>("ChoiceButtonB", choiceButtonB);
        choiceButtonAText = FindText("ChoiceButtonAText", choiceButtonAText);
        choiceButtonBText = FindText("ChoiceButtonBText", choiceButtonBText);
        orText = FindText("OR", orText);

        targetHeader = FindText("TargetHeader", targetHeader);
        targetIcon = FindComponent<Image>("TargetIcon", targetIcon);
        targetNameText = FindText("TargetNameText", targetNameText);
        targetInfoText = FindText("TargetInfoText", targetInfoText);
        targetNextButton = FindComponent<Button>("TargetNextButton", targetNextButton);
        targetSelectButton = FindComponent<Button>("TargetSelectButton", targetSelectButton);

        resultHeader = FindText("ResultHeader", resultHeader);
        resultIcon = FindComponent<Image>("ResultIcon", resultIcon);
        resultMainText = FindText("ResultMainText", resultMainText);
        resultDetailText = FindText("ResultDetailText", resultDetailText);
        resultStatusText = FindText("ResultStatusText", resultStatusText);

        closeButtonBottom = FindComponent<Button>("CloseButtonBottom", closeButtonBottom);
        if (closeButtonBottom == null)
            closeButtonBottom = FindComponent<Button>("CloseButton", closeButtonBottom);

        if (targetArea != null)
        {
            // If the hierarchy has a single TargetSelectButton, it is used by
            // property selection in the Community Chest manager.
            selectPropertyButton = targetSelectButton ?? selectPropertyButton;
        }
    }

    private T FindComponent<T>(string objectName, T current) where T : Component
    {
        if (current != null)
            return current;

        Transform found = FindTransform(objectName, null);
        return found != null ? found.GetComponent<T>() : null;
    }

    private TMP_Text FindText(string objectName, TMP_Text current)
    {
        if (current != null)
            return current;

        Transform found = FindTransform(objectName, null);
        return found != null ? found.GetComponent<TMP_Text>() : null;
    }

    private GameObject FindObject(string objectName, GameObject current)
    {
        if (current != null)
            return current;

        Transform found = FindTransform(objectName, null);
        return found != null ? found.gameObject : null;
    }

    private Transform FindTransform(string objectName, Transform current)
    {
        if (current != null)
            return current;

        if (transform.name == objectName)
            return transform;

        return FindChildRecursive(transform, objectName);
    }

    private Transform FindChildRecursive(Transform parent, string objectName)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == objectName)
                return child;

            Transform nested = FindChildRecursive(child, objectName);
            if (nested != null)
                return nested;
        }

        return null;
    }

    private void WireButtons()
    {
        Wire(closeButtonTop, HideImmediate);
        Wire(closeButtonBottom, HideImmediate);
    }

    private void Wire(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
            return;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }

    // ============================================================
    // PUBLIC API — MATCHES COMMUNITY CHEST MANAGER
    // ============================================================

    public void ShowCard(string title, string description)
    {
        EnsureBuilt();
        ResetAllSections();
        SetActive(true);
        SetMainCard(title, description);
        SetCloseInteractable(false);
    }

    private void SetMainCard(string title, string description)
    {
        SetText(communityChestLabel, "COMMUNITY CHEST");
        SetText(cardTitle, title);
        SetText(cardDescription, description);
    }

    public void ShowMoneyEffect(
        string label,
        string amount,
        string detail)
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(moneyEffect, true);
        SetText(moneyEffectLabel, label);
        SetText(moneyAmountText, amount);
        SetText(moneySourceText, "FROM THE BANK");
        SetText(moneyDetailText, detail);
        SetText(moneyBreakdownText, string.Empty);
    }

    public void ShowPaymentEffect(
        string label,
        string amount,
        string detail)
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(moneyEffect, true);
        SetText(moneyEffectLabel, label);
        SetText(moneyAmountText, amount);
        SetText(moneySourceText, "TO THE BANK");
        SetText(moneyDetailText, detail);
        SetText(moneyBreakdownText, string.Empty);

        if (moneyAmountText != null)
            moneyAmountText.color = amount != null && amount.Contains("-")
                ? new Color(1f, 0.40f, 0.40f)
                : Color.white;
    }

    public void ShowConditionEffect(
        string condition,
        string details,
        string status,
        string result)
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(conditionEffect, true);
        SetText(conditionHeader, "CONDITION");
        SetText(conditionText, condition);
        SetText(conditionDetailsText, details);
        SetText(conditionStatusText, status);
        SetText(conditionResultText, result);
    }

    public void ShowPropertyEffect(
        string property,
        string info,
        string action,
        string detail)
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(propertyEffect, true);
        SetText(propertyEffectHeader, "PROPERTY EFFECT");
        SetText(propertyNameText, property);
        SetText(propertyInfoText, info);
        SetText(propertyActionText, action);
        SetText(propertyTargetText, "TARGET PROPERTY");
        SetText(propertyResultText, detail);
    }

    public void ShowMovementEffect(
        string current,
        string destination,
        string movement,
        string rule)
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(movementEffect, true);
        SetText(movementEffectHeader, "MOVEMENT");
        SetText(currentPositionText, current);
        SetText(destinationText, destination);
        SetText(movementDistanceText, movement);
        SetText(movementRuleText, rule);
    }

    public void ShowPlayerEffect(
        string interaction,
        string direction,
        string amount,
        string detail = "")
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(playerEffect, true);

        SetText(playerEffectHeader, "PLAYER INTERACTION");
        SetText(interactionText, interaction);
        SetText(transferDirectionText, direction);
        SetText(transferAmountText, amount);
    }

    public void ShowSpecialEffect(
        string header,
        string duration,
        string details,
        string status)
    {
        SetSection(effectArea, true);
        HideEffectChildren();
        SetSection(specialEffect, true);
        SetText(specialEffectHeader, header);
        SetText(specialEffectText, header);
        SetText(specialDurationText, duration);
        SetText(specialDetailsText, details);
        SetText(specialStatusIndicator, status);
    }

    public void ShowChoice(
        string header,
        string optionA,
        string optionB,
        Action actionA,
        Action actionB)
    {
        SetSection(choiceArea, true);
        SetSection(targetArea, false);
        SetSection(resultArea, false);

        SetText(choiceLabel, header);
        SetButtonText(choiceButtonA, optionA);
        SetButtonText(choiceButtonB, optionB);

        Wire(choiceButtonA, () => actionA?.Invoke());
        Wire(choiceButtonB, () => actionB?.Invoke());

        choiceButtonA.interactable = true;
        choiceButtonB.interactable = true;
        SetCloseInteractable(false);
    }

    public void ShowTarget(
        string header,
        string name,
        string info,
        string selectText,
        bool showNext,
        Action onSelect,
        Action onNext)
    {
        SetSection(choiceArea, false);
        SetSection(targetArea, true);
        SetSection(resultArea, false);

        SetText(targetHeader, header);
        SetText(targetNameText, name);
        SetText(targetInfoText, info);

        Button selectButton = targetSelectButton != null
            ? targetSelectButton
            : selectPropertyButton;

        SetButtonText(selectButton, selectText);
        Wire(selectButton, () => onSelect?.Invoke());
        Wire(targetNextButton, () => onNext?.Invoke());

        if (selectButton != null)
        {
            selectButton.gameObject.SetActive(true);
            selectButton.interactable = true;
        }

        SetTargetNextButtonVisible(showNext);
        SetCloseInteractable(false);
    }

    /// <summary>
    /// Displays player information in the existing TargetArea without
    /// turning it into a selection UI. Used by automatic player-effect cards.
    /// </summary>
    public void ShowPlayerTargetInfo(
        string name,
        string info,
        string status)
    {
        SetSection(choiceArea, false);
        SetSection(targetArea, true);
        SetSection(resultArea, false);

        SetText(targetHeader, "TARGET PLAYER");
        SetText(targetNameText, name);

        string finalInfo = info ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(status))
        {
            finalInfo +=
                (string.IsNullOrWhiteSpace(finalInfo) ? string.Empty : "\n") +
                status;
        }

        SetText(targetInfoText, finalInfo);

        if (targetNextButton != null)
        {
            targetNextButton.gameObject.SetActive(false);
            targetNextButton.interactable = false;
        }

        if (targetSelectButton != null)
        {
            targetSelectButton.gameObject.SetActive(false);
            targetSelectButton.interactable = false;
        }
    }

    public void SetTargetNextButtonVisible(bool visible)
    {
        if (targetNextButton != null)
            targetNextButton.gameObject.SetActive(visible);
    }

    public void ShowResult(
        string mainText,
        string detail,
        string status,
        bool keepTargetVisible = false)
    {
        SetSection(choiceArea, false);
        SetSection(targetArea, keepTargetVisible);
        SetSection(resultArea, true);

        SetText(resultHeader, "RESULT");
        SetText(resultMainText, mainText);
        SetText(resultDetailText, detail);
        SetText(resultStatusText, status);
        SetCloseInteractable(true);
    }

    public void SetCloseInteractable(bool interactable)
    {
        if (closeButtonTop != null)
            closeButtonTop.interactable = interactable;

        if (closeButtonBottom != null)
            closeButtonBottom.interactable = interactable;
    }

    public void HideImmediate()
    {
        SetActive(false);

        if (cardTransform != null)
            cardTransform.localScale = originalScale;

        SetCloseInteractable(false);
    }

    private void SetActive(bool active)
    {
        EnsurePopupCanvasGroup();

        if (popupCanvasGroup != null)
        {
            popupCanvasGroup.alpha = active ? 1f : 0f;
            popupCanvasGroup.interactable = active;
            popupCanvasGroup.blocksRaycasts = active;
        }
        else if (popup != null)
        {
            popup.SetActive(active);
        }
    }

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

        if (targetNextButton != null)
            targetNextButton.gameObject.SetActive(false);

        if (targetSelectButton != null)
            targetSelectButton.gameObject.SetActive(false);
    }

    private void HideEffectChildren()
    {
        SetSection(moneyEffect, false);
        SetSection(conditionEffect, false);
        SetSection(propertyEffect, false);
        SetSection(movementEffect, false);
        SetSection(playerEffect, false);
        SetSection(specialEffect, false);
    }

    private void SetSection(GameObject section, bool active)
    {
        if (section != null)
            section.SetActive(active);
    }

    private void SetText(TMP_Text text, string value)
    {
        if (text != null)
            text.text = value ?? string.Empty;
    }

    private void SetButtonText(Button button, string value)
    {
        if (button == null)
            return;

        TMP_Text text =
            button.GetComponentInChildren<TMP_Text>(true);

        if (text != null)
            text.text = value ?? string.Empty;
    }
}
