using UnityEngine;
using UnityEngine.UI;

// Manages the UI progress bar for objects implementing IHasProgress (e.g., CuttingCounter, StoveCounter).
public class ProgressBarUI : MonoBehaviour
{
    [SerializeField] private GameObject hasProgressGameObject; // Reference to the object implementing IHasProgress.
    [SerializeField] private Image barImage; // Reference to the progress bar UI element.

    private IHasProgress hasProgress; // Interface reference to track progress.

    private void Start()
    {
        // Get the IHasProgress component from the assigned GameObject.
        hasProgress = hasProgressGameObject.GetComponent<IHasProgress>();

        // Ensure the assigned GameObject has an IHasProgress component.
        if (hasProgress == null)
        {
            Debug.LogError("GameObject " + hasProgressGameObject + " does not implement IHasProgress!");
        }

        // Subscribe to progress change events.
        hasProgress.OnProgressChanged += HasProgress_OnProgressChanged;

        // Initialize progress bar UI state.
        barImage.fillAmount = 0f; // Set to empty at the start.

        // Hide the progress bar initially since there's no progress yet.
        // NOTE: Hiding after subscribing to events ensures UI updates when the object becomes active.
        Hide();
    }

    private void HasProgress_OnProgressChanged(object sender, IHasProgress.OnProgressChangedEventArgs e)
    {
        // Update the UI progress bar fill amount based on normalized progress.
        barImage.fillAmount = e.progressNormalized;

        // Show or hide the progress bar depending on progress state.
        if (e.progressNormalized == 0f || e.progressNormalized == 1f)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    // Makes the progress bar UI visible.
    private void Show()
    {
        gameObject.SetActive(true);
    }

    // Hides the progress bar UI.
    private void Hide()
    {
        gameObject.SetActive(false);
    }
}
