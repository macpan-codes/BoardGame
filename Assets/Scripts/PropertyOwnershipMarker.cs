using UnityEngine;
using UnityEngine.UI;

[ExecuteAlways]
public class PropertyOwnershipMarker : MonoBehaviour
{
    private Image markerImage;
    private GameObject visualObject;
    private RectTransform visualRect;
    private BoardPlayer owner;
    private BoardSpace boardSpace;
    private BoardBuildingVisualSettings settings;
    private Sprite roundedSprite;
    private float lastWidth = float.NaN;
    private float lastHeight = float.NaN;
    private Vector2 lastOffset = new Vector2(float.NaN, float.NaN);
    private float lastRotation = float.NaN;
    private bool lastShowMarkers;

    public BoardPlayer Owner => owner;

    private void Awake()
    {
        CacheReferences();
        CreateVisual();
        Hide();
    }

    private void OnEnable()
    {
        CacheReferences();
        CreateVisual();
        ApplyLayout(true);
    }

    private void LateUpdate()
    {
        if (boardSpace == null || settings == null)
            CacheReferences();

        ApplyLayout(false);
    }

    private void CacheReferences()
    {
        if (boardSpace == null)
            boardSpace = GetComponentInParent<BoardSpace>();

        if (settings == null)
            settings = FindFirstObjectByType<BoardBuildingVisualSettings>(FindObjectsInactive.Include);
    }

    private void CreateVisual()
    {
        if (visualObject == null)
        {
            Transform existing = transform.Find("OwnershipColor");
            if (existing != null)
                visualObject = existing.gameObject;
            else
            {
                visualObject = new GameObject("OwnershipColor", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                visualObject.transform.SetParent(transform, false);
            }
        }

        visualRect = visualObject.GetComponent<RectTransform>();
        if (visualRect == null)
            visualRect = visualObject.AddComponent<RectTransform>();

        visualRect.anchorMin = new Vector2(0.5f, 0.5f);
        visualRect.anchorMax = new Vector2(0.5f, 0.5f);
        visualRect.pivot = new Vector2(0.5f, 0.5f);
        visualRect.localScale = Vector3.one;

        markerImage = visualObject.GetComponent<Image>();
        if (markerImage == null)
            markerImage = visualObject.AddComponent<Image>();

        if (roundedSprite == null)
            roundedSprite = CreateRoundedPillSprite();

        markerImage.sprite = roundedSprite;
        markerImage.type = Image.Type.Sliced;
        markerImage.raycastTarget = false;
        markerImage.preserveAspect = false;
    }

    private Sprite CreateRoundedPillSprite()
    {
        const int width = 64;
        const int height = 24;
        const float radius = 11.5f;
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "OwnershipMarkerRoundedPill";
        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cx = Mathf.Clamp(x, radius, width - 1 - radius);
                float cy = Mathf.Clamp(y, radius, height - 1 - radius);
                float dx = x - cx;
                float dy = y - cy;
                bool inside = dx * dx + dy * dy <= radius * radius;
                // The central section is filled; only the two ends are rounded.
                if (x >= radius && x <= width - 1 - radius)
                    inside = true;
                texture.SetPixel(x, y, inside ? Color.white : Color.clear);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f), 100f,
            0, SpriteMeshType.FullRect, new Vector4(12f, 12f, 12f, 12f));
    }

    public void SetOwner(BoardPlayer newOwner)
    {
        owner = newOwner;
        if (owner == null)
        {
            Hide();
            return;
        }

        CreateVisual();
        RefreshColor();
        ApplyLayout(true);
        if (visualObject != null)
            visualObject.SetActive(settings == null || settings.ShowOwnershipMarkers);
    }

    public void RefreshColor()
    {
        if (owner == null)
            return;

        if (markerImage == null)
            CreateVisual();

        if (markerImage != null)
            markerImage.color = owner.TokenColor;
    }

    public void RefreshLayout()
    {
        CacheReferences();
        CreateVisual();
        ApplyLayout(true);
    }

    private void ApplyLayout(bool force)
    {
        if (visualRect == null || settings == null || boardSpace == null)
            return;

        Vector2 size = settings.OwnershipMarkerSize;
        Vector2 offset = settings.GetOwnershipOffset(boardSpace.BoardIndex);
        float rotation = settings.GetOwnershipRotation(boardSpace.BoardIndex);

        if (!force &&
            Mathf.Approximately(lastWidth, size.x) &&
            Mathf.Approximately(lastHeight, size.y) &&
            lastOffset == offset &&
            Mathf.Approximately(lastRotation, rotation) &&
            lastShowMarkers == settings.ShowOwnershipMarkers)
            return;

        visualRect.sizeDelta = size;
        visualRect.anchoredPosition = offset;
        visualRect.localEulerAngles = new Vector3(0f, 0f, rotation);

        lastWidth = size.x;
        lastHeight = size.y;
        lastOffset = offset;
        lastRotation = rotation;
        lastShowMarkers = settings.ShowOwnershipMarkers;

        if (visualObject != null && owner != null)
            visualObject.SetActive(settings.ShowOwnershipMarkers);
    }

    public void Hide()
    {
        owner = null;
        if (visualObject != null)
            visualObject.SetActive(false);
    }
}
