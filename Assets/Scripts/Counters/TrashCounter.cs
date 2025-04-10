using System;
using Unity.Netcode;
using UnityEngine;

public class TrashCounter : BaseCounter
{
    // Event that will be triggered when Player put an object in trash to play sound effect
    public static event EventHandler OnAnyObjectTrashed;

    // Resets the static event when switching scenes or restarting the game to prevent duplicate event subscriptions.
    new public static void ResetStaticData()
    {
        OnAnyObjectTrashed = null;
    }

    public override void Interact(Player player)
    {
        // If player has kitchen object and interacts with TrashCounter then destroy the object.
        // The new destruction logic now ensures that the object is removed on the server,
        // and that all clients are informed to clear their local references (parent links) before the object is actually destroyed.
        if (player.HasKitchenObject())
        {
            // This static method calls into KitchenGameMultiplayer to perform networked destruction.
            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());

            // Additional network logic is triggered to play any associated sound effects across clients.
            InteractLogicServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicServerRpc()
    {
        // Once the server processes the interaction, a ClientRpc is used to update all clients.
        // This ensures that any visual or audio feedback (like a trash sound effect) is synchronized.
        InteractLogicClientRpc();
    }

    [ClientRpc]
    private void InteractLogicClientRpc()
    {
        OnAnyObjectTrashed?.Invoke(this, EventArgs.Empty);
    }
}
