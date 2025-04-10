using System;
using Unity.Netcode;
using UnityEngine;
using static CuttingCounter;

// Handles interactions where players can place, pick up, fry, or burn kitchen objects.
public class StoveCounter : BaseCounter, IHasProgress
{
    // Event triggered when frying progress changes (used for UI updates).
    public event EventHandler<IHasProgress.OnProgressChangedEventArgs> OnProgressChanged;

    // Event triggered when the stove state changes (used for UI and visuals).
    public event EventHandler<OnStateChangeEventArgs> OnStateChanged;

    // Custom EventArgs class to pass the current stove state to listeners.
    public class OnStateChangeEventArgs : EventArgs
    {
        public State state; // Stores the current state of the stove.
    }

    // Public enum representing the various states of the stove.
    // This state drives both the gameplay (frying, burning) and the visuals/UI.
    public enum State
    {
        Idle,   // No food on the stove.
        Frying, // Food is being fried.
        Fried,  // Food is fully fried but not burned yet.
        Burned  // Food is overcooked and burned.
    }

    [SerializeField] private FryingRecipeSO[] fryingRecipeSOArray;   // List of available frying recipes.
    [SerializeField] private BurningRecipeSO[] burningRecipeSOArray;   // List of available burning recipes.

    // NetworkVariables ensure that the critical state and timer data are synchronized across all clients.
    // Only the server can write to these variables, and changes are propagated to every client.
    private NetworkVariable<State> state = new NetworkVariable<State>(State.Idle);  // Tracks the stove's current state.
    private NetworkVariable<float> fryingTimer = new NetworkVariable<float>(0f);      // Tracks how long the food has been frying.
    private NetworkVariable<float> burningTimer = new NetworkVariable<float>(0f);      // Tracks how long the food has been burning.

    // The active frying recipe currently in use. Set via a ClientRpc.
    private FryingRecipeSO fryingRecipeSO;
    // The active burning recipe currently in use. Set via a ClientRpc.
    private BurningRecipeSO burningRecipeSO;

    // Called when the network object is spawned. Here we subscribe to the OnValueChanged events
    // for our NetworkVariables so that any change triggers our local UI updates.
    public override void OnNetworkSpawn()
    {
        fryingTimer.OnValueChanged += FryingTimer_OnValueChanged;
        burningTimer.OnValueChanged += BurningTimer_OnValueChanged;
        state.OnValueChanged += State_OnValueChanged;
    }

