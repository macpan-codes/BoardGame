# BoardGame Project Instructions

## Project

This is a Unity 6 2D mobile board game project.

The developer is a beginner in game development and C#.

Keep explanations beginner-friendly and explain important concepts when introducing them.

## Game Concept

The game is a custom board game inspired by the way Monopoly is played locally.

The first version is offline/local multiplayer on one mobile device.

Players:

- Minimum: 2

- Maximum: 4

Players take turns rolling two dice and moving around a square board.

## Core Game Systems

The game will eventually contain:

- Two dice

- Turn-based gameplay

- Board movement

- Properties

- Property ownership

- Houses

- Hotels

- Railways/stations

- Utilities

- Chance cards

- Community Chest-style cards

- Central bank system

- Starting money

- Money for completing a full board round

- Rent

- Player-to-player transactions

- Bankruptcy

- Winning conditions

There will NOT be an auction system in the initial version.

Online multiplayer will be added later.

Do not implement networking unless specifically requested.

## Bank System

The bank is a central game system.

The bank handles:

- Giving players their starting money

- Giving players money when they complete a full board round

- Receiving payment for property purchases

- Receiving payment for houses and hotels

- Paying rewards specified by cards

- Receiving taxes and other bank payments

Player-to-player transactions should remain separate from bank transactions.

Examples:

Player -> Bank

- Property purchase

- House purchase

- Hotel purchase

- Taxes

Bank -> Player

- Starting money

- Round-completion reward

- Card rewards

Player -> Player

- Rent

- Player-to-player card payments

## Development Rules

1. Do not create the entire game at once.

2. Build one feature at a time.

3. Do not rewrite working systems unnecessarily.

4. Do not create duplicate scripts when an existing script can be extended.

5. Keep systems modular and easy to understand.

6. Prefer simple solutions suitable for a beginner.

7. Avoid unnecessary third-party assets or plugins.

8. Do not add online multiplayer unless explicitly requested.

9. Do not add an auction system unless explicitly requested.

10. Before making major architectural changes, explain the change first.

11. When creating code, explain where the file should be placed and how it connects to Unity.

12. If there is a simpler implementation, prefer the simpler implementation.

13. Do not modify unrelated files.

14. Preserve existing functionality when adding new features.

## AI Behavior

Before making significant changes:

- Explain what you are going to change.

- Identify which files will be created or modified.

- Explain any important Unity concepts involved.

When debugging:

- Find the root cause instead of blindly changing code.

- Explain the error in beginner-friendly language.

- Make the smallest reasonable fix.

When generating code:

- Use clear C# naming.

- Keep scripts focused on one main responsibility.

- Avoid unnecessary complexity.

- Do not generate code that depends on packages we have not installed unless explicitly requested.

## Current Development Stage

We are currently building the first offline prototype.

The first prototype should eventually contain:

1. Basic board

2. One player piece

3. Two dice

4. Dice rolling

5. Dice total

6. Player movement

7. Basic turn system

Do not implement properties, bank, cards, houses, hotels, networking, or other advanced systems until specifically requested.