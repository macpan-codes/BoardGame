using UnityEngine;
using UnityEngine.UI;

public class PropertyBuildingVisual : MonoBehaviour
{
    [Header("Container")]
    [SerializeField]
    private RectTransform buildingContainer;

    [Header("House Visual")]
    [SerializeField]
    private Color houseColor = Color.green;

    [SerializeField]
    private Vector2 houseSize = new Vector2(10f, 10f);

    [Header("Hotel Visual")]
    [SerializeField]
    private Color hotelColor =
        new Color(0.65f, 0.05f, 0.05f, 1f);

    [SerializeField]
    private Vector2 hotelSize = new Vector2(12f, 12f);

    [Header("Layout")]
    [SerializeField]
    private float spacing = 2f;

    [SerializeField]
    private float topOffset = 2f;

    private BoardSpace boardSpace;

    private int lastHouseCount = -1;
    private bool lastHotelState;

    // ============================================================
    // CONTAINER
    // ============================================================

    public void SetContainer(
        RectTransform container)
    {
        buildingContainer = container;

        Refresh();
    }

    // ============================================================
    // UNITY
    // ============================================================

    private void Awake()
    {
        boardSpace =
            GetComponentInParent<BoardSpace>();

        if (buildingContainer == null)
        {
            buildingContainer =
                GetComponent<RectTransform>();
        }

        Refresh();
    }

    private void LateUpdate()
    {
        if (boardSpace == null)
        {
            boardSpace =
                GetComponentInParent<BoardSpace>();

            if (boardSpace == null)
                return;
        }

        if (lastHouseCount != boardSpace.Houses ||
            lastHotelState != boardSpace.HasHotel)
        {
            Refresh();
        }
    }

    // ============================================================
    // REFRESH
    // ============================================================

    public void Refresh()
    {
        if (boardSpace == null)
        {
            boardSpace =
                GetComponentInParent<BoardSpace>();
        }

        if (boardSpace == null)
            return;

        if (buildingContainer == null)
        {
            buildingContainer =
                GetComponent<RectTransform>();
        }

        if (buildingContainer == null)
            return;

        ClearIndicators();

        lastHouseCount =
            Mathf.Clamp(
                boardSpace.Houses,
                0,
                4
            );

        lastHotelState =
            boardSpace.HasHotel;

        // --------------------------------------------------------
        // HOTEL
        // --------------------------------------------------------

        if (boardSpace.HasHotel)
        {
            CreateIndicator(
                "Hotel",
                hotelColor,
                hotelSize,
                0
            );

            return;
        }

        // --------------------------------------------------------
        // HOUSES
        // --------------------------------------------------------

        for (int i = 0;
             i < lastHouseCount;
             i++)
        {
            CreateIndicator(
                $"House_{i + 1}",
                houseColor,
                houseSize,
                i
            );
        }
    }

    // ============================================================
    // CREATE INDICATOR
    // ============================================================

    private void CreateIndicator(
        string indicatorName,
        Color color,
        Vector2 size,
        int index)
    {
        GameObject indicator =
            new GameObject(
                indicatorName,
                typeof(RectTransform),
                typeof(Image)
            );

        indicator.transform.SetParent(
            buildingContainer,
            false
        );

        RectTransform rect =
            indicator.GetComponent<RectTransform>();

        if (rect == null)
            return;

        // Top-left positioning.
        rect.anchorMin =
            new Vector2(0f, 1f);

        rect.anchorMax =
            new Vector2(0f, 1f);

        rect.pivot =
            new Vector2(0f, 1f);

        rect.sizeDelta =
            size;

        rect.anchoredPosition =
            new Vector2(
                index *
                (size.x + spacing),
                -topOffset
            );

        rect.localScale =
            Vector3.one;

        Image image =
            indicator.GetComponent<Image>();

        if (image != null)
        {
            image.color = color;
            image.raycastTarget = false;
        }
    }

    // ============================================================
    // CLEAR
    // ============================================================

    private void ClearIndicators()
    {
        if (buildingContainer == null)
            return;

        for (int i =
                 buildingContainer.childCount - 1;
             i >= 0;
             i--)
        {
            GameObject child =
                buildingContainer
                    .GetChild(i)
                    .gameObject;

            if (child != null)
            {
                Destroy(child);
            }
        }
    }
}