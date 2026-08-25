using System.Collections.Generic;
using UnityEngine;

public static class LocalGameSettings
{
    public class PlayerConfig
    {
        public string playerName;
        public bool isBot;
        public Color color;
    }

    public static int PlayerCount
    {
        get;
        private set;
    } = 2;

    public static readonly List<PlayerConfig> Players =
        new List<PlayerConfig>();

    public static void SetPlayers(
        List<PlayerConfig> configs)
    {
        Players.Clear();

        if (configs == null ||
            configs.Count < 2)
        {
            PlayerCount = 2;
            return;
        }

        PlayerCount =
            Mathf.Clamp(
                configs.Count,
                2,
                6
            );

        Players.AddRange(configs);
    }

    public static PlayerConfig GetPlayer(
        int index)
    {
        if (index < 0 ||
            index >= Players.Count)
        {
            return null;
        }

        return Players[index];
    }

    public static void Clear()
    {
        Players.Clear();
        PlayerCount = 2;
    }
}