#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Friendly text editor for the seven contributor cards.
/// Select CreditsScreen in Assets/Scenes/Credits.unity while NOT in Play Mode.
/// Changes are serialized into the Credits scene and included in Android builds.
/// </summary>
[CustomEditor(typeof(CreditsScreen))]
public class CreditsTextInspector : Editor
{
    private SerializedProperty people;
    private bool[] expanded = new bool[0];

    private void OnEnable()
    {
        people = serializedObject.FindProperty("people");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.HelpBox(
            "EDIT CREDITS HERE: Open Assets/Scenes/Credits.unity, select CreditsScreen in the Hierarchy, " +
            "then expand any contributor below. Edit the text, press Ctrl+S to save the scene, " +
            "and enter Play Mode to preview. Do NOT edit in Play Mode.",
            MessageType.Info);

        if (people == null)
        {
            EditorGUILayout.HelpBox("Contributor data was not found.", MessageType.Error);
            return;
        }

        if (expanded.Length != people.arraySize)
            System.Array.Resize(ref expanded, people.arraySize);

        for (int i = 0; i < people.arraySize; i++)
        {
            SerializedProperty person = people.GetArrayElementAtIndex(i);
            SerializedProperty name = person.FindPropertyRelative("name");
            SerializedProperty title = person.FindPropertyRelative("title");
            SerializedProperty contribution = person.FindPropertyRelative("contribution");
            SerializedProperty funFact = person.FindPropertyRelative("funFact");
            SerializedProperty achievement = person.FindPropertyRelative("achievement");

            EditorGUILayout.Space(7);
            string heading = string.IsNullOrWhiteSpace(name.stringValue)
                ? "Contributor " + (i + 1) : (i + 1) + ". " + name.stringValue;
            expanded[i] = EditorGUILayout.Foldout(expanded[i], heading, true, EditorStyles.foldoutHeader);
            if (!expanded[i]) continue;

            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(name, new GUIContent("Display Name"));
            EditorGUILayout.PropertyField(title, new GUIContent("Funny Title"));
            EditorGUILayout.LabelField("How They Helped");
            contribution.stringValue = EditorGUILayout.TextArea(contribution.stringValue,
                GUILayout.MinHeight(75));
            EditorGUILayout.LabelField("Fun Fact");
            funFact.stringValue = EditorGUILayout.TextArea(funFact.stringValue,
                GUILayout.MinHeight(60));
            EditorGUILayout.PropertyField(achievement, new GUIContent("Achievement"));
            EditorGUI.indentLevel--;
        }

        if (serializedObject.ApplyModifiedProperties())
            EditorUtility.SetDirty(target);
    }
}
#endif
