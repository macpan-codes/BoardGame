using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Builds the PropertyManagementPanel UI layout.
/// Safe to run repeatedly.
/// Does not create another PropertyManagementPanel.
/// </summary>
public static class PropertyManagementPanelSetup
{
    private const float CardWidth = 450f;
    private const float CardHeight = 590f;

    private const float HeaderHeight = 90f;
    private const float TypeBarHeight = 42f;

    private const float RowHeight = 30f;
    private const float ButtonHeight = 48f;
    private const float ButtonSpacing = 8f;
    private const float ButtonAreaPadding = 20f;

    private const float TitleFontSize = 30f;
    private const float TypeFontSize = 17f;
    private const float InfoFontSize = 16f;
    private const float ButtonFontSize = 17f;

    private static readonly Color Navy =
        new Color(0.08f, 0.12f, 0.22f, 1f);

    private static readonly Color Magenta =
        new Color(0.72f, 0.11f, 0.42f, 1f);

    private static readonly Color CardColor =
        new Color(0.96f, 0.96f, 0.95f, 1f);

    private static readonly Color TextColor =
        new Color(0.08f, 0.08f, 0.08f, 1f);

    [MenuItem("Monopoly/Setup Property Management UI")]
    public static void SetupPropertyManagementPanel()
    {
        PropertyManagementPanel panelComponent =
            Object.FindFirstObjectByType<PropertyManagementPanel>(
                FindObjectsInactive.Include);

        if (panelComponent == null)
        {
            Debug.LogError(
                "PropertyManagementPanelSetup: PropertyManagementPanel was not found.");
            return;
        }

        Transform panelTransform = panelComponent.transform;

        Undo.RegisterFullObjectHierarchyUndo(
            panelComponent.gameObject,
            "Setup Property Management Panel");

        TMP_FontAsset font = GetFont();

        Transform propertyNameText =
            FindDeepChild(panelTransform, "PropertyNameText");

        Transform ownerText =
            FindDeepChild(panelTransform, "OwnerText");

        Transform groupText =
            FindDeepChild(panelTransform, "GroupText");

        Transform housesText =
            FindDeepChild(panelTransform, "HousesText");

        Transform statusText =
            FindDeepChild(panelTransform, "StatusText");

        Transform houseButtonTransform =
            FindDeepChild(panelTransform, "BuildHouseButton");

        if (houseButtonTransform == null)
            houseButtonTransform =
                FindDeepChild(panelTransform, "HouseButton");

        Transform hotelButtonTransform =
            FindDeepChild(panelTransform, "BuildHotelButton");

        if (hotelButtonTransform == null)
            hotelButtonTransform =
                FindDeepChild(panelTransform, "HotelButton");

        Transform mortgageButtonTransform =
            FindDeepChild(panelTransform, "MortgageButton");

        Transform unmortgageButtonTransform =
            FindDeepChild(panelTransform, "UnmortgageButton");

        Transform closeButtonTransform =
            FindDeepChild(panelTransform, "CloseButton");

        ConfigurePanelRoot(panelTransform);

        Transform popup =
            GetOrCreate(panelTransform, "PopupBackground");

        ConfigurePopup(popup);

        Transform header = GetOrCreate(popup, "Header");
        ConfigureHeader(header);

        if (propertyNameText != null)
        {
            propertyNameText.SetParent(header, false);

            ConfigureTMP(
                propertyNameText,
                font,
                TitleFontSize,
                Color.white,
                TextAlignmentOptions.Center);

            RectTransform nameRt =
                GetOrAdd<RectTransform>(propertyNameText.gameObject);

            Stretch(nameRt, 20f, 20f, 0f, 0f);
        }

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

        typeBarText.text = "MANAGE PROPERTY";

        Stretch(
            GetOrAdd<RectTransform>(typeBarText.gameObject),
            0f, 0f, 0f, 0f);

        Transform infoArea =
            GetOrCreate(popup, "InformationArea");

        ConfigureInformationArea(infoArea);

        ConfigureInfoText(ownerText, font, "Owner: —");
        ConfigureInfoText(groupText, font, "Group: —");
        ConfigureInfoText(housesText, font, "Houses: 0/4");
        ConfigureInfoText(statusText, font, "Status");

        PlaceInfoText(infoArea, ownerText);
        PlaceInfoText(infoArea, groupText);
        PlaceInfoText(infoArea, housesText);
        PlaceInfoText(infoArea, statusText);

        float buttonAreaHeight =
            (ButtonHeight * 5f) +
            (ButtonSpacing * 4f) +
            (ButtonAreaPadding * 2f);

        Transform buttonArea =
            GetOrCreate(popup, "ButtonArea");

        ConfigureButtonArea(buttonArea, buttonAreaHeight);

        Button houseButton =
            ConfigureActionButton(
                buttonArea,
                houseButtonTransform,
                "BuildHouseButton",
                "BUILD HOUSE",
                font);

        Button hotelButton =
            ConfigureActionButton(
                buttonArea,
                hotelButtonTransform,
                "BuildHotelButton",
                "BUILD HOTEL",
                font);

        Button mortgageButton =
            ConfigureActionButton(
                buttonArea,
                mortgageButtonTransform,
                "MortgageButton",
                "MORTGAGE",
                font);

        Button unmortgageButton =
            ConfigureActionButton(
                buttonArea,
                unmortgageButtonTransform,
                "UnmortgageButton",
                "UNMORTGAGE",
                font);

        Button closeButton =
            ConfigureActionButton(
                buttonArea,
                closeButtonTransform,
                "CloseButton",
                "CLOSE",
                font);

        AssignReferences(
            panelComponent,
            popup.gameObject,
            propertyNameText,
            ownerText,
            groupText,
            housesText,
            statusText,
            houseButton,
            hotelButton,
            mortgageButton,
            unmortgageButton,
            closeButton);

        popup.gameObject.SetActive(false);

        EditorUtility.SetDirty(panelComponent);
        EditorSceneManager.MarkSceneDirty(panelComponent.gameObject.scene);

        Debug.Log(
            "PropertyManagementPanelSetup: UI layout successfully rebuilt.");
    }

