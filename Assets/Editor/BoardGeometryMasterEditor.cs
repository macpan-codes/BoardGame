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
        BoardGeometryMaster master =
            (BoardGeometryMaster)target;

        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "Final board geometry: 1000x1000, four slightly larger corners, all 36 non-corner spaces identical. Airports, utilities, properties and special spaces use the same geometry.",
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

        EditorGUILayout.Space(8);

        EditorGUILayout.LabelField(
            "BOARD GEOMETRY",
            EditorStyles.boldLabel
        );

        Draw("boardSize", "Board Size");
        Draw("cornerSize", "Corner Size");
        Draw("normalDepth", "Normal Space Depth");
        Draw("livePreview", "Live Preview");

        EditorGUILayout.Space(8);

        SerializedProperty list =
            serializedObject.FindProperty("spaces");

        if (list == null || list.arraySize != 40)
        {
            EditorGUILayout.HelpBox(
                "Press SYNC 40 SPACES first.",
                MessageType.Warning
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.LabelField(
            "INDIVIDUAL FINE OFFSETS",
            EditorStyles.boldLabel
        );

        EditorGUILayout.HelpBox(
            "These offsets are only for tiny visual corrections. They do not change the equal-size rule.",
            MessageType.None
        );

        scroll = EditorGUILayout.BeginScrollView(
            scroll,
            GUILayout.MinHeight(420)
        );

        for (int i = 0; i < 40; i++)
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

            Draw(
                profile,
                "positionOffset",
                "Position Offset"
            );

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();

        bool changed =
            serializedObject.ApplyModifiedProperties();

        if (changed && master.LivePreview)
            master.ApplyLayout();
    }

    private void Draw(
        string propertyName,
        string label)
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
