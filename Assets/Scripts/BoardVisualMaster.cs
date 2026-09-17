using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Master visual builder for the existing 40 BoardSpace objects.
/// Gameplay data stays inside BoardSpace / BoardGenerator.
///
/// This class deliberately separates geometry from visuals:
/// - BoardGeometryMaster owns slot position/size.
/// - BoardVisualMaster owns appearance/content.
///
/// BuildVisuals never rewrites user-edited visual profiles and never changes
/// BoardSpace gameplay values. BuildWholeBoard optionally applies geometry first.
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
    [SerializeField] private bool livePreview = false;
    [SerializeField] private List<SpaceVisual> spaces = new List<SpaceVisual>(40);

    [Header("OPTIONAL ONE-CLICK GEOMETRY")]
    [SerializeField] private BoardGeometryMaster geometryMaster;
    [SerializeField] private bool applyGeometryWhenBuilding = true;

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

    public BoardGeometryMaster GeometryMaster
    {
        get => geometryMaster;
        set => geometryMaster = value;
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
                $"BoardVisualMaster: Expected 40 BoardSpace objects, found {boardSpaces.Length}."
            );
            return;
        }

        List<SpaceVisual> old =
            spaces ?? new List<SpaceVisual>();

        var rebuilt = new List<SpaceVisual>(40);

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            int number = GetSpaceNumber(boardSpace);
            SpaceVisual profile = FindProfileIn(old, number);

            // New profile = create exact board defaults.
            // Existing profile = preserve ALL visual edits.
            if (profile == null)
            {
                profile = CreateDefaultProfile(
                    boardSpace,
                    number
                );
            }
            else
            {
                RefreshIdentityOnly(
                    profile,
                    boardSpace,
                    number
                );
            }

            rebuilt.Add(profile);
        }

        spaces = rebuilt;

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    [ContextMenu("BUILD VISUALS")]
    public void BuildVisuals()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError(
                $"BoardVisualMaster: Expected 40 BoardSpace objects, found {boardSpaces.Length}."
            );
            return;
        }

        EnsureProfilesExist();
        ClearGenerated();

#if UNITY_EDITOR
        foreach (BoardSpace boardSpace in boardSpaces)
        {
            if (boardSpace != null)
                Undo.RecordObject(
                    boardSpace.transform,
                    "Build Board Visuals"
                );
        }
