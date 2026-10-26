using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the My Properties button as a real, editable scene object.
/// The runtime portfolio script reuses this button instead of generating a duplicate.
/// </summary>
[InitializeOnLoad]
public static class MyPropertiesPortfolioButtonEditor
{
    static MyPropertiesPortfolioButtonEditor()
    {
        EditorApplication.delayCall += EnsureButtonExists;
        EditorSceneManager.sceneOpened += (_, __) => EditorApplication.delayCall += EnsureButtonExists;
    }

    [MenuItem("Board Game/Create My Properties Button")]
    private static void CreateButtonFromMenu()
    {
        EnsureButtonExists();
    }

    private static void EnsureButtonExists()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        Transform existing = canvas.transform.Find("MyPropertiesButton");
        if (existing != null)
            return;

        GameObject buttonObject = new GameObject("MyPropertiesButton", typeof(RectTransform), typeof(Image), typeof(Button));
        Undo.RegisterCreatedObjectUndo(buttonObject, "Create My Properties Button");
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-24f, -24f);
        rect.sizeDelta = new Vector2(178f, 48f);

        buttonObject.GetComponent<Image>().color = new Color(0.30f, 0.83f, 0.70f, 1f);

        GameObject labelObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        labelObject.transform.SetParent(buttonObject.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = new Vector2(8f, 4f);
        labelRect.offsetMax = new Vector2(-8f, -4f);

        Text label = labelObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.text = "MY PROPERTIES";
        label.fontSize = 16;
        label.color = Color.black;
        label.alignment = TextAnchor.MiddleCenter;
        label.raycastTarget = false;

        EditorUtility.SetDirty(buttonObject);
        EditorSceneManager.MarkSceneDirty(buttonObject.scene);
        Selection.activeGameObject = buttonObject;
    }
}
