using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Editor/development-only layout tool for the existing 40-space board.
/// It changes ONLY each board space RectTransform.
/// BoardSpace data, BoardIndex, movement, rent, ownership and gameplay are untouched.
///
/// Add to the existing Board object and use:
///     Apply Precise Board Layout
///     Restore Original Square Layout
/// </summary>
[ExecuteAlways]
public class BoardLayoutController : MonoBehaviour
{
    [Header("Exact Board Size")]
    [SerializeField] private float boardSize = 800f;

    [Header("Corners")]
    [SerializeField] private float cornerSize = 150f;

    [Header("Normal Properties")]
    [SerializeField] private float normalAlongSide = 1f;
    [SerializeField] private float normalDepth = 122f;

    [Header("Special Spaces")]
    [SerializeField] private float specialAlongSide = 1.12f;
    [SerializeField] private float specialDepth = 132f;

    [Header("Airports / Utilities")]
    [SerializeField] private float transportAlongSide = 1.18f;
    [SerializeField] private float transportDepth = 136f;

    [Header("Original Layout")]
    [SerializeField] private float originalCellSize = 81.818184f;

    [ContextMenu("Apply Precise Board Layout")]
    public void ApplyPreciseBoardLayout()
    {
        BoardSpace[] spaces = FindBoardSpaces();

        if (spaces.Length != 40)
        {
            Debug.LogError(
                $"BoardLayoutController: Expected 40 BoardSpace objects, found {spaces.Length}."
            );
            return;
        }

#if UNITY_EDITOR
        RecordUndo(spaces, "Apply Precise Board Layout");
#endif

        float half = boardSize * 0.5f;
        float usableSide = boardSize - (cornerSize * 2f);

        if (usableSide <= 0f)
        {
            Debug.LogError(
                "BoardLayoutController: Corner Size is too large for Board Size."
            );
            return;
        }

        // The board has exactly 9 non-corner spaces on every side.
        // We calculate each side independently and normalize its requested
        // widths so the 9 spaces ALWAYS fit exactly between the two corners.
        LayoutSide(spaces, 0, 10, 0f, half, usableSide);
        LayoutSide(spaces, 10, 20, -90f, half, usableSide);
        LayoutSide(spaces, 20, 30, 180f, half, usableSide);
        LayoutSide(spaces, 30, 0, 90f, half, usableSide);

#if UNITY_EDITOR
        MarkDirty();
#endif

        Debug.Log(
            "BoardLayoutController: Precise board layout applied. " +
            "All 40 spaces were fitted exactly between the four corners."
        );
    }

    [ContextMenu("Restore Original Square Layout")]
    public void RestoreOriginalSquareLayout()
    {
        BoardSpace[] spaces = FindBoardSpaces();

        if (spaces.Length != 40)
        {
            Debug.LogError(
                $"BoardLayoutController: Expected 40 BoardSpace objects, found {spaces.Length}."
            );
            return;
        }

#if UNITY_EDITOR
        RecordUndo(spaces, "Restore Original Square Layout");
#endif

        for (int index = 0; index < 40; index++)
        {
            RectTransform rect = GetRect(spaces[index]);
            if (rect == null)
                continue;

            rect.localRotation = Quaternion.identity;
            rect.sizeDelta =
                new Vector2(
                    originalCellSize,
                    originalCellSize
                );

            rect.anchoredPosition =
                GetOriginalPosition(index);
        }

#if UNITY_EDITOR
        MarkDirty();
#endif

        Debug.Log(
            "BoardLayoutController: Original square layout restored."
        );
    }

    private void LayoutSide(
        BoardSpace[] spaces,
        int cornerA,
        int cornerB,
        float rotation,
        float half,
        float usableSide)
    {
        float[] weights = new float[9];
        float totalWeight = 0f;

        for (int slot = 0; slot < 9; slot++)
        {
            int index = (cornerA + slot + 1) % 40;

            float weight = GetAlongWeight(spaces[index]);

            weights[slot] = weight;
            totalWeight += weight;
        }

        float current = -half + cornerSize;

        // Corner A
        SetSpace(
            spaces[cornerA],
            GetCornerPosition(cornerA, half),
            new Vector2(cornerSize, cornerSize),
            GetCornerRotation(cornerA)
        );

        for (int slot = 0; slot < 9; slot++)
        {
            int index = (cornerA + slot + 1) % 40;

            // Exact fit: every side consumes precisely usableSide.
            float along =
                usableSide *
                (weights[slot] / totalWeight);

            float centerAlong =
                current + (along * 0.5f);

            Vector2 position =
                GetSidePosition(
                    cornerA,
                    centerAlong,
                    half,
                    cornerSize
                );

            float depth =
                GetDepth(spaces[index]);

            SetSpace(
                spaces[index],
                position,
                new Vector2(along, depth),
                rotation
            );

            current += along;
        }

        // Corner B
        SetSpace(
            spaces[cornerB],
            GetCornerPosition(cornerB, half),
            new Vector2(cornerSize, cornerSize),
            GetCornerRotation(cornerB)
        );
    }

