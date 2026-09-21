using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

public static class LandingActionPanelSetup
{
    private const float CardWidth = 560f;
    private const float CardHeight = 760f;

    private const float HeaderHeight = 130f;
    private const float TypeBarHeight = 50f;
    private const float OwnerHeight = 62f;

    private const float RowHeight = 32f;
    private const float ActionButtonHeight = 54f;
    private const float ActionSpacing = 8f;

    private static readonly Color CardColor =
        new Color(0.055f, 0.075f, 0.12f, 1f);

    private static readonly Color HeaderColor =
        new Color(0.075f, 0.105f, 0.17f, 1f);

    private static readonly Color InfoColor =
        new Color(0.085f, 0.125f, 0.20f, 1f);

    private static readonly Color OwnerColor =
        new Color(0.075f, 0.12f, 0.20f, 1f);

    private static readonly Color AccentColor =
        new Color(0.18f, 0.52f, 0.95f, 1f);

    private static readonly Color BuyColor =
        new Color(0.08f, 0.42f, 0.78f, 1f);

    private static readonly Color DeclineColor =
        new Color(0.18f, 0.22f, 0.29f, 1f);

    private static readonly Color MainText =
        new Color(0.96f, 0.98f, 1f, 1f);

    private static readonly Color SecondaryText =
        new Color(0.60f, 0.68f, 0.78f, 1f);

