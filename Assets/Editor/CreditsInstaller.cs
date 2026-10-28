#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor.Events;

/// <summary>Run once from Board Game > Credits > Install Credits Scene And Button.</summary>
public static class CreditsInstaller
{
    private const string MenuPath="Assets/Scenes/MainMenu.unity";
    private const string CreditsPath="Assets/Scenes/Credits.unity";

    [MenuItem("Board Game/Credits/Install Credits Scene And Button")]
    public static void Install()
    {
        if (EditorApplication.isPlaying)
        {
            EditorUtility.DisplayDialog("Credits Installer","Stop Play Mode before installing.","OK");
            return;
        }
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        var host=new GameObject("CreditsScreen");
        host.AddComponent<CreditsScreen>();
        EditorSceneManager.SaveScene(scene,CreditsPath);

        var menu=EditorSceneManager.OpenScene(MenuPath,OpenSceneMode.Single);
        GameObject exit=GameObject.Find("ExitButton");
        if(exit==null)
        {
            Debug.LogError("Credits Installer: ExitButton not found in MainMenu.");
            return;
        }
        Transform parent=exit.transform.parent;
        Transform existing=parent.Find("CreditsButton");
        GameObject credits=existing!=null ? existing.gameObject :
            new GameObject("CreditsButton",typeof(RectTransform),typeof(Image),typeof(Button));
        credits.transform.SetParent(parent,false);
        RectTransform r=credits.GetComponent<RectTransform>();
        RectTransform exitRect=exit.GetComponent<RectTransform>();
        r.anchorMin=exitRect.anchorMin; r.anchorMax=exitRect.anchorMax;
        r.pivot=exitRect.pivot; r.anchoredPosition=exitRect.anchoredPosition+new Vector2(0,-145);
        r.sizeDelta=new Vector2(440,105);
        var image=credits.GetComponent<Image>();
        if(image==null) image=credits.AddComponent<Image>();
        // Temporary flat appearance; drop your AI-generated image into Source Image.
        if(image.sprite==null) image.color=new Color(.27f,.33f,.19f,1f);
        var button=credits.GetComponent<Button>();
        if(button==null) button=credits.AddComponent<Button>();
        // Make sure there is no inherited ExitGame action.
        for(int i=button.onClick.GetPersistentEventCount()-1;i>=0;i--)
            UnityEventTools.RemovePersistentListener(button.onClick,i);
        button.onClick.RemoveAllListeners();

        var controller=Object.FindFirstObjectByType<MainMenuController>();
        if(controller==null)
        {
            Debug.LogError("Credits Installer: MainMenuController not found.");
            return;
        }
        UnityEventTools.AddPersistentListener(button.onClick,controller.OpenCredits);
        EditorUtility.SetDirty(button);

        var labelTransform=credits.transform.Find("CreditsLabel");
        GameObject labelGO=labelTransform!=null ? labelTransform.gameObject :
            new GameObject("CreditsLabel",typeof(RectTransform),typeof(TextMeshProUGUI));
        labelGO.transform.SetParent(credits.transform,false);
        var lr=labelGO.GetComponent<RectTransform>();
        lr.anchorMin=Vector2.zero; lr.anchorMax=Vector2.one; lr.offsetMin=Vector2.zero; lr.offsetMax=Vector2.zero;
        var text=labelGO.GetComponent<TextMeshProUGUI>();
        text.text="CREDITS"; text.fontSize=40; text.fontStyle=FontStyles.Bold;
        text.alignment=TextAlignmentOptions.Center;
        text.color=new Color(.94f,.82f,.60f,1f); text.raycastTarget=false;
        EditorUtility.SetDirty(text);
        EditorSceneManager.MarkSceneDirty(menu);
        EditorSceneManager.SaveScene(menu);

        var scenes=new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        bool found=false;
        foreach(var s in scenes) if(s.path==CreditsPath) {s.enabled=true;found=true;}
        if(!found) scenes.Add(new EditorBuildSettingsScene(CreditsPath,true));
        EditorBuildSettings.scenes=scenes.ToArray();

        Debug.Log("Credits installed! MainMenu has CreditsButton; Credits scene is in Build Settings. Select CreditsButton > Image > Source Image to apply your AI artwork.");
        EditorUtility.DisplayDialog("Credits ready",
            "Credits scene + main menu button created. Open MainMenu to test. Select CreditsButton to replace its Source Image later.","OK");
        EditorSceneManager.OpenScene(MenuPath,OpenSceneMode.Single);
    }
}
#endif
