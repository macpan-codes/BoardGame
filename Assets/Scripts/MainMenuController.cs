using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [Header("Scenes")]
    [SerializeField] private string localGameSetupScene = "GameSetup";

    [Header("Panels")]
    [SerializeField] private GameObject settingsPanel;

    private void Awake()
    {
        // The current MainMenu scene has a designed Settings button but no
        // serialized OnClick target. Wire it without replacing its styling.
        GameObject settingsButtonObject =
            GameObject.Find("SettingsButton");

        Button settingsButton =
            settingsButtonObject != null
                ? settingsButtonObject.GetComponent<Button>()
                : null;

        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveListener(OpenSettings);
            settingsButton.onClick.AddListener(OpenSettings);
        }
    }

    // ============================================================
    // PLAY LOCAL
    // ============================================================

    public void PlayLocal()
    {
        if (string.IsNullOrWhiteSpace(localGameSetupScene))
        {
            Debug.LogError(
                "MainMenuController: Local game setup scene is not assigned."
            );

            return;
        }

        Debug.Log(
            $"MainMenuController: Loading {localGameSetupScene}."
        );

        SceneManager.LoadScene(
            localGameSetupScene
        );
    }

    // ============================================================
    // MULTIPLAYER
    // ============================================================

    public void OpenMultiplayer()
    {
        SceneManager.LoadScene("MultiplayerComingSoon");
    }

    // ============================================================
    // SETTINGS
    // ============================================================

    public void OpenSettings()
    {
        // Always use the shared editable Settings UI, rather than a legacy
        // scene-specific panel that can bypass saved audio preferences.
        GameAudioSettingsUI.Open();
    }

    // ============================================================
    // CREDITS
    // ============================================================

    public void OpenCredits()
    {
        SceneManager.LoadScene("Credits");
    }

    // ============================================================
    // EXIT
    // ============================================================

    public void ExitGame()
    {
        Debug.Log(
            "MainMenuController: Exit requested."
        );

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    // ============================================================
    // CLOSE SETTINGS
    // ============================================================

    public void CloseSettings()
    {
        GameAudioSettingsUI.CloseOpenSettings();

        // Keep legacy panel references harmless for older scene setups.
        if (settingsPanel != null)
            settingsPanel.SetActive(false);
    }
}