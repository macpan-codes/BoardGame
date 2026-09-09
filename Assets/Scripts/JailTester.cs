#if UNITY_EDITOR || DEVELOPMENT_BUILD
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Development-only utility for testing the existing Jail APIs.
/// Add this component to an empty GameObject while testing.
/// </summary>
public class JailTester : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameManager gameManager;
    [SerializeField] private DiceManager diceManager;

    [Header("Keyboard")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F6;
    [SerializeField] private bool openOnStart;

    private GameObject panel;
    private TMP_Text currentPlayerText;
    private TMP_Text jailStatusText;
    private TMP_Text jailTurnsText;
    private TMP_Text messageText;

    private void Awake()
    {
        FindReferences();
    }

    private void Start()
    {
        BuildTesterUI();
        RefreshStatus();
        SetVisible(openOnStart);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            SetVisible(!IsVisible());
        }
    }

    public void SendToJail()
    {
        if (!TryGetCurrentPlayer(out BoardPlayer player))
            return;

        player.SendToJail();
        SetMessage($"Sent {player.PlayerName} to Jail.");
        RefreshStatus();
    }

    public void SkipOneJailTurn()
    {
        if (!TryGetJailedPlayer(out BoardPlayer player))
            return;

        player.DecreaseJailTurn();
        NotifyAndLogJailStatus(player, "Skipped one Jail turn");
        RefreshStatus();
    }

    public void SkipTwoJailTurns()
    {
        if (!TryGetJailedPlayer(out BoardPlayer player))
            return;

        for (int i = 0; i < 2 && player.IsInJail; i++)
        {
            player.DecreaseJailTurn();
        }

        NotifyAndLogJailStatus(player, "Skipped up to two Jail turns");
        RefreshStatus();
    }

    public void ReleaseFromJail()
    {
        if (!TryGetCurrentPlayer(out BoardPlayer player))
            return;

        player.LeaveJail();
        SetMessage($"Released {player.PlayerName} from Jail.");
        Debug.Log($"JailTester: Released {player.PlayerName} from Jail.");
        RefreshStatus();
    }

    public void RefreshStatus()
    {
        FindReferences();

        BoardPlayer player =
            gameManager != null
                ? gameManager.CurrentPlayer
                : null;

        if (currentPlayerText != null)
        {
            currentPlayerText.text = player != null
                ? $"CURRENT PLAYER: {player.PlayerName}"
                : "CURRENT PLAYER: NONE";
        }

        if (jailStatusText != null)
        {
            jailStatusText.text = player != null
                ? $"IS IN JAIL: {player.IsInJail}"
                : "IS IN JAIL: N/A";
        }

        if (jailTurnsText != null)
        {
            jailTurnsText.text = player != null
                ? $"JAIL TURNS REMAINING: {player.JailTurnsRemaining}"
                : "JAIL TURNS REMAINING: N/A";
        }
    }

    public void TestNextRoll()
    {
        if (!TryGetCurrentPlayer(out BoardPlayer player))
            return;

        if (!player.IsInJail)
        {
            SetMessage("Current player is not in Jail. Test Next Roll was not started.");
            Debug.LogWarning(
                "JailTester: Current player is not in Jail."
            );
            return;
        }

        if (diceManager == null)
        {
            SetMessage("DiceManager not found.");
            Debug.LogWarning("JailTester: DiceManager not found.");
            return;
        }

        SetVisible(false);
        diceManager.RollDice();
    }

    public void Toggle()
    {
        SetVisible(!IsVisible());
    }

    private bool TryGetCurrentPlayer(out BoardPlayer player)
    {
        FindReferences();
        player = gameManager != null
            ? gameManager.CurrentPlayer
            : null;

        if (player != null)
            return true;

        SetMessage(
            gameManager == null
                ? "GameManager not found."
                : "Current player not found."
        );

        return false;
    }

    private bool TryGetJailedPlayer(out BoardPlayer player)
    {
        if (!TryGetCurrentPlayer(out player))
            return false;

        if (player.IsInJail)
            return true;

        SetMessage($"{player.PlayerName} is not currently in Jail.");
        return false;
    }

    private void FindReferences()
    {
        if (gameManager == null)
        {
            gameManager =
                FindFirstObjectByType<GameManager>(
                    FindObjectsInactive.Include
                );
        }

        if (diceManager == null)
        {
            diceManager =
                FindFirstObjectByType<DiceManager>(
                    FindObjectsInactive.Include
                );
        }
    }

    private void NotifyAndLogJailStatus(
        BoardPlayer player,
        string action)
    {
        string status = player.IsInJail
            ? $"{player.PlayerName.ToUpperInvariant()} IS IN JAIL - " +
              $"{player.JailTurnsRemaining} TURN(S) REMAINING"
            : $"{player.PlayerName.ToUpperInvariant()} IS RELEASED FROM JAIL";

        GameNotificationUI.Show(status);
        Debug.Log($"JailTester: {action}. {status}");
        SetMessage(status);
    }

    private void BuildTesterUI()
    {
        GameObject canvasObject = new GameObject(
            "JailTesterCanvas",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster)
        );

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 600;

        CanvasScaler scaler =
            canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode =
            CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode =
            CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        panel = new GameObject(
            "JailTesterPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup)
        );
        panel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect =
            panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.anchoredPosition = Vector2.zero;
        panelRect.sizeDelta = new Vector2(650f, 520f);

        panel.GetComponent<Image>().color =
            new Color(0.04f, 0.08f, 0.14f, 0.98f);

        VerticalLayoutGroup layout =
            panel.GetComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(20, 20, 20, 20);
        layout.spacing = 8f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        GameObject header = new GameObject(
            "Header",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup)
        );
        header.transform.SetParent(panel.transform, false);
        header.GetComponent<Image>().color =
            new Color(0.08f, 0.20f, 0.38f, 1f);

        LayoutElement headerLayout =
            header.AddComponent<LayoutElement>();
        headerLayout.preferredHeight = 58f;

        VerticalLayoutGroup headerGroup =
            header.GetComponent<VerticalLayoutGroup>();
        headerGroup.padding = new RectOffset(14, 14, 8, 8);
        headerGroup.childAlignment = TextAnchor.MiddleCenter;
        headerGroup.childControlWidth = true;
        headerGroup.childControlHeight = true;
        headerGroup.childForceExpandWidth = true;
        headerGroup.childForceExpandHeight = false;

        TMP_Text title = CreateText(
            header.transform,
            "JAIL TESTER",
            34f,
            new Color(1f, 0.80f, 0.24f, 1f),
            40f
        );
        title.fontStyle = FontStyles.Bold;

        GameObject statusPanel = new GameObject(
            "StatusPanel",
            typeof(RectTransform),
            typeof(Image),
            typeof(VerticalLayoutGroup)
        );
        statusPanel.transform.SetParent(panel.transform, false);
        statusPanel.GetComponent<Image>().color =
            new Color(0.02f, 0.03f, 0.05f, 0.85f);

        LayoutElement statusLayout =
            statusPanel.AddComponent<LayoutElement>();
        statusLayout.preferredHeight = 112f;

        VerticalLayoutGroup statusGroup =
            statusPanel.GetComponent<VerticalLayoutGroup>();
        statusGroup.padding = new RectOffset(12, 12, 6, 6);
        statusGroup.spacing = 0f;
        statusGroup.childAlignment = TextAnchor.MiddleCenter;
        statusGroup.childControlWidth = true;
        statusGroup.childControlHeight = true;
        statusGroup.childForceExpandWidth = true;
        statusGroup.childForceExpandHeight = false;

        currentPlayerText = CreateText(
            statusPanel.transform, "", 26f, Color.white, 32f
        );
        jailStatusText = CreateText(
            statusPanel.transform, "", 26f, Color.white, 32f
        );
        jailTurnsText = CreateText(
            statusPanel.transform, "", 26f, Color.white, 32f
        );

        Transform firstRow = CreateButtonRow(panel.transform);
        Button sendButton = CreateButton(firstRow, "SEND TO JAIL");
        sendButton.onClick.AddListener(SendToJail);
        Button skipOneButton = CreateButton(firstRow, "SKIP 1 JAIL TURN");
        skipOneButton.onClick.AddListener(SkipOneJailTurn);

        Transform secondRow = CreateButtonRow(panel.transform);
        Button skipTwoButton = CreateButton(secondRow, "SKIP 2 JAIL TURNS");
        skipTwoButton.onClick.AddListener(SkipTwoJailTurns);
        Button releaseButton = CreateButton(secondRow, "RELEASE FROM JAIL");
        releaseButton.onClick.AddListener(ReleaseFromJail);

        Transform thirdRow = CreateButtonRow(panel.transform);
        Button testRollButton = CreateButton(thirdRow, "TEST NEXT ROLL");
        testRollButton.onClick.AddListener(TestNextRoll);
        Button refreshButton = CreateButton(thirdRow, "REFRESH STATUS");
        refreshButton.onClick.AddListener(RefreshStatus);

        messageText = CreateText(
            panel.transform,
            "F6 toggles this development tester.",
            16f,
            new Color(0.72f, 0.80f, 0.90f, 1f),
            24f
        );

        Button closeButton = CreateButton(
            panel.transform, "CLOSE TESTER (F6)", 46f
        );
        closeButton.onClick.AddListener(() => SetVisible(false));
    }

    private TMP_Text CreateText(
        Transform parent,
        string value,
        float size,
        Color color,
        float preferredHeight = -1f)
    {
        GameObject textObject = new GameObject(
            "Text",
            typeof(RectTransform)
        );
        textObject.transform.SetParent(parent, false);

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = size;
        text.color = color;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;

        LayoutElement layout = textObject.AddComponent<LayoutElement>();
        layout.preferredHeight = preferredHeight > 0f
            ? preferredHeight
            : size + 18f;

        return text;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        float height = 54f)
    {
        GameObject buttonObject = new GameObject(
            label + "Button",
            typeof(RectTransform),
            typeof(Image),
            typeof(Button)
        );
        buttonObject.transform.SetParent(parent, false);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.08f, 0.30f, 0.62f, 1f);

        Button button = buttonObject.GetComponent<Button>();
        TMP_Text labelText = CreateText(
            buttonObject.transform,
            label,
            23f,
            Color.white,
            height
        );
        labelText.fontStyle = FontStyles.Bold;

        RectTransform labelRect =
            labelText.GetComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        LayoutElement layout = buttonObject.AddComponent<LayoutElement>();
        layout.preferredHeight = height;

        return button;
    }

    private Transform CreateButtonRow(Transform parent)
    {
        GameObject row = new GameObject(
            "ButtonRow",
            typeof(RectTransform),
            typeof(HorizontalLayoutGroup)
        );
        row.transform.SetParent(parent, false);

        HorizontalLayoutGroup layout =
            row.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 10f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        LayoutElement rowLayout = row.AddComponent<LayoutElement>();
        rowLayout.preferredHeight = 54f;

        return row.transform;
    }

    private bool IsVisible()
    {
        return panel != null && panel.activeSelf;
    }

    private void SetVisible(bool visible)
    {
        if (panel != null)
            panel.SetActive(visible);

        if (visible)
            RefreshStatus();
    }

    private void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message;
    }
}
#endif
