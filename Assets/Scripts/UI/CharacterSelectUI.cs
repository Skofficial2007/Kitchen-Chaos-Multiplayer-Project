using TMPro;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectUI : MonoBehaviour
{
    [SerializeField] private Button mainMenuButton; // Button to return to the main menu.
    [SerializeField] private Button readyButton;      // Button to mark the player as ready.
    [SerializeField] private TextMeshProUGUI lobbyNameText;
    [SerializeField] private TextMeshProUGUI lobbyCodeText;

    private void Awake()
    {
        // Set up UI event listeners when the script is first initialized.
        mainMenuButton.onClick.AddListener(() =>
        {
            Destroy(MusicManager.Instance.gameObject);
            KitchenGameLobby.Instance.LeaveLobby();
            // Shut down the network when returning to the main menu.
            NetworkManager.Singleton.Shutdown();
            // Using Load instead of LoadNetwork because we just shutdown the network to go back to main menu scene
            Loader.Load(Loader.Scene.MainMenuScene);
        });
        readyButton.onClick.AddListener(() =>
        {
            // Signal that the player is ready for the game.
            CharacterSelectReady.Instance.SetPlayerReady();
        });
    }

    private void Start()
    {
        Lobby lobby = KitchenGameLobby.Instance.GetLobby();

        lobbyNameText.text = "Lobby Name: " + lobby.Name;
        lobbyCodeText.text = "Lobby Code: " + lobby.LobbyCode;
    }
}
