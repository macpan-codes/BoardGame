using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the PropertyInfoPanel UI layout.
/// Safe to run repeatedly.
/// Does not create another popup or another PropertyInfoPanel.
/// </summary>
public static class PropertyInfoPanelSetup
{
    private const float CardWidth = 450f;
    private const float CardHeight = 650f;

    private const float HeaderHeight = 90f;
    private const float TypeBarHeight = 42f;

    private const float RowHeight = 30f;
    private const float SeparatorHeight = 1f;

    private const float CloseButtonWidth = 180f;
    private const float CloseButtonHeight = 48f;

    private const float TitleFontSize = 30f;
    private const float TypeFontSize = 17f;
    private const float InfoFontSize = 16f;
    private const float CloseFontSize = 17f;

    private static readonly Color Navy =
        new Color(0.08f, 0.12f, 0.22f, 1f);

    private static readonly Color Magenta =
        new Color(0.72f, 0.11f, 0.42f, 1f);

    private static readonly Color CardColor =
        new Color(0.96f, 0.96f, 0.95f, 1f);

    private static readonly Color TextColor =
        new Color(0.08f, 0.08f, 0.08f, 1f);

    private static readonly Color LabelColor =
        new Color(0.30f, 0.30f, 0.30f, 1f);

    private static readonly Color SeparatorColor =
        new Color(0.78f, 0.78f, 0.78f, 1f);

    private static readonly Color CloseColor =
        new Color(0.08f, 0.12f, 0.22f, 1f);

    private static readonly Color CloseXColor =
        new Color(0.75f, 0.12f, 0.12f, 1f);

