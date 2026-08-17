using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds TradePanel and TradeRequestPanel UI.
/// Safe to run repeatedly — reuses existing objects when possible.
/// </summary>
public static class TradePanelSetup
{
    private const float PanelWidth = 760f;
    private const float PanelHeight = 880f;

    private const float HeaderHeight = 72f;
    private const float SectionHeaderHeight = 34f;
    private const float RowHeight = 36f;
    private const float ButtonHeight = 44f;
    private const float SmallButtonWidth = 44f;

    private static readonly Color OverlayColor =
        new Color(0f, 0f, 0f, 0.55f);

    private static readonly Color Navy =
        new Color(0.08f, 0.12f, 0.22f, 1f);

    private static readonly Color Magenta =
        new Color(0.72f, 0.11f, 0.42f, 1f);

    private static readonly Color CardColor =
        new Color(0.12f, 0.14f, 0.18f, 1f);

    private static readonly Color SectionColor =
        new Color(0.18f, 0.20f, 0.26f, 1f);

    private static readonly Color TextColor =
        new Color(0.92f, 0.94f, 0.96f, 1f);

    private static readonly Color AccentGreen =
        new Color(0.16f, 0.55f, 0.32f, 1f);

    private static readonly Color AccentRed =
        new Color(0.72f, 0.18f, 0.18f, 1f);

