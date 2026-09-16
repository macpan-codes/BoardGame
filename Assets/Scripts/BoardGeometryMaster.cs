using System;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class BoardGeometryMaster : MonoBehaviour
{
    public enum SpaceKind
    {
        Property,
        Airport,
        Utility,
        Special,
        Corner
    }

    [Serializable]
    public class SpaceGeometry
    {
        [HideInInspector] public int spaceNumber;
        [HideInInspector] public string sceneObjectName;

        public SpaceKind kind = SpaceKind.Property;

        [Min(0.25f)]
        [Tooltip("Relative width along the side. The whole side is fitted exactly.")]
        public float widthWeight = 1f;

        [Min(20f)]
        [Tooltip("How far the space extends inward toward the center.")]
        public float depth = 125f;

        public Vector2 positionOffset = Vector2.zero;

        [Min(60f)]
        public float cornerWidth = 180f;

        [Min(60f)]
        public float cornerHeight = 180f;
    }

    [Header("MASTER BOARD")]
    [SerializeField, Min(100f)] private float boardSize = 1000f;
    [SerializeField, Min(60f)] private float defaultCornerSize = 180f;

    [Header("DEFAULT DEPTHS")]
    [SerializeField, Min(20f)] private float propertyDepth = 125f;
    [SerializeField, Min(20f)] private float airportDepth = 132f;
    [SerializeField, Min(20f)] private float utilityDepth = 132f;
    [SerializeField, Min(20f)] private float specialDepth = 132f;

    [Header("40 INDIVIDUAL PROFILES")]
    [SerializeField] private List<SpaceGeometry> spaces =
        new List<SpaceGeometry>(40);

    [Header("LIVE PREVIEW")]
    [SerializeField] private bool livePreview = false;

    public List<SpaceGeometry> Spaces => spaces;

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (Application.isPlaying || !livePreview)
            return;

        if (spaces == null || spaces.Count != 40)
            SyncProfiles();

        ApplyLayout();
    }
#endif

    [ContextMenu("SYNC 40 SPACES")]
    public void SyncProfiles()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (boardSpaces.Length != 40)
        {
            Debug.LogError(
                $"BoardGeometryMaster: Expected 40 BoardSpace objects, found {boardSpaces.Length}."
            );
            return;
        }

        List<SpaceGeometry> old =
            spaces ?? new List<SpaceGeometry>();

        var rebuilt = new List<SpaceGeometry>(40);

        foreach (BoardSpace boardSpace in boardSpaces)
        {
            int number = GetSpaceNumber(boardSpace);
            SpaceGeometry profile = FindProfileIn(old, number);

            if (profile == null)
                profile = CreateDefaultProfile(boardSpace, number);

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
            Debug.LogError(
                $"BoardGeometryMaster: Expected 40 BoardSpace objects, found {boardSpaces.Length}."
            );
            return;
        }

        if (spaces == null || spaces.Count != 40)
            SyncProfiles();

#if UNITY_EDITOR
        Undo.RecordObjects(
            GetRects(boardSpaces),
            "Apply Board Geometry"
        );
#endif

        LayoutSide(boardSpaces, 0);
        LayoutSide(boardSpaces, 10);
        LayoutSide(boardSpaces, 20);
        LayoutSide(boardSpaces, 30);

        PlaceCorner(boardSpaces[0], FindProfile(0));
        PlaceCorner(boardSpaces[10], FindProfile(10));
        PlaceCorner(boardSpaces[20], FindProfile(20));
        PlaceCorner(boardSpaces[30], FindProfile(30));

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
            Debug.LogError(
                $"BoardGeometryMaster: Expected 40 BoardSpace objects, found {boardSpaces.Length}."
            );
            return;
        }

#if UNITY_EDITOR
        Undo.RecordObjects(
            GetRects(boardSpaces),
            "Restore Board Geometry"
        );
#endif

        const float cell = 81.818184f;
        float half = boardSize * 0.5f;

        for (int i = 0; i < 40; i++)
        {
            RectTransform rect = GetRect(boardSpaces[i]);
            if (rect == null)
                continue;

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

    private void LayoutSide(
        BoardSpace[] boardSpaces,
        int startingCorner)
    {
        int endingCorner = (startingCorner + 10) % 40;

        float firstCornerWidth = GetCornerWidth(startingCorner);
        float secondCornerWidth = GetCornerWidth(endingCorner);

        float usable =
            boardSize -
            firstCornerWidth -
            secondCornerWidth;

        if (usable <= 0f)
        {
            Debug.LogError(
                "BoardGeometryMaster: Corner sizes are too large for the board."
            );
            return;
        }

        float totalWeight = 0f;

        for (int offset = 1; offset <= 9; offset++)
        {
            int index = (startingCorner + offset) % 40;
            SpaceGeometry profile = FindProfile(index);

            totalWeight += profile == null
                ? 1f
                : Mathf.Max(0.25f, profile.widthWeight);
        }

        float current =
            -boardSize * 0.5f + firstCornerWidth;

        for (int offset = 1; offset <= 9; offset++)
        {
            int index = (startingCorner + offset) % 40;
            SpaceGeometry profile = FindProfile(index);

            if (profile == null)
                continue;

            float width =
                usable *
                Mathf.Max(0.25f, profile.widthWeight) /
                totalWeight;

            float center = current + width * 0.5f;
            Vector2 position = GetSidePosition(startingCorner, center);
            position += profile.positionOffset;

            SetRect(
                boardSpaces[index],
                position,
                new Vector2(width, Mathf.Max(20f, profile.depth))
            );

            current += width;
        }
    }

    private Vector2 GetSidePosition(
        int startingCorner,
        float center)
    {
        float half = boardSize * 0.5f;

        switch (startingCorner)
        {
            case 0:
                return new Vector2(
                    center,
                    -half + GetCornerHeight(0) * 0.5f
                );

            case 10:
                return new Vector2(
                    half - GetCornerWidth(10) * 0.5f,
                    center
                );

            case 20:
                return new Vector2(
                    -center,
                    half - GetCornerHeight(20) * 0.5f
                );

            case 30:
                return new Vector2(
                    -half + GetCornerWidth(30) * 0.5f,
                    -center
                );

            default:
                return Vector2.zero;
        }
    }

    private void PlaceCorner(
        BoardSpace boardSpace,
        SpaceGeometry profile)
    {
        if (boardSpace == null || profile == null)
            return;

        float half = boardSize * 0.5f;
        float width = Mathf.Max(60f, profile.cornerWidth);
        float height = Mathf.Max(60f, profile.cornerHeight);

        float x = half - width * 0.5f;
        float y = half - height * 0.5f;

        Vector2 position;

        switch (profile.spaceNumber)
        {
            case 0:
                position = new Vector2(-x, -y);
                break;
            case 10:
                position = new Vector2(x, -y);
                break;
            case 20:
                position = new Vector2(x, y);
                break;
            case 30:
                position = new Vector2(-x, y);
                break;
            default:
                position = Vector2.zero;
                break;
        }

        position += profile.positionOffset;

        SetRect(
            boardSpace,
            position,
            new Vector2(width, height)
        );
    }

    private SpaceGeometry CreateDefaultProfile(
        BoardSpace boardSpace,
        int number)
    {
        SpaceKind kind = DetectKind(boardSpace);

        return new SpaceGeometry
        {
            spaceNumber = number,
            sceneObjectName = boardSpace != null
                ? boardSpace.gameObject.name
                : string.Empty,
            kind = kind,
            widthWeight =
                kind == SpaceKind.Airport ||
                kind == SpaceKind.Utility
                    ? 1.15f
                    : kind == SpaceKind.Special
                        ? 1.08f
                        : 1f,
            depth = GetDefaultDepth(kind),
            positionOffset = Vector2.zero,
            cornerWidth = defaultCornerSize,
            cornerHeight = defaultCornerSize
        };
    }

    private float GetDefaultDepth(SpaceKind kind)
    {
        switch (kind)
        {
            case SpaceKind.Airport: return airportDepth;
            case SpaceKind.Utility: return utilityDepth;
            case SpaceKind.Special: return specialDepth;
            default: return propertyDepth;
        }
    }

    private SpaceKind DetectKind(BoardSpace boardSpace)
    {
        if (boardSpace == null)
            return SpaceKind.Property;

        int number = GetSpaceNumber(boardSpace);

        if (number == 0 || number == 10 || number == 20 || number == 30)
            return SpaceKind.Corner;

        if (number == 5 || number == 15 || number == 25 || number == 35)
            return SpaceKind.Airport;

        if (number == 2 || number == 12 || number == 28 || number == 36)
            return SpaceKind.Utility;

        if (number == 4 || number == 7 || number == 17 ||
            number == 22 || number == 33 || number == 38)
            return SpaceKind.Special;

        return SpaceKind.Property;
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
            .CompareTo(GetSpaceNumber(b));
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

    private SpaceGeometry FindProfile(int number)
    {
        return FindProfileIn(spaces, number);
    }

    private SpaceGeometry FindProfileIn(
        List<SpaceGeometry> list,
        int number)
    {
        if (list == null)
            return null;

        foreach (SpaceGeometry profile in list)
        {
            if (profile != null &&
                profile.spaceNumber == number)
            {
                return profile;
            }
        }

        return null;
    }

    private RectTransform GetRect(BoardSpace space)
    {
        return space == null
            ? null
            : space.GetComponent<RectTransform>();
    }

    private void SetRect(
        BoardSpace space,
        Vector2 position,
        Vector2 size)
    {
        RectTransform rect = GetRect(space);
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localRotation = Quaternion.identity;
        rect.localScale = Vector3.one;
    }

    private float GetCornerWidth(int number)
    {
        SpaceGeometry profile = FindProfile(number);
        return profile == null
            ? defaultCornerSize
            : Mathf.Max(60f, profile.cornerWidth);
    }

    private float GetCornerHeight(int number)
    {
        SpaceGeometry profile = FindProfile(number);
        return profile == null
            ? defaultCornerSize
            : Mathf.Max(60f, profile.cornerHeight);
    }

    private Vector2 GetOriginalPosition(
        int index,
        float half,
        float cell)
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
        EditorUtility.SetDirty(this);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            gameObject.scene
        );
    }

    [CustomEditor(typeof(BoardGeometryMaster))]
    private class BoardGeometryMasterEditor : UnityEditor.Editor
    {
        private Vector2 scroll;
        private readonly bool[] foldouts = new bool[40];

        public override void OnInspectorGUI()
        {
            BoardGeometryMaster master =
                (BoardGeometryMaster)target;

            serializedObject.Update();

            EditorGUILayout.HelpBox(
                "Geometry only. BoardSpace gameplay data is untouched.",
                MessageType.Info
            );

            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("SYNC 40 SPACES", GUILayout.Height(28)))
                master.SyncProfiles();

            if (GUILayout.Button("APPLY LAYOUT", GUILayout.Height(28)))
                master.ApplyLayout();

            if (GUILayout.Button("RESTORE GRID", GUILayout.Height(28)))
                master.RestoreOriginalSquareLayout();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(6);

            Draw("boardSize", "Board Size");
            Draw("defaultCornerSize", "Default Corner Size");
            Draw("livePreview", "Live Preview");

            EditorGUILayout.Space(4);
            EditorGUILayout.LabelField(
                "DEFAULT DEPTHS",
                EditorStyles.boldLabel
            );

            Draw("propertyDepth", "Property");
            Draw("airportDepth", "Airport");
            Draw("utilityDepth", "Utility");
            Draw("specialDepth", "Special");

            EditorGUILayout.Space(8);

            SerializedProperty list =
                serializedObject.FindProperty("spaces");

            if (list.arraySize != 40)
            {
                EditorGUILayout.HelpBox(
                    "Press SYNC 40 SPACES first.",
                    MessageType.Warning
                );

                serializedObject.ApplyModifiedProperties();
                return;
            }

            scroll = EditorGUILayout.BeginScrollView(
                scroll,
                GUILayout.MinHeight(500)
            );

            for (int i = 0; i < 40; i++)
            {
                SerializedProperty profile =
                    list.GetArrayElementAtIndex(i);

                int number =
                    profile.FindPropertyRelative(
                        "spaceNumber"
                    ).intValue;

                string name =
                    profile.FindPropertyRelative(
                        "sceneObjectName"
                    ).stringValue;

                foldouts[i] = EditorGUILayout.Foldout(
                    foldouts[i],
                    $"Space {number:00} — {name}",
                    true
                );

                if (!foldouts[i])
                    continue;

                EditorGUI.indentLevel++;

                Draw(profile, "kind", "Type");

                int kind =
                    profile.FindPropertyRelative(
                        "kind"
                    ).enumValueIndex;

                if (kind ==
                    (int)SpaceKind.Corner)
                {
                    Draw(profile, "cornerWidth", "Width");
                    Draw(profile, "cornerHeight", "Height");
                }
                else
                {
                    Draw(profile, "widthWeight", "Width Weight");
                    Draw(profile, "depth", "Depth");
                }

                Draw(profile, "positionOffset", "Position Offset");

                EditorGUI.indentLevel--;
                EditorGUILayout.Space(7);
            }

            EditorGUILayout.EndScrollView();

            bool changed =
                serializedObject.ApplyModifiedProperties();

            if (changed && master.livePreview)
                master.ApplyLayout();
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