    [MenuItem("Tools/Board Game/Setup Property Info Panel")]
    public static void SetupPropertyInfoPanel()
    {
        PropertyInfoPanel panel =
            Object.FindFirstObjectByType<PropertyInfoPanel>(
                FindObjectsInactive.Include);

        if (panel == null)
        {
            Debug.LogError(
                "PropertyInfoPanelSetup: PropertyInfoPanel was not found.");
            return;
        }

        Transform panelTransform = panel.transform;

        Transform popup =
            FindChild(panelTransform, "PopupBackground");

        if (popup == null)
        {
            Debug.LogError(
                "PropertyInfoPanelSetup: PopupBackground was not found.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(
            panel.gameObject,
            "Setup Property Info Panel");

        TMP_FontAsset font = GetFont();

        // ------------------------------------------------------------
        // CARD
        // ------------------------------------------------------------

        ConfigurePanelRoot(panelTransform);
        ConfigurePopup(popup);

        // ------------------------------------------------------------
        // EXISTING OBJECTS
        // ------------------------------------------------------------

        Transform header = GetOrCreate(popup, "Header");
        Transform propertyName =
            FindDeepChild(popup, "PropertyNameText");

        Transform propertyType =
            FindDeepChild(popup, "Property TypeText");

        if (propertyType == null)
            propertyType = FindDeepChild(popup, "PropertyTypeText");

        Transform purchasePrice =
            FindDeepChild(popup, "Purchase Price Text");

        if (purchasePrice == null)
            purchasePrice =
                FindDeepChild(popup, "PurchasePriceText");

        Transform baseRent =
            FindDeepChild(popup, "BaseRentText");

        Transform oneHouse =
            FindDeepChild(popup, "OneHouseRent Text");

        if (oneHouse == null)
            oneHouse =
                FindDeepChild(popup, "OneHouseRentText");

        Transform twoHouse =
            FindDeepChild(popup, "TwoHouse RentText");

        if (twoHouse == null)
            twoHouse =
                FindDeepChild(popup, "TwoHouseRentText");

        Transform threeHouse =
            FindDeepChild(popup, "ThreeHouseRentText");

        Transform fourHouse =
            FindDeepChild(popup, "FourHouseRentText");

        Transform hotelRent =
            FindDeepChild(popup, "HotelRentText");

        Transform houseCost =
            FindDeepChild(popup, "HouseCostText");

        Transform hotelCost =
            FindDeepChild(popup, "HotelCostText");

        Transform mortgage =
            FindDeepChild(popup, "MortgageValueText");

        Transform existingClose =
            FindDeepChild(popup, "CloseButton");

        // ------------------------------------------------------------
        // HEADER
        // ------------------------------------------------------------

        ConfigureHeader(header);

        if (propertyName != null)
        {
            propertyName.SetParent(header, false);

            ConfigureTMP(
                propertyName,
                font,
                TitleFontSize,
                Color.white,
                TextAlignmentOptions.Center);

            RectTransform rt =
                GetOrAdd<RectTransform>(propertyName.gameObject);

            Stretch(rt, 20f, 20f, 0f, 0f);

            rt.offsetMin = new Vector2(20f, 0f);
            rt.offsetMax = new Vector2(-20f, 0f);
        }

        // ------------------------------------------------------------
        // TYPE BAR
        // ------------------------------------------------------------

        Transform typeBar =
            GetOrCreate(popup, "PropertyTypeBar");

        ConfigureTypeBar(typeBar);

        TextMeshProUGUI typeBarText =
            GetOrCreateTMP(typeBar, "PropertyTypeBarText");

        ConfigureTMP(
            typeBarText.transform,
            font,
            TypeFontSize,
            Color.white,
            TextAlignmentOptions.Center);

        typeBarText.text = "PROPERTY";

        Stretch(
            GetOrAdd<RectTransform>(typeBarText.gameObject),
            0f, 0f, 0f, 0f);

        // ------------------------------------------------------------
        // INFORMATION AREA
        // ------------------------------------------------------------

        Transform info =
            GetOrCreate(popup, "InformationArea");

        ConfigureInformationArea(info);

        // Remove old generated rows.
        RemoveGeneratedRows(info);

        // ------------------------------------------------------------
        // ROWS
        // ------------------------------------------------------------

        CreateRow(
            info,
            "TypeRow",
            "TYPE",
            propertyType,
            font);

        CreateRow(
            info,
            "PurchasePriceRow",
            "PURCHASE PRICE",
            purchasePrice,
            font);

        CreateSeparator(info, "Separator1");

        CreateRow(
            info,
            "BaseRentRow",
            "BASE RENT",
            baseRent,
            font);

        CreateRow(
            info,
            "OneHouseRow",
            "1 HOUSE",
            oneHouse,
            font);

        CreateRow(
            info,
            "TwoHouseRow",
            "2 HOUSES",
            twoHouse,
            font);

        CreateRow(
            info,
            "ThreeHouseRow",
            "3 HOUSES",
            threeHouse,
            font);

        CreateRow(
            info,
            "FourHouseRow",
            "4 HOUSES",
            fourHouse,
            font);

        CreateRow(
            info,
            "HotelRow",
            "HOTEL",
            hotelRent,
            font);

        CreateSeparator(info, "Separator2");

        CreateRow(
            info,
            "HouseCostRow",
            "HOUSE COST",
            houseCost,
            font);

        CreateRow(
            info,
            "HotelCostRow",
            "HOTEL COST",
            hotelCost,
            font);

        CreateRow(
            info,
            "MortgageRow",
            "MORTGAGE VALUE",
            mortgage,
            font);

        // ------------------------------------------------------------
        // CLOSE BUTTON
        // ------------------------------------------------------------

        Transform closeArea =
            GetOrCreate(popup, "CloseButtonArea");

        ConfigureCloseArea(closeArea);

        Button closeButton;

        if (existingClose != null)
        {
            existingClose.SetParent(closeArea, false);

            closeButton =
                GetOrAdd<Button>(existingClose.gameObject);
        }
        else
        {
            GameObject buttonObject =
                new GameObject(
                    "CloseButton",
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button));

            Undo.RegisterCreatedObjectUndo(
                buttonObject,
                "Create Close Button");

            buttonObject.transform.SetParent(
                closeArea,
                false);

            closeButton =
                buttonObject.GetComponent<Button>();
        }

        ConfigureCloseButton(closeButton, font);

        // ------------------------------------------------------------
        // RECONNECT SERIALIZED REFERENCES
        // ------------------------------------------------------------

        AssignReferences(
            panel,
            propertyName,
            propertyType,
            typeBarText,
            purchasePrice,
            baseRent,
            oneHouse,
            twoHouse,
            threeHouse,
            fourHouse,
            hotelRent,
            houseCost,
            hotelCost,
            mortgage,
            closeButton);

        EditorUtility.SetDirty(panel);

        EditorSceneManager.MarkSceneDirty(
            panel.gameObject.scene);

        Debug.Log(
            "PropertyInfoPanelSetup: UI layout successfully rebuilt.");
    }

