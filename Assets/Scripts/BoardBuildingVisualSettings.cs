using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Shared Inspector controls for building sprites, sizes and placement.
/// Add this component to the existing Board GameObject (no prefab required).
/// </summary>
[ExecuteAlways]
public class BoardBuildingVisualSettings : MonoBehaviour
{
    [Header("Shared Sprites")]
    [SerializeField] private Sprite houseSprite;
    [SerializeField] private Sprite hotelSprite;

    [Header("Building Size (Unity UI units / Canvas units)")]
    [SerializeField] private Vector2 houseSize = new Vector2(17f, 17f);
    [SerializeField] private Vector2 hotelSize = new Vector2(20f, 20f);
    [Tooltip("Gap between neighboring houses. Negative values make houses overlap slightly.")]
    [SerializeField] private float buildingSpacing = 2f;

    [Header("Position From Property Edge")]
    [SerializeField] private Vector2 bottomOffset = new Vector2(0f, 15f);
    [SerializeField] private Vector2 rightOffset = new Vector2(-15f, 0f);
    [SerializeField] private Vector2 topOffset = new Vector2(0f, -15f);
    [SerializeField] private Vector2 leftOffset = new Vector2(15f, 0f);

    [Header("Eligible Property Board Indices")]
    [SerializeField] private List<int> eligibleBoardIndices = new List<int>
    {
        1, 3, 6, 8, 9,
        11, 13, 14, 16, 18, 19,
        21, 23, 24, 26, 27, 29,
        31, 32, 34, 37, 39
    };

    public Sprite HouseSprite => houseSprite;
    public Sprite HotelSprite => hotelSprite;
    public Vector2 HouseSize => houseSize;
    public Vector2 HotelSize => hotelSize;
    public float BuildingSpacing => buildingSpacing;
    public Vector2 BottomOffset => bottomOffset;
    public Vector2 RightOffset => rightOffset;
    public Vector2 TopOffset => topOffset;
    public Vector2 LeftOffset => leftOffset;

    public bool IsEligibleBoardIndex(int index) =>
        eligibleBoardIndices != null && eligibleBoardIndices.Contains(index);

    private void OnEnable() => RefreshAllBuildingVisuals();
    private void OnValidate() => RefreshAllBuildingVisuals();

    [ContextMenu("Refresh All Building Visuals")]
    public void RefreshAllBuildingVisuals()
    {
        BoardSpace[] spaces = FindObjectsByType<BoardSpace>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BoardSpace space in spaces)
        {
            if (space != null)
                space.RefreshBuildingVisual();
        }
    }
}
