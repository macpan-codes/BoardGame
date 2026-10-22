using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Creates a saved, editable Settings UI prefab and installs it into the
/// main-menu and gameplay scenes. Run from Tools > Board Game > Install Editable Settings UI.
/// </summary>
public static class EditableSettingsUIInstaller
{
    private const string PrefabPath = "Assets/Prefabs/UI/GameAudioSettingsUI.prefab";
    private static readonly string[] TargetScenes =
    {
        "Assets/Scenes/MainMenu.unity",
        "Assets/Scenes/MainScene.unity"
    };

    [MenuItem("Tools/Board Game/Install Editable Settings UI")]
    public static void Install()
    {
        if (!EditorUtility.DisplayDialog(
                "Install Editable Settings UI",
                "This will create/update the Settings UI prefab and save the MainMenu and MainScene scenes. Continue?",
                "Install",
                "Cancel"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        string previousScenePath = SceneManager.GetActiveScene().path;
        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Prefabs/UI");

        GameObject prefab = CreateOrUpdatePrefab();
        if (prefab == null)
        {
            EditorUtility.DisplayDialog(
                "Settings UI installation failed",
                "Unity could not create the Settings UI prefab. Check the Console for errors.",
                "OK");
            return;
        }

        int installed = 0;
        foreach (string scenePath in TargetScenes)
        {
            if (!File.Exists(scenePath))
            {
                Debug.LogError("EditableSettingsUIInstaller: Scene not found: " + scenePath);
                continue;
            }

            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            GameAudioSettingsUI existing =
                Object.FindFirstObjectByType<GameAudioSettingsUI>(FindObjectsInactive.Include);

            if (existing == null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
                if (instance != null)
                {
                    instance.name = "GameAudioSettingsUI";
                    instance.SetActive(true);
                    EditorSceneManager.MarkSceneDirty(scene);
                    installed++;
                }
                else
                {
                    Debug.LogError("EditableSettingsUIInstaller: Failed to add Settings UI to " + scenePath);
                }
            }
            else
            {
                Debug.Log("EditableSettingsUIInstaller: Settings UI already exists in " + scenePath);
            }

            EditorSceneManager.SaveScene(scene);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (!string.IsNullOrEmpty(previousScenePath) && File.Exists(previousScenePath))
            EditorSceneManager.OpenScene(previousScenePath, OpenSceneMode.Single);

        EditorUtility.DisplayDialog(
            "Editable Settings UI ready",
            "Settings UI prefab created at " + PrefabPath +
            "\n\nInstalled into " + installed + " scene(s). Open the MainMenu or MainScene Hierarchy and expand GameAudioSettingsUI to edit the layout, text, colors, and fonts.",
            "OK");
    }

    private static GameObject CreateOrUpdatePrefab()
    {
        GameObject root = new GameObject(
            "GameAudioSettingsUI",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster),
            typeof(GameAudioSettingsUI));

        try
        {
            GameAudioSettingsUI settings = root.GetComponent<GameAudioSettingsUI>();
            settings.BuildUIForEditor();

            GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            if (savedPrefab == null)
                Debug.LogError("EditableSettingsUIInstaller: SaveAsPrefabAsset returned null.");

            return savedPrefab;
        }
        finally
        {
            Object.DestroyImmediate(root);
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
        string folderName = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !AssetDatabase.IsValidFolder(parent))
            EnsureFolder(parent);

        if (!string.IsNullOrEmpty(parent))
            AssetDatabase.CreateFolder(parent, folderName);
    }
}
