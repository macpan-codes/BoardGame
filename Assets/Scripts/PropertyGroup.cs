using UnityEngine;

public class PropertyGroup : MonoBehaviour
{
    [Header("Group")]
    [SerializeField] private string groupName = "Group";
    [SerializeField] private Color groupColor = Color.white;

    [Header("Monopoly Rent")]
    [SerializeField]
    [Min(1)]
    private int completedGroupRentMultiplier = 2;

    public string GroupName => groupName;
    public Color GroupColor => groupColor;

    public int CompletedGroupRentMultiplier =>
        Mathf.Max(1, completedGroupRentMultiplier);

    public void SetGroupName(string value)
    {
        groupName =
            string.IsNullOrWhiteSpace(value)
                ? "Group"
                : value.Trim();
    }

    public bool IsCompleteFor(BoardPlayer player)
    {
        if (player == null)
            return false;

        BoardSpace[] spaces =
            FindObjectsByType<BoardSpace>(
                FindObjectsSortMode.None
            );

        bool foundAny = false;

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            if (space.PropertyGroup != this)
                continue;

            foundAny = true;

            if (space.Owner != player)
                return false;
        }

        return foundAny;
    }
}