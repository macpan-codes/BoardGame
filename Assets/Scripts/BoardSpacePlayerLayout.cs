using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Editable per-space player-token layout. Add this component to a board-space
/// prefab to customize its patterns in the Inspector. BoardGenerator supplies
/// a default instance at runtime when one is not present.
/// </summary>
[DisallowMultipleComponent]
public class BoardSpacePlayerLayout : MonoBehaviour
{
    [System.Serializable]
    public class Pattern
    {
        [Tooltip("Normalized position inside the space: -0.5 to +0.5 on each axis.")]
        public Vector2[] positions = new Vector2[6];
        [Tooltip("Scale multiplier relative to each token's original scale.")]
        [Range(0.1f, 2f)] public float scale = 1f;
    }

    [Header("Layout Type")]
    [SerializeField] private bool useCornerLayout;
    [SerializeField] private bool detectCornerByBoardIndex = true;
    [SerializeField] private bool useRectTransformBounds = true;

    [Header("Corner Spacing")]
    [SerializeField, Min(0f)] private float sixPlayerCornerRowSpacing = 12f;

    [Header("Regular Spaces: pattern for 1 through 6 players")]
    [SerializeField] private Pattern[] regularPatterns = CreateRegularPatterns();

    [Header("Side Spaces (left/right): pattern for 1 through 6 players")]
    [SerializeField] private Pattern[] sidePatterns = CreateSidePatterns();

    [Header("Corner Spaces: pattern for 1 through 6 players")]
    [SerializeField] private Pattern[] cornerPatterns = CreateCornerPatterns();

    private readonly Dictionary<Transform, Vector3> originalScales = new Dictionary<Transform, Vector3>();
    private readonly Dictionary<Transform, Vector3> originalPositions = new Dictionary<Transform, Vector3>();

    private static Pattern Make(float scale, params Vector2[] positions)
    {
        Vector2[] all = new Vector2[6];
        for (int i = 0; i < all.Length; i++)
            all[i] = i < positions.Length ? positions[i] : Vector2.zero;
        return new Pattern { scale = scale, positions = all };
    }

    private static Pattern[] CreateRegularPatterns()
    {
        return new[]
        {
            Make(1f, new Vector2(0f, 0f)),
            Make(0.90f, new Vector2(0f, 0.12f), new Vector2(0f, -0.12f)),
            Make(0.82f, new Vector2(-0.16f, 0.12f), new Vector2(0.16f, 0.12f), new Vector2(0f, -0.16f)),
            Make(0.72f, new Vector2(-0.16f, 0.14f), new Vector2(0.16f, 0.14f), new Vector2(-0.16f, -0.14f), new Vector2(0.16f, -0.14f)),
            Make(0.64f, new Vector2(-0.16f, 0.16f), new Vector2(0.16f, 0.16f), new Vector2(0f, 0f), new Vector2(-0.16f, -0.16f), new Vector2(0.16f, -0.16f)),
            Make(0.56f, new Vector2(-0.15f, 0.22f), new Vector2(0.15f, 0.22f), new Vector2(-0.15f, 0f), new Vector2(0.15f, 0f), new Vector2(-0.15f, -0.22f), new Vector2(0.15f, -0.22f))
        };
    }

    private static Pattern[] CreateSidePatterns()
    {
        // Side slots use a wider three-column layout for six tokens.
        return new[]
        {
            Make(1f, new Vector2(0f, 0f)),
            Make(0.90f, new Vector2(-0.18f, 0f), new Vector2(0.18f, 0f)),
            Make(0.82f, new Vector2(-0.22f, 0.15f), new Vector2(0.22f, 0.15f), new Vector2(0f, -0.16f)),
            Make(0.72f, new Vector2(-0.22f, 0.16f), new Vector2(0.22f, 0.16f), new Vector2(-0.22f, -0.16f), new Vector2(0.22f, -0.16f)),
            Make(0.64f, new Vector2(-0.22f, 0.20f), new Vector2(0f, 0.20f), new Vector2(0.22f, 0.20f), new Vector2(-0.22f, -0.20f), new Vector2(0.22f, -0.20f)),
            Make(0.56f, new Vector2(-0.24f, 0.20f), new Vector2(0f, 0.20f), new Vector2(0.24f, 0.20f), new Vector2(-0.24f, -0.20f), new Vector2(0f, -0.20f), new Vector2(0.24f, -0.20f))
        };
    }

