using UnityEngine;

// Manages the UI representation of ingredients on the plate by displaying icons.
public class PlateIconsUI : MonoBehaviour
{
    [SerializeField] private PlateKitchenObject plateKitchenObject; // Reference to the PlateKitchenObject to track ingredients.
    [SerializeField] private Transform iconTemplate; // Template for the ingredient icons.

    private void Awake()
    {
        // Hide the icon template at the start to prevent it from being visible in the scene.
        iconTemplate.gameObject.SetActive(false);
    }

    private void Start()
    {
        // Subscribe to the event triggered when an ingredient is added to the plate.
        plateKitchenObject.OnIngredientAdded += PlateKitchenObject_OnIngredientAdded;
    }

    private void PlateKitchenObject_OnIngredientAdded(object sender, PlateKitchenObject.OnIngredientAddedEventArgs e)
    {
        // Update the UI to reflect the newly added ingredient.
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        // Remove all previously displayed icons, except the template.
        foreach (Transform child in transform)
        {
            if (child == iconTemplate)
            {
                continue; // Skip the template to keep it available for instantiation.
            }
            Destroy(child.gameObject);
        }

        // Loop through all ingredients on the plate and create icons for them.
        foreach (KitchenObjectSO kitchenObjectSO in plateKitchenObject.GetKitchenObjectSOList())
        {
            Transform iconTransform = Instantiate(iconTemplate, transform); // Create a new icon.
            iconTransform.gameObject.SetActive(true); // Make the icon visible.
            iconTransform.GetComponent<PlateIconSingleUI>().SetKitchenObjectSO(kitchenObjectSO); // Set the icon image.
        }
    }
}
