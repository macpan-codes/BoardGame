using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif
/// <summary>
/// Master visual controller for the 40 existing board spaces.
/// Presentation only: it never changes BoardSpace gameplay data.
///
/// The scene's BoardGenerator remains untouched and continues to own the board
/// discovery/gameplay route. This component only controls how each BoardSpace
/// looks.
///
/// Each space has an independent profile for:
/// name, background, corner radius, border, shadow, offset, orientation,
/// color-group bar, icon, price, typography, and subtitle.
///
/// Add this component to the existing Board object.
/// Use the custom Inspector buttons:
///     Sync 40 Profiles
///     Build Visuals
///     Clear Visuals
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

    [Serializable]
    public class SpaceVisual
    {
        [HideInInspector] public int spaceNumber;
        [HideInInspector] public string sceneObjectName;

        [Header("IDENTITY")]
        public string displayName = "";
        public VisualKind kind = VisualKind.Property;

        [Header("CARD")]
        public Color background = new Color(0.965f, 0.945f, 0.890f, 1f);
        public Color textColor = new Color(0.08f, 0.10f, 0.13f, 1f);

        [Range(0f, 45f)]
        public float cornerRadius = 16f;

        [Range(0f, 6f)]
        public float borderWidth = 1.25f;

        public Color borderColor = new Color(0.12f, 0.16f, 0.20f, 0.24f);
        public bool shadow = true;

        [Range(0.85f, 1f)]
        public float visualScale = 0.965f;

        public Vector2 visualOffset = Vector2.zero;

        [Header("ORIENTATION")]
        [Tooltip("Normally leave OFF. Board cards remain upright and readable on every side.")]
        public bool manualOrientation = false;

        [Range(-180f, 180f)]
        public float rotation = 0f;

        [Header("PROPERTY GROUP")]
        public bool showColorBar = false;
        public string groupName = "";
        public Color groupColor = Color.white;

        [Header("ICON")]
        public bool showIcon = false;
        public Sprite icon;

        [Range(10f, 120f)]
        public float iconSize = 38f;

        [Header("PRICE")]
        public bool showPrice = false;
        public int priceMillions = 0;

        [Header("TEXT")]
        [Range(8f, 32f)]
        public float nameSize = 16f;

        [Range(8f, 28f)]
        public float priceSize = 14f;

        [Header("SUBTITLE")]
        [TextArea(1, 2)]
        public string subtitle = "";

        [Header("SPECIAL")]
        public Color specialAccent = new Color(0.15f, 0.42f, 0.68f, 1f);
    }

    [Header("MASTER")]
    [SerializeField] private bool livePreview = true;
    [SerializeField] private List<SpaceVisual> spaces = new List<SpaceVisual>(40);

    [Header("POLISHED GROUP COLORS")]
    [SerializeField] private Color brown = ParseHex("8A5A3B");
    [SerializeField] private Color lightBlue = ParseHex("64B9E8");
    [SerializeField] private Color pink = ParseHex("E58AB7");
    [SerializeField] private Color orange = ParseHex("ED963B");
    [SerializeField] private Color red = ParseHex("D95755");
    [SerializeField] private Color yellow = ParseHex("E4C63D");
    [SerializeField] private Color green = ParseHex("5EAD73");
    [SerializeField] private Color blue = ParseHex("4F8FDE");

    private const string GeneratedRootName = "BoardVisualMasterGenerated";

    public List<SpaceVisual> Spaces => spaces;
    public bool LivePreview
    {
        get => livePreview;
        set => livePreview = value;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !livePreview)
            return;

        if (spaces == null || spaces.Count != 40)
            SyncProfiles();

        BuildVisuals();
    }
#endif

    [ContextMenu("SYNC 40 PROFILES")]
    public void SyncProfiles()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError(
                $"BoardVisualMaster: Expected 40 BoardSpace objects under Board, found {boardSpaces.Length}."
            );
            return;
        }

        var old = spaces ?? new List<SpaceVisual>();
        var rebuilt = new List<SpaceVisual>(40);

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            int number = GetSpaceNumber(boardSpace);
            SpaceVisual profile = FindProfileIn(old, number);

            if (profile == null)
                profile = CreateDefaultProfile(boardSpace, number);
            else
                RefreshAutomaticMetadata(profile, boardSpace, number);

            profile.spaceNumber = number;
            profile.sceneObjectName = boardSpace.gameObject.name;
            rebuilt.Add(profile);
        }

        spaces = rebuilt;

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    [ContextMenu("BUILD BOARD")]
    public void BuildVisuals()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError(
                $"BoardVisualMaster: Expected 40 BoardSpace objects under Board, found {boardSpaces.Length}."
            );
            return;
        }

        EnsureProfiles();
        ApplyGoalGeometry(boardSpaces);
        ClearGenerated();

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            SpaceVisual profile =
                FindProfile(GetSpaceNumber(boardSpace));

            if (profile == null)
                continue;

            BuildSpaceVisual(
                boardSpace,
                profile
            );
        }

