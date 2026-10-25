using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Applies the shared board-side rotation to a space's artwork Image.
/// Assign Artwork Image in the Inspector when the artwork is not the first
/// child Image. The board slot and its text/other UI are not rotated.
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

        if (artworkImage == null)
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
        Image[] images = GetComponentsInChildren<Image>(true);

        foreach (Image image in images)
        {
            if (image == null || image.transform == transform)
                continue;

            if (image.transform.name == "BuildingVisuals" ||
                image.transform.IsChildOf(transform.Find("BuildingVisuals")))
                continue;

            // Prefer a direct child Image so only the space artwork rotates.
            if (image.transform.parent == transform)
                return image;
        }

        return null;
    }

    public void SetArtworkImage(Image image)
    {
        artworkImage = image;
        RefreshRotation();
    }
}
