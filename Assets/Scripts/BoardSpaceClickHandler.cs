using UnityEngine;
using UnityEngine.EventSystems;

public class BoardSpaceClickHandler :
    MonoBehaviour,
    IPointerClickHandler
{
    private BoardSpace boardSpace;

    private void Awake()
    {
        boardSpace =
            GetComponent<BoardSpace>();
    }

    public void OnPointerClick(
        PointerEventData eventData)
    {
        if (boardSpace == null)
        {
            boardSpace =
                GetComponent<BoardSpace>();
        }

        if (boardSpace == null)
            return;

        PropertyInfoPanel panel =
            FindFirstObjectByType<PropertyInfoPanel>();

        if (panel == null)
        {
            Debug.LogWarning(
                "BoardSpaceClickHandler: " +
                "PropertyInfoPanel not found."
            );

            return;
        }

        panel.ShowSpace(
            boardSpace
        );
    }
}