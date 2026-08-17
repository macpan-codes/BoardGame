using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Opens the trade panel from a UI button.
/// Does not affect turn state.
/// </summary>
public class TradeOpenButton : MonoBehaviour
{
    private void Awake()
    {
        Button button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OpenTradePanel);
        }
    }

    public void OpenTradePanel()
    {
        TradePanel tradePanel =
            FindFirstObjectByType<TradePanel>(
                FindObjectsInactive.Include
            );

        if (tradePanel == null)
        {
            Debug.LogError(
                "TradeOpenButton: TradePanel not found."
            );

            return;
        }

        tradePanel.Show();
    }
}
