#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(BoardVisualMaster))]
public class BoardVisualMasterEditor : Editor
{
    private Vector2 scroll;
    private string search = string.Empty;
    private readonly bool[] foldouts = new bool[40];

    public override void OnInspectorGUI()
    {
        BoardVisualMaster master = (BoardVisualMaster)target;
        serializedObject.Update();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Live Preview", GUILayout.Width(90f));

        SerializedProperty livePreview =
            serializedObject.FindProperty("livePreview");

        if (livePreview != null)
        {
            bool newLiveValue =
                EditorGUILayout.Toggle(
                    livePreview.boolValue
                );

            if (newLiveValue != livePreview.boolValue)
            {
                livePreview.boolValue = newLiveValue;

                if (newLiveValue)
                    master.RefreshLivePreview();
            }
        }

        if (GUILayout.Button("Refresh Now", GUILayout.Width(95f)))
            master.RefreshLivePreview();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Presentation-only controller. Gameplay BoardSpace data is not edited.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Sync Profiles", GUILayout.Height(28)))
            master.SyncProfilesFromBoard();
        if (GUILayout.Button("Build Visuals", GUILayout.Height(28)))
            master.BuildMasterVisuals();
        if (GUILayout.Button("Clear Visuals", GUILayout.Height(28)))
            master.ClearMasterVisuals();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);
        search = EditorGUILayout.TextField("Find Space", search);

        SerializedProperty profiles = serializedObject.FindProperty("spaces");

        EditorGUILayout.Space(4);
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MinHeight(450));

        if (profiles == null || profiles.arraySize == 0)
        {
            EditorGUILayout.HelpBox("Press Sync Profiles first.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            serializedObject.ApplyModifiedProperties();
            return;
        }

        int visible = 0;
        bool profileValuesChanged = false;

        for (int i = 0; i < profiles.arraySize; i++)
        {
            SerializedProperty profile = profiles.GetArrayElementAtIndex(i);
            int number = profile.FindPropertyRelative("spaceNumber").intValue;
            string name = profile.FindPropertyRelative("displayName").stringValue;
            string label = $"Space {number:00} — {name}";

            if (!string.IsNullOrWhiteSpace(search) &&
                label.IndexOf(search, System.StringComparison.OrdinalIgnoreCase) < 0)
                continue;

            visible++;
            foldouts[i] = EditorGUILayout.Foldout(foldouts[i], label, true);
            if (!foldouts[i])
                continue;

            EditorGUI.BeginChangeCheck();

            EditorGUI.indentLevel++;

            Draw(profile, "displayName", "Display Name");
            Draw(profile, "kind", "Visual Type");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Surface", EditorStyles.boldLabel);
            Draw(profile, "backgroundColor", "Background");
            Draw(profile, "textColor", "Text Color");
            Draw(profile, "cornerRadius", "Corner Radius");
            Draw(profile, "borderWidth", "Border Width");
            Draw(profile, "borderColor", "Border Color");
            Draw(profile, "showShadow", "Shadow");
            Draw(profile, "visualScale", "Visual Scale");
            Draw(profile, "visualOffset", "Visual Offset");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Orientation", EditorStyles.boldLabel);
            Draw(profile, "orientation", "Mode");
            Draw(profile, "manualRotation", "Manual Rotation");

            BoardVisualMaster.VisualKind kind =
                (BoardVisualMaster.VisualKind)profile.FindPropertyRelative("kind").enumValueIndex;

            if (kind == BoardVisualMaster.VisualKind.Property)
            {
                EditorGUILayout.Space(3);
                EditorGUILayout.LabelField("Property Group", EditorStyles.boldLabel);
                Draw(profile, "showGroupBar", "Show Color Bar");
                Draw(profile, "useGroupColor", "Use Group Color");
                Draw(profile, "groupName", "Group Name");
                Draw(profile, "groupColor", "Group Color");
            }

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Icon", EditorStyles.boldLabel);
            Draw(profile, "showIcon", "Show Icon");
            Draw(profile, "icon", "Icon Sprite");
            Draw(profile, "iconSize", "Icon Size");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Price", EditorStyles.boldLabel);
            Draw(profile, "showPrice", "Show Price");
            Draw(profile, "priceMillions", "Price (Millions)");
            Draw(profile, "priceFontSize", "Price Size");

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Typography", EditorStyles.boldLabel);
            Draw(profile, "nameFontSize", "Name Size");
            Draw(profile, "subtitle", "Subtitle");
            Draw(profile, "barThickness", "Color Bar Thickness");

            EditorGUI.indentLevel--;

            if (EditorGUI.EndChangeCheck())
                profileValuesChanged = true;

            EditorGUILayout.Space(8);
        }

        if (visible == 0)
            EditorGUILayout.HelpBox("No matching spaces.", MessageType.Info);

        EditorGUILayout.EndScrollView();

        bool serializedChanged =
            serializedObject.ApplyModifiedProperties();

        if ((profileValuesChanged || serializedChanged) &&
            livePreview != null &&
            livePreview.boolValue)
        {
            master.RefreshLivePreview();
        }
    }

    private static void Draw(SerializedProperty parent, string name, string label)
    {
        SerializedProperty property = parent.FindPropertyRelative(name);
        if (property != null)
            EditorGUILayout.PropertyField(property, new GUIContent(label), true);
    }
}
#endif
