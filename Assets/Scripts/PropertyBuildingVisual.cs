using UnityEngine;
using UnityEngine.UI;

public class PropertyBuildingVisual : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private RectTransform buildingContainer;

    [Header("House Visual")]
    [SerializeField] private Color houseColor = Color.green;
    [SerializeField] private Vector2 houseSize = new Vector2(10f, 10f);

    [Header("Hotel Visual")]
    [SerializeField] private Color hotelColor = new Color(0.65f, 0.05f, 0.05f, 1f);
    [SerializeField] private Vector2 hotelSize = new Vector2(12f, 12f);

    [Header("Layout")]
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float inwardOffset = 15f;

    private BoardSpace boardSpace;
    private int lastHouseCount = -1;
    private bool lastHotelState;

    public void SetContainer(RectTransform container)
    {
        buildingContainer = container;
        Refresh();
    }

    private void Awake()
    {
        boardSpace = GetComponentInParent<BoardSpace>();
        if (buildingContainer == null)
            buildingContainer = GetComponent<RectTransform>();

        Refresh();
    }

    private void LateUpdate()
    {
        if (boardSpace == null)
        {
            boardSpace = GetComponentInParent<BoardSpace>();
            if (boardSpace == null)
                return;
        }

        if (lastHouseCount != boardSpace.Houses ||
            lastHotelState != boardSpace.HasHotel)
        {
            Refresh();
        }
    }

    public void Refresh()
    {
        if (boardSpace == null)
            boardSpace = GetComponentInParent<BoardSpace>();

        if (boardSpace == null)
            return;

        if (buildingContainer == null)
            buildingContainer = GetComponent<RectTransform>();

        if (buildingContainer == null)
            return;

        ClearIndicators();

        lastHouseCount = Mathf.Clamp(boardSpace.Houses, 0, 4);
        lastHotelState = boardSpace.HasHotel;

        if (boardSpace.HasHotel)
        {
            CreateIndicator("Hotel", hotelColor, hotelSize, 0, true);
            return;
        }

        for (int i = 0; i < lastHouseCount; i++)
            CreateIndicator($"House_{i + 1}", houseColor, houseSize, i, false);
    }

    private void CreateIndicator(
        string indicatorName,
        Color color,
        Vector2 size,
        int index,
        bool isHotel)
    {
        GameObject indicator = new GameObject(
            indicatorName,
            typeof(RectTransform),
            typeof(Image));

        indicator.transform.SetParent(buildingContainer, false);

        RectTransform rect = indicator.GetComponent<RectTransform>();
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;

        RectTransform spaceRect = boardSpace.GetComponent<RectTransform>();
        float halfWidth = spaceRect != null ? spaceRect.rect.width * 0.5f : 0f;
        float halfHeight = spaceRect != null ? spaceRect.rect.height * 0.5f : 0f;

        // The building sits outside the property slot, toward the board centre.
        // Board indices follow the existing 40-space layout:
        // bottom 1-9, right 11-19, top 21-29, left 31-39.
        bool bottom = boardSpace.BoardIndex >= 1 && boardSpace.BoardIndex <= 9;
        bool right = boardSpace.BoardIndex >= 11 && boardSpace.BoardIndex <= 19;
        bool top = boardSpace.BoardIndex >= 21 && boardSpace.BoardIndex <= 29;
        bool left = boardSpace.BoardIndex >= 31 && boardSpace.BoardIndex <= 39;

        Vector2 position;
        if (bottom || top)
        {
            float totalWidth = isHotel ? 0f :
                (lastHouseCount * size.x + Mathf.Max(0, lastHouseCount - 1) * spacing);
            float x = isHotel ? 0f :
                -totalWidth * 0.5f + size.x * 0.5f + index * (size.x + spacing);
            float y = bottom
                ? halfHeight + inwardOffset + size.y * 0.5f
                : -halfHeight - inwardOffset - size.y * 0.5f;
            position = new Vector2(x, y);
        }
        else if (left || right)
        {
            float totalHeight = isHotel ? 0f :
                (lastHouseCount * size.y + Mathf.Max(0, lastHouseCount - 1) * spacing);
            float y = isHotel ? 0f :
                totalHeight * 0.5f - size.y * 0.5f - index * (size.y + spacing);
            float x = left
                ? halfWidth + inwardOffset + size.x * 0.5f
                : -halfWidth - inwardOffset - size.x * 0.5f;
            position = new Vector2(x, y);
        }
        else
        {
            // Safe fallback for an unindexed property.
            position = new Vector2(0f, halfHeight + inwardOffset + size.y * 0.5f);
        }

        rect.anchoredPosition = position;

        Image image = indicator.GetComponent<Image>();
        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
        }
    }

    private void ClearIndicators()
    {
        if (buildingContainer == null)
            return;

        for (int i = buildingContainer.childCount - 1; i >= 0; i--)
        {
            GameObject child = buildingContainer.GetChild(i).gameObject;
            if (child != null)
                Destroy(child);
        }
    }
}