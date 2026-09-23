using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Handles outstanding-debt recovery and final elimination.
/// Zero cash alone never eliminates a player.
/// The existing property UI remains the recovery interaction surface.
/// </summary>
public class FinancialRecoveryManager : MonoBehaviour
{
    public static FinancialRecoveryManager Instance { get; private set; }

    private BoardPlayer activeDebtor;
    private BoardPlayer creditor;
    private long remainingDebt;
    private string debtReason;
    private bool resolving;

    public bool IsRecoveryActive =>
        activeDebtor != null;

    public BoardPlayer CurrentDebtor =>
        activeDebtor;

    public BoardPlayer CurrentCreditor =>
        creditor;

    public long RemainingDebt =>
        remainingDebt;

    private void Awake()
    {
        if (Instance != null &&
            Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (!IsRecoveryActive ||
            resolving)
        {
            return;
        }

        if (activeDebtor == null ||
            activeDebtor.IsBankrupt)
        {
            ClearRecoveryState();
            return;
        }

        if (activeDebtor.Money >= remainingDebt)
        {
            CompleteDebt();
            return;
        }

        if (GetTotalRecoveryValue(activeDebtor) < remainingDebt)
        {
            EliminateForDebt();
        }
    }

    public void ResetForNewGame()
    {
        ClearRecoveryState();
    }

    public bool IsRecovering(
        BoardPlayer player)
    {
        return IsRecoveryActive &&
               activeDebtor == player;
    }

    /// <summary>
    /// Starts debt recovery. Existing cash is paid first. Any remainder
    /// remains open while the player liquidates assets.
    /// </summary>
    public bool BeginDebtResolution(
        BoardPlayer debtor,
        BoardPlayer debtCreditor,
        long debtAmount,
        string reason)
    {
        if (debtor == null ||
            debtor.IsBankrupt ||
            debtAmount <= 0)
        {
            return false;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null ||
            gameManager.GameOver ||
            gameManager.CurrentPlayer != debtor)
        {
            return false;
        }

        if (IsRecoveryActive)
        {
            return activeDebtor == debtor;
        }

        long cashContribution =
            System.Math.Min(
                (long)debtor.Money,
                debtAmount
            );

        if (cashContribution > 0)
        {
            if (!debtor.RemoveMoney(
                    SafeInt(cashContribution)))
            {
                return false;
            }

            CreditDebt(
                debtCreditor,
                cashContribution
            );
        }

        long remaining =
            debtAmount -
            cashContribution;

        if (remaining <= 0)
        {
            gameManager.EndTurn();
            return true;
        }

        activeDebtor = debtor;
        creditor = debtCreditor;
        remainingDebt = remaining;
        debtReason =
            string.IsNullOrWhiteSpace(reason)
                ? "OUTSTANDING DEBT"
                : reason;

        gameManager.BeginPlayerAction();

        GameNotificationUI.Show(
            $"{debtor.PlayerName.ToUpperInvariant()} OWES " +
            $"${remainingDebt:N0}M — MORTGAGE / LIQUIDATE PROPERTY"
        );

        Debug.Log(
            $"FINANCIAL RECOVERY STARTED: " +
            $"{debtor.PlayerName} | " +
            $"Remaining ${remainingDebt:N0}M | " +
            $"Creditor: {(creditor != null ? creditor.PlayerName : "BANK")} | " +
            $"Reason: {debtReason}"
        );

        if (GetTotalRecoveryValue(debtor) < remainingDebt)
        {
            EliminateForDebt();
        }

        return true;
    }

    public List<BoardSpace> GetMortgageableProperties(
        BoardPlayer player)
    {
        List<BoardSpace> result =
            new List<BoardSpace>();

        if (player == null)
            return result;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null ||
                space.Owner != player ||
                !space.CanMortgage())
            {
                continue;
            }

            result.Add(space);
        }

        result.Sort(
            (a, b) =>
                a.BoardIndex.CompareTo(
                    b.BoardIndex
                )
        );

