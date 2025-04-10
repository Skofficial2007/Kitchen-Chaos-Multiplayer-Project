using System;
using Unity.Netcode;
using UnityEngine;

// BaseCounter is the parent class for all counters, implementing IKitchenObjectParent.
// This allows various counter types (e.g., ClearCounter, ContainerCounter) to share common functionality.
public class BaseCounter : NetworkBehaviour, IKitchenObjectParent
{
    // Event to be triggered when an object is placed on a counter, staic because counters are of many type
    public static event EventHandler OnAnyObjectPlacedHere;

    // Resets the static event when switching scenes or restarting the game to prevent duplicate event subscriptions.
    public static void ResetStaticData()
    {
        OnAnyObjectPlacedHere = null;
    }

    [SerializeField] private Transform counterTopPoint; // Position where the KitchenObject will be placed.

    // Reference to the current KitchenObject on the counter.
    private KitchenObject kitchenObject;

    // The Interact method is overridden by child classes to define specific interactions.
    public virtual void Interact(Player player)
    {
        Debug.LogError("BaseCounter.Interact() executed");
    }

    // The alternate interact method is overridden by child classes for additional actions.
    public virtual void InteractAlternate(Player player)
    {
        // Debug.LogError("BaseCounter.InteractAlternate() executed");
    }

    // Returns the transform where the KitchenObject should be placed.
    public Transform GetKitchenObjectFollowTransform()
    {
        return counterTopPoint;
    }

    // Sets the current KitchenObject on this counter.
    public void SetKitchenObject(KitchenObject kitchenObject)
    {
        this.kitchenObject = kitchenObject;

        // Means Player placed a object on counter
        if (kitchenObject != null)
        {
            OnAnyObjectPlacedHere?.Invoke(this, EventArgs.Empty);
        }
    }

    // Returns the current KitchenObject on this counter.
    public KitchenObject GetKitchenObject()
    {
        return kitchenObject;
    }

    // Clears the KitchenObject reference when an object is removed.
    public void ClearKitchenObject()
    {
        kitchenObject = null;
    }

    // Checks if a KitchenObject is present on the counter.
    public bool HasKitchenObject()
    {
        return (kitchenObject != null);
    }

    public NetworkObject GetNetworkObject()
    {
        return NetworkObject;
    }
}
