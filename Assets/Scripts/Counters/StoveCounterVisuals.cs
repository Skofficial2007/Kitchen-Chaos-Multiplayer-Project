using UnityEngine;

public class StoveCounterVisuals : MonoBehaviour
{
    [SerializeField] private StoveCounter stoveCounter; // Reference to the stove counter.
    [SerializeField] private GameObject stoveOnGameObject; // Visual representation of the stove being on.
    [SerializeField] private GameObject particlesGameObject; // Particle effects (e.g., smoke, sizzling).

    private void Start()
    {
        // Subscribe to state changes in StoveCounter.
        stoveCounter.OnStateChanged += StoveCounter_OnStateChanged;
    }

    // Handles the visual update when the stove's state changes.
    private void StoveCounter_OnStateChanged(object sender, StoveCounter.OnStateChangeEventArgs e)
    {
        // Show visuals if the stove is in the Frying or Fried state.
        bool showVisual = e.state == StoveCounter.State.Frying || e.state == StoveCounter.State.Fried;

        if (showVisual)
        {
            Show();
        }
        else
        {
            Hide();
        }
    }

    // Activates stove visuals (turns on stove glow and particles).
    private void Show()
    {
        stoveOnGameObject?.SetActive(true);
        particlesGameObject?.SetActive(true);
    }

    // Deactivates stove visuals (turns off stove glow and particles).
    private void Hide()
    {
        stoveOnGameObject?.SetActive(false);
        particlesGameObject?.SetActive(false);
    }
}
