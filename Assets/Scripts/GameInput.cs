using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages player input using Unity's new Input System.
/// Handles movement, interactions, and pausing actions, as well as key rebindings.
/// This class follows a singleton pattern to ensure only one instance exists at a time.
/// </summary>
public class GameInput : MonoBehaviour
{
    // Key used to store input bindings in PlayerPrefs for persistent key configurations.
    private const string PLAYER_PREFS_BINDINGS = "InputBindings";

    // Singleton instance to ensure only one GameInput instance exists across the game.
    public static GameInput Instance { get; private set; }

    // Events triggered when the player performs specific actions.
    public event EventHandler OnInteractAction;          // Triggered when the player performs the primary interaction (e.g., picking up objects).
    public event EventHandler OnInteractAlternateAction; // Triggered when the player performs an alternate interaction (e.g., cutting food).
    public event EventHandler OnPauseAction;             // Triggered when the player pauses the game.
    public event EventHandler OnBindingRebind;           // Triggered when we rebind a input.

    /// <summary>
    /// Enum representing different key bindings that can be customized by the player.
    /// </summary>
    public enum Binding
    {
        Move_Up,
        Move_Down,
        Move_Left,
        Move_Right,
        Interact,
        InteractAlternate,
        Pause
    }

    private PlayerInputActions playerInputActions; // Manages player input actions via the Unity Input System.

    private void Awake()
    {
        // Enforce the singleton pattern: if another instance exists, log an error.
        if (Instance != null)
        {
            Debug.LogError("Multiple instances of GameInput detected! Ensure only one exists.");
        }
        Instance = this;

        // Initialize input actions.
        playerInputActions = new PlayerInputActions();

        // Load saved key bindings if they exist in PlayerPrefs.
        if (PlayerPrefs.HasKey(PLAYER_PREFS_BINDINGS))
        {
            playerInputActions.LoadBindingOverridesFromJson(PlayerPrefs.GetString(PLAYER_PREFS_BINDINGS));
        }

        // Enable the player's input actions.
        playerInputActions.Player.Enable();

        // Subscribe to input action events to trigger respective game events when actions are performed.
        playerInputActions.Player.Interact.performed += Interact_performed;
        playerInputActions.Player.InteractAlternate.performed += InteractAlternate_performed;
        playerInputActions.Player.Pause.performed += Pause_performed;
    }

    /// <summary>
    /// Ensures proper cleanup when the GameInput object is destroyed (e.g., when switching scenes).
    /// Unsubscribes from input events and disposes of the input actions to prevent memory leaks.
    /// </summary>
    private void OnDestroy()
    {
        playerInputActions.Player.Interact.performed -= Interact_performed;
        playerInputActions.Player.InteractAlternate.performed -= InteractAlternate_performed;
        playerInputActions.Player.Pause.performed -= Pause_performed;

        playerInputActions.Dispose(); // Free up input system resources.
    }

    /// <summary>
    /// Called when the pause action is performed.
    /// Triggers the OnPauseAction event, allowing other game systems to respond.
    /// </summary>
    private void Pause_performed(InputAction.CallbackContext obj)
    {
        OnPauseAction?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Reads and normalizes player movement input to ensure consistent movement speed.
    /// </summary>
    /// <returns>A normalized Vector2 representing the player's movement direction.</returns>
    public Vector2 GetMovementVectorNormalized()
    {
        Vector2 inputVector = playerInputActions.Player.Move.ReadValue<Vector2>();
        return inputVector.normalized;
    }

    /// <summary>
    /// Called when the primary interact button is pressed.
    /// Triggers the OnInteractAction event to notify other game systems.
    /// </summary>
    private void Interact_performed(InputAction.CallbackContext obj)
    {
        OnInteractAction?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Called when the alternate interact button is pressed.
    /// Triggers the OnInteractAlternateAction event to notify other game systems.
    /// </summary>
    private void InteractAlternate_performed(InputAction.CallbackContext obj)
    {
        OnInteractAlternateAction?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Retrieves the currently assigned keybinding for a given action.
    /// </summary>
    /// <param name="binding">The action whose keybinding text is needed.</param>
    /// <returns>A string representing the key assigned to the given action.</returns>
    public string GetBindingText(Binding binding)
    {
        // The Move action has multiple bindings, where index 0 is a composite Vector2 input.
        // Therefore, movement keybindings start at index 1.
        switch (binding)
        {
            case Binding.Move_Up:
                return playerInputActions.Player.Move.bindings[1].ToDisplayString();
            case Binding.Move_Down:
                return playerInputActions.Player.Move.bindings[2].ToDisplayString();
            case Binding.Move_Left:
                return playerInputActions.Player.Move.bindings[3].ToDisplayString();
            case Binding.Move_Right:
                return playerInputActions.Player.Move.bindings[4].ToDisplayString();
            case Binding.Interact:
                return playerInputActions.Player.Interact.bindings[0].ToDisplayString();
            case Binding.InteractAlternate:
                return playerInputActions.Player.InteractAlternate.bindings[0].ToDisplayString();
            case Binding.Pause:
                return playerInputActions.Player.Pause.bindings[0].ToDisplayString();
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Allows the player to change key bindings dynamically.
    /// Temporarily disables inputs, listens for a new key, then reassigns and saves the new binding.
    /// </summary>
    /// <param name="binding">The action whose keybinding should be changed.</param>
    /// <param name="onActionRebound">Callback to execute once rebinding is complete (e.g., hide the UI prompt).</param>
    public void RebindBinding(Binding binding, Action onActionRebound)
    {
        playerInputActions.Player.Disable(); // Disable input to prevent conflicts during rebinding.

        InputAction inputAction;
        int bindingIndex;

        // Determine which input action and binding index to rebind.
        switch (binding)
        {
            case Binding.Move_Up:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 1;
                break;
            case Binding.Move_Down:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 2;
                break;
            case Binding.Move_Left:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 3;
                break;
            case Binding.Move_Right:
                inputAction = playerInputActions.Player.Move;
                bindingIndex = 4;
                break;
            case Binding.Interact:
                inputAction = playerInputActions.Player.Interact;
                bindingIndex = 0;
                break;
            case Binding.InteractAlternate:
                inputAction = playerInputActions.Player.InteractAlternate;
                bindingIndex = 0;
                break;
            case Binding.Pause:
                inputAction = playerInputActions.Player.Pause;
                bindingIndex = 0;
                break;
            default:
                return;
        }

        // Start the interactive rebinding process for the selected key.
        inputAction.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(callback =>
            {
                callback.Dispose();  // Dispose of the rebinding operation to free memory.
                playerInputActions.Player.Enable(); // Re-enable input actions.
                onActionRebound?.Invoke(); // Invoke the callback function (e.g., hide UI).

                // Save the updated bindings to PlayerPrefs for persistence.
                PlayerPrefs.SetString(PLAYER_PREFS_BINDINGS, playerInputActions.SaveBindingOverridesAsJson());
                PlayerPrefs.Save();

                // Trigger the rebind event
                OnBindingRebind?.Invoke(this, EventArgs.Empty);
            })
            .Start(); // Begin the rebinding process.
    }
}