        return result;
    }

    public int GetTotalRecoveryValue(
        BoardPlayer player)
    {
        if (player == null)
            return 0;

        long total = 0;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null ||
                space.Owner != player)
            {
                continue;
            }

            total +=
                space.GetRecoveryLiquidationValue();

            if (total >= int.MaxValue)
                return int.MaxValue;
        }

        return Mathf.Max(
            0,
            (int)total
        );
    }

    /// <summary>
    /// Compatibility alias for diagnostics/testers.
    /// This returns the total recovery value available through the
    /// current property's effective mortgage/liquidation values.
    /// </summary>
    public int GetTotalMortgageValue(
        BoardPlayer player)
    {
        return GetTotalRecoveryValue(player);
    }

    public void EliminateNow(
        BoardPlayer player,
        BoardPlayer debtCreditor,
        string reason)
    {
        if (player == null ||
            player.IsBankrupt)
        {
            return;
        }

        resolving = true;

        SettleAssets(
            player,
            debtCreditor
        );

        player.MarkEliminated();

        GameNotificationUI.Show(
            $"{player.PlayerName.ToUpperInvariant()} HAS BEEN ELIMINATED"
        );

        Debug.Log(
            $"PLAYER ELIMINATED: {player.PlayerName} | {reason}"
        );

        ClearRecoveryState();

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.CheckForWinner();

            if (!gameManager.GameOver &&
                gameManager.WaitingForPlayerAction)
            {
                gameManager.EndPlayerAction();
            }

            if (!gameManager.GameOver)
            {
                gameManager.EndTurn();
            }
        }

        resolving = false;
    }

    private void CompleteDebt()
    {
        if (!IsRecoveryActive ||
            resolving)
        {
            return;
        }

        BoardPlayer debtor = activeDebtor;
        BoardPlayer debtCreditor = creditor;
        long amount = remainingDebt;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (debtor == null ||
            gameManager == null ||
            debtor.Money < amount)
        {
            return;
        }

        resolving = true;

        if (!debtor.RemoveMoney(
                SafeInt(amount)))
        {
            resolving = false;
            return;
        }

        CreditDebt(
            debtCreditor,
            amount
        );

        GameNotificationUI.Show(
            debtCreditor != null
                ? $"{debtor.PlayerName.ToUpperInvariant()} PAID " +
                  $"${amount:N0}M TO {debtCreditor.PlayerName.ToUpperInvariant()}"
                : $"{debtor.PlayerName.ToUpperInvariant()} PAID " +
                  $"${amount:N0}M TO THE BANK"
        );

        ClearRecoveryState();

        if (gameManager.WaitingForPlayerAction)
            gameManager.EndPlayerAction();

        gameManager.EndTurn();

        resolving = false;
    }

    private void EliminateForDebt()
    {
        if (!IsRecoveryActive ||
            resolving)
        {
            return;
        }

        resolving = true;

        BoardPlayer debtor = activeDebtor;
        BoardPlayer debtCreditor = creditor;

        SettleAssets(
            debtor,
            debtCreditor
        );

        debtor.MarkEliminated();

        GameNotificationUI.Show(
            $"{debtor.PlayerName.ToUpperInvariant()} " +
            "COULD NOT SATISFY THE DEBT AND WAS ELIMINATED"
        );

        Debug.Log(
            $"FINANCIAL ELIMINATION: " +
            $"{debtor.PlayerName} | " +
            $"Unpaid ${remainingDebt:N0}M | " +
            $"Reason: {debtReason}"
        );

        ClearRecoveryState();

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.CheckForWinner();

            if (!gameManager.GameOver &&
                gameManager.WaitingForPlayerAction)
            {
                gameManager.EndPlayerAction();
            }

            if (!gameManager.GameOver)
            {
                gameManager.EndTurn();
            }
        }

        resolving = false;
    }

    private void SettleAssets(
        BoardPlayer player,
        BoardPlayer debtCreditor)
    {
        if (player == null)
            return;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null ||
                space.Owner != player)
            {
                continue;
            }

            if (debtCreditor != null &&
                debtCreditor != player)
            {
                space.TransferOwnershipForBankruptcy(
                    debtCreditor
                );
            }
            else
            {
                space.ResetProperty();
            }
        }
    }

    private void CreditDebt(
        BoardPlayer debtCreditor,
        long amount)
    {
        if (amount <= 0)
            return;

        if (debtCreditor != null &&
            !debtCreditor.IsBankrupt)
        {
            debtCreditor.AddMoney(
                SafeInt(amount)
            );

            return;
        }

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager != null)
        {
            gameManager.AddBankMoney(amount);
        }
    }

    private void ClearRecoveryState()
    {
        activeDebtor = null;
        creditor = null;
        remainingDebt = 0;
        debtReason = null;
    }

    private int SafeInt(long amount)
    {
        if (amount <= 0)
            return 0;

        return amount > int.MaxValue
            ? int.MaxValue
            : (int)amount;
    }
}
