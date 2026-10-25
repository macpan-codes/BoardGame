using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Keeps the BoardSpace GameObject/layout fixed while rotating its displayed
/// artwork. If the artwork is currently the Image on the slot itself, this
/// component moves that appearance to a child Image automatically.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class BoardSpaceArtworkRotation : MonoBehaviour
{
    private const string ArtworkChildName = "RotatableArtwork";

    private BoardSpace boardSpace;
    private Image artworkImage;
    private float lastAppliedRotation = float.NaN;
    private int lastAppliedIndex = int.MinValue;

    private void OnEnable()
    {
        CacheBoardSpace();
        EnsureArtworkChild();
        RefreshRotation();
    }

    private void LateUpdate()
    {
        if (boardSpace == null)
            CacheBoardSpace();

        if (artworkImage == null)
            EnsureArtworkChild();

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
        EnsureArtworkChild();

        if (boardSpace == null || artworkImage == null)
            return;

        Vector3 angles = artworkImage.rectTransform.localEulerAngles;
        angles.z = GetDesiredRotation();
        artworkImage.rectTransform.localEulerAngles = angles;

        lastAppliedIndex = boardSpace.BoardIndex;
        lastAppliedRotation = GetDesiredRotation();
    }

    private void EnsureArtworkChild()
    {
        if (artworkImage != null)
            return;

        Transform existingChild = transform.Find(ArtworkChildName);
        if (existingChild != null)
        {
            artworkImage = existingChild.GetComponent<Image>();
            if (artworkImage != null)
                return;
        }

        // The root Image is often the property artwork itself. Move its visual
        // settings to a child so the slot/layout transform can stay unrotated.
        Image rootImage = GetComponent<Image>();
        if (rootImage == null || rootImage.sprite == null)
        {
            // If a child artwork Image already exists, use the first direct child.
            foreach (Image candidate in GetComponentsInChildren<Image>(true))
            {
                if (candidate != null && candidate.transform != transform &&
                    candidate.transform.parent == transform &&
                    candidate.name != "BuildingVisuals")
                {
                    artworkImage = candidate;
                    return;
                }
            }
            return;
        }

        GameObject child = new GameObject(ArtworkChildName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        child.layer = gameObject.layer;
        child.transform.SetParent(transform, false);

        RectTransform childRect = child.GetComponent<RectTransform>();
        childRect.anchorMin = Vector2.zero;
        childRect.anchorMax = Vector2.one;
        childRect.offsetMin = Vector2.zero;
        childRect.offsetMax = Vector2.zero;
        childRect.pivot = rootImage.rectTransform.pivot;
        childRect.localScale = Vector3.one;

        artworkImage = child.GetComponent<Image>();
        artworkImage.sprite = rootImage.sprite;
        artworkImage.overrideSprite = rootImage.overrideSprite;
        artworkImage.type = rootImage.type;
        artworkImage.preserveAspect = rootImage.preserveAspect;
        artworkImage.fillCenter = rootImage.fillCenter;
        artworkImage.fillMethod = rootImage.fillMethod;
        artworkImage.fillAmount = rootImage.fillAmount;
        artworkImage.fillClockwise = rootImage.fillClockwise;
        artworkImage.fillOrigin = rootImage.fillOrigin;
        artworkImage.color = rootImage.color;
        artworkImage.material = rootImage.material;
        artworkImage.raycastTarget = rootImage.raycastTarget;
        artworkImage.maskable = rootImage.maskable;

        // Keep the original Image component for the slot, but hide its artwork.
        Color rootColor = rootImage.color;
        rootColor.a = 0f;
        rootImage.color = rootColor;
        rootImage.sprite = null;
        rootImage.overrideSprite = null;
        rootImage.raycastTarget = false;
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
}
