using UnityEngine;
using UnityEngine.UI;

public class BoardGenerator : MonoBehaviour
{
    [Header("Board")]
    [SerializeField]
    private Transform boardRoot;

    [SerializeField]
    private BoardSpace[] spaces;

    public BoardSpace[] Spaces =>
        spaces;

    private void Awake()
    {
        RefreshSpaces();
        SetupBoardIndexes();
        SetupBoardInteractions();
    }

    // ============================================================
    // REFRESH
    // ============================================================

    [ContextMenu("Refresh Board Spaces")]
    public void RefreshSpaces()
    {
        Transform root =
            boardRoot != null
                ? boardRoot
                : transform;

        spaces =
            root.GetComponentsInChildren<BoardSpace>(
                true
            );

        Debug.Log(
            $"BoardGenerator: Found " +
            $"{spaces.Length} board spaces."
        );
    }

    // ============================================================
    // BOARD INDEX
    // ============================================================

    private void SetupBoardIndexes()
    {
        if (spaces == null)
            return;

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            int index =
                ExtractSpaceIndex(
                    space.gameObject.name
                );

            if (index >= 0 &&
                index < 40)
            {
                space.SetBoardIndex(index);
            }
        }
    }

    private int ExtractSpaceIndex(
        string objectName)
    {
        if (string.IsNullOrWhiteSpace(objectName))
            return -1;

        if (!objectName.StartsWith("Space_"))
            return -1;

        string number =
            objectName.Substring(6);

        if (int.TryParse(
                number,
                out int index))
        {
            return index;
        }

        return -1;
    }

    // ============================================================
    // BOARD INTERACTIONS
    // ============================================================

    private void SetupBoardInteractions()
    {
        if (spaces == null)
            return;

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            SetupClickHandler(space);
            SetupOwnershipMarker(space);
        }
    }

    // ============================================================
    // CLICK HANDLER
    // ============================================================

    private void SetupClickHandler(
        BoardSpace space)
    {
        BoardSpaceClickHandler clickHandler =
            space.GetComponent<
                BoardSpaceClickHandler
            >();

        if (clickHandler == null)
        {
            clickHandler =
                space.gameObject.AddComponent<
                    BoardSpaceClickHandler
                >();
        }

        Image image =
            space.GetComponent<Image>();

        if (image != null)
        {
            image.raycastTarget = true;
        }
    }

    // ============================================================
    // OWNERSHIP MARKER
    // ============================================================

    private void SetupOwnershipMarker(
        BoardSpace space)
    {
        bool needsMarker =
            space.IsProperty ||
            space.IsRailroad ||
            space.IsUtility;

        if (!needsMarker)
            return;

        PropertyOwnershipMarker marker =
            FindChildOwnershipMarker(
                space.transform
            );

        if (marker == null)
        {
            GameObject markerObject =
                new GameObject(
                    "OwnershipMarker"
                );

            markerObject.transform.SetParent(
                space.transform,
                false
            );

            RectTransform rect =
                markerObject.AddComponent<
                    RectTransform
                >();

            rect.anchorMin =
                new Vector2(0f, 0f);

            rect.anchorMax =
                new Vector2(1f, 1f);

            rect.offsetMin =
                Vector2.zero;

            rect.offsetMax =
                Vector2.zero;

            marker =
                markerObject.AddComponent<
                    PropertyOwnershipMarker
                >();
        }

        marker.Hide();

        space.AssignOwnershipMarker(
            marker
        );

        space.RefreshOwnershipMarker();
    }

    private PropertyOwnershipMarker
        FindChildOwnershipMarker(
            Transform parent)
    {
        if (parent == null)
            return null;

        PropertyOwnershipMarker marker =
            parent.GetComponentInChildren<
                PropertyOwnershipMarker
            >(true);

        return marker;
    }

    // ============================================================
    // GET SPACE
    // ============================================================

    public BoardSpace GetSpace(
        int index)
    {
        if (spaces == null ||
            spaces.Length == 0)
        {
            RefreshSpaces();
        }

        foreach (BoardSpace space in spaces)
        {
            if (space == null)
                continue;

            if (space.BoardIndex == index)
                return space;
        }

        return null;
    }

    // ============================================================
    // VALIDATION
    // ============================================================

    [ContextMenu("Validate Board")]
    public void ValidateBoard()
    {
        RefreshSpaces();
        SetupBoardIndexes();

        if (spaces == null ||
            spaces.Length != 40)
        {
            Debug.LogError(
                $"BOARD VALIDATION FAILED: " +
                $"Expected 40 spaces, found " +
                $"{(spaces == null ? 0 : spaces.Length)}."
            );

            return;
        }

        bool valid = true;

        for (int i = 0; i < 40; i++)
        {
            BoardSpace space =
                GetSpace(i);

            if (space == null)
            {
                Debug.LogError(
                    $"Missing board space {i}."
                );

                valid = false;
                continue;
            }

            if (space.BoardIndex != i)
            {
                Debug.LogWarning(
                    $"{space.name}: Board Index is " +
                    $"{space.BoardIndex}, expected {i}."
                );

                valid = false;
            }
        }

        if (valid)
        {
            Debug.Log(
                "BOARD VALIDATION SUCCESS: " +
                "All 40 Monopoly board spaces are present."
            );
        }
        else
        {
            Debug.LogWarning(
                "BOARD VALIDATION FINISHED WITH WARNINGS."
            );
        }
    }
}