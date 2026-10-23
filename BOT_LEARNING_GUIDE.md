# Bot Self-Play Learning

The bot-learning system is integrated into the existing Unity bot strategy. It keeps the current legality/action-execution rules and learns small strategy preferences from all-bot matches.

## What it learns

- **Purchases:** shifts the cash reserve up/down to become more cautious or more willing to buy.
- **Development:** shifts the cash reserve used when deciding whether to build houses/hotels.
- **Mortgages:** adjusts the cash threshold that triggers defensive mortgage recovery.
- **Trades:** adjusts the value margin required before accepting an incoming trade.

Learning uses two signals:
1. **Turn-level reward shaping:** on completed bot turns, it compares the player's estimated net-worth change with the average change of opponents and nudges preferences for actions taken that turn.
2. **Match outcome:** when a match ends, it compares the winner's action frequency per turn with the other bots and makes a smaller policy update.

The saved policy is written to `bot_learning_data.json` under Unity's `Application.persistentDataPath`. It is local to the device/editor profile and persists across scene reloads and game sessions.

## How to train

1. Pull the latest `feature/match-end-system-core` branch into your Unity project.
2. Open the game in Unity and let scripts compile.
3. In the Unity menu, open **Board Game → Bot Learning → Show Learning Status** to see the current preferences and saved-data location.
4. Start a local match with every player set to **Bot**. Learning updates during completed bot turns; a completed match adds the final-outcome update.
5. The default evaluation limit is **300 completed player turns** (not dice rolls). Change it through **Board Game → Bot Learning → Set Turn Limit** (100, 250, 300, 500, or 1000). This limit applies only to all-bot training matches; human and mixed matches keep normal Monopoly end conditions.
6. At the limit, the match is closed as an **evaluation result**, not a bankruptcy victory. The player with the highest estimated net worth among active players is recorded as the leader. Assets and bankruptcy states are not changed by the evaluation. The saved JSON keeps the last 100 match records, including the leader, net worth, turn count, and standings.
7. To run unattended repeated matches, select **Board Game → Bot Learning → Toggle Auto-Restart Self-Play** to turn it ON. After a normal bankruptcy win or turn-limit evaluation, the active scene reloads after 3 seconds. Toggle it OFF to stop automatic restarts. Auto-restart remains OFF by default to avoid unexpectedly reloading a manually played match.
8. Use **Board Game → Bot Learning → Show Learning Status** to inspect the turn limit, latest result, recent standings, completed-match count and preferences.
9. Use **Board Game → Bot Learning → Reset Saved Learning** only if you intentionally want to discard the saved policy and evaluation history and start fresh.

## Important limits

This is a first practical online-learning layer, not a claim of human-level AI or a fully trained neural-network reinforcement-learning agent. Its estimated net worth does not model every strategic effect (for example, future rent distributions), and a turn-level signal can be noisy. Evaluate changes over many matches and compare win rates against a fixed baseline before concluding that the policy improved.

Learning updates are restricted to matches where every player is a bot, so human matches do not contaminate the self-play policy. Automatic restarts are OFF by default.
