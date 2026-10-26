using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime-created My Properties portfolio. Uses the existing board/game state
/// and opens the existing PropertyManagementPanel for actions.
/// </summary>
public class MyPropertiesPortfolioUI : MonoBehaviour
{
    private GameManager gameManager;
    private Canvas canvas;
    private GameObject panel;
    private Transform listContent;
    private Text summaryText;
    private Text detailText;
    private string activeFilter = "ALL";
    private BoardSpace selectedSpace;
    private readonly List<GameObject> generatedRows = new List<GameObject>();
    private Font uiFont;
    private float nextRefreshTime;

    private static readonly Color Ink = new Color(0.07f, 0.10f, 0.16f, 0.98f);
    private static readonly Color Card = new Color(0.12f, 0.16f, 0.23f, 1f);
    private static readonly Color Muted = new Color(0.68f, 0.73f, 0.81f, 1f);
    private static readonly Color Accent = new Color(0.30f, 0.83f, 0.70f, 1f);

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("My Properties: no Canvas found; portfolio UI was not created.");
            enabled = false;
            return;
        }

        CreateOpenButton();
        CreatePortfolioPanel();
        panel.SetActive(false);
    }

    private void Update()
    {
        if (panel != null && panel.activeSelf && Time.unscaledTime >= nextRefreshTime)
        {
            nextRefreshTime = Time.unscaledTime + 0.5f;
            if (gameManager == null)
                gameManager = FindFirstObjectByType<GameManager>();
            RefreshPortfolio();
        }
    }

    private void CreateOpenButton()
    {
        GameObject buttonObject = MakeUI("MyPropertiesButton", canvas.transform, typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1, 1);
        rect.anchorMax = new Vector2(1, 1);
        rect.pivot = new Vector2(1, 1);
        rect.anchoredPosition = new Vector2(-24, -24);
        rect.sizeDelta = new Vector2(178, 48);
        buttonObject.GetComponent<Image>().color = Accent;
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.AddListener(OpenPanel);
        Text label = MakeText("Label", buttonObject.transform, "MY PROPERTIES", 16, Color.black, TextAnchor.MiddleCenter);
        Stretch(label.rectTransform, 8, 4, 8, 4);
    }

    private void CreatePortfolioPanel()
    {
        panel = MakeUI("MyPropertiesPortfolio", canvas.transform, typeof(RectTransform), typeof(Image));
        RectTransform root = panel.GetComponent<RectTransform>();
        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = new Vector2(820, 620);
        root.anchoredPosition = Vector2.zero;
        panel.GetComponent<Image>().color = Ink;

        Text title = MakeText("Title", panel.transform, "PROPERTY PORTFOLIO", 24, Color.white, TextAnchor.MiddleLeft);
        Place(title.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(22, -18), new Vector2(-76, -62));

        Button close = MakeButton("CloseX", panel.transform, "×", new Color(0.83f, 0.27f, 0.33f, 1f), 30);
        RectTransform closeRect = close.GetComponent<RectTransform>();
        closeRect.anchorMin = new Vector2(1, 1);
        closeRect.anchorMax = new Vector2(1, 1);
        closeRect.pivot = new Vector2(1, 1);
        closeRect.anchoredPosition = new Vector2(-16, -14);
        closeRect.sizeDelta = new Vector2(44, 44);
        close.onClick.AddListener(ClosePanel);

        summaryText = MakeText("Summary", panel.transform, "Owned 0   •   Active 0   •   Mortgaged 0", 15, Muted, TextAnchor.MiddleLeft);
        Place(summaryText.rectTransform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(24, -66), new Vector2(-24, -104));

        string[] filters = { "ALL", "ACTIVE", "MORTGAGED", "DEVELOPED" };
        float x = 24;
        foreach (string filter in filters)
        {
            Button filterButton = MakeButton("Filter_" + filter, panel.transform, filter, filter == activeFilter ? Accent : Card, 12);
            RectTransform r = filterButton.GetComponent<RectTransform>();
            r.anchorMin = new Vector2(0, 1);
            r.anchorMax = new Vector2(0, 1);
            r.pivot = new Vector2(0, 1);
            r.anchoredPosition = new Vector2(x, -112);
            r.sizeDelta = new Vector2(filter == "MORTGAGED" ? 128 : 104, 34);
            filterButton.onClick.AddListener(() => { activeFilter = filter; RefreshPortfolio(); });
            x += (filter == "MORTGAGED" ? 138 : 114);
        }

        GameObject scrollObject = MakeUI("PropertyScroll", panel.transform, typeof(RectTransform), typeof(Image), typeof(ScrollRect));
        RectTransform scrollRect = scrollObject.GetComponent<RectTransform>();
        Place(scrollRect, new Vector2(0, 0), new Vector2(0.62f, 1), new Vector2(22, 22), new Vector2(-8, -158));
        scrollObject.GetComponent<Image>().color = new Color(0.09f, 0.12f, 0.18f, 1f);

        GameObject viewport = MakeUI("Viewport", scrollObject.transform, typeof(RectTransform), typeof(Image), typeof(Mask));
        RectTransform viewportRect = viewport.GetComponent<RectTransform>();
        Stretch(viewportRect, 0, 0, 0, 0);
        viewport.GetComponent<Image>().color = Color.white;
        viewport.GetComponent<Mask>().showMaskGraphic = false;

        GameObject contentObject = MakeUI("Content", viewport.transform, typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        RectTransform contentRect = contentObject.GetComponent<RectTransform>();
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = contentObject.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.spacing = 8;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = contentObject.GetComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

        ScrollRect scroll = scrollObject.GetComponent<ScrollRect>();
        scroll.viewport = viewportRect;
        scroll.content = contentRect;
        scroll.horizontal = false;
        scroll.vertical = true;
        listContent = contentObject.transform;

        GameObject details = MakeUI("SelectedPropertyDetails", panel.transform, typeof(RectTransform), typeof(Image));
        RectTransform detailRect = details.GetComponent<RectTransform>();
        Place(detailRect, new Vector2(0.64f, 0), new Vector2(1, 1), new Vector2(0, 22), new Vector2(-22, -158));
        details.GetComponent<Image>().color = Card;
        detailText = MakeText("Details", details.transform, "Select a property to inspect its rent and development.", 15, Color.white, TextAnchor.UpperLeft);
        Stretch(detailText.rectTransform, 16, 16, 16, 16);

        Button manage = MakeButton("ManageSelected", details.transform, "MANAGE PROPERTY", Accent, 13);
        RectTransform manageRect = manage.GetComponent<RectTransform>();
        manageRect.anchorMin = new Vector2(0, 0);
        manageRect.anchorMax = new Vector2(1, 0);
        manageRect.pivot = new Vector2(0.5f, 0);
        manageRect.anchoredPosition = new Vector2(0, 14);
        manageRect.sizeDelta = new Vector2(-28, 42);
        manageRect.offsetMin = new Vector2(14, 14);
        manageRect.offsetMax = new Vector2(-14, 56);
        manage.onClick.AddListener(ManageSelected);
    }

    private void OpenPanel()
    {
        if (panel == null) return;
        selectedSpace = null;
        panel.SetActive(true);
        RefreshPortfolio();
    }

    private void ClosePanel()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void RefreshPortfolio()
    {
        if (listContent == null || gameManager == null) return;
        nextRefreshTime = Time.unscaledTime + 0.5f;

        BoardPlayer player = gameManager.CurrentPlayer;
        BoardSpace[] allSpaces = FindObjectsByType<BoardSpace>(FindObjectsSortMode.None);
        List<BoardSpace> owned = allSpaces
            .Where(s => s != null && player != null && s.Owner == player && s.IsOwned)
            .OrderBy(s => s.BoardIndex)
            .ToList();

        int mortgaged = owned.Count(s => s.IsMortgaged);
        int active = owned.Count - mortgaged;
        if (summaryText != null)
            summaryText.text = $"{player?.PlayerName ?? "Player"}  •  Owned {owned.Count}   /   Active {active}   /   Mortgaged {mortgaged}";

        foreach (GameObject row in generatedRows)
            if (row != null) Destroy(row);
        generatedRows.Clear();

        IEnumerable<BoardSpace> filtered = owned;
        if (activeFilter == "ACTIVE") filtered = filtered.Where(s => !s.IsMortgaged);
        else if (activeFilter == "MORTGAGED") filtered = filtered.Where(s => s.IsMortgaged);
        else if (activeFilter == "DEVELOPED") filtered = filtered.Where(s => s.IsProperty && (s.Houses > 0 || s.HasHotel));

        foreach (BoardSpace space in filtered)
            CreatePropertyRow(space);

        if (selectedSpace == null || !owned.Contains(selectedSpace))
            selectedSpace = owned.FirstOrDefault();
        RefreshDetails();
    }

    private void CreatePropertyRow(BoardSpace space)
    {
        GameObject row = MakeUI("PropertyRow_" + space.BoardIndex, listContent, typeof(RectTransform), typeof(Image), typeof(LayoutElement), typeof(Button));
        generatedRows.Add(row);
        row.GetComponent<Image>().color = selectedSpace == space ? new Color(0.18f, 0.28f, 0.34f, 1f) : Card;
        LayoutElement layout = row.GetComponent<LayoutElement>();
        layout.minHeight = 86;
        layout.preferredHeight = 86;
        Button button = row.GetComponent<Button>();
        button.onClick.AddListener(() => { selectedSpace = space; RefreshPortfolio(); });

        GameObject stripe = MakeUI("GroupColor", row.transform, typeof(RectTransform), typeof(Image));
        RectTransform stripeRect = stripe.GetComponent<RectTransform>();
        Place(stripeRect, new Vector2(0, 0), new Vector2(0, 1), new Vector2(0, 0), new Vector2(7, 0));
        stripe.GetComponent<Image>().color = space.PropertyGroup != null ? space.PropertyGroup.GroupColor : (space.Owner != null ? space.Owner.TokenColor : Accent);

        string group = space.PropertyGroup != null ? space.PropertyGroup.GroupName : TypeLabel(space);
        string status = space.IsMortgaged ? "MORTGAGED" : "ACTIVE";
        string buildings = space.HasHotel ? "1 HOTEL" : $"{space.Houses} HOUSES";
        int rent = space.GetRent();
        string rentLabel = space.IsMortgaged ? "RENT SUSPENDED" : space.IsUtility ? "RENT: DICE × " + (GetOwnedUtilityCount(space.Owner) >= 2 ? "10" : "4") : $"RENT: {rent:N0}M";

        Text main = MakeText("Name", row.transform, space.SpaceName, 16, Color.white, TextAnchor.MiddleLeft);
        Place(main.rectTransform, new Vector2(0, 0.5f), new Vector2(0.66f, 1), new Vector2(18, 0), new Vector2(0, -4));
        Text meta = MakeText("Meta", row.transform, $"{group}  •  COST {space.PurchasePrice:N0}M\n{buildings}  •  {rentLabel}", 11, Muted, TextAnchor.MiddleLeft);
        Place(meta.rectTransform, new Vector2(0, 0), new Vector2(0.76f, 0.56f), new Vector2(18, 4), new Vector2(0, 0));
        Text badge = MakeText("Status", row.transform, status, 10, space.IsMortgaged ? new Color(1f, 0.65f, 0.35f) : Accent, TextAnchor.MiddleRight);
        Place(badge.rectTransform, new Vector2(0.62f, 0.5f), new Vector2(1, 1), new Vector2(0, -2), new Vector2(-12, -8));
    }

    private void RefreshDetails()
    {
        if (detailText == null) return;
        if (selectedSpace == null)
        {
            detailText.text = "NO PROPERTIES YET\n\nPurchase a property to see its portfolio details here.";
            return;
        }

        BoardSpace s = selectedSpace;
        string status = s.IsMortgaged ? "MORTGAGED — rent is suspended" : "ACTIVE";
        string buildings = s.HasHotel ? "Houses: 0\nHotels: 1" : $"Houses: {s.Houses}\nHotels: 0";
        string rent = s.IsMortgaged ? "0M (mortgaged)" :
            s.IsUtility ? $"Variable: dice roll × {(GetOwnedUtilityCount(s.Owner) >= 2 ? 10 : 4)}" : $"{s.GetRent():N0}M per landing";
        detailText.text =
            $"{s.SpaceName.ToUpperInvariant()}\n" +
            $"{(s.PropertyGroup != null ? s.PropertyGroup.GroupName : TypeLabel(s))}\n\n" +
            $"PURCHASE COST\n{s.PurchasePrice:N0}M\n\n" +
            $"CURRENT RENT\n{rent}\n\n" +
            $"{buildings}\n\n" +
            $"STATUS\n{status}\n\n" +
            $"MORTGAGE VALUE\n{s.MortgageValue:N0}M";
    }

    private void ManageSelected()
    {
        if (selectedSpace == null || gameManager == null || selectedSpace.Owner != gameManager.CurrentPlayer)
            return;
        PropertyManagementPanel management = FindFirstObjectByType<PropertyManagementPanel>(FindObjectsInactive.Include);
        if (management == null)
        {
            Debug.LogWarning("My Properties: existing PropertyManagementPanel was not found.");
            return;
        }
        ClosePanel();
        management.Show(selectedSpace);
    }

    private int GetOwnedUtilityCount(BoardPlayer player)
    {
        if (player == null) return 0;
        return FindObjectsByType<BoardSpace>(FindObjectsSortMode.None)
            .Count(s => s != null && s.IsUtility && s.Owner == player && s.IsOwned);
    }

    private string TypeLabel(BoardSpace space)
    {
        if (space == null) return "PROPERTY";
        switch (space.SpaceType)
        {
            case BoardSpaceType.Airport: return "AIRPORT";
            case BoardSpaceType.Utility: return "UTILITY";
            default: return "PROPERTY";
        }
    }

    private GameObject MakeUI(string objectName, Transform parent, params System.Type[] components)
    {
        GameObject go = new GameObject(objectName, components);
        go.transform.SetParent(parent, false);
        return go;
    }

    private Text MakeText(string objectName, Transform parent, string value, int size, Color color, TextAnchor anchor)
    {
        GameObject go = MakeUI(objectName, parent, typeof(RectTransform), typeof(Text));
        Text text = go.GetComponent<Text>();
        text.font = uiFont != null ? uiFont : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = anchor;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;
        return text;
    }

    private Button MakeButton(string objectName, Transform parent, string label, Color color, int fontSize)
    {
        GameObject go = MakeUI(objectName, parent, typeof(RectTransform), typeof(Image), typeof(Button));
        go.GetComponent<Image>().color = color;
        Button button = go.GetComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        Text text = MakeText("Label", go.transform, label, fontSize, color == Accent ? Color.black : Color.white, TextAnchor.MiddleCenter);
        Stretch(text.rectTransform, 4, 2, 4, 2);
        return button;
    }

    private void Stretch(RectTransform rect, float left, float bottom, float right, float top)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
    }

    private void Place(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
