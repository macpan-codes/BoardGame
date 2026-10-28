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
        [Range(0.1f, 1f)] public float scale = 1f;
    }

    [Header("Layout Type")]
    [SerializeField] private bool useCornerLayout;
    [SerializeField] private bool detectCornerByBoardIndex = true;
    [SerializeField] private bool useRectTransformBounds = true;

    [Header("Regular Spaces: pattern for 1 through 6 players")]
    [SerializeField] private Pattern[] regularPatterns = CreateRegularPatterns();

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

    private static Pattern[] CreateCornerPatterns()
    {
        return new[]
        {
            Make(1f, new Vector2(0f, 0f)),
            Make(0.88f, new Vector2(-0.12f, 0.12f), new Vector2(0.12f, -0.12f)),
            Make(0.76f, new Vector2(-0.18f, 0.16f), new Vector2(0.18f, 0.16f), new Vector2(0f, -0.16f)),
            Make(0.66f, new Vector2(-0.18f, 0.18f), new Vector2(0.18f, 0.18f), new Vector2(-0.18f, -0.18f), new Vector2(0.18f, -0.18f)),
            Make(0.58f, new Vector2(-0.20f, 0.20f), new Vector2(0.20f, 0.20f), new Vector2(0f, 0f), new Vector2(-0.20f, -0.20f), new Vector2(0.20f, -0.20f)),
            Make(0.50f, new Vector2(-0.20f, 0.22f), new Vector2(0.20f, 0.22f), new Vector2(-0.20f, 0f), new Vector2(0.20f, 0f), new Vector2(-0.20f, -0.22f), new Vector2(0.20f, -0.22f))
        };
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

        Pattern pattern = GetPattern(corner ? cornerPatterns : regularPatterns, players.Count);
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

    public void RestoreOriginals()
    {
        foreach (KeyValuePair<Transform, Vector3> pair in originalScales)
            if (pair.Key != null) pair.Key.localScale = pair.Value;
        foreach (KeyValuePair<Transform, Vector3> pair in originalPositions)
            if (pair.Key != null) pair.Key.localPosition = pair.Value;
    }
}