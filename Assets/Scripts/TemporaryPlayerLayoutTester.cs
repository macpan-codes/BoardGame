using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// TEMPORARY in-game tester for board-space token layouts.
/// Add to a Canvas. Select any BoardSpace, choose 1-6 preview players,
/// and press Apply Preview. This only moves existing tokens temporarily.
/// Disable the component or remove it when finished.
/// </summary>
public class TemporaryPlayerLayoutTester : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private BoardSpace targetSpace;
    [SerializeField, Range(1, 6)] private int previewPlayerCount = 6;
    [SerializeField] private bool useCornerPattern;

    [Header("Preview")]
    [SerializeField] private bool showOnScreenPanel = true;
    [SerializeField] private Vector2 panelPosition = new Vector2(20f, -20f);
    [SerializeField] private Vector2 panelSize = new Vector2(300f, 300f);
    [SerializeField] private bool createRuntimePlayersIfNeeded = true;

    private readonly List<BoardPlayer> allPlayers = new List<BoardPlayer>();
    private readonly List<Transform> originalParents = new List<Transform>();
    private readonly List<Vector3> originalPositions = new List<Vector3>();
    private readonly List<Quaternion> originalRotations = new List<Quaternion>();
    private readonly List<Vector3> originalScales = new List<Vector3>();
    private readonly List<bool> originalActiveStates = new List<bool>();

    private Canvas canvas;
    private GameObject panel;
    private bool previewApplied;
    private const string PanelName = "TEMP_PLAYER_LAYOUT_TESTER";

    private void Awake()
    {
        FindPlayers();
        if (showOnScreenPanel)
            BuildPanel();
    }

    private void OnDestroy()
    {
        RestorePreview();
        if (panel != null)
            Destroy(panel);
    }

    private void FindPlayers()
    {
        allPlayers.Clear();
        BoardPlayer[] found = FindObjectsByType<BoardPlayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        allPlayers.AddRange(found);
        allPlayers.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));
    }

    private void BuildPanel()
    {
        canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
            canvas = FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasObject = new GameObject("TEMP_LAYOUT_TESTER_CANVAS", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObject.GetComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        panel = new GameObject(PanelName, typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvas.transform, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = panelPosition;
        rect.sizeDelta = panelSize;
        panel.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.92f);

        AddLabel("TEMP LAYOUT TESTER", 12, 18, 24, FontStyle.Bold);
        AddLabel("Target: select a BoardSpace below", 12, 48, 18, FontStyle.Normal);
        AddButton("Previous Space", 12, 78, 130, 32, () => SelectSpace(-1));
        AddButton("Next Space", 154, 78, 130, 32, () => SelectSpace(1));
        AddButton("Players: -", 12, 118, 130, 34, () => SetPlayerCount(-1));
        AddButton("Players: +", 154, 118, 130, 34, () => SetPlayerCount(1));
        AddButton("Regular Pattern", 12, 160, 130, 32, () => { useCornerPattern = false; ApplyPreview(); });
        AddButton("Corner Pattern", 154, 160, 130, 32, () => { useCornerPattern = true; ApplyPreview(); });
        AddButton("APPLY PREVIEW", 12, 202, 272, 38, ApplyPreview);
        AddButton("RESTORE / CLEAR", 12, 250, 272, 34, RestorePreview);
        RefreshPanelText();
    }

    private Text AddLabel(string value, int x, int y, int height, FontStyle style)
    {
        GameObject obj = new GameObject(value, typeof(RectTransform), typeof(Text));
        obj.transform.SetParent(panel.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(panelSize.x - x * 2, height);
        Text text = obj.GetComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        text.fontSize = 14;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleLeft;
        text.text = value;
        return text;
    }

    private void AddButton(string label, int x, int y, int width, int height, UnityEngine.Events.UnityAction action)
    {
        GameObject obj = new GameObject(label, typeof(RectTransform), typeof(Image), typeof(Button));
        obj.transform.SetParent(panel.transform, false);
        RectTransform rect = obj.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(x, -y);
        rect.sizeDelta = new Vector2(width, height);
        obj.GetComponent<Image>().color = new Color(0.20f, 0.30f, 0.42f, 1f);
        obj.GetComponent<Button>().onClick.AddListener(action);
        Text text = AddLabel(label, x + 4, y + 2, height - 4, FontStyle.Bold);
        text.transform.SetParent(obj.transform, false);
        RectTransform tr = text.GetComponent<RectTransform>();
        tr.anchorMin = Vector2.zero;
        tr.anchorMax = Vector2.one;
        tr.offsetMin = Vector2.zero;
        tr.offsetMax = Vector2.zero;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
    }

    private BoardSpace[] GetSpaces()
    {
        BoardGenerator generator = FindFirstObjectByType<BoardGenerator>();
        if (generator != null && generator.Spaces != null && generator.Spaces.Length > 0)
            return generator.Spaces;
        return FindObjectsByType<BoardSpace>(FindObjectsInactive.Include, FindObjectsSortMode.None);
    }

    private void SelectSpace(int direction)
    {
        BoardSpace[] spaces = GetSpaces();
        if (spaces.Length == 0) return;
        int index = System.Array.IndexOf(spaces, targetSpace);
        index = index < 0 ? 0 : (index + direction + spaces.Length) % spaces.Length;
        targetSpace = spaces[index];
        RestorePreview();
        RefreshPanelText();
    }

    private void SetPlayerCount(int delta)
    {
        previewPlayerCount = Mathf.Clamp(previewPlayerCount + delta, 1, 6);
        RefreshPanelText();
        if (previewApplied) ApplyPreview();
    }

    private void RefreshPanelText()
    {
        if (panel == null) return;
        Text[] labels = panel.GetComponentsInChildren<Text>(true);
        foreach (Text label in labels)
        {
            if (label.text.StartsWith("Target:"))
                label.text = targetSpace == null ? "Target: choose a board space" : "Target: " + targetSpace.name;
            if (label.text.StartsWith("Players:"))
                label.text = "Players: " + previewPlayerCount;
        }
    }

    public void ApplyPreview()
    {
        if (targetSpace == null)
        {
            BoardSpace[] spaces = GetSpaces();
            if (spaces.Length == 0) return;
            targetSpace = spaces[0];
        }

        RestorePreview();
        FindPlayers();

        BoardPlayer[] onTarget = allPlayers.FindAll(p => p != null && p.transform.parent == targetSpace.transform).ToArray();
        List<BoardPlayer> selected = new List<BoardPlayer>(onTarget);

        if (selected.Count < previewPlayerCount && createRuntimePlayersIfNeeded)
        {
            foreach (BoardPlayer player in allPlayers)
            {
                if (player == null || selected.Contains(player) || player.transform.parent == targetSpace.transform)
                    continue;
                selected.Add(player);
                if (selected.Count >= previewPlayerCount) break;
            }
        }

        if (selected.Count == 0)
        {
            Debug.LogWarning("TemporaryPlayerLayoutTester: No BoardPlayer tokens found in the scene.");
            return;
        }

        SaveOriginalState();
        previewApplied = true;

        BoardSpacePlayerLayout layout = targetSpace.GetComponent<BoardSpacePlayerLayout>();
        if (layout == null) layout = targetSpace.gameObject.AddComponent<BoardSpacePlayerLayout>();

        // Temporarily move chosen existing tokens to target, retaining their original state for restoration.
        for (int i = 0; i < selected.Count; i++)
        {
            BoardPlayer player = selected[i];
            if (player == null) continue;
            player.gameObject.SetActive(i < previewPlayerCount);
            player.transform.SetParent(targetSpace.transform, false);
        }

        layout.SetTemporaryCornerPattern(useCornerPattern);
        layout.RefreshLayout();
        RefreshPanelText();
        Debug.Log("Temporary layout preview: " + Mathf.Min(selected.Count, previewPlayerCount) + " player(s) on " + targetSpace.name + ". Press RESTORE / CLEAR to undo.");
    }

    private void SaveOriginalState()
    {
        originalParents.Clear();
        originalPositions.Clear();
        originalRotations.Clear();
        originalScales.Clear();
        originalActiveStates.Clear();

        FindPlayers();
        foreach (BoardPlayer player in allPlayers)
        {
            if (player == null) continue;
            originalParents.Add(player.transform.parent);
            originalPositions.Add(player.transform.localPosition);
            originalRotations.Add(player.transform.localRotation);
            originalScales.Add(player.transform.localScale);
            originalActiveStates.Add(player.gameObject.activeSelf);
        }
    }

    public void RestorePreview()
    {
        if (!previewApplied) return;
        FindPlayers();
        for (int i = 0; i < allPlayers.Count && i < originalParents.Count; i++)
        {
            BoardPlayer player = allPlayers[i];
            if (player == null) continue;
            player.transform.SetParent(originalParents[i], false);
            player.transform.localPosition = originalPositions[i];
            player.transform.localRotation = originalRotations[i];
            player.transform.localScale = originalScales[i];
            player.gameObject.SetActive(originalActiveStates[i]);
        }
        previewApplied = false;
        RefreshPanelText();
    }
}