    private float GetAlongWeight(BoardSpace space)
    {
        if (space == null)
            return normalAlongSide;

        if (space.IsRailroad || space.IsUtility)
            return transportAlongSide;

        string name =
            space.SpaceName ?? string.Empty;

        if (Contains(name, "Chance") ||
            Contains(name, "Community Chest") ||
            Contains(name, "Tax"))
        {
            return specialAlongSide;
        }

        return normalAlongSide;
    }

    private float GetDepth(BoardSpace space)
    {
        if (space == null)
            return normalDepth;

        if (space.IsRailroad || space.IsUtility)
            return transportDepth;

        string name =
            space.SpaceName ?? string.Empty;

        if (Contains(name, "Chance") ||
            Contains(name, "Community Chest") ||
            Contains(name, "Tax"))
        {
            return specialDepth;
        }

        return normalDepth;
    }

    private bool Contains(string source, string value)
    {
        return source.IndexOf(
                   value,
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }

    private Vector2 GetSidePosition(
        int startCorner,
        float centerAlong,
        float half,
        float corner)
    {
        float edgeCenter =
            half - (corner * 0.5f);

        switch (startCorner)
        {
            // Bottom: left -> right
            case 0:
                return new Vector2(
                    centerAlong,
                    -edgeCenter
                );

            // Right: bottom -> top
            case 10:
                return new Vector2(
                    edgeCenter,
                    centerAlong
                );

            // Top: right -> left
            case 20:
                return new Vector2(
                    -centerAlong,
                    edgeCenter
                );

            // Left: top -> bottom
            case 30:
                return new Vector2(
                    -edgeCenter,
                    -centerAlong
                );

            default:
                return Vector2.zero;
        }
    }

    private Vector2 GetCornerPosition(
        int index,
        float half)
    {
        float offset =
            half - (cornerSize * 0.5f);

        switch (index)
        {
            case 0:
                return new Vector2(-offset, -offset);

            case 10:
                return new Vector2(offset, -offset);

            case 20:
                return new Vector2(offset, offset);

            case 30:
                return new Vector2(-offset, offset);

            default:
                return Vector2.zero;
        }
    }

    private float GetCornerRotation(int index)
    {
        // Keep corners upright during the geometry phase.
        // Special-space visual orientation will be handled later.
        return 0f;
    }

    private void SetSpace(
        BoardSpace space,
        Vector2 position,
        Vector2 size,
        float rotationZ)
    {
        if (space == null)
            return;

        RectTransform rect = GetRect(space);

        if (rect == null)
            return;

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            size;

        rect.localRotation =
            Quaternion.Euler(
                0f,
                0f,
                rotationZ
            );

        rect.localScale =
            Vector3.one;
    }

    private RectTransform GetRect(BoardSpace space)
    {
        return space != null
            ? space.GetComponent<RectTransform>()
            : null;
    }

    private BoardSpace[] FindBoardSpaces()
    {
        BoardSpace[] spaces =
            GetComponentsInChildren<BoardSpace>(
                true
            );

        Array.Sort(
            spaces,
            (a, b) =>
            {
                return ExtractIndex(a) -
                       ExtractIndex(b);
            }
        );

        return spaces;
    }

    private int ExtractIndex(BoardSpace space)
    {
        if (space == null)
            return 999;

        string name =
            space.gameObject.name;

        if (!name.StartsWith(
                "Space_",
                StringComparison.OrdinalIgnoreCase))
        {
            return 999;
        }

        return int.TryParse(
            name.Substring(6),
            out int index
        )
            ? index
            : 999;
    }

    private Vector2 GetOriginalPosition(int index)
    {
        const float half = 409.0909f;
        const float step = 81.818184f;

        if (index == 0)
            return new Vector2(-half, -half);

        if (index == 10)
            return new Vector2(half, -half);

        if (index == 20)
            return new Vector2(half, half);

        if (index == 30)
            return new Vector2(-half, half);

        if (index > 0 && index < 10)
        {
            return new Vector2(
                -half + step * index,
                -half
            );
        }

        if (index > 10 && index < 20)
        {
            return new Vector2(
                half,
                -half + step * (index - 10)
            );
        }

        if (index > 20 && index < 30)
        {
            return new Vector2(
                half - step * (index - 20),
                half
            );
        }

        if (index > 30 && index < 40)
        {
            return new Vector2(
                -half,
                half - step * (index - 30)
            );
        }

        return Vector2.zero;
    }

#if UNITY_EDITOR
    private void RecordUndo(
        BoardSpace[] spaces,
        string title)
    {
        var objects =
            new UnityEngine.Object[spaces.Length];

        for (int i = 0; i < spaces.Length; i++)
            objects[i] = GetRect(spaces[i]);

        Undo.RecordObjects(
            objects,
            title
        );

        Undo.RecordObject(
            gameObject,
            title
        );
    }

    private void MarkDirty()
    {
        EditorUtility.SetDirty(gameObject);

        RectTransform rect =
            GetComponent<RectTransform>();

        if (rect != null)
            EditorUtility.SetDirty(rect);

        EditorSceneManager.MarkSceneDirty(
            gameObject.scene
        );
    }
#endif
}
