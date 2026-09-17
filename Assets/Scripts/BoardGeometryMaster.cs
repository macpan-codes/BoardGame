using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Geometry-only controller for the existing 40 BoardSpace objects.
/// Board = 1000x1000. Four corners are slightly larger; every other
/// space has exactly the same width and depth. Airports, utilities,
/// properties and special spaces are geometrically identical.
/// Gameplay data is never changed.
/// </summary>
[ExecuteAlways]
public class BoardGeometryMaster : MonoBehaviour
{
    [Serializable]
    public class SpaceGeometry
    {
        [HideInInspector] public int spaceNumber;
        [HideInInspector] public string sceneObjectName;

        [Tooltip("Fine position adjustment only. Does not change slot size.")]
        public Vector2 positionOffset = Vector2.zero;
    }

    [Header("MASTER BOARD")]
    [SerializeField, Min(100f)] private float boardSize = 1000f;

    [Header("CORNERS")]
    [SerializeField, Min(60f)] private float cornerSize = 150f;

    [Header("ALL NORMAL SPACES")]
    [SerializeField, Min(40f)] private float normalDepth = 140f;

    [Header("40 INDIVIDUAL OFFSETS")]
    [SerializeField] private List<SpaceGeometry> spaces = new List<SpaceGeometry>(40);

    [Header("EDITOR")]
    [SerializeField] private bool livePreview = false;

    public List<SpaceGeometry> Spaces => spaces;

    private void OnValidate()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && livePreview)
        {
            EnsureProfiles();
            ApplyLayout();
        }
#endif
    }

    [ContextMenu("SYNC 40 SPACES")]
    public void SyncProfiles()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError($"BoardGeometryMaster: Expected exactly 40 BoardSpace objects, found {boardSpaces.Length}.");
            return;
        }

        List<SpaceGeometry> old = spaces ?? new List<SpaceGeometry>();
        var rebuilt = new List<SpaceGeometry>(40);

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            int number = GetSpaceNumber(boardSpace);
            SpaceGeometry profile = FindProfileIn(old, number);

            if (profile == null)
                profile = new SpaceGeometry();

            profile.spaceNumber = number;
            profile.sceneObjectName = boardSpace.gameObject.name;
            rebuilt.Add(profile);
        }

        spaces = rebuilt;

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    [ContextMenu("APPLY LAYOUT")]
    public void ApplyLayout()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError($"BoardGeometryMaster: Expected exactly 40 BoardSpace objects, found {boardSpaces.Length}.");
            return;
        }

        EnsureProfiles();

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObjects(GetRects(boardSpaces), "Apply Board Geometry");
#endif

        // This is the intended final proportion.
        // 1000 board - 150 corner - 150 corner = 700 usable.
        // 700 / 9 = 77.777... units for EVERY normal space.
        float usableLength = boardSize - cornerSize * 2f;
        if (usableLength <= 0f)
        {
            Debug.LogError("BoardGeometryMaster: Corner size is too large for the board.");
            return;
        }

        float normalWidth = usableLength / 9f;

        PlaceCorner(boardSpaces[0],  true,  true);
        PlaceCorner(boardSpaces[10], true,  false);
        PlaceCorner(boardSpaces[20], false, false);
        PlaceCorner(boardSpaces[30], false, true);

        // Bottom: 01 -> 09, left to right.
        for (int number = 1; number <= 9; number++)
        {
            float x =
                -boardSize * 0.5f +
                cornerSize +
                normalWidth * (number - 0.5f);

            SetRect(
                boardSpaces[number],
                new Vector2(
                    x,
                    -boardSize * 0.5f + normalDepth * 0.5f
                ) + GetOffset(number),
                new Vector2(
                    normalWidth,
                    normalDepth
                )
            );
        }

        // Right: 11 -> 19, bottom to top.
        for (int number = 11; number <= 19; number++)
        {
            float y =
                -boardSize * 0.5f +
                cornerSize +
                normalWidth * ((number - 10) - 0.5f);

            SetRect(
                boardSpaces[number],
                new Vector2(
                    boardSize * 0.5f - normalDepth * 0.5f,
                    y
                ) + GetOffset(number),
                new Vector2(
                    normalDepth,
                    normalWidth
                )
            );
        }

        // Top: 21 -> 29, right to left.
        for (int number = 21; number <= 29; number++)
        {
            float x =
                boardSize * 0.5f -
                cornerSize -
                normalWidth * ((number - 20) - 0.5f);

            SetRect(
                boardSpaces[number],
                new Vector2(
                    x,
                    boardSize * 0.5f - normalDepth * 0.5f
                ) + GetOffset(number),
                new Vector2(
                    normalWidth,
                    normalDepth
                )
            );
        }

        // Left: 31 -> 39, top to bottom.
        for (int number = 31; number <= 39; number++)
        {
            float y =
                boardSize * 0.5f -
                cornerSize -
                normalWidth * ((number - 30) - 0.5f);

            SetRect(
                boardSpaces[number],
                new Vector2(
                    -boardSize * 0.5f + normalDepth * 0.5f,
                    y
                ) + GetOffset(number),
                new Vector2(
                    normalDepth,
                    normalWidth
                )
            );
        }

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    [ContextMenu("RESTORE ORIGINAL SQUARE LAYOUT")]
    public void RestoreOriginalSquareLayout()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError($"BoardGeometryMaster: Expected exactly 40 BoardSpace objects, found {boardSpaces.Length}.");
            return;
        }

