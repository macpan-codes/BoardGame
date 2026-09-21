using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds a clean, editable visual template for the existing trade system.
///
/// IMPORTANT:
/// - This file does NOT replace TradePanel.cs or TradeManager.cs.
/// - Existing trade logic remains intact.
/// - The generated hierarchy keeps the names required by TradePanel.cs and
///   TradeRequestPanel.cs.
/// - Run once, then redesign the generated UI/prefabs however you want.
///
/// Menu:
/// Tools -> Board Game -> Build Trade UI Template
/// </summary>
public static class TradeUIVisualTemplateSetup
{
    private const float TradeWidth = 920f;
    private const float TradeHeight = 920f;
    private const float RequestWidth = 760f;
    private const float RequestHeight = 760f;

    private const float HeaderHeight = 86f;
    private const float ButtonHeight = 56f;
    private const float FooterHeight = 80f;

    private static readonly Color Overlay =
        new Color(0f, 0f, 0f, 0.52f);

    private static readonly Color Card =
        new Color(0.055f, 0.075f, 0.12f, 1f);

    private static readonly Color Header =
        new Color(0.075f, 0.105f, 0.17f, 1f);

    private static readonly Color Section =
        new Color(0.09f, 0.135f, 0.21f, 1f);

    private static readonly Color Field =
        new Color(0.065f, 0.095f, 0.15f, 1f);

    private static readonly Color White =
        new Color(0.96f, 0.98f, 1f, 1f);

    private static readonly Color Muted =
        new Color(0.62f, 0.69f, 0.79f, 1f);

    private static readonly Color Blue =
        new Color(0.18f, 0.52f, 0.95f, 1f);

    private static readonly Color Green =
        new Color(0.12f, 0.65f, 0.39f, 1f);
    private static readonly Color Red =
        new Color(0.70f, 0.20f, 0.24f, 1f);
    private static readonly Color Navy =
        new Color(0.055f, 0.075f, 0.12f, 1f);
    private static readonly Color Gold =
        new Color(0.90f, 0.67f, 0.20f, 1f);

