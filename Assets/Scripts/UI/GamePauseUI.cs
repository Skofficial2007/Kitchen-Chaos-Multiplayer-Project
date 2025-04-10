using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class GamePauseUI : MonoBehaviour
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private Button optionsButton;

    private void Awake()
    {
        resumeButton.onClick.AddListener(() =>
        {
            KitchenGameManager.Instance.TogglePauseGame();
        });
        mainMenuButton.onClick.AddListener(() =>
        {
            Destroy(MusicManager.Instance.gameObject);
            KitchenGameLobby.Instance.LeaveLobby();
            NetworkManager.Singleton.Shutdown();
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        optionsButton.onClick.AddListener(() =>
        {
            Hide();
            OptionsUI.Instance.Show(Show); // When options ui closes again show pause ui
        });
    }

    private void Start()
    {
        // Subscribe to Pause and Unpause event
        KitchenGameManager.Instance.OnLocalGamePause += KitchenGameManager_OnLocalGamePause;
        KitchenGameManager.Instance.OnLocalGameUnpause += KitchenGameManager_OnLocalGameUnpause;

        Hide();
    }

    private void KitchenGameManager_OnLocalGamePause(object sender, System.EventArgs e)
    {
        Show();
    }

    private void KitchenGameManager_OnLocalGameUnpause(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
