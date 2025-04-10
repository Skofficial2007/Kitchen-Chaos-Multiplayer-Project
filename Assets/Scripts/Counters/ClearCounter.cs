using UnityEngine;

// ClearCounter extends BaseCounter.
// Unlike ContainerCounter, ClearCounter is used for placing and picking up items (not spawning new objects).
public class ClearCounter : BaseCounter
{
    [Header("Kitchen Objects")]
    [SerializeField] private KitchenObjectSO kitchenObjectSO; // The KitchenObject type that can be placed on this counter.

    public override void Interact(Player player)
    {
        if (!HasKitchenObject()) // If there is no KitchenObject on the counter.
        {
            if (player.HasKitchenObject())
            {
                // If the player is carrying something, let them drop it onto the counter.
                player.GetKitchenObject().SetKitchenObjectParent(this);
            }
            else
            {
                // If the player is not carrying anything, do nothing.
            }
        }
        else // There is a KitchenObject on the counter.
        {
            if (player.HasKitchenObject())
            {
                // If the player already holds an object, check if it's a plate
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    // Player is holding a plate then place the object on the counter on the plate
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                    }
                }
                else
                {
                    // Player is carrying something but it's not a plate
                    // Check if Kitchen Object on the counter is a Plate
                    if (GetKitchenObject().TryGetPlate(out plateKitchenObject))
                    {
                        if (plateKitchenObject.TryAddIngredient(player.GetKitchenObject().GetKitchenObjectSO()))
                        {
                            // Destroy the object from player's hand as it get added to Plate
                            KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
                        }
                    }
                }
            }
            else
            {
                // Let the player pick up the KitchenObject from the counter.
                GetKitchenObject().SetKitchenObjectParent(player);
            }
        }
    }
}
