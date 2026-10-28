using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;

/// <summary>Self-contained portrait credits screen. All text is editable below.</summary>
public class CreditsScreen : MonoBehaviour
{
    [System.Serializable] public class Person
    {
        public string name, title;
        [TextArea(2, 5)] public string contribution;
        [TextArea(2, 4)] public string funFact;
        public string achievement;
    }

    [Header("Customize these entries in the Credits scene Inspector")]
    public Person[] people =
    {
        new Person { name="ChatGPT", title="The Brain Behind the Curtain", contribution="Helped think through gameplay logic, Unity systems, debugging, and the tiny details that turn a prototype into a real board game.", funFact="An unlimited supply of ideas. Not all of them were requested.", achievement="Infinite Suggestions Generator" },
        new Person { name="Gemini", title="The Imagination Machine", contribution="Contributed creative brainstorming, visual directions, and fresh ways to imagine the game.", funFact="Can turn one simple idea into twelve possible designs.", achievement="Imagination Overdrive" },
        new Person { name="Macpan", title="The Master Linker", contribution="Brought all the moving pieces together, connected the systems, and carried the game toward the finish line.", funFact="One more tiny change... famous last words.", achievement="Chief Chaos Integrator" },
        new Person { name="Tahsan", title="The Property Architect", contribution="Provided creative intelligence and designed the properties that give the board its own identity.", funFact="Real estate empire? Built on excellent taste.", achievement="Lord of the Properties" },
        new Person { name="Purna", title="The Idea Generator", contribution="Shared ideas that helped shape the experience and make the game more fun.", funFact="Sometimes all it takes is one random suggestion.", achievement="Brainstorm Champion" },
        new Person { name="Turjo", title="The Royal Advisor", contribution="Gave suggestions and useful perspectives during development.", funFact="Every board needs someone willing to make a suggestion.", achievement="Council of Good Ideas" },
        new Person { name="Toha", title="Chief Motivation Officer", contribution="An honorary part of this game's story — here for motivation, friendship, and the fun of it.", funFact="No code required to be part of the lore.", achievement="Emotional Support DLC" }
    };

    private readonly Color background = new Color(.10f,.13f,.075f);
    private readonly Color panel = new Color(.22f,.27f,.15f);
    private readonly Color gold = new Color(.83f,.70f,.44f);
    private readonly Color cream = new Color(.96f,.89f,.71f);
    private GameObject modal;

