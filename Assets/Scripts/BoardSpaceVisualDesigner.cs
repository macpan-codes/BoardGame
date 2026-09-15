using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Modern first-pass board-space visual designer.
/// 
/// This script ONLY creates/removes presentation children under the existing
/// BoardSpace objects. It does not modify BoardSpace gameplay data.
/// 
/// After importing RoundedBoardCard.png as a Sprite (and setting its Sprite
/// Editor border to about 36px on all sides), assign it to roundedCardSprite.
/// Then use the Board component menu:
///     Build Modern Board Space Visuals
///     Clear Board Space Visuals
/// </summary>
[ExecuteAlways]
public class BoardSpaceVisualDesigner : MonoBehaviour
{
    private const string RootName = "BoardSpaceVisual";

    [Header("Rounded Card Asset")]
    [SerializeField] private Sprite roundedCardSprite;

    [Header("Base Colors")]
    [SerializeField] private Color cardColor =
        new Color(0.95f, 0.94f, 0.90f, 1f);

    [SerializeField] private Color textColor =
        new Color(0.08f, 0.11f, 0.15f, 1f);

    [SerializeField] private Color mutedTextColor =
        new Color(0.34f, 0.37f, 0.40f, 1f);

    [SerializeField] private Color borderColor =
        new Color(0.20f, 0.23f, 0.27f, 0.55f);

    [Header("Typography")]
    [SerializeField] private float propertyNameSize = 18f;
    [SerializeField] private float priceSize = 16f;
    [SerializeField] private float specialTitleSize = 19f;
    [SerializeField] private float specialSubSize = 12f;

    [Header("Layout")]
    [SerializeField] private float cardInset = 3f;
    [SerializeField] private float barThickness = 13f;
    [SerializeField] private float iconAreaSize = 36f;

    [ContextMenu("Build Modern Board Space Visuals")]
    public void BuildModernBoardSpaceVisuals()
    {
        BoardSpace[] spaces =
            GetComponentsInChildren<BoardSpace>(true);

        if (spaces == null || spaces.Length != 40)
        {
            Debug.LogError(
                $"BoardSpaceVisualDesigner: Expected 40 BoardSpace objects, found {(spaces == null ? 0 : spaces.Length)}."
            );
            return;
        }

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            BuildSpace(space);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            gameObject.scene
        );
#endif

        Debug.Log(
            "BoardSpaceVisualDesigner: Modern rounded visual design applied to all 40 spaces."
        );
    }

    [ContextMenu("Clear Board Space Visuals")]
    public void ClearBoardSpaceVisuals()
    {
        BoardSpace[] spaces =
            GetComponentsInChildren<BoardSpace>(true);

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            Transform oldRoot =
                space.transform.Find(RootName);

            if (oldRoot == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(oldRoot.gameObject);
            else
#endif
                Destroy(oldRoot.gameObject);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameObject);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            gameObject.scene
        );