    // ================================================================
    // CARD
    // ================================================================

    private static void ConfigurePanelRoot(
        Transform root)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(root.gameObject);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta =
            new Vector2(CardWidth, CardHeight);
    }

    private static void ConfigurePopup(
        Transform popup)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(popup.gameObject);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta =
            new Vector2(CardWidth, CardHeight);

        Image image =
            GetOrAdd<Image>(popup.gameObject);

        image.color = CardColor;
        image.raycastTarget = true;

        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");

        image.type = Image.Type.Sliced;
    }

    // ================================================================
    // HEADER
    // ================================================================

    private static void ConfigureHeader(
        Transform header)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(header.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        rt.offsetMin =
            new Vector2(0f, -HeaderHeight);

        rt.offsetMax =
            new Vector2(0f, 0f);

        Image image =
            GetOrAdd<Image>(header.gameObject);

        image.color = Navy;
        image.raycastTarget = true;
    }

    // ================================================================
    // TYPE BAR
    // ================================================================

    private static void ConfigureTypeBar(
        Transform bar)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(bar.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);

        rt.offsetMin =
            new Vector2(0f, -(HeaderHeight + TypeBarHeight));

        rt.offsetMax =
            new Vector2(0f, -HeaderHeight);

        Image image =
            GetOrAdd<Image>(bar.gameObject);

        image.color = Magenta;
        image.raycastTarget = false;
    }

    // ================================================================
    // INFORMATION AREA
    // ================================================================

    private static void ConfigureInformationArea(
        Transform info)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(info.gameObject);

        rt.anchorMin =
            new Vector2(0f, 0f);

        rt.anchorMax =
            new Vector2(1f, 1f);

        rt.pivot =
            new Vector2(0.5f, 0.5f);

        // Space between type bar and close button.
        rt.offsetMin =
            new Vector2(25f, 85f);

        rt.offsetMax =
            new Vector2(-25f, -(HeaderHeight + TypeBarHeight + 10f));

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(
                info.gameObject);

        layout.spacing = 5f;

        layout.padding =
            new RectOffset(5, 5, 5, 5);

        layout.childAlignment =
            TextAnchor.UpperCenter;

        layout.childControlWidth = true;
        layout.childControlHeight = true;

        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image image =
            info.GetComponent<Image>();

        if (image != null)
            image.enabled = false;
    }

    // ================================================================
    // ROW
    // ================================================================

    private static void CreateRow(
        Transform info,
        string rowName,
        string label,
        Transform existingValue,
        TMP_FontAsset font)
    {
        if (existingValue == null)
        {
            Debug.LogWarning(
                $"PropertyInfoPanelSetup: Could not find value object for {rowName}.");
            return;
        }

        GameObject rowObject =
            new GameObject(
                rowName,
                typeof(RectTransform),
                typeof(LayoutElement),
                typeof(HorizontalLayoutGroup));

        Undo.RegisterCreatedObjectUndo(
            rowObject,
            "Create Property Row");

        Transform row =
            rowObject.transform;

        row.SetParent(info, false);

        LayoutElement rowLayout =
            rowObject.GetComponent<LayoutElement>();

        rowLayout.preferredHeight = RowHeight;
        rowLayout.minHeight = RowHeight;

        HorizontalLayoutGroup horizontal =
            rowObject.GetComponent<HorizontalLayoutGroup>();

        horizontal.spacing = 5f;

        horizontal.padding =
            new RectOffset(0, 0, 0, 0);

        horizontal.childAlignment =
            TextAnchor.MiddleCenter;

        horizontal.childControlWidth = true;
        horizontal.childControlHeight = true;

        horizontal.childForceExpandWidth = false;
        horizontal.childForceExpandHeight = true;

        // ------------------------------------------------------------
        // LABEL
        // ------------------------------------------------------------

        GameObject labelObject =
            new GameObject(
                "Label",
                typeof(RectTransform),
                typeof(TextMeshProUGUI),
                typeof(LayoutElement));

        Undo.RegisterCreatedObjectUndo(
            labelObject,
            "Create Property Label");

        labelObject.transform.SetParent(
            row,
            false);

        TextMeshProUGUI labelText =
            labelObject.GetComponent<TextMeshProUGUI>();

        ConfigureTMP(
            labelText.transform,
            font,
            InfoFontSize,
            LabelColor,
            TextAlignmentOptions.Left);

        labelText.text = label;

        LayoutElement labelLayout =
            labelObject.GetComponent<LayoutElement>();

        labelLayout.flexibleWidth = 1f;
        labelLayout.minWidth = 0f;

        // ------------------------------------------------------------
        // VALUE
        // ------------------------------------------------------------

        existingValue.SetParent(row, false);

        TextMeshProUGUI valueText =
            existingValue.GetComponent<TextMeshProUGUI>();

        if (valueText != null)
        {
            ConfigureTMP(
                valueText.transform,
                font,
                InfoFontSize,
                TextColor,
                TextAlignmentOptions.Right);

            valueText.enableWordWrapping = false;
            valueText.overflowMode =
                TextOverflowModes.Ellipsis;
        }

        LayoutElement valueLayout =
            GetOrAdd<LayoutElement>(
                existingValue.gameObject);

        valueLayout.flexibleWidth = 1f;
        valueLayout.minWidth = 0f;
    }

    // ================================================================
    // SEPARATOR
    // ================================================================

    private static void CreateSeparator(
        Transform info,
        string name)
    {
        GameObject separator =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(Image),
                typeof(LayoutElement));

        Undo.RegisterCreatedObjectUndo(
            separator,
            "Create Separator");

        separator.transform.SetParent(
            info,
            false);

        Image image =
            separator.GetComponent<Image>();

        image.color = SeparatorColor;
        image.raycastTarget = false;

        LayoutElement layout =
            separator.GetComponent<LayoutElement>();

        layout.preferredHeight =
            SeparatorHeight;

        layout.minHeight =
            SeparatorHeight;

        layout.flexibleWidth = 1f;
    }

    // ================================================================
    // CLOSE AREA
    // ================================================================

    private static void ConfigureCloseArea(
        Transform area)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(area.gameObject);

        rt.anchorMin =
            new Vector2(0f, 0f);

        rt.anchorMax =
            new Vector2(1f, 0f);

        rt.pivot =
            new Vector2(0.5f, 0f);

        rt.offsetMin =
            new Vector2(0f, 20f);

        rt.offsetMax =
            new Vector2(0f, 20f + CloseButtonHeight);

        HorizontalLayoutGroup layout =
            GetOrAdd<HorizontalLayoutGroup>(
                area.gameObject);

        layout.childAlignment =
            TextAnchor.MiddleCenter;

        layout.childControlWidth = false;
        layout.childControlHeight = false;

        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    // ================================================================
    // CLOSE BUTTON
    // ================================================================

    private static void ConfigureCloseButton(
        Button button,
        TMP_FontAsset font)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(
                button.gameObject);

        rt.anchorMin =
            new Vector2(0.5f, 0.5f);

        rt.anchorMax =
            new Vector2(0.5f, 0.5f);

        rt.pivot =
            new Vector2(0.5f, 0.5f);

        rt.sizeDelta =
            new Vector2(
                CloseButtonWidth,
                CloseButtonHeight);

        Image image =
            GetOrAdd<Image>(
                button.gameObject);

        image.color = CloseColor;
        image.raycastTarget = true;

        button.targetGraphic = image;

        TextMeshProUGUI text =
            FindDeepComponent<TextMeshProUGUI>(
                button.transform,
                "Text");

        if (text == null)
        {
            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create Close Button Text");

            textObject.transform.SetParent(
                button.transform,
                false);

            text =
                textObject.GetComponent<TextMeshProUGUI>();
        }

        ConfigureTMP(
            text.transform,
            font,
            CloseFontSize,
            Color.white,
            TextAlignmentOptions.Center);

        text.text = "CLOSE";

        Stretch(
            GetOrAdd<RectTransform>(
                text.gameObject),
            0f, 0f, 0f, 0f);
    }

    // ================================================================
    // TEXT
    // ================================================================

    private static void ConfigureTMP(
        Transform transform,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text =
            GetOrAdd<TextMeshProUGUI>(
                transform.gameObject);

        if (font != null)
            text.font = font;

        text.fontSize = fontSize;
        text.color = color;

        text.alignment = alignment;

        text.enableWordWrapping = false;
        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget = false;

        transform.localScale =
            Vector3.one;
    }

    // ================================================================
    // SERIALIZED REFERENCES
    // ================================================================

    private static void AssignReferences(
        PropertyInfoPanel panel,
        Transform propertyName,
        Transform propertyType,
        TextMeshProUGUI typeBarText,
        Transform purchasePrice,
        Transform baseRent,
        Transform oneHouse,
        Transform twoHouse,
        Transform threeHouse,
        Transform fourHouse,
        Transform hotelRent,
        Transform houseCost,
        Transform hotelCost,
        Transform mortgage,
        Button closeButton)
    {
        SerializedObject serialized =
            new SerializedObject(panel);

        SetObject(
            serialized,
            "propertyNameText",
            Component<TextMeshProUGUI>(
                propertyName));

        SetObject(
            serialized,
            "propertyTypeText",
            Component<TextMeshProUGUI>(
                propertyType));

        SetObject(
            serialized,
            "propertyTypeBarText",
            typeBarText);

        SetObject(
            serialized,
            "purchasePriceText",
            Component<TextMeshProUGUI>(
                purchasePrice));

        SetObject(
            serialized,
            "baseRentText",
            Component<TextMeshProUGUI>(
                baseRent));

        SetObject(
            serialized,
            "oneHouseRentText",
            Component<TextMeshProUGUI>(
                oneHouse));

        SetObject(
            serialized,
            "twoHouseRentText",
            Component<TextMeshProUGUI>(
                twoHouse));

        SetObject(
            serialized,
            "threeHouseRentText",
            Component<TextMeshProUGUI>(
                threeHouse));

        SetObject(
            serialized,
            "fourHouseRentText",
            Component<TextMeshProUGUI>(
                fourHouse));

        SetObject(
            serialized,
            "hotelRentText",
            Component<TextMeshProUGUI>(
                hotelRent));

        SetObject(
            serialized,
            "houseCostText",
            Component<TextMeshProUGUI>(
                houseCost));

        SetObject(
            serialized,
            "hotelCostText",
            Component<TextMeshProUGUI>(
                hotelCost));

        SetObject(
            serialized,
            "mortgageValueText",
            Component<TextMeshProUGUI>(
                mortgage));

        SetObject(
            serialized,
            "closeButton",
            closeButton);

        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorUtility.SetDirty(panel);
    }

    private static void SetObject(
        SerializedObject serialized,
        string field,
        Object value)
    {
        SerializedProperty property =
            serialized.FindProperty(field);

        if (property != null)
        {
            property.objectReferenceValue =
                value;
        }
    }

    // ================================================================
    // HELPERS
    // ================================================================

    private static TMP_FontAsset GetFont()
    {
        string[] guids =
            AssetDatabase.FindAssets(
                "Liberation Sans SDF t:TMP_FontAsset");

        if (guids.Length > 0)
        {
            string path =
                AssetDatabase.GUIDToAssetPath(
                    guids[0]);

            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                path);
        }

        return Resources.Load<TMP_FontAsset>(
            "Fonts & Materials/LiberationSans SDF");
    }

    private static Transform GetOrCreate(
        Transform parent,
        string name)
    {
        Transform existing =
            FindDirectChild(parent, name);

        if (existing != null)
            return existing;

        GameObject objectToCreate =
            new GameObject(
                name,
                typeof(RectTransform));

        Undo.RegisterCreatedObjectUndo(
            objectToCreate,
            "Create UI Object");

        objectToCreate.transform.SetParent(
            parent,
            false);

        return objectToCreate.transform;
    }

    private static void RemoveGeneratedRows(
        Transform info)
    {
        string[] names =
        {
            "TypeRow",
            "PurchasePriceRow",
            "Separator1",
            "BaseRentRow",
            "OneHouseRow",
            "TwoHouseRow",
            "ThreeHouseRow",
            "FourHouseRow",
            "HotelRow",
            "Separator2",
            "HouseCostRow",
            "HotelCostRow",
            "MortgageRow"
        };

        foreach (string name in names)
        {
            Transform child =
                FindDirectChild(info, name);

            if (child != null)
            {
                Object.DestroyImmediate(
                    child.gameObject);
            }
        }
    }

    private static void Stretch(
        RectTransform rt,
        float left,
        float right,
        float bottom,
        float top)
    {
        rt.anchorMin =
            Vector2.zero;

        rt.anchorMax =
            Vector2.one;

        rt.offsetMin =
            new Vector2(left, bottom);

        rt.offsetMax =
            new Vector2(-right, -top);

        rt.localScale =
            Vector3.one;
    }

    private static T GetOrAdd<T>(
        GameObject objectToModify)
        where T : Component
    {
        T component =
            objectToModify.GetComponent<T>();

        if (component == null)
        {
            component =
                objectToModify.AddComponent<T>();
        }

        return component;
    }

    private static T Component<T>(
        Transform transform)
        where T : Component
    {
        if (transform == null)
            return null;

        return transform.GetComponent<T>();
    }

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

    private static Transform FindChild(
        Transform parent,
        string name)
    {
        if (parent.name == name)
            return parent;

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform result =
                FindChild(
                    parent.GetChild(i),
                    name);

            if (result != null)
                return result;
        }

        return null;
    }

    private static T FindDeepComponent<T>(
        Transform parent,
        string name)
        where T : Component
    {
        Transform transform =
            FindDeepChild(
                parent,
                name);

        if (transform == null)
            return null;

        return transform.GetComponent<T>();
    }

    private static Transform FindDeepChild(
        Transform parent,
        string name)
    {
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
    private static TextMeshProUGUI GetOrCreateTMP(
    Transform parent,
    string name)
{
    Transform existing = FindDirectChild(parent, name);

    if (existing != null)
    {
        return GetOrAdd<TextMeshProUGUI>(
            existing.gameObject);
    }

    GameObject obj = new GameObject(
        name,
        typeof(RectTransform),
        typeof(TextMeshProUGUI));

    Undo.RegisterCreatedObjectUndo(
        obj,
        "Create TMP Text");

    obj.transform.SetParent(
        parent,
        false);

    return obj.GetComponent<TextMeshProUGUI>();
}
}