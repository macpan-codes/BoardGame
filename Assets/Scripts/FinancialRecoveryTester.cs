using System;
using System.Collections.Generic;
using UnityEngine;

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
