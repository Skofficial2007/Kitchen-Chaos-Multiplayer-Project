using UnityEngine;

public class DeliveryCounter : BaseCounter
{
    public static DeliveryCounter Instance { get; private set; }

    private void Awake()
    {
        // Ensures there is only one instance of DeliveryCounter.
        if (Instance != null)
        {
            Debug.LogError("There is more than one DeliveryCounter Instance");
        }
        Instance = this;
    }

    public override void Interact(Player player)
    {
        // Check if the player is holding a kitchen object.
        if (player.HasKitchenObject())
        {
            // Try to get the plate from the player's kitchen object.
            if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
            {
                // Only plates can be delivered.
                DeliveryManager.Instance.DeliveryRecipe(plateKitchenObject);

                // After delivery, destroy the kitchen object (plate).
                // This static method calls into KitchenGameMultiplayer to perform networked destruction.
                KitchenObject.DestroyKitchenObject(player.GetKitchenObject());
            }
        }
    }
}