    [MenuItem("Tools/Board Game/Build Trade UI Template")]
    public static void BuildTradeUITemplate()
    {
        Canvas canvas =
            Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        TradePanel tradePanel =
            Object.FindFirstObjectByType<TradePanel>(FindObjectsInactive.Include);

        if (canvas == null)
        {
            Debug.LogError("TradeUIVisualTemplateSetup: Canvas not found.");
            return;
        }

        if (tradePanel == null)
        {
            Debug.LogError("TradeUIVisualTemplateSetup: TradePanel not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            canvas.gameObject,
            "Build Trade UI Template");

        TMP_FontAsset font = GetFont();
        Sprite sliced = GetSlicedSprite();

        BuildTradePanel(tradePanel, font, sliced);

        TradeRequestPanel requestPanel =
            Object.FindFirstObjectByType<TradeRequestPanel>(
                FindObjectsInactive.Include);

        if (requestPanel == null)
            requestPanel = CreateRequestPanel(canvas.transform, font, sliced);
        else
            BuildRequestPanel(requestPanel, font, sliced);

        AssignTradePrefabs(tradePanel);
        BuildTradeOpenButton(canvas.transform, font);

        EditorUtility.SetDirty(tradePanel);
        EditorUtility.SetDirty(requestPanel);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log(
            "TradeUIVisualTemplateSetup: Trade UI template built successfully.");
    }

    // ============================================================
    // OUTGOING TRADE PANEL
    // ============================================================

    private static void BuildTradePanel(
        TradePanel tradePanel,
        TMP_FontAsset font,
        Sprite sliced)
    {
        Transform root = tradePanel.transform;
        ConfigureOverlayRoot(root);

        Image overlay = GetOrAdd<Image>(root.gameObject);
        overlay.color = Overlay;
        overlay.raycastTarget = true;

        Transform popup = GetOrCreate(root, "PopupBackground");
        ClearChildren(popup);
        ConfigurePopup(popup, sliced, TradeWidth, TradeHeight);

        Transform header = CreateRect(popup, "Header");
        ConfigureHeader(header, HeaderHeight);

        TMP_Text headerText = CreateText(
            header, "HeaderText", font, 28f, White,
            TextAlignmentOptions.Center);
        headerText.text = "TRADE";
        headerText.fontStyle = FontStyles.Bold;
        Stretch(headerText.rectTransform, 18f, 18f, 6f, 6f);

        // PLAYER SELECTION
        Transform playerSection = CreateRect(
            popup, "PlayerSelectionSection");
        SetSection(playerSection, HeaderHeight + 10f, FooterHeight + 10f);

        TMP_Text playerTitle = CreateSectionTitle(
            playerSection, "PlayerSelectionTitle", font, "SELECT PLAYER");

        TMP_Text currentPlayerText = CreateInfoText(
            playerSection, "CurrentPlayerText", font,
            "Trading as: Player 1");

        TMP_Text selectedPlayerText = CreateInfoText(
            playerSection, "SelectedPlayerText", font,
            "Select a player");

        Transform playerList = CreateScrollList(
            playerSection, "PlayerButtonContainer", 540f);

        Button continueButton = CreateActionButton(
            playerSection, "ContinueButton", "CONTINUE", font, Blue, 180f);

        // OFFER BUILDER
        Transform offerSection = CreateRect(
            popup, "OfferBuilderSection");
        SetSection(offerSection, HeaderHeight + 10f, FooterHeight + 10f);
        offerSection.gameObject.SetActive(false);

        CreateSectionTitle(
            offerSection, "OfferBuilderTitle", font, "BUILD TRADE OFFER");

        TMP_Text proposerLabel = CreateSectionTitle(
            offerSection, "OfferProposerLabel", font, "PLAYER 1 OFFERS");

        TMP_Text receiverLabel = CreateSectionTitle(
            offerSection, "OfferReceiverLabel", font, "PLAYER 2 GIVES");

        TMP_InputField offeredMoney = CreateMoneyRow(
            offerSection, "OfferedMoneyRow", "MONEY OFFERED", font,
            out Button offeredMinus, out Button offeredPlus);

        TMP_InputField requestedMoney = CreateMoneyRow(
            offerSection, "RequestedMoneyRow", "MONEY REQUESTED", font,
            out Button requestedMinus, out Button requestedPlus);

        Button selectOffered = CreateActionButton(
            offerSection, "SelectOfferedPropertyButton",
            "SELECT OFFERED PROPERTIES", font, Section, 0f);

        Transform offeredChips = CreateChipContainer(
            offerSection, "OfferedPropertyChipContainer");

        Button selectRequested = CreateActionButton(
            offerSection, "SelectRequestedPropertyButton",
            "SELECT REQUESTED PROPERTIES", font, Section, 0f);

        Transform requestedChips = CreateChipContainer(
            offerSection, "RequestedPropertyChipContainer");

        TMP_Text summary = CreateInfoText(
            offerSection, "SummaryText", font,
            "TRADE SUMMARY\n\nNothing selected.");
        summary.alignment = TextAlignmentOptions.TopLeft;

        AddPreferredHeight(summary.gameObject, 150f);

        Transform offerButtons = CreateHorizontalRow(
            offerSection, "OfferButtonRow");

        Button back = CreateActionButton(
            offerButtons, "BackButton", "BACK", font, Section, 150f);

        Button send = CreateActionButton(
            offerButtons, "SendTradeButton", "SEND TRADE", font, Green, 210f);

        // PROPERTY PICKER
        Transform pickerSection = CreateRect(
            popup, "PropertyPickerSection");
        SetSection(pickerSection, HeaderHeight + 10f, FooterHeight + 10f);
        pickerSection.gameObject.SetActive(false);

        TMP_Text pickerTitle = CreateSectionTitle(
            pickerSection, "PropertyPickerTitle", font, "SELECT PROPERTIES");

        Transform pickerList = CreateScrollList(
            pickerSection, "PropertyPickerList", 620f);

        Transform pickerButtons = CreateHorizontalRow(
            pickerSection, "PropertyPickerButtonRow");

        Button pickerConfirm = CreateActionButton(
            pickerButtons, "PropertyPickerConfirmButton",
            "CONFIRM", font, Green, 190f);

        Button pickerCancel = CreateActionButton(
            pickerButtons, "PropertyPickerCancelButton",
            "CANCEL", font, Red, 190f);

        TMP_Text statusText = CreateText(
            popup, "StatusText", font, 14f, Muted,
            TextAlignmentOptions.Center);
        statusText.text = "Choose a player to trade with.";

        RectTransform statusRt = statusText.rectTransform;
        statusRt.anchorMin = new Vector2(0f, 0f);
        statusRt.anchorMax = new Vector2(1f, 0f);
        statusRt.pivot = new Vector2(0.5f, 0f);
        statusRt.offsetMin = new Vector2(24f, 46f);
        statusRt.offsetMax = new Vector2(-24f, 78f);

        Button close = CreateActionButton(
            popup, "CloseButton", "CLOSE", font, Red, 120f);

        RectTransform closeRt = close.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(1f, 1f);
        closeRt.anchorMax = new Vector2(1f, 1f);
        closeRt.pivot = new Vector2(1f, 1f);
        closeRt.anchoredPosition = new Vector2(-16f, -14f);
        closeRt.sizeDelta = new Vector2(120f, 42f);

        SerializedObject so = new SerializedObject(tradePanel);

        SetObject(so, "panel", popup.gameObject);
        SetObject(so, "playerSelectionSection", playerSection.gameObject);
        SetObject(so, "offerBuilderSection", offerSection.gameObject);
        SetObject(so, "propertyPickerSection", pickerSection.gameObject);
        SetObject(so, "headerText", headerText);
        SetObject(so, "currentPlayerText", currentPlayerText);
        SetObject(so, "selectedPlayerText", selectedPlayerText);
        SetObject(so, "statusText", statusText);
        SetObject(so, "playerButtonContainer", playerList);
        SetObject(so, "offerProposerLabelText", proposerLabel);
        SetObject(so, "offerReceiverLabelText", receiverLabel);
        SetObject(so, "offeredMoneyInput", offeredMoney);
        SetObject(so, "requestedMoneyInput", requestedMoney);
        SetObject(so, "offeredMoneyMinusButton", offeredMinus);
        SetObject(so, "offeredMoneyPlusButton", offeredPlus);
        SetObject(so, "requestedMoneyMinusButton", requestedMinus);
        SetObject(so, "requestedMoneyPlusButton", requestedPlus);
        SetObject(so, "offeredPropertyChipContainer", offeredChips);
        SetObject(so, "requestedPropertyChipContainer", requestedChips);
        SetObject(so, "selectOfferedPropertyButton", selectOffered);
        SetObject(so, "selectRequestedPropertyButton", selectRequested);
        SetObject(so, "summaryText", summary);
        SetObject(so, "propertyPickerListContainer", pickerList);
        SetObject(so, "propertyPickerTitleText", pickerTitle);
        SetObject(so, "propertyPickerConfirmButton", pickerConfirm);
        SetObject(so, "propertyPickerCancelButton", pickerCancel);
        SetObject(so, "continueButton", continueButton);
        SetObject(so, "backButton", back);
        SetObject(so, "sendTradeButton", send);
        SetObject(so, "closeButton", close);
        so.ApplyModifiedPropertiesWithoutUndo();

        popup.gameObject.SetActive(false);
    }

    // ============================================================
    // INCOMING REQUEST PANEL
    // ============================================================

    private static TradeRequestPanel CreateRequestPanel(
        Transform canvas,
        TMP_FontAsset font,
        Sprite sliced)
    {
        GameObject obj = new GameObject(
            "TradeRequestPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(TradeRequestPanel));

        obj.transform.SetParent(canvas, false);

        TradeRequestPanel panel =
            obj.GetComponent<TradeRequestPanel>();

        BuildRequestPanel(panel, font, sliced);
        return panel;
    }

    private static void BuildRequestPanel(
        TradeRequestPanel panel,
        TMP_FontAsset font,
        Sprite sliced)
    {
        Transform root = panel.transform;
        ConfigureOverlayRoot(root);

        Image overlay = GetOrAdd<Image>(root.gameObject);
        overlay.color = Overlay;
        overlay.raycastTarget = true;

        Transform popup = GetOrCreate(root, "PopupBackground");
        ClearChildren(popup);
        ConfigurePopup(popup, sliced, RequestWidth, RequestHeight);

        Transform header = CreateRect(popup, "Header");
        ConfigureHeader(header, HeaderHeight);

        TMP_Text headerText = CreateText(
            header, "HeaderText", font, 27f, White,
            TextAlignmentOptions.Center);
        headerText.text = "TRADE REQUEST";
        headerText.fontStyle = FontStyles.Bold;
        Stretch(headerText.rectTransform, 18f, 18f, 6f, 6f);

        Transform content = CreateVerticalContent(
            popup, "Content", HeaderHeight + 14f, FooterHeight + 14f);

        TMP_Text message = CreateInfoText(
            content, "MessageText", font,
            "Player wants to trade with you.");

        TMP_Text offered = CreateInfoBox(
            content, "OfferedText", font, "OFFERED");

        TMP_Text requested = CreateInfoBox(
            content, "RequestedText", font, "REQUESTED");

        TMP_Text status = CreateInfoText(
            content, "StatusText", font,
            "Review the offer.");

        Transform buttons = CreateHorizontalRow(
            popup, "ButtonRow");

        RectTransform buttonRt =
            buttons.GetComponent<RectTransform>();
        buttonRt.anchorMin = new Vector2(0f, 0f);
        buttonRt.anchorMax = new Vector2(1f, 0f);
        buttonRt.pivot = new Vector2(0.5f, 0f);
        buttonRt.offsetMin = new Vector2(24f, 16f);
        buttonRt.offsetMax = new Vector2(-24f, 16f + ButtonHeight);

        Button accept = CreateActionButton(
            buttons, "AcceptButton", "ACCEPT", font, Green, 190f);

        Button decline = CreateActionButton(
            buttons, "DeclineButton", "DECLINE", font, Red, 190f);

        Button close = CreateActionButton(
            buttons, "CloseButton", "CLOSE", font, Section, 150f);

        popup.gameObject.SetActive(false);
        root.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(panel);
        SetObject(so, "panel", popup.gameObject);
        SetObject(so, "headerText", headerText);
        SetObject(so, "messageText", message);
        SetObject(so, "offeredText", offered);
        SetObject(so, "requestedText", requested);
        SetObject(so, "statusText", status);
        SetObject(so, "acceptButton", accept);
        SetObject(so, "declineButton", decline);
        SetObject(so, "closeButton", close);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ============================================================
    // OPEN TRADE BUTTON
    // ============================================================

    private static void BuildTradeOpenButton(
        Transform canvas,
        TMP_FontAsset font)
    {
        Transform existing =
            FindDeepChild(canvas, "TradeOpenButton");

        if (existing == null)
            return;

        RectTransform rt =
            existing.GetComponent<RectTransform>();

        if (rt == null)
            return;

        Image image =
            GetOrAdd<Image>(existing.gameObject);

        image.color = Navy;

        Button button =
            GetOrAdd<Button>(existing.gameObject);

        button.targetGraphic = image;

        TMP_Text text =
            existing.GetComponentInChildren<TMP_Text>(true);

        if (text == null)
        {
            text = CreateText(
                existing,
                "Text",
                font,
                17f,
                White,
                TextAlignmentOptions.Center);

            Stretch(text.rectTransform, 4f, 4f, 0f, 0f);
        }

        text.text = "TRADE";
        text.fontStyle = FontStyles.Bold;
    }

    // ============================================================
    // PREFAB REFERENCES
    // ============================================================

    private static void AssignTradePrefabs(
        TradePanel panel)
    {
        SerializedObject so = new SerializedObject(panel);

        GameObject playerPrefab = FindPrefab("TradePlayerButton");
        GameObject chipPrefab = FindPrefab("TradePropertyChip");
        GameObject togglePrefab = FindPrefab("TradePropertyToggle");

        if (playerPrefab != null)
            SetObject(
                so,
                "playerButtonPrefab",
                playerPrefab.GetComponent<Button>());

        if (chipPrefab != null)
            SetObject(
                so,
                "propertyChipPrefab",
                chipPrefab.GetComponent<Button>());

        if (togglePrefab != null)
            SetObject(
                so,
                "propertyPickerTogglePrefab",
                togglePrefab.GetComponent<Toggle>());

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject FindPrefab(
        string name)
    {
        string[] guids =
            AssetDatabase.FindAssets(
                $"{name} t:Prefab");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab != null)
                return prefab;
        }

        return null;
    }

    // ============================================================
    // LAYOUT HELPERS
    // ============================================================

    private static void ConfigureOverlayRoot(
        Transform root)
    {
        Stretch(
            GetOrAdd<RectTransform>(root.gameObject),
            0f,
            0f,
            0f,
            0f);
    }

    private static void ConfigurePopup(
        Transform popup,
        Sprite sliced,
        float width,
        float height)
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

        image.color = Card;
        image.raycastTarget = true;

        if (sliced != null)
        {
            image.sprite = sliced;
            image.type = Image.Type.Sliced;
        }
    }

    private static void ConfigureHeader(
        Transform header,
        float height)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(header.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -height);
        rt.offsetMax = Vector2.zero;

        Image image =
            GetOrAdd<Image>(header.gameObject);

        image.color = Header;
        image.raycastTarget = false;
    }

