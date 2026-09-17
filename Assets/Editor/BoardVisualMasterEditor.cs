#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardVisualMaster))]
public class BoardVisualMasterEditor : Editor
{
    private Vector2 scroll;
    private readonly bool[] foldouts = new bool[40];

    public override void OnInspectorGUI()
    {
        BoardVisualMaster master = (BoardVisualMaster)target;
        serializedObject.Update();

        EditorGUILayout.HelpBox(
            "One-click board designer. Visuals are editable per slot; gameplay data is kept in BoardSpace.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("SYNC 40 PROFILES", GUILayout.Height(28)))
            master.SyncProfiles();

        if (GUILayout.Button("BUILD WHOLE BOARD", GUILayout.Height(28)))
            master.BuildWholeBoard();

        if (GUILayout.Button("CLEAR VISUALS", GUILayout.Height(28)))
            master.ClearVisuals();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        Draw("livePreview", "Live Preview");
        Draw("geometryMaster", "Geometry Master");
        Draw("applyGeometryWhenBuilding", "Apply Geometry On Build");

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("GROUP COLORS", EditorStyles.boldLabel);
        Draw("brown", "Brown");
        Draw("lightBlue", "Light Blue");
        Draw("pink", "Pink");
        Draw("orange", "Orange");
        Draw("red", "Red");
        Draw("yellow", "Yellow");
        Draw("green", "Green");
        Draw("blue", "Blue");

        EditorGUILayout.Space(8);

        SerializedProperty list = serializedObject.FindProperty("spaces");

        if (list == null || list.arraySize != 40)
        {
            EditorGUILayout.HelpBox(
                "Press SYNC 40 PROFILES first.",
                MessageType.Warning
            );

            serializedObject.ApplyModifiedProperties();
            return;
        }

        scroll = EditorGUILayout.BeginScrollView(
            scroll,
            GUILayout.MinHeight(520)
        );

        for (int i = 0; i < list.arraySize; i++)
        {
            SerializedProperty profile = list.GetArrayElementAtIndex(i);

            int number = profile.FindPropertyRelative("spaceNumber").intValue;
            string name = profile.FindPropertyRelative("displayName").stringValue;

            foldouts[i] = EditorGUILayout.Foldout(
                foldouts[i],
                $"Space {number:00} — {name}",
                true
            );

            if (!foldouts[i])
                continue;

            EditorGUI.indentLevel++;

            EditorGUILayout.LabelField("IDENTITY", EditorStyles.boldLabel);
            Draw(profile, "displayName", "Display Name");
            Draw(profile, "kind", "Visual Type");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("CARD", EditorStyles.boldLabel);
            Draw(profile, "background", "Background");
            Draw(profile, "textColor", "Text Color");
            Draw(profile, "cornerRadius", "Corner Radius");
            Draw(profile, "borderWidth", "Border Width");
            Draw(profile, "borderColor", "Border Color");
            Draw(profile, "shadow", "Shadow");
            Draw(profile, "visualScale", "Scale");
            Draw(profile, "visualOffset", "Position Offset");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("ORIENTATION", EditorStyles.boldLabel);
            Draw(profile, "manualOrientation", "Manual Orientation");

            if (profile.FindPropertyRelative("manualOrientation").boolValue)
                Draw(profile, "rotation", "Rotation");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("CONTENT", EditorStyles.boldLabel);

            if ((BoardVisualMaster.VisualKind)
                profile.FindPropertyRelative("kind").enumValueIndex
                == BoardVisualMaster.VisualKind.Property)
            {
                Draw(profile, "showColorBar", "Show Color Bar");
                Draw(profile, "groupName", "Group Name");
                Draw(profile, "groupColor", "Group Color");
            }

            Draw(profile, "showIcon", "Show Icon");
            Draw(profile, "icon", "Icon");
            Draw(profile, "iconSize", "Icon Size");
            Draw(profile, "showPrice", "Show Price");
            Draw(profile, "priceMillions", "Price (Millions)");
            Draw(profile, "subtitle", "Subtitle");
            Draw(profile, "nameSize", "Name Size");
            Draw(profile, "priceSize", "Price Size");

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);
        }

        EditorGUILayout.EndScrollView();
        serializedObject.ApplyModifiedProperties();
    }

    private void Draw(string propertyName, string label)
    {
        SerializedProperty property = serializedObject.FindProperty(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }

    private void Draw(SerializedProperty parent, string propertyName, string label)
    {
        SerializedProperty property = parent.FindPropertyRelative(propertyName);
        if (property != null)
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }
}
#endif
