#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// TEMPORARY visual-only layout tester. Delete this file after testing.
/// Open: Board Game > Tests > Token Layout Preview.
/// Requires Play Mode and a local match configured with enough active players.
/// </summary>
public sealed class TemporaryTokenLayoutPreview : EditorWindow
{
    private struct SavedToken
    {
        public BoardPlayer player;
        public Transform parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
        public int siblingIndex;
    }

    private readonly List<SavedToken> originals = new List<SavedToken>();
    private int spaceIndex;
    private int tokenCount = 6;
    private bool previewing;
    private Vector2 scroll;

    [MenuItem("Board Game/Tests/Token Layout Preview")]
    private static void OpenWindow()
    {
        GetWindow<TemporaryTokenLayoutPreview>("Token Layout Preview");
    }

    private void OnEnable()
    {
        EditorApplication.playModeStateChanged += OnPlayModeChanged;
    }

    private void OnDisable()
    {
        if (Application.isPlaying)
            Restore();
        EditorApplication.playModeStateChanged -= OnPlayModeChanged;
    }

    private void OnPlayModeChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.ExitingPlayMode ||
            change == PlayModeStateChange.EnteredEditMode)
        {
            if (Application.isPlaying)
                Restore();
            originals.Clear();
            previewing = false;
        }
    }

    private void OnGUI()
    {
        scroll = EditorGUILayout.BeginScrollView(scroll);
        EditorGUILayout.HelpBox(
            "Temporary visual tester. Start a LOCAL match with 6 players, then use this window while in Play Mode. " +
            "No dice rolls, landing logic, rent or money changes are triggered. " +
            "Press Restore before resuming normal gameplay.", MessageType.Info);

        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode and open MainScene first.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        BoardGenerator board = FindFirstObjectByType<BoardGenerator>();
        if (board == null)
        {
            EditorGUILayout.HelpBox("BoardGenerator was not found in the active scene.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        BoardSpace[] spaces = board.Spaces;
        if (spaces == null || spaces.Length == 0)
        {
            EditorGUILayout.HelpBox("No board spaces detected.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        List<BoardSpace> ordered = new List<BoardSpace>();
        foreach (BoardSpace space in spaces)
            if (space != null && space.BoardIndex >= 0)
                ordered.Add(space);
        ordered.Sort((a, b) => a.BoardIndex.CompareTo(b.BoardIndex));

        if (ordered.Count == 0)
        {
            EditorGUILayout.HelpBox("No indexed BoardSpace objects found.", MessageType.Warning);
            EditorGUILayout.EndScrollView();
            return;
        }

        string[] options = new string[ordered.Count];
        int selected = 0;
        for (int i = 0; i < ordered.Count; i++)
        {
            options[i] = ordered[i].BoardIndex.ToString("00") + " - " + ordered[i].SpaceName;
            if (ordered[i].BoardIndex == spaceIndex)
                selected = i;
        }

        selected = EditorGUILayout.Popup("Target board space", selected, options);
        spaceIndex = ordered[selected].BoardIndex;
        tokenCount = EditorGUILayout.IntSlider("Tokens in space", tokenCount, 1, 6);

        BoardPlayer[] found = FindObjectsByType<BoardPlayer>(
            FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        List<BoardPlayer> active = new List<BoardPlayer>();
        foreach (BoardPlayer p in found)
            if (p != null && p.IsActive)
                active.Add(p);
        active.Sort((a, b) => a.PlayerNumber.CompareTo(b.PlayerNumber));

        EditorGUILayout.LabelField("Active player tokens", active.Count.ToString());
        if (active.Count < tokenCount)
            EditorGUILayout.HelpBox(
                "Only " + active.Count + " active tokens exist. Start a local game with 6 players " +
                "to preview all six (bots are fine).", MessageType.Warning);

        using (new EditorGUI.DisabledScope(active.Count < tokenCount))
        {
            if (GUILayout.Button("PREVIEW TOKENS IN SELECTED SPACE", GUILayout.Height(36)))
                Preview(ordered[selected], active);
        }

        if (GUILayout.Button("Restore original token positions", GUILayout.Height(28)))
            Restore();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Quick presets", EditorStyles.boldLabel);
        for (int count = 1; count <= 6; count++)
        {
            int n = count;
            using (new EditorGUI.DisabledScope(active.Count < n))
            {
                if (GUILayout.Button(n + " token" + (n == 1 ? "" : "s")))
                {
                    tokenCount = n;
                    Preview(ordered[selected], active);
                }
            }
        }

        if (previewing)
            EditorGUILayout.HelpBox(
                "PREVIEW ACTIVE. Tokens are visually repositioned only. " +
                "Restore before clicking any in-game actions.", MessageType.Warning);

        EditorGUILayout.EndScrollView();
    }

    private void Preview(BoardSpace target, List<BoardPlayer> active)
    {
        Restore();
        if (target == null || active.Count < tokenCount)
            return;

        for (int i = 0; i < tokenCount; i++)
        {
            BoardPlayer player = active[i];
            Transform t = player.transform;
            BoardSpacePlayerLayout previous = t.parent != null
                ? t.parent.GetComponent<BoardSpacePlayerLayout>() : null;
            if (previous != null)
                previous.RestoreToken(t);

            originals.Add(new SavedToken
            {
                player = player,
                parent = t.parent,
                localPosition = t.localPosition,
                localRotation = t.localRotation,
                localScale = t.localScale,
                siblingIndex = t.GetSiblingIndex()
            });

            t.SetParent(target.transform, false);
            t.localPosition = Vector3.zero;
            t.localRotation = Quaternion.identity;
        }

        RefreshAllLayouts();
        previewing = true;
        SceneView.RepaintAll();
        Repaint();
    }

    private void Restore()
    {
        if (!previewing && originals.Count == 0)
            return;

        foreach (SavedToken saved in originals)
        {
            if (saved.player == null)
                continue;
            Transform t = saved.player.transform;
            BoardSpacePlayerLayout previewLayout = t.parent != null
                ? t.parent.GetComponent<BoardSpacePlayerLayout>() : null;
            if (previewLayout != null)
                previewLayout.RestoreToken(t);
            if (saved.parent == null)
                continue;
            t.SetParent(saved.parent, false);
            t.localPosition = saved.localPosition;
            t.localRotation = saved.localRotation;
            t.localScale = saved.localScale;
            t.SetSiblingIndex(Mathf.Clamp(saved.siblingIndex, 0, saved.parent.childCount - 1));
        }
        originals.Clear();
        previewing = false;
        RefreshAllLayouts();
        SceneView.RepaintAll();
        Repaint();
    }

    private static void RefreshAllLayouts()
    {
        BoardSpacePlayerLayout[] layouts = FindObjectsByType<BoardSpacePlayerLayout>(
            FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (BoardSpacePlayerLayout layout in layouts)
            if (layout != null)
                layout.RefreshLayout();
    }
}
#endif