    private static void SetSection(
        Transform section,
        float top,
        float bottom)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(section.gameObject);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(18f, bottom);
        rt.offsetMax = new Vector2(-18f, -top);

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(section.gameObject);

        layout.spacing = 8f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static Transform CreateVerticalContent(
        Transform parent,
        string name,
        float top,
        float bottom)
    {
        Transform content =
            CreateRect(parent, name);

        RectTransform rt =
            content.GetComponent<RectTransform>();

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(24f, bottom);
        rt.offsetMax = new Vector2(-24f, -top);

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(content.gameObject);

        layout.spacing = 10f;
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return content;
    }

    private static Transform CreateScrollList(
        Transform parent,
        string name,
        float height)
    {
        Transform scrollRoot =
            CreateRect(parent, name);

        LayoutElement rootLayout =
            AddLayout(scrollRoot.gameObject);
        rootLayout.preferredHeight = height;
        rootLayout.minHeight = height;

        ScrollRect scroll =
            GetOrAdd<ScrollRect>(scrollRoot.gameObject);
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 35f;

        Transform viewport =
            CreateRect(scrollRoot, "Viewport");

        Stretch(
            viewport.GetComponent<RectTransform>(),
            0f,
            0f,
            0f,
            0f);

        Image viewportImage =
            GetOrAdd<Image>(viewport.gameObject);
        viewportImage.color =
            new Color(0f, 0f, 0f, 0.08f);

        Mask mask =
            GetOrAdd<Mask>(viewport.gameObject);
        mask.showMaskGraphic = false;

        Transform content =
            CreateRect(viewport, "Content");

        RectTransform contentRt =
            content.GetComponent<RectTransform>();

        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.anchoredPosition = Vector2.zero;
        contentRt.sizeDelta = Vector2.zero;

        VerticalLayoutGroup contentLayout =
            GetOrAdd<VerticalLayoutGroup>(content.gameObject);

        contentLayout.spacing = 6f;
        contentLayout.padding = new RectOffset(8, 8, 8, 8);
        contentLayout.childControlWidth = true;
        contentLayout.childControlHeight = true;
        contentLayout.childForceExpandWidth = true;
        contentLayout.childForceExpandHeight = false;

        ContentSizeFitter fitter =
            GetOrAdd<ContentSizeFitter>(content.gameObject);
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

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
            CreateRect(parent, name);

        LayoutElement layout =
            AddLayout(container.gameObject);
        layout.preferredHeight = 76f;
        layout.minHeight = 76f;

        VerticalLayoutGroup group =
            GetOrAdd<VerticalLayoutGroup>(container.gameObject);
        group.spacing = 4f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = false;

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
            CreateRect(parent, rowName);

        LayoutElement rowLayout =
            AddLayout(row.gameObject);
        rowLayout.preferredHeight = ButtonHeight;

        HorizontalLayoutGroup group =
            GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        group.spacing = 8f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = false;
        group.childForceExpandHeight = true;

        TMP_Text labelText =
            CreateText(
                row,
                "Label",
                font,
                14f,
                Muted,
                TextAlignmentOptions.Left);
        labelText.text = label;

        LayoutElement labelLayout =
            AddLayout(labelText.gameObject);
        labelLayout.preferredWidth = 190f;

        minusButton =
            CreateActionButton(
                row,
                "MinusButton",
                "−",
                font,
                Section,
                48f);

        TMP_InputField input =
            CreateInputField(
                row,
                "MoneyInput",
                font);

        AddLayout(input.gameObject).flexibleWidth = 1f;

        plusButton =
            CreateActionButton(
                row,
                "PlusButton",
                "+",
                font,
                Section,
                48f);

        return input;
    }

