using UnityEditor;
using UnityEngine;

/// <summary>
/// Editor controls for persistent bot self-play training.
/// </summary>
public static class BotLearningTools
{
    [MenuItem("Board Game/Bot Learning/Show Learning Status")]
    public static void ShowLearningStatus()
    {
        string status = BotLearningSystem.GetPolicySummary() +
            "\n\nLast result: " + BotLearningSystem.LastMatchSummary +
            "\nAuto-restart self-play: " +
            (BotLearningSystem.AutoRestartEnabled ? "ON" : "OFF") +
            "\n\nSaved policy file: " +
            System.IO.Path.Combine(Application.persistentDataPath, "bot_learning_data.json");

        EditorUtility.DisplayDialog("Bot Learning Status", status, "OK");
        Debug.Log("BOT LEARNING STATUS: " + status.Replace("\n", " | "));
    }

    [MenuItem("Board Game/Bot Learning/Toggle Auto-Restart Self-Play")]
    public static void ToggleAutoRestart()
    {
        bool enabled = !BotLearningSystem.AutoRestartEnabled;
        BotLearningSystem.SetAutoRestartEnabled(enabled);
        EditorUtility.DisplayDialog(
            "Bot Self-Play",
            enabled
                ? "Auto-restart is ON. When an all-bot match ends, the scene will reload after 3 seconds and start another match. Keep all players set to Bot."
                : "Auto-restart is OFF. Matches will stop normally at the results screen.",
            "OK"
        );
        Debug.Log("BOT LEARNING: auto-restart self-play " + (enabled ? "enabled." : "disabled."));
    }

    [MenuItem("Board Game/Bot Learning/Reset Saved Learning")]
    public static void ResetSavedLearning()
    {
        if (!EditorUtility.DisplayDialog(
            "Reset Bot Learning",
            "Delete the saved policy and reset all learned preferences? This cannot be undone.",
            "Reset Learning",
            "Cancel"))
            return;

        BotLearningSystem.ResetSavedLearning();
        EditorUtility.DisplayDialog("Bot Learning", "Saved learning has been reset.", "OK");
    }
}
