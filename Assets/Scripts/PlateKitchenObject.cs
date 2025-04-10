using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// PlateKitchenObject is a specialized KitchenObject that can hold other valid KitchenObjects as ingredients.
// It triggers events whenever ingredients are added and enforces rules like no duplicates.
public class PlateKitchenObject : KitchenObject
{
    // Event triggered whenever an ingredient is added to the plate.
    // Listeners (e.g., UI, sound managers) can subscribe to this event to react to ingredient additions.
    public event EventHandler<OnIngredientAddedEventArgs> OnIngredientAdded;

    // Custom EventArgs to store the kitchen object that was added.
    public class OnIngredientAddedEventArgs : EventArgs
    {
        public KitchenObjectSO kitchenObjectSO;
    }

    [SerializeField] private List<KitchenObjectSO> validKitchenObjectSOList; // List of objects that are allowed to be added to the plate.
    private List<KitchenObjectSO> kitchenObjectSOList; // List storing the currently added ingredients.

    // Override Awake to initialize the ingredient list.
    // Using base.Awake() ensures that any initialization in the parent KitchenObject is preserved.
    protected override void Awake()
    {
        base.Awake();
        // Initialize the list that will hold the ingredients added to the plate.
        kitchenObjectSOList = new List<KitchenObjectSO>();
    }

    // Attempts to add an ingredient to the plate.
    // Returns true if the ingredient is valid, not already added, and the add process was initiated.
    public bool TryAddIngredient(KitchenObjectSO kitchenObjectSO)
    {
        // Check if the ingredient is valid for this plate.
        if (!validKitchenObjectSOList.Contains(kitchenObjectSO))
        {
            // Ingredient is not valid; return false to indicate failure.
            return false;
        }

        // Check for duplicates to ensure the same ingredient isn't added more than once.
        if (kitchenObjectSOList.Contains(kitchenObjectSO))
        {
            // Duplicate ingredient is not allowed; return false.
            return false;
        }
        else
        {
            // If valid and not a duplicate, initiate a networked call to add the ingredient.
            // Instead of directly adding the ingredient locally, we send a ServerRpc.
            // This ServerRpc ensures that the addition is performed authoritatively on the server,
            // then a ClientRpc propagates the change to all clients.
            AddIngredientServerRpc(
                KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObjectSO)
            );
            return true;
        }
    }

    // ServerRpc to handle the addition of an ingredient on the server.
    // The server receives the KitchenObjectSO index (because we cannot send full serialized objects in RPC calls)
    // and then relays the instruction to all clients.
    [ServerRpc(RequireOwnership = false)]
    private void AddIngredientServerRpc(int kitchenObjectSOIndex)
    {
        // Call the ClientRpc to add the ingredient on each client.
        AddIngredientClientRpc(kitchenObjectSOIndex);
    }

    // ClientRpc to update all clients with the new ingredient.
    // This ensures that every client adds the ingredient to their local plate representation.
    [ClientRpc]
    private void AddIngredientClientRpc(int kitchenObjectSOIndex)
    {
        // Convert the index back to the corresponding KitchenObjectSO.
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        // Add the valid, non-duplicate ingredient to the local list.
        kitchenObjectSOList.Add(kitchenObjectSO);

        // Trigger the event to notify any listeners (like UI updates or sound effects) that an ingredient was added.
        OnIngredientAdded?.Invoke(this, new OnIngredientAddedEventArgs
        {
            kitchenObjectSO = kitchenObjectSO
        });
    }

    // Returns the list of ingredients currently on the plate.
    // This can be used by other parts of the game (e.g., recipe logic or UI) to determine the plate's contents.
    public List<KitchenObjectSO> GetKitchenObjectSOList()
    {
        return kitchenObjectSOList;
    }
}
