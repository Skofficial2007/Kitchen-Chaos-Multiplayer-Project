using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DeliveryManagerSingleUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI recipeNameText; // UI text for displaying the recipe name.
    [SerializeField] private Transform iconContainer; // Container for ingredient icons.
    [SerializeField] private Transform iconTemplate; // Template for displaying an ingredient icon.

    private void Awake()
    {
        // Hide the icon template by default (it will be cloned to display actual ingredients).
        iconTemplate.gameObject.SetActive(false);
    }

    // Updates the UI with the recipe name and its ingredient icons.
    public void SetRecipeSOName(RecipeSO recipeSO)
    {
        // Set the recipe name in the UI.
        recipeNameText.text = recipeSO.recipeName;

        // Clear any existing ingredient icons before updating.
        foreach (Transform child in iconContainer)
        {
            if (child == iconTemplate)
            {
                continue; // Keep the template intact.
            }
            Destroy(child.gameObject); // Remove old ingredient icons.
        }

        // Create an icon for each ingredient in the recipe.
        foreach (KitchenObjectSO kitchenObjectSO in recipeSO.kitchenObjectSOList)
        {
            Transform iconTransform = Instantiate(iconTemplate, iconContainer); // Create a new icon.
            iconTransform.gameObject.SetActive(true); // Make the icon visible.
            iconTransform.GetComponent<Image>().sprite = kitchenObjectSO.sprite; // Set the ingredient image.
        }
    }
}