#if UNITY_EDITOR
        MarkDirty();
#endif

        Debug.Log(
            "BoardVisualMaster: Complete board visual layout built."
        );
    }

    [ContextMenu("APPLY GEOMETRY ONLY")]
    public void ApplyGeometryOnly()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError(
                $"BoardVisualMaster: Expected 40 BoardSpace objects under Board, found {boardSpaces.Length}."
            );
            return;
        }

        ApplyGoalGeometry(boardSpaces);

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    [ContextMenu("CLEAR VISUALS")]
    public void ClearVisuals()
    {
        ClearGenerated();

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    private void BuildSpaceVisual(
        BoardSpace boardSpace,
        SpaceVisual profile)
    {
        GameObject rootObject = new GameObject(
            GeneratedRootName,
            typeof(RectTransform)
        );

        rootObject.transform.SetParent(
            boardSpace.transform,
            false
        );

        RectTransform root = rootObject.GetComponent<RectTransform>();
        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.anchoredPosition = profile.visualOffset;
        root.localScale = Vector3.one * Mathf.Clamp(profile.visualScale, 0.85f, 1f);
        root.localRotation = Quaternion.Euler(
            0f,
            0f,
            profile.manualOrientation ? profile.rotation : 0f
        );

        BoardRoundedGraphic card =
            rootObject.AddComponent<BoardRoundedGraphic>();

        card.FillColor = profile.background;
        card.BorderColor = profile.borderColor;
        card.CornerRadius = profile.cornerRadius;
        card.BorderWidth = profile.borderWidth;
        card.raycastTarget = false;

        if (profile.shadow)
        {
            Shadow shadow = rootObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0f, 0f, 0f, 0.14f);
            shadow.effectDistance = new Vector2(0f, -2f);
        }

        switch (profile.kind)
        {
            case VisualKind.Property:
                BuildProperty(root, profile);
                break;

            case VisualKind.Airport:
            case VisualKind.Utility:
                BuildTransport(root, profile);
                break;

            case VisualKind.Special:
                BuildSpecial(root, profile);
                break;

            case VisualKind.Corner:
                BuildCorner(root, profile);
                break;
        }
    }

    private void BuildProperty(
        RectTransform parent,
        SpaceVisual profile)
    {
        int number = FindSpaceNumberFromGeneratedParent(parent);

        Vector2 barMin;
        Vector2 barMax;
        GetInwardBar(number, out barMin, out barMax);

        if (profile.showColorBar)
        {
            CreateRoundedPanel(
                parent,
                "GroupColorBar",
                barMin,
                barMax,
                profile.groupColor,
                Mathf.Min(profile.cornerRadius * 0.55f, 10f)
            );
        }

        CreateText(
            parent,
            "PropertyName",
            profile.displayName,
            profile.nameSize,
            FontStyles.Bold,
            new Vector2(0.06f, 0.46f),
            new Vector2(0.94f, 0.78f),
            profile.textColor
        );

        if (profile.showIcon && profile.icon != null)
        {
            CreateIcon(
                parent,
                profile.icon,
                profile.iconSize
            );
        }

        if (profile.showPrice)
        {
            CreateText(
                parent,
                "Price",
                $"${profile.priceMillions:N0}M",
                profile.priceSize,
                FontStyles.Bold,
                new Vector2(0.06f, 0.06f),
                new Vector2(0.94f, 0.20f),
                profile.textColor
            );
        }
    }

    private void BuildTransport(
        RectTransform parent,
        SpaceVisual profile)
    {
        if (profile.showIcon && profile.icon != null)
        {
            CreateIcon(
                parent,
                profile.icon,
                profile.iconSize
            );
        }

        CreateText(
            parent,
            "TransportName",
            profile.displayName,
            profile.nameSize,
            FontStyles.Bold,
            new Vector2(0.05f, 0.30f),
            new Vector2(0.95f, 0.70f),
            profile.textColor
        );

        if (profile.showPrice)
        {
            CreateText(
                parent,
                "TransportPrice",
                $"${profile.priceMillions:N0}M",
                profile.priceSize,
                FontStyles.Bold,
                new Vector2(0.06f, 0.06f),
                new Vector2(0.94f, 0.21f),
                profile.textColor
            );
        }
    }

    private void BuildSpecial(
        RectTransform parent,
        SpaceVisual profile)
    {
        if (profile.showIcon && profile.icon != null)
        {
            CreateIcon(
                parent,
                profile.icon,
                profile.iconSize
            );
        }

        CreateText(
            parent,
            "SpecialName",
            profile.displayName,
            profile.nameSize,
            FontStyles.Bold,
            new Vector2(0.05f, 0.33f),
            new Vector2(0.95f, 0.70f),
            profile.textColor
        );

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "SpecialSubtitle",
                profile.subtitle,
                profile.priceSize,
                FontStyles.Normal,
                new Vector2(0.06f, 0.08f),
                new Vector2(0.94f, 0.24f),
                new Color(0.36f, 0.38f, 0.40f, 1f)
            );
        }
    }

    private void BuildCorner(
        RectTransform parent,
        SpaceVisual profile)
    {
        if (profile.showIcon && profile.icon != null)
        {
            CreateIcon(
                parent,
                profile.icon,
                Mathf.Max(42f, profile.iconSize)
            );
        }

        CreateText(
            parent,
            "CornerName",
            profile.displayName,
            Mathf.Max(18f, profile.nameSize),
            FontStyles.Bold,
            new Vector2(0.05f, 0.25f),
            new Vector2(0.95f, 0.59f),
            profile.textColor
        );

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "CornerSubtitle",
                profile.subtitle,
                Mathf.Max(10f, profile.priceSize),
                FontStyles.Normal,
                new Vector2(0.05f, 0.08f),
                new Vector2(0.95f, 0.22f),
                new Color(0.36f, 0.38f, 0.40f, 1f)
            );
        }

        if (profile.showPrice)
        {
            CreateText(
                parent,
                "CornerPrice",
                $"${profile.priceMillions:N0}M",
                profile.priceSize,
                FontStyles.Bold,
                new Vector2(0.05f, 0.04f),
                new Vector2(0.95f, 0.14f),
                profile.textColor
            );
        }
    }

    private void GetInwardBar(
        int number,
        out Vector2 min,
        out Vector2 max)
    {
        const float thickness = 0.085f;

        // Bottom: bar on top, facing the center.
        if (number >= 1 && number <= 9)
        {
            min = new Vector2(0.07f, 1f - thickness);
            max = new Vector2(0.93f, 1f);
            return;
        }

        // Right: bar on left, facing the center.
        if (number >= 11 && number <= 19)
        {
            min = new Vector2(0f, 0.07f);
            max = new Vector2(thickness, 0.93f);
            return;
        }

        // Top: bar on bottom, facing the center.
        if (number >= 21 && number <= 29)
        {
            min = new Vector2(0.07f, 0f);
            max = new Vector2(0.93f, thickness);
            return;
        }

        // Left: bar on right, facing the center.
        min = new Vector2(1f - thickness, 0.07f);
        max = new Vector2(1f, 0.93f);
    }

    private void CreateIcon(
        RectTransform parent,
        Sprite sprite,
        float size)
    {
        GameObject obj = new GameObject(
            "Icon",
            typeof(RectTransform),
            typeof(Image)
        );

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

    private GameObject CreateRoundedPanel(
        RectTransform parent,
        string objectName,
        Vector2 min,
        Vector2 max,
        Color color,
        float radius)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        BoardRoundedGraphic graphic =
            obj.AddComponent<BoardRoundedGraphic>();

        graphic.FillColor = color;
        graphic.CornerRadius = radius;
        graphic.BorderWidth = 0f;
        graphic.raycastTarget = false;

        return obj;
    }

    private void CreateText(
        RectTransform parent,
        string objectName,
        string value,
        float size,
        FontStyles style,
        Vector2 min,
        Vector2 max,
        Color color)
    {
        GameObject obj = new GameObject(
            objectName,
            typeof(RectTransform)
        );

        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        text.text = value ?? string.Empty;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.color = color;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
    }

    private int FindSpaceNumberFromGeneratedParent(
        RectTransform generatedParent)
    {
        Transform owner = generatedParent.parent;
        if (owner == null)
            return -1;

        BoardSpace boardSpace =
            owner.GetComponent<BoardSpace>();

        return boardSpace == null
            ? -1
            : GetSpaceNumber(boardSpace);
    }

    private SpaceVisual CreateDefaultProfile(
        BoardSpace boardSpace,
        int number)
    {
        SpaceVisual profile = new SpaceVisual();

        profile.spaceNumber = number;
        profile.sceneObjectName = boardSpace.gameObject.name;
        profile.displayName = GetDisplayName(number);
        profile.kind = DetectKindByNumber(number, boardSpace);

        profile.background =
            new Color(0.965f, 0.945f, 0.890f, 1f);

        profile.textColor =
            new Color(0.08f, 0.10f, 0.13f, 1f);

        profile.cornerRadius =
            profile.kind == VisualKind.Corner
                ? 28f
                : 14f;

        profile.borderWidth = 1.25f;
        profile.borderColor =
            new Color(0.12f, 0.16f, 0.20f, 0.23f);

        profile.shadow = true;
        profile.visualScale = 0.965f;
        profile.visualOffset = Vector2.zero;
        profile.manualOrientation = false;
        profile.rotation = 0f;
        profile.showIcon = false;
        profile.icon = null;
        profile.iconSize = 38f;
        profile.priceMillions = GetExactPrice(number);
        profile.showPrice = IsPropertyOrTransport(number);
        profile.nameSize =
            profile.kind == VisualKind.Corner
                ? 20f
                : profile.kind == VisualKind.Airport ||
                  profile.kind == VisualKind.Utility
                    ? 13f
                    : 16f;
        profile.priceSize = 14f;
        profile.subtitle = GetSubtitle(number);
        profile.specialAccent = GetAccent(profile.kind);
        profile.showColorBar = false;
        profile.groupName = string.Empty;
        profile.groupColor = Color.clear;

        ApplyExactGroup(profile, number);
        return profile;
    }

    private void RefreshAutomaticMetadata(
        SpaceVisual profile,
        BoardSpace boardSpace,
        int number)
    {
        // Keep user-edited visual values, but always keep the identity/type
        // synchronized with the known 40-slot board definition.
        profile.spaceNumber = number;
        profile.sceneObjectName = boardSpace.gameObject.name;
        profile.kind = DetectKindByNumber(number, boardSpace);
        ApplyExactGroup(profile, number);
    }

    private void ApplyExactGroup(
        SpaceVisual profile,
        int number)
    {
        string group;
        Color color;

        profile.showColorBar =
            TryGetGroup(
                number,
                out group,
                out color
            );

        if (profile.showColorBar)
        {
            profile.groupName = group;
            profile.groupColor = color;
        }
        else
        {
            profile.groupName = string.Empty;
            profile.groupColor = Color.clear;
        }

        if (profile.kind == VisualKind.Airport ||
            profile.kind == VisualKind.Utility ||
            profile.kind == VisualKind.Special ||
            profile.kind == VisualKind.Corner)
        {
            profile.showColorBar = false;
            profile.groupName = string.Empty;
            profile.groupColor = Color.clear;
        }
    }

    private bool TryGetGroup(
        int number,
        out string group,
        out Color color)
    {
        switch (number)
        {
            case 1:
            case 3:
                group = "Brown";
                color = brown;
                return true;

            case 6:
            case 8:
            case 9:
                group = "Light Blue";
                color = lightBlue;
                return true;

            case 11:
            case 13:
            case 14:
                group = "Pink";
                color = pink;
                return true;

            case 16:
            case 18:
            case 19:
                group = "Orange";
                color = orange;
                return true;

            case 21:
            case 23:
            case 24:
                group = "Red";
                color = red;
                return true;

            case 26:
            case 27:
            case 29:
                group = "Yellow";
                color = yellow;
                return true;

            case 31:
            case 32:
            case 34:
                group = "Green";
                color = green;
                return true;

            case 37:
            case 39:
                group = "Blue";
                color = blue;
                return true;

            default:
                group = string.Empty;
                color = Color.clear;
                return false;
        }
    }

    private VisualKind DetectKindByNumber(
        int number,
        BoardSpace boardSpace)
    {
        if (number == 0 ||
            number == 10 ||
            number == 20 ||
            number == 30)
        {
            return VisualKind.Corner;
        }

        if (number == 5 ||
            number == 15 ||
            number == 25 ||
            number == 35)
        {
            return VisualKind.Airport;
        }

        if (number == 2 ||
            number == 12 ||
            number == 28 ||
            number == 36)
        {
            return VisualKind.Utility;
        }

        if (number == 4 ||
            number == 7 ||
            number == 17 ||
            number == 22 ||
            number == 33 ||
            number == 38)
        {
            return VisualKind.Special;
        }

        if (boardSpace != null)
        {
            if (boardSpace.SpaceType == BoardSpaceType.Airport)
                return VisualKind.Airport;

            if (boardSpace.SpaceType == BoardSpaceType.Utility)
                return VisualKind.Utility;

            if (boardSpace.SpaceType == BoardSpaceType.Chance ||
                boardSpace.SpaceType == BoardSpaceType.CommunityChest)
            {
                return VisualKind.Special;
            }
        }

        return VisualKind.Property;
    }

    private bool IsPropertyOrTransport(int number)
    {
        VisualKind kind =
            DetectKindByNumber(
                number,
                null
            );

        return kind == VisualKind.Property ||
               kind == VisualKind.Airport ||
               kind == VisualKind.Utility;
    }

    private string GetDisplayName(int number)
    {
        switch (number)
        {
            case 0: return "GO";
            case 1: return "Dhaka";
            case 2: return "Natural Gas Company (Gazprom)";
            case 3: return "Delhi";
            case 4: return "Income Tax";
            case 5: return "Dubai International Airport";
            case 6: return "Istanbul";
            case 7: return "Chance";
            case 8: return "Miami";
            case 9: return "Amsterdam";
            case 10: return "Just Visiting";
            case 11: return "Madrid";
            case 12: return "Electricity Company (NextEra Energy)";
            case 13: return "Seoul";
            case 14: return "Hawaii";
            case 15: return "London Heathrow Airport";
            case 16: return "Beijing";
            case 17: return "Community Chest";
            case 18: return "Texas";
            case 19: return "Alaska";
            case 20: return "Free Parking";
            case 21: return "California";
            case 22: return "Chance";
            case 23: return "Bangkok";
            case 24: return "Kyoto";
            case 25: return "Doha Hamad Airport";
            case 26: return "Moscow";
            case 27: return "Chicago";
            case 28: return "Water Company (Veolia)";
            case 29: return "Venice";
            case 30: return "Jail";
            case 31: return "Florence";
            case 32: return "Edinburgh";
            case 33: return "Community Chest";
            case 34: return "Rome";
            case 35: return "Paris Charles de Gaulle Airport";
            case 36: return "ISP (Seeyam Enterprise)";
            case 37: return "Sydney";
            case 38: return "Super Tax";
            case 39: return "Tokyo";
            default: return "Space";
        }
    }

    private int GetExactPrice(int number)
    {
        switch (number)
        {
            case 1: return 60;
            case 2: return 150;
            case 3: return 70;
            case 5: return 200;
            case 6: return 100;
            case 8: return 100;
            case 9: return 120;
            case 11: return 140;
            case 12: return 150;
            case 13: return 140;
            case 14: return 180;
            case 15: return 200;
            case 16: return 190;
            case 18: return 190;
            case 19: return 200;
            case 21: return 220;
            case 23: return 220;
            case 24: return 240;
            case 25: return 200;
            case 26: return 250;
            case 27: return 260;
            case 28: return 150;
            case 29: return 280;
            case 31: return 300;
            case 32: return 300;
            case 34: return 320;
            case 35: return 200;
            case 36: return 250;
            case 37: return 340;
            case 39: return 400;
            default: return 0;
        }
    }

    private string GetSubtitle(int number)
    {
        switch (number)
        {
            case 0: return "COLLECT $200M";
            case 4: return "PAY $200M";
            case 7: return "DRAW A CARD";
            case 17: return "DRAW A CARD";
            case 20: return "NO EFFECT";
            case 22: return "DRAW A CARD";
            case 30: return "GO TO 10";
            case 33: return "DRAW A CARD";
            case 38: return "PAY $100M";
            default: return string.Empty;
        }
    }

    private Color GetAccent(VisualKind kind)
    {
        switch (kind)
        {
            case VisualKind.Airport:
                return ParseHex("2E91C2");
            case VisualKind.Utility:
                return ParseHex("349B8B");
            case VisualKind.Special:
                return ParseHex("7C4DAA");
            case VisualKind.Corner:
                return ParseHex("1F2E3B");
            default:
                return Color.white;
        }
    }

    private void EnsureProfiles()
    {
        if (spaces == null || spaces.Count != 40)
        {
            SyncProfiles();
            return;
        }

        // Keep authoritative identity/group/price data locked to the 40-slot
        // definition while preserving user-editable presentation settings.
        BoardSpace[] boardSpaces = GetBoardSpaces();

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            int number = GetSpaceNumber(boardSpace);
            SpaceVisual profile = FindProfile(number);

            if (profile == null)
                continue;

            profile.spaceNumber = number;
            profile.sceneObjectName = boardSpace.gameObject.name;
            profile.displayName = GetDisplayName(number);
            profile.priceMillions = GetExactPrice(number);
            profile.kind = DetectKindByNumber(number, boardSpace);

            ApplyExactGroup(profile, number);

            // No automatically invented icons.
            // The user assigns artwork per profile later.
            if (profile.kind == VisualKind.Property ||
                profile.kind == VisualKind.Airport ||
                profile.kind == VisualKind.Utility)
            {
                profile.showIcon = false;
            }
        }
    }

    private void ApplyGoalGeometry(
        BoardSpace[] boardSpaces)
    {
        // Goal board: 1000 x 1000 square, four larger corners,
        // normal perimeter cards with controlled depth.
        const float boardSize = 1000f;
        const float corner = 185f;
        const float normalDepth = 132f;

        float half = boardSize * 0.5f;
        float sideLength = boardSize - corner - corner;
        float normalWidth = sideLength / 9f;

#if UNITY_EDITOR
        foreach (BoardSpace boardSpace in boardSpaces)
        {
            RectTransform rect =
                boardSpace != null
                    ? boardSpace.GetComponent<RectTransform>()
                    : null;

            if (rect != null)
            {
                UnityEditor.Undo.RecordObject(
                    rect,
                    "Build Board Geometry"
                );
            }
        }
#endif

        for (int index = 0; index < 40; index++)
        {
            BoardSpace boardSpace =
                boardSpaces[index];

            if (boardSpace == null)
                continue;

            RectTransform rect =
                boardSpace.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            rect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );

            rect.localRotation =
                Quaternion.identity;

            rect.localScale =
                Vector3.one;

            if (index == 0)
            {
                SetGeometry(
                    rect,
                    new Vector2(
                        -half + corner * 0.5f,
                        -half + corner * 0.5f
                    ),
                    new Vector2(
                        corner,
                        corner
                    )
                );

                continue;
            }

            if (index == 10)
            {
                SetGeometry(
                    rect,
                    new Vector2(
                        half - corner * 0.5f,
                        -half + corner * 0.5f
                    ),
                    new Vector2(
                        corner,
                        corner
                    )
                );

                continue;
            }

            if (index == 20)
            {
                SetGeometry(
                    rect,
                    new Vector2(
                        half - corner * 0.5f,
                        half - corner * 0.5f
                    ),
                    new Vector2(
                        corner,
                        corner
                    )
                );

                continue;
            }

            if (index == 30)
            {
                SetGeometry(
                    rect,
                    new Vector2(
                        -half + corner * 0.5f,
                        half - corner * 0.5f
                    ),
                    new Vector2(
                        corner,
                        corner
                    )
                );

                continue;
            }

            Vector2 position;
            Vector2 size =
                new Vector2(
                    normalWidth,
                    normalDepth
                );

            if (index >= 1 && index <= 9)
            {
                float x =
                    -half +
                    corner +
                    normalWidth *
                    (index - 0.5f);

                position =
                    new Vector2(
                        x,
                        -half +
                        normalDepth * 0.5f
                    );
            }
            else if (index >= 11 && index <= 19)
            {
                float y =
                    -half +
                    corner +
                    normalWidth *
                    (index - 10.5f);

                position =
                    new Vector2(
                        half -
                        normalDepth * 0.5f,
                        y
                    );

                size =
                    new Vector2(
                        normalDepth,
                        normalWidth
                    );
            }
            else if (index >= 21 && index <= 29)
            {
                float x =
                    half -
                    corner -
                    normalWidth *
                    (index - 20.5f);

                position =
                    new Vector2(
                        x,
                        half -
                        normalDepth * 0.5f
                    );
            }
            else
            {
                float y =
                    half -
                    corner -
                    normalWidth *
                    (index - 30.5f);

                position =
                    new Vector2(
                        -half +
                        normalDepth * 0.5f,
                        y
                    );

                size =
                    new Vector2(
                        normalDepth,
                        normalWidth
                    );
            }

            SetGeometry(
                rect,
                position,
                size
            );
        }
    }

    private void SetGeometry(
        RectTransform rect,
        Vector2 position,
        Vector2 size)
    {
        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;
    }

    private BoardSpace[] GetBoardSpaces()
    {
        BoardSpace[] boardSpaces =
            GetComponentsInChildren<BoardSpace>(true);

        Array.Sort(
            boardSpaces,
            (a, b) => GetSpaceNumber(a).CompareTo(GetSpaceNumber(b))
        );

        return boardSpaces;
    }

    private int GetSpaceNumber(BoardSpace boardSpace)
    {
        if (boardSpace == null)
            return 999;

        string name = boardSpace.gameObject.name;

        if (!name.StartsWith(
                "Space_",
                StringComparison.OrdinalIgnoreCase))
        {
            return 999;
        }

        return int.TryParse(
            name.Substring(6),
            out int number
        )
            ? number
            : 999;
    }

    private SpaceVisual FindProfileIn(
        List<SpaceVisual> list,
        int number)
    {
        if (list == null)
            return null;

        foreach (SpaceVisual profile in list)
        {
            if (profile != null &&
                profile.spaceNumber == number)
            {
                return profile;
            }
        }

        return null;
    }

    private SpaceVisual FindProfile(int number)
    {
        return FindProfileIn(spaces, number);
    }

    private void ClearGenerated()
    {
        BoardSpace[] boardSpaces =
            GetComponentsInChildren<BoardSpace>(true);

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            Transform existing =
                boardSpace.transform.Find(
                    GeneratedRootName
                );

            if (existing == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
#endif
                Destroy(existing.gameObject);
        }
    }

    private static Color ParseHex(string hex)
    {
        return ColorUtility.TryParseHtmlString(
            "#" + hex,
            out Color color
        )
            ? color
            : Color.white;
    }

