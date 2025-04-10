using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CharacterSelectReady : NetworkBehaviour
{
    public static CharacterSelectReady Instance { get; private set; } // Singleton instance for global access.

    public event EventHandler OnReadyChanged; // Event to notify subscribers when a player's ready status changes.

    // Dictionary to track each player's "ready" status for joining the game (multiplayer sync)
    private Dictionary<ulong, bool> playerReadyDictionary;

    private void Awake()
    {
        // Assign singleton instance.
        Instance = this;

        // Initialize dictionary for tracking player readiness.
        // This ensures we have a central store of each player's status that is updated across the network.
        playerReadyDictionary = new Dictionary<ulong, bool>();
    }

    // Called when the player clicks the ready button.
    public void SetPlayerReady()
    {
        // Initiate the process to mark the player as ready on the server.
        SetPlayerReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetPlayerReadyServerRpc(ServerRpcParams serverRpcParams = default)
    {
        // This will tell all the clients if they are ready
        SetPlayerReadyClientRpc(serverRpcParams.Receive.SenderClientId);

        // When a player presses interact, mark the corresponding client as ready in the server dictionary.
        playerReadyDictionary[serverRpcParams.Receive.SenderClientId] = true;

        bool allClientsReady = true;

        // Check if every connected client is ready.
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (!playerReadyDictionary.ContainsKey(clientId) || !playerReadyDictionary[clientId])
            {
                // The player is not ready.
                allClientsReady = false;
                break;
            }
        }

        // If every player is ready, transition the game state to start the countdown.
        if (allClientsReady)
        {
            KitchenGameLobby.Instance.DeleteLobby();
            // Load the game scene over the network.
            Loader.LoadNetwork(Loader.Scene.GameScene);
        }
    }

    // All the other logic is on server but to show if player is ready or not in characterSelectUI we have to give this info to every client if they are ready or not
    [ClientRpc]
    private void SetPlayerReadyClientRpc(ulong clientId)
    {
        // Update the local dictionary with the ready status for the given client.
        playerReadyDictionary[clientId] = true;

        // Notify any listeners (like UI) that a player's ready status has changed.
        OnReadyChanged?.Invoke(this, EventArgs.Empty);
    }

    // To return if a particular player is ready or not.
    // Checks if the player's ready state exists and returns its value.
    public bool IsPlayerReady(ulong clientId)
    {
        return playerReadyDictionary.ContainsKey(clientId) && playerReadyDictionary[clientId];
    }
}
