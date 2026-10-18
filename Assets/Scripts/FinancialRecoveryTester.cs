using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Development-only tester for FinancialRecoveryManager.
///
/// Attach this component to the GameManager object and use the custom
/// Inspector buttons to prepare and run controlled recovery scenarios.
///
/// This tester does NOT replace the real recovery system.
/// It only creates a controlled test state and then calls the same
/// FinancialRecoveryManager API used by the game.
///
/// Recommended test order:
/// 1. CashZeroWithMortgageableProperty
/// 2. MultipleMortgageRecovery
/// 3. RentDebtToPlayer
/// 4. BankDebt
/// 5. BankruptcyToCreditor
/// 6. BankruptcyToBank
/// </summary>
public class FinancialRecoveryTester : MonoBehaviour
{
    public enum TestScenario
    {
        CashZeroWithMortgageableProperty,
        RentDebtToPlayer,
        BankDebt,
        BankruptcyToCreditor,
        BankruptcyToBank,
        MultipleMortgageRecovery
    }

    // ============================================================
    // TEST SETUP
    // ============================================================

    [Header("TEST SETUP")]
    [SerializeField]
    private TestScenario scenario =
        TestScenario.CashZeroWithMortgageableProperty;

    [SerializeField]
    private int debtorPlayerNumber = 1;

    [SerializeField]
    private int creditorPlayerNumber = 2;

    [SerializeField]
    private long debtAmount = 50L;

    // ============================================================
    // PROPERTY SELECTION
    // ============================================================

    [Header("PROPERTY SELECTION")]

    [Tooltip(
        "Optional. Leave empty to automatically choose a clean property."
    )]
    [SerializeField]
    private BoardSpace selectedProperty;

    [Tooltip(
        "Optional second property used by MultipleMortgageRecovery."
    )]
    [SerializeField]
    private BoardSpace secondProperty;

    // ============================================================
    // STATUS
    // ============================================================

    [Header("RUNTIME STATUS")]

    [SerializeField]
    private bool testRunning;

    [SerializeField]
    private string lastResult =
        "Not started.";

    [Header("BATCH AUTOMATION")]
    [SerializeField]
    private bool batchTestRunning;

    [SerializeField]
    private string batchTestSummary =
        "Batch suite has not run.";

    private BatchSnapshot activeBatchSnapshot;
    private BoardPlayer batchDebtor;
    private BoardPlayer batchCreditor;
    private BoardSpace batchPrimaryProperty;
    private BoardSpace batchSecondaryProperty;
    private long batchExpectedDebt;
    private int batchCreditorBalanceBefore;
    private readonly List<string> batchResults =
        new List<string>();

    public bool BatchTestRunning =>
        batchTestRunning;

    public string BatchTestSummary =>
        batchTestSummary;

    public static bool IsBatchTestRunning
    {
        get;
        private set;
    }

    // ============================================================
    // INTERNAL STATE
    // ============================================================

    private readonly List<BoardSpace> preparedProperties =
        new List<BoardSpace>();

    private BoardPlayer debtor;
    private BoardPlayer creditor;

    private GameManager gameManager;
    private FinancialRecoveryManager recoveryManager;

    public TestScenario Scenario =>
        scenario;

    public bool TestRunning =>
        testRunning;

    public string LastResult =>
        lastResult;

    public BoardPlayer Debtor =>
        debtor;

    public BoardPlayer Creditor =>
        creditor;