    private static Pattern[] CreateCornerPatterns()
    {
        return new[]
        {
            Make(2f, new Vector2(0f, 0f)),
            Make(1.76f, new Vector2(-0.12f, 0.12f), new Vector2(0.12f, -0.12f)),
            Make(1.52f, new Vector2(-0.18f, 0.16f), new Vector2(0.18f, 0.16f), new Vector2(0f, -0.16f)),
            Make(1.32f, new Vector2(-0.18f, 0.18f), new Vector2(0.18f, 0.18f), new Vector2(-0.18f, -0.18f), new Vector2(0.18f, -0.18f)),
            Make(1.16f, new Vector2(-0.20f, 0.20f), new Vector2(0.20f, 0.20f), new Vector2(0f, 0f), new Vector2(-0.20f, -0.20f), new Vector2(0.20f, -0.20f)),
            Make(1f, new Vector2(-0.20f, 0.22f), new Vector2(0.20f, 0.22f), new Vector2(-0.20f, 0f), new Vector2(0.20f, 0f), new Vector2(-0.20f, -0.22f), new Vector2(0.20f, -0.22f))
        };
    }

    public void SetTemporaryCornerPattern(bool value)
    {
        useCornerLayout = value;
    }

    public void RefreshLayout()
    {
        BoardSpace space = GetComponent<BoardSpace>();
        List<BoardPlayer> players = new List<BoardPlayer>();

        for (int i = 0; i < transform.childCount; i++)
        {
            BoardPlayer player = transform.GetChild(i).GetComponent<BoardPlayer>();
            if (player != null && player.IsActive)
                players.Add(player);
        }

        players.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));

        if (players.Count == 0)
            return;

        bool corner = useCornerLayout ||
            (detectCornerByBoardIndex && space != null &&
             (space.BoardIndex == 0 || space.BoardIndex == 10 ||
              space.BoardIndex == 20 || space.BoardIndex == 30));

        // Standard 40-space board: left side indices 11-19, right side 31-39.
        bool side = space != null &&
            ((space.BoardIndex >= 11 && space.BoardIndex <= 19) ||
             (space.BoardIndex >= 31 && space.BoardIndex <= 39));

        Pattern pattern = GetPattern(
            corner ? cornerPatterns : side ? sidePatterns : regularPatterns,
            players.Count);
        if (pattern == null)
            return;

        RectTransform spaceRect = transform as RectTransform;
        Vector2 rectSize = spaceRect != null ? spaceRect.rect.size : Vector2.zero;

        for (int i = 0; i < players.Count; i++)
        {
            Transform token = players[i].transform;
            RememberOriginal(token);

            Vector2 normalized = pattern.positions != null && i < pattern.positions.Length
                ? pattern.positions[i] : Vector2.zero;

            Vector3 localPosition = originalPositions[token];
            if (useRectTransformBounds && spaceRect != null)
            {
                localPosition.x = normalized.x * rectSize.x;
                localPosition.y = normalized.y * rectSize.y;

                // Add extra vertical gap between the three rows for six players in corners.
                if (corner && players.Count == 6)
                {
                    int row = i / 2; // 0 = top, 1 = middle, 2 = bottom
                    localPosition.y += row == 0
                        ? sixPlayerCornerRowSpacing
                        : row == 2 ? -sixPlayerCornerRowSpacing : 0f;
                }
            }
            else
            {
                localPosition.x = normalized.x;
                localPosition.y = normalized.y;
            }

            token.localPosition = localPosition;
            token.localScale = originalScales[token] * pattern.scale;
        }
    }

    private Pattern GetPattern(Pattern[] patterns, int count)
    {
        if (patterns == null || patterns.Length == 0)
            return null;
        int index = Mathf.Clamp(count - 1, 0, patterns.Length - 1);
        return patterns[index];
    }

    private void RememberOriginal(Transform token)
    {
        if (!originalScales.ContainsKey(token))
            originalScales[token] = token.localScale;
        if (!originalPositions.ContainsKey(token))
            originalPositions[token] = token.localPosition;
    }

    public void RestoreToken(Transform token)
    {
        if (token == null)
            return;

        if (originalScales.TryGetValue(token, out Vector3 scale))
            token.localScale = scale;
        if (originalPositions.TryGetValue(token, out Vector3 position))
            token.localPosition = position;

        originalScales.Remove(token);
        originalPositions.Remove(token);
    }

    public void RestoreOriginals()
    {
        foreach (KeyValuePair<Transform, Vector3> pair in originalScales)
            if (pair.Key != null) pair.Key.localScale = pair.Value;
        foreach (KeyValuePair<Transform, Vector3> pair in originalPositions)
            if (pair.Key != null) pair.Key.localPosition = pair.Value;
    }
}