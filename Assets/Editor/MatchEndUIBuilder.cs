#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates an editable Match-End UI hierarchy in the open gameplay scene.
/// Run: Board Game > UI > Create Editable Match-End UI.
/// The created GameObjects are normal scene objects and can be restyled/resized.
/// </summary>
public static class MatchEndUIBuilder
{
    private const string CanvasName = "MatchEndUICanvas";

    private static readonly Color Navy = new Color(0.035f, 0.055f, 0.10f, 0.98f);
    private static readonly Color PanelColor = new Color(0.075f, 0.10f, 0.16f, 0.99f);
    private static readonly Color ButtonColor = new Color(0.16f, 0.22f, 0.32f, 1f);
    private static readonly Color Green = new Color(0.14f, 0.42f, 0.31f, 1f);
    private static readonly Color Red = new Color(0.55f, 0.20f, 0.20f, 1f);
    private static readonly Color Gold = new Color(0.96f, 0.74f, 0.30f, 1f);
    private static readonly Color White = new Color(0.95f, 0.96f, 0.98f, 1f);
    private static readonly Color Muted = new Color(0.69f, 0.73f, 0.80f, 1f);

    [MenuItem("Board Game/UI/Create Editable Match-End UI")]
    public static void CreateEditableUI()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Editable Match-End UI",
                "Stop Play Mode before creating or replacing the UI.",
                "OK"
            );
            return;
        }

        GameManager manager = Object.FindFirstObjectByType<GameManager>();
        if (manager == null)
        {
            EditorUtility.DisplayDialog(
                "Editable Match-End UI",
                "Open the gameplay scene containing GameManager first.",
                "OK"
            );
            return;
        }

        GameObject existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            bool replace = EditorUtility.DisplayDialog(
                "Replace Match-End UI?",
                "A MatchEndUICanvas already exists in this scene. Replacing it will discard your edits to that canvas.",
                "Replace UI",
                "Cancel"
            );

            if (!replace)
                return;

            Undo.DestroyObjectImmediate(existing);
        }

        MatchEndUI oldController = manager.GetComponent<MatchEndUI>();
        if (oldController != null)
            Undo.DestroyObjectImmediate(oldController);

        GameObject canvasObject = CreateObject(CanvasName, null, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 1000;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform canvasRect = canvasObject.GetComponent<RectTransform>();
        Stretch(canvasRect);

        Button menuButton;
        TMP_Text menuButtonLabel;
        menuButton = CreateButton(
            canvasObject.transform,
            "MenuButton",
            "MAIN MENU",
            new Vector2(1f, 1f),
            new Vector2(-24f, -24f),
            new Vector2(190f, 56f),
            ButtonColor,
            out menuButtonLabel
        );

        GameObject overlay = CreateObject("MatchEndOverlay", canvasObject.transform, typeof(RectTransform), typeof(Image));
        RectTransform overlayRect = overlay.GetComponent<RectTransform>();
        Stretch(overlayRect);
        Image overlayImage = overlay.GetComponent<Image>();
        overlayImage.color = new Color(0.015f, 0.025f, 0.045f, 0.88f);
        overlayImage.raycastTarget = true;

        GameObject mainMenu = CreatePanel(overlay.transform, "MainMenuPanel", 620f, 500f);
        AddText(mainMenu.transform, "PanelTitle", "MAIN MENU", 34f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -42f), new Vector2(0f, 48f));
        AddText(mainMenu.transform, "PanelSubtitle", "Choose what to do next.", 20f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(0f, 34f));

        Button menuVote = CreateButton(mainMenu.transform, "VoteToEndButton", "VOTE TO END MATCH",
            new Vector2(0.5f, 1f), new Vector2(0f, -165f), new Vector2(440f, 60f), Green, out _);
        Button menuLeave = CreateButton(mainMenu.transform, "LeaveGameButton", "LEAVE GAME",
            new Vector2(0.5f, 1f), new Vector2(0f, -240f), new Vector2(440f, 60f), Red, out _);
        Button menuClose = CreateButton(mainMenu.transform, "CloseMenuButton", "CLOSE",
            new Vector2(0.5f, 0f), new Vector2(0f, 28f), new Vector2(200f, 48f), ButtonColor, out _);

        GameObject votePanel = CreatePanel(overlay.transform, "VotePanel", 760f, 690f);
        AddText(votePanel.transform, "PanelTitle", "VOTE TO END MATCH", 30f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(0f, 44f));
        AddText(votePanel.transform, "VoteInstruction", "Every active player must vote YES. Any NO cancels the vote.", 18f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -83f), new Vector2(0f, 54f));
        Transform voteRows = CreateRowsContainer(votePanel.transform, "VoteRows", new Vector2(0.05f, 0.13f), new Vector2(0.95f, 0.78f), 7f);
        GameObject voteTemplate = CreateVoteRowTemplate(voteRows);
        Button voteCancel = CreateButton(votePanel.transform, "CancelVoteButton", "CANCEL VOTE",
            new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(210f, 46f), ButtonColor, out _);

        GameObject leavePanel = CreatePanel(overlay.transform, "LeavePlayerPanel", 660f, 650f);
        AddText(leavePanel.transform, "PanelTitle", "LEAVE GAME", 30f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -35f), new Vector2(0f, 44f));
        AddText(leavePanel.transform, "LeaveInstruction", "Select a player. Their properties will return to the bank.", 18f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.06f, 1f), new Vector2(0.94f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -83f), new Vector2(0f, 52f));
        Transform leaveRows = CreateRowsContainer(leavePanel.transform, "LeavePlayerRows", new Vector2(0.08f, 0.14f), new Vector2(0.92f, 0.78f), 8f);
        GameObject leaveTemplate = CreateLeaveRowTemplate(leaveRows);
        Button leaveBack = CreateButton(leavePanel.transform, "LeavePlayerBackButton", "BACK",
            new Vector2(0.5f, 0f), new Vector2(0f, 20f), new Vector2(190f, 46f), ButtonColor, out _);

        GameObject confirmPanel = CreatePanel(overlay.transform, "LeaveConfirmPanel", 680f, 450f);
        AddText(confirmPanel.transform, "PanelTitle", "CONFIRM LEAVE", 30f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(0f, 44f));
        TMP_Text confirmText = AddText(confirmPanel.transform, "LeaveConfirmText",
            "Are you sure this player wants to leave? All owned assets will be cleared.",
            21f, White, TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.79f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Button confirmYes = CreateButton(confirmPanel.transform, "ConfirmLeaveYesButton", "YES, LEAVE",
            new Vector2(0.35f, 0f), new Vector2(-8f, 36f), new Vector2(210f, 52f), Red, out _);
        Button confirmCancel = CreateButton(confirmPanel.transform, "ConfirmLeaveCancelButton", "CANCEL",
            new Vector2(0.65f, 0f), new Vector2(8f, 36f), new Vector2(210f, 52f), ButtonColor, out _);

        GameObject resultsPanel = CreatePanel(overlay.transform, "FinalResultsPanel", 1600f, 960f);
        AddText(resultsPanel.transform, "StarsText", "★      ★      ★", 28f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.05f, 1f), new Vector2(0.95f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(0f, 36f));
        TMP_Text winnerText = AddText(resultsPanel.transform, "WinnerText", "PLAYER WINS", 38f, Gold, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -68f), new Vector2(0f, 52f));
        TMP_Text reasonText = AddText(resultsPanel.transform, "ReasonText", "MATCH WON BY ELIMINATION", 17f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -111f), new Vector2(0f, 30f));
        AddText(resultsPanel.transform, "ReportHeader", "FINAL FINANCIAL REPORT", 21f, White, TextAlignmentOptions.Center,
            new Vector2(0.04f, 1f), new Vector2(0.96f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -152f), new Vector2(0f, 32f));

        Transform resultRows = CreateRowsContainer(resultsPanel.transform, "ResultRows", new Vector2(0.025f, 0.13f), new Vector2(0.975f, 0.76f), 5f);
        GameObject resultTemplate = CreateResultRowTemplate(resultRows);

        TMP_Text valuationNote = AddText(resultsPanel.transform, "ValuationNote",
            "Net worth = cash + property value + building cost − mortgage payoff (110% of mortgage value).",
            14f, Muted, TextAlignmentOptions.Center,
            new Vector2(0.03f, 0f), new Vector2(0.97f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 58f), new Vector2(0f, 24f));
        Button newMatch = CreateButton(resultsPanel.transform, "NewMatchButton", "NEW MATCH",
            new Vector2(0.5f, 0f), new Vector2(-125f, 15f), new Vector2(220f, 46f), Green, out _);
        Button closeResults = CreateButton(resultsPanel.transform, "CloseResultsButton", "CLOSE REPORT",
            new Vector2(0.5f, 0f), new Vector2(125f, 15f), new Vector2(220f, 46f), ButtonColor, out _);

        mainMenu.SetActive(false);
        votePanel.SetActive(false);
        leavePanel.SetActive(false);
        confirmPanel.SetActive(false);
        resultsPanel.SetActive(false);
        overlay.SetActive(false);
        voteTemplate.SetActive(false);
        leaveTemplate.SetActive(false);
        resultTemplate.SetActive(false);

        if (Object.FindFirstObjectByType<EventSystem>() == null)
        {
            GameObject eventSystem = CreateObject("EventSystem", null, typeof(EventSystem), typeof(StandaloneInputModule));
            eventSystem.transform.SetAsLastSibling();
        }

        MatchEndUI controller = Undo.AddComponent<MatchEndUI>(manager.gameObject);
        SerializedObject serializedController = new SerializedObject(controller);
        SetRef(serializedController, "overlay", overlay);
        SetRef(serializedController, "menuButton", menuButton);
        SetRef(serializedController, "menuButtonLabel", menuButtonLabel);
        SetRef(serializedController, "mainMenuPanel", mainMenu);
        SetRef(serializedController, "menuVoteButton", menuVote);
        SetRef(serializedController, "menuLeaveButton", menuLeave);
        SetRef(serializedController, "menuCloseButton", menuClose);
        SetRef(serializedController, "votePanel", votePanel);
        SetRef(serializedController, "voteRows", voteRows);
        SetRef(serializedController, "voteRowTemplate", voteTemplate);
        SetRef(serializedController, "voteCancelButton", voteCancel);
        SetRef(serializedController, "leavePlayerPanel", leavePanel);
        SetRef(serializedController, "leavePlayerRows", leaveRows);
        SetRef(serializedController, "leavePlayerRowTemplate", leaveTemplate);
        SetRef(serializedController, "leavePlayerBackButton", leaveBack);
        SetRef(serializedController, "leaveConfirmPanel", confirmPanel);
        SetRef(serializedController, "leaveConfirmText", confirmText);
        SetRef(serializedController, "leaveConfirmYesButton", confirmYes);
        SetRef(serializedController, "leaveConfirmCancelButton", confirmCancel);
        SetRef(serializedController, "finalResultsPanel", resultsPanel);
        SetRef(serializedController, "finalWinnerText", winnerText);
        SetRef(serializedController, "finalReasonText", reasonText);
        SetRef(serializedController, "resultRows", resultRows);
        SetRef(serializedController, "resultRowTemplate", resultTemplate);
        SetRef(serializedController, "valuationNoteText", valuationNote);
        SetRef(serializedController, "newMatchButton", newMatch);
        SetRef(serializedController, "closeResultsButton", closeResults);
        serializedController.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(controller);
        EditorUtility.SetDirty(manager);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        Selection.activeGameObject = canvasObject;
        EditorUtility.DisplayDialog(
            "Editable Match-End UI Created",
            "Editable UI objects were created in the open scene and connected to GameManager. Save the scene (Ctrl+S). You can now resize panels, rename text, change colors, and add sticker/sprite objects in the Hierarchy.",
            "OK"
        );
    }

    private static GameObject CreatePanel(Transform parent, string name, float width, float height)
    {
        GameObject panel = CreateObject(name, parent, typeof(RectTransform), typeof(Image));
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(width, height);
        Image image = panel.GetComponent<Image>();
        image.color = PanelColor;
        image.raycastTarget = true;
        return panel;
    }

    private static Transform CreateRowsContainer(
        Transform parent,
        string name,
        Vector2 anchorMin,
        Vector2 anchorMax,
        float spacing)
    {
        GameObject container = CreateObject(name, parent, typeof(RectTransform), typeof(VerticalLayoutGroup));
        RectTransform rect = container.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = container.GetComponent<VerticalLayoutGroup>();
        layout.spacing = spacing;
        layout.padding = new RectOffset(4, 4, 4, 4);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        return container.transform;
    }

    private static GameObject CreateVoteRowTemplate(Transform parent)
    {
        GameObject row = CreateObject("VoteRowTemplate", parent, typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        row.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.19f, 1f);
        LayoutElement element = row.GetComponent<LayoutElement>();
        element.preferredHeight = 58f;
        HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 5, 5);
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = true;

        TMP_Text name = AddText(row.transform, "VoterName", "Player Name", 19f, White, TextAlignmentOptions.Left,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        LayoutElement nameLayout = name.gameObject.AddComponent<LayoutElement>();
        nameLayout.flexibleWidth = 1f;

        Button yesButton = CreateButton(row.transform, "YesButton", "YES", Vector2.zero, Vector2.zero, new Vector2(88f, 42f), Green, out _);
        LayoutElement yesElement = yesButton.gameObject.AddComponent<LayoutElement>();
        yesElement.preferredWidth = 88f;
        yesElement.preferredHeight = 42f;

        Button noButton = CreateButton(row.transform, "NoButton", "NO", Vector2.zero, Vector2.zero, new Vector2(78f, 42f), Red, out _);
        LayoutElement noElement = noButton.gameObject.AddComponent<LayoutElement>();
        noElement.preferredWidth = 78f;
        noElement.preferredHeight = 42f;
        return row;
    }

    private static GameObject CreateLeaveRowTemplate(Transform parent)
    {
        Button button = CreateButton(parent, "LeavePlayerRowTemplate", "PLAYER NAME — LEAVE GAME",
            Vector2.zero, Vector2.zero, new Vector2(440f, 54f), Red, out _);
        LayoutElement element = button.gameObject.AddComponent<LayoutElement>();
        element.preferredHeight = 56f;
        return button.gameObject;
    }

    private static GameObject CreateResultRowTemplate(Transform parent)
    {
        GameObject row = CreateObject("ResultRowTemplate", parent, typeof(RectTransform), typeof(Image), typeof(LayoutElement));
        row.GetComponent<Image>().color = new Color(0.10f, 0.13f, 0.19f, 1f);
        row.GetComponent<LayoutElement>().preferredHeight = 68f;

        AddText(row.transform, "PlayerSummary", "#1 Player Name [STATUS]\nCASH $0 | PROPERTIES 0 / $0 | BUILDINGS $0 | MORTGAGE -$0 | NET WORTH $0",
            15f, White, TextAlignmentOptions.Left,
            new Vector2(0.02f, 0.05f), new Vector2(0.98f, 0.95f), new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        return row;
    }

    private static Button CreateButton(
        Transform parent,
        string name,
        string label,
        Vector2 anchor,
        Vector2 position,
        Vector2 size,
        Color color,
        out TMP_Text labelText)
    {
        GameObject buttonObject = CreateObject(name, parent, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Image image = buttonObject.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = color;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.18f);
        colors.pressedColor = Color.Lerp(color, Color.black, 0.15f);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;

        labelText = AddText(buttonObject.transform, "Label", label, 18f, White, TextAlignmentOptions.Center,
            Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        labelText.raycastTarget = false;
        return button;
    }

    private static TMP_Text AddText(
        Transform parent,
        string name,
        string value,
        float size,
        Color color,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Vector2 pivot,
        Vector2 position,
        Vector2 rectSize)
    {
        GameObject textObject = CreateObject(name, parent, typeof(RectTransform), typeof(TextMeshProUGUI));
        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = position;
        rect.sizeDelta = rectSize;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = alignment;
        text.enableWordWrapping = true;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        if (TMP_Settings.defaultFontAsset != null)
            text.font = TMP_Settings.defaultFontAsset;

        return text;
    }

    private static GameObject CreateObject(string name, Transform parent, params System.Type[] components)
    {
        GameObject go = new GameObject(name, components);
        Undo.RegisterCreatedObjectUndo(go, "Create Editable Match-End UI");
        if (parent != null)
            go.transform.SetParent(parent, false);
        return go;
    }

    private static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void SetRef(SerializedObject target, string propertyName, Object value)
    {
        SerializedProperty property = target.FindProperty(propertyName);
        if (property == null)
        {
            Debug.LogError("MatchEndUIBuilder: Missing serialized field " + propertyName);
            return;
        }

        property.objectReferenceValue = value;
    }
}
#endif