    public FinancialRecoveryManager RecoveryManager =>
        recoveryManager;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        FindSystems();
    }

    private void OnDisable()
    {
        testRunning = false;

        if (batchTestRunning)
        {
            RestoreBatchEnvironment();
            batchTestRunning = false;
            batchTestSummary = "Batch run stopped; original scene state restored.";
        }

        IsBatchTestRunning = false;
    }

    // ============================================================
    // FIND SYSTEMS
    // ============================================================

    public void FindSystems()
    {
        gameManager =
            FindFirstObjectByType<GameManager>(
                FindObjectsInactive.Include
            );

        recoveryManager =
            FindFirstObjectByType<FinancialRecoveryManager>(
                FindObjectsInactive.Include
            );

        if (recoveryManager == null &&
            gameManager != null)
        {
            recoveryManager =
                gameManager.gameObject.AddComponent<
                    FinancialRecoveryManager
                >();
        }
    }

    // ============================================================
    // PREPARE
    // ============================================================

    public void PrepareScenario()
    {
        FindSystems();

        if (gameManager == null)
        {
            SetResult(
                "FAILED: GameManager not found."
            );

            return;
        }

        if (recoveryManager == null)
        {
            SetResult(
                "FAILED: FinancialRecoveryManager not found."
            );

            return;
        }

        debtor =
            FindPlayerByNumber(
                debtorPlayerNumber
            );

        creditor =
            FindPlayerByNumber(
                creditorPlayerNumber
            );

        if (debtor == null)
        {
            SetResult(
                $"FAILED: Debtor Player " +
                $"{debtorPlayerNumber} not found."
            );

            return;
        }

        if (scenario != TestScenario.BankDebt &&
            scenario != TestScenario.BankruptcyToBank)
        {
            if (creditor == null)
            {
                SetResult(
                    $"FAILED: Creditor Player " +
                    $"{creditorPlayerNumber} not found."
                );

                return;
            }

            if (creditor == debtor)
            {
                SetResult(
                    "FAILED: Debtor and creditor must be different players."
                );

                return;
            }
        }

        // --------------------------------------------------------
        // CLEAN PREVIOUS TEST STATE
        // --------------------------------------------------------

        RestorePlayersAndPropertiesOnly();

        recoveryManager.ResetForNewGame();

        // --------------------------------------------------------
        // RESET TEST PLAYERS
        // --------------------------------------------------------

        debtor.ResetForNewGame();

        if (creditor != null &&
            creditor != debtor)
        {
            creditor.ResetForNewGame();
        }

        // --------------------------------------------------------
        // MAKE DEBTOR ACTIVE
        // --------------------------------------------------------

        if (!SetTestTurn(debtor))
        {
            SetResult(
                "FAILED: Could not make debtor the active test turn."
            );

            return;
        }

        // --------------------------------------------------------
        // FORCE $0 WITHOUT MARKING ELIMINATION
        //
        // ResetForNewGame() gives the player starting money.
        // Removing that balance through RemoveMoney() leaves the
        // player at $0 while preserving the new recovery rule.
        // --------------------------------------------------------

        if (debtor.Money > 0)
        {
            int currentMoney =
                debtor.Money;

            if (!debtor.RemoveMoney(currentMoney))
            {
                SetResult(
                    "FAILED: Could not reduce debtor cash to $0."
                );

                return;
            }
        }

        if (debtor.IsBankrupt)
        {
            SetResult(
                "FAILED: Debtor became eliminated while preparing " +
                "the test. Check the current BoardPlayer.RemoveMoney() " +
                "implementation."
            );

            return;
        }

        preparedProperties.Clear();

        // --------------------------------------------------------
        // SCENARIO
        // --------------------------------------------------------

        switch (scenario)
        {
            case TestScenario.CashZeroWithMortgageableProperty:

                PrepareSingleMortgageProperty();

                debtAmount =
                    Math.Max(
                        10L,
                        debtAmount
                    );

                break;

            case TestScenario.RentDebtToPlayer:

                PrepareSingleMortgageProperty();

                debtAmount =
                    Math.Max(
                        10L,
                        debtAmount
                    );

                break;

            case TestScenario.BankDebt:

                RemoveAllPropertiesFromDebtor();

                debtAmount =
                    Math.Max(
                        100L,
                        debtAmount
                    );

                break;

            case TestScenario.BankruptcyToCreditor:

                PrepareSingleMortgageProperty();

                debtAmount =
                    GetPreparedMortgageValue() + 1L;

                break;

            case TestScenario.BankruptcyToBank:

                PrepareSingleMortgageProperty();

                debtAmount =
                    GetPreparedMortgageValue() + 1L;

                break;

            case TestScenario.MultipleMortgageRecovery:

                PrepareMultipleMortgageProperties();

                ConfigureMultipleMortgageDebt();

                break;
        }

        SetResult(
            $"SCENARIO READY | " +
            $"{scenario} | " +
            $"Debtor={debtor.PlayerName} | " +
            $"Cash=${debtor.Money:N0}M | " +
            $"Debt=${debtAmount:N0}M | " +
            $"Eliminated={debtor.IsBankrupt}"
        );
    }

    // ============================================================
    // RUN
    // ============================================================

    public void RunRecovery()
    {
        FindSystems();

        if (gameManager == null ||
            recoveryManager == null)
        {
            SetResult(
                "FAILED: Required systems are missing."
            );

            return;
        }

        if (debtor == null)
        {
            SetResult(
                "FAILED: Prepare the scenario first."
            );

            return;
        }

        if (debtor.IsBankrupt)
        {
            SetResult(
                "FAILED: Debtor is already eliminated."
            );

            return;
        }

        BoardPlayer debtCreditor =
            scenario == TestScenario.BankDebt ||
            scenario == TestScenario.BankruptcyToBank
                ? null
                : creditor;

        string reason;

        switch (scenario)
        {
            case TestScenario.RentDebtToPlayer:

                reason =
                    "TEST RENT";

                break;

            case TestScenario.BankDebt:
            case TestScenario.BankruptcyToBank:

                reason =
                    "TEST BANK DEBT";

                break;

            default:

                reason =
                    "TEST DEBT";

                break;
        }

        bool started =
            recoveryManager.BeginDebtResolution(
                debtor,
                debtCreditor,
                debtAmount,
                reason
            );

        testRunning =
            started &&
            recoveryManager.IsRecoveryActive;

        if (!started)
        {
            SetResult(
                "FAILED: BeginDebtResolution returned false."
            );

            return;
        }

        SetResult(
            recoveryManager.IsRecoveryActive
                ? $"RECOVERY ACTIVE | " +
                  $"Remaining Debt=" +
                  $"${recoveryManager.RemainingDebt:N0}M"
                : "DEBT RESOLVED / PLAYER ADVANCED."
        );
    }

    // ============================================================
    // MORTGAGE
    // ============================================================

    public void MortgageSelectedProperty()
    {
        FindSystems();

        if (recoveryManager == null ||
            !recoveryManager.IsRecoveryActive)
        {
            SetResult(
                "No active financial recovery."
            );

            return;
        }

        BoardSpace property =
            selectedProperty;

        if (property == null ||
            property.Owner != debtor ||
            !property.CanMortgage())
        {
            property = null;

            List<BoardSpace> options =
                recoveryManager.GetMortgageableProperties(
                    debtor
                );

            if (options.Count > 0)
            {
                property =
                    options[0];
            }
        }

        if (property == null)
        {
            SetResult(
                "No mortgageable property is available."
            );

            return;
        }

        bool success =
            property.Mortgage();

        if (!success)
        {
            SetResult(
                $"Mortgage failed: {property.SpaceName}."
            );

            return;
        }

        SetResult(
            $"MORTGAGED | " +
            $"{property.SpaceName} | " +
            $"Cash=${debtor.Money:N0}M"
        );
    }

    // ============================================================
    // MANUAL ELIMINATION
    // ============================================================

    public void EliminateCurrentDebtor()
    {
        FindSystems();

        if (recoveryManager == null ||
            debtor == null)
        {
            SetResult(
                "No debtor/recovery manager available."
            );

            return;
        }

        BoardPlayer debtCreditor =
            scenario == TestScenario.BankruptcyToBank
                ? null
                : creditor;

        recoveryManager.EliminateNow(
            debtor,
            debtCreditor,
            "MANUAL TEST ELIMINATION"
        );

        testRunning = false;

        SetResult(
            $"MANUAL ELIMINATION | " +
            $"{debtor.PlayerName}"
        );
    }

    // ============================================================
    // RESTORE
    // ============================================================

    public void RestoreTestState()
    {
        FindSystems();

        if (recoveryManager != null)
        {
            recoveryManager.ResetForNewGame();
        }

        RestorePlayersAndPropertiesOnly();

        testRunning = false;

        SetResult(
            "Test state restored."
        );
    }

    // ============================================================
    // LOG CURRENT STATE
    // ============================================================

    public void LogCurrentState()
    {
        FindSystems();

        if (debtor == null)
        {
            SetResult(
                "No debtor selected."
            );

            return;
        }

        int mortgageValue = 0;

        int mortgageableCount = 0;

        if (recoveryManager != null)
        {
            mortgageValue =
                recoveryManager.GetTotalMortgageValue(
                    debtor
                );

            mortgageableCount =
                recoveryManager
                    .GetMortgageableProperties(
                        debtor
                    )
                    .Count;
        }

        long remainingDebt =
            recoveryManager != null
                ? recoveryManager.RemainingDebt
                : 0L;

        bool recoveryActive =
            recoveryManager != null &&
            recoveryManager.IsRecoveryActive;

        SetResult(
            $"Debtor={debtor.PlayerName} | " +
            $"Cash=${debtor.Money:N0}M | " +
            $"Mortgageable={mortgageableCount} | " +
            $"RecoveryValue=${mortgageValue:N0}M | " +
            $"RecoveryActive={recoveryActive} | " +
            $"RemainingDebt=${remainingDebt:N0}M | " +
            $"Eliminated={debtor.IsBankrupt}"
        );
    }

    // ============================================================
    // PREPARE SINGLE PROPERTY
    // ============================================================

    private void PrepareSingleMortgageProperty()
    {
        BoardSpace property =
            selectedProperty;

        if (property == null ||
            property.Owner != null)
        {
            property =
                FindFreshProperty();
        }

        if (property == null)
        {
            SetResult(
                "FAILED: No suitable property found."
            );

            return;
        }

        property.ResetProperty();

        property.SetOwner(
            debtor
        );

        preparedProperties.Add(
            property
        );

        selectedProperty =
            property;
    }

    // ============================================================
    // PREPARE TWO PROPERTIES
    // ============================================================

    private void PrepareMultipleMortgageProperties()
    {
        BoardSpace first =
            selectedProperty;

        if (first == null ||
            first.Owner != null)
        {
            first =
                FindFreshProperty();
        }

        if (first == null)
        {
            SetResult(
                "FAILED: Could not find first property."
            );

            return;
        }

        first.ResetProperty();

        first.SetOwner(
            debtor
        );

        preparedProperties.Add(
            first
        );

        BoardSpace second =
            secondProperty;

        if (second == null ||
            second == first ||
            second.Owner != null)
        {
            second =
                FindFreshProperty(
                    first
                );
        }

        if (second == null)
        {
            SetResult(
                "FAILED: Could not find second property."
            );

            return;
        }

        second.ResetProperty();

        second.SetOwner(
            debtor
        );

        preparedProperties.Add(
            second
        );
    }

    private void ConfigureMultipleMortgageDebt()
    {
        int total =
            GetPreparedMortgageValue();

        if (total <= 0)
        {
            debtAmount = 10L;
            return;
        }

        // Force the normal multiple-property test to require
        // more than one mortgage whenever possible.
        int largestValue = 0;

        foreach (BoardSpace property in preparedProperties)
        {
            if (property == null)
                continue;

            largestValue =
                Math.Max(
                    largestValue,
                    property.MortgageValue
                );
        }

        long required =
            (long)total -
            largestValue +
            1L;

        debtAmount =
            Math.Max(
                1L,
                required
            );
    }

    // ============================================================
    // FIND PROPERTY
    // ============================================================

    private BoardSpace FindFreshProperty(
        BoardSpace excluded = null)
    {
        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space == null ||
                space == excluded)
            {
                continue;
            }

            if (!space.IsProperty)
                continue;

            if (space.PurchasePrice <= 0)
                continue;

            if (space.Owner != null)
                continue;

            if (space.MortgageValue <= 0)
                continue;

            return space;
        }

        return null;
    }

    // ============================================================
    // REMOVE DEBTOR PROPERTIES
    // ============================================================

    private void RemoveAllPropertiesFromDebtor()
    {
        if (debtor == null)
            return;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BoardSpace space in spaces)
        {
            if (space != null &&
                space.Owner == debtor)
            {
                space.ResetProperty();
            }
        }
    }

    // ============================================================
    // RECOVERY VALUE
    // ============================================================

    private int GetPreparedMortgageValue()
    {
        int total = 0;

        foreach (BoardSpace property
                 in preparedProperties)
        {
            if (property == null)
                continue;

            total +=
                Math.Max(
                    0,
                    property.MortgageValue
                );
        }

        return total;
    }

    // ============================================================
    // RESTORE PREPARED STATE
    // ============================================================

    private void RestorePlayersAndPropertiesOnly()
    {
        foreach (BoardSpace property
                 in preparedProperties)
        {
            if (property == null)
                continue;

            property.ResetProperty();
        }

        preparedProperties.Clear();

        if (debtor != null)
        {
            debtor.ResetForNewGame();
        }

        if (creditor != null &&
            creditor != debtor)
        {
            creditor.ResetForNewGame();
        }
    }

    // ============================================================
    // PLAYER LOOKUP
    // ============================================================

    private BoardPlayer FindPlayerByNumber(
        int playerNumber)
    {
        BoardPlayer[] players =
            FindObjectsByType<BoardPlayer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (BoardPlayer player in players)
        {
            if (player != null &&
                player.PlayerNumber == playerNumber)
            {
                return player;
            }
        }

        return null;
    }

    // ============================================================
    // TEST TURN
    // ============================================================

    private bool SetTestTurn(
        BoardPlayer player)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD

        if (gameManager == null ||
            player == null)
        {
            return false;
        }

        return gameManager.BeginRentTesterTurn(
            player
        );