    private static RectTransform Rect(GameObject go) { return go.GetComponent<RectTransform>(); }
    private GameObject UI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go;
    }
    private void Fill(GameObject go, Color color)
    {
        var image = go.AddComponent<Image>(); image.color = color;
    }
    private TextMeshProUGUI Label(Transform parent, string content, int size, Color color, TextAlignmentOptions alignment)
    {
        var go=UI("Text",parent);
        var txt=go.AddComponent<TextMeshProUGUI>();
        txt.text=content; txt.fontSize=size; txt.color=color; txt.alignment=alignment;
        txt.enableWordWrapping=true; txt.raycastTarget=false;
        return txt;
    }
    private void Stretch(RectTransform r, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    { r.anchorMin=min; r.anchorMax=max; r.offsetMin=offsetMin; r.offsetMax=offsetMax; }
    private void Layout(GameObject go, float gap=12f)
    {
        var g=go.AddComponent<VerticalLayoutGroup>();
        g.spacing=gap; g.childControlHeight=true; g.childForceExpandHeight=false;
        g.childControlWidth=true; g.childForceExpandWidth=true;
        g.padding=new RectOffset(14,14,14,14);
        var fit=go.AddComponent<ContentSizeFitter>();
        fit.verticalFit=ContentSizeFitter.FitMode.PreferredSize;
    }
    private Button Clickable(GameObject go, System.Action callback)
    {
        var b=go.AddComponent<Button>(); b.onClick.AddListener(()=>callback()); return b;
    }
    private GameObject Block(Transform parent, float height, Color color)
    {
        var go=UI("Panel",parent); Fill(go,color);
        var e=go.AddComponent<LayoutElement>(); e.preferredHeight=height;
        return go;
    }

    private void Awake()
    {
        // This UI-only scene has no camera by default. A persistent music source
        // still needs an AudioListener to be audible when the MainMenu unloads.
        if (FindFirstObjectByType<AudioListener>() == null)
            new GameObject("CreditsAudioListener", typeof(AudioListener));

        if (FindFirstObjectByType<EventSystem>() == null)
        {
            var input = new GameObject("CreditsEventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }
        var canvasGO=new GameObject("CreditsCanvas",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
        var canvas=canvasGO.GetComponent<Canvas>(); canvas.renderMode=RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder=100;
        var scaler=canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution=new Vector2(1080,1920);
        scaler.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight=.5f;

        var root=UI("Background",canvasGO.transform); Fill(root,background);
        Stretch(Rect(root),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);

        var header=UI("Heading",root.transform);
        Stretch(Rect(header),new Vector2(0,1),new Vector2(1,1),new Vector2(32,-310),new Vector2(-32,-42));
        var heading=Label(header.transform,"THE LEGENDS\nBEHIND THE GAME",65,gold,TextAlignmentOptions.Center);
        Stretch(Rect(heading.gameObject),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);

        var back=UI("BackButton",root.transform); Fill(back,panel);
        Stretch(Rect(back),new Vector2(0,1),new Vector2(0,1),new Vector2(34,-405),new Vector2(310,-325));
        Clickable(back,()=>SceneManager.LoadScene("MainMenu"));
        var backText=Label(back.transform,"‹  BACK",37,cream,TextAlignmentOptions.Center);
        Stretch(Rect(backText.gameObject),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);

        var viewport=UI("ScrollArea",root.transform);
        Stretch(Rect(viewport),new Vector2(0,0),new Vector2(1,1),new Vector2(32,165),new Vector2(-32,-435));
        var scroll=viewport.AddComponent<ScrollRect>(); scroll.horizontal=false;
        var mask=UI("Viewport",viewport.transform);
        Stretch(Rect(mask),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        Fill(mask,background); mask.AddComponent<Mask>().showMaskGraphic=false;
        var content=UI("Cards",mask.transform);
        Stretch(Rect(content),new Vector2(0,1),new Vector2(1,1),Vector2.zero,Vector2.zero);
        Rect(content).pivot=new Vector2(.5f,1);
        Layout(content,17);
        scroll.viewport=Rect(mask); scroll.content=Rect(content);
        for(int i=0;i<people.Length;i++)
        {
            int index=i; var p=people[i];
            var card=Block(content.transform,170,panel);
            var text=Label(card.transform,p.name.ToUpperInvariant()+"  ✦\n"+p.title+"  ›",40,cream,TextAlignmentOptions.MidlineLeft);
            Stretch(Rect(text.gameObject),Vector2.zero,Vector2.one,new Vector2(30,10),new Vector2(-18,-10));
            Clickable(card,()=>ShowPerson(index));
        }

        var footer=UI("Dedication",root.transform);
        Stretch(Rect(footer),Vector2.zero,new Vector2(1,0),new Vector2(35,20),new Vector2(-35,150));
        var foot=Label(footer.transform,"Built with code, chaos, and questionable financial decisions.\nMade with friends. ♥",29,gold,TextAlignmentOptions.Center);
        Stretch(Rect(foot.gameObject),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);

        modal=UI("DetailsOverlay",root.transform); Fill(modal,new Color(.02f,.03f,.01f,.95f));
        Stretch(Rect(modal),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        modal.SetActive(false);
    }

    private void ShowPerson(int i)
    {
        if(i<0 || i>=people.Length)return;
        foreach(Transform child in modal.transform) Destroy(child.gameObject);
        var p=people[i];
        var box=UI("ContributorDetails",modal.transform); Fill(box,panel);
        Stretch(Rect(box),new Vector2(.06f,.19f),new Vector2(.94f,.84f),Vector2.zero,Vector2.zero);
        var text=Label(box.transform,p.name.ToUpperInvariant()+"\n"+p.title+
            "\n\nHOW THEY HELPED\n"+p.contribution+
            "\n\nFUN FACT\n"+p.funFact+
            "\n\nACHIEVEMENT UNLOCKED\n"+p.achievement,40,cream,TextAlignmentOptions.Center);
        Stretch(Rect(text.gameObject),Vector2.zero,Vector2.one,new Vector2(30,118),new Vector2(-30,-25));
        text.enableAutoSizing=true; text.fontSizeMin=22; text.fontSizeMax=40;
        var close=UI("CloseDetails",box.transform); Fill(close,background);
        Stretch(Rect(close),new Vector2(.22f,0),new Vector2(.78f,0),new Vector2(0,25),new Vector2(0,110));
        Clickable(close,()=>modal.SetActive(false));
        var ct=Label(close.transform,"CLOSE",36,gold,TextAlignmentOptions.Center);
        Stretch(Rect(ct.gameObject),Vector2.zero,Vector2.one,Vector2.zero,Vector2.zero);
        modal.SetActive(true);
    }
}
