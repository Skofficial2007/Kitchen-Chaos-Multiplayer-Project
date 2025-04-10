using System;
using Unity.Netcode;
using UnityEngine;

// Handles interactions where players can place, pick up, or cut kitchen objects.
public class CuttingCounter : BaseCounter, IHasProgress
{
    // A static event that triggers the chopping sound effect when a cutting action occurs.
    // It is static so that it can be accessed and triggered by multiple instances of the CuttingCounter class.
    public static event EventHandler OnAnyCut;

    // Resets the static event when switching scenes or restarting the game to prevent duplicate event subscriptions.
    new public static void ResetStaticData()
    {
        OnAnyCut = null;
    }

    // Event triggered when cutting progress changes (used for UI updates).
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    // Event triggered when a cut action occurs (used for animations).
    // This event cannot be used with SoundManager as this can't be a static event as we will have multiple cutting counters in game.
    public event EventHandler OnCut;

    [SerializeField] private CuttingRecipeSO[] cuttingRecipeSOArray; // Array of available cutting recipes.

    private int cuttingProgress; // Tracks the current cutting progress.
    // NOTE: Although each client will have its own instance of cuttingProgress, we run critical progress checking on the server.
    // This avoids synchronization issues that might occur when each client maintains its own counter.

    public override void Interact(Player player)
    {
        // If the counter is empty and the player has an object that can be cut.
        if (!HasKitchenObject())
        {
            if (player.HasKitchenObject())
            {
                // Player is carrying something that can be cut.
                // Extra check: Only proceed if the carried object has an associated cutting recipe.
                if (HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
                {
                    // Place the player's object onto the counter and reset cutting progress.
                    KitchenObject kitchenObject = player.GetKitchenObject();
                    // Here, we change the parent of the object from the player to this counter.
                    kitchenObject.SetKitchenObjectParent(this);

                    // We call a ServerRpc to reset the cutting progress on both the server and all clients.
                    // This ensures that the UI (and any progress tracking logic) starts from zero across the network.
                    InteractLogicPlaceObjectServerRpc();
                }
            }
        }
        // If the player is not holding an object, allow them to pick up the one on the counter.
        else
        {
            // If player holds an object...
            if (player.HasKitchenObject())
            {
                // If the player already holds an object, check if it's a plate.
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    // When a player holds a plate, try to add the counter's object (ingredient) to the plate.
                    // This allows for combining ingredients without needing to drop the object first.
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        // Once added, destroy the original object on the counter.
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                }
            }
            else
            {
                // If the player isn't holding anything, simply transfer the object from the counter to the player.
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }

    // To reset the cutting progress for host and each client, we use a ServerRpc that then calls a ClientRpc.
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicPlaceObjectServerRpc()
    {
        // This ServerRpc informs all clients (via a ClientRpc) that an object has been placed on the counter.
        // It resets the local cutting progress to 0 on every client.
        InteractLogicPlaceObjectClientRpc();
    }

    [ClientRpc]
    private void InteractLogicPlaceObjectClientRpc()
    {
        // Reset the cutting progress for the newly placed object.
        cuttingProgress = 0;

        // Notify any UI elements or listeners to reset their progress display (initially 0).
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = 0f
        });
    }

    public override void InteractAlternate(Player player)
    {
        // Ensure there is an object on the counter and it has a valid cutting recipe.
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            // This ServerRpc call will trigger the cutting process across the network.
            CutObjectServerRpc();
            // Check if the cutting progress has reached the required threshold for processing the object.
            // This logic is only run on the server to maintain authoritative state.
            TestCuttingProgressDoneServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void CutObjectServerRpc()
    {
        // The server instructs all clients to increment the cutting progress.
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            // This ServerRpc call will trigger the cutting process across the network.
            CutObjectClientRpc();
        }
    }

    // Sync the cutting process on every client so that all players see the same progress update.
    [ClientRpc]
    private void CutObjectClientRpc()
    {
        // Increment cutting progress for the object.
        cuttingProgress++;

        // Trigger an animation event for the cutting action.
        OnCut?.Invoke(this, EventArgs.Empty);

        // Trigger the static event for cutting, used for playing sound effects globally.
        OnAnyCut?.Invoke(this, EventArgs.Empty);

        // Retrieve the relevant cutting recipe for the current object.
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

        // Notify UI elements to update the cutting progress bar.
        // The progress is normalized based on the maximum progress defined in the recipe.
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = (float)cuttingProgress / cuttingRecipeSO.cuttingProgressMax
        });
    }

    // This method is run only on the server to check if cutting progress has reached its maximum.
    // Running this on the server avoids duplication and ensures that only one authoritative decision is made,
    // regardless of the number of clients.
    [ServerRpc(RequireOwnership = false)]
    private void TestCuttingProgressDoneServerRpc()
    {
        if (HasKitchenObject() && HasRecipeWithInput(GetKitchenObject().GetKitchenObjectSO()))
        {
            // Get the cutting recipe for the current object on the counter.
            CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(GetKitchenObject().GetKitchenObjectSO());

            // If cutting is complete, then process the object.
            if (cuttingProgress >= cuttingRecipeSO.cuttingProgressMax)
            {
                // Get the resulting KitchenObjectSO after the cutting is done.
                KitchenObjectSO outputKitchenObjectSO = GetOutputForInput(GetKitchenObject().GetKitchenObjectSO());

                // Remove the original (uncut) object from the counter.
                KitchenObject.DestroyKitchenObject(GetKitchenObject());

                // Spawn the processed (cut) object on the counter.
                KitchenObject.SpawnKitchenObject(outputKitchenObjectSO, this);
            }
        }
    }

    // Checks if the given KitchenObjectSO has an associated cutting recipe.
    // This method ensures that only objects that can be cut are processed.
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return GetCuttingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    // Retrieves the cutting recipe associated with the given input object.
    // It loops through the array of recipes and returns the matching one.
    private CuttingRecipeSO GetCuttingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (CuttingRecipeSO cuttingRecipeSO in cuttingRecipeSOArray)
        {
            if (cuttingRecipeSO.input == inputKitchenObjectSO)
            {
                // Return the matching recipe.
                return cuttingRecipeSO;
            }
        }
        // Return null if no valid recipe is found.
        return null;
    }

    // Returns the resulting KitchenObjectSO after cutting the given input object.
    // This method retrieves the output defined in the matching cutting recipe.
    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO)
    {
        CuttingRecipeSO cuttingRecipeSO = GetCuttingRecipeSOWithInput(inputKitchenObjectSO);
        return cuttingRecipeSO != null ? cuttingRecipeSO.output : null;
    }
}
