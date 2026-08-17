using System.Collections;
using UnityEngine;

/// <summary>
/// Handles rolling the two dice and starting movement for
/// the current player.
///
/// GameManager remains the single source of truth for turns.
/// </summary>
public class DiceManager : MonoBehaviour
{
    [Header("Game Manager")]
    [SerializeField] private GameManager gameManager;

    [Header("Dice Result")]
    [SerializeField] private int firstDie;
    [SerializeField] private int secondDie;
    [SerializeField] private int total;

    [Header("State")]
    [SerializeField] private bool isRolling;

    public int FirstDie => firstDie;
    public int SecondDie => secondDie;
    public int Total => total;
    public bool IsRolling => isRolling;

    private void Awake()
    {
        FindGameManager();
    }

    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>();
        }
    }

    // ============================================================
    // ROLL DICE
    // ============================================================

    public void RollDice()
    {
        if (isRolling)
            return;

        FindGameManager();

        if (gameManager == null)
        {
            Debug.LogError(
                "DiceManager: GameManager could not be found."
            );

            return;
        }

        if (gameManager.GameOver)
        {
            Debug.Log(
                "DiceManager: The game is already over."
            );

            return;
        }

        if (!gameManager.GameStarted)
        {
            Debug.LogWarning(
                "DiceManager: Game has not started."
            );

            return;
        }

        if (!gameManager.IsTurnActive)
        {
            Debug.LogWarning(
                "DiceManager: There is no active turn."
            );

            return;
        }

        if (gameManager.WaitingForPlayerAction)
        {
            Debug.Log(
                "DiceManager: Player must resolve the " +
                "current action before rolling again."
            );

            return;
        }

        if (gameManager.CurrentPhase !=
            GamePhase.WaitingToRoll)
        {
            Debug.Log(
                "DiceManager: Player cannot roll during " +
                gameManager.CurrentPhase + "."
            );

            return;
        }

        BoardPlayer currentPlayer =
            gameManager.CurrentPlayer;

        if (currentPlayer == null)
        {
            Debug.LogWarning(
                "DiceManager: No current player."
            );

            return;
        }

        if (currentPlayer.IsMoving)
        {
            Debug.Log(
                "DiceManager: Player is still moving."
            );

            return;
        }

        if (currentPlayer.IsBankrupt)
        {
            Debug.LogWarning(
                "DiceManager: Current player is bankrupt."
            );

            return;
        }

        StartCoroutine(
            RollRoutine(currentPlayer)
        );
    }

    // ============================================================
    // ROLL ROUTINE
    // ============================================================

    private IEnumerator RollRoutine(
        BoardPlayer currentPlayer)
    {
        isRolling = true;

        // --------------------------------------------------------
        // ROLL
        // --------------------------------------------------------

        firstDie =
            Random.Range(1, 7);

        secondDie =
            Random.Range(1, 7);

        total =
            firstDie + secondDie;

        bool isDouble =
            firstDie == secondDie;

        string rollMessage =
            $"{currentPlayer.PlayerName} ROLLED " +
            $"{firstDie} + {secondDie} = {total}";

        if (isDouble)
        {
            rollMessage += " • DOUBLE";
        }

        Debug.Log(rollMessage);

        GameNotificationUI.Show(
            rollMessage
        );

        // --------------------------------------------------------
        // SAVE RESULT
        // --------------------------------------------------------

        gameManager.SetLastDiceRoll(total);

        // --------------------------------------------------------
        // HANDLE DOUBLES
        // --------------------------------------------------------

        if (isDouble)
        {
            gameManager.RegisterDouble();

            GameNotificationUI.Show(
                $"{currentPlayer.PlayerName} ROLLED A DOUBLE"
            );
        }
        else
        {
            gameManager.ResetConsecutiveDoubles();
        }

        // --------------------------------------------------------
        // THREE DOUBLES = JAIL
        // --------------------------------------------------------

        if (gameManager.ConsecutiveDoubles >= 3)
        {
            Debug.Log(
                $"{currentPlayer.PlayerName} rolled " +
                "doubles three times and goes to Jail."
            );

            GameNotificationUI.Show(
                $"{currentPlayer.PlayerName} ROLLED THREE DOUBLES • JAIL"
            );

            currentPlayer.SendToJail();

            gameManager.ResetConsecutiveDoubles();

            isRolling = false;

            gameManager.EndTurn();

            yield break;
        }

        // --------------------------------------------------------
        // MOVE
        // --------------------------------------------------------

        currentPlayer.MoveBySteps(
            total
        );

        // --------------------------------------------------------
        // WAIT FOR MOVEMENT
        // --------------------------------------------------------

        yield return new WaitUntil(
            () => !currentPlayer.IsMoving
        );

        // --------------------------------------------------------
        // MOVEMENT FINISHED
        // --------------------------------------------------------

        isRolling = false;

        // BoardPlayer handles landing resolution.
    }

    // ============================================================
    // KEYBOARD TESTING
    // ============================================================

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RollDice();
        }
    }
}