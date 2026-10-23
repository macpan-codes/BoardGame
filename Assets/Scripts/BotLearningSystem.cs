using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Lightweight persistent, reward-shaped learning for Monopoly bot decisions.
/// It learns from relative net-worth changes during bot turns and from final
/// match outcomes. Existing GameManager rules still execute legal actions.
/// </summary>
public static class BotLearningSystem
{
    public enum ActionType
    {
        Purchase,
        Development,
        Mortgage,
        Trade
    }

    [Serializable]
    private class LearningData
    {
        public int schemaVersion = 1;
        public int completedTrainingMatches;
        public int completedTrainingTurns;
        public int totalRecordedActions;
        public float purchasePreference;
        public float developmentPreference;
        public float mortgagePreference;
        public float tradePreference;
        public string lastMatchSummary = "No completed training matches yet.";
    }

    private sealed class PlayerStats
    {
        public int turns;
        public readonly Dictionary<ActionType, int> actions =
            new Dictionary<ActionType, int>();
    }

    private const float OnlineLearningRate = 0.025f;
    private const float MatchLearningRate = 0.08f;
    private const int RecentTurnLogInterval = 25;
    private static readonly string DataPath =
        Path.Combine(Application.persistentDataPath, "bot_learning_data.json");

    private static LearningData data;
    private static bool loaded;
    private static bool trainingMatch;
    private static int onlineTurnsSinceLog;
    private static BoardPlayer currentTurnPlayer;
    private static readonly Dictionary<BoardPlayer, PlayerStats> matchStats =
        new Dictionary<BoardPlayer, PlayerStats>();
    private static readonly Dictionary<BoardPlayer, long> turnStartScores =
        new Dictionary<BoardPlayer, long>();
    private static readonly Dictionary<ActionType, int> currentTurnActions =
        new Dictionary<ActionType, int>();

    public static int CompletedTrainingMatches
    {
        get { EnsureLoaded(); return data.completedTrainingMatches; }
    }

    public static bool IsTrainingMatch => trainingMatch;

    public static bool AutoRestartEnabled =>
        PlayerPrefs.GetInt("BotLearning.AutoRestart", 0) == 1;

