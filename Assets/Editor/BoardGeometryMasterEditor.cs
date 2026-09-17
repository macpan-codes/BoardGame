#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardGeometryMaster))]
public class BoardGeometryMasterEditor : Editor
{
    private Vector2 scroll;
    private readonly bool[] foldouts = new bool[40];

    public override void OnInspectorGUI()
    {
        BoardGeometryMaster master = (BoardGeometryMaster)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Geometry only. Gameplay BoardSpace data is untouched.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Sync 40 Spaces", GUILayout.Height(28)))
            master.SyncProfiles();

        if (GUILayout.Button("Apply Layout", GUILayout.Height(28)))
            master.ApplyLayout();

        if (GUILayout.Button("Restore Grid", GUILayout.Height(28)))
            master.RestoreOriginalSquareLayout();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        Draw("boardSize", "Board Size");
        Draw("defaultCornerSize", "Default Corner Size");

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Default Depths", EditorStyles.boldLabel);
        Draw("propertyDepth", "Property");
        Draw("airportDepth", "Airport");
        Draw("utilityDepth", "Utility");
        Draw("specialDepth", "Special");

        EditorGUILayout.Space(10);

        SerializedProperty list =
            serializedObject.FindProperty("spaces");

        if (list == null || list.arraySize != 40)
        {
            EditorGUILayout.HelpBox(
                "Press Sync 40 Spaces first.",
                MessageType.Warning
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(
            scroll,
            GUILayout.MinHeight(450)
        );

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty profile =
                list.GetArrayElementAtIndex(i);

            int number =
                profile.FindPropertyRelative("spaceNumber").intValue;

            string sourceName =
                profile.FindPropertyRelative("sceneObjectName").stringValue;

            string label =
                $"Space {number:00} — {sourceName}";

            foldouts[i] = EditorGUILayout.Foldout(
                foldouts[i],
                label,
                true
            );

            if (!foldouts[i])
                continue;

            EditorGUI.indentLevel++;

            Draw(profile, "kind", "Type");
            Draw(profile, "widthWeight", "Width Weight");
            Draw(profile, "depth", "Depth");
            Draw(profile, "positionOffset", "Position Offset");

            if ((BoardGeometryMaster.SpaceKind)
                profile.FindPropertyRelative("kind").enumValueIndex
                == BoardGeometryMaster.SpaceKind.Corner)
            {
                Draw(profile, "cornerWidth", "Corner Width");
                Draw(profile, "cornerHeight", "Corner Height");
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        EditorGUILayout.EndScrollView();
        serializedObject.ApplyModifiedProperties();
    }

    private void Draw(string propertyName, string label)
    {
        SerializedProperty property =
            serializedObject.FindProperty(propertyName);

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
            parent.FindPropertyRelative(propertyName);

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
