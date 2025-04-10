using System.Collections;
using System.Collections.Generic;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Authentication;

// This class manages the multiplayer spawning and destruction of kitchen objects and ensures that objects are synchronized across all clients.
// It uses ServerRpc to let the server handle object instantiation and destruction, and ClientRpc (from other scripts) to propagate state changes.
public class KitchenGameMultiplayer : NetworkBehaviour
{
    public const int MAX_PLAYER_AMOUNT = 4; // Maximum number of players allowed in the game.
    private const string PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER = "PlayerNameMultiplayer";

    // Singleton instance to easily access the multiplayer manager from anywhere in the code.
    public static KitchenGameMultiplayer Instance { get; private set; }

    public static bool playMultiplayer = false;

    public event EventHandler OnTryingToJoinGame;          // Event fired when a client is attempting to join.
    public event EventHandler OnFailedToJoinGame;           // Event fired when a client fails to join.
    public event EventHandler OnPlayerDataNetworkListChanged; // Event fired whenever the list of player data changes.

    [SerializeField] private KitchenObjectListSO kitchenObjectListSO; // Holds all available KitchenObjectSO entries for reference.
    [SerializeField] private List<Color> playerColorList;

    // NetworkList that holds player data; must be initialized in Awake for proper network setup.
    // NOTE: Initialization is done in Awake because NetworkList objects need to be set up before any network events occur.
    private NetworkList<PlayerData> playerDataNetworkList;

    private string playerName;

    private void Awake()
    {
        // Assign this instance to the singleton for global access.
        Instance = this;
        // Preserve this object across scene loads so network data persists.
        DontDestroyOnLoad(gameObject);

        playerName = PlayerPrefs.GetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, "PlayerName" + UnityEngine.Random.Range(100, 1000));

        // Initialize the network list to store player data.
        playerDataNetworkList = new NetworkList<PlayerData>();

