using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// One-click automated checks for the match-end/player-state rules.
/// These checks use isolated temporary objects and do not modify the open scene.
/// Run from: Board Game > Tests > Run Match-End Tests.
/// </summary>
public static class MatchEndAutomatedTests
{
    [MenuItem("Board Game/Tests/Run Match-End Tests")]
    public static void RunMatchEndTests()
    {
        if (Application.isPlaying)
        {
            EditorUtility.DisplayDialog(
                "Match-End Tests",
                "Stop Play Mode before running these isolated tests.",
                "OK"
            );
            return;
        }

        int passed = 0;
        int failed = 0;
        List<string> failures = new List<string>();

        RunCase(
            "Active player counts work for 2–6 players",
            TestPlayerCounts,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Bankruptcy blocks money changes and hides the token",
            TestBankruptcyState,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Leaving the game blocks money changes and hides the token",
            TestLeftGameState,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "A 2-player match ends when one player is eliminated",
            TestTwoPlayerMatchEnd,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "A 3-player match continues until only one remains",
            TestThreePlayerMatchEnd,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Turns skip bankrupt and left-game players",
            TestTurnSkipping,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "A new game resets bankrupt/left-game status",
            TestNewGameReset,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Voting pauses turns and a cancelled vote resumes the match",
            TestVotePauseAndCancel,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "A unanimous end-game vote ends the match",
            TestUnanimousVote,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Leaving the current player clears property and advances the turn",
            TestVoluntaryLeave,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Bankruptcy property transfer removes buildings and preserves mortgage",
            TestBankruptcyPropertyTransfer,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Zero-cash player can recover cash by mortgaging property",
            TestZeroCashMortgageRecovery,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "The last active player cannot voluntarily leave without a winner",
            TestLastActiveCannotLeave,
            ref passed,
            ref failed,
            failures
        );

        string summary =
            "Match-End Automated Tests\n\n" +
            "PASSED: " + passed + "\n" +
            "FAILED: " + failed;

        if (failures.Count > 0)
        {
            summary += "\n\nFailures:\n- " +
                string.Join("\n- ", failures);
            Debug.LogError(summary);
        }
        else
        {
            Debug.Log(summary);
        }

        EditorUtility.DisplayDialog(
            "Match-End Tests: " + (failed == 0 ? "PASS" : "FAIL"),
            summary,
            "OK"
        );
    }

    private static void TestPlayerCounts()
    {
        for (int count = 2; count <= 6; count++)
        {
            using (TestFixture fixture = new TestFixture(count))
            {
                Require(
                    fixture.Manager.ActivePlayerCount == count,
                    "Expected " + count + " active players at setup."
                );

                Require(
                    fixture.Manager.GetActivePlayers().Length == count,
                    "GetActivePlayers returned the wrong count for " + count + " players."
                );

                fixture.Players[count - 1].MarkLeftGame();

                Require(
                    fixture.Manager.ActivePlayerCount == count - 1,
                    "A left-game player was still counted as active."
                );

                fixture.Players[count - 1].ResetForNewGame();

                Require(
                    fixture.Manager.ActivePlayerCount == count,
                    "ResetForNewGame did not reactivate the player."
                );
            }
        }
    }

    private static void TestBankruptcyState()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardPlayer player = fixture.Players[0];
            player.SetMoney(123);
            player.MarkEliminated();

            Require(player.IsBankrupt, "Player was not marked bankrupt.");
            Require(!player.IsActive, "Bankrupt player was still active.");
            Require(
                player.GameState == PlayerGameState.Bankrupt,
                "PlayerGameState did not report Bankrupt."
            );
            Require(
                !player.GetComponent<Image>().enabled,
                "Player token Image remained visible after bankruptcy."
            );

            player.AddMoney(500);
            Require(player.Money == 123, "Bankrupt player received money.");

            bool removed = player.RemoveMoney(10);
            Require(!removed, "Bankrupt player was allowed to spend money.");
            Require(player.Money == 123, "Bankrupt player's balance changed.");
        }
    }

    private static void TestLeftGameState()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            BoardPlayer player = fixture.Players[1];
            player.SetMoney(234);
            player.MarkLeftGame();

            Require(player.HasLeftGame, "Player was not marked as left.");
            Require(!player.IsActive, "Left-game player was still active.");
            Require(
                player.GameState == PlayerGameState.LeftGame,
                "PlayerGameState did not report LeftGame."
            );
            Require(
                !player.GetComponent<Image>().enabled,
                "Player token Image remained visible after leaving."
            );

            player.AddMoney(500);
            Require(player.Money == 234, "Left-game player received money.");

            bool removed = player.RemoveMoney(10);
            Require(!removed, "Left-game player was allowed to spend money.");
            Require(player.Money == 234, "Left-game player's balance changed.");

            Require(
                fixture.Manager.ActivePlayerCount == 2,
                "The left-game player was not excluded from the active count."
            );
        }
    }

    private static void TestTwoPlayerMatchEnd()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            fixture.Players[0].MarkEliminated();

            SetPrivateField(fixture.Manager, "isTurnActive", true);
            SetPrivateField(fixture.Manager, "waitingForPlayerAction", true);

            RunQuietly(fixture.Manager.CheckForWinner);

            Require(fixture.Manager.GameOver, "Two-player match did not end.");
            Require(
                fixture.Manager.ActivePlayerCount == 1,
                "Expected exactly one surviving player."
            );
            Require(
                !fixture.Manager.IsTurnActive,
                "Turn remained active after the match ended."
            );
            Require(
                !fixture.Manager.WaitingForPlayerAction,
                "Player action was still pending after the match ended."
            );
            Require(
                fixture.Manager.CurrentPhase.ToString() == "GameOver",
                "Game phase was not changed to GameOver."
            );
            Require(
                fixture.Manager.GetActivePlayers()[0] == fixture.Players[1],
                "The wrong player survived."
            );
        }
    }

    private static void TestThreePlayerMatchEnd()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            fixture.Players[0].MarkEliminated();
            RunQuietly(fixture.Manager.CheckForWinner);

            Require(
                !fixture.Manager.GameOver,
                "The match ended while two players were still active."
            );
            Require(
                fixture.Manager.ActivePlayerCount == 2,
                "Expected two players to continue."
            );

            fixture.Players[1].MarkLeftGame();
            RunQuietly(fixture.Manager.CheckForWinner);

            Require(fixture.Manager.GameOver, "Match did not end at one active player.");
            Require(
                fixture.Manager.GetActivePlayers().Length == 1 &&
                fixture.Manager.GetActivePlayers()[0] == fixture.Players[2],
                "The remaining active player was incorrect."
            );
        }
    }

    private static void TestTurnSkipping()
    {
        using (TestFixture fixture = new TestFixture(4))
        {
            fixture.Players[1].MarkEliminated();
            fixture.Players[2].MarkLeftGame();

            SetPrivateField(fixture.Manager, "currentPlayerIndex", 0);
            SetPrivateField(fixture.Manager, "gameOver", false);

            MethodInfo moveToNextPlayer = typeof(GameManager).GetMethod(
                "MoveToNextPlayer",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

            Require(moveToNextPlayer != null, "Could not find MoveToNextPlayer.");

            RunQuietly(() => moveToNextPlayer.Invoke(fixture.Manager, null));

            Require(
                fixture.Manager.CurrentPlayerIndex == 3,
                "Turn did not skip inactive players to reach Player 4."
            );
            Require(
                fixture.Manager.CurrentPlayer == fixture.Players[3],
                "The wrong player became current."
            );
            Require(
                fixture.Manager.IsTurnActive,
                "The next active player's turn did not start."
            );
            Require(
                !fixture.Manager.GameOver,
                "The match ended even though two active players remained."
            );
        }
    }

    private static void TestNewGameReset()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            fixture.Players[0].MarkEliminated();
            fixture.Players[1].MarkLeftGame();

            fixture.Players[0].ResetForNewGame();
            fixture.Players[1].ResetForNewGame();

            Require(fixture.Players[0].IsActive, "Bankrupt player did not reset to Active.");
            Require(fixture.Players[1].IsActive, "Left-game player did not reset to Active.");
            Require(
                fixture.Manager.ActivePlayerCount == 2,
                "Not all players were active after reset."
            );
        }
    }

    private static void TestVotePauseAndCancel()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            SetPrivateField(fixture.Manager, "isTurnActive", true);

            Require(
                fixture.Manager.IsPlayerTurn(fixture.Players[0]),
                "The current player should be able to act before the vote."
            );

            Require(
                fixture.Manager.StartEndGameVote(),
                "The end-game vote did not start."
            );

            Require(
                fixture.Manager.IsEndGameVoteInProgress,
                "Vote state was not enabled."
            );

            Require(
                !fixture.Manager.IsPlayerTurn(fixture.Players[0]),
                "Current player could still act during the vote."
            );

            int currentIndex = fixture.Manager.CurrentPlayerIndex;
            fixture.Manager.EndTurn();

            Require(
                fixture.Manager.CurrentPlayerIndex == currentIndex,
                "The turn changed while voting was active."
            );

            fixture.Manager.CancelEndGameVote();

            Require(
                !fixture.Manager.IsEndGameVoteInProgress,
                "Vote state was not cleared after cancellation."
            );

            Require(
                fixture.Manager.IsPlayerTurn(fixture.Players[0]),
                "The current player's turn did not resume after a NO vote."
            );
        }
    }

    private static void TestUnanimousVote()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            SetPrivateField(fixture.Manager, "isTurnActive", true);

            Require(
                fixture.Manager.StartEndGameVote(),
                "The end-game vote did not start."
            );

            Require(
                fixture.Manager.EndGameByUnanimousVote(),
                "The unanimous vote was not accepted."
            );

            Require(fixture.Manager.GameOver, "GameOver was not set after unanimous vote.");
            Require(!fixture.Manager.IsTurnActive, "A turn remained active after voting ended the game.");
            Require(!fixture.Manager.WaitingForPlayerAction, "A player action remained pending.");
            Require(!fixture.Manager.IsEndGameVoteInProgress, "Vote state remained active after game over.");

            fixture.Manager.CancelEndGameVote();
            Require(fixture.Manager.GameOver, "Cancelling a completed vote unexpectedly resumed the match.");
        }
    }

    private static void TestVoluntaryLeave()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            BoardPlayer leavingPlayer = fixture.Players[0];
            BoardSpace space = CreateTestProperty(fixture.Root.transform, leavingPlayer, 120);

            SetPrivateField(space, "houses", 2);
            SetPrivateField(space, "hotel", true);
            SetPrivateField(space, "mortgaged", true);

            Require(
                fixture.Manager.LeaveGame(leavingPlayer),
                "LeaveGame rejected a valid leave request."
            );

            Require(leavingPlayer.HasLeftGame, "Player was not marked LEFT GAME.");
            Require(!leavingPlayer.IsActive, "Left player remained active.");
            Require(space.Owner == null, "The leaving player's property was not returned to the bank.");
            Require(!space.IsOwned, "The leaving property remained marked as owned.");
            Require(space.Houses == 0, "Houses were not cleared.");
            Require(!space.HasHotel, "Hotel was not cleared.");
            Require(!space.IsMortgaged, "Mortgage was not cleared.");
            Require(
                fixture.Manager.CurrentPlayer == fixture.Players[1],
                "Turn did not advance to the next active player."
            );
            Require(
                fixture.Manager.ActivePlayerCount == 2,
                "Leaving player was not excluded from active player count."
            );
        }
    }

    private static void TestBankruptcyPropertyTransfer()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            BoardPlayer debtor = fixture.Players[0];
            BoardPlayer creditor = fixture.Players[1];

            BoardSpace space = CreateTestProperty(fixture.Root.transform, debtor, 150);
            SetPrivateField(space, "houses", 3);
            SetPrivateField(space, "hotel", true);
            SetPrivateField(space, "mortgaged", true);

            space.TransferOwnershipForBankruptcy(creditor);

            Require(space.Owner == creditor, "Property was not transferred to the creditor.");
            Require(space.Houses == 0, "Buildings were not removed during bankruptcy transfer.");
            Require(!space.HasHotel, "Hotel was not removed during bankruptcy transfer.");
            Require(space.IsMortgaged, "Mortgage state should remain with transferred property.");
        }
    }

    private static void TestZeroCashMortgageRecovery()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardPlayer player = fixture.Players[0];
            player.SetMoney(0);

            BoardSpace property = CreateTestProperty(fixture.Root.transform, player, 100);

            Require(player.IsActive, "A player with zero cash was incorrectly eliminated.");
            Require(property.CanMortgage(), "Owned clean property was not mortgageable.");
            Require(property.Mortgage(), "Mortgage operation failed.");
            Require(property.IsMortgaged, "Property did not enter the mortgaged state.");
            Require(player.Money == property.MortgageValue, "Mortgage money was not credited correctly.");
            Require(player.IsActive, "Player was incorrectly eliminated after mortgage recovery.");
        }
    }

    private static void TestLastActiveCannotLeave()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            fixture.Players[0].MarkEliminated();

            bool result = fixture.Manager.LeaveGame(fixture.Players[1]);

            Require(!result, "The last active player was allowed to leave.");
            Require(fixture.Players[1].IsActive, "The last player was marked LEFT GAME.");
            Require(!fixture.Manager.GameOver, "The game was left without a winner.");
            Require(
                fixture.Manager.ActivePlayerCount == 1,
                "The last active player count changed unexpectedly."
            );
        }
    }

    private static BoardSpace CreateTestProperty(Transform parent, BoardPlayer owner, int price)
    {
        GameObject propertyObject = new GameObject("TestProperty_" + price);
        propertyObject.transform.SetParent(parent, false);

        BoardSpace space = propertyObject.AddComponent<BoardSpace>();

        SetPrivateField(
            space,
            "spaceType",
            Enum.Parse(typeof(BoardSpaceType), "Property")
        );
        SetPrivateField(space, "purchasePrice", price);
        SetPrivateField(space, "mortgageValue", price / 2);
        SetPrivateField(space, "showBuildingVisual", false);
        SetPrivateField(space, "spaceName", "Automated Test Property");

        space.SetOwner(owner);
        return space;
    }

    private static void RunCase(
        string name,
        Action test,
        ref int passed,
        ref int failed,
        List<string> failures)
    {
        try
        {
            test();
            passed++;
            Debug.Log("[MATCH TEST PASS] " + name);
        }
        catch (Exception exception)
        {
            failed++;
            string failure = name + ": " + exception.Message;
            failures.Add(failure);
            Debug.LogError("[MATCH TEST FAIL] " + failure + "\n" + exception);
        }
    }

    private static void Require(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void RunQuietly(Action action)
    {
        bool previousLogState = Debug.unityLogger.logEnabled;
        Debug.unityLogger.logEnabled = false;

        try
        {
            action();
        }
        finally
        {
            Debug.unityLogger.logEnabled = previousLogState;
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic
        );

        if (field == null)
            throw new MissingFieldException(target.GetType().Name, fieldName);

        field.SetValue(target, value);
    }

    private sealed class TestFixture : IDisposable
    {
        public readonly GameObject Root;
        public readonly BoardPlayer[] Players;
        public readonly GameManager Manager;

        public TestFixture(int playerCount)
        {
            Root = new GameObject("__MatchEndAutomatedTestFixture__");
            Root.hideFlags = HideFlags.HideAndDontSave;

            Players = new BoardPlayer[playerCount];

            for (int i = 0; i < playerCount; i++)
            {
                GameObject playerObject =
                    new GameObject("TestPlayer_" + (i + 1));

                playerObject.transform.SetParent(Root.transform, false);
                playerObject.AddComponent<Image>();

                BoardPlayer player =
                    playerObject.AddComponent<BoardPlayer>();

                player.SetPlayerNumber(i + 1);
                player.SetPlayerName("Test Player " + (i + 1));

                Players[i] = player;
            }

            GameObject managerObject = new GameObject("TestGameManager");
            managerObject.transform.SetParent(Root.transform, false);

            Manager = managerObject.AddComponent<GameManager>();

            SetPrivateField(Manager, "players", Players);
            SetPrivateField(Manager, "currentPlayerIndex", 0);
            SetPrivateField(Manager, "gameOver", false);
            SetPrivateField(Manager, "isTurnActive", false);
            SetPrivateField(Manager, "waitingForPlayerAction", false);
        }

        public void Dispose()
        {
            if (Root != null)
                UnityEngine.Object.DestroyImmediate(Root);
        }
    }
}