    private static TMP_InputField CreateInputField(
        Transform parent,
        string name,
        TMP_FontAsset font)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(TMP_InputField));

        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = Field;

        TMP_InputField input =
            obj.GetComponent<TMP_InputField>();

        GameObject textObject =
            new GameObject(
                "Text",
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        textObject.transform.SetParent(
            obj.transform,
            false);

        TextMeshProUGUI text =
            textObject.GetComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = 16f;
        text.color = White;
        text.alignment = TextAlignmentOptions.Center;

        Stretch(
            text.rectTransform,
            8f,
            8f,
            2f,
            2f);

        input.textComponent = text;
        input.contentType =
            TMP_InputField.ContentType.IntegerNumber;
        input.text = "0";

        return input;
    }

    private static TMP_Text CreateInfoBox(
        Transform parent,
        string name,
        TMP_FontAsset font,
        string placeholder)
    {
        Transform box =
            CreateRect(
                parent,
                name + "Box");

        Image image =
            GetOrAdd<Image>(box.gameObject);
        image.color = Field;

        LayoutElement layout =
            AddLayout(box.gameObject);
        layout.preferredHeight = 150f;
        layout.minHeight = 150f;

        TMP_Text text =
            CreateText(
                box,
                name,
                font,
                15f,
                White,
                TextAlignmentOptions.TopLeft);

        text.text = placeholder;
        Stretch(
            text.rectTransform,
            16f,
            16f,
            12f,
            12f);

        return text;
    }

    private static TMP_Text CreateInfoText(
        Transform parent,
        string name,
        TMP_FontAsset font,
        string placeholder)
    {
        TMP_Text text =
            CreateText(
                parent,
                name,
                font,
                16f,
                White,
                TextAlignmentOptions.Center);

        text.text = placeholder;
        AddPreferredHeight(text.gameObject, 46f);

        return text;
    }

    private static TMP_Text CreateSectionTitle(
        Transform parent,
        string name,
        TMP_FontAsset font,
        string label)
    {
        TMP_Text text =
            CreateText(
                parent,
                name,
                font,
                14f,
                Gold,
                TextAlignmentOptions.Center);

        text.text = label;
        text.fontStyle = FontStyles.Bold;
        AddPreferredHeight(text.gameObject, 30f);

        return text;
    }

    private static Transform CreateHorizontalRow(
        Transform parent,
        string name)
    {
        Transform row =
            CreateRect(parent, name);

        LayoutElement layout =
            AddLayout(row.gameObject);
        layout.preferredHeight = ButtonHeight;

        HorizontalLayoutGroup group =
            GetOrAdd<HorizontalLayoutGroup>(row.gameObject);
        group.spacing = 10f;
        group.childControlWidth = true;
        group.childControlHeight = true;
        group.childForceExpandWidth = true;
        group.childForceExpandHeight = true;

        return row;
    }

    private static Button CreateActionButton(
        Transform parent,
        string name,
        string label,
        TMP_FontAsset font,
        Color color,
        float preferredWidth)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(LayoutElement));

        obj.transform.SetParent(parent, false);

        Image image = obj.GetComponent<Image>();
        image.color = color;

        Button button = obj.GetComponent<Button>();
        button.targetGraphic = image;

        LayoutElement layout =
            obj.GetComponent<LayoutElement>();
        layout.preferredHeight = ButtonHeight;
        layout.minHeight = ButtonHeight;

        if (preferredWidth > 0f)
        {
            layout.preferredWidth = preferredWidth;
            layout.flexibleWidth = 0f;
        }

        TMP_Text text =
            CreateText(
                obj.transform,
                "Text",
                font,
                15f,
                White,
                TextAlignmentOptions.Center);

        text.text = label;
        text.fontStyle = FontStyles.Bold;

        Stretch(
            text.rectTransform,
            8f,
            8f,
            0f,
            0f);

        return button;
    }

    private static Transform CreateRect(
        Transform parent,
        string name)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform));

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create Trade UI Object");

        obj.transform.SetParent(parent, false);
        return obj.transform;
    }

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        TMP_FontAsset font,
        float size,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        obj.transform.SetParent(parent, false);

        TextMeshProUGUI text =
            obj.GetComponent<TextMeshProUGUI>();

        if (font != null)
            text.font = font;

        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

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
    }

    private static LayoutElement AddLayout(
        GameObject obj)
    {
        return GetOrAdd<LayoutElement>(obj);
    }

    private static void AddPreferredHeight(
        GameObject obj,
        float height)
    {
        LayoutElement layout = AddLayout(obj);
        layout.preferredHeight = height;
        layout.minHeight = height;
    }



    private static Transform GetOrCreate(
        Transform parent,
        string name)
    {
        Transform existing =
            FindDirectChild(
                parent,
                name
            );

        if (existing != null)
            return existing;

        return CreateRect(
            parent,
            name
        );
    }

    private static Transform FindDirectChild(
        Transform parent,
        string name)
    {
        if (parent == null)
            return null;

        for (int i = 0;
            i < parent.childCount;
            i++)
        {
            Transform child =
                parent.GetChild(i);

            if (child.name == name)
                return child;
        }

        return null;
    }

    // ============================================================
    // ASSET HELPERS
    // ============================================================

    private static TMP_FontAsset GetFont()
    {
        string[] guids =
            AssetDatabase.FindAssets("t:TMP_FontAsset");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            TMP_FontAsset font =
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);

            if (font != null)
                return font;
        }

        return null;
    }

    private static Sprite GetSlicedSprite()
    {
        string[] guids =
            AssetDatabase.FindAssets("RoundedBoardCard t:Sprite");

        foreach (string guid in guids)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(guid);

            Sprite sprite =
                AssetDatabase.LoadAssetAtPath<Sprite>(path);

            if (sprite != null)
                return sprite;
        }

        return null;
    }

    private static T GetOrAdd<T>(GameObject obj)
        where T : Component
    {
        T component = obj.GetComponent<T>();

        if (component == null)
            component = obj.AddComponent<T>();

        return component;
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

    private static void ClearChildren(
        Transform parent)
    {
        for (int i = parent.childCount - 1;
             i >= 0;
             i--)
        {
            Object.DestroyImmediate(
                parent.GetChild(i).gameObject);
        }
    }

    private static Transform FindDeepChild(
        Transform parent,
        string name)
    {
        if (parent == null)
            return null;

        if (parent.name == name)
            return parent;

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform result =
                FindDeepChild(
                    parent.GetChild(i),
                    name);

            if (result != null)
                return result;
        }

        return null;
    }
}