#endif

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            if (boardSpace == null)
                continue;

            SpaceVisual profile = FindProfile(
                GetSpaceNumber(boardSpace)
            );

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
            "BoardVisualMaster: visuals built without modifying gameplay data or geometry."
        );
    }

    /// <summary>
    /// Optional one-click workflow. Geometry is delegated to BoardGeometryMaster;
    /// visual data remains controlled by this component.
    /// </summary>
    [ContextMenu("BUILD WHOLE BOARD")]
    public void BuildWholeBoard()
    {
        if (applyGeometryWhenBuilding && geometryMaster != null)
            geometryMaster.ApplyLayout();

        BuildVisuals();
    }

    [ContextMenu("APPLY GEOMETRY ONLY")]
    public void ApplyGeometryOnly()
    {
        if (geometryMaster == null)
        {
            Debug.LogWarning(
                "BoardVisualMaster: Assign a BoardGeometryMaster first."
            );
            return;
        }

        geometryMaster.ApplyLayout();
    }

    [ContextMenu("CLEAR VISUALS")]
    public void ClearVisuals()
    {
        ClearGenerated();

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    // Compatibility methods for older local BoardVisualMasterEditor files.
    public void RefreshLivePreview()
    {
        BuildWholeBoard();
    }

    public void SyncProfilesFromBoard()
    {
        SyncProfiles();
    }

    public void BuildMasterVisuals()
    {
        BuildWholeBoard();
    }

    public void ClearMasterVisuals()
    {
        ClearVisuals();
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

        RectTransform root =
            rootObject.GetComponent<RectTransform>();

        root.anchorMin = Vector2.zero;
        root.anchorMax = Vector2.one;
        root.pivot = new Vector2(0.5f, 0.5f);
        root.offsetMin = Vector2.zero;
        root.offsetMax = Vector2.zero;
        root.anchoredPosition = profile.visualOffset;
        root.localScale = Vector3.one * Mathf.Clamp(
            profile.visualScale,
            0.85f,
            1f
        );
        root.localRotation = Quaternion.Euler(
            0f,
            0f,
            profile.manualOrientation
                ? profile.rotation
                : 0f
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
            Shadow shadow =
                rootObject.AddComponent<Shadow>();

            shadow.effectColor =
                new Color(0f, 0f, 0f, 0.13f);

            shadow.effectDistance =
                new Vector2(0f, -2f);
        }

        switch (profile.kind)
        {
            case VisualKind.Property:
                BuildProperty(
                    root,
                    profile,
                    GetSpaceNumber(boardSpace)
                );
                break;

            case VisualKind.Airport:
            case VisualKind.Utility:
                BuildTransport(
                    root,
                    profile
                );
                break;

            case VisualKind.Special:
                BuildSpecial(
                    root,
                    profile
                );
                break;

            case VisualKind.Corner:
                BuildCorner(
                    root,
                    profile
                );
                break;
        }
    }

    private void BuildProperty(
        RectTransform parent,
        SpaceVisual profile,
        int number)
    {
        Vector2 barMin;
        Vector2 barMax;

        GetInwardBar(
            number,
            out barMin,
            out barMax
        );

        if (profile.showColorBar)
        {
            CreateRoundedPanel(
                parent,
                "GroupColorBar",
                barMin,
                barMax,
                profile.groupColor,
                Mathf.Min(
                    profile.cornerRadius * 0.55f,
                    10f
                )
            );
        }

        CreateText(
            parent,
            "PropertyName",
            profile.displayName,
            profile.nameSize,
            FontStyles.Bold,
            new Vector2(0.06f, 0.44f),
            new Vector2(0.94f, 0.77f),
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
                new Vector2(0.06f, 0.05f),
                new Vector2(0.94f, 0.20f),
                profile.textColor
            );
        }

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "Subtitle",
                profile.subtitle,
                Mathf.Max(
                    8f,
                    profile.priceSize * 0.9f
                ),
                FontStyles.Normal,
                new Vector2(0.06f, 0.20f),
                new Vector2(0.94f, 0.32f),
                new Color(
                    0.36f,
                    0.38f,
                    0.40f,
                    1f
                )
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
            new Vector2(0.05f, 0.31f),
            new Vector2(0.95f, 0.71f),
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
                new Vector2(0.06f, 0.05f),
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
            new Vector2(0.05f, 0.34f),
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
                new Color(
                    0.36f,
                    0.38f,
                    0.40f,
                    1f
                )
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
                Mathf.Max(
                    44f,
                    profile.iconSize
                )
            );
        }

        CreateText(
            parent,
            "CornerName",
            profile.displayName,
            Mathf.Max(
                18f,
                profile.nameSize
            ),
            FontStyles.Bold,
            new Vector2(0.05f, 0.26f),
            new Vector2(0.95f, 0.58f),
            profile.textColor
        );

        if (!string.IsNullOrWhiteSpace(profile.subtitle))
        {
            CreateText(
                parent,
                "CornerSubtitle",
                profile.subtitle,
                Mathf.Max(
                    10f,
                    profile.priceSize
                ),
                FontStyles.Normal,
                new Vector2(0.05f, 0.08f),
                new Vector2(0.95f, 0.22f),
                new Color(
                    0.36f,
                    0.38f,
                    0.40f,
                    1f
                )
            );
        }
    }

    private void GetInwardBar(
        int number,
        out Vector2 min,
        out Vector2 max)
    {
        const float thickness = 0.085f;

        // Bottom side: bar toward center.
        if (number >= 1 && number <= 9)
        {
            min = new Vector2(
                0.07f,
                1f - thickness
            );

            max = new Vector2(
                0.93f,
                1f
            );

            return;
        }

        // Right side: bar toward center.
        if (number >= 11 && number <= 19)
        {
            min = new Vector2(
                0f,
                0.07f
            );

            max = new Vector2(
                thickness,
                0.93f
            );

            return;
        }

        // Top side: bar toward center.
        if (number >= 21 && number <= 29)
        {
            min = new Vector2(
                0.07f,
                0f
            );

            max = new Vector2(
                0.93f,
                thickness
            );

            return;
        }

        // Left side: bar toward center.
        min = new Vector2(
            1f - thickness,
            0.07f
        );

        max = new Vector2(
            1f,
            0.93f
        );
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

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.54f);

        rect.anchorMax =
            new Vector2(0.5f, 0.54f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(size, size);

        Image image =
            obj.GetComponent<Image>();

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

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

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

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        text.text =
            value ?? string.Empty;

        text.fontSize = size;
        text.fontStyle = style;
        text.alignment =
            TextAlignmentOptions.Center;

        text.color = color;
        text.textWrappingMode =
            TextWrappingModes.Normal;

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget = false;
    }

    private SpaceVisual CreateDefaultProfile(
        BoardSpace boardSpace,
        int number)
    {
        SpaceVisual profile =
            new SpaceVisual();

        profile.spaceNumber = number;
        profile.sceneObjectName =
            boardSpace.gameObject.name;
        profile.displayName =
            GetDisplayName(number);
        profile.kind =
            DetectKindByNumber(
                number,
                boardSpace
            );

        profile.background =
            new Color(
                0.965f,
                0.945f,
                0.890f,
                1f
            );

        profile.textColor =
            new Color(
                0.08f,
                0.10f,
                0.13f,
                1f
            );

        profile.cornerRadius =
            profile.kind == VisualKind.Corner
                ? 28f
                : 14f;

        profile.borderWidth = 1.25f;
        profile.borderColor =
            new Color(
                0.12f,
                0.16f,
                0.20f,
                0.23f
            );

        profile.shadow = true;
        profile.visualScale = 0.965f;
        profile.visualOffset = Vector2.zero;
        profile.manualOrientation = false;
        profile.rotation = 0f;
        profile.showIcon = false;
        profile.icon = null;
        profile.iconSize = 38f;
        profile.priceMillions =
            GetExactPrice(number);
        profile.showPrice =
            IsPropertyOrTransport(number);

        profile.nameSize =
            profile.kind == VisualKind.Corner
                ? 20f
                : profile.kind == VisualKind.Airport ||
                  profile.kind == VisualKind.Utility
                    ? 13f
                    : 16f;

        profile.priceSize = 14f;
        profile.subtitle = GetSubtitle(number);
        profile.specialAccent =
            GetAccent(profile.kind);

        ApplyDefaultGroup(
            profile,
            number
        );

        return profile;
    }

    private void RefreshIdentityOnly(
        SpaceVisual profile,
        BoardSpace boardSpace,
        int number)
    {
        profile.spaceNumber = number;
        profile.sceneObjectName =
            boardSpace.gameObject.name;
        profile.kind =
            DetectKindByNumber(
                number,
                boardSpace
            );

        // IMPORTANT:
        // Do not overwrite displayName, groupColor, showIcon, icon,
        // priceMillions, styling, typography, etc. Existing values are
        // deliberate user edits.
        if (profile.kind != VisualKind.Property)
        {
            profile.showColorBar = false;
        }
    }

    private void ApplyDefaultGroup(
        SpaceVisual profile,
        int number)
    {
        string group;
        Color color;

        if (TryGetGroup(
                number,
                out group,
                out color
            ))
        {
            profile.showColorBar = true;
            profile.groupName = group;
            profile.groupColor = color;
        }
        else
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
            if (boardSpace.SpaceType ==
                BoardSpaceType.Airport)
            {
                return VisualKind.Airport;
            }

            if (boardSpace.SpaceType ==
                BoardSpaceType.Utility)
            {
                return VisualKind.Utility;
            }

            if (boardSpace.SpaceType ==
                    BoardSpaceType.Chance ||
                boardSpace.SpaceType ==
                    BoardSpaceType.CommunityChest)
            {
                return VisualKind.Special;
            }
        }

        return VisualKind.Property;
    }

    private bool IsPropertyOrTransport(
        int number)
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

    private string GetDisplayName(
        int number)
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

    private int GetExactPrice(
        int number)
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

    private string GetSubtitle(
        int number)
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

    private Color GetAccent(
        VisualKind kind)
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

    private BoardSpace[] GetBoardSpaces()
    {
        BoardSpace[] boardSpaces =
            GetComponentsInChildren<BoardSpace>(true);

        Array.Sort(
            boardSpaces,
            CompareSpaces
        );

        return boardSpaces;
    }

    private int CompareSpaces(
        BoardSpace a,
        BoardSpace b)
    {
        return GetSpaceNumber(a)
            .CompareTo(
                GetSpaceNumber(b)
            );
    }

    private int GetSpaceNumber(
        BoardSpace boardSpace)
    {
        if (boardSpace == null)
            return 999;

        string objectName =
            boardSpace.gameObject.name;

        if (!objectName.StartsWith(
                "Space_",
                StringComparison.OrdinalIgnoreCase))
        {
            return 999;
        }

        return int.TryParse(
            objectName.Substring(6),
            out int number
        )
            ? number
            : 999;
    }

    private SpaceVisual FindProfile(
        int number)
    {
        return FindProfileIn(
            spaces,
            number
        );
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

    private void EnsureProfilesExist()
    {
        if (spaces != null &&
            spaces.Count == 40)
        {
            return;
        }

        SyncProfiles();
    }

    private void ClearGenerated()
    {
        BoardSpace[] boardSpaces =
            GetBoardSpaces();

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            if (boardSpace == null)
                continue;

            Transform existing =
                boardSpace.transform.Find(
                    GeneratedRootName
                );

            if (existing == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(
                    existing.gameObject
                );
            else
#endif
                Destroy(
                    existing.gameObject
                );
        }
    }

    private static Color ParseHex(
        string hex)
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
        EditorUtility.SetDirty(this);

        UnityEditor.SceneManagement
            .EditorSceneManager
            .MarkSceneDirty(
                gameObject.scene
            );
    }