#endif
    }

    private void BuildSpace(BoardSpace space)
    {
        Transform existing =
            space.transform.Find(RootName);

        if (existing != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(existing.gameObject);
            else
#endif
                Destroy(existing.gameObject);
        }

        GameObject root =
            new GameObject(
                RootName,
                typeof(RectTransform)
            );

        root.transform.SetParent(
            space.transform,
            false
        );

        RectTransform rootRect =
            root.GetComponent<RectTransform>();

        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.offsetMin =
            new Vector2(cardInset, cardInset);
        rootRect.offsetMax =
            new Vector2(-cardInset, -cardInset);

        Image background =
            root.AddComponent<Image>();

        background.sprite =
            roundedCardSprite;

        background.type =
            roundedCardSprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;

        background.color =
            cardColor;

        background.raycastTarget = false;

        AddShadow(background);

        int boardIndex =
            ExtractBoardIndex(space);

        if (IsCorner(space))
        {
            BuildCorner(
                root.transform,
                space
            );
        }
        else if (IsTransport(space.SpaceType))
        {
            BuildTransport(
                root.transform,
                space
            );
        }
        else if (IsSpecial(space))
        {
            BuildSpecial(
                root.transform,
                space
            );
        }
        else
        {
            BuildProperty(
                root.transform,
                space,
                boardIndex
            );
        }
    }

    private void BuildProperty(
        Transform parent,
        BoardSpace space,
        int boardIndex)
    {
        // The bar always points toward the CENTER of the board:
        // bottom + top = local top
        // right + left = local bottom
        Vector2 barMin;
        Vector2 barMax;

        bool sideIsLeftOrRight =
            (boardIndex >= 10 && boardIndex <= 19) ||
            (boardIndex >= 30 && boardIndex <= 39);

        if (sideIsLeftOrRight)
        {
            barMin = new Vector2(0.06f, 0f);
            barMax = new Vector2(0.94f, 0f + barThickness / 100f);
        }
        else
        {
            barMin = new Vector2(0.06f, 1f - barThickness / 100f);
            barMax = new Vector2(0.94f, 1f);
        }

        Color groupColor =
            GetGroupColor(space);

        CreateSlicedPanel(
            parent,
            "GroupColorBar",
            barMin,
            barMax,
            groupColor
        );

        CreateText(
            parent,
            "PropertyName",
            FitName(space.SpaceName),
            propertyNameSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.06f, 0.56f),
            new Vector2(0.94f, 0.81f),
            textColor
        );

        // Clean, intentionally restrained placeholder until actual city art is added.
        GameObject icon =
            CreateSlicedPanel(
                parent,
                "PropertyIcon",
                new Vector2(0.25f, 0.23f),
                new Vector2(0.75f, 0.52f),
                new Color(0.90f, 0.89f, 0.85f, 1f)
            );

        CreateText(
            icon.transform,
            "IconMark",
            GetInitials(space.SpaceName),
            15f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            Vector2.zero,
            Vector2.one,
            mutedTextColor
        );

        CreateText(
            parent,
            "PurchasePrice",
            $"${space.PurchasePrice:N0}M",
            priceSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.05f),
            new Vector2(0.92f, 0.20f),
            textColor
        );

        AddThinBorder(parent);
    }

    private void BuildTransport(
        Transform parent,
        BoardSpace space)
    {
        Color accent =
            space.IsRailroad
                ? new Color(0.13f, 0.25f, 0.34f, 1f)
                : new Color(0.06f, 0.44f, 0.50f, 1f);

        CreateSlicedPanel(
            parent,
            "AccentBar",
            new Vector2(0.06f, 0.87f),
            new Vector2(0.94f, 0.98f),
            accent
        );

        CreateText(
            parent,
            "TransportIcon",
            space.IsRailroad ? "✈" : "⚡",
            28f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.48f),
            new Vector2(0.90f, 0.80f),
            accent
        );

        CreateText(
            parent,
            "TransportName",
            FitName(space.SpaceName),
            14f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.22f),
            new Vector2(0.95f, 0.46f),
            textColor
        );

        CreateText(
            parent,
            "TransportPrice",
            $"${space.PurchasePrice:N0}M",
            priceSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.05f),
            new Vector2(0.92f, 0.19f),
            textColor
        );

        AddThinBorder(parent);
    }

    private void BuildSpecial(
        Transform parent,
        BoardSpace space)
    {
        Color accent =
            GetSpecialAccent(space);

        CreateSlicedPanel(
            parent,
            "SpecialAccent",
            new Vector2(0.08f, 0.87f),
            new Vector2(0.92f, 0.98f),
            accent
        );

        CreateText(
            parent,
            "SpecialIcon",
            GetSpecialIcon(space),
            28f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.10f, 0.45f),
            new Vector2(0.90f, 0.82f),
            accent
        );

        CreateText(
            parent,
            "SpecialTitle",
            FitName(space.SpaceName),
            specialTitleSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.05f, 0.20f),
            new Vector2(0.95f, 0.42f),
            textColor
        );

        CreateText(
            parent,
            "SpecialSubtitle",
            GetSpecialSubtitle(space),
            specialSubSize,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.05f),
            new Vector2(0.92f, 0.17f),
            mutedTextColor
        );

        AddThinBorder(parent);
    }

    private void BuildCorner(
        Transform parent,
        BoardSpace space)
    {
        string name =
            FitName(space.SpaceName);

        CreateText(
            parent,
            "CornerIcon",
            GetCornerIcon(name),
            38f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.08f, 0.43f),
            new Vector2(0.92f, 0.84f),
            new Color(0.10f, 0.14f, 0.18f, 1f)
        );

        CreateText(
            parent,
            "CornerTitle",
            name,
            20f,
            FontStyles.Bold,
            TextAlignmentOptions.Center,
            new Vector2(0.07f, 0.24f),
            new Vector2(0.93f, 0.43f),
            textColor
        );

        CreateText(
            parent,
            "CornerSubtitle",
            GetCornerSubtitle(name),
            12f,
            FontStyles.Normal,
            TextAlignmentOptions.Center,
            new Vector2(0.07f, 0.08f),
            new Vector2(0.93f, 0.21f),
            mutedTextColor
        );

        AddThinBorder(parent);
    }

    private GameObject CreateSlicedPanel(
        Transform parent,
        string objectName,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform),
                typeof(Image)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image =
            obj.GetComponent<Image>();

        image.sprite =
            roundedCardSprite;

        image.type =
            roundedCardSprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;

        image.color = color;
        image.raycastTarget = false;

        return obj;
    }

    private TMP_Text CreateText(
        Transform parent,
        string objectName,
        string value,
        float size,
        FontStyles style,
        TextAlignmentOptions alignment,
        Vector2 anchorMin,
        Vector2 anchorMax,
        Color color)
    {
        GameObject obj =
            new GameObject(
                objectName,
                typeof(RectTransform)
            );

        obj.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            obj.GetComponent<RectTransform>();

        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI text =
            obj.AddComponent<TextMeshProUGUI>();

        text.text = value ?? string.Empty;
        text.fontSize = size;
        text.fontStyle = style;
        text.alignment = alignment;
        text.color = color;

#if UNITY_2023_1_OR_NEWER
        text.textWrappingMode = TextWrappingModes.Normal;
#else
        text.enableWordWrapping = true;
#endif

        text.overflowMode =
            TextOverflowModes.Ellipsis;

        text.raycastTarget = false;

        return text;
    }

    private void AddThinBorder(
        Transform parent)
    {
        GameObject border =
            new GameObject(
                "Border",
                typeof(RectTransform),
                typeof(Image)
            );

        border.transform.SetParent(
            parent,
            false
        );

        RectTransform rect =
            border.GetComponent<RectTransform>();

        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image =
            border.GetComponent<Image>();

        image.sprite =
            roundedCardSprite;

        image.type =
            roundedCardSprite != null
                ? Image.Type.Sliced
                : Image.Type.Simple;

        image.color =
            new Color(
                borderColor.r,
                borderColor.g,
                borderColor.b,
                0.18f
            );

        image.raycastTarget = false;

        // Border must sit behind the text/content.
        border.transform.SetAsFirstSibling();
    }

    private void AddShadow(Image image)
    {
        Shadow shadow =
            image.gameObject.AddComponent<Shadow>();

        shadow.effectColor =
            new Color(0f, 0f, 0f, 0.14f);

        shadow.effectDistance =
            new Vector2(0f, -2f);

        shadow.useGraphicAlpha = true;
    }

    private Color GetGroupColor(
        BoardSpace space)
    {
        if (space != null &&
            space.PropertyGroup != null)
        {
            return space.PropertyGroup.GroupColor;
        }

        return new Color(
            0.34f,
            0.38f,
            0.42f,
            1f
        );
    }

    private bool IsTransport(
        BoardSpaceType type)
    {
        return type == BoardSpaceType.Airport ||
               type == BoardSpaceType.Utility;
    }

    private bool IsSpecial(
        BoardSpace space)
    {
        if (space == null)
            return false;

        BoardSpaceType type =
            space.SpaceType;

        if (type == BoardSpaceType.Chance ||
            type == BoardSpaceType.CommunityChest)
        {
            return true;
        }

        string name =
            space.SpaceName ?? string.Empty;

        return name.IndexOf(
                   "Tax",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0 ||
               name.IndexOf(
                   "Super Tax",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }

    private bool IsCorner(
        BoardSpace space)
    {
        if (space == null)
            return false;

        if (space.SpaceType == BoardSpaceType.Jail ||
            space.SpaceType == BoardSpaceType.GoToJail)
        {
            return true;
        }

        string name =
            space.SpaceName ?? string.Empty;

        return
            string.Equals(
                name,
                "GO",
                StringComparison.OrdinalIgnoreCase
            ) ||
            name.IndexOf(
                "Free Parking",
                StringComparison.OrdinalIgnoreCase
            ) >= 0 ||
            name.IndexOf(
                "Just Visiting",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
    }

    private Color GetSpecialAccent(
        BoardSpace space)
    {
        if (space.SpaceType == BoardSpaceType.Chance)
        {
            return new Color(
                0.64f,
                0.30f,
                0.72f,
                1f
            );
        }

        if (space.SpaceType ==
            BoardSpaceType.CommunityChest)
        {
            return new Color(
                0.10f,
                0.50f,
                0.73f,
                1f
            );
        }

        return new Color(
            0.78f,
            0.36f,
            0.27f,
            1f
        );
    }

    private string GetSpecialIcon(
        BoardSpace space)
    {
        if (space.SpaceType == BoardSpaceType.Chance)
            return "?";

        if (space.SpaceType ==
            BoardSpaceType.CommunityChest)
            return "CC";

        return "$";
    }

    private string GetSpecialSubtitle(
        BoardSpace space)
    {
        if (space.SpaceType == BoardSpaceType.Chance ||
            space.SpaceType == BoardSpaceType.CommunityChest)
        {
            return "DRAW A CARD";
        }

        return "PAY THE BANK";
    }

    private string GetCornerIcon(
        string name)
    {
        if (string.Equals(
                name,
                "GO",
                StringComparison.OrdinalIgnoreCase))
        {
            return "GO";
        }

        if (name.IndexOf(
                "Free Parking",
                StringComparison.OrdinalIgnoreCase
            ) >= 0)
        {
            return "P";
        }

        if (name.IndexOf(
                "Jail",
                StringComparison.OrdinalIgnoreCase
            ) >= 0 ||
            name.IndexOf(
                "Visiting",
                StringComparison.OrdinalIgnoreCase
            ) >= 0)
        {
            return "▣";
        }

        return "•";
    }

    private string GetCornerSubtitle(
        string name)
    {
        if (string.Equals(
                name,
                "GO",
                StringComparison.OrdinalIgnoreCase))
        {
            return "COLLECT $200M";
        }

        if (name.IndexOf(
                "Free Parking",
                StringComparison.OrdinalIgnoreCase
            ) >= 0)
        {
            return "FREE PARKING";
        }

        if (name.IndexOf(
                "Go To Jail",
                StringComparison.OrdinalIgnoreCase
            ) >= 0)
        {
            return "GO TO JAIL";
        }

        if (name.IndexOf(
                "Visiting",
                StringComparison.OrdinalIgnoreCase
            ) >= 0)
        {
            return "JUST VISITING";
        }

        if (name.IndexOf(
                "Jail",
                StringComparison.OrdinalIgnoreCase
            ) >= 0)
        {
            return "JAIL";
        }

        return string.Empty;
    }

    private int ExtractBoardIndex(
        BoardSpace space)
    {
        if (space == null)
            return -1;

        string objectName =
            space.gameObject.name;

        if (!objectName.StartsWith(
                "Space_",
                StringComparison.OrdinalIgnoreCase))
        {
            return -1;
        }

        return int.TryParse(
            objectName.Substring(6),
            out int index
        )
            ? index
            : -1;
    }

    private string FitName(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return value.Trim();
    }

    private string GetInitials(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "?";

        string[] words =
            value.Trim().Split(
                new[] { ' ' },
                StringSplitOptions.RemoveEmptyEntries
            );

        if (words.Length == 1)
            return words[0][0]
                .ToString()
                .ToUpperInvariant();

        return (
            words[0][0].ToString() +
            words[words.Length - 1][0]
        ).ToUpperInvariant();
    }
}
