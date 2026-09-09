using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceManager : MonoBehaviour
{
    [Header("Game Manager")]
    [SerializeField] private GameManager gameManager;

    [Header("Dice UI")]
    [SerializeField] private Image firstDieImage;
    [SerializeField] private Image secondDieImage;

    [Header("Result Text")]
    [SerializeField] private TMP_Text lastRollText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text totalText;

    [Header("Dice Sprites")]
    [SerializeField] private Sprite[] diceSprites = new Sprite[6];

    [Header("Dice Animation")]
    [SerializeField] private float rollAnimationDuration = 0.65f;
    [SerializeField] private float spriteChangeInterval = 0.08f;

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

        // Make sure the dice images are visible.
        if (firstDieImage != null)
            firstDieImage.enabled = true;

        if (secondDieImage != null)
            secondDieImage.enabled = true;
    }

    private void FindGameManager()
    {
        if (gameManager == null)
        {
            gameManager = FindFirstObjectByType<GameManager>();
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
                "DiceManager: Player must resolve the current action before rolling again."
            );
            return;
        }

        if (gameManager.CurrentPhase != GamePhase.WaitingToRoll)
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

        if (currentPlayer.IsInJail)
        {
            currentPlayer.DecreaseJailTurn();

            if (currentPlayer.IsInJail)
            {
                GameNotificationUI.Show(
                    $"{currentPlayer.PlayerName.ToUpperInvariant()} " +
                    $"IS IN JAIL â€” " +
                    $"{currentPlayer.JailTurnsRemaining} TURN(S) REMAINING"
                );
            }
            else
            {
                GameNotificationUI.Show(
                    $"{currentPlayer.PlayerName.ToUpperInvariant()} " +
                    "IS RELEASED FROM JAIL"
                );
            }

            gameManager.EndTurn();
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
        // ANIMATION
        // --------------------------------------------------------

        float elapsed = 0f;

        while (elapsed < rollAnimationDuration)
        {
            int randomFirst =
                Random.Range(1, 7);

            int randomSecond =
                Random.Range(1, 7);

            SetDieSprite(
                firstDieImage,
                randomFirst
            );

            SetDieSprite(
                secondDieImage,
                randomSecond
            );

            elapsed += spriteChangeInterval;

            yield return new WaitForSeconds(
                spriteChangeInterval
            );
        }

        // --------------------------------------------------------
        // FINAL RESULT
        // --------------------------------------------------------

        firstDie =
            Random.Range(1, 7);

        secondDie =
            Random.Range(1, 7);

        total =
            firstDie + secondDie;

        // --------------------------------------------------------
        // DISPLAY FINAL DICE
        // --------------------------------------------------------

        SetDieSprite(
            firstDieImage,
            firstDie
        );

        SetDieSprite(
            secondDieImage,
            secondDie
        );

        // --------------------------------------------------------
        // UPDATE RESULT TEXT
        // --------------------------------------------------------

        UpdateResultText();

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

        currentPlayer.MoveBySteps(total);

        // --------------------------------------------------------
        // WAIT FOR MOVEMENT
        // --------------------------------------------------------

        yield return new WaitUntil(
            () => !currentPlayer.IsMoving
        );

        // --------------------------------------------------------
        // FINISHED
        // --------------------------------------------------------

        isRolling = false;
    }

    // ============================================================
    // DISPLAY DICE SPRITE
    // ============================================================

    private void SetDieSprite(
        Image dieImage,
        int value)
    {
        if (dieImage == null)
            return;

        // IMPORTANT:
        // Always make the Image visible.
        dieImage.enabled = true;

        if (value < 1 || value > 6)
        {
            Debug.LogError(
                $"DiceManager: Invalid dice value {value}."
            );
            return;
        }

        int spriteIndex = value - 1;

        if (diceSprites == null ||
            diceSprites.Length < 6)
        {
            Debug.LogError(
                "DiceManager: Six dice sprites are required."
            );
            return;
        }

        Sprite sprite =
            diceSprites[spriteIndex];

        if (sprite == null)
        {
            Debug.LogError(
                $"DiceManager: Dice sprite for value {value} is missing."
            );
            return;
        }

        dieImage.sprite = sprite;
    }

    // ============================================================
    // RESULT TEXT
    // ============================================================

    private void UpdateResultText()
    {
        if (resultText != null)
        {
            resultText.text =
                $"{firstDie} + {secondDie}";
        }

        if (totalText != null)
        {
            totalText.text =
                $"Total: {total}";
        }

        if (lastRollText != null)
        {
            lastRollText.text =
                "Last Roll";
        }
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
