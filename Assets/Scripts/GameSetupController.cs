using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameSetupController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Transform playerList;

    [SerializeField] private Button decreasePlayersButton;
    [SerializeField] private Button increasePlayersButton;
    [SerializeField] private Button startGameButton;

    [Header("Player Row")]
    [SerializeField] private GameObject playerRowPrefab;

    [Header("Game")]
    [SerializeField] private string gameSceneName = "MainScene";

    private int playerCount = 2;

    private readonly List<PlayerRowUI> rows =
        new List<PlayerRowUI>();

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        WireButtons();
        RefreshUI();
    }

    // ============================================================
    // BUTTONS
    // ============================================================

    private void WireButtons()
    {
        if (decreasePlayersButton != null)
        {
            decreasePlayersButton.onClick.RemoveAllListeners();
            decreasePlayersButton.onClick.AddListener(
                DecreasePlayerCount
            );
        }

        if (increasePlayersButton != null)
        {
            increasePlayersButton.onClick.RemoveAllListeners();
            increasePlayersButton.onClick.AddListener(
                IncreasePlayerCount
            );
        }

        if (startGameButton != null)
        {
            startGameButton.onClick.RemoveAllListeners();
            startGameButton.onClick.AddListener(
                StartGame
            );
        }
    }

    private void IncreasePlayerCount()
    {
        if (playerCount >= 6)
            return;

        playerCount++;

        RefreshUI();
    }

    private void DecreasePlayerCount()
    {
        if (playerCount <= 2)
            return;

        playerCount--;

        RefreshUI();
    }

    // ============================================================
    // UI
    // ============================================================

    private void RefreshUI()
    {
        if (playerCountText != null)
        {
            playerCountText.text =
                $"PLAYERS: {playerCount}";
        }

        BuildPlayerRows();

        if (decreasePlayersButton != null)
        {
            decreasePlayersButton.interactable =
                playerCount > 2;
        }

        if (increasePlayersButton != null)
        {
            increasePlayersButton.interactable =
                playerCount < 6;
        }
    }

    private void BuildPlayerRows()
    {
        ClearRows();

        if (playerList == null)
        {
            Debug.LogError(
                "GameSetupController: PlayerList is not assigned."
            );

            return;
        }

        if (playerRowPrefab == null)
        {
            Debug.LogError(
                "GameSetupController: PlayerRowPrefab is not assigned."
            );

            return;
        }

        for (int i = 0; i < playerCount; i++)
        {
            GameObject rowObject =
                Instantiate(
                    playerRowPrefab,
                    playerList
                );

            PlayerRowUI row =
                rowObject.GetComponent<PlayerRowUI>();

            if (row == null)
            {
                Debug.LogError(
                    "GameSetupController: " +
                    "PlayerRow prefab does not contain PlayerRowUI."
                );

                continue;
            }

            row.Setup(
                i + 1,
                GetDefaultColor(i)
            );

            rows.Add(row);
        }
    }

    private void ClearRows()
    {
        if (playerList == null)
            return;

        for (int i = playerList.childCount - 1;
             i >= 0;
             i--)
        {
            Destroy(
                playerList.GetChild(i).gameObject
            );
        }

        rows.Clear();
    }

    // ============================================================
    // PLAYER COLORS
    // ============================================================

    private Color GetDefaultColor(int index)
    {
        Color[] colors =
        {
            new Color(0.90f, 0.15f, 0.15f),
            new Color(0.15f, 0.35f, 0.95f),
            new Color(0.15f, 0.80f, 0.30f),
            new Color(0.95f, 0.80f, 0.10f),
            new Color(0.70f, 0.20f, 0.85f),
            new Color(1.00f, 0.45f, 0.10f)
        };

        return colors[
            Mathf.Clamp(
                index,
                0,
                colors.Length - 1
            )
        ];
    }

    // ============================================================
    // START GAME
    // ============================================================

    private void StartGame()
    {
        List<LocalGameSettings.PlayerConfig> configs =
            new List<LocalGameSettings.PlayerConfig>();

        foreach (PlayerRowUI row in rows)
        {
            if (row == null)
                continue;

            LocalGameSettings.PlayerConfig config =
                new LocalGameSettings.PlayerConfig();

            config.playerName =
                row.GetPlayerName();

            config.isBot =
                row.IsBot();

            config.color =
                row.GetColor();

            configs.Add(config);
        }

        if (configs.Count < 2)
        {
            Debug.LogError(
                "GameSetupController: " +
                "At least 2 players are required."
            );

            return;
        }

        LocalGameSettings.SetPlayers(
            configs
        );

        Debug.Log(
            $"Starting local game with " +
            $"{configs.Count} players."
        );

        if (string.IsNullOrWhiteSpace(gameSceneName))
        {
            Debug.LogError(
                "GameSetupController: " +
                "Game scene name is empty."
            );

            return;
        }

        SceneManager.LoadScene(
            gameSceneName
        );
    }
}