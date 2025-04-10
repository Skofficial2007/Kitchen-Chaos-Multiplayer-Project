using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class CharacterSelectPlayer : MonoBehaviour
{
    [SerializeField] private int playerIndex;        // Index to identify which player this UI element corresponds to.
    [SerializeField] private GameObject readyGameObject; // Visual indicator (e.g., an icon) showing if the player is ready.
    [SerializeField] private PlayerVisual playerVisual;
    [SerializeField] private Button kickButton;
    [SerializeField] private TextMeshPro playerNameText;

    private void Awake()
    {
        kickButton.onClick.AddListener(() =>
        {
            // Retrieve player data to access their unique client ID.
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);

            // Check if the player to be kicked is the host/owner.
            // (Assuming that the host's clientId matches the LocalClientId.)
            if (playerData.clientId == NetworkManager.Singleton.LocalClientId)
            {
                // For host, perform full shutdown procedures.
                KitchenGameLobby.Instance.LeaveLobby();
                NetworkManager.Singleton.Shutdown();
                Loader.Load(Loader.Scene.MainMenuScene);
            }
            else
            {
                // For other players, kick them normally.
                KitchenGameLobby.Instance.KickPlayer(playerData.playerId.ToString());
                KitchenGameMultiplayer.Instance.KickPlayer(playerData.clientId);
            }
        });
    }

    private void Start()
    {
        // Subscribe to events that update the UI when player data or readiness status changes.
        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged += KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
        CharacterSelectReady.Instance.OnReadyChanged += CharacterSelectReady_OnReadyChanged;

        // Only show kick button to host
        kickButton.gameObject.SetActive(NetworkManager.Singleton.IsServer);

        // Initial UI update based on current player data.
        UpdatePlayer();
    }

    // Event callback triggered when a player's ready status changes.
    private void CharacterSelectReady_OnReadyChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    // Event callback triggered when the network list of player data changes.
    private void KitchenGameMultiplayer_OnPlayerDataNetworkListChanged(object sender, System.EventArgs e)
    {
        UpdatePlayer();
    }

    // Updates the player UI element based on connectivity and readiness.
    private void UpdatePlayer()
    {
        // Check if the player index corresponds to a connected player.
        if (KitchenGameMultiplayer.Instance.IsPlayerIndexConnected(playerIndex))
        {
            Show();

            // Retrieve player data to access their unique client ID.
            PlayerData playerData = KitchenGameMultiplayer.Instance.GetPlayerDataFromPlayerIndex(playerIndex);

            // Activate the ready indicator based on the player's ready status.
            readyGameObject.SetActive(CharacterSelectReady.Instance.IsPlayerReady(playerData.clientId));

            playerNameText.text = playerData.playerName.ToString();

            playerVisual.SetPlayerColor(KitchenGameMultiplayer.Instance.GetPlayerColor(playerData.colorId));
        }
        else
        {
            // Hide the UI element if the player is not connected.
            Hide();
        }
    }

    // Makes the player UI element visible.
    private void Show() => gameObject.SetActive(true);

    // Hides the player UI element.
    private void Hide() => gameObject.SetActive(false);

    private void OnDestroy()
    {
        KitchenGameMultiplayer.Instance.OnPlayerDataNetworkListChanged -= KitchenGameMultiplayer_OnPlayerDataNetworkListChanged;
    }
}
