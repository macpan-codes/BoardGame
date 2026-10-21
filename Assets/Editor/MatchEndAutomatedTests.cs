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

        RunCase(
            "A player cannot remove a different player from the local match",
            TestCannotLeaveAnotherPlayer,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Trades reject bankrupt and left-game players",
            TestTradesRejectInactivePlayers,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Pending trade is cancelled when an involved player leaves",
            TestPendingTradeCancelledForPlayer,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Money-only trades transfer the exact amounts",
            TestMoneyTrade,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Property trades transfer ownership",
            TestPropertyTrade,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Stale trade offers are rejected without moving assets",
            TestStaleTradeOfferRejected,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Only the designated receiver can accept or decline a trade",
            TestTradeResponsesRequireReceiver,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Developed and mortgaged properties cannot be traded",
            TestDevelopedOrMortgagedPropertiesCannotTrade,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Market Freeze blocks property transactions and trade offers",
            TestMarketFreezeBlocksPropertyTransactions,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Property rent follows base, house, hotel, and mortgage states",
            TestPropertyRentLevelsAndMortgage,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Railroad rent scales with railroad ownership",
            TestRailroadRentScalesWithOwnershipCount,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Utility rent uses dice total and utility ownership count",
            TestUtilityRentUsesDiceAndOwnershipCount,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Normal rent transfers the exact amount between players",
            TestNormalRentTransfersExactAmount,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Jail countdown releases a player after two skipped turns",
            TestJailCountdown,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "Market Freeze lasts for the next two full turns",
            TestMarketFreezeDuration,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "A pending trade does not block the sender from ending their turn",
            TestPendingTradeDoesNotBlockTurnEnd,
            ref passed,
            ref failed,
            failures
        );

        RunCase(
            "An inactive incoming-request panel can reactivate and receive an offer",
            TestIncomingRequestPanelWakesFromInactiveState,
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
            Require(
                fixture.Manager.CurrentPlayer == fixture.Players[1],
                "Current player was not updated to the winner."
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

            SetPrivateField(fixture.Manager, "waitingForPlayerAction", true);
            Require(
                !fixture.Manager.StartEndGameVote(),
                "Voting was allowed while a player action was unresolved."
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

    private static void TestCannotLeaveAnotherPlayer()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            BoardPlayer currentPlayer = fixture.Players[0];
            BoardPlayer otherPlayer = fixture.Players[1];

            BoardSpace otherPlayerProperty =
                CreateTestProperty(fixture.Root.transform, otherPlayer, 140);

            bool result = fixture.Manager.LeaveGame(otherPlayer);

            Require(!result, "Current player was allowed to remove a different player.");
            Require(otherPlayer.IsActive, "Target player was incorrectly marked LEFT GAME.");
            Require(otherPlayerProperty.Owner == otherPlayer, "Target player's property was cleared.");
            Require(
                fixture.Manager.ActivePlayerCount == 3,
                "Active player count changed after a rejected leave request."
            );
            Require(
                fixture.Manager.CurrentPlayer == currentPlayer,
                "Current player changed after a rejected leave request."
            );
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

    private static void TestTradesRejectInactivePlayers()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 10
            };

            receiver.MarkLeftGame();

            Require(
                !tradeManager.CanTradeWith(proposer, receiver),
                "Trade access remained available for a left-game player."
            );
            Require(
                !tradeManager.ValidateOffer(offer, out string leftGameError),
                "Trade validation accepted a left-game player."
            );
            Require(
                leftGameError != null && leftGameError.Contains("left-game"),
                "Left-game trade rejection did not provide a clear reason."
            );

            receiver.ResetForNewGame();
            receiver.MarkEliminated();

            Require(
                !tradeManager.CanTradeWith(proposer, receiver),
                "Trade access remained available for a bankrupt player."
            );
            Require(
                !tradeManager.ValidateOffer(offer, out _),
                "Trade validation accepted a bankrupt player."
            );
        }
    }

    private static void TestPendingTradeCancelledForPlayer()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];

            proposer.SetMoney(100);
            receiver.SetMoney(100);

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 25
            };

            Require(tradeManager.SendOffer(offer), "A valid test trade offer was rejected.");
            Require(tradeManager.HasPendingOffer, "Test trade was not left pending.");
            Require(
                !tradeManager.CancelPendingOfferForPlayer(fixture.Players[2]),
                "A player not involved in the offer cancelled the pending trade."
            );
            Require(tradeManager.HasPendingOffer, "An unrelated player cleared the pending trade.");

            Require(
                fixture.Manager.LeaveGame(proposer),
                "GameManager rejected the valid leave request."
            );
            Require(proposer.HasLeftGame, "Leaving player was not marked LEFT GAME.");
            Require(!tradeManager.HasPendingOffer, "Pending trade remained after its proposer left.");
            Require(
                fixture.Manager.CurrentPlayer == receiver,
                "Turn did not advance after the proposer left."
            );
        }
    }

    private static void TestMoneyTrade()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];

            proposer.SetMoney(100);
            receiver.SetMoney(200);

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 25
            };

            Require(tradeManager.SendOffer(offer), "Valid money-only offer was rejected.");

            // The sender's turn ends before the receiver responds.
            SetPrivateField(fixture.Manager, "currentPlayerIndex", 1);
            fixture.Manager.StartTurn();

            Require(
                tradeManager.ValidatePendingOffer(
                    tradeManager.PendingOffer,
                    out string pendingError
                ),
                "A valid queued offer was marked invalid on the receiver's turn: " + pendingError
            );
            Require(
                tradeManager.AcceptOffer(receiver),
                "Receiver could not accept a valid offer after the turn passed."
            );

            Require(proposer.Money == 75, "Proposer did not pay the exact offered amount.");
            Require(receiver.Money == 225, "Receiver did not receive the exact offered amount.");
            Require(!tradeManager.HasPendingOffer, "Completed money trade remained pending.");
        }
    }

    private static void TestPropertyTrade()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            BoardSpace property = CreateTestProperty(fixture.Root.transform, proposer, 100);
            SetPrivateField(property, "ownerLandingCount", 4);

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver
            };
            offer.OfferedProperties.Add(property);

            Require(tradeManager.SendOffer(offer), "Valid property offer was rejected.");
            Require(tradeManager.AcceptOffer(receiver), "Valid property offer was not completed.");

            Require(property.Owner == receiver, "Property ownership did not transfer to the receiver.");
            Require(property.IsOwned, "Traded property was no longer marked as owned.");
            Require(property.MaximumAllowedHouses == 0, "Traded property kept the previous owner's development progress.");
            Require(!tradeManager.HasPendingOffer, "Completed property trade remained pending.");
        }
    }

    private static void TestStaleTradeOfferRejected()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            BoardPlayer newOwner = fixture.Players[2];

            proposer.SetMoney(100);
            receiver.SetMoney(100);
            BoardSpace property = CreateTestProperty(fixture.Root.transform, proposer, 100);

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 20
            };
            offer.OfferedProperties.Add(property);

            Require(tradeManager.SendOffer(offer), "Initially valid offer was rejected.");

            property.SetOwner(newOwner);

            Require(!tradeManager.AcceptOffer(receiver), "Offer succeeded after proposer lost property ownership.");
            Require(proposer.Money == 100, "Proposer lost money after the stale offer was rejected.");
            Require(receiver.Money == 100, "Receiver money changed after the stale offer was rejected.");
            Require(property.Owner == newOwner, "Stale offer changed property ownership.");
            Require(!tradeManager.HasPendingOffer, "Failed stale offer remained pending.");
        }
    }

    private static void TestTradeResponsesRequireReceiver()
    {
        using (TestFixture fixture = new TestFixture(3))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            BoardPlayer unrelated = fixture.Players[2];

            proposer.SetMoney(100);
            receiver.SetMoney(100);
            unrelated.SetMoney(100);

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 20
            };

            Require(tradeManager.SendOffer(offer), "Valid offer was rejected.");
            TradeOffer firstPending = tradeManager.PendingOffer;
            Require(!tradeManager.AcceptOffer(unrelated), "An unrelated player accepted the offer.");
            Require(!tradeManager.DeclineOffer(unrelated), "An unrelated player declined the offer.");
            Require(tradeManager.HasPendingOffer, "Unauthorized response cleared the offer.");
            Require(proposer.Money == 100 && receiver.Money == 100, "Unauthorized response changed balances.");

            Require(tradeManager.AcceptOffer(receiver), "The designated receiver could not accept the offer.");
            Require(proposer.Money == 80 && receiver.Money == 120, "Authorized acceptance transferred the wrong amounts.");
            Require(!tradeManager.HasPendingOffer, "Accepted offer remained pending.");

            TradeOffer secondOffer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 10
            };
            Require(tradeManager.SendOffer(secondOffer), "Second valid offer was rejected.");
            TradeOffer secondPending = tradeManager.PendingOffer;

            Require(!tradeManager.AcceptOffer(receiver, firstPending), "A stale request accepted a newer offer.");
            Require(!tradeManager.DeclineOffer(receiver, firstPending), "A stale request declined a newer offer.");
            Require(tradeManager.HasPendingOffer && tradeManager.PendingOffer == secondPending, "Stale response changed the current pending offer.");
            Require(proposer.Money == 80 && receiver.Money == 120, "A stale response changed balances.");

            Require(tradeManager.DeclineOffer(receiver, secondPending), "Receiver could not decline the current offer.");
            Require(proposer.Money == 80 && receiver.Money == 120, "Declining an offer changed balances.");
            Require(!tradeManager.HasPendingOffer, "Declined offer remained pending.");
        }
    }

    private static void TestDevelopedOrMortgagedPropertiesCannotTrade()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            BoardSpace property = CreateTestProperty(fixture.Root.transform, proposer, 100);

            SetPrivateField(property, "houses", 1);
            TradeOffer developedOffer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver
            };
            developedOffer.OfferedProperties.Add(property);

            Require(!tradeManager.SendOffer(developedOffer), "A property with houses was allowed to trade.");
            Require(!tradeManager.HasPendingOffer, "An invalid developed-property offer remained pending.");

            SetPrivateField(property, "houses", 0);
            SetPrivateField(property, "mortgaged", true);
            TradeOffer mortgagedOffer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver
            };
            mortgagedOffer.OfferedProperties.Add(property);

            Require(!tradeManager.SendOffer(mortgagedOffer), "A mortgaged property was allowed to trade.");
            Require(!tradeManager.HasPendingOffer, "An invalid mortgaged-property offer remained pending.");
        }
    }

    private static void TestPendingTradeDoesNotBlockTurnEnd()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            proposer.SetMoney(100);

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 10
            };

            Require(tradeManager.SendOffer(offer), "Valid offer was rejected.");
            Require(tradeManager.HasPendingOffer, "The sent offer was not left pending.");
            fixture.Manager.EndTurn();

            Require(fixture.Manager.CurrentPlayer == receiver, "The sender could not end their turn while an offer was pending.");
            Require(fixture.Manager.IsTurnActive, "The receiver's turn did not start.");
            Require(tradeManager.HasPendingOffer, "The pending offer was cleared when the sender ended their turn.");
        }
    }

    private static void TestMarketFreezeBlocksPropertyTransactions()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            proposer.SetMoney(500);
            receiver.SetMoney(500);

            CreateTestCommunityChestManager(
                fixture,
                proposer,
                2,
                false
            );

            BoardSpace unownedProperty =
                CreateTestProperty(
                    fixture.Root.transform,
                    null,
                    100
                );

            Require(
                !fixture.Manager.BuySpace(unownedProperty),
                "A frozen player purchased an unowned property."
            );
            Require(
                unownedProperty.Owner == null && proposer.Money == 500,
                "A blocked property purchase changed ownership or money."
            );

            BoardSpace ownedProperty =
                CreateTestProperty(
                    fixture.Root.transform,
                    proposer,
                    100
                );

            SetPrivateField(ownedProperty, "houses", 1);
            Require(
                !fixture.Manager.CanBuildHouse(ownedProperty),
                "House-building remained available during Market Freeze."
            );
            Require(
                !ownedProperty.SellHouse(),
                "A frozen player sold a house."
            );
            Require(
                ownedProperty.Houses == 1 && proposer.Money == 500,
                "A blocked house sale changed buildings or money."
            );

            SetPrivateField(ownedProperty, "houses", 0);
            Require(
                !fixture.Manager.CanBuildHotel(ownedProperty),
                "Hotel-building remained available during Market Freeze."
            );
            Require(
                !fixture.Manager.MortgageProperty(ownedProperty),
                "A frozen player mortgaged a property."
            );
            Require(
                !ownedProperty.Mortgage(),
                "A direct property mortgage bypassed Market Freeze."
            );

            SetPrivateField(ownedProperty, "mortgaged", true);
            Require(
                !fixture.Manager.UnmortgageProperty(ownedProperty),
                "A frozen player unmortgaged a property."
            );
            Require(
                !ownedProperty.Unmortgage(),
                "A direct property unmortgage bypassed Market Freeze."
            );

            SetPrivateField(ownedProperty, "mortgaged", false);
            SetPrivateField(ownedProperty, "hotel", true);
            Require(
                !ownedProperty.SellHotel(),
                "A frozen player sold a hotel."
            );

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 10
            };

            Require(
                !tradeManager.SendOffer(offer),
                "A frozen player sent a trade offer."
            );
            Require(
                !tradeManager.HasPendingOffer,
                "A trade offer remained pending despite Market Freeze."
            );
            Require(
                proposer.Money == 500 && receiver.Money == 500,
                "A blocked trade offer changed player balances."
            );
        }
    }

    private static void TestMarketFreezeDuration()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardPlayer player = fixture.Players[0];

            CommunityChestCardManager communityChest =
                CreateTestCommunityChestManager(
                    fixture,
                    player,
                    2,
                    true
                );

            Require(
                communityChest.IsMarketFrozen(player),
                "New Market Freeze was not active."
            );

            communityChest.AdvanceMarketFreezeTurn(player);
            Require(
                communityChest.MarketFreezeTurns(player) == 2,
                "Market Freeze incorrectly consumed a turn on the turn it was applied."
            );

            communityChest.AdvanceMarketFreezeTurn(player);
            Require(
                communityChest.MarketFreezeTurns(player) == 1,
                "Market Freeze did not decrement after the first full turn."
            );

            communityChest.AdvanceMarketFreezeTurn(player);
            Require(
                !communityChest.IsMarketFrozen(player),
                "Market Freeze did not expire after the second full turn."
            );
        }
    }

    private static void TestPropertyRentLevelsAndMortgage()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardSpace property =
                CreateTestProperty(
                    fixture.Root.transform,
                    fixture.Players[0],
                    100
                );

            SetPrivateField(property, "baseRent", 20);
            SetPrivateField(property, "rentLevel1", 40);
            SetPrivateField(property, "rentLevel2", 80);
            SetPrivateField(property, "rentLevel3", 120);
            SetPrivateField(property, "rentLevel4", 160);
            SetPrivateField(property, "hotelRent", 200);

            Require(property.GetRent() == 20, "Base property rent was incorrect.");

            SetPrivateField(property, "houses", 1);
            Require(property.GetRent() == 40, "One-house rent was incorrect.");

            SetPrivateField(property, "houses", 2);
            Require(property.GetRent() == 80, "Two-house rent was incorrect.");

            SetPrivateField(property, "houses", 3);
            Require(property.GetRent() == 120, "Three-house rent was incorrect.");

            SetPrivateField(property, "houses", 4);
            Require(property.GetRent() == 160, "Four-house rent was incorrect.");

            SetPrivateField(property, "houses", 0);
            SetPrivateField(property, "hotel", true);
            Require(property.GetRent() == 200, "Hotel rent was incorrect.");

            SetPrivateField(property, "mortgaged", true);
            Require(property.GetRent() == 0, "Mortgaged property still charged rent.");
        }
    }

    private static void TestRailroadRentScalesWithOwnershipCount()
    {
        int[] expectedRent = { 25, 50, 100, 200 };

        for (int count = 1; count <= 4; count++)
        {
            using (TestFixture fixture = new TestFixture(2))
            {
                BoardPlayer owner = fixture.Players[0];
                List<BoardSpace> railroads = new List<BoardSpace>();

                for (int i = 0; i < count; i++)
                {
                    BoardSpace railroad =
                        CreateTestProperty(
                            fixture.Root.transform,
                            owner,
                            100
                        );

                    SetPrivateField(
                        railroad,
                        "spaceType",
                        Enum.Parse(typeof(BoardSpaceType), "Airport")
                    );

                    railroads.Add(railroad);
                }

                Require(
                    railroads[0].GetRent() == expectedRent[count - 1],
                    "Railroad rent was incorrect when the owner held " + count + " railroad(s)."
                );
            }
        }
    }

    private static void TestUtilityRentUsesDiceAndOwnershipCount()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardPlayer owner = fixture.Players[0];

            BoardSpace firstUtility =
                CreateTestProperty(
                    fixture.Root.transform,
                    owner,
                    100
                );

            SetPrivateField(
                firstUtility,
                "spaceType",
                Enum.Parse(typeof(BoardSpaceType), "Utility")
            );

            Require(
                firstUtility.GetUtilityRent(7) == 28,
                "One utility should charge four times the dice total."
            );

            BoardSpace secondUtility =
                CreateTestProperty(
                    fixture.Root.transform,
                    owner,
                    100
                );

            SetPrivateField(
                secondUtility,
                "spaceType",
                Enum.Parse(typeof(BoardSpaceType), "Utility")
            );

            Require(
                firstUtility.GetUtilityRent(7) == 70,
                "Two utilities should charge ten times the dice total."
            );

            SetPrivateField(firstUtility, "mortgaged", true);
            Require(
                firstUtility.GetUtilityRent(7) == 0,
                "A mortgaged utility still charged rent."
            );
        }
    }

    private static void TestNormalRentTransfersExactAmount()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardPlayer payer = fixture.Players[0];
            BoardPlayer owner = fixture.Players[1];

            payer.SetMoney(100);
            owner.SetMoney(50);

            BoardSpace property =
                CreateTestProperty(
                    fixture.Root.transform,
                    owner,
                    100
                );

            SetPrivateField(property, "baseRent", 30);

            Require(
                fixture.Manager.PayRent(property),
                "A player with enough cash could not pay rent."
            );

            Require(payer.Money == 70, "Payer did not lose the exact rent amount.");
            Require(owner.Money == 80, "Property owner did not receive the exact rent amount.");
        }
    }

    private static void TestJailCountdown()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            BoardPlayer player = fixture.Players[0];

            SetPrivateField(player, "inJail", true);
            SetPrivateField(player, "jailTurnsRemaining", 2);

            Require(player.IsInJail, "Player was not in the jailed test state.");
            Require(player.JailTurnsRemaining == 2, "Jail should begin with two turns remaining.");

            player.DecreaseJailTurn();
            Require(player.IsInJail, "Player was released after only one skipped turn.");
            Require(player.JailTurnsRemaining == 1, "Jail countdown did not decrease to one.");

            player.DecreaseJailTurn();
            Require(!player.IsInJail, "Player was not released after the second skipped turn.");
            Require(player.JailTurnsRemaining == 0, "Jail countdown did not reach zero.");
        }
    }

    private static void TestIncomingRequestPanelWakesFromInactiveState()
    {
        using (TestFixture fixture = new TestFixture(2))
        {
            TradeManager tradeManager = CreateTestTradeManager(fixture);
            BoardPlayer proposer = fixture.Players[0];
            BoardPlayer receiver = fixture.Players[1];
            proposer.SetMoney(100);

            GameObject panelObject = new GameObject("TestTradeRequestPanel");
            panelObject.transform.SetParent(fixture.Root.transform, false);
            TradeRequestPanel requestPanel = panelObject.AddComponent<TradeRequestPanel>();
            panelObject.SetActive(false);

            Require(requestPanel.PrepareForOfferDelivery(tradeManager), "Inactive request controller could not be activated and bound to the sending manager.");
            Require(panelObject.activeSelf, "Request controller remained inactive.");

            TradeOffer offer = new TradeOffer
            {
                Proposer = proposer,
                Receiver = receiver,
                OfferedMoney = 10
            };

            Require(tradeManager.SendOffer(offer), "Valid offer was rejected.");
            CanvasGroup group = panelObject.GetComponent<CanvasGroup>();
            Require(group != null && group.alpha < 0.01f, "Incoming request popup appeared before the receiver's turn.");
            Require(group != null && !group.blocksRaycasts, "Hidden request popup blocked gameplay before the receiver's turn.");

            fixture.Manager.EndTurn();

            Require(fixture.Manager.CurrentPlayer == receiver, "The receiver's turn did not start after the sender ended their turn.");
            Require(group != null && group.alpha > 0.99f, "Queued incoming request did not appear when the receiver's turn started.");
            Require(group != null && group.blocksRaycasts, "Visible request panel did not block gameplay clicks.");
        }
    }

    private static TradeManager CreateTestTradeManager(TestFixture fixture)
    {
        SetPrivateField(fixture.Manager, "isTurnActive", true);
        GameObject tradeObject = new GameObject("TestTradeManager");
        tradeObject.transform.SetParent(fixture.Root.transform, false);
        return tradeObject.AddComponent<TradeManager>();
    }

    private static CommunityChestCardManager CreateTestCommunityChestManager(
        TestFixture fixture,
        BoardPlayer frozenPlayer,
        int turnsRemaining,
        bool newlyApplied)
    {
        GameObject managerObject =
            new GameObject("TestCommunityChestCardManager");

        managerObject.transform.SetParent(
            fixture.Root.transform,
            false
        );

        // Keep it inactive to avoid creating UI for this isolated test.
        managerObject.SetActive(false);

        CommunityChestCardManager manager =
            managerObject.AddComponent<CommunityChestCardManager>();

        FieldInfo turnsField =
            typeof(CommunityChestCardManager).GetField(
                "marketFreezeTurns",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        FieldInfo newlyAppliedField =
            typeof(CommunityChestCardManager).GetField(
                "newlyAppliedMarketFreezes",
                BindingFlags.Instance | BindingFlags.NonPublic
            );

        if (turnsField == null || newlyAppliedField == null)
        {
            throw new MissingFieldException(
                "CommunityChestCardManager Market Freeze state fields were not found."
            );
        }

        Dictionary<BoardPlayer, int> turns =
            (Dictionary<BoardPlayer, int>)turnsField.GetValue(manager);

        HashSet<BoardPlayer> newlyAppliedPlayers =
            (HashSet<BoardPlayer>)newlyAppliedField.GetValue(manager);

        turns[frozenPlayer] = turnsRemaining;

        if (newlyApplied)
            newlyAppliedPlayers.Add(frozenPlayer);

        return manager;
    }

    private static BoardSpace CreateTestProperty(Transform parent, BoardPlayer owner, int price)
    {
        GameObject propertyObject = new GameObject("TestProperty_" + price);
        propertyObject.transform.SetParent(parent, false);
        propertyObject.SetActive(false);

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

        propertyObject.SetActive(true);
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
