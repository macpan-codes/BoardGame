using UnityEngine;
using UnityEngine.UI;

public class PropertyOwnershipMarker : MonoBehaviour
{
    private Image markerImage;
    private GameObject visualObject;

    private BoardPlayer owner;

    public BoardPlayer Owner =>
        owner;

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        CreateVisual();
        Hide();
    }

    // ============================================================
    // VISUAL CREATION
    // ============================================================

    private void CreateVisual()
    {
        if (visualObject != null)
            return;

        visualObject =
            new GameObject(
                "OwnershipColor"
            );

        visualObject.transform.SetParent(
            transform,
            false
        );

        RectTransform rect =
            visualObject.AddComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(1f, 0f);

        rect.anchorMax =
            new Vector2(1f, 0f);

        rect.pivot =
            new Vector2(1f, 0f);

        rect.anchoredPosition =
            new Vector2(
                -3f,
                3f
            );

        rect.sizeDelta =
            new Vector2(
                10f,
                10f
            );

        markerImage =
            visualObject.AddComponent<Image>();

        markerImage.raycastTarget =
            false;
    }

    // ============================================================
    // OWNERSHIP
    // ============================================================

    public void SetOwner(
        BoardPlayer newOwner)
    {
        owner =
            newOwner;

        if (owner == null)
        {
            Hide();
            return;
        }

        RefreshColor();

        if (visualObject != null)
        {
            visualObject.SetActive(true);
        }
    }

    // ============================================================
    // REFRESH COLOR
    // ============================================================

    public void RefreshColor()
    {
        if (owner == null)
            return;

        if (markerImage == null)
        {
            CreateVisual();
        }

        if (markerImage == null)
            return;

        markerImage.color =
            owner.TokenColor;
    }

    // ============================================================
    // HIDE
    // ============================================================

    public void Hide()
    {
        owner = null;

        if (visualObject != null)
        {
            visualObject.SetActive(false);
        }
    }
}