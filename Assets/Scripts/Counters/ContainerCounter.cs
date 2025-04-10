using System;
using Unity.Netcode;
using UnityEngine;

// ContainerCounter extends BaseCounter and represents a counter that spawns a KitchenObject.
// When the player interacts with this counter, it immediately spawns the designated KitchenObject and gives it to the player.
public class ContainerCounter : BaseCounter
{
    // Event triggered when the player picks up an object from this counter (useful for animations or sound effects).
    public event EventHandler OnPlayerGrabbedObject;

    [Header("Kitchen Objects")]
    [SerializeField] private KitchenObjectSO kitchenObjectSO; // The type of KitchenObject that this counter spawns.

    // Override the base interaction method.
    public override void Interact(Player player)
    {
        // Only allow interaction if the player is not already holding an object.
        if (!player.HasKitchenObject())
        {
            // Spawn the KitchenObject and assign it to the player.
            KitchenObject.SpawnKitchenObject(kitchenObjectSO, player);

            // Call a ServerRpc to propagate additional interaction logic.
            // This ensures that any side effects (like playing a sound or animation) are synchronized across the network.
            InteractLogicServerRpc();
        }
    }

    // ServerRpc for handling the interaction logic on the server.
    // This method ensures that the server dictates the game state change.
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc()
    {
        // Once the server processes the interaction, a ClientRpc is used to update all clients.
        InteractLogicClientRpc();
    }

    // ClientRpc to update all clients with the interaction logic.
    // This method is responsible for triggering events such as animations or sound effects on every client.
    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        // Trigger the event that notifies all listeners that the player grabbed an object.
        OnPlayerGrabbedObject?.Invoke(this, EventArgs.Empty);
    }
}
