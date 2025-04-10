using System;
using Unity.Netcode;
using UnityEngine;

public class PlatesCounter : BaseCounter
{
    // Event triggered when a new plate is spawned on the counter.
    public event EventHandler OnPlateSpawned;

    // Event triggered when a plate is removed from the counter.
    public event EventHandler OnPlateRemoved;

    [SerializeField] private KitchenObjectSO plateKitchenObjectSO; // The plate object to be spawned.

    private float spawnPlateTimer;           // Timer to track spawning intervals.
    private float spawnPlateTimerMax = 4f;     // Maximum time interval between spawns.
    private int plateSpawnedAmount;            // Current count of spawned plates.
    private int plateSpawnedAmountMax = 4;      // Maximum plates that can be spawned.

    private void Update()
    {
        // Only the server controls the game logic for plate spawning.
        // This check ensures that only the server executes the following code.
        if (!IsServer)
        {
            return;
        }

        // Update the spawn timer each frame using the elapsed time.
        spawnPlateTimer += Time.deltaTime;

        // When the timer exceeds the maximum allowed interval, it's time to spawn a new plate.
        if (spawnPlateTimer > spawnPlateTimerMax)
        {
            // Reset the timer for the next spawn cycle.
            spawnPlateTimer = 0f;

            // Only spawn a new plate if the game is in a playing state and the counter isn't full.
            if (KitchenGameManager.Instance.IsGamePlaying() && plateSpawnedAmount < plateSpawnedAmountMax)
            {
                // Calling a ServerRpc method to ensure the plate spawn is controlled by the server.
                SpawnPlateServerRpc();
            }
        }
    }

    // ServerRpc to initiate the plate spawn process on the server.
    // Although this is being called from the server-side Update, using a ServerRpc structure
    // helps keep our network logic consistent, especially if future changes require client-side requests.
    [ServerRpc]
    private void SpawnPlateServerRpc()
    {
        // After processing on the server, we notify all clients to update their local state.
        SpawnPlateClientRpc();
    }

    // ClientRpc that runs on all clients to update the plate count and trigger any associated events.
    [ClientRpc]
    private void SpawnPlateClientRpc()
    {
        // Increase the counter for spawned plates.
        plateSpawnedAmount++;

        // Trigger the event to notify listeners (e.g., for UI, animations, or sound effects)
        // that a new plate has been spawned on the counter.
        OnPlateSpawned?.Invoke(this, EventArgs.Empty);
    }

    public override void Interact(Player player)
    {
        // Only allow interaction if the player is not already holding a KitchenObject.
        if (!player.HasKitchenObject())
        {
            // Check if there is at least one plate available on the counter.
            if (plateSpawnedAmount > 0)
            {
                // Spawn the plate and assign it to the player.
                // This static method call ensures the correct KitchenObject is spawned in a networked manner.
                KitchenObject.SpawnKitchenObject(plateKitchenObjectSO, player);

                // Handle additional logic for plate removal using a ServerRpc to centralize game state changes.
                InteractLogicServerRpc();
            }
        }
    }

    // ServerRpc for handling the interaction logic on the server.
    // This method ensures that the game state change (i.e., a plate being taken by a player)
    // is authoritatively processed on the server.
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc()
    {
        // Once the server processes the interaction, a ClientRpc is used to update all clients.
        InteractLogicClientRpc();
    }

    // ClientRpc to update all clients after a plate has been taken.
    // This method decrements the plate count and triggers any visual/audio feedback (via OnPlateRemoved).
    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        // Decrease the plate count as a plate has been removed from the counter.
        plateSpawnedAmount--;

        // Trigger the event to notify listeners that a plate has been removed.
        OnPlateRemoved?.Invoke(this, EventArgs.Empty);
    }
}