        // Subscribe to list change events to notify any listeners about updates.
        playerDataNetworkList.OnListChanged += PlayerDataNetworkList_OnListChanged;
    }

    private void Start()
    {
        // Means it's singleplayer mode
        if (!playMultiplayer)
        {
            StartHost();
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    public string GetPlayerName()
    {
        return playerName;
    }

    public void SetPlayerName(string playerName)
    {
        this.playerName = playerName;

        PlayerPrefs.SetString(PLAYER_PREFS_PLAYER_NAME_MULTIPLAYER, playerName);
    }

    // Callback for when the player data network list changes.
    private void PlayerDataNetworkList_OnListChanged(NetworkListEvent<PlayerData> changeEvent)
    {
        // Trigger the event to update any UI or game logic that depends on the player list.
        OnPlayerDataNetworkListChanged?.Invoke(this, EventArgs.Empty);
    }

    // Method to start hosting a multiplayer game.
    public void StartHost()
    {
        // Setup connection approval callback and start hosting.
        NetworkManager.Singleton.ConnectionApprovalCallback += NetworkManager_ConnectionApprovalCallback;
        // Subscribe to client connected events to update player data.
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        // Subscribe to server disconnected events
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Server_OnClientDisconnectCallback;

        // Begin hosting the game.
        NetworkManager.Singleton.StartHost();
    }

    private void NetworkManager_Server_OnClientDisconnectCallback(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            PlayerData playerData = playerDataNetworkList[i];

            if (playerData.clientId == clientId)
            {
                // Means this player/client disconnected
                playerDataNetworkList.RemoveAt(i);
            }
        }
    }

    // Called when a new client connects.
    private void NetworkManager_OnClientConnectedCallback(ulong clientId)
    {
        // Add new player data to the network list with the connected client's ID.
        playerDataNetworkList.Add(new PlayerData
        {
            clientId = clientId,
            colorId = GetFirstUnusedColorId()
        });
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    // Callback to approve or reject client connection requests.
    private void NetworkManager_ConnectionApprovalCallback(NetworkManager.ConnectionApprovalRequest connectionApprovalRequest, NetworkManager.ConnectionApprovalResponse connectionApprovalResponse)
    {
        // If this is the server, automatically approve the connection.
        if (connectionApprovalRequest.ClientNetworkId == NetworkManager.Singleton.LocalClientId)
        {
            connectionApprovalResponse.Approved = true;
            return;
        }

        // Approve connection only if the current scene is CharacterSelectScene.
        if (SceneManager.GetActiveScene().name != Loader.Scene.CharacterSelectScene.ToString())
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game has already started";
            return;
        }

        // Reject connection if maximum player count is reached.
        if (NetworkManager.Singleton.ConnectedClientsIds.Count >= MAX_PLAYER_AMOUNT)
        {
            connectionApprovalResponse.Approved = false;
            connectionApprovalResponse.Reason = "Game is full";
            return;
        }

        // Approve connection if none of the rejection criteria are met.
        connectionApprovalResponse.Approved = true;
    }

    // Method to start a client connection to a host.
    public void StartClient()
    {
        // Trigger event to notify that a join attempt is starting.
        OnTryingToJoinGame?.Invoke(this, EventArgs.Empty);

        // Setup callback for when the client disconnects.
        NetworkManager.Singleton.OnClientDisconnectCallback += NetworkManager_Client_OnClientDisconnectCallback;
        NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_Client_OnClientConnectedCallback;

        // Begin the client connection process.
        NetworkManager.Singleton.StartClient();
    }

    private void NetworkManager_Client_OnClientConnectedCallback(ulong clientId)
    {
        SetPlayerNameServerRpc(GetPlayerName());
        SetPlayerIdServerRpc(AuthenticationService.Instance.PlayerId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerNameServerRpc(string playerName, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerName = playerName;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerIdServerRpc(string playerId, ServerRpcParams serverRpcParams = default)
    {
        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.playerId = playerId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    // Callback when the client disconnects unexpectedly.
    private void NetworkManager_Client_OnClientDisconnectCallback(ulong clientId)
    {
        // Notify that joining has failed.
        OnFailedToJoinGame?.Invoke(this, EventArgs.Empty);
    }

    // Called to spawn a KitchenObject on the network.
    // The method takes in the ScriptableObject for the kitchen object and the target parent where the object should reside.
    public void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        // Get the index from the list for easier network transmission (since full objects can't be sent in an RPC).
        SpawnKitchenObjectServerRpc(GetKitchenObjectSOIndex(kitchenObjectSO), kitchenObjectParent.GetNetworkObject());
    }

    // NOTE: You can't send serialized objects in ServerRpc methods.
    // To avoid this, we send the index of the object inside a list (kitchenObjectListSO).
    // This pattern helps reduce bandwidth and avoids serialization issues.
    [ServerRpc(RequireOwnership = false)]
    public void SpawnKitchenObjectServerRpc(int kitchenObjectSOIndex, NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        // Convert the index back to the corresponding KitchenObjectSO.
        KitchenObjectSO kitchenObjectSO = GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        // Resolve the network object reference for the parent.
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);

        // Get the IKitchenObjectParent interface from the parent's NetworkObject.
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        if (kitchenObjectParent.HasKitchenObject())
        {
            // Parent already spawned an object
            return;
        }

        // Instantiate the kitchen object prefab in the server scene.
        Transform kitchenObjectTransform = Instantiate(kitchenObjectSO.prefab);

        // Get the NetworkObject component to enable networked behavior.
        NetworkObject kitchenObjectNetworkObject = kitchenObjectTransform.GetComponent<NetworkObject>();

        // Spawn the object on the network; this ensures all clients are aware of the new object.
        kitchenObjectNetworkObject.Spawn(true);

        // Retrieve the KitchenObject component for further initialization.
        KitchenObject kitchenObject = kitchenObjectTransform.GetComponent<KitchenObject>();

        // IMPORTANT: The following logic originally worked well on the host.
        // However, in multiplayer the parent might be a player on the host which isn't directly synced on the client.
        // Therefore, we must use a ClientRpc (inside KitchenObject) to update the parent on each client.
        kitchenObject.SetKitchenObjectParent(kitchenObjectParent);
    }

    // Helper method to get the index of the kitchen object ScriptableObject in our list.
    public int GetKitchenObjectSOIndex(KitchenObjectSO kitchenObjectSO)
    {
        return kitchenObjectListSO.kitchenObjectSOList.IndexOf(kitchenObjectSO);
    }

    // Helper method to convert an index back to a KitchenObjectSO.
    public KitchenObjectSO GetKitchenObjectSOFromIndex(int kitchenObjectSOIndex)
    {
        return kitchenObjectListSO.kitchenObjectSOList[kitchenObjectSOIndex];
    }

    // Public method to destroy a KitchenObject over the network.
    // This method is called by the KitchenObject.DestroyKitchenObject static helper.
    public void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        // Initiate destruction via a ServerRpc to ensure authoritative state changes.
        DestroyKitchenObjectServerRpc(kitchenObject.NetworkObject);
    }

    // ServerRpc to handle destruction of a KitchenObject.
    // This method first instructs all clients to clear the object's parent reference,
    // then destroys the object on the server.
    [ServerRpc(RequireOwnership = false)]
    private void DestroyKitchenObjectServerRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        // Retrieve the NetworkObject from the reference.
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        if (kitchenObjectNetworkObject == null)
        {
            // Means this object is already been destroyed
            return;
        }

        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        // Instruct all clients to clear their local reference to this KitchenObject's parent.
        ClearKitchenObjectOnParentClientRpc(kitchenObjectNetworkObjectReference);

        // Destroy the object on the server.
        kitchenObject.DestroySelf();
    }

    // ClientRpc to instruct all clients to clear the KitchenObject's parent reference.
    // This ensures that, before the object is destroyed on the server, each client updates its state to no longer reference it.
    [ClientRpc]
    private void ClearKitchenObjectOnParentClientRpc(NetworkObjectReference kitchenObjectNetworkObjectReference)
    {
        // Retrieve the NetworkObject from the reference.
        kitchenObjectNetworkObjectReference.TryGet(out NetworkObject kitchenObjectNetworkObject);

        KitchenObject kitchenObject = kitchenObjectNetworkObject.GetComponent<KitchenObject>();

        kitchenObject.ClearKitchenObjectOnParent();
    }

    // Function to check if a particular player index is connected to the game.
    public bool IsPlayerIndexConnected(int playerIndex)
    {
        // Means particular player is connected if the index is less than the total count in the list.
        return playerIndex < playerDataNetworkList.Count;
    }

    // Helper method to retrieve the PlayerData object for a given player index.
    public PlayerData GetPlayerDataFromPlayerIndex(int playerIndex)
    {
        return playerDataNetworkList[playerIndex];
    }

    // Get the color info of player
    public Color GetPlayerColor(int colorId)
    {
        return playerColorList[colorId];
    }

    public PlayerData GetPlayerDataFromClientId(ulong clientId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.clientId == clientId)
            {
                return playerData;
            }
        }

        return default;
    }

    public int GetPlayerDataIndexFromClientId(ulong clientId)
    {
        for (int i = 0; i < playerDataNetworkList.Count; i++)
        {
            if (playerDataNetworkList[i].clientId == clientId)
            {
                return i;
            }
        }

        return -1;
    }

    public PlayerData GetPlayerData()
    {
        return GetPlayerDataFromClientId(NetworkManager.Singleton.LocalClientId);
    }

    public void ChangePlayerColor(int colorId)
    {
        ChangePlayerColorServerRpc(colorId);
    }

    [ServerRpc(RequireOwnership = false)]
    private void ChangePlayerColorServerRpc(int colorId, ServerRpcParams serverRpcParams = default)
    {
        if (!IsColorAvailable(colorId))
        {
            // Color not available
            return;
        }

        int playerDataIndex = GetPlayerDataIndexFromClientId(serverRpcParams.Receive.SenderClientId);

        PlayerData playerData = playerDataNetworkList[playerDataIndex];

        playerData.colorId = colorId;

        playerDataNetworkList[playerDataIndex] = playerData;
    }

    private bool IsColorAvailable(int colorId)
    {
        foreach (PlayerData playerData in playerDataNetworkList)
        {
            if (playerData.colorId == colorId)
            {
                // Color is already in use
                return false;
            }
        }

        return true;
    }

    // To get the first color that is not used yet
    private int GetFirstUnusedColorId()
    {
        for (int i = 0; i < playerColorList.Count; i++)
        {
            if (IsColorAvailable(i))
            {
                return i;
            }
        }

        return -1;
    }

    // To kick player
    public void KickPlayer(ulong clientId)
    {
        NetworkManager.Singleton.DisconnectClient(clientId);
        NetworkManager_Server_OnClientDisconnectCallback(clientId);
    }
}