#else

        Debug.LogWarning(
            "FinancialRecoveryTester requires " +
            "Unity Editor or a Development Build."
        );

        return false;

#endif
    }

    // ============================================================
    // ONE-CLICK BATCH AUTOMATION
    // ============================================================

    public void RunAllAutomatedScenarios()
    {
        if (!Application.isPlaying)
        {
            SetResult("START PLAY MODE BEFORE RUNNING THE BATCH TESTS.");
            return;
        }

        if (batchTestRunning)
        {
            SetResult("BATCH TESTS ARE ALREADY RUNNING.");
            return;
        }

        FindSystems();

        if (gameManager == null || recoveryManager == null)
        {
            SetResult("BATCH FAILED: GameManager or FinancialRecoveryManager is missing.");
            return;
        }

        if (gameManager.GameOver ||
            recoveryManager.IsRecoveryActive ||
            gameManager.WaitingForPlayerAction)
        {
            SetResult("BATCH NOT STARTED: finish the current action and ensure no recovery is active.");
            return;
        }

        BoardPlayer[] scenePlayers = FindObjectsByType<BoardPlayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        if (scenePlayers.Length < 2 ||
            gameManager.Players == null ||
            gameManager.Players.Length < 2 ||
            FindPlayerByNumber(debtorPlayerNumber) == null ||
            FindPlayerByNumber(creditorPlayerNumber) == null ||
            debtorPlayerNumber == creditorPlayerNumber)
        {
            SetResult("BATCH FAILED: Configure two different existing players as debtor and creditor.");
            return;
        }

        foreach (BoardPlayer player in scenePlayers)
        {
            if (player != null && player.IsMoving)
            {
                SetResult("BATCH NOT STARTED: wait until all players finish moving.");
                return;
            }
        }

        batchResults.Clear();
        batchTestRunning = true;
        batchTestSummary = "Running six scenarios...";

        try
        {
            activeBatchSnapshot = BatchSnapshot.Capture(
                this,
                gameManager,
                recoveryManager
            );
        }
        catch (Exception exception)
        {
            batchTestRunning = false;
            activeBatchSnapshot = null;
            batchTestSummary = "Batch not started: scene snapshot failed.";
            SetResult(
                "BATCH NOT STARTED: could not snapshot scene state. " +
                exception.GetType().Name + ": " + exception.Message
            );
            return;
        }

        IsBatchTestRunning = true;
        batchCoroutine = StartCoroutine(RunAllAutomatedScenariosRoutine());
    }

    private Coroutine batchCoroutine;

    private IEnumerator RunAllAutomatedScenariosRoutine()
    {
        string[] names =
        {
            "CashZeroWithMortgageableProperty",
            "RentDebtToPlayer",
            "BankDebt",
            "BankruptcyToCreditor",
            "BankruptcyToBank",
            "MultipleMortgageRecovery"
        };

        TestScenario[] scenarios =
        {
            TestScenario.CashZeroWithMortgageableProperty,
            TestScenario.RentDebtToPlayer,
            TestScenario.BankDebt,
            TestScenario.BankruptcyToCreditor,
            TestScenario.BankruptcyToBank,
            TestScenario.MultipleMortgageRecovery
        };

        bool completed = false;

        try
        {
            for (int i = 0; i < scenarios.Length; i++)
            {
                string startError = null;

                try
                {
                    startError = PrepareAndStartBatchScenario(scenarios[i]);
                }
                catch (Exception exception)
                {
                    startError = exception.GetType().Name + ": " + exception.Message;
                }

                if (!string.IsNullOrEmpty(startError))
                {
                    RecordBatchResult(names[i], false, startError);
                    yield return null;
                    continue;
                }

                // Allow FinancialRecoveryManager.Update to complete the
                // normal debt resolution path before asserting outcomes.
                yield return null;

                string result = null;
                bool waitForSecondMortgageResult = false;

                try
                {
                    if (scenarios[i] == TestScenario.MultipleMortgageRecovery)
                    {
                        result = ValidateFirstMultipleMortgage();
                        if (string.IsNullOrEmpty(result))
                        {
                            MortgageSelectedProperty();
                            if (lastResult == null ||
                                !lastResult.StartsWith("MORTGAGED", StringComparison.Ordinal))
                            {
                                result = "Second property mortgage failed: " + lastResult;
                            }
                            else
                            {
                                waitForSecondMortgageResult = true;
                            }
                        }
                    }
                    else
                    {
                        result = ValidateBatchScenario(scenarios[i]);
                    }
                }
                catch (Exception exception)
                {
                    result = exception.GetType().Name + ": " + exception.Message;
                }

                if (waitForSecondMortgageResult && string.IsNullOrEmpty(result))
                {
                    yield return null;

                    try
                    {
                        result = ValidateCompletedMultipleMortgage();
                    }
                    catch (Exception exception)
                    {
                        result = exception.GetType().Name + ": " + exception.Message;
                    }
                }

                RecordBatchResult(
                    names[i],
                    string.IsNullOrEmpty(result),
                    string.IsNullOrEmpty(result) ? "Expected result confirmed." : result
                );

                yield return null;
            }

            completed = true;
        }
        finally
        {
            List<string> completedResults =
                new List<string>(batchResults);

            bool originalSceneRestored = RestoreBatchEnvironment();
            IsBatchTestRunning = false;

            batchResults.Clear();
            batchResults.AddRange(completedResults);

            batchTestRunning = false;
            batchCoroutine = null;

            int passed = 0;
            int failed = 0;

            foreach (string result in batchResults)
            {
                if (result.StartsWith("PASS", StringComparison.Ordinal))
                    passed++;
                else
                    failed++;
            }

            if (!completed && failed == 0)
            {
                batchResults.Add("FAIL | BatchInterrupted | Batch was interrupted.");
                failed++;
            }

            batchTestSummary =
                "FINANCIAL RECOVERY BATCH\n" +
                "PASSED: " + passed + "\n" +
                "FAILED: " + failed + "\n" +
                (originalSceneRestored
                    ? "Original scene state restored."
                    : "WARNING: state restore reported an error. Check Console before continuing.");

            SetResult(batchTestSummary);
            Debug.Log(batchTestSummary + "\n" + string.Join("\n", batchResults), this);
        }
    }

    private string PrepareAndStartBatchScenario(TestScenario nextScenario)
    {
        // Clear the board only inside a captured temporary test state.
        // BatchSnapshot restores each property's original owner, buildings,
        // mortgage, player balance, turn state, and token position afterward.
        BoardSpace[] spaces = FindObjectsByType<BoardSpace>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (BoardSpace space in spaces)
        {
            if (space != null)
                space.ResetProperty();
        }

        BoardPlayer[] players = FindObjectsByType<BoardPlayer>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None
        );

        foreach (BoardPlayer player in players)
        {
            if (player != null)
                player.ResetForNewGame();
        }

        recoveryManager.ResetForNewGame();

        SetPrivateField(gameManager, "gameOver", false);
        SetPrivateField(gameManager, "endGameVoteInProgress", false);
        SetPrivateField(gameManager, "gameStarted", true);
        SetPrivateField(gameManager, "isTurnActive", false);
        SetPrivateField(gameManager, "waitingForPlayerAction", false);
        SetPrivateField(gameManager, "lastDiceRoll", 0);
        SetPrivateField(gameManager, "consecutiveDoubles", 0);
        SetPrivateField(gameManager, "currentPhase", GamePhase.WaitingToRoll);

        scenario = nextScenario;
        debtAmount =
            nextScenario == TestScenario.BankDebt
                ? 100L
                : 10L;

        PrepareScenario();

        if (lastResult == null ||
            !lastResult.StartsWith("SCENARIO READY", StringComparison.Ordinal))
        {
            return "Scenario setup failed: " + lastResult;
        }

        batchDebtor = debtor;
        batchCreditor = creditor;
        batchPrimaryProperty = GetPrivateField<BoardSpace>(this, "selectedProperty");
        batchSecondaryProperty = null;
        batchExpectedDebt = debtAmount;
        batchCreditorBalanceBefore =
            batchCreditor != null
                ? batchCreditor.Money
                : 0;

        if (nextScenario == TestScenario.MultipleMortgageRecovery)
        {
            foreach (BoardSpace space in spaces)
            {
                if (space == null ||
                    space.Owner != batchDebtor ||
                    space == batchPrimaryProperty)
                {
                    continue;
                }

                batchSecondaryProperty = space;
                break;
            }

            if (batchPrimaryProperty == null ||
                batchSecondaryProperty == null)
            {
                return "Multiple-mortgage setup did not produce two properties.";
            }

            int largestMortgage = Math.Max(
                batchPrimaryProperty.MortgageValue,
                batchSecondaryProperty.MortgageValue
            );

            long twoMortgageDebt = (long)largestMortgage + 1L;
            long totalMortgage =
                (long)batchPrimaryProperty.MortgageValue +
                batchSecondaryProperty.MortgageValue;

            if (totalMortgage <= twoMortgageDebt)
            {
                return "The selected properties cannot prove multi-mortgage recovery.";
            }

            debtAmount = twoMortgageDebt;
            batchExpectedDebt = twoMortgageDebt;
        }

        RunRecovery();

        if (lastResult == null ||
            lastResult.StartsWith("FAILED", StringComparison.Ordinal))
        {
            return "Recovery did not start: " + lastResult;
        }

        if (nextScenario == TestScenario.CashZeroWithMortgageableProperty ||
            nextScenario == TestScenario.RentDebtToPlayer)
        {
            if (!recoveryManager.IsRecovering(batchDebtor))
                return "Expected active recovery with a mortgageable property, but recovery did not remain active.";

            MortgageSelectedProperty();

            if (lastResult == null ||
                !lastResult.StartsWith("MORTGAGED", StringComparison.Ordinal))
            {
                return "Could not mortgage the prepared property: " + lastResult;
            }
        }
        else if (nextScenario == TestScenario.MultipleMortgageRecovery)
        {
            if (!recoveryManager.IsRecovering(batchDebtor))
                return "Expected recovery to remain active before either property was mortgaged.";

            MortgageSelectedProperty();

            if (lastResult == null ||
                !lastResult.StartsWith("MORTGAGED", StringComparison.Ordinal))
            {
                return "First property mortgage failed: " + lastResult;
            }
        }

        return null;
    }

    private string ValidateBatchScenario(TestScenario checkedScenario)
    {
        if (batchDebtor == null)
            return "Debtor reference was lost during the test.";

        switch (checkedScenario)
        {
            case TestScenario.CashZeroWithMortgageableProperty:
                if (batchDebtor.IsBankrupt || recoveryManager.IsRecoveryActive)
                    return "Debtor was eliminated or recovery remained active after one mortgage.";
                if (batchPrimaryProperty == null || !batchPrimaryProperty.IsMortgaged)
                    return "Property did not remain mortgaged after recovery.";
                if (batchCreditor == null ||
                    batchCreditor.Money != batchCreditorBalanceBefore + batchExpectedDebt)
                    return "Debt money was not transferred to the creditor correctly.";
                return null;

            case TestScenario.RentDebtToPlayer:
                if (batchDebtor.IsBankrupt || recoveryManager.IsRecoveryActive)
                    return "Debtor was eliminated or rent recovery remained active after mortgage.";
                if (batchPrimaryProperty == null || !batchPrimaryProperty.IsMortgaged)
                    return "Rent debtor's property was not mortgaged.";
                if (batchCreditor == null ||
                    batchCreditor.Money != batchCreditorBalanceBefore + batchExpectedDebt)
                    return "Rent debt was not credited to the property owner.";
                return null;

            case TestScenario.BankDebt:
                if (!batchDebtor.IsBankrupt)
                    return "Bank debt should eliminate a player with no cash or assets.";
                if (recoveryManager.IsRecoveryActive)
                    return "Recovery state remained active after bank-debt elimination.";
                foreach (BoardSpace space in FindObjectsByType<BoardSpace>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None))
                {
                    if (space != null && space.Owner == batchDebtor)
                        return "A property remained assigned to the eliminated debtor.";
                }
                return null;

            case TestScenario.BankruptcyToCreditor:
                if (!batchDebtor.IsBankrupt)
                    return "Debtor was not eliminated despite debt exceeding all recoverable assets.";
                if (recoveryManager.IsRecoveryActive)
                    return "Recovery state remained active after creditor bankruptcy settlement.";
                if (batchPrimaryProperty == null ||
                    batchPrimaryProperty.Owner != batchCreditor)
                    return "Property was not transferred to the creditor.";
                if (batchPrimaryProperty.Houses != 0 ||
                    batchPrimaryProperty.HasHotel ||
                    batchPrimaryProperty.IsMortgaged)
                    return "Transferred property buildings or mortgage state changed unexpectedly.";
                return null;

            case TestScenario.BankruptcyToBank:
                if (!batchDebtor.IsBankrupt)
                    return "Debtor was not eliminated for unrecoverable bank debt.";
                if (recoveryManager.IsRecoveryActive)
                    return "Recovery state remained active after bank bankruptcy settlement.";
                if (batchPrimaryProperty == null ||
                    batchPrimaryProperty.Owner != null ||
                    batchPrimaryProperty.IsOwned ||
                    batchPrimaryProperty.Houses != 0 ||
                    batchPrimaryProperty.HasHotel ||
                    batchPrimaryProperty.IsMortgaged)
                    return "Bank-owned property was not reset correctly.";
                return null;

            default:
                return "Unexpected scenario in simple validation.";
        }
    }

    private string ValidateFirstMultipleMortgage()
    {
        if (batchDebtor == null ||
            !batchDebtor.IsActive ||
            batchPrimaryProperty == null ||
            batchSecondaryProperty == null)
        {
            return "Player/property state became invalid before the second mortgage.";
        }

        if (!batchPrimaryProperty.IsMortgaged)
            return "First property did not become mortgaged.";

        if (batchDebtor.IsBankrupt || !recoveryManager.IsRecovering(batchDebtor))
            return "Regression: player was eliminated or recovery ended before a second mortgage could be used.";

        return null;
    }

    private string ValidateCompletedMultipleMortgage()
    {
        if (batchDebtor == null || batchDebtor.IsBankrupt)
            return "Player was eliminated during a valid multi-mortgage recovery.";

        if (recoveryManager.IsRecoveryActive)
            return "Recovery remained active after two property mortgages.";

        if (batchPrimaryProperty == null ||
            batchSecondaryProperty == null ||
            !batchPrimaryProperty.IsMortgaged ||
            !batchSecondaryProperty.IsMortgaged)
        {
            return "Both properties were not mortgaged during multi-mortgage recovery.";
        }

        return null;
    }

    private void RecordBatchResult(string name, bool passed, string detail)
    {
        string result =
            (passed ? "PASS" : "FAIL") + " | " + name + " | " + detail;

        batchResults.Add(result);
        Debug.Log(result, this);
    }

    private bool RestoreBatchEnvironment()
    {
        BatchSnapshot snapshot = activeBatchSnapshot;
        bool restored = true;

        activeBatchSnapshot = null;

        if (snapshot != null)
        {
            try
            {
                snapshot.Restore();
            }
            catch (Exception exception)
            {
                restored = false;
                Debug.LogError(
                    "FinancialRecoveryTester: Failed to restore all scene state after batch tests. " +
                    exception
                );
            }
        }

        return restored;
    }

    private static T GetPrivateField<T>(object target, string fieldName)
    {
        if (target == null)
            return default(T);

        FieldInfo field = FindField(target.GetType(), fieldName);
        if (field == null)
            return default(T);

        object value = field.GetValue(target);
        return value is T ? (T)value : default(T);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        if (target == null)
            throw new ArgumentNullException(nameof(target));

        FieldInfo field = FindField(target.GetType(), fieldName);
        if (field == null)
            throw new MissingFieldException(target.GetType().Name, fieldName);

        field.SetValue(target, value);
    }

    private static FieldInfo FindField(Type type, string fieldName)
    {
        while (type != null &&
               type != typeof(MonoBehaviour) &&
               type != typeof(Behaviour) &&
               type != typeof(Component) &&
               type != typeof(UnityEngine.Object))
        {
            FieldInfo field = type.GetField(
                fieldName,
                BindingFlags.Instance |
                BindingFlags.Public |
                BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly
            );

            if (field != null)
                return field;

            type = type.BaseType;
        }

        return null;
    }

    private sealed class BatchSnapshot
    {
        private readonly List<ComponentFieldSnapshot> componentStates =
            new List<ComponentFieldSnapshot>();

        private readonly List<PlayerVisualSnapshot> playerVisuals =
            new List<PlayerVisualSnapshot>();

        private readonly List<BoardSpace> spaces =
            new List<BoardSpace>();

        public static BatchSnapshot Capture(
            FinancialRecoveryTester tester,
            GameManager manager,
            FinancialRecoveryManager recovery)
        {
            BatchSnapshot snapshot = new BatchSnapshot();
            HashSet<Component> captured = new HashSet<Component>();

            snapshot.CaptureComponent(manager, captured);
            snapshot.CaptureComponent(recovery, captured);
            snapshot.CaptureComponent(tester, captured);

            BoardPlayer[] players = FindObjectsByType<BoardPlayer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (BoardPlayer player in players)
            {
                snapshot.CaptureComponent(player, captured);
                if (player != null)
                    snapshot.playerVisuals.Add(new PlayerVisualSnapshot(player));
            }

            BoardSpace[] boardSpaces = FindObjectsByType<BoardSpace>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

            foreach (BoardSpace space in boardSpaces)
            {
                snapshot.CaptureComponent(space, captured);
                if (space != null)
                    snapshot.spaces.Add(space);
            }

            CommunityChestCardManager[] chestManagers =
                FindObjectsByType<CommunityChestCardManager>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None
                );

            foreach (CommunityChestCardManager chestManager in chestManagers)
                snapshot.CaptureComponent(chestManager, captured);

            return snapshot;
        }

        private void CaptureComponent(
            Component component,
            HashSet<Component> captured)
        {
            if (component == null || !captured.Add(component))
                return;

            componentStates.Add(new ComponentFieldSnapshot(component));
        }

        public void Restore()
        {
            foreach (ComponentFieldSnapshot state in componentStates)
                state.Restore();

            foreach (PlayerVisualSnapshot state in playerVisuals)
                state.Restore();

            foreach (BoardSpace space in spaces)
            {
                if (space == null)
                    continue;

                space.RefreshOwnershipMarker();
                space.RefreshBuildingVisual();
            }
        }
    }

    private sealed class ComponentFieldSnapshot
    {
        private readonly Component target;
        private readonly List<FieldValueSnapshot> fields =
            new List<FieldValueSnapshot>();

        public ComponentFieldSnapshot(Component target)
        {
            this.target = target;

            Type type = target.GetType();
            while (type != null &&
                   type != typeof(MonoBehaviour) &&
                   type != typeof(Behaviour) &&
                   type != typeof(Component) &&
                   type != typeof(UnityEngine.Object))
            {
                FieldInfo[] declaredFields = type.GetFields(
                    BindingFlags.Instance |
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.DeclaredOnly
                );

                foreach (FieldInfo field in declaredFields)
                {
                    if (field.IsStatic)
                        continue;

                    object value = field.GetValue(target);
                    fields.Add(new FieldValueSnapshot(
                        field,
                        SnapshotValue(value)
                    ));
                }

                type = type.BaseType;
            }
        }

        public void Restore()
        {
            if (target == null)
                return;

            foreach (FieldValueSnapshot state in fields)
            {
                if (state.Field.IsInitOnly &&
                    IsMutableCollection(state.Value))
                {
                    RestoreCollectionInPlace(
                        state.Field.GetValue(target),
                        state.Value
                    );
                }
                else
                {
                    state.Field.SetValue(
                        target,
                        SnapshotValue(state.Value)
                    );
                }
            }
        }
    }

    private sealed class FieldValueSnapshot
    {
        public readonly FieldInfo Field;
        public readonly object Value;

        public FieldValueSnapshot(FieldInfo field, object value)
        {
            Field = field;
            Value = value;
        }
    }

    private sealed class PlayerVisualSnapshot
    {
        private readonly BoardPlayer player;
        private readonly Transform parent;
        private readonly int siblingIndex;
        private readonly Vector3 position;
        private readonly Quaternion rotation;
        private readonly Vector3 localScale;
        private readonly Image tokenImage;
        private readonly bool tokenImageEnabled;

        public PlayerVisualSnapshot(BoardPlayer player)
        {
            this.player = player;
            parent = player.transform.parent;
            siblingIndex = player.transform.GetSiblingIndex();
            position = player.transform.position;
            rotation = player.transform.rotation;
            localScale = player.transform.localScale;

            tokenImage = GetPrivateField<Image>(player, "tokenImage");
            if (tokenImage == null)
                tokenImage = player.GetComponent<Image>();

            tokenImageEnabled = tokenImage != null && tokenImage.enabled;
        }

        public void Restore()
        {
            if (player == null)
                return;

            player.transform.SetParent(parent, true);
            player.transform.position = position;
            player.transform.rotation = rotation;
            player.transform.localScale = localScale;

            if (parent != null)
            {
                player.transform.SetSiblingIndex(
                    Mathf.Clamp(siblingIndex, 0, parent.childCount - 1)
                );
            }

            if (tokenImage != null)
                tokenImage.enabled = tokenImageEnabled;
        }
    }

    private static object SnapshotValue(object value)
    {
        if (ReferenceEquals(value, null) ||
            value is string ||
            value is UnityEngine.Object)
        {
            return value;
        }

        Type type = value.GetType();

        if (type.IsArray)
            return ((Array)value).Clone();

        if (value is IDictionary dictionary)
        {
            IDictionary copy = (IDictionary)Activator.CreateInstance(type);

            foreach (DictionaryEntry entry in dictionary)
                copy.Add(entry.Key, SnapshotValue(entry.Value));

            return copy;
        }

        if (value is IList list)
        {
            IList copy = (IList)Activator.CreateInstance(type);

            foreach (object item in list)
                copy.Add(SnapshotValue(item));

            return copy;
        }

        if (type.IsGenericType &&
            type.GetGenericTypeDefinition() == typeof(HashSet<>))
        {
            object copy = Activator.CreateInstance(type);
            MethodInfo add = type.GetMethod("Add");

            foreach (object item in (IEnumerable)value)
                add.Invoke(copy, new[] { item });

            return copy;
        }

        MethodInfo enqueue = type.GetMethod("Enqueue");
        if (enqueue != null && value is IEnumerable enumerable)
        {
            object copy = Activator.CreateInstance(type);

            foreach (object item in enumerable)
                enqueue.Invoke(copy, new[] { SnapshotValue(item) });

            return copy;
        }

        return value;
    }

    private static bool IsMutableCollection(object value)
    {
        if (ReferenceEquals(value, null))
            return false;

        Type type = value.GetType();

        return value is IDictionary ||
               value is IList ||
               (type.IsGenericType &&
                type.GetGenericTypeDefinition() == typeof(HashSet<>)) ||
               type.GetMethod("Enqueue") != null;
    }

    private static void RestoreCollectionInPlace(object destination, object source)
    {
        if (ReferenceEquals(destination, null) ||
            ReferenceEquals(source, null))
        {
            return;
        }

        if (destination is IDictionary targetDictionary &&
            source is IDictionary sourceDictionary)
        {
            targetDictionary.Clear();

            foreach (DictionaryEntry entry in sourceDictionary)
                targetDictionary.Add(entry.Key, SnapshotValue(entry.Value));

            return;
        }

        if (destination is IList targetList &&
            source is IList sourceList)
        {
            targetList.Clear();

            foreach (object item in sourceList)
                targetList.Add(SnapshotValue(item));

            return;
        }

        Type destinationType = destination.GetType();
        MethodInfo clear = destinationType.GetMethod("Clear", Type.EmptyTypes);
        if (clear == null || !(source is IEnumerable sourceEnumerable))
            return;

        clear.Invoke(destination, null);

        MethodInfo add = destinationType.GetMethod("Add");
        MethodInfo enqueue = destinationType.GetMethod("Enqueue");
        MethodInfo insert = add != null ? add : enqueue;

        if (insert == null)
            return;

        foreach (object item in sourceEnumerable)
            insert.Invoke(destination, new[] { SnapshotValue(item) });
    }

    // ============================================================
    // RESULT
    // ============================================================

    private void SetResult(
        string message)
    {
        lastResult =
            message;

        Debug.Log(
            $"FINANCIAL RECOVERY TESTER: {message}",
            this
        );
    }
}