#endif
}

/// <summary>
/// Simple procedural rounded UI surface for generated board cards.
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
            cornerRadius = Mathf.Max(
                0f,
                value
            );

            SetVerticesDirty();
        }
    }

    public float BorderWidth
    {
        get => borderWidth;
        set
        {
            borderWidth = Mathf.Max(
                0f,
                value
            );

            SetVerticesDirty();
        }
    }

    protected override void OnPopulateMesh(
        VertexHelper vh)
    {
        vh.Clear();

        Rect rect =
            GetPixelAdjustedRect();

        float radius =
            Mathf.Clamp(
                cornerRadius,
                0f,
                Mathf.Min(
                    rect.width,
                    rect.height
                ) * 0.5f
            );

        float border =
            Mathf.Clamp(
                borderWidth,
                0f,
                Mathf.Min(
                    rect.width,
                    rect.height
                ) * 0.25f
            );

        int segments =
            Mathf.Clamp(
                cornerSegments,
                2,
                16
            );

        Vector2[] outer =
            BuildPoints(
                rect,
                radius,
                segments
            );

        Rect innerRect =
            new Rect(
                rect.xMin + border,
                rect.yMin + border,
                Mathf.Max(
                    0f,
                    rect.width - border * 2f
                ),
                Mathf.Max(
                    0f,
                    rect.height - border * 2f
                )
            );

        Vector2[] inner =
            BuildPoints(
                innerRect,
                Mathf.Max(
                    0f,
                    radius - border
                ),
                segments
            );

        int count = outer.Length;

        UIVertex center =
            UIVertex.simpleVert;

        center.position =
            rect.center;

        center.color =
            fillColor;

        vh.AddVert(center);

        for (int i = 0; i < count; i++)
        {
            UIVertex v =
                UIVertex.simpleVert;

            v.position = inner[i];
            v.color = fillColor;

            vh.AddVert(v);
        }

        // Fill only. The border is drawn with an Outline component by the
        // generated card, avoiding duplicate/inverted border triangles.
        for (int i = 0; i < count; i++)
        {
            int next =
                (i + 1) % count;

            vh.AddTriangle(
                0,
                i + 1,
                next + 1
            );
        }
    }

    private Vector2[] BuildPoints(
        Rect rect,
        float radius,
        int segments)
    {
        var points =
            new List<Vector2>(
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
            float t =
                i / (float)segments;

            float angle =
                Mathf.Lerp(
                    start,
                    end,
                    t
                ) *
                Mathf.Deg2Rad;

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