    // Called whenever the frying timer changes.
    // This method calculates the normalized progress (0 to 1) and notifies any UI elements.
    private void FryingTimer_OnValueChanged(float previousValue, float newValue)
    {
        // If no recipe is set, default to a max value of 1 to avoid division by zero.
        float fryingTimerMax = fryingRecipeSO != null ? fryingRecipeSO.fryingTimerMax : 1f;

        // Notify progress bar (or any UI) to update its display for frying progress.
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = fryingTimer.Value / fryingTimerMax
        });
    }

    // Called whenever the burning timer changes.
    // Similar to the frying timer, it calculates normalized progress and updates the UI.
    private void BurningTimer_OnValueChanged(float previousValue, float newValue)
    {
        // Default value to prevent division by zero.
        float burningTimerMax = burningRecipeSO != null ? burningRecipeSO.burningTimerMax : 1f;

        // Update UI progress based on the burning timer.
        OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
        {
            progressNormalized = burningTimer.Value / burningTimerMax
        });
    }

    // Called when the stove state changes (for example, from Idle to Frying).
    // This informs subscribers (like visual components) of the new state.
    private void State_OnValueChanged(State previousState, State newState)
    {
        // Trigger an event to notify subscribers (e.g., to update animations or visuals).
        OnStateChanged?.Invoke(this, new OnStateChangeEventArgs
        {
            state = state.Value
        });

        // If the stove returns to Idle or the food is burned, hide the progress bar by resetting progress.
        if (state.Value == State.Burned || state.Value == State.Idle)
        {
            OnProgressChanged?.Invoke(this, new IHasProgress.OnProgressChangedEventArgs
            {
                progressNormalized = 0f
            });
        }
    }

    // The Update method handles the cooking process and is run only on the server.
    // This ensures that the game logic is authoritative and synchronized.
    private void Update()
    {
        if (!IsServer)
        {
            return;
        }

        // Process only if there is a KitchenObject on the stove.
        if (HasKitchenObject())
        {
            switch (state.Value)
            {
                case State.Idle:
                    // In Idle state, no processing occurs.
                    break;

                case State.Frying:
                    // While frying, increment the frying timer based on elapsed time.
                    fryingTimer.Value += Time.deltaTime;

                    // Check if the frying time has exceeded the maximum defined in the active recipe.
                    if (fryingTimer.Value > fryingRecipeSO.fryingTimerMax)
                    {
                        // Once frying is complete:
                        // 1. Destroy the raw object on the stove.
                        // 2. Spawn the fried version using the recipe's output.
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        KitchenObject.SpawnKitchenObject(fryingRecipeSO.output, this);

                        // Transition the stove state to Fried.
                        state.Value = State.Fried;
                        burningTimer.Value = 0f; // Reset burning timer for the next phase.

                        // Retrieve the burning recipe for this food by sending the object's index to all clients.
                        // This ensures that burningRecipeSO is set appropriately on every client.
                        SetBurningRecipeSOClientRpc(
                            KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(GetKitchenObject().GetKitchenObjectSO())
                        );
                    }
                    break;

                case State.Fried:
                    // Once food is fried, start the burning timer.
                    burningTimer.Value += Time.deltaTime;

                    // Check if the burning timer exceeds the maximum defined in the burning recipe.
                    if (burningTimer.Value > burningRecipeSO.burningTimerMax)
                    {
                        // Once burning is complete:
                        // 1. Destroy the fried object.
                        // 2. Spawn the burned version.
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());
                        KitchenObject.SpawnKitchenObject(burningRecipeSO.output, this);

                        // Transition the stove state to Burned.
                        state.Value = State.Burned;
                    }
                    break;

                case State.Burned:
                    // In the Burned state, no further processing is needed.
                    break;
            }
        }
    }

    // Handles the primary interaction of placing or retrieving objects on/from the stove.
    public override void Interact(Player player)
    {
        // If the stove is empty and the player has a kitchen object that can be cooked.
        if (!HasKitchenObject())
        {
            if (player.HasKitchenObject() && HasRecipeWithInput(player.GetKitchenObject().GetKitchenObjectSO()))
            {
                // Transfer the object from the player to the stove.
                KitchenObject kitchenObject = player.GetKitchenObject();
                kitchenObject.SetKitchenObjectParent(this);

                // Use a ServerRpc to handle the logic of starting the cooking process.
                // The object's KitchenObjectSO index is sent so the correct recipe can be retrieved on clients.
                InteractLogicPlaceObjectOnCounterServerRpc(
                    KitchenGameMultiplayer.Instance.GetKitchenObjectSOIndex(kitchenObject.GetKitchenObjectSO())
                );
            }
        }
        // If the stove is not empty, allow the player to pick up the food.
        else
        {
            // If the player is already carrying something...
            if (player.HasKitchenObject())
            {
                // Check if the held object is a plate to allow ingredient addition.
                if (player.GetKitchenObject().TryGetPlate(out PlateKitchenObject plateKitchenObject))
                {
                    // Attempt to add the stove's object to the plate.
                    if (plateKitchenObject.TryAddIngredient(GetKitchenObject().GetKitchenObjectSO()))
                    {
                        // If successful, remove the food from the stove.
                        KitchenObject.DestroyKitchenObject(GetKitchenObject());

                        // Reset the stove state to Idle via a ServerRpc.
                        SetStateIdleServerRpc();
                    }
                }
            }
            // If the player is empty-handed, simply transfer the food from the stove to the player.
            else
            {
                GetKitchenObject().SetKitchenObjectParent(player);

                // Reset the stove state to Idle via a ServerRpc.
                SetStateIdleServerRpc();
            }
        }
    }

    // ServerRpc to reset the stove state to Idle.
    // This ensures that the authoritative state change is made on the server.
    [ServerRpc(RequireOwnership = false)]
    private void SetStateIdleServerRpc()
    {
        state.Value = State.Idle;
    }

    // ServerRpc to initialize the cooking process when an object is placed on the stove.
    // It resets the frying timer and sets the stove state to Frying.
    // The kitchenObjectSOIndex is sent so that clients can look up the corresponding frying recipe.
    [ServerRpc(RequireOwnership = false)]
    private void InteractLogicPlaceObjectOnCounterServerRpc(int kitchenObjectSOIndex)
    {
        fryingTimer.Value = 0f; // Reset the frying progress timer.
        state.Value = State.Frying; // Set the state to start frying.

        // Call a ClientRpc to assign the correct frying recipe on all clients.
        SetFryingRecipeSOClientRpc(kitchenObjectSOIndex);
    }

    // ClientRpc to set the frying recipe based on the provided KitchenObjectSO index.
    // This ensures that all clients are using the same recipe for frying.
    [ClientRpc]
    private void SetFryingRecipeSOClientRpc(int kitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        // Retrieve the appropriate frying recipe for this kitchen object.
        fryingRecipeSO = GetFryingRecipeSOWithInput(kitchenObjectSO);
    }

    // ClientRpc to set the burning recipe on all clients.
    // This is called when frying completes and burning begins.
    [ClientRpc]
    private void SetBurningRecipeSOClientRpc(int kitchenObjectSOIndex)
    {
        KitchenObjectSO kitchenObjectSO = KitchenGameMultiplayer.Instance.GetKitchenObjectSOFromIndex(kitchenObjectSOIndex);

        // Retrieve the appropriate burning recipe for this kitchen object.
        burningRecipeSO = GetBurningRecipeSOWithInput(kitchenObjectSO);
    }

    // Helper method to check if a given KitchenObjectSO has a valid frying recipe.
    private bool HasRecipeWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        return GetFryingRecipeSOWithInput(inputKitchenObjectSO) != null;
    }

    // Retrieves the FryingRecipeSO associated with the given input object.
    // It iterates over the fryingRecipeSOArray to find a match.
    private FryingRecipeSO GetFryingRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (FryingRecipeSO fryingRecipe in fryingRecipeSOArray)
        {
            if (fryingRecipe.input == inputKitchenObjectSO)
            {
                // Return the matching recipe.
                return fryingRecipe;
            }
        }
        // Return null if no valid recipe is found.
        return null;
    }

    // Returns the expected output KitchenObjectSO after frying the given input.
    // Uses the retrieved frying recipe to determine the result.
    private KitchenObjectSO GetOutputForInput(KitchenObjectSO inputKitchenObjectSO)
    {
        FryingRecipeSO fryingRecipe = GetFryingRecipeSOWithInput(inputKitchenObjectSO);
        return fryingRecipe != null ? fryingRecipe.output : null;
    }

    // Retrieves the BurningRecipeSO for the given input object.
    // It searches through the burningRecipeSOArray for the matching recipe.
    private BurningRecipeSO GetBurningRecipeSOWithInput(KitchenObjectSO inputKitchenObjectSO)
    {
        foreach (BurningRecipeSO burningRecipe in burningRecipeSOArray)
        {
            if (burningRecipe.input == inputKitchenObjectSO)
            {
                // Return the matching burning recipe.
                return burningRecipe;
            }
        }
        // Return null if no valid recipe is found.
        return null;
    }

    // Utility method to check if the food is in the Fried state.
    // This can be used by other scripts to determine if the food is ready to be picked up.
    public bool IsFried()
    {
        return state.Value == State.Fried;
    }
}