#if UNITY_EDITOR
    private void MarkDirty()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement
            .EditorSceneManager
            .MarkSceneDirty(gameObject.scene);
    }

    [UnityEditor.CustomEditor(typeof(BoardVisualMaster))]
    private class BoardVisualMasterEditor : UnityEditor.Editor
    {
        private Vector2 scroll;
        private readonly bool[] foldouts = new bool[40];

        public override void OnInspectorGUI()
        {
            BoardVisualMaster master =
                (BoardVisualMaster)target;

            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "ONE-CLICK BOARD DESIGNER • visuals/layout only; gameplay data remains untouched.",
                UnityEditor.MessageType.Info
            );

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("SYNC BOARD DATA", GUILayout.Height(28)))
                master.SyncProfiles();

            if (GUILayout.Button("BUILD WHOLE BOARD", GUILayout.Height(28)))
                master.BuildVisuals();

            if (GUILayout.Button("CLEAR GENERATED DESIGN", GUILayout.Height(28)))
                master.ClearVisuals();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(5);

            Draw("livePreview", "Live Preview");

            EditorGUILayout.HelpBox(
                "Automatic build keeps every card upright. Property color bars point toward the board center. No placeholder artwork is generated; assign your own sprites per space.",
                UnityEditor.MessageType.None
            );

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "GROUP COLORS",
                UnityEditor.EditorStyles.boldLabel
            );

            Draw("brown", "Brown");
            Draw("lightBlue", "Light Blue");
            Draw("pink", "Pink");
            Draw("orange", "Orange");
            Draw("red", "Red");
            Draw("yellow", "Yellow");
            Draw("green", "Green");
            Draw("blue", "Blue");

            EditorGUILayout.Space(6);

            SerializedProperty list =
                serializedObject.FindProperty("spaces");

            if (list.arraySize != 40)
            {
                EditorGUILayout.HelpBox(
                    "Press SYNC 40 PROFILES first.",
                    UnityEditor.MessageType.Warning
                );

                serializedObject.ApplyModifiedProperties();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(
                scroll,
                GUILayout.MinHeight(520)
            );

            for (int i = 0; i < list.arraySize; i++)
            {
                SerializedProperty profile =
                    list.GetArrayElementAtIndex(i);

                int number =
                    profile.FindPropertyRelative(
                        "spaceNumber"
                    ).intValue;

                string name =
                    profile.FindPropertyRelative(
                        "displayName"
                    ).stringValue;

                foldouts[i] =
                    EditorGUILayout.Foldout(
                        foldouts[i],
                        $"Space {number:00} — {name}",
                        true
                    );

                if (!foldouts[i])
                    continue;

                EditorGUI.indentLevel++;

                EditorGUILayout.LabelField(
                    "IDENTITY",
                    UnityEditor.EditorStyles.boldLabel
                );

                Draw(profile, "displayName", "Display Name");
                Draw(profile, "kind", "Visual Type");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    "CARD",
                    UnityEditor.EditorStyles.boldLabel
                );

                Draw(profile, "background", "Background");
                Draw(profile, "textColor", "Text Color");
                Draw(profile, "cornerRadius", "Corner Radius");
                Draw(profile, "borderWidth", "Border Width");
                Draw(profile, "borderColor", "Border Color");
                Draw(profile, "shadow", "Shadow");
                Draw(profile, "visualScale", "Scale");
                Draw(profile, "visualOffset", "Position Offset");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    "ORIENTATION",
                    UnityEditor.EditorStyles.boldLabel
                );

                Draw(profile, "manualOrientation", "Manual Orientation");

                if (profile.FindPropertyRelative(
                        "manualOrientation"
                    ).boolValue)
                {
                    Draw(profile, "rotation", "Rotation");
                }

                BoardVisualMaster.VisualKind kind =
                    (BoardVisualMaster.VisualKind)
                    profile.FindPropertyRelative(
                        "kind"
                    ).enumValueIndex;

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    kind == VisualKind.Property
                        ? "PROPERTY"
                        : "SPECIAL SPACE",
                    UnityEditor.EditorStyles.boldLabel
                );

                if (kind == VisualKind.Property)
                {
                    Draw(profile, "showColorBar", "Show Color Bar");
                    Draw(profile, "groupName", "Group Name");
                    Draw(profile, "groupColor", "Group Color");
                }

                Draw(profile, "showIcon", "Show Icon");
                Draw(profile, "icon", "Icon");
                Draw(profile, "iconSize", "Icon Size");
                Draw(profile, "showPrice", "Show Price");
                Draw(profile, "priceMillions", "Price (Millions)");
                Draw(profile, "subtitle", "Subtitle");

                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField(
                    "TEXT",
                    UnityEditor.EditorStyles.boldLabel
                );

                Draw(profile, "nameSize", "Name Size");
                Draw(profile, "priceSize", "Price Size");

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(7);
            }

            EditorGUILayout.EndScrollView();

            bool changed =
                serializedObject.ApplyModifiedProperties();

            if (changed && master.livePreview)
                master.BuildVisuals();
        }

        private void Draw(
            string propertyName,
            string label)
        {
            SerializedProperty property =
                serializedObject.FindProperty(
                    propertyName
                );

            if (property != null)
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent(label),
                    true
                );
            }
        }

        private void Draw(
            SerializedProperty parent,
            string propertyName,
            string label)
        {
            SerializedProperty property =
                parent.FindPropertyRelative(
                    propertyName
                );

            if (property != null)
            {
                EditorGUILayout.PropertyField(
                    property,
                    new GUIContent(label),
                    true
                );
            }
        }
    }
