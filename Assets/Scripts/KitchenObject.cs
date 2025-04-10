using Unity.Netcode;
using UnityEngine;

// Script to represent a KitchenObject that can be placed on different parents.
// Spawning and parent assignment are handled via server/client RPCs to ensure synchronization in multiplayer.
public class KitchenObject : NetworkBehaviour
{
    [SerializeField] private KitchenObjectSO kitchenObjectSO; // Reference to the ScriptableObject holding data for this KitchenObject.

    // The current parent (e.g., counter, player, plate) that holds this KitchenObject.
    private IKitchenObjectParent kitchenObjectParent;

    // Component used to smoothly follow a target transform (updated for multiplayer positioning).
    private FollowTransform followTransform;

    // Note: This Awake is overridden/hidden by Awake on PlateKitchenObject because it runs later.
    protected virtual void Awake()
    {
        // Cache the FollowTransform component for later use.
        followTransform = GetComponent<FollowTransform>();
    }

    // Returns the ScriptableObject data for this KitchenObject.
    public KitchenObjectSO GetKitchenObjectSO()
    {
        return kitchenObjectSO;
    }

    // Sets a new parent for this KitchenObject.
    // Instead of directly changing the parent, we delegate the change via ServerRpc and ClientRpc to ensure all clients update their state.
    public void SetKitchenObjectParent(IKitchenObjectParent kitchenObjectParent)
    {
        // Previously, code here directly changed the parent.
        // In multiplayer, we need to synchronize this change across all clients.
        // The ServerRpc call will inform the server, and then the server will use a ClientRpc to update every client.
        SetKitchenObjectParentServerRpc(kitchenObjectParent.GetNetworkObject());
    }

    // ServerRpc to request a change of parent on the server.
    // This method does not require ownership because we want any client to be able to request a parent change.
    [ServerRpc(RequireOwnership = false)]
    private void SetKitchenObjectParentServerRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        // The server uses a ClientRpc to notify all clients of the updated parent.
        SetKitchenObjectParentClientRpc(kitchenObjectParentNetworkObjectReference);
    }

    // ClientRpc to update the parent of the KitchenObject on all clients.
    [ClientRpc]
    private void SetKitchenObjectParentClientRpc(NetworkObjectReference kitchenObjectParentNetworkObjectReference)
    {
        // Resolve the network object reference into an actual NetworkObject.
        kitchenObjectParentNetworkObjectReference.TryGet(out NetworkObject kitchenObjectParentNetworkObject);

        // Get the IKitchenObjectParent component from the resolved object.
        IKitchenObjectParent kitchenObjectParent = kitchenObjectParentNetworkObject.GetComponent<IKitchenObjectParent>();

        // If this KitchenObject already had a parent, notify the old parent to clear its reference.
        if (this.kitchenObjectParent != null)
        {
            this.kitchenObjectParent.ClearKitchenObject();
        }

        // Update our local reference to the new parent.
        this.kitchenObjectParent = kitchenObjectParent;

        // Check to ensure the new parent isn't already holding another KitchenObject.
        if (kitchenObjectParent.HasKitchenObject())
        {
            Debug.LogError("IKitchenObjectParent already has a KitchenObject!");
        }

        // Set this KitchenObject as the current object of the new parent.
        kitchenObjectParent.SetKitchenObject(this);

        // IMPORTANT: Multiplayer position syncing
        // Previously, we directly set transform.parent to match the parent's follow transform.
        // However, with multiplayer and dynamic parent objects (like players), direct parenting isn’t reliable
        // because child objects might not be networked or might not be synced correctly.
        // Instead, we use the FollowTransform component to smoothly update the KitchenObject's position based on the parent's follow transform.
        // Previous code:
        // transform.parent = kitchenObjectParent.GetKitchenObjectFollowTransform();
        // transform.localPosition = Vector3.zero;
        // Updated approach using FollowTransform:
        followTransform.SetTargetTransform(kitchenObjectParent.GetKitchenObjectFollowTransform());
    }

    // Returns the current parent of this KitchenObject.
    public IKitchenObjectParent GetKitchenObjectParent()
    {
        return kitchenObjectParent;
    }

    // Destroys this KitchenObject on the server.
    // Updated: This method now only destroys the game object; however, we must ensure that clients clear any references to this object beforehand.
    public void DestroySelf()
    {
        Destroy(gameObject);
    }

    // We needed this logic for every client that's why we separated it from DestroySelf method.
    // This method is called via a ClientRpc to ensure that all clients clear the KitchenObject reference from their parent.
    public void ClearKitchenObjectOnParent()
    {
        kitchenObjectParent.ClearKitchenObject();
    }

    // Checks if this KitchenObject is a PlateKitchenObject and outputs it if true.
    // This allows code to directly access plate-specific logic without additional type checks elsewhere.
    public bool TryGetPlate(out PlateKitchenObject plateKitchenObject)
    {
        if (this is PlateKitchenObject)
        {
            plateKitchenObject = this as PlateKitchenObject;
            return true;
        }
        else
        {
            // Always assign the out parameter to avoid unassigned variable issues.
            plateKitchenObject = null;
            return false;
        }
    }

    // Static helper method to spawn a KitchenObject. This avoids code duplication.
    // It delegates the spawning logic to KitchenGameMultiplayer which handles network-wide synchronization.
    public static void SpawnKitchenObject(KitchenObjectSO kitchenObjectSO, IKitchenObjectParent kitchenObjectParent)
    {
        KitchenGameMultiplayer.Instance.SpawnKitchenObject(kitchenObjectSO, kitchenObjectParent);
    }

    // Static helper method to destroy a KitchenObject over the network.
    // This method delegates the destruction to the KitchenGameMultiplayer instance,
    // which handles clearing the object's parent reference on all clients before destruction.
    public static void DestroyKitchenObject(KitchenObject kitchenObject)
    {
        KitchenGameMultiplayer.Instance.DestroyKitchenObject(kitchenObject);
    }
}
