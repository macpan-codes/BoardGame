using TMPro;
using UnityEngine;

public class MoneyUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI balanceText;

    [Header("Player")]
    [SerializeField] private BoardPlayer player;

    private void Start()
    {
        if (player == null)
        {
            player = FindFirstObjectByType<BoardPlayer>();
        }

        UpdateBalance();
    }

    private void Update()
    {
        UpdateBalance();
    }

    private void UpdateBalance()
    {
        if (balanceText == null || player == null)
            return;

        balanceText.text = $"BALANCE\n${player.Money:N0}M";
    }
}