    [MenuItem("Monopoly/Setup Trade UI")]
    public static void SetupTradeUI()
    {
        EnsureTradeManager();

        TradePanel tradePanel =
            Object.FindFirstObjectByType<TradePanel>(
                FindObjectsInactive.Include);

        if (tradePanel == null)
        {
            Debug.LogError(
                "TradePanelSetup: TradePanel not found in scene."
            );

            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            tradePanel.gameObject,
            "Setup Trade UI");

        TMP_FontAsset font = GetFont();
        Sprite slicedSprite = GetSlicedSprite();

        Transform root = tradePanel.transform;

        ConfigureFullScreenRoot(root);

        Image overlay =
            GetOrAdd<Image>(root.gameObject);

        overlay.color = OverlayColor;
        overlay.raycastTarget = true;

        Transform popup =
            GetOrCreate(root, "PopupBackground");

        ConfigurePopup(popup, slicedSprite);

        Transform header = GetOrCreate(popup, "Header");
        ConfigureBar(header, HeaderHeight, Navy);

        TMP_Text headerText =
            GetOrCreateTMP(header, "HeaderText", font, 28f, Color.white);

        headerText.text = "TRADE";
        headerText.alignment = TextAlignmentOptions.Center;
        Stretch(headerText.rectTransform, 12f, 12f, 0f, 0f);

        Transform playerSection =
            GetOrCreate(popup, "PlayerSelectionSection");

        ConfigureSection(playerSection, popup, HeaderHeight, 360f);

        TMP_Text currentPlayerText =
            CreateInfoText(
                playerSection,
                "CurrentPlayerText",
                font,
                "Trading as: Player 1");

        TMP_Text selectedPlayerText =
            CreateInfoText(
                playerSection,
                "SelectedPlayerText",
                font,
                "Select a player");

        Transform playerButtonContainer =
            CreateScrollContainer(
                playerSection,
                "PlayerButtonContainer",
                180f);

        Button playerButtonPrefab =
            CreatePlayerButtonPrefab(font);

        Button continueButton =
            CreateActionButton(
                playerSection,
                "ContinueButton",
                "CONTINUE",
                font,
                AccentGreen);

        Transform offerSection =
            GetOrCreate(popup, "OfferBuilderSection");

        ConfigureSection(
            offerSection,
            popup,
            HeaderHeight,
            PanelHeight - HeaderHeight - 70f);

        offerSection.gameObject.SetActive(false);

        TMP_Text offerProposerLabel =
            CreateSectionLabel(
                offerSection,
                "OfferProposerLabel",
                font,
                "PLAYER 1 OFFERS");

        TMP_Text offerReceiverLabel =
            CreateSectionLabel(
                offerSection,
                "OfferReceiverLabel",
                font,
                "PLAYER 2 GIVES");

        TMP_InputField offeredMoneyInput =
            CreateMoneyRow(
                offerSection,
                "OfferedMoneyRow",
                "Money Offered",
                font,
                out Button offeredMinus,
                out Button offeredPlus);

        TMP_InputField requestedMoneyInput =
            CreateMoneyRow(
                offerSection,
                "RequestedMoneyRow",
                "Money Requested",
                font,
                out Button requestedMinus,
                out Button requestedPlus);

        Button selectOfferedPropertyButton =
            CreateActionButton(
                offerSection,
                "SelectOfferedPropertyButton",
                "SELECT OFFERED PROPERTIES",
                font,
                Navy);

        Transform offeredChipContainer =
            CreateChipContainer(
                offerSection,
                "OfferedPropertyChipContainer");

        Button selectRequestedPropertyButton =
            CreateActionButton(
                offerSection,
                "SelectRequestedPropertyButton",
                "SELECT REQUESTED PROPERTIES",
                font,
                Navy);

        Transform requestedChipContainer =
            CreateChipContainer(
                offerSection,
                "RequestedPropertyChipContainer");

        TMP_Text summaryText =
            CreateInfoText(
                offerSection,
                "SummaryText",
                font,
                "TRADE SUMMARY");

        summaryText.alignment = TextAlignmentOptions.TopLeft;
        summaryText.fontSize = 15f;

        LayoutElement summaryLayout =
            GetOrAdd<LayoutElement>(summaryText.gameObject);

        summaryLayout.preferredHeight = 130f;

        Transform offerButtonRow =
            GetOrCreate(offerSection, "OfferButtonRow");

        ConfigureHorizontalRow(offerButtonRow, 10f);

        Button backButton =
            CreateActionButton(
                offerButtonRow,
                "BackButton",
                "BACK",
                font,
                SectionColor);

        Button sendTradeButton =
            CreateActionButton(
                offerButtonRow,
                "SendTradeButton",
                "SEND TRADE",
                font,
                AccentGreen);

        Transform pickerSection =
            GetOrCreate(popup, "PropertyPickerSection");

        ConfigureSection(
            pickerSection,
            popup,
            HeaderHeight,
            PanelHeight - HeaderHeight - 70f);

        pickerSection.gameObject.SetActive(false);

        TMP_Text pickerTitle =
            CreateSectionLabel(
                pickerSection,
                "PropertyPickerTitle",
                font,
                "Select Properties");

        Transform pickerList =
            CreateScrollContainer(
                pickerSection,
                "PropertyPickerList",
                420f);

        Transform pickerButtonRow =
            GetOrCreate(pickerSection, "PropertyPickerButtonRow");

        ConfigureHorizontalRow(pickerButtonRow, 10f);

        Button pickerConfirm =
            CreateActionButton(
                pickerButtonRow,
                "PropertyPickerConfirmButton",
                "CONFIRM",
                font,
                AccentGreen);

        Button pickerCancel =
            CreateActionButton(
                pickerButtonRow,
                "PropertyPickerCancelButton",
                "CANCEL",
                font,
                AccentRed);

        TMP_Text statusText =
            CreateInfoText(
                popup,
                "StatusText",
                font,
                "Choose a player to trade with.");

        RectTransform statusRt =
            statusText.rectTransform;

        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(1f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.offsetMin = new Vector2(20f, 52f);
        statusRt.offsetMax = new Vector2(-20f, 92f);

        Button closeButton =
            CreateActionButton(
                popup,
                "CloseButton",
                "CLOSE",
                font,
                AccentRed);

        RectTransform closeRt =
            closeButton.GetComponent<RectTransform>();

        closeRt.anchorMin = new Vector2(0.5f, 0f);
        closeRt.anchorMax = new Vector2(0.5f, 0f);
        closeRt.pivot = new Vector2(0.5f, 0f);
        closeRt.sizeDelta = new Vector2(220f, ButtonHeight);
        closeRt.anchoredPosition = new Vector2(0f, 12f);

        Button propertyChipPrefab =
            CreatePropertyChipPrefab(font);

        Toggle propertyPickerTogglePrefab =
            CreatePropertyTogglePrefab(font);

        AssignTradePanelReferences(
            tradePanel,
            tradePanel.gameObject,
            popup.gameObject,
            playerSection.gameObject,
            offerSection.gameObject,
            pickerSection.gameObject,
            headerText,
            currentPlayerText,
            selectedPlayerText,
            statusText,
            playerButtonContainer,
            playerButtonPrefab,
            offerProposerLabel,
            offerReceiverLabel,
            offeredMoneyInput,
            requestedMoneyInput,
            offeredMinus,
            offeredPlus,
            requestedMinus,
            requestedPlus,
            offeredChipContainer,
            requestedChipContainer,
            selectOfferedPropertyButton,
            selectRequestedPropertyButton,
            summaryText,
            pickerList,
            pickerTitle,
            pickerConfirm,
            pickerCancel,
            continueButton,
            backButton,
            sendTradeButton,
            closeButton,
            propertyChipPrefab,
            propertyPickerTogglePrefab);

        popup.gameObject.SetActive(false);
        tradePanel.gameObject.SetActive(false);

        TradeRequestPanel requestPanel =
            EnsureTradeRequestPanel(
                root.parent,
                font,
                slicedSprite);

        CreateTradeOpenButton(root.parent, font);

        EditorUtility.SetDirty(tradePanel);
        EditorUtility.SetDirty(requestPanel);

        EditorSceneManager.MarkSceneDirty(
            tradePanel.gameObject.scene);

        Debug.Log(
            "TradePanelSetup: Trade UI successfully built."
        );
    }

    // ============================================================
    // TRADE MANAGER
    // ============================================================

    private static void EnsureTradeManager()
    {
        TradeManager existing =
            Object.FindFirstObjectByType<TradeManager>(
                FindObjectsInactive.Include);

        if (existing != null)
            return;

        GameManager gameManager =
            Object.FindFirstObjectByType<GameManager>();

        if (gameManager == null)
        {
            Debug.LogWarning(
                "TradePanelSetup: GameManager not found. " +
                "TradeManager was not created."
            );

            return;
        }

        Undo.AddComponent<TradeManager>(
            gameManager.gameObject);
    }

    // ============================================================
    // TRADE REQUEST PANEL
    // ============================================================

    private static TradeRequestPanel EnsureTradeRequestPanel(
        Transform canvasRoot,
        TMP_FontAsset font,
        Sprite slicedSprite)
    {
        TradeRequestPanel existing =
            Object.FindFirstObjectByType<TradeRequestPanel>(
                FindObjectsInactive.Include);

        if (existing != null)
        {
            RebuildTradeRequestPanel(
                existing,
                font,
                slicedSprite);

            return existing;
        }

        GameObject rootObject =
            new GameObject(
                "TradeRequestPanel",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(TradeRequestPanel));

        Undo.RegisterCreatedObjectUndo(
            rootObject,
            "Create TradeRequestPanel");

        Transform parent =
            canvasRoot != null
                ? canvasRoot
                : Object.FindFirstObjectByType<Canvas>()?.transform;

        rootObject.transform.SetParent(parent, false);

        TradeRequestPanel panel =
            rootObject.GetComponent<TradeRequestPanel>();

        RebuildTradeRequestPanel(
            panel,
            font,
            slicedSprite);

        return panel;
    }

    private static void RebuildTradeRequestPanel(
        TradeRequestPanel panelComponent,
        TMP_FontAsset font,
        Sprite slicedSprite)
    {
        Transform root = panelComponent.transform;

        ConfigureFullScreenRoot(root);

        Image overlay =
            GetOrAdd<Image>(root.gameObject);

        overlay.color = OverlayColor;
        overlay.raycastTarget = true;

        Transform popup =
            GetOrCreate(root, "PopupBackground");

        ConfigurePopup(popup, slicedSprite, 620f, 720f);

        Transform header = GetOrCreate(popup, "Header");
        ConfigureBar(header, HeaderHeight, Magenta);

        TMP_Text headerText =
            GetOrCreateTMP(header, "HeaderText", font, 28f, Color.white);

        headerText.text = "TRADE REQUEST";
        headerText.alignment = TextAlignmentOptions.Center;
        Stretch(headerText.rectTransform, 12f, 12f, 0f, 0f);

        Transform content =
            GetOrCreate(popup, "Content");

        ConfigureScrollSection(
            content,
            popup,
            HeaderHeight,
            150f);

        TMP_Text messageText =
            CreateInfoText(content, "MessageText", font, "Player wants to trade.");

        TMP_Text offeredText =
            CreateInfoText(content, "OfferedText", font, "OFFERS:");

        offeredText.alignment = TextAlignmentOptions.TopLeft;

        TMP_Text requestedText =
            CreateInfoText(content, "RequestedText", font, "REQUESTS:");

        requestedText.alignment = TextAlignmentOptions.TopLeft;

        TMP_Text statusText =
            CreateInfoText(content, "StatusText", font, "Review the offer.");

        Transform buttonRow =
            GetOrCreate(popup, "ButtonRow");

        RectTransform buttonRowRt =
            buttonRow.GetComponent<RectTransform>();

        buttonRowRt.anchorMin = new Vector2(0f, 0f);
        buttonRowRt.anchorMax = new Vector2(1f, 0f);
        buttonRowRt.pivot = new Vector2(0.5f, 0f);
        buttonRowRt.offsetMin = new Vector2(20f, 16f);
        buttonRowRt.offsetMax = new Vector2(-20f, 16f + ButtonHeight);

        ConfigureHorizontalRow(buttonRow, 12f);

        Button acceptButton =
            CreateActionButton(
                buttonRow,
                "AcceptButton",
                "ACCEPT",
                font,
                AccentGreen);

        Button declineButton =
            CreateActionButton(
                buttonRow,
                "DeclineButton",
                "DECLINE",
                font,
                AccentRed);

        Button closeButton =
            CreateActionButton(
                buttonRow,
                "CloseButton",
                "CLOSE",
                font,
                Navy);

        AssignTradeRequestReferences(
            panelComponent,
            root.gameObject,
            popup.gameObject,
            headerText,
            messageText,
            offeredText,
            requestedText,
            statusText,
            acceptButton,
            declineButton,
            closeButton);

        popup.gameObject.SetActive(false);
        root.gameObject.SetActive(false);
    }

    // ============================================================
    // TRADE OPEN BUTTON
    // ============================================================

    private static void CreateTradeOpenButton(
        Transform canvasRoot,
        TMP_FontAsset font)
    {
        Transform existing =
            FindDeepChild(canvasRoot, "TradeOpenButton");

        if (existing != null)
            return;

        Transform parent =
            canvasRoot != null
                ? canvasRoot
                : Object.FindFirstObjectByType<Canvas>()?.transform;

        if (parent == null)
            return;

        GameObject buttonObject =
            new GameObject(
                "TradeOpenButton",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button),
                typeof(TradeOpenButton));

        Undo.RegisterCreatedObjectUndo(
            buttonObject,
            "Create Trade Open Button");

        buttonObject.transform.SetParent(parent, false);

        RectTransform rt =
            buttonObject.GetComponent<RectTransform>();

        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(1f, 0f);
        rt.sizeDelta = new Vector2(160f, 48f);
        rt.anchoredPosition = new Vector2(-20f, 20f);

        Image image =
            buttonObject.GetComponent<Image>();

        image.color = Navy;

        Button button =
            buttonObject.GetComponent<Button>();

        button.targetGraphic = image;

        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        textObject.transform.SetParent(buttonObject.transform, false);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.text = "TRADE";
        text.fontSize = 18f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        Stretch(text.rectTransform, 0f, 0f, 0f, 0f);
    }

