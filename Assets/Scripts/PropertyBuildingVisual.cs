using UnityEngine;
using UnityEngine.UI;

public class PropertyBuildingVisual : MonoBehaviour
{
    [Header("Container")]
    [SerializeField] private RectTransform buildingContainer;

    [Header("Fallback Colors (used when no sprite is assigned)")]
    [SerializeField] private Color houseColor = Color.green;
    [SerializeField] private Color hotelColor = new Color(0.65f, 0.05f, 0.05f, 1f);

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

        if (lastHouseCount != boardSpace.Houses || lastHotelState != boardSpace.HasHotel)
            Refresh();
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
            CreateIndicator("Hotel", boardSpace.HotelSprite, hotelColor, boardSpace.HotelVisualSize, 0, true);
            return;
        }

        for (int i = 0; i < lastHouseCount; i++)
            CreateIndicator($"House_{i + 1}", boardSpace.HouseSprite, houseColor, boardSpace.HouseVisualSize, i, false);
    }

    private void CreateIndicator(string indicatorName, Sprite sprite, Color fallbackColor, Vector2 size, int index, bool isHotel)
    {
        GameObject indicator = new GameObject(indicatorName, typeof(RectTransform), typeof(Image));
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
        bool bottom = boardSpace.BoardIndex >= 1 && boardSpace.BoardIndex <= 9;
        bool right = boardSpace.BoardIndex >= 11 && boardSpace.BoardIndex <= 19;
        bool top = boardSpace.BoardIndex >= 21 && boardSpace.BoardIndex <= 29;
        bool left = boardSpace.BoardIndex >= 31 && boardSpace.BoardIndex <= 39;

        Vector2 position;
        if (bottom || top)
        {
            float gap = boardSpace.BuildingSpacing;
            float totalWidth = isHotel ? 0f : lastHouseCount * size.x + (lastHouseCount - 1) * gap;
            float x = isHotel ? 0f : -totalWidth * 0.5f + size.x * 0.5f + index * (size.x + gap);
            Vector2 offset = bottom ? boardSpace.BottomBuildingOffset : boardSpace.TopBuildingOffset;
            float y = bottom ? halfHeight + offset.y + size.y * 0.5f : -halfHeight + offset.y - size.y * 0.5f;
            position = new Vector2(x + offset.x, y);
        }
        else if (left || right)
        {
            float gap = boardSpace.BuildingSpacing;
            float totalHeight = isHotel ? 0f : lastHouseCount * size.y + (lastHouseCount - 1) * gap;
            float y = isHotel ? 0f : totalHeight * 0.5f - size.y * 0.5f - index * (size.y + gap);
            Vector2 offset = left ? boardSpace.LeftBuildingOffset : boardSpace.RightBuildingOffset;
            float x = left ? halfWidth + offset.x + size.x * 0.5f : -halfWidth + offset.x - size.x * 0.5f;
            position = new Vector2(x, y + offset.y);
        }
        else
        {
            position = new Vector2(0f, halfHeight + boardSpace.BottomBuildingOffset.y + size.y * 0.5f);
        }

        rect.anchoredPosition = position;
        rect.localRotation = Quaternion.Euler(0f, 0f,
            bottom ? boardSpace.BottomBuildingRotation :
            right ? boardSpace.RightBuildingRotation :
            top ? boardSpace.TopBuildingRotation :
            left ? boardSpace.LeftBuildingRotation : 0f);
        Image image = indicator.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = sprite;
            image.color = sprite != null ? Color.white : fallbackColor;
            image.preserveAspect = true;
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
            if (child == null)
                continue;
#if UNITY_EDITOR
            if (!Application.isPlaying)
                DestroyImmediate(child);
            else
                Destroy(child);
#else
            Destroy(child);
#endif
        }
    }
}
