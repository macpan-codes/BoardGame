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
            serializedObject.FindProperty("profiles");

        if (list.arraySize == 0)
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
                profile.FindPropertyRelative(
                    "spaceNumber"
                ).intValue;

            string sourceName =
                profile.FindPropertyRelative(
                    "sourceName"
                ).stringValue;

            string label =
                $"Space {number:00} — {sourceName}";

            foldouts[i] =
                EditorGUILayout.Foldout(
                    foldouts[i],
                    label,
                    true
                );

            if (!foldouts[i])
                continue;

            EditorGUI.indentLevel++;

            Draw(
                profile,
                "kind",
                "Type"
            );

            int kind =
                profile.FindPropertyRelative(
                    "kind"
                ).enumValueIndex;

            if (kind ==
                (int)BoardGeometryMaster.SpaceKind.Corner)
            {
                Draw(
                    profile,
                    "cornerWidth",
                    "Corner Width"
                );

                Draw(
                    profile,
                    "cornerHeight",
                    "Corner Height"
                );
            }
            else
            {
                Draw(
                    profile,
                    "alongWeight",
                    "Width Weight"
                );

                Draw(
                    profile,
                    "inwardDepth",
                    "Depth"
                );
            }

            EditorGUILayout.Space(2);

            Draw(
                profile,
                "positionOffset",
                "Position Offset"
            );

            Draw(
                profile,
                "manualRotation",
                "Rotation"
            );

            Draw(
                profile,
                "useManualPosition",
                "Manual Position"
            );

            if (profile.FindPropertyRelative(
                    "useManualPosition"
                ).boolValue)
            {
                Draw(
                    profile,
                    "manualPosition",
                    "Manual Position Value"
                );
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        EditorGUILayout.EndScrollView();
        serializedObject.ApplyModifiedProperties();
    }

    private void Draw(
        string propertyName,
        string label)
    {
        SerializedProperty p =
            serializedObject.FindProperty(
                propertyName
            );

        if (p != null)
            EditorGUILayout.PropertyField(
                p,
                new GUIContent(label),
                true
            );
    }

    private void Draw(
        SerializedProperty parent,
        string propertyName,
        string label)
    {
        SerializedProperty p =
            parent.FindPropertyRelative(
                propertyName
            );

        if (p != null)
            EditorGUILayout.PropertyField(
                p,
                new GUIContent(label),
                true
            );
    }
}
#endif
