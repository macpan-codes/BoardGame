using UnityEngine;
using UnityEngine.UI;

public class PropertyOwnershipMarker : MonoBehaviour
{
    private Image markerImage;
    private GameObject visualObject;

    private BoardPlayer owner;

    public BoardPlayer Owner => owner;

    private void Awake()
    {
        CreateVisual();
        Hide();
    }

    private void CreateVisual()
    {
        if (visualObject != null)
            return;

        visualObject = new GameObject(
            "OwnershipColor"
        );

        visualObject.transform.SetParent(
            transform,
            false
        );

        RectTransform rect =
            visualObject.AddComponent<RectTransform>();

        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);

        rect.anchoredPosition =
            new Vector2(-3f, 3f);

        rect.sizeDelta =
            new Vector2(10f, 10f);

        markerImage =
            visualObject.AddComponent<Image>();

        markerImage.raycastTarget = false;
    }

    public void SetOwner(
        BoardPlayer newOwner)
    {
        owner = newOwner;

        if (owner == null)
        {
            Hide();
            return;
        }

        if (markerImage != null)
        {
            markerImage.color =
                GetPlayerColor(
                    owner.PlayerNumber
                );
        }

        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }
    }

    private Color GetPlayerColor(
        int playerNumber)
    {
        switch (playerNumber)
        {
            case 1:
                return Color.red;

            case 2:
                return Color.yellow;

            case 3:
                return Color.blue;

            case 4:
                return Color.green;

            default:
                return Color.white;
        }
    }

    public void Hide()
    {
        owner = null;

        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }
    }
}