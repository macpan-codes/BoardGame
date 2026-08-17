using UnityEngine;

/// <summary>
/// Controls the bank's money and basic transactions.
/// All amounts are represented in millions.
/// </summary>
public class BankManager : MonoBehaviour
{
    [Header("Bank")]
    [SerializeField] private int startingBankBalance = 50000000;

    private int bankBalance;

    public int BankBalance => bankBalance;

    private void Awake()
    {
        bankBalance = startingBankBalance;
    }

    /// <summary>
    /// Gives money from the bank to a player.
    /// </summary>
    public bool PayPlayer(BoardPlayer player, int amount)
    {
        if (player == null || amount <= 0)
            return false;

        if (bankBalance < amount)
        {
            Debug.LogWarning(
                $"BankManager: Bank does not have enough money to pay ${amount}M."
            );

            return false;
        }

        bankBalance -= amount;
        player.AddMoney(amount);

        Debug.Log(
            $"Bank paid ${amount}M to player. " +
            $"Bank balance: ${bankBalance}M"
        );

        return true;
    }

    /// <summary>
    /// Takes money from a player and gives it to the bank.
    /// </summary>
    public bool PlayerPayBank(BoardPlayer player, int amount)
    {
        if (player == null || amount <= 0)
            return false;

        if (!player.RemoveMoney(amount))
        {
            Debug.LogWarning(
                $"BankManager: Player cannot pay ${amount}M."
            );

            return false;
        }

        bankBalance += amount;

        Debug.Log(
            $"Player paid ${amount}M to bank. " +
            $"Bank balance: ${bankBalance}M"
        );

        return true;
    }

    /// <summary>
    /// Transfers money directly between two players.
    /// </summary>
    public bool PlayerPayPlayer(
        BoardPlayer fromPlayer,
        BoardPlayer toPlayer,
        int amount)
    {
        if (fromPlayer == null || toPlayer == null || amount <= 0)
            return false;

        if (!fromPlayer.RemoveMoney(amount))
        {
            Debug.LogWarning(
                $"BankManager: Player cannot pay ${amount}M."
            );

            return false;
        }

        toPlayer.AddMoney(amount);

        Debug.Log(
            $"Player paid another player ${amount}M."
        );

        return true;
    }
}