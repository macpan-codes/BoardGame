using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Master visual controller for the 40 existing BoardSpace objects.
/// Presentation only: it does not modify gameplay data.
///
/// Add this to the existing Board GameObject.
/// Use the custom Inspector buttons:
///   1) Sync Profiles
///   2) Build Visuals
///   3) Clear Visuals
/// </summary>
[ExecuteAlways]
public class BoardVisualMaster : MonoBehaviour
{
    public enum VisualKind
    {
        Property,
        Airport,
        Utility,
        Special,
        Corner
    }

    public enum OrientationMode
    {
        Automatic,
        Manual
    }

    [Serializable]
    public class SpaceVisualProfile
    {
        [HideInInspector] public int spaceNumber;

        [Header("Identity")]
        public string displayName = "";
        public VisualKind kind = VisualKind.Property;

        [Header("Surface")]
        public Color backgroundColor = new Color(0.96f, 0.95f, 0.91f, 1f);
        public Color textColor = new Color(0.08f, 0.10f, 0.13f, 1f);
        [Range(0f, 40f)] public float cornerRadius = 16f;
        [Range(0f, 8f)] public float borderWidth = 1.25f;
        public Color borderColor = new Color(0.13f, 0.16f, 0.20f, 0.25f);
        public bool showShadow = true;
        [Range(0.75f, 1f)] public float visualScale = 0.96f;
        public Vector2 visualOffset = Vector2.zero;

        [Header("Orientation")]
        public OrientationMode orientation = OrientationMode.Automatic;
        public float manualRotation = 0f;

        [Header("Property Group")]
        public bool showGroupBar = true;
        public bool useGroupColor = true;
        public string groupName = "";
        public Color groupColor = new Color(0.55f, 0.38f, 0.22f, 1f);

        [Header("Icon")]
        public bool showIcon = false;
        public Sprite icon;

        [Header("Price")]
        public bool showPrice = true;
        public int priceMillions;

        [Header("Typography")]
        [Range(8f, 32f)] public float nameFontSize = 18f;
        [Range(8f, 28f)] public float priceFontSize = 15f;
        [Range(8f, 48f)] public float iconSize = 28f;

        [Header("Color Bar")]
        [Range(4f, 24f)] public float barThickness = 12f;

        [Header("Subtitle")]
        [TextArea(1, 3)] public string subtitle = "";
    }

    [Header("Profiles")]
    [SerializeField] private List<SpaceVisualProfile> spaces = new List<SpaceVisualProfile>(40);

    [Header("Master Defaults")]
    [Tooltip("When enabled in the Unity Editor, changing any profile value immediately rebuilds the board-space visuals.")]
    [SerializeField] private bool livePreview = true;

    [SerializeField] private Color defaultBackground = new Color(0.96f, 0.95f, 0.91f, 1f);
    [SerializeField] private Color defaultText = new Color(0.08f, 0.10f, 0.13f, 1f);
    [SerializeField] private Color defaultMutedText = new Color(0.36f, 0.39f, 0.42f, 1f);

    private const string VisualRootName = "BoardSpaceVisual";

    public List<SpaceVisualProfile> Profiles => spaces;

    public bool LivePreview
    {
        get => livePreview;
        set => livePreview = value;
    }

public void RefreshLivePreview()
    {
        if (!livePreview || Application.isPlaying)
            return;

        BuildMasterVisuals();
    }

    [ContextMenu("Sync Profiles")]
    public void SyncProfilesFromBoard()
    {
        BoardSpace[] boardSpaces = GetBoardSpacesSorted();
        var rebuilt = new List<SpaceVisualProfile>(40);

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            if (boardSpace == null)
                continue;

            int number = ExtractSpaceNumber(boardSpace);
            SpaceVisualProfile oldProfile = FindProfile(number);
            SpaceVisualProfile profile = oldProfile ?? CreateDefaultProfile(boardSpace, number);

            profile.spaceNumber = number;
            if (string.IsNullOrWhiteSpace(profile.displayName))
                profile.displayName = boardSpace.SpaceName;

            if (profile.priceMillions <= 0 && boardSpace.PurchasePrice > 0)
                profile.priceMillions = boardSpace.PurchasePrice;

            rebuilt.Add(profile);
        }

        spaces = rebuilt;

#if UNITY_EDITOR
        MarkDirty();
#endif

        Debug.Log($"BoardVisualMaster: synced {spaces.Count} profiles.");
    }

    [ContextMenu("Build Master Visuals")]
    public void BuildMasterVisuals()
    {
        BoardSpace[] boardSpaces = GetBoardSpacesSorted();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError($"BoardVisualMaster: expected 40 BoardSpace objects, found {boardSpaces.Length}.");
            return;
        }

        if (spaces == null || spaces.Count != 40)
            SyncProfilesFromBoard();

        ClearGeneratedVisuals();

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            SpaceVisualProfile profile = FindProfile(ExtractSpaceNumber(boardSpace));
            if (profile == null)
                profile = CreateDefaultProfile(boardSpace, ExtractSpaceNumber(boardSpace));

            BuildSpace(boardSpace, profile);
        }

#if UNITY_EDITOR
        MarkDirty();
#endif

        Debug.Log("BoardVisualMaster: built visuals for all 40 spaces.");
    }

    [ContextMenu("Clear Master Visuals")]
    public void ClearMasterVisuals()
    {
        ClearGeneratedVisuals();
#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    private void BuildSpace(BoardSpace boardSpace, SpaceVisualProfile profile)
    {
        GameObject rootObject = new GameObject(VisualRootName, typeof(RectTransform), typeof(RoundedRectGraphic));
        rootObject.transform.SetParent(boardSpace.transform, false);

        RectTransform root = rootObject.GetComponent<RectTransform>();
        RectTransform boardSpaceRect = boardSpace.GetComponent<RectTransform>();

        root.anchorMin = new Vector2(0.5f, 0.5f);
        root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.sizeDelta = boardSpaceRect != null ? boardSpaceRect.rect.size : new Vector2(80f, 80f);
        root.anchoredPosition = profile.visualOffset;
        root.localScale = Vector3.one * Mathf.Clamp(profile.visualScale, 0.75f, 1f);
        root.localRotation = Quaternion.Euler(0f, 0f, GetRotation(boardSpace, profile));

        RoundedRectGraphic card = rootObject.GetComponent<RoundedRectGraphic>();
        card.FillColor = profile.backgroundColor;
        card.BorderColor = profile.borderColor;
        card.CornerRadius = profile.cornerRadius;
        card.BorderWidth = profile.borderWidth;
        card.raycastTarget = false;

        if (profile.showShadow)
        {
            Shadow shadow = rootObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.12f);
            shadow.effectDistance = new Vector2(0f, -2f);
            shadow.useGraphicAlpha = true;
        }

        switch (profile.kind)
        {
            case VisualKind.Corner:
                BuildCorner(root, profile);
                break;
            case VisualKind.Special:
                BuildSpecial(root, profile);
                break;
            default:
                BuildStandard(root, profile);
                break;
        }
    }

    private void BuildStandard(RectTransform parent, SpaceVisualProfile profile)
    {
        if (profile.kind == VisualKind.Property && profile.showGroupBar)
        {
            CreateRoundedPanel(
                parent,
                "GroupColorBar",
                new Vector2(0.07f, 0.86f),
                new Vector2(0.93f, 0.98f),
                profile.useGroupColor ? profile.groupColor : profile.groupColor,
                Mathf.Min(profile.cornerRadius, 8f)
            );
        }

        float nameBottom = profile.showIcon && profile.icon != null ? 0.50f : 0.23f;
        float nameTop = profile.kind == VisualKind.Airport || profile.kind == VisualKind.Utility ? 0.72f : 0.82f;

        CreateText(
            parent,
            "DisplayName",
            profile.displayName,
            profile.nameFontSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.07f, nameBottom),
            new Vector2(0.93f, nameTop),
            profile.textColor
        );

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "Subtitle",
                profile.subtitle,
                Mathf.Max(8f, profile.nameFontSize * 0.62f),
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.18f),
                new Vector2(0.92f, 0.31f),
                defaultMutedText
            );
        }

        if (profile.showIcon && profile.icon != null)
            CreateIcon(parent, profile.icon, profile.iconSize);

        if (profile.showPrice)
        {
            CreateText(
                parent,
                "Price",
                $"${profile.priceMillions:N0}M",
                profile.priceFontSize,
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.04f),
                new Vector2(0.92f, 0.18f),
                profile.textColor
            );
        }

        if (profile.kind == VisualKind.Airport || profile.kind == VisualKind.Utility)
        {
            CreateText(
                parent,
                "TypeLabel",
                profile.kind == VisualKind.Airport ? "AIRPORT" : "UTILITY",
                Mathf.Max(8f, profile.nameFontSize * 0.50f),
                FontStyles.Bold,
                TextAlignmentOptions.Center,
                new Vector2(0.08f, 0.82f),
                new Vector2(0.92f, 0.98f),
                profile.textColor
            );
        }
    }

    private void BuildSpecial(RectTransform parent, SpaceVisualProfile profile)
    {
        if (profile.showIcon && profile.icon != null)
            CreateIcon(parent, profile.icon, profile.iconSize);

        CreateText(
            parent,
            "DisplayName",
            profile.displayName,
            profile.nameFontSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.06f, 0.29f),
            new Vector2(0.94f, 0.65f),
            profile.textColor
        );

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "Subtitle",
                profile.subtitle,
                profile.priceFontSize,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.07f),
                new Vector2(0.94f, 0.22f),
                defaultMutedText
            );
        }
    }

    private void BuildCorner(RectTransform parent, SpaceVisualProfile profile)
    {
        if (profile.showIcon && profile.icon != null)
            CreateIcon(parent, profile.icon, Mathf.Max(profile.iconSize, 34f));

        CreateText(
            parent,
            "CornerTitle",
            profile.displayName,
            Mathf.Max(profile.nameFontSize, 18f),
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.06f, 0.27f),
            new Vector2(0.94f, 0.56f),
            profile.textColor
        );

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "CornerSubtitle",
                profile.subtitle,
                profile.priceFontSize,
                FontStyles.Normal,
                TextAlignmentOptions.Center,
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.22f),
                defaultMutedText
            );
        }
    }

    private GameObject CreateRoundedPanel(
        RectTransform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color,
        float radius)
    {
        GameObject panel = new GameObject(objectName, typeof(RectTransform), typeof(RoundedRectGraphic));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        RoundedRectGraphic graphic = panel.GetComponent<RoundedRectGraphic>();
        graphic.FillColor = color;
        graphic.CornerRadius = radius;
        graphic.BorderWidth = 0f;
        graphic.raycastTarget = false;

        return panel;
    }

    private TMP_Text CreateText(
        RectTransform parent,
        string objectName,
        string value,
        float fontSize,
        FontStyles style,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject obj = new GameObject(objectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = obj.GetComponent<TextMeshProUGUI>();
        text.text = value ?? string.Empty;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;

        return text;
    }

    private void CreateIcon(RectTransform parent, Sprite sprite, float size)
    {
        GameObject obj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.54f);
        rect.anchorMax = new Vector2(0.5f, 0.54f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(size, size);

        Image image = obj.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
    }

    private float GetRotation(BoardSpace boardSpace, SpaceVisualProfile profile)
    {
        if (profile.orientation == OrientationMode.Manual)
            return profile.manualRotation;

        int number = ExtractSpaceNumber(boardSpace);

        if (number >= 1 && number <= 9)
            return 0f;
        if (number >= 11 && number <= 19)
            return 90f;
        if (number >= 21 && number <= 29)
            return 180f;
        if (number >= 31 && number <= 39)
            return -90f;

        return 0f;
    }

    private SpaceVisualProfile CreateDefaultProfile(BoardSpace space, int number)
    {
        var profile = new SpaceVisualProfile();
        profile.spaceNumber = number;
        profile.displayName = space.SpaceName;
        profile.priceMillions = Mathf.Max(0, space.PurchasePrice);
        profile.backgroundColor = defaultBackground;
        profile.textColor = defaultText;
        profile.visualScale = 0.96f;

        if (IsCornerSpace(space))
        {
            profile.kind = VisualKind.Corner;
            profile.showGroupBar = false;
            profile.showPrice = false;
            profile.cornerRadius = 24f;
            profile.nameFontSize = 20f;
        }
        else if (space.IsRailroad)
        {
            profile.kind = VisualKind.Airport;
            profile.showGroupBar = false;
            profile.showPrice = true;
            profile.nameFontSize = 14f;
            profile.iconSize = 30f;
        }
        else if (space.IsUtility)
        {
            profile.kind = VisualKind.Utility;
            profile.showGroupBar = false;
            profile.showPrice = true;
            profile.nameFontSize = 14f;
            profile.iconSize = 30f;
        }
        else if (IsSpecialName(space.SpaceName) || space.SpaceType == BoardSpaceType.Chance || space.SpaceType == BoardSpaceType.CommunityChest)
        {
            profile.kind = VisualKind.Special;
            profile.showGroupBar = false;
            profile.showPrice = false;
        }
        else
        {
            profile.kind = VisualKind.Property;
            profile.showGroupBar = true;
            profile.showPrice = true;

            if (space.PropertyGroup != null)
            {
                profile.groupName = space.PropertyGroup.GroupName;
                profile.groupColor = space.PropertyGroup.GroupColor;
            }
        }

        return profile;
    }

    private bool IsCornerSpace(BoardSpace space)
    {
        if (space == null)
            return false;

        string name = space.SpaceName ?? string.Empty;
        return string.Equals(name, "GO", StringComparison.OrdinalIgnoreCase) ||
               name.IndexOf("Free Parking", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Just Visiting", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.Equals("Jail", StringComparison.OrdinalIgnoreCase) ||
               name.Equals("Go To Jail", StringComparison.OrdinalIgnoreCase);
    }

    private bool IsSpecialName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return name.IndexOf("Chance", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Community Chest", StringComparison.OrdinalIgnoreCase) >= 0 ||
               name.IndexOf("Tax", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private BoardSpace[] GetBoardSpacesSorted()
    {
        BoardSpace[] spacesFound = GetComponentsInChildren<BoardSpace>(true);
        Array.Sort(spacesFound, CompareBoardSpaces);
        return spacesFound;
    }

    private int CompareBoardSpaces(BoardSpace a, BoardSpace b)
    {
        return ExtractSpaceNumber(a).CompareTo(ExtractSpaceNumber(b));
    }

    private int ExtractSpaceNumber(BoardSpace space)
    {
        if (space == null)
            return 999;

        string name = space.gameObject.name;
        if (!name.StartsWith("Space_", StringComparison.OrdinalIgnoreCase))
            return 999;

        return int.TryParse(name.Substring(6), out int number) ? number : 999;
    }

    private SpaceVisualProfile FindProfile(int number)
    {
        if (spaces == null)
            return null;

        foreach (SpaceVisualProfile profile in spaces)
        {
            if (profile != null && profile.spaceNumber == number)
                return profile;
        }

        return null;
    }

    private void ClearGeneratedVisuals()
    {
        BoardSpace[] boardSpaces = GetComponentsInChildren<BoardSpace>(true);
        foreach (BoardSpace boardSpace in boardSpaces)
        {
            if (boardSpace == null)
                continue;

            Transform visual = boardSpace.transform.Find(VisualRootName);
            if (visual == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(visual.gameObject);
            else
#endif
                Destroy(visual.gameObject);
        }
    }

#if UNITY_EDITOR
    private void MarkDirty()
    {
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
