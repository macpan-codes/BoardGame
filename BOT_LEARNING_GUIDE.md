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
5. To run unattended repeated matches, select **Board Game → Bot Learning → Toggle Auto-Restart Self-Play** once to turn it ON. All-bot matches then reload the active scene three seconds after the results screen and start another match. Toggle it OFF to stop the automatic restart.
6. Use **Show Learning Status** between runs to verify the completed-match count and preferences are changing.
7. Use **Board Game → Bot Learning → Reset Saved Learning** only if you intentionally want to discard the saved policy and start fresh.

## Important limits

This is a first practical online-learning layer, not a claim of human-level AI or a fully trained neural-network reinforcement-learning agent. Its estimated net worth does not model every strategic effect (for example, future rent distributions), and a turn-level signal can be noisy. Evaluate changes over many matches and compare win rates against a fixed baseline before concluding that the policy improved.

Learning updates are restricted to matches where every player is a bot, so human matches do not contaminate the self-play policy. Automatic restarts are OFF by default.
