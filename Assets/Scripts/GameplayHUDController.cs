using System.Collections.Generic;
using UnityEngine;

public class GameplayHUDController : MonoBehaviour
{
    [Header("Game")]
    [SerializeField] private GameManager gameManager;

    [Header("Current Player")]
    [SerializeField] private CurrentPlayerHUD currentPlayerHUD;

    [Header("Player Balance List")]
    [SerializeField] private Transform playerList;
    [SerializeField] private GameObject playerBalanceRowPrefab;

    [Header("Refresh")]
    [SerializeField]
    [Min(0.02f)]
    private float refreshInterval = 0.1f;

    private readonly List<PlayerBalanceRowUI> rows =
        new List<PlayerBalanceRowUI>();

    private int lastPlayerCount = -1;
    private BoardPlayer lastCurrentPlayer;

    private float refreshTimer;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }
    }

    private void Start()
    {
        RefreshImmediately();
    }

    private void Update()
    {
        refreshTimer += Time.unscaledDeltaTime;

        if (refreshTimer < refreshInterval)
            return;

        refreshTimer = 0f;

        RefreshHUD();
    }

    // ============================================================
    // MAIN REFRESH
    // ============================================================

    private void RefreshHUD()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();

            if (gameManager == null)
                return;
        }

        BoardPlayer[] players =
            gameManager.Players;

        if (players == null)
            return;

        BoardPlayer currentPlayer =
            gameManager.CurrentPlayer;

        if (players.Length != lastPlayerCount ||
            currentPlayer != lastCurrentPlayer)
        {
            RebuildPlayerRows(players);

            lastPlayerCount =
                players.Length;

            lastCurrentPlayer =
                currentPlayer;
        }

        if (currentPlayerHUD != null)
        {
            currentPlayerHUD.SetPlayer(
                currentPlayer
            );

            currentPlayerHUD.Refresh();
        }

        for (int i = 0;
             i < rows.Count;
             i++)
        {
            if (rows[i] != null)
            {
                rows[i].Refresh();
            }
        }
    }

    // ============================================================
    // REBUILD PLAYER LIST
    // ============================================================

    private void RebuildPlayerRows(
        BoardPlayer[] players)
    {
        ClearPlayerRows();

        if (playerList == null)
        {
            Debug.LogError(
                "GameplayHUDController: " +
                "Player List is not assigned."
            );

            return;
        }

        if (playerBalanceRowPrefab == null)
        {
            Debug.LogError(
                "GameplayHUDController: " +
                "Player Balance Row Prefab is not assigned."
            );

            return;
        }

        foreach (BoardPlayer player in players)
        {
            if (player == null)
                continue;

            GameObject rowObject =
                Instantiate(
                    playerBalanceRowPrefab,
                    playerList
                );

            PlayerBalanceRowUI row =
                rowObject.GetComponent<PlayerBalanceRowUI>();

            if (row == null)
            {
                Debug.LogError(
                    "GameplayHUDController: " +
                    "PlayerBalanceRow prefab does not " +
                    "contain PlayerBalanceRowUI."
                );

                Destroy(rowObject);

                continue;
            }

            row.Setup(player);

            rows.Add(row);
        }
    }

    // ============================================================
    // CLEAR
    // ============================================================

    private void ClearPlayerRows()
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
    // IMMEDIATE REFRESH
    // ============================================================

    public void RefreshImmediately()
    {
        refreshTimer = 0f;

        lastPlayerCount = -1;
        lastCurrentPlayer = null;

        RefreshHUD();
    }
}