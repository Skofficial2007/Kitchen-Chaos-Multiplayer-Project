using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class DeliveryManager : NetworkBehaviour
{
    // Event that will be triggered when recipe gets spawned
    public event EventHandler OnRecipeSpawned;

    // Event that will be triggered when recipe gets completed
    public event EventHandler OnRecipeCompleted;

    // Event that will be triggered when a recipe is successfully completed in time to trigger sound
    public event EventHandler OnRecipeSuccess;

    // Event that will be triggered when a recipe is not completed in time to trigger sound
    public event EventHandler OnRecipeFailed;

    // Singleton Pattern: Ensures there's only one instance of DeliveryManager in the scene.
    public static DeliveryManager Instance { get; private set; }

    // List of valid recipes that can be spawned.
    [SerializeField] private RecipeListSO recipeListSO;

    // List of recipes that are waiting to be delivered by the player.
    private List<RecipeSO> waitingRecipeSOList;

    private float spawnRecipeTimer; // Timer that controls when a new recipe should spawn.
    private float spawnRecipeTimerMax = 4f; // Maximum time before spawning a new recipe.
    private int waitingRecipeMax = 4; // Max number of recipes that can be waiting at the same time.
    private int successfulRecipeAmount = 0; // To keep track of the number of successfully delivered recipes.

    private void Awake()
    {
        // Ensures there is only one instance of DeliveryManager.
        if (Instance != null)
        {
            Debug.LogError("There is more than one DeliveryManager Instance");
        }
        Instance = this;

        // Initialize the waiting recipe list when the game starts.
        waitingRecipeSOList = new List<RecipeSO>();
    }

    // We want this logic to only run on the server.
    // When a client tries to deliver a recipe, the server will handle the logic.
    private void Update()
    {
        if (!IsServer)
        {
            return; // Ensure only the server handles recipe spawning.
        }

        // Reduce the spawn timer every frame.
        spawnRecipeTimer -= Time.deltaTime;

        // Check if it's time to spawn a new recipe.
        if (spawnRecipeTimer <= 0f)
        {
            // Reset the spawn timer.
            spawnRecipeTimer = spawnRecipeTimerMax;

            // Ensure there are not more than the max allowed waiting recipes.
            if (KitchenGameManager.Instance.IsGamePlaying() && waitingRecipeSOList.Count < waitingRecipeMax)
            {
                // Pick a random recipe from the recipe list and add it to the waiting list.
                int waitingRecipeSOIndex = UnityEngine.Random.Range(0, recipeListSO.recipeSOList.Count);

                // Server tells all clients to spawn a new recipe.
                SpawnNewWaitingRecipeClientRpc(waitingRecipeSOIndex);
            }
        }
    }

    // NOTE: The host runs both server and client logic.
    // To prevent the same recipe from being spawned twice on the host, 
    // we use a ClientRpc so that each client, including the host, handles spawning.

    [ClientRpc]
    private void SpawnNewWaitingRecipeClientRpc(int waitingRecipeSOIndex)
    {
        RecipeSO waitingRecipeSO = recipeListSO.recipeSOList[waitingRecipeSOIndex];

        waitingRecipeSOList.Add(waitingRecipeSO);

        // Trigger the event as a recipe is spawned.
        OnRecipeSpawned?.Invoke(this, EventArgs.Empty);
    }

    // Clients handle the delivery logic and send results to the server.
    // The server verifies the result and notifies all clients if the recipe was correct or incorrect.

    public void DeliveryRecipe(PlateKitchenObject plateKitchenObject)
    {
        // Iterate through all the waiting recipes to see if one matches the plate.
        for (int i = 0; i < waitingRecipeSOList.Count; i++)
        {
            RecipeSO waitingRecipeSO = waitingRecipeSOList[i];

            // Check if the number of ingredients on the plate matches the recipe.
            if (waitingRecipeSO.kitchenObjectSOList.Count == plateKitchenObject.GetKitchenObjectSOList().Count)
            {
                bool plateContentsMatchesRecipe = true; // Assume ingredients match initially.

                // Check if all ingredients on the plate match the recipe.
                foreach (KitchenObjectSO recipeKitchenObjectSO in waitingRecipeSO.kitchenObjectSOList)
                {
                    bool ingredientFound = false;

                    // Check each ingredient on the plate to see if it matches.
                    foreach (KitchenObjectSO plateKitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
                    {
                        if (plateKitchenObjectSO == recipeKitchenObjectSO)
                        {
                            ingredientFound = true; // Ingredient found on the plate.
                            break;
                        }
                    }

                    // If any ingredient from the recipe is missing on the plate, the recipe doesn't match.
                    if (!ingredientFound)
                    {
                        plateContentsMatchesRecipe = false;
                        break;
                    }
                }

                // If the plate contains the correct ingredients, deliver the recipe.
                if (plateContentsMatchesRecipe)
                {
                    // Notify the server that the client has delivered the correct recipe.
                    DeliverCorrectRecipeServerRpc(i);
                    return; // Recipe successfully delivered, exit the method.
                }
            }
        }

        // No matches found!
        // Notify the server that the client has delivered an incorrect recipe.
        DeliverIncorrectRecipeServerRpc();
    }

    // This method runs on the server.
    // The server then informs all clients about the correct delivery.

    [ServerRpc(RequireOwnership = false)] // Allow any client to call this, not just the owner.
    private void DeliverCorrectRecipeServerRpc(int waitingRecipeSOListIndex)
    {
        // Notify all clients that a correct recipe was delivered.
        DeliverCorrectRecipeClientRpc(waitingRecipeSOListIndex);
    }

    // The server informs all clients to process the correct recipe delivery logic.

    [ClientRpc]
    private void DeliverCorrectRecipeClientRpc(int waitingRecipeSOListIndex)
    {
        // Increase the number of successful recipes delivered.
        successfulRecipeAmount++;

        // Remove the delivered recipe from the waiting list.
        waitingRecipeSOList.RemoveAt(waitingRecipeSOListIndex);

        // Trigger the event as the recipe gets completed.
        OnRecipeCompleted?.Invoke(this, EventArgs.Empty);

        // Trigger Success event.
        OnRecipeSuccess?.Invoke(this, EventArgs.Empty);
    }

    // This method runs on the server when a client delivers an incorrect recipe.

    [ServerRpc(RequireOwnership = false)]
    private void DeliverIncorrectRecipeServerRpc()
    {
        // Notify all clients about the failed delivery.
        DeliverIncorrectRecipeClientRpc();
    }

    // The server informs all clients to trigger the failure logic.

    [ClientRpc]
    private void DeliverIncorrectRecipeClientRpc()
    {
        // Player delivered an incorrect recipe.
        OnRecipeFailed?.Invoke(this, EventArgs.Empty);
    }

    // Returns the list of currently waiting recipes.
    public List<RecipeSO> GetWaitingRecipeSOList()
    {
        return waitingRecipeSOList;
    }

    // Returns the number of successfully delivered recipes.
    public int GetSuccessfulRecipeAmount()
    {
        return successfulRecipeAmount;
    }
}