#if UNITY_EDITOR
        UnityEditor.Undo.RecordObjects(GetRects(boardSpaces), "Restore Board Geometry");
#endif

        const float cell = 81.818184f;
        float half = boardSize * 0.5f;

        for (int i = 0; i < 40; i++)
        {
            RectTransform rect = GetRect(boardSpaces[i]);
            if (rect == null) continue;

            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = GetOriginalPosition(i, half, cell);
            rect.sizeDelta = new Vector2(cell, cell);
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
        }

#if UNITY_EDITOR
        MarkDirty();
#endif
    }

    private void PlaceCorner(BoardSpace boardSpace, bool bottom, bool left)
    {
        if (boardSpace == null) return;

        float half = boardSize * 0.5f;

        Vector2 position = new Vector2(
            left ? -half + cornerSize * 0.5f : half - cornerSize * 0.5f,
            bottom ? -half + cornerSize * 0.5f : half - cornerSize * 0.5f
        );

        int number = GetSpaceNumber(boardSpace);
        position += GetOffset(number);

        SetRect(
            boardSpace,
            position,
            new Vector2(cornerSize, cornerSize)
        );
    }

    private Vector2 GetOffset(int number)
    {
        SpaceGeometry profile = FindProfile(number);
        return profile == null ? Vector2.zero : profile.positionOffset;
    }

    private void SetRect(BoardSpace boardSpace, Vector2 position, Vector2 size)
    {
        RectTransform rect = GetRect(boardSpace);
        if (rect == null) return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private void EnsureProfiles()
    {
        if (spaces == null || spaces.Count != 40)
            SyncProfiles();
    }

    private SpaceGeometry FindProfile(int number)
    {
        return FindProfileIn(spaces, number);
    }

    private SpaceGeometry FindProfileIn(List<SpaceGeometry> list, int number)
    {
        if (list == null) return null;

        foreach (SpaceGeometry profile in list)
        {
            if (profile != null && profile.spaceNumber == number)
                return profile;
        }

        return null;
    }

    private BoardSpace[] GetBoardSpaces()
    {
        BoardSpace[] boardSpaces = GetComponentsInChildren<BoardSpace>(true);
        Array.Sort(boardSpaces, CompareSpaces);
        return boardSpaces;
    }

    private int CompareSpaces(BoardSpace a, BoardSpace b)
    {
        return GetSpaceNumber(a).CompareTo(GetSpaceNumber(b));
    }

    private int GetSpaceNumber(BoardSpace boardSpace)
    {
        if (boardSpace == null) return 999;

        string name = boardSpace.gameObject.name;
        if (!name.StartsWith("Space_", StringComparison.OrdinalIgnoreCase))
            return 999;

        return int.TryParse(name.Substring(6), out int number)
            ? number
            : 999;
    }

    private RectTransform GetRect(BoardSpace boardSpace)
    {
        return boardSpace == null
            ? null
            : boardSpace.GetComponent<RectTransform>();
    }

    private Vector2 GetOriginalPosition(int index, float half, float cell)
    {
        if (index == 0) return new Vector2(-half, -half);
        if (index == 10) return new Vector2(half, -half);
        if (index == 20) return new Vector2(half, half);
        if (index == 30) return new Vector2(-half, half);

        if (index > 0 && index < 10)
            return new Vector2(-half + cell * index, -half);

        if (index > 10 && index < 20)
            return new Vector2(half, -half + cell * (index - 10));

        if (index > 20 && index < 30)
            return new Vector2(half - cell * (index - 20), half);

        if (index > 30 && index < 40)
            return new Vector2(-half, half - cell * (index - 30));

        return Vector2.zero;
    }

#if UNITY_EDITOR
    private UnityEngine.Object[] GetRects(BoardSpace[] boardSpaces)
    {
        var result = new UnityEngine.Object[boardSpaces.Length];

        for (int i = 0; i < boardSpaces.Length; i++)
            result[i] = GetRect(boardSpaces[i]);

        return result;
    }

    private void MarkDirty()
    {
        UnityEditor.EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
    }
#endif
}