    public static void SetAutoRestartEnabled(bool enabled)
    {
        PlayerPrefs.SetInt("BotLearning.AutoRestart", enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void ResetSavedLearning()
    {
        EnsureLoaded();
        data = new LearningData();
        matchStats.Clear();
        turnStartScores.Clear();
        currentTurnActions.Clear();
        currentTurnPlayer = null;
        trainingMatch = false;
        try
        {
            if (File.Exists(DataPath))
                File.Delete(DataPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("BOT LEARNING: Could not remove saved policy. " + exception.Message);
        }
        Save();
    }

    public static string LastMatchSummary
    {
        get { EnsureLoaded(); return data.lastMatchSummary; }
    }

    public static float GetPreference(ActionType action)
    {
        EnsureLoaded();
        switch (action)
        {
            case ActionType.Purchase: return data.purchasePreference;
            case ActionType.Development: return data.developmentPreference;
            case ActionType.Mortgage: return data.mortgagePreference;
            case ActionType.Trade: return data.tradePreference;
            default: return 0f;
        }
    }

    /// <summary>Positive value encourages the action; negative discourages it.</summary>
    public static int GetCashReserveAdjustment(ActionType action, int maximumAdjustment)
    {
        return Mathf.RoundToInt(-GetPreference(action) * Mathf.Max(0, maximumAdjustment));
    }

    public static void BeginMatch(BoardPlayer[] players)
    {
        EnsureLoaded();
        trainingMatch = players != null && players.Length >= 2;
        matchStats.Clear();
        turnStartScores.Clear();
        currentTurnActions.Clear();
        currentTurnPlayer = null;
        onlineTurnsSinceLog = 0;

        if (trainingMatch)
        {
            foreach (BoardPlayer player in players)
            {
                if (player == null || !player.IsBot)
                {
                    trainingMatch = false;
                    break;
                }
            }
        }

        if (trainingMatch)
        {
            foreach (BoardPlayer player in players)
                matchStats[player] = new PlayerStats();

            Debug.Log("BOT LEARNING: self-play enabled. Persistent policy loaded. " +
                GetPolicySummary());
        }
        else
        {
            Debug.Log("BOT LEARNING: learning updates are enabled only for all-bot matches.");
        }
    }

    public static void BeginTurn(BoardPlayer player, BoardPlayer[] players, BoardSpace[] spaces)
    {
        if (!trainingMatch || player == null || !player.IsBot)
            return;

        currentTurnPlayer = player;
        currentTurnActions.Clear();
        turnStartScores.Clear();

        foreach (BoardPlayer participant in players)
        {
            if (participant == null)
                continue;

            turnStartScores[participant] = EstimateNetWorth(participant, spaces);
        }

        PlayerStats stats;
        if (matchStats.TryGetValue(player, out stats))
            stats.turns++;
    }

    public static void RecordAction(BoardPlayer player, ActionType action)
    {
        if (!trainingMatch || player == null || player != currentTurnPlayer)
            return;

        PlayerStats stats;
        if (!matchStats.TryGetValue(player, out stats))
            return;

        int count;
        stats.actions.TryGetValue(action, out count);
        stats.actions[action] = count + 1;

        currentTurnActions.TryGetValue(action, out count);
        currentTurnActions[action] = count + 1;

        data.totalRecordedActions++;
    }

    /// <summary>
    /// Applies a small online update from the player's change in net worth
    /// relative to the average change of the other players.
    /// </summary>
    public static void CompleteTurn(BoardPlayer player, BoardPlayer[] players, BoardSpace[] spaces)
    {
        if (!trainingMatch || player == null || player != currentTurnPlayer)
            return;

        PlayerStats stats;
        if (!matchStats.TryGetValue(player, out stats))
            return;

        long startingScore;
        turnStartScores.TryGetValue(player, out startingScore);
        long endingScore = EstimateNetWorth(player, spaces);
        long ownChange = endingScore - startingScore;

        double opponentChangeTotal = 0;
        int opponentCount = 0;
        foreach (BoardPlayer opponent in players)
        {
            if (opponent == null || opponent == player)
                continue;

            long before;
            if (!turnStartScores.TryGetValue(opponent, out before))
                continue;

            opponentChangeTotal += EstimateNetWorth(opponent, spaces) - before;
            opponentCount++;
        }

        if (opponentCount > 0)
        {
            if (currentTurnActions.Count > 0)
            {
                double relativeChange = ownChange - (opponentChangeTotal / opponentCount);
                float reward = Mathf.Clamp((float)(relativeChange / 500.0), -1f, 1f);
                foreach (KeyValuePair<ActionType, int> action in currentTurnActions)
                {
                    float scaledReward = reward / Mathf.Max(1, action.Value);
                    AdjustPreference(action.Key, scaledReward * OnlineLearningRate);
                }
            }

            data.completedTrainingTurns++;
            onlineTurnsSinceLog++;
            Save();

            if (onlineTurnsSinceLog >= RecentTurnLogInterval)
            {
                onlineTurnsSinceLog = 0;
                Debug.Log("BOT LEARNING: online policy updated after " +
                    data.completedTrainingTurns + " training turns. " + GetPolicySummary());
            }
        }

        currentTurnPlayer = null;
        currentTurnActions.Clear();
        turnStartScores.Clear();
    }

    public static void CompleteMatch(BoardPlayer winner, BoardPlayer[] players, int completedTurns)
    {
        if (!trainingMatch || winner == null || !winner.IsBot)
        {
            trainingMatch = false;
            return;
        }

        PlayerStats winnerStats;
        if (!matchStats.TryGetValue(winner, out winnerStats))
        {
            trainingMatch = false;
            return;
        }

        foreach (ActionType action in Enum.GetValues(typeof(ActionType)))
        {
            float winnerRate = ActionRate(winnerStats, action);
            float loserRateTotal = 0f;
            int loserCount = 0;

            foreach (BoardPlayer player in players)
            {
                if (player == null || player == winner)
                    continue;

                PlayerStats loserStats;
                if (!matchStats.TryGetValue(player, out loserStats))
                    continue;

                loserRateTotal += ActionRate(loserStats, action);
                loserCount++;
            }

            if (loserCount > 0)
            {
                float loserRate = loserRateTotal / loserCount;
                // Actions used more frequently by the winner than by opponents
                // become modestly more attractive in future matches.
                AdjustPreference(action, (winnerRate - loserRate) * MatchLearningRate);
            }
        }

        data.completedTrainingMatches++;
        data.lastMatchSummary = "Match " + data.completedTrainingMatches +
            ": " + winner.PlayerName + " won; " + Mathf.Max(0, completedTurns) +
            " completed turns; policy updated.";
        Save();

        Debug.Log("BOT LEARNING: " + data.lastMatchSummary + " " + GetPolicySummary());
        trainingMatch = false;
        currentTurnPlayer = null;
        matchStats.Clear();
        turnStartScores.Clear();
        currentTurnActions.Clear();
    }

    public static string GetPolicySummary()
    {
        EnsureLoaded();
        return "matches=" + data.completedTrainingMatches +
            ", turns=" + data.completedTrainingTurns +
            ", buy=" + data.purchasePreference.ToString("F2") +
            ", build=" + data.developmentPreference.ToString("F2") +
            ", mortgage=" + data.mortgagePreference.ToString("F2") +
            ", trade=" + data.tradePreference.ToString("F2");
    }

    private static float ActionRate(PlayerStats stats, ActionType action)
    {
        if (stats == null || stats.turns <= 0)
            return 0f;

        int count;
        stats.actions.TryGetValue(action, out count);
        return (float)count / stats.turns;
    }

    private static long EstimateNetWorth(BoardPlayer player, BoardSpace[] spaces)
    {
        if (player == null)
            return 0;

        long value = player.Money;
        if (spaces == null)
            return value;

        foreach (BoardSpace space in spaces)
        {
            if (space == null || space.Owner != player)
                continue;

            long propertyValue = Mathf.Max(0, space.PurchasePrice);
            if (space.IsProperty)
            {
                propertyValue += (long)Mathf.Max(0, space.EffectiveHouseCost) *
                    Mathf.Max(0, space.Houses);
                if (space.HasHotel)
                    propertyValue += Mathf.Max(0, space.EffectiveHotelCost);
            }

            if (space.IsMortgaged)
                propertyValue -= Mathf.Max(0, space.MortgageValue);

            value += propertyValue;
        }

        return value;
    }

    private static void AdjustPreference(ActionType action, float delta)
    {
        const float limit = 1f;
        switch (action)
        {
            case ActionType.Purchase:
                data.purchasePreference = Mathf.Clamp(data.purchasePreference + delta, -limit, limit);
                break;
            case ActionType.Development:
                data.developmentPreference = Mathf.Clamp(data.developmentPreference + delta, -limit, limit);
                break;
            case ActionType.Mortgage:
                data.mortgagePreference = Mathf.Clamp(data.mortgagePreference + delta, -limit, limit);
                break;
            case ActionType.Trade:
                data.tradePreference = Mathf.Clamp(data.tradePreference + delta, -limit, limit);
                break;
        }
    }

    private static void EnsureLoaded()
    {
        if (loaded)
            return;

        loaded = true;
        data = new LearningData();
        try
        {
            if (File.Exists(DataPath))
            {
                LearningData saved = JsonUtility.FromJson<LearningData>(File.ReadAllText(DataPath));
                if (saved != null && saved.schemaVersion == 1)
                    data = saved;
            }
        }
        catch (Exception exception)
        {
            Debug.LogWarning("BOT LEARNING: Could not load saved policy; using defaults. " +
                exception.Message);
            data = new LearningData();
        }
    }

    private static void Save()
    {
        EnsureLoaded();
        try
        {
            string directory = Path.GetDirectoryName(DataPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string temporaryPath = DataPath + ".tmp";
            File.WriteAllText(temporaryPath, JsonUtility.ToJson(data, true));
            if (File.Exists(DataPath))
                File.Delete(DataPath);
            File.Move(temporaryPath, DataPath);
        }
        catch (Exception exception)
        {
            Debug.LogWarning("BOT LEARNING: Could not save policy. " + exception.Message);
        }
    }
}
