using System.Runtime.InteropServices;
using UnityEngine;

public class DeliveryManagerUI : MonoBehaviour
{
    [SerializeField] private Transform container; // Parent container that holds recipe UI elements.
    [SerializeField] private Transform recipeTemplate; // Template for displaying a recipe in the UI.

    private void Awake()
    {
        // Hide the recipe template by default (it will be cloned to display actual recipes).
        recipeTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Subscribe to events triggered when recipes are added or completed.
        DeliveryManager.Instance.OnRecipeSpawned += DeliveryManager_OnRecipeSpawned;
        DeliveryManager.Instance.OnRecipeCompleted += DeliveryManager_OnRecipeCompleted;

        // Ensure the UI is cleared and updated at the start.
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeCompleted(object sender, System.EventArgs e)
    {
        // Update the UI when a recipe is completed.
        UpdateVisual();
    }

    private void DeliveryManager_OnRecipeSpawned(object sender, System.EventArgs e)
    {
        // Update the UI when a new recipe is spawned.
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // Clear previous recipe UI elements before updating.
        foreach (Transform child in container)
        {
            if (child == recipeTemplate)
            {
                continue; // Keep the template itself.
            }
            Destroy(child.gameObject); // Remove old recipe UI elements.
        }

        // Display all currently waiting recipes.
        foreach (RecipeSO recipeSO in DeliveryManager.Instance.GetWaitingRecipeSOList())
        {
            Transform recipeTransform = Instantiate(recipeTemplate, container); // Create a new UI element for the recipe.
            recipeTransform.gameObject.SetActive(true); // Make the new UI element visible.

            // Set recipe name for name
            recipeTransform.GetComponent<DeliveryManagerSingleUI>().SetRecipeSOName(recipeSO);
        }
    }
}