    [MenuItem("Tools/Board Game/Build Landing Action Template")]
    public static void BuildTemplate()
    {
        LandingActionPanel panel =
            Object.FindFirstObjectByType<LandingActionPanel>(
                FindObjectsInactive.Include
            );

        if (panel == null)
        {
            Debug.LogError(
                "LandingActionPanelSetup: LandingActionPanel was not found."
            );

            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            panel.gameObject,
            "Build Landing Action Template"
        );

        Transform popup =
            FindDirectChild(
                panel.transform,
                "PopupBackground"
            );

        if (popup == null)
        {
            GameObject popupObject =
                new GameObject(
                    "PopupBackground",
                    typeof(RectTransform),
                    typeof(Image)
                );

            popupObject.transform.SetParent(
                panel.transform,
                false
            );

            popup =
                popupObject.transform;
        }

        ClearChildren(popup);

        ConfigureCard(popup);

        // ========================================================
        // HEADER
        // ========================================================

        Transform header =
            CreateRect(
                popup,
                "Header"
            );

        ConfigureHeader(header);

        TMP_Text nameText =
            CreateText(
                header,
                "PropertyNameText",
                30f,
                MainText,
                TextAlignmentOptions.Center
            );

        nameText.fontStyle =
            FontStyles.Bold;

        nameText.enableAutoSizing =
            true;

        nameText.fontSizeMin =
            18f;

        nameText.fontSizeMax =
            30f;

        StretchAt(
            nameText.rectTransform,
            0.10f,
            0.72f,
            0.35f,
            0.94f
        );

        TMP_Text priceText =
            CreateText(
                header,
                "HeaderPriceText",
                19f,
                AccentColor,
                TextAlignmentOptions.Center
            );

        priceText.fontStyle =
            FontStyles.Bold;

        StretchAt(
            priceText.rectTransform,
            0.25f,
            0.25f,
            0.08f,
            0.36f
        );

        Transform closeButton =
            CreateButton(
                header,
                "CloseXButton"
            );

        ConfigureCloseButton(
            closeButton
        );

        // ========================================================
        // TYPE BAR
        // ========================================================

        Transform typeBar =
            CreateRect(
                popup,
                "PropertyTypeBar"
            );

        ConfigureTypeBar(typeBar);

        TMP_Text typeBarText =
            CreateText(
                typeBar,
                "PropertyTypeBarText",
                17f,
                MainText,
                TextAlignmentOptions.Center
            );

        typeBarText.fontStyle =
            FontStyles.Bold;

        typeBarText.text =
            "PROPERTY";

        Stretch(
            typeBarText.rectTransform
        );

        // ========================================================
        // OWNER ROW
        // ========================================================

        Transform ownerRow =
            CreateRect(
                popup,
                "OwnerRow"
            );

        ConfigureOwnerRow(ownerRow);

        TMP_Text ownerLabel =
            CreateText(
                ownerRow,
                "OwnerLabelText",
                14f,
                SecondaryText,
                TextAlignmentOptions.Left
            );

        ownerLabel.text =
            "OWNER";

        StretchAt(
            ownerLabel.rectTransform,
            0.07f,
            0.70f,
            0f,
            0f
        );

        Transform ownerIndicator =
            CreateRect(
                ownerRow,
                "OwnerIndicator"
            );

        RectTransform indicatorRect =
            ownerIndicator.GetComponent<RectTransform>();

        indicatorRect.anchorMin =
            new Vector2(
                0.35f,
                0.5f
            );

        indicatorRect.anchorMax =
            new Vector2(
                0.35f,
                0.5f
            );

        indicatorRect.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        indicatorRect.sizeDelta =
            new Vector2(
                18f,
                18f
            );

        Image indicatorImage =
            ownerIndicator.gameObject.AddComponent<Image>();

        indicatorImage.color =
            new Color(
                0.35f,
                0.40f,
                0.48f,
                1f
            );

        TMP_Text ownerText =
            CreateText(
                ownerRow,
                "OwnerText",
                16f,
                MainText,
                TextAlignmentOptions.Right
            );

        ownerText.fontStyle =
            FontStyles.Bold;

        ownerText.text =
            "UNOWNED";

        StretchAt(
            ownerText.rectTransform,
            0.40f,
            0.08f,
            0f,
            0f
        );

        // ========================================================
        // INFORMATION AREA
        // ========================================================

        Transform informationArea =
            CreateRect(
                popup,
                "InformationArea"
            );

        ConfigureInformationArea(
            informationArea
        );

        CreateValueRow(
            informationArea,
            "PurchasePriceInfoRow",
            "PURCHASE PRICE",
            "PurchasePriceInfoText"
        );

        CreateValueRow(
            informationArea,
            "BaseRentRow",
            "BASE RENT",
            "BaseRentText"
        );

        CreateValueRow(
            informationArea,
            "OneHouseRow",
            "WITH 1 HOUSE",
            "OneHouseRentText"
        );

        CreateValueRow(
            informationArea,
            "TwoHouseRow",
            "WITH 2 HOUSES",
            "TwoHouseRentText"
        );

        CreateValueRow(
            informationArea,
            "ThreeHouseRow",
            "WITH 3 HOUSES",
            "ThreeHouseRentText"
        );

        CreateValueRow(
            informationArea,
            "FourHouseRow",
            "WITH 4 HOUSES",
            "FourHouseRentText"
        );

        CreateValueRow(
            informationArea,
            "HotelRow",
            "WITH HOTEL",
            "HotelRentText"
        );

        // ========================================================
        // ACTION AREA
        // ========================================================

        Transform actionArea =
            CreateRect(
                popup,
                "ActionArea"
            );

        ConfigureActionArea(
            actionArea
        );

        Transform buyButton =
            CreateButton(
                actionArea,
                "BuyButton"
            );

        ConfigureActionButton(
            buyButton,
            "BUY",
            BuyColor
        );

        Transform declineButton =
            CreateButton(
                actionArea,
                "DeclineButton"
            );

        ConfigureActionButton(
            declineButton,
            "DECLINE",
            DeclineColor
        );

        // ========================================================
        // RECONNECT SCRIPT REFERENCES
        // ========================================================

        SerializedObject serialized =
            new SerializedObject(panel);

        SetObject(
            serialized,
            "cardRoot",
            popup.gameObject
        );

        SetObject(
            serialized,
            "propertyNameText",
            nameText
        );

        SetObject(
            serialized,
            "headerPriceText",
            priceText
        );

        SetObject(
            serialized,
            "propertyTypeBarText",
            typeBarText
        );

        SetObject(
            serialized,
            "propertyTypeText",
            typeBarText
        );

        SetObject(
            serialized,
            "purchasePriceText",
            priceText
        );

        SetObject(
            serialized,
            "ownerIndicator",
            ownerIndicator.gameObject
        );

        SetObject(
            serialized,
            "ownerIndicatorImage",
            indicatorImage
        );

        SetObject(
            serialized,
            "ownerText",
            ownerText
        );

        SetObject(
            serialized,
            "informationAreaObject",
            informationArea.gameObject
        );

        SetObject(
            serialized,
            "purchasePriceInfoText",
            FindTMP(
                informationArea,
                "PurchasePriceInfoText"
            )
        );

        SetObject(
            serialized,
            "baseRentText",
            FindTMP(
                informationArea,
                "BaseRentText"
            )
        );

        SetObject(
            serialized,
            "oneHouseRentText",
            FindTMP(
                informationArea,
                "OneHouseRentText"
            )
        );

        SetObject(
            serialized,
            "twoHouseRentText",
            FindTMP(
                informationArea,
                "TwoHouseRentText"
            )
        );

        SetObject(
            serialized,
            "threeHouseRentText",
            FindTMP(
                informationArea,
                "ThreeHouseRentText"
            )
        );

        SetObject(
            serialized,
            "fourHouseRentText",
            FindTMP(
                informationArea,
                "FourHouseRentText"
            )
        );

        SetObject(
            serialized,
            "hotelRentText",
            FindTMP(
                informationArea,
                "HotelRentText"
            )
        );

        SetObject(
            serialized,
            "buyButton",
            buyButton.GetComponent<Button>()
        );

        SetObject(
            serialized,
            "declineButton",
            declineButton.GetComponent<Button>()
        );

        SetObject(
            serialized,
            "closeXButton",
            closeButton.GetComponent<Button>()
        );

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(panel);

        EditorSceneManager.MarkSceneDirty(
            panel.gameObject.scene
        );

        Debug.Log(
            "LandingActionPanelSetup: " +
            "Landing Action template built successfully."
        );
    }

