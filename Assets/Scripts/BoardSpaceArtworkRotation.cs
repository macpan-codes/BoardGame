using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Rotates only the artwork Image inside a board space. The BoardSpace object
/// and its root/background Image are deliberately never rotated. Assign the
/// specific artwork Image in the Inspector if automatic child selection is not correct.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class BoardSpaceArtworkRotation : MonoBehaviour
{
    [SerializeField] private Image artworkImage;

    private BoardSpace boardSpace;
    private float lastAppliedRotation = float.NaN;
    private int lastAppliedIndex = int.MinValue;

    private void OnEnable()
    {
        CacheBoardSpace();
        RefreshRotation();
    }

    private void LateUpdate()
    {
        if (boardSpace == null)
            CacheBoardSpace();

        if (boardSpace != null &&
            (lastAppliedIndex != boardSpace.BoardIndex ||
             !Mathf.Approximately(lastAppliedRotation, GetDesiredRotation())))
        {
            RefreshRotation();
        }
    }

    public void RefreshRotation()
    {
        CacheBoardSpace();

        if (boardSpace == null)
            return;

        if (artworkImage == null || artworkImage.transform == transform)
            artworkImage = FindArtworkImage();

        if (artworkImage == null)
            return;

        float rotation = GetDesiredRotation();
        Vector3 angles = artworkImage.rectTransform.localEulerAngles;
        angles.z = rotation;
        artworkImage.rectTransform.localEulerAngles = angles;

        lastAppliedIndex = boardSpace.BoardIndex;
        lastAppliedRotation = rotation;
    }

    private float GetDesiredRotation()
    {
        BoardBuildingVisualSettings settings =
            FindFirstObjectByType<BoardBuildingVisualSettings>(FindObjectsInactive.Include);

        return settings != null
            ? settings.GetSpaceArtworkRotation(boardSpace != null ? boardSpace.BoardIndex : -1)
            : GetDefaultRotation(boardSpace != null ? boardSpace.BoardIndex : -1);
    }

    private static float GetDefaultRotation(int index)
    {
        if (index >= 11 && index <= 19) return 90f;
        if (index >= 30 && index <= 39) return -90f;
        return 0f;
    }

    private void CacheBoardSpace()
    {
        if (boardSpace == null)
            boardSpace = GetComponent<BoardSpace>();
    }

    private Image FindArtworkImage()
    {
        // Never rotate the root Image: it may be the slot background/placeholder.
        Transform buildingVisuals = transform.Find("BuildingVisuals");
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null || image.transform == transform)
                continue;

            if (buildingVisuals != null &&
                (image.transform == buildingVisuals || image.transform.IsChildOf(buildingVisuals)))
                continue;

            // Prefer an Image directly inside the slot, not nested UI such as labels.
            if (image.transform.parent == transform)
                return image;
        }

        return null;
    }

    public void SetArtworkImage(Image image)
    {
        // Ignore the root Image to protect the slot placeholder.
        artworkImage = image != null && image.transform != transform ? image : null;
        RefreshRotation();
    }
}
