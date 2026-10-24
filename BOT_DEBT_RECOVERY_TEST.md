# Bot Debt-Recovery Regression Test

## Purpose

Reproduce the reported freeze where a bot has no cash, owns mortgageable property, and cannot select an asset to pay a debt. The batch also checks that the property panel cannot end the active recovery action.

## Run it in Unity

1. Pull `feature/match-end-system-core` and wait for Unity to finish compiling.
2. Open the game scene and enter **Play Mode**.
3. Select the GameManager object that has `FinancialRecoveryTester`.
4. In its Inspector, click **RUN ALL 7 FINANCIAL RECOVERY TESTS**.
5. Wait for the batch summary in the Inspector and inspect the Unity Console for the detailed PASS/FAIL lines.

## What the new scenario checks

`BotAutomaticMortgageRecovery` temporarily configures the selected debtor as a bot, gives it zero cash, assigns it a mortgageable property, and starts a small debt against another player. It then verifies:

- `PropertyManagementPanel.ResolveForBot()` does not close the active recovery action.
- The bot automatically mortgages the prepared property without a manual mortgage click.
- The debt is paid to the creditor, recovery ends, and the bot is not eliminated.
- Recovery does not remain active waiting for a property selection; the test waits up to 120 frames before reporting a failure.

The batch captures and restores the original scene state after all seven scenarios. The bot/human setting is also restored between scenarios.

## Expected result

The final batch summary should report **7 passed, 0 failed**, and say that the original scene state was restored. Any failed scenario should be treated as a regression; copy the full batch summary and relevant Console errors before changing code.

## Important

This is a Unity Play Mode regression test. The code has been committed, but it has **not** been run in Unity by the author of this change. Passing it locally is still required before resuming overnight self-play.