    // ============================================================
    // CARD
    // ============================================================

    private static void ConfigureCard(
        Transform popup)
    {
        RectTransform rt =
            popup.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchoredPosition =
            Vector2.zero;

        rt.sizeDelta =
            new Vector2(
                CardWidth,
                CardHeight
            );

        Image image =
            popup.GetComponent<Image>();

        image.color =
            CardColor;

        image.raycastTarget =
            true;
    }

    // ============================================================
    // HEADER
    // ============================================================

    private static void ConfigureHeader(
        Transform header)
    {
        RectTransform rt =
            header.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0f,
                1f
            );

        rt.anchorMax =
            new Vector2(
                1f,
                1f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                1f
            );

        rt.offsetMin =
            new Vector2(
                0f,
                -HeaderHeight
            );

        rt.offsetMax =
            Vector2.zero;

        Image image =
            header.gameObject.AddComponent<Image>();

        image.color =
            HeaderColor;

        image.raycastTarget =
            false;
    }

    // ============================================================
    // TYPE BAR
    // ============================================================

    private static void ConfigureTypeBar(
        Transform bar)
    {
        RectTransform rt =
            bar.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0.05f,
                1f
            );

        rt.anchorMax =
            new Vector2(
                0.95f,
                1f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                1f
            );

        rt.offsetMin =
            new Vector2(
                0f,
                -(HeaderHeight +
                  TypeBarHeight)
            );

        rt.offsetMax =
            new Vector2(
                0f,
                -HeaderHeight
            );

        Image image =
            bar.gameObject.AddComponent<Image>();

        image.color =
            AccentColor;

        image.raycastTarget =
            false;
    }

    // ============================================================
    // OWNER
    // ============================================================

    private static void ConfigureOwnerRow(
        Transform row)
    {
        RectTransform rt =
            row.GetComponent<RectTransform>();

        float top =
            HeaderHeight +
            TypeBarHeight +
            10f;

        rt.anchorMin =
            new Vector2(
                0.05f,
                1f
            );

        rt.anchorMax =
            new Vector2(
                0.95f,
                1f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                1f
            );

        rt.offsetMin =
            new Vector2(
                0f,
                -(top +
                  OwnerHeight)
            );

        rt.offsetMax =
            new Vector2(
                0f,
                -top
            );

        Image image =
            row.gameObject.AddComponent<Image>();

        image.color =
            OwnerColor;

        image.raycastTarget =
            false;
    }

    // ============================================================
    // INFORMATION
    // ============================================================

    private static void ConfigureInformationArea(
        Transform info)
    {
        RectTransform rt =
            info.GetComponent<RectTransform>();

        float top =
            HeaderHeight +
            TypeBarHeight +
            OwnerHeight +
            24f;

        rt.anchorMin =
            new Vector2(
                0.05f,
                0f
            );

        rt.anchorMax =
            new Vector2(
                0.95f,
                1f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.offsetMin =
            new Vector2(
                0f,
                ActionButtonHeight * 2f +
                ActionSpacing +
                28f
            );

        rt.offsetMax =
            new Vector2(
                0f,
                -top
            );

        Image image =
            info.gameObject.AddComponent<Image>();

        image.color =
            InfoColor;

        image.raycastTarget =
            false;

        VerticalLayoutGroup layout =
            info.gameObject.AddComponent<
                VerticalLayoutGroup
            >();

        layout.padding =
            new RectOffset(
                22,
                22,
                18,
                18
            );

        layout.spacing =
            3f;

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
    }

    // ============================================================
    // INFORMATION ROW
    // ============================================================

    private static void CreateValueRow(
        Transform info,
        string rowName,
        string label,
        string valueName)
    {
        GameObject row =
            new GameObject(
                rowName,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup)
            );

        row.transform.SetParent(
            info,
            false
        );

        LayoutElement rowElement =
            row.GetComponent<LayoutElement>();

        rowElement.preferredHeight =
            RowHeight;

        rowElement.minHeight =
            RowHeight;

        HorizontalLayoutGroup layout =
            row.GetComponent<HorizontalLayoutGroup>();

        layout.spacing =
            8f;

        layout.childAlignment =
            TextAnchor.MiddleCenter;

        layout.childControlWidth =
            true;

        layout.childControlHeight =
            true;

        layout.childForceExpandWidth =
            false;

        layout.childForceExpandHeight =
            true;

        TMP_Text labelText =
            CreateText(
                row.transform,
                "Label",
                13f,
                SecondaryText,
                TextAlignmentOptions.Left
            );

        labelText.text =
            label;

        LayoutElement labelLayout =
            labelText.gameObject.AddComponent<
                LayoutElement
            >();

        labelLayout.flexibleWidth =
            1f;

        TMP_Text valueText =
            CreateText(
                row.transform,
                valueName,
                14f,
                MainText,
                TextAlignmentOptions.Right
            );

        valueText.text =
            "—";

        LayoutElement valueLayout =
            valueText.gameObject.AddComponent<
                LayoutElement
            >();

        valueLayout.flexibleWidth =
            1f;
    }

    // ============================================================
    // ACTION AREA
    // ============================================================

    private static void ConfigureActionArea(
        Transform area)
    {
        RectTransform rt =
            area.GetComponent<RectTransform>();

        float totalHeight =
            ActionButtonHeight * 2f +
            ActionSpacing;

        rt.anchorMin =
            new Vector2(
                0.05f,
                0f
            );

        rt.anchorMax =
            new Vector2(
                0.95f,
                0f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0f
            );

        rt.offsetMin =
            new Vector2(
                0f,
                18f
            );

        rt.offsetMax =
            new Vector2(
                0f,
                18f +
                totalHeight
            );

        VerticalLayoutGroup layout =
            area.gameObject.AddComponent<
                VerticalLayoutGroup
            >();

        layout.spacing =
            ActionSpacing;

        layout.childAlignment =
            TextAnchor.MiddleCenter;

        layout.childControlWidth =
            false;

        layout.childControlHeight =
            false;

        layout.childForceExpandWidth =
            false;

        layout.childForceExpandHeight =
            false;
    }

    // ============================================================
    // ACTION BUTTON
    // ============================================================

    private static void ConfigureActionButton(
        Transform button,
        string label,
        Color color)
    {
        RectTransform rt =
            button.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.anchorMax =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.pivot =
            new Vector2(
                0.5f,
                0.5f
            );

        rt.sizeDelta =
            new Vector2(
                330f,
                ActionButtonHeight
            );

        Image image =
            button.gameObject.AddComponent<Image>();

        image.color =
            color;

        Button uiButton =
            button.gameObject.AddComponent<Button>();

        uiButton.targetGraphic =
            image;

        TMP_Text text =
            CreateText(
                button,
                "Text",
                16f,
                MainText,
                TextAlignmentOptions.Center
            );

        text.fontStyle =
            FontStyles.Bold;

        text.text =
            label;

        Stretch(
            text.rectTransform
        );
    }

    // ============================================================
    // CLOSE BUTTON
    // ============================================================

    private static void ConfigureCloseButton(
        Transform button)
    {
        RectTransform rt =
            button.GetComponent<RectTransform>();

        rt.anchorMin =
            new Vector2(
                1f,
                1f
            );

        rt.anchorMax =
            new Vector2(
                1f,
                1f
            );

        rt.pivot =
            new Vector2(
                1f,
                1f
            );

        rt.anchoredPosition =
            new Vector2(
                -12f,
                -12f
            );

        rt.sizeDelta =
            new Vector2(
                52f,
                52f
            );

        Image image =
            button.gameObject.AddComponent<Image>();

        image.color =
            new Color(
                1f,
                1f,
                1f,
                0.06f
            );

        Button uiButton =
            button.gameObject.AddComponent<Button>();

        uiButton.targetGraphic =
            image;

        TMP_Text text =
            CreateText(
                button,
                "Icon",
                27f,
                MainText,
                TextAlignmentOptions.Center
            );

        text.text =
            "×";

        Stretch(
            text.rectTransform
        );
    }

    // ============================================================
    // CREATE RECT
    // ============================================================

    private static Transform CreateRect(
        Transform parent,
        string name)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create Landing Action UI"
        );

        obj.transform.SetParent(
            parent,
            false
        );

        return obj.transform;
    }

    // ============================================================
    // CREATE BUTTON
    // ============================================================

    private static Transform CreateButton(
        Transform parent,
        string name)
    {
        return CreateRect(
            parent,
            name
        );
    }

    // ============================================================
    // CREATE TEXT
    // ============================================================

    private static TMP_Text CreateText(
        Transform parent,
        string name,
        float size,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI)
            );

        Undo.RegisterCreatedObjectUndo(
            obj,
            "Create Landing Action Text"
        );

        obj.transform.SetParent(
            parent,
            false
        );

        TextMeshProUGUI text =
            obj.GetComponent<TextMeshProUGUI>();

        text.fontSize =
            size;

        text.color =
            color;

        text.alignment =
            alignment;

        text.textWrappingMode =
            TextWrappingModes.NoWrap;

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget =
            false;

        text.transform.localScale =
            Vector3.one;

        return text;
    }

    // ============================================================
    // FIND TMP
    // ============================================================

    private static TMP_Text FindTMP(
        Transform root,
        string name)
    {
        Transform child =
            FindDeepChild(
                root,
                name
            );

        return child != null
            ? child.GetComponent<TMP_Text>()
            : null;
    }

    // ============================================================
    // SERIALIZED OBJECT
    // ============================================================

    private static void SetObject(
        SerializedObject serialized,
        string field,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(
                field
            );

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }

    // ============================================================
    // STRETCH
    // ============================================================

    private static void Stretch(
        RectTransform rt)
    {
        rt.anchorMin =
            Vector2.zero;

        rt.anchorMax =
            Vector2.one;

        rt.offsetMin =
            Vector2.zero;

        rt.offsetMax =
            Vector2.zero;
    }

    private static void StretchAt(
        RectTransform rt,
        float left,
        float right,
        float bottom,
        float top)
    {
        rt.anchorMin =
            new Vector2(
                left,
                bottom
            );

        rt.anchorMax =
            new Vector2(
                1f - right,
                1f - top
            );

        rt.offsetMin =
            Vector2.zero;

        rt.offsetMax =
            Vector2.zero;
    }

    // ============================================================
    // FIND CHILD
    // ============================================================

    private static Transform FindDirectChild(
        Transform parent,
        string name)
    {
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
                    name
                );

            if (result != null)
                return result;
        }

        return null;
    }

    // ============================================================
    // CLEAR
    // ============================================================

    private static void ClearChildren(
        Transform parent)
    {
        for (int i =
                 parent.childCount - 1;
             i >= 0;
             i--)
        {
            Object.DestroyImmediate(
                parent.GetChild(i).gameObject
            );
        }
    }
}