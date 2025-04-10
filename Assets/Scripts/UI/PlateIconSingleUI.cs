using UnityEngine;
using UnityEngine.UI;

// Represents a single ingredient icon in the UI for a plate.
public class PlateIconSingleUI : MonoBehaviour
{
    [SerializeField] private Image image; // UI Image component to display the ingredient's sprite.

    // Sets the sprite of the icon based on the given KitchenObjectSO.
    public void SetKitchenObjectSO(KitchenObjectSO kitchenObjectSO)
    {
        image.sprite = kitchenObjectSO.sprite;
    }
}
