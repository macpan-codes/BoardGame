#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>Run once; creates a scene and wires the EXISTING themed Multiplayer button.</summary>
public static class MultiplayerPlaceholderInstaller
{
    private const string MenuPath = "Assets/Scenes/MainMenu.unity";
    private const string PlaceholderPath = "Assets/Scenes/MultiplayerComingSoon.unity";

    [MenuItem("Board Game/Multiplayer/Install Under Development Scene")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Multiplayer Setup", "Stop Play Mode first.", "OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        new GameObject("MultiplayerComingSoonScreen").AddComponent<MultiplayerComingSoonScreen>();
        EditorSceneManager.SaveScene(scene, PlaceholderPath);

        var menu = EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
        GameObject existing = GameObject.Find("MultiplayerButton");
        MainMenuController controller = Object.FindFirstObjectByType<MainMenuController>();
        if (existing == null || controller == null)
        {
            Debug.LogError("Multiplayer Setup: MultiplayerButton or MainMenuController missing.");
            return;
        }
        Button button = existing.GetComponent<Button>();
        if (button == null) button = existing.AddComponent<Button>();
        for (int i = button.onClick.GetPersistentEventCount() - 1; i >= 0; --i)
            UnityEventTools.RemovePersistentListener(button.onClick, i);
        button.onClick.RemoveAllListeners();
        UnityEventTools.AddPersistentListener(button.onClick, controller.OpenMultiplayer);
        EditorUtility.SetDirty(button);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        var buildScenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found = false;
        foreach (var entry in buildScenes)
        {
            if (entry.path != PlaceholderPath) continue;
            entry.enabled = true;
            found = true;
        }
        if (!found) buildScenes.Add(new EditorBuildSettingsScene(PlaceholderPath, true));
        EditorBuildSettings.scenes = buildScenes.ToArray();

        Debug.Log("Multiplayer placeholder installed. MainMenu MultiplayerButton routes to MultiplayerComingSoon; added to build.");
        EditorSceneManager.OpenScene(MenuPath, OpenSceneMode.Single);
        EditorUtility.DisplayDialog("Multiplayer Placeholder Ready", "Main menu button connected. Press Play and tap Multiplayer to preview.", "OK");
    }
}
#endif