    // ============================================================
    // UI BUILD HELPERS
    // ============================================================

    private static void ConfigureFullScreenRoot(Transform root)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(root.gameObject);

        Stretch(rt, 0f, 0f, 0f, 0f);
    }

    private static void ConfigurePopup(
        Transform popup,
        Sprite sprite,
        float width = PanelWidth,
        float height = PanelHeight)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(popup.gameObject);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(width, height);

        Image image =
            GetOrAdd<Image>(popup.gameObject);

        image.color = CardColor;
        image.raycastTarget = true;
        image.sprite = sprite;
        image.type = Image.Type.Sliced;
    }

    private static void ConfigureBar(
        Transform bar,
        float height,
        Color color)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(bar.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;

        Image image =
            GetOrAdd<Image>(bar.gameObject);

        image.color = color;
        image.raycastTarget = true;
    }

    private static void ConfigureSection(
        Transform section,
        Transform popup,
        float topOffset,
        float height)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(section.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(16f, -(topOffset + height));
        rt.offsetMax = new Vector2(-16f, -topOffset);

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(section.gameObject);

        layout.spacing = 8f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void ConfigureScrollSection(
        Transform section,
        Transform popup,
        float topOffset,
        float bottomOffset)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(section.gameObject);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(16f, bottomOffset);
        rt.offsetMax = new Vector2(-16f, -(topOffset + 8f));

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(section.gameObject);

        layout.spacing = 10f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static TMP_Text CreateInfoText(
        Transform parent,
        string name,
        TMP_FontAsset font,
        string placeholder)
    {
        TMP_Text text =
            GetOrCreateTMP(
                parent,
                name,
                font,
                16f,
                TextColor);

        text.text = placeholder;
        text.alignment = TextAlignmentOptions.Center;

        LayoutElement layout =
            GetOrAdd<LayoutElement>(text.gameObject);

        layout.preferredHeight = RowHeight;
        layout.minHeight = RowHeight;

        return text;
    }

    private static TMP_Text CreateSectionLabel(
        Transform parent,
        string name,
        TMP_FontAsset font,
        string label)
    {
        Transform labelRoot =
            GetOrCreate(parent, name);

        Image bg =
            GetOrAdd<Image>(labelRoot.gameObject);

        bg.color = SectionColor;

        LayoutElement layout =
            GetOrAdd<LayoutElement>(labelRoot.gameObject);

        layout.preferredHeight = SectionHeaderHeight;

        TMP_Text text =
            GetOrCreateTMP(
                labelRoot,
                "Text",
                font,
                15f,
                Color.white);

        text.text = label;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;

        Stretch(text.rectTransform, 8f, 8f, 0f, 0f);

        return text;
    }

    private static Transform CreateScrollContainer(
        Transform parent,
        string name,
        float height)
    {
        Transform scrollRoot =
            GetOrCreate(parent, name);

        LayoutElement layout =
            GetOrAdd<LayoutElement>(scrollRoot.gameObject);

        layout.preferredHeight = height;
        layout.minHeight = height;

        RectTransform scrollRt =
            GetOrAdd<RectTransform>(scrollRoot.gameObject);

        scrollRt.sizeDelta = new Vector2(0f, height);

        ScrollRect scroll =
            GetOrAdd<ScrollRect>(scrollRoot.gameObject);

        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;

        Transform viewport =
            GetOrCreate(scrollRoot, "Viewport");

        Stretch(viewport.GetComponent<RectTransform>(), 0f, 0f, 0f, 0f);

        Image viewportImage =
            GetOrAdd<Image>(viewport.gameObject);

        viewportImage.color =
            new Color(0f, 0f, 0f, 0.15f);

        Mask mask =
            GetOrAdd<Mask>(viewport.gameObject);

        mask.showMaskGraphic = false;

        Transform content =
            GetOrCreate(viewport, "Content");

        RectTransform contentRt =
            content.GetComponent<RectTransform>();

        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = new Vector2(0f, 0f);

        VerticalLayoutGroup contentLayout =
            GetOrAdd<VerticalLayoutGroup>(content.gameObject);

        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(6, 6, 6, 6);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            GetOrAdd<ContentSizeFitter>(content.gameObject);

        fitter.verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        scroll.viewport =
            viewport.GetComponent<RectTransform>();

        scroll.content = contentRt;

        return content;
    }

    private static Transform CreateChipContainer(
        Transform parent,
        string name)
    {
        Transform container =
            GetOrCreate(parent, name);

        LayoutElement layout =
            GetOrAdd<LayoutElement>(container.gameObject);

        layout.preferredHeight = 72f;
        layout.minHeight = 72f;

        VerticalLayoutGroup vertical =
            GetOrAdd<VerticalLayoutGroup>(container.gameObject);

        vertical.spacing = 4f;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        return container;
    }

    private static TMP_InputField CreateMoneyRow(
        Transform parent,
        string rowName,
        string label,
        TMP_FontAsset font,
        out Button minusButton,
        out Button plusButton)
    {
        Transform row =
            GetOrCreate(parent, rowName);

        ConfigureHorizontalRow(row, 8f);

        LayoutElement rowLayout =
            GetOrAdd<LayoutElement>(row.gameObject);

        rowLayout.preferredHeight = RowHeight + 8f;

        TMP_Text labelText =
            GetOrCreateTMP(row, "Label", font, 15f, TextColor);

        labelText.text = label;
        labelText.alignment = TextAlignmentOptions.MidlineLeft;

        LayoutElement labelLayout =
            GetOrAdd<LayoutElement>(labelText.gameObject);

        labelLayout.preferredWidth = 170f;
        labelLayout.flexibleWidth = 0f;

        minusButton =
            CreateSmallButton(row, "MinusButton", "-", font);

        TMP_InputField input =
            CreateMoneyInput(row, font);

        plusButton =
            CreateSmallButton(row, "PlusButton", "+", font);

        return input;
    }

    private static TMP_InputField CreateMoneyInput(
        Transform parent,
        TMP_FontAsset font)
    {
        GameObject inputRoot =
            new GameObject(
                "MoneyInput",
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField),
                typeof(LayoutElement));

        inputRoot.transform.SetParent(parent, false);

        LayoutElement layout =
            inputRoot.GetComponent<LayoutElement>();

        layout.flexibleWidth = 1f;
        layout.preferredHeight = RowHeight;

        Image bg =
            inputRoot.GetComponent<Image>();

        bg.color =
            new Color(0.08f, 0.10f, 0.14f, 1f);

        TMP_InputField input =
            inputRoot.GetComponent<TMP_InputField>();

        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        textObject.transform.SetParent(inputRoot.transform, false);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = 16f;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineRight;

        Stretch(text.rectTransform, 10f, 10f, 4f, 4f);

        input.textComponent = text;
        input.text = "0";
        input.contentType = TMP_InputField.ContentType.IntegerNumber;

        return input;
    }

    private static Button CreateSmallButton(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset font)
    {
        Button button =
            CreateActionButton(
                parent,
                name,
                label,
                font,
                SectionColor);

        LayoutElement layout =
            GetOrAdd<LayoutElement>(button.gameObject);

        layout.preferredWidth = SmallButtonWidth;
        layout.flexibleWidth = 0f;

        return button;
    }

    private static Button CreateActionButton(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset font,
        Color color)
    {
        Transform existing =
            FindDeepChild(parent, name);

        GameObject buttonObject;

        if (existing != null)
        {
            buttonObject = existing.gameObject;
        }
        else
        {
            buttonObject =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));

            buttonObject.transform.SetParent(parent, false);
        }

        LayoutElement layout =
            GetOrAdd<LayoutElement>(buttonObject);

        layout.preferredHeight = ButtonHeight;
        layout.minHeight = ButtonHeight;
        layout.flexibleWidth = 1f;

        Image image =
            GetOrAdd<Image>(buttonObject);

        image.color = color;

        Button button =
            GetOrAdd<Button>(buttonObject);

        button.targetGraphic = image;

        TextMeshProUGUI text =
            GetOrCreateTMP(
                buttonObject.transform,
                "Text",
                font,
                16f,
                Color.white);

        text.text = label;
        text.alignment = TextAlignmentOptions.Center;

        Stretch(text.rectTransform, 8f, 8f, 0f, 0f);

        return button;
    }

    private static Button CreatePlayerButtonPrefab(
        TMP_FontAsset font)
    {
        const string PrefabPath =
            "Assets/Prefabs/TradePlayerButton.prefab";

        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);

        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
        {
            AssetDatabase.CreateFolder("Assets", "Prefabs");
        }

        GameObject prefabRoot =
            new GameObject(
                "TradePlayerButton",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

        LayoutElement layout =
            prefabRoot.GetComponent<LayoutElement>();

        layout.preferredHeight = 52f;

        Image image =
            prefabRoot.GetComponent<Image>();

        image.color = Navy;

        Button button =
            prefabRoot.GetComponent<Button>();

        button.targetGraphic = image;

        TextMeshProUGUI text =
            GetOrCreateTMP(
                prefabRoot.transform,
                "Text",
                font,
                16f,
                Color.white);

        text.alignment = TextAlignmentOptions.Center;
        Stretch(text.rectTransform, 8f, 8f, 0f, 0f);

        PrefabUtility.SaveAsPrefabAsset(
            prefabRoot,
            PrefabPath);

        Object.DestroyImmediate(prefabRoot);

        return AssetDatabase
            .LoadAssetAtPath<GameObject>(PrefabPath)
            .GetComponent<Button>();
    }

    private static Button CreatePropertyChipPrefab(
        TMP_FontAsset font)
    {
        const string PrefabPath =
            "Assets/Prefabs/TradePropertyChip.prefab";

        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);

        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        GameObject prefabRoot =
            new GameObject(
                "TradePropertyChip",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

        LayoutElement layout =
            prefabRoot.GetComponent<LayoutElement>();

        layout.preferredHeight = 30f;

        Image image =
            prefabRoot.GetComponent<Image>();

        image.color = Magenta;

        Button button =
            prefabRoot.GetComponent<Button>();

        button.targetGraphic = image;

        TextMeshProUGUI text =
            GetOrCreateTMP(
                prefabRoot.transform,
                "Text",
                font,
                14f,
                Color.white);

        text.alignment = TextAlignmentOptions.MidlineLeft;
        Stretch(text.rectTransform, 8f, 8f, 0f, 0f);

        PrefabUtility.SaveAsPrefabAsset(
            prefabRoot,
            PrefabPath);

        Object.DestroyImmediate(prefabRoot);

        return AssetDatabase
            .LoadAssetAtPath<GameObject>(PrefabPath)
            .GetComponent<Button>();
    }

    private static Toggle CreatePropertyTogglePrefab(
        TMP_FontAsset font)
    {
        const string PrefabPath =
            "Assets/Prefabs/TradePropertyToggle.prefab";

        GameObject existing =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                PrefabPath);

        if (existing != null)
        {
            return existing.GetComponent<Toggle>();
        }

        GameObject prefabRoot =
            new GameObject(
                "TradePropertyToggle",
                typeof(RectTransform),
                typeof(Toggle),
                typeof(LayoutElement));

        LayoutElement layout =
            prefabRoot.GetComponent<LayoutElement>();

        layout.preferredHeight = 34f;

        HorizontalLayoutGroup horizontal =
            prefabRoot.AddComponent<HorizontalLayoutGroup>();

        horizontal.spacing = 8f;
        horizontal.childAlignment = TextAnchor.MiddleLeft;
        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;
        horizontal.childForceExpandWidth = true;

        GameObject background =
            new GameObject(
                "Background",
                typeof(RectTransform),
                typeof(Image));

        background.transform.SetParent(prefabRoot.transform, false);

        LayoutElement bgLayout =
            background.AddComponent<LayoutElement>();

        bgLayout.preferredWidth = 24f;
        bgLayout.preferredHeight = 24f;

        Image bgImage =
            background.GetComponent<Image>();

        bgImage.color = SectionColor;

        GameObject checkmark =
            new GameObject(
                "Checkmark",
                typeof(RectTransform),
                typeof(Image));

        checkmark.transform.SetParent(background.transform, false);

        Image checkImage =
            checkmark.GetComponent<Image>();

        checkImage.color = AccentGreen;
        Stretch(checkmark.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);

        GameObject labelObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        labelObject.transform.SetParent(prefabRoot.transform, false);

        TextMeshProUGUI label =
            labelObject.GetComponent<TextMeshProUGUI>();

        if (font != null)
            label.font = font;

        label.fontSize = 14f;
        label.color = TextColor;
        label.alignment = TextAlignmentOptions.MidlineLeft;
        label.text = "Property";

        Toggle toggle =
            prefabRoot.GetComponent<Toggle>();

        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;

        PrefabUtility.SaveAsPrefabAsset(
            prefabRoot,
            PrefabPath);

        Object.DestroyImmediate(prefabRoot);

        return AssetDatabase
            .LoadAssetAtPath<GameObject>(PrefabPath)
            .GetComponent<Toggle>();
    }

    private static void ConfigureHorizontalRow(
        Transform row,
        float spacing)
    {
        HorizontalLayoutGroup layout =
            GetOrAdd<HorizontalLayoutGroup>(row.gameObject);

        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    // ============================================================
    // SERIALIZED ASSIGNMENTS
    // ============================================================

    private static void AssignTradePanelReferences(
        TradePanel panel,
        GameObject rootPanel,
        GameObject popup,
        GameObject playerSection,
        GameObject offerSection,
        GameObject pickerSection,
        TMP_Text headerText,
        TMP_Text currentPlayerText,
        TMP_Text selectedPlayerText,
        TMP_Text statusText,
        Transform playerButtonContainer,
        Button playerButtonPrefab,
        TMP_Text offerProposerLabel,
        TMP_Text offerReceiverLabel,
        TMP_InputField offeredMoneyInput,
        TMP_InputField requestedMoneyInput,
        Button offeredMinus,
        Button offeredPlus,
        Button requestedMinus,
        Button requestedPlus,
        Transform offeredChipContainer,
        Transform requestedChipContainer,
        Button selectOfferedPropertyButton,
        Button selectRequestedPropertyButton,
        TMP_Text summaryText,
        Transform pickerList,
        TMP_Text pickerTitle,
        Button pickerConfirm,
        Button pickerCancel,
        Button continueButton,
        Button backButton,
        Button sendTradeButton,
        Button closeButton,
        Button propertyChipPrefab,
        Toggle propertyPickerTogglePrefab)
    {
        SerializedObject serialized =
            new SerializedObject(panel);

        SetObject(serialized, "panel", rootPanel);
        SetObject(serialized, "playerSelectionSection", playerSection);
        SetObject(serialized, "offerBuilderSection", offerSection);
        SetObject(serialized, "propertyPickerSection", pickerSection);
        SetObject(serialized, "headerText", headerText);
        SetObject(serialized, "currentPlayerText", currentPlayerText);
        SetObject(serialized, "selectedPlayerText", selectedPlayerText);
        SetObject(serialized, "statusText", statusText);
        SetObject(serialized, "playerButtonContainer", playerButtonContainer);
        SetObject(serialized, "playerButtonPrefab", playerButtonPrefab);
        SetObject(serialized, "offerProposerLabelText", offerProposerLabel);
        SetObject(serialized, "offerReceiverLabelText", offerReceiverLabel);
        SetObject(serialized, "offeredMoneyInput", offeredMoneyInput);
        SetObject(serialized, "requestedMoneyInput", requestedMoneyInput);
        SetObject(serialized, "offeredMoneyMinusButton", offeredMinus);
        SetObject(serialized, "offeredMoneyPlusButton", offeredPlus);
        SetObject(serialized, "requestedMoneyMinusButton", requestedMinus);
        SetObject(serialized, "requestedMoneyPlusButton", requestedPlus);
        SetObject(serialized, "offeredPropertyChipContainer", offeredChipContainer);
        SetObject(serialized, "requestedPropertyChipContainer", requestedChipContainer);
        SetObject(serialized, "selectOfferedPropertyButton", selectOfferedPropertyButton);
        SetObject(serialized, "selectRequestedPropertyButton", selectRequestedPropertyButton);
        SetObject(serialized, "summaryText", summaryText);
        SetObject(serialized, "propertyPickerListContainer", pickerList);
        SetObject(serialized, "propertyPickerTitleText", pickerTitle);
        SetObject(serialized, "propertyPickerConfirmButton", pickerConfirm);
        SetObject(serialized, "propertyPickerCancelButton", pickerCancel);
        SetObject(serialized, "continueButton", continueButton);
        SetObject(serialized, "backButton", backButton);
        SetObject(serialized, "sendTradeButton", sendTradeButton);
        SetObject(serialized, "closeButton", closeButton);
        SetObject(serialized, "propertyChipPrefab", propertyChipPrefab);
        SetObject(serialized, "propertyPickerTogglePrefab", propertyPickerTogglePrefab);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void AssignTradeRequestReferences(
        TradeRequestPanel panel,
        GameObject rootPanel,
        GameObject popup,
        TMP_Text headerText,
        TMP_Text messageText,
        TMP_Text offeredText,
        TMP_Text requestedText,
        TMP_Text statusText,
        Button acceptButton,
        Button declineButton,
        Button closeButton)
    {
        SerializedObject serialized =
            new SerializedObject(panel);

        SetObject(serialized, "panel", rootPanel);
        SetObject(serialized, "headerText", headerText);
        SetObject(serialized, "messageText", messageText);
        SetObject(serialized, "offeredText", offeredText);
        SetObject(serialized, "requestedText", requestedText);
        SetObject(serialized, "statusText", statusText);
        SetObject(serialized, "acceptButton", acceptButton);
        SetObject(serialized, "declineButton", declineButton);
        SetObject(serialized, "closeButton", closeButton);

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(
        SerializedObject serialized,
        string field,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(field);

        if (property != null)
            property.objectReferenceValue = value;
    }

    // ============================================================
    // SHARED HELPERS
    // ============================================================

    private static TMP_FontAsset GetFont()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "Liberation Sans SDF t:TMP_FontAsset");

        if (guids.Length > 0)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guids[0]);

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        return Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF");
    }

    private static Sprite GetSlicedSprite()
    {
        return AssetDatabase.GetBuiltinExtraResource<Sprite>(
            "UI/Skin/UISprite.psd");
    }

    private static Transform GetOrCreate(
        Transform parent,
        string name)
    {
        Transform existing = FindDirectChild(parent, name);

        if (existing != null)
            return existing;

        GameObject created =
            new GameObject(name, typeof(RectTransform));

        created.transform.SetParent(parent, false);
        return created.transform;
    }

    private static TextMeshProUGUI GetOrCreateTMP(
        Transform parent,
        string name,
        TMP_FontAsset font,
        float size,
        Color color)
    {
        Transform existing = FindDirectChild(parent, name);

        GameObject obj;

        if (existing != null)
        {
            obj = existing.gameObject;
        }
        else
        {
            obj =
                new GameObject(
                    name,
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));

            obj.transform.SetParent(parent, false);
        }

        TextMeshProUGUI text =
            GetOrAdd<TextMeshProUGUI>(obj);

        if (font != null)
            text.font = font;

        text.fontSize = size;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;

        return text;
    }

    private static void Stretch(
        RectTransform rt,
        float left,
        float right,
        float bottom,
        float top)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(left, bottom);
        rt.offsetMax = new Vector2(-right, -top);
        rt.localScale = Vector3.one;
    }

    private static T GetOrAdd<T>(GameObject obj)
        where T : Component
    {
        T component = obj.GetComponent<T>();
        return component != null
            ? component
            : obj.AddComponent<T>();
    }

    private static Transform FindDirectChild(
        Transform parent,
        string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child.name == name)
                return child;
        }

        return null;
    }

    private static Transform FindDeepChild(
        Transform parent,
        string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform result =
                FindDeepChild(parent.GetChild(i), name);

            if (result != null)
                return result;
        }

        return null;
    }
}