#endif
}

/// <summary>
/// Procedural rounded rectangle used by BoardVisualMaster.
/// No extra sprite asset is required.
/// </summary>
[ExecuteAlways]
[AddComponentMenu("")]
public class BoardRoundedGraphic : MaskableGraphic
{
    [SerializeField] private Color fillColor = Color.white;
    [SerializeField] private Color borderColor = Color.clear;

    [SerializeField, Range(0f, 80f)]
    private float cornerRadius = 16f;

    [SerializeField, Range(0f, 12f)]
    private float borderWidth = 0f;

    [SerializeField, Range(2, 16)]
    private int cornerSegments = 8;

    public Color FillColor
    {
        get => fillColor;
        set
        {
            fillColor = value;
            SetVerticesDirty();
        }
    }

    public Color BorderColor
    {
        get => borderColor;
        set
        {
            borderColor = value;
            SetVerticesDirty();
        }
    }

    public float CornerRadius
    {
        get => cornerRadius;
        set
        {
            cornerRadius = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    public float BorderWidth
    {
        get => borderWidth;
        set
        {
            borderWidth = Mathf.Max(0f, value);
            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(
        VertexHelper vh)
    {
        vh.Clear();

        Rect rect = GetPixelAdjustedRect();

        float maxRadius =
            Mathf.Min(rect.width, rect.height) * 0.5f;

        float radius =
            Mathf.Clamp(
                cornerRadius,
                0f,
                maxRadius
            );

        float border =
            Mathf.Clamp(
                borderWidth,
                0f,
                Mathf.Min(rect.width, rect.height) * 0.25f
            );

        int segments = Mathf.Clamp(
            cornerSegments,
            2,
            16
        );

        Vector2[] outer =
            BuildPoints(rect, radius, segments);

        Rect innerRect =
            new Rect(
                rect.xMin + border,
                rect.yMin + border,
                Mathf.Max(0f, rect.width - border * 2f),
                Mathf.Max(0f, rect.height - border * 2f)
            );

        Vector2[] inner =
            BuildPoints(
                innerRect,
                Mathf.Max(0f, radius - border),
                segments
            );

        int count = outer.Length;
        int outerStart = count + 1;

        UIVertex center = UIVertex.simpleVert;
        center.position = rect.center;
        center.color = fillColor;
        vh.AddVert(center);

        for (int i = 0; i < count; i++)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = inner[i];
            v.color = fillColor;
            vh.AddVert(v);
        }

        for (int i = 0; i < count; i++)
        {
            UIVertex v = UIVertex.simpleVert;
            v.position = outer[i];
            v.color = borderColor;
            vh.AddVert(v);
        }

        for (int i = 0; i < count; i++)
        {
            int next = (i + 1) % count;

            vh.AddTriangle(
                0,
                i + 1,
                next + 1
            );

            if (border > 0.001f &&
                borderColor.a > 0.001f)
            {
                vh.AddTriangle(
                    outerStart + i,
                    outerStart + next,
                    i + 1
                );

                vh.AddTriangle(
                    outerStart + i,
                    i + 1,
                    outerStart + next
                );
            }
        }
    }

    private Vector2[] BuildPoints(
        Rect rect,
        float radius,
        int segments)
    {
        var points = new List<Vector2>(
            (segments + 1) * 4
        );

        AddArc(
            points,
            new Vector2(
                rect.xMax - radius,
                rect.yMax - radius
            ),
            radius,
            0f,
            90f,
            segments
        );

        AddArc(
            points,
            new Vector2(
                rect.xMin + radius,
                rect.yMax - radius
            ),
            radius,
            90f,
            180f,
            segments
        );

        AddArc(
            points,
            new Vector2(
                rect.xMin + radius,
                rect.yMin + radius
            ),
            radius,
            180f,
            270f,
            segments
        );

        AddArc(
            points,
            new Vector2(
                rect.xMax - radius,
                rect.yMin + radius
            ),
            radius,
            270f,
            360f,
            segments
        );

        return points.ToArray();
    }

    private void AddArc(
        List<Vector2> points,
        Vector2 center,
        float radius,
        float start,
        float end,
        int segments)
    {
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(start, end, t) * Mathf.Deg2Rad;

            points.Add(
                center +
                new Vector2(
                    Mathf.Cos(angle),
                    Mathf.Sin(angle)
                ) *
                radius
            );
        }
    }
}
