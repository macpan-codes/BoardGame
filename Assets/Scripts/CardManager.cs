using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CardManager : MonoBehaviour
{
    private enum CardEffect
    {
        ReceiveMoney,
        PayMoney,
        MoveRelative,
        GoToJail
    }

    [System.Serializable]
    private class CardDefinition
    {
        public string title;
        public string description;
        public CardEffect effect;
        public int amount;
        public int movement;
    }

    [Header("Chance Cards")]
    [SerializeField]
    private List<CardDefinition> chanceCards =
        new List<CardDefinition>();

    [Header("Community Chest Cards")]
    [SerializeField]
    private List<CardDefinition> communityChestCards =
        new List<CardDefinition>();

    private void Awake()
    {
        CreateDefaultCards();
    }

    public void DrawChanceCard(BoardPlayer player)
    {
        DrawCard(
            player,
            chanceCards,
            "CHANCE"
        );
    }

    public void DrawCommunityChestCard(BoardPlayer player)
    {
        DrawCard(
            player,
            communityChestCards,
            "COMMUNITY CHEST"
        );
    }

    private void DrawCard(
        BoardPlayer player,
        List<CardDefinition> deck,
        string deckName)
    {
        if (player == null)
            return;

        GameManager gameManager =
            FindFirstObjectByType<GameManager>();

        if (gameManager == null)
            return;

        if (deck == null || deck.Count == 0)
        {
            Debug.LogWarning(
                $"{deckName}: No cards available."
            );

            gameManager.EndPlayerAction();
            gameManager.EndTurn();
            return;
        }

        CardDefinition card =
            deck[Random.Range(0, deck.Count)];

        Debug.Log(
            $"{deckName}: {card.title} - " +
            $"{card.description}"
        );

        ResolveCard(
            player,
            card,
            gameManager
        );
    }

    private void ResolveCard(
        BoardPlayer player,
        CardDefinition card,
        GameManager gameManager)
    {
        switch (card.effect)
        {
            case CardEffect.ReceiveMoney:

                gameManager.PayPlayer(
                    card.amount
                );

                FinishCard(
                    gameManager
                );

                break;


            case CardEffect.PayMoney:

                if (player.Money >= card.amount)
                {
                    gameManager.PlayerPaysBank(
                        card.amount
                    );

                    FinishCard(
                        gameManager
                    );
                }
                else
                {
                    gameManager.HandleBankruptcy(
                        player
                    );
                }

                break;


            case CardEffect.MoveRelative:

                if (card.movement > 0)
                {
                    player.MoveBySteps(
                        card.movement
                    );

                    StartCoroutine(
                        WaitForMovement(
                            player,
                            gameManager
                        )
                    );
                }
                else
                {
                    FinishCard(
                        gameManager
                    );
                }

                break;


            case CardEffect.GoToJail:

                player.SendToJail();

                FinishCard(
                    gameManager
                );

                break;
        }
    }

    private IEnumerator WaitForMovement(
        BoardPlayer player,
        GameManager gameManager)
    {
        yield return new WaitUntil(
            () => !player.IsMoving
        );

        gameManager.EndPlayerAction();
        gameManager.EndTurn();
    }

    private void FinishCard(
        GameManager gameManager)
    {
        gameManager.EndPlayerAction();
        gameManager.EndTurn();
    }

    private void CreateDefaultCards()
    {
        if (chanceCards.Count == 0)
        {
            chanceCards.Add(
                new CardDefinition
                {
                    title = "Bank Reward",
                    description = "Receive $150M.",
                    effect = CardEffect.ReceiveMoney,
                    amount = 150
                }
            );

            chanceCards.Add(
                new CardDefinition
                {
                    title = "Bank Fee",
                    description = "Pay $100M.",
                    effect = CardEffect.PayMoney,
                    amount = 100
                }
            );

            chanceCards.Add(
                new CardDefinition
                {
                    title = "Move Forward",
                    description = "Move forward 3 spaces.",
                    effect = CardEffect.MoveRelative,
                    movement = 3
                }
            );

            chanceCards.Add(
                new CardDefinition
                {
                    title = "Go To Jail",
                    description = "Go directly to Jail.",
                    effect = CardEffect.GoToJail
                }
            );
        }

        if (communityChestCards.Count == 0)
        {
            communityChestCards.Add(
                new CardDefinition
                {
                    title = "Community Bonus",
                    description = "Receive $100M.",
                    effect = CardEffect.ReceiveMoney,
                    amount = 100
                }
            );

            communityChestCards.Add(
                new CardDefinition
                {
                    title = "Unexpected Expense",
                    description = "Pay $50M.",
                    effect = CardEffect.PayMoney,
                    amount = 50
                }
            );

            communityChestCards.Add(
                new CardDefinition
                {
                    title = "Bank Award",
                    description = "Receive $200M.",
                    effect = CardEffect.ReceiveMoney,
                    amount = 200
                }
            );

            communityChestCards.Add(
                new CardDefinition
                {
                    title = "Special Assessment",
                    description = "Pay $150M.",
                    effect = CardEffect.PayMoney,
                    amount = 150
                }
            );

            communityChestCards.Add(
                new CardDefinition
                {
                    title = "Go To Jail",
                    description = "Go directly to Jail.",
                    effect = CardEffect.GoToJail
                }
            );
        }
    }
}