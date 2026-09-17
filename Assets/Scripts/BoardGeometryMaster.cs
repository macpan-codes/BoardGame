using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Single source of truth for the physical geometry of the 40 board spaces.
///
/// Board:
///     1000 x 1000
///
/// Corners:
///     185 x 185
///
/// Normal spaces:
///     70 x 132
///
/// The board contains:
///     4 corners
///     36 identical normal spaces
///
/// This script controls ONLY geometry.
/// It does not modify BoardSpace gameplay data.
/// </summary>
[ExecuteAlways]
public class BoardGeometryMaster : MonoBehaviour
{
    // ============================================================
    // FIXED BOARD GEOMETRY
    // ============================================================

    public const float BoardSize = 1000f;

    public const float CornerSize = 185f;

    public const float NormalWidth = 70f;

    public const float NormalDepth = 132f;

    public const int TotalSpaces = 40;


    // ============================================================
    // INSPECTOR
    // ============================================================

    [Header("BOARD")]

    [SerializeField]
    private bool livePreview = false;


    // ============================================================
    // UNITY
    // ============================================================

#if UNITY_EDITOR

    private void OnValidate()
    {
        if (Application.isPlaying)
            return;

        if (!livePreview)
            return;

        ApplyLayout();
    }

#endif


    // ============================================================
    // APPLY LAYOUT
    // ============================================================

    [ContextMenu("APPLY BOARD GEOMETRY")]
    public void ApplyLayout()
    {
        BoardSpace[] boardSpaces = GetBoardSpaces();

        if (!ValidateBoardSpaces(boardSpaces))
            return;

#if UNITY_EDITOR

        Undo.RecordObjects(
            GetRectTransforms(boardSpaces),
            "Apply Board Geometry"
        );

#endif

        for (int index = 0; index < TotalSpaces; index++)
        {
            BoardSpace boardSpace = boardSpaces[index];

            if (boardSpace == null)
                continue;

            RectTransform rect =
                boardSpace.GetComponent<RectTransform>();

            if (rect == null)
                continue;

            // ----------------------------------------------------
            // ALL SPACES USE THE SAME TRANSFORM ORIENTATION
            // ----------------------------------------------------

            rect.anchorMin =
                new Vector2(0.5f, 0.5f);

            rect.anchorMax =
                new Vector2(0.5f, 0.5f);

            rect.pivot =
                new Vector2(0.5f, 0.5f);

            rect.localRotation =
                Quaternion.identity;

            rect.localScale =
                Vector3.one;


            // ----------------------------------------------------
            // CORNERS
            // ----------------------------------------------------

            if (IsCorner(index))
            {
                SetCornerGeometry(
                    rect,
                    index
                );

                continue;
            }


            // ----------------------------------------------------
            // NORMAL SPACES
            // ----------------------------------------------------

            SetNormalGeometry(
                rect,
                index
            );
        }

#if UNITY_EDITOR
        MarkDirty();
#endif
    }


    // ============================================================
    // RESTORE / RESET
    // ============================================================

    [ContextMenu("RESET BOARD GEOMETRY")]
    public void ResetBoardGeometry()
    {
        ApplyLayout();
    }


    // ============================================================
    // CORNER DETECTION
    // ============================================================

    private bool IsCorner(int index)
    {
        return index == 0 ||
               index == 10 ||
               index == 20 ||
               index == 30;
    }


    // ============================================================
    // CORNER GEOMETRY
    // ============================================================

    private void SetCornerGeometry(
        RectTransform rect,
        int index)
    {
        float half =
            BoardSize * 0.5f;

        float halfCorner =
            CornerSize * 0.5f;

        Vector2 position;

        switch (index)
        {
            // Bottom-left
            case 0:

                position =
                    new Vector2(
                        -half + halfCorner,
                        -half + halfCorner
                    );

                break;


            // Bottom-right
            case 10:

                position =
                    new Vector2(
                        half - halfCorner,
                        -half + halfCorner
                    );

                break;


            // Top-right
            case 20:

                position =
                    new Vector2(
                        half - halfCorner,
                        half - halfCorner
                    );

                break;


            // Top-left
            case 30:

                position =
                    new Vector2(
                        -half + halfCorner,
                        half - halfCorner
                    );

                break;


            default:

                position =
                    Vector2.zero;

                break;
        }

        rect.anchoredPosition =
            position;

        rect.sizeDelta =
            new Vector2(
                CornerSize,
                CornerSize
            );
    }


    // ============================================================
    // NORMAL SPACE GEOMETRY
    // ============================================================

    private void SetNormalGeometry(
        RectTransform rect,
        int index)
    {
        float half =
            BoardSize * 0.5f;


        // --------------------------------------------------------
        // Bottom
        // Spaces 1 -> 9
        // --------------------------------------------------------

        if (index >= 1 && index <= 9)
        {
            float x =
                -half +
                CornerSize +
                NormalWidth *
                (index - 0.5f);

            rect.anchoredPosition =
                new Vector2(
                    x,
                    -half +
                    NormalDepth * 0.5f
                );

            rect.sizeDelta =
                new Vector2(
                    NormalWidth,
                    NormalDepth
                );

            return;
        }


        // --------------------------------------------------------
        // Right
        // Spaces 11 -> 19
        // --------------------------------------------------------

        if (index >= 11 && index <= 19)
        {
            float y =
                -half +
                CornerSize +
                NormalWidth *
                (index - 10.5f);

            rect.anchoredPosition =
                new Vector2(
                    half -
                    NormalDepth * 0.5f,
                    y
                );

            // Same physical dimensions,
            // simply exchanged for the vertical side.
            //
            // IMPORTANT:
            // The object itself is NOT rotated.

            rect.sizeDelta =
                new Vector2(
                    NormalDepth,
                    NormalWidth
                );

            return;
        }


        // --------------------------------------------------------
        // Top
        // Spaces 21 -> 29
        // --------------------------------------------------------

        if (index >= 21 && index <= 29)
        {
            float x =
                half -
                CornerSize -
                NormalWidth *
                (index - 20.5f);

            rect.anchoredPosition =
                new Vector2(
                    x,
                    half -
                    NormalDepth * 0.5f
                );

            rect.sizeDelta =
                new Vector2(
                    NormalWidth,
                    NormalDepth
                );

            return;
        }


        // --------------------------------------------------------
        // Left
        // Spaces 31 -> 39
        // --------------------------------------------------------

        if (index >= 31 && index <= 39)
        {
            float y =
                half -
                CornerSize -
                NormalWidth *
                (index - 30.5f);

            rect.anchoredPosition =
                new Vector2(
                    -half +
                    NormalDepth * 0.5f,
                    y
                );

            rect.sizeDelta =
                new Vector2(
                    NormalDepth,
                    NormalWidth
                );

            return;
        }
    }


    // ============================================================
    // BOARD DISCOVERY
    // ============================================================

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


    // ============================================================
    // VALIDATION
    // ============================================================

    private bool ValidateBoardSpaces(
        BoardSpace[] boardSpaces)
    {
        if (boardSpaces == null)
        {
            Debug.LogError(
                "BoardGeometryMaster: BoardSpace array is null."
            );

            return false;
        }

        if (boardSpaces.Length != TotalSpaces)
        {
            Debug.LogError(
                "BoardGeometryMaster: Expected " +
                TotalSpaces +
                " BoardSpace objects, found " +
                boardSpaces.Length +
                "."
            );

            return false;
        }

        for (int i = 0; i < TotalSpaces; i++)
        {
            int number =
                GetSpaceNumber(
                    boardSpaces[i]
                );

            if (number != i)
            {
                Debug.LogError(
                    "BoardGeometryMaster: Expected " +
                    "Space_" +
                    i +
                    " but found space number " +
                    number +
                    "."
                );

                return false;
            }

            RectTransform rect =
                boardSpaces[i]
                    .GetComponent<RectTransform>();

            if (rect == null)
            {
                Debug.LogError(
                    "BoardGeometryMaster: " +
                    boardSpaces[i].name +
                    " does not have a RectTransform."
                );

                return false;
            }
        }

        return true;
    }


    // ============================================================
    // EDITOR HELPERS
    // ============================================================

#if UNITY_EDITOR

    private UnityEngine.Object[] GetRectTransforms(
        BoardSpace[] boardSpaces)
    {
        UnityEngine.Object[] result =
            new UnityEngine.Object[
                boardSpaces.Length
            ];

        for (int i = 0; i < boardSpaces.Length; i++)
        {
            result[i] =
                boardSpaces[i] != null
                    ? boardSpaces[i]
                        .GetComponent<RectTransform>()
                    : null;
        }

        return result;
    }


    private void MarkDirty()
    {
        EditorUtility.SetDirty(this);

        EditorSceneManager.MarkSceneDirty(
            gameObject.scene
        );
    }


    [CustomEditor(typeof(BoardGeometryMaster))]
    private class BoardGeometryMasterEditor
        : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            BoardGeometryMaster master =
                (BoardGeometryMaster)target;

            EditorGUILayout.HelpBox(
                "Fixed board geometry:\n" +
                "Board: 1000 x 1000\n" +
                "Corners: 185 x 185\n" +
                "Normal spaces: 70 x 132\n" +
                "All slot transforms remain at 0° rotation.",
                MessageType.Info
            );

            EditorGUILayout.Space(6);

            master.livePreview =
                EditorGUILayout.Toggle(
                    "Live Preview",
                    master.livePreview
                );

            EditorGUILayout.Space(6);

            if (GUILayout.Button(
                    "APPLY BOARD GEOMETRY",
                    GUILayout.Height(32)))
            {
                master.ApplyLayout();
            }

            if (GUILayout.Button(
                    "RESET BOARD GEOMETRY"))
            {
                master.ResetBoardGeometry();
            }
        }
    }

#endif
}