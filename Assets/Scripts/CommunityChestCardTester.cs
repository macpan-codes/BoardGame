using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CommunityChestCardTester : MonoBehaviour
{
    [SerializeField] private CommunityChestCardManager manager;
    [SerializeField] private KeyCode toggleKey = KeyCode.F7;
    [SerializeField] private bool openOnStart = true;

    private GameObject window;
    private TMP_Text status;
    private void Awake()
    {
        if (manager == null)
            manager =
                FindFirstObjectByType<CommunityChestCardManager>(
                    FindObjectsInactive.Include
                );
    }

    private void Start()
    {
        BuildTester();

        if (manager == null)
        {
            manager =
                FindFirstObjectByType<CommunityChestCardManager>(
                    FindObjectsInactive.Include
                );
        }

        RefreshStatus();

        window.SetActive(
            openOnStart
        );
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
            window.SetActive(
                !window.activeSelf
            );
    }

    private void BuildTester()
    {
        Canvas canvas =
            FindFirstObjectByType<Canvas>();

        if (canvas == null)
        {
            GameObject canvasGo =
                new GameObject(
                    "CommunityChestTesterCanvas",
                    typeof(Canvas),
                    typeof(CanvasScaler),
                    typeof(GraphicRaycaster)
                );

            canvas =
                canvasGo.GetComponent<Canvas>();

            canvas.renderMode =
                RenderMode.ScreenSpaceOverlay;

            canvas.GetComponent<CanvasScaler>()
                .uiScaleMode =
                    CanvasScaler.ScaleMode.ScaleWithScreenSize;
        }

        window =
            new GameObject(
                "CommunityChestTester",
                typeof(RectTransform),
                typeof(Image),
                typeof(VerticalLayoutGroup)
            );

        window.transform.SetParent(
            canvas.transform,
            false
        );

        RectTransform rect =
            window.GetComponent<RectTransform>();

        rect.anchorMin =
            new Vector2(0.5f, 0.5f);

        rect.anchorMax =
            new Vector2(0.5f, 0.5f);

        rect.pivot =
            new Vector2(0.5f, 0.5f);

        rect.sizeDelta =
            new Vector2(820f, 850f);

        window.GetComponent<Image>().color =
            new Color(
                0.02f,
                0.06f,
                0.12f,
                0.98f
            );

        VerticalLayoutGroup layout =
            window.GetComponent<VerticalLayoutGroup>();

        layout.padding =
            new RectOffset(20, 20, 20, 20);

        layout.spacing = 7f;

        TMP_Text header =
            CreateText(
                window.transform,
                "COMMUNITY CHEST TESTER",
                28
            );

        header.color =
            new Color(
                1f,
                0.80f,
                0.24f
            );

        status =
            CreateText(
                window.transform,
                "",
                16
            );

        for (int i = 0; i < 20; i++)
        {
            int index = i;

            Button button =
                CreateButton(
                    window.transform,
                    $"CARD {i + 1}",
                    32
                );

            button.onClick.AddListener(
                () =>
                {
                    if (manager == null)
                    {
                        manager =
                            FindFirstObjectByType<
                                CommunityChestCardManager
                            >(
                                FindObjectsInactive.Include
                            );
                    }

                    if (manager != null)
                    {
                        manager.TestCard(
                            index
                        );
                    }
                }
            );
        }

        Button close =
            CreateButton(
                window.transform,
                "CLOSE TESTER (F7)",
                44
            );

        close.onClick.AddListener(
            () => window.SetActive(false)
        );
    }

    private void RefreshStatus()
    {
        if (status == null)
            return;

        if (manager == null)
        {
            status.text =
                "CommunityChestCardManager not found.";
            return;
        }

        status.text =
            $"{manager.CardCount} Community Chest cards loaded. " +
            "Buttons run the real card effects.";
    }

    private TMP_Text CreateText(
        Transform parent,
        string value,
        float size)
    {
        GameObject go =
            new GameObject(
                "Text",
                typeof(RectTransform)
            );

        go.transform.SetParent(
            parent,
            false
        );

        TMP_Text text =
            go.AddComponent<TextMeshProUGUI>();

        text.text = value;
        text.fontSize = size;
        text.color = Color.white;
        text.alignment =
            TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.Normal;
        LayoutElement le =
            go.AddComponent<LayoutElement>();

        le.preferredHeight =
            size + 16f;

        return text;
    }

    private Button CreateButton(
        Transform parent,
        string label,
        float height)
    {
        GameObject go =
            new GameObject(
                label,
                typeof(RectTransform),
                typeof(Image),
                typeof(Button)
            );

        go.transform.SetParent(
            parent,
            false
        );

        go.GetComponent<Image>().color =
            new Color(
                0.08f,
                0.30f,
                0.62f
            );

        Button button =
            go.GetComponent<Button>();

        TMP_Text text =
            CreateText(
                go.transform,
                label,
                17
            );

        text.alignment =
            TextAlignmentOptions.Center;

        LayoutElement le =
            go.AddComponent<LayoutElement>();

        le.preferredHeight =
            height;

        return button;
    }
}
