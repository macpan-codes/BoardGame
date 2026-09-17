using System;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif

/// <summary>
/// Creates a completely blank visual surface for all 40 board spaces.
///
/// BoardGeometryMaster:
///     Controls the position and size of each slot.
///
/// BoardGenerator:
///     Controls board indexes, clicking and ownership.
///
/// BoardVisualMaster:
///     Removes old generated slot visuals and creates a simple
///     blank background for every slot.
///
/// No text.
/// No prices.
/// No icons.
/// No property colors.
/// No generated artwork.
/// </summary>
[ExecuteAlways]
public class BoardVisualMaster : MonoBehaviour
{
    private const string BlankBackgroundName =
        "BlankSlotBackground";

    private const string OwnershipMarkerName =
        "OwnershipMarker";

    private const string OldCanvaName =
        "CanvaArtwork";

    private static readonly string[] OldGeneratedRoots =
    {
        "BoardVisualMasterGenerated",
        "BoardSpaceVisual"
    };

    [SerializeField]
    private Color blankSlotColor =
        new Color(
            0.93f,
            0.93f,
            0.93f,
            1f
        );

    [ContextMenu("RESTORE BLANK BOARD")]
    public void RestoreBlankBoard()
    {
        BoardSpace[] spaces =
            GetComponentsInChildren<BoardSpace>(true);

        if (spaces == null || spaces.Length != 40)
        {
            Debug.LogError(
                $"BoardVisualMaster: Expected 40 BoardSpace objects, found {spaces?.Length ?? 0}."
            );

            return;
        }

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            RemoveOldVisualRoots(space);
            RemoveOldVisualChildren(space);

            CreateBlankBackground(space);
        }

#if UNITY_EDITOR
        EditorUtility.SetDirty(this);

        if (gameObject.scene.IsValid())
        {
            EditorSceneManager.MarkSceneDirty(
                gameObject.scene
            );
        }
#endif

        Debug.Log(
            "BoardVisualMaster: Restored 40 blank board spaces."
        );
    }

    private void RemoveOldVisualRoots(
        BoardSpace space)
    {
        foreach (string rootName in OldGeneratedRoots)
        {
            Transform child =
                space.transform.Find(rootName);

            if (child == null)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(
                    child.gameObject
                );

                continue;
            }
#endif

            Destroy(child.gameObject);
        }
    }

    private void RemoveOldVisualChildren(
        BoardSpace space)
    {
        for (int i = space.transform.childCount - 1;
             i >= 0;
             i--)
        {
            Transform child =
                space.transform.GetChild(i);

            if (child == null)
                continue;

            // Ownership marker belongs to gameplay.
            if (child.name == OwnershipMarkerName)
                continue;

            // Future Canva artwork should not be destroyed
            // by this cleanup.
            if (child.name == OldCanvaName)
                continue;

            // Our own blank background is handled separately.
            if (child.name == BlankBackgroundName)
                continue;

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(
                    child.gameObject
                );

                continue;
            }
#endif

            Destroy(child.gameObject);
        }
    }

    private void CreateBlankBackground(
        BoardSpace space)
    {
        Image image =
            space.GetComponent<Image>();

        if (image == null)
        {
            image =
                space.gameObject.AddComponent<Image>();
        }

        image.sprite = null;
        image.type = Image.Type.Simple;
        image.color = blankSlotColor;

        // Important:
        // BoardSpaceClickHandler can still use this Image
        // as the clickable surface.
        image.raycastTarget = true;

        Transform oldBackground =
            space.transform.Find(
                BlankBackgroundName
            );

        if (oldBackground != null)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Undo.DestroyObjectImmediate(
                    oldBackground.gameObject
                );
            }
            else
#endif
            {
                Destroy(
                    oldBackground.gameObject
                );
            }
        }
    }

#if UNITY_EDITOR

    [CustomEditor(typeof(BoardVisualMaster))]
    private class BoardVisualMasterEditor
        : Editor
    {
        public override void OnInspectorGUI()
        {
            BoardVisualMaster master =
                (BoardVisualMaster)target;

            EditorGUILayout.HelpBox(
                "This keeps every board slot completely blank. " +
                "Only the geometry and gameplay systems remain.",
                MessageType.Info
            );

            EditorGUILayout.Space(8);

            SerializedProperty color =
                serializedObject.FindProperty(
                    "blankSlotColor"
                );

            EditorGUILayout.PropertyField(
                color,
                new GUIContent(
                    "Blank Slot Color"
                )
            );

            EditorGUILayout.Space(8);

            if (GUILayout.Button(
                    "RESTORE BLANK BOARD",
                    GUILayout.Height(36)))
            {
                master.RestoreBlankBoard();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }

#endif
}