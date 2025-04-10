using Unity.Netcode;
using UnityEngine;

// We needed an interface (IKitchenObjectParent) because:
// 1. We want multiple objects (counters, players, plates, etc.) to be able to hold and move KitchenObjects.
// 2. This interface standardizes how objects interact with KitchenObjects without needing separate implementations for each type.
// 3. It allows us to easily switch the parent of a KitchenObject, enabling flexible interactions.
// 4. Using an interface avoids code duplication and makes future expansions easier (e.g., adding a plate or storage system).

// Interface to define common behaviors for any object that can hold a KitchenObject
public interface IKitchenObjectParent
{
    // To get the position where the KitchenObject should be placed when it is assigned to this parent
    public Transform GetKitchenObjectFollowTransform();

    // To set the given KitchenObject as the current object held by this parent
    public void SetKitchenObject(KitchenObject kitchenObject);

    // To get the currently assigned KitchenObject (if any)
    public KitchenObject GetKitchenObject();

    // To clear the reference to the KitchenObject, marking this parent as empty
    public void ClearKitchenObject();

    // To check if this parent already has a KitchenObject assigned
    public bool HasKitchenObject();
    
    // To get the NetworkObject
    public NetworkObject GetNetworkObject();
}