    private static void ConfigurePanelRoot(Transform root)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(root.gameObject);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image image = root.GetComponent<Image>();

        if (image != null)
        {
            image.enabled = false;
            image.raycastTarget = false;
        }
    }

    private static void ConfigurePopup(Transform popup)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(popup.gameObject);

        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);

        Image image =
            GetOrAdd<Image>(popup.gameObject);

        image.color = CardColor;
        image.raycastTarget = true;
        image.sprite =
            AssetDatabase.GetBuiltinExtraResource<Sprite>(
                "UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
    }

    private static void ConfigureHeader(Transform header)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(header.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -HeaderHeight);
        rt.offsetMax = new Vector2(0f, 0f);

        Image image =
            GetOrAdd<Image>(header.gameObject);

        image.color = Navy;
        image.raycastTarget = true;
    }

    private static void ConfigureTypeBar(Transform bar)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(bar.gameObject);

        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(0f, -(HeaderHeight + TypeBarHeight));
        rt.offsetMax = new Vector2(0f, -HeaderHeight);

        Image image =
            GetOrAdd<Image>(bar.gameObject);

        image.color = Magenta;
        image.raycastTarget = false;
    }

    private static void ConfigureInformationArea(Transform info)
    {
        float buttonAreaHeight =
            (ButtonHeight * 5f) +
            (ButtonSpacing * 4f) +
            (ButtonAreaPadding * 2f);

        RectTransform rt =
            GetOrAdd<RectTransform>(info.gameObject);

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = new Vector2(25f, buttonAreaHeight);
        rt.offsetMax = new Vector2(
            -25f,
            -(HeaderHeight + TypeBarHeight + 10f));

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(info.gameObject);

        layout.spacing = 5f;
        layout.padding = new RectOffset(5, 5, 5, 5);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        Image image = info.GetComponent<Image>();

        if (image != null)
            image.enabled = false;
    }

    private static void ConfigureButtonArea(
        Transform area,
        float height)
    {
        RectTransform rt =
            GetOrAdd<RectTransform>(area.gameObject);

        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.offsetMin = new Vector2(25f, ButtonAreaPadding);
        rt.offsetMax = new Vector2(-25f, ButtonAreaPadding + height);

        VerticalLayoutGroup layout =
            GetOrAdd<VerticalLayoutGroup>(area.gameObject);

        layout.spacing = ButtonSpacing;
        layout.padding = new RectOffset(0, 0, 0, 0);
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
    }

    private static void ConfigureInfoText(
        Transform textTransform,
        TMP_FontAsset font,
        string placeholder)
    {
        if (textTransform == null)
            return;

        ConfigureTMP(
            textTransform,
            font,
            InfoFontSize,
            TextColor,
            TextAlignmentOptions.Left);

        TextMeshProUGUI text =
            textTransform.GetComponent<TextMeshProUGUI>();

        if (text != null && string.IsNullOrWhiteSpace(text.text))
            text.text = placeholder;

        LayoutElement layout =
            GetOrAdd<LayoutElement>(textTransform.gameObject);

        layout.preferredHeight = RowHeight;
        layout.minHeight = RowHeight;
        layout.flexibleWidth = 1f;
    }

    private static void PlaceInfoText(
        Transform infoArea,
        Transform textTransform)
    {
        if (textTransform == null)
            return;

        textTransform.SetParent(infoArea, false);
    }

    private static Button ConfigureActionButton(
        Transform buttonArea,
        Transform existingButton,
        string fallbackName,
        string label,
        TMP_FontAsset font)
    {
        Button button;

        if (existingButton != null)
        {
            existingButton.SetParent(buttonArea, false);
            button = GetOrAdd<Button>(existingButton.gameObject);
        }
        else
        {
            GameObject buttonObject =
                new GameObject(
                    fallbackName,
                    typeof(RectTransform),
                    typeof(Image),
                    typeof(Button),
                    typeof(LayoutElement));

            Undo.RegisterCreatedObjectUndo(
                buttonObject,
                "Create Management Button");

            buttonObject.transform.SetParent(buttonArea, false);
            button = buttonObject.GetComponent<Button>();
        }

        LayoutElement layout =
            GetOrAdd<LayoutElement>(button.gameObject);

        layout.preferredHeight = ButtonHeight;
        layout.minHeight = ButtonHeight;
        layout.flexibleWidth = 1f;

        Image image =
            GetOrAdd<Image>(button.gameObject);

        image.color = Navy;
        image.raycastTarget = true;
        button.targetGraphic = image;

        TextMeshProUGUI text =
            FindDeepComponent<TextMeshProUGUI>(
                button.transform,
                "Text");

        if (text == null)
        {
            text =
                button.GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (text == null)
        {
            GameObject textObject =
                new GameObject(
                    "Text",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));

            Undo.RegisterCreatedObjectUndo(
                textObject,
                "Create Button Text");

            textObject.transform.SetParent(button.transform, false);
            text = textObject.GetComponent<TextMeshProUGUI>();
        }

        ConfigureTMP(
            text.transform,
            font,
            ButtonFontSize,
            Color.white,
            TextAlignmentOptions.Center);

        text.text = label;

        Stretch(
            GetOrAdd<RectTransform>(text.gameObject),
            0f, 0f, 0f, 0f);

        return button;
    }

    private static void ConfigureTMP(
        Transform transform,
        TMP_FontAsset font,
        float fontSize,
        Color color,
        TextAlignmentOptions alignment)
    {
        TextMeshProUGUI text =
            GetOrAdd<TextMeshProUGUI>(transform.gameObject);

        if (font != null)
            text.font = font;

        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        transform.localScale = Vector3.one;
    }

    private static void AssignReferences(
        PropertyManagementPanel panel,
        GameObject popupBackground,
        Transform propertyNameText,
        Transform ownerText,
        Transform groupText,
        Transform housesText,
        Transform statusText,
        Button houseButton,
        Button hotelButton,
        Button mortgageButton,
        Button unmortgageButton,
        Button closeButton)
    {
        SerializedObject serialized =
            new SerializedObject(panel);

        SetObject(serialized, "panel", popupBackground);
        SetObject(serialized, "propertyNameText", Component<TMP_Text>(propertyNameText));
        SetObject(serialized, "ownerText", Component<TMP_Text>(ownerText));
        SetObject(serialized, "groupText", Component<TMP_Text>(groupText));
        SetObject(serialized, "housesText", Component<TMP_Text>(housesText));
        SetObject(serialized, "statusText", Component<TMP_Text>(statusText));
        SetObject(serialized, "houseButton", houseButton);
        SetObject(serialized, "hotelButton", hotelButton);
        SetObject(serialized, "mortgageButton", mortgageButton);
        SetObject(serialized, "unmortgageButton", unmortgageButton);
        SetObject(serialized, "closeButton", closeButton);

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
            property.objectReferenceValue = value;
    }

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

    private static Transform GetOrCreate(
        Transform parent,
        string name)
    {
        Transform existing = FindDirectChild(parent, name);

        if (existing != null)
            return existing;

        GameObject objectToCreate =
            new GameObject(name, typeof(RectTransform));

        Undo.RegisterCreatedObjectUndo(
            objectToCreate,
            "Create UI Object");

        objectToCreate.transform.SetParent(parent, false);
        return objectToCreate.transform;
    }

    private static TextMeshProUGUI GetOrCreateTMP(
        Transform parent,
        string name)
    {
        Transform existing = FindDirectChild(parent, name);

        if (existing != null)
            return GetOrAdd<TextMeshProUGUI>(existing.gameObject);

        GameObject obj =
            new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));

        Undo.RegisterCreatedObjectUndo(obj, "Create TMP Text");
        obj.transform.SetParent(parent, false);
        return obj.GetComponent<TextMeshProUGUI>();
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

    private static T GetOrAdd<T>(GameObject objectToModify)
        where T : Component
    {
        T component = objectToModify.GetComponent<T>();
        return component != null
            ? component
            : objectToModify.AddComponent<T>();
    }

    private static T Component<T>(Transform transform)
        where T : Component
    {
        return transform == null
            ? null
            : transform.GetComponent<T>();
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

    private static T FindDeepComponent<T>(
        Transform parent,
        string name)
        where T : Component
    {
        Transform transform = FindDeepChild(parent, name);
        return transform == null ? null : transform.GetComponent<T>();
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
