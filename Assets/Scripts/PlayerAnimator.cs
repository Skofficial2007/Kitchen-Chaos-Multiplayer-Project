using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Handles the player's animation based on movement.
/// - Uses a NetworkBehaviour to ensure multiplayer compatibility.
/// - Only updates animations on the owner client to prevent animation desync.
/// </summary>
public class PlayerAnimator : NetworkBehaviour
{
    private const string IS_WALKING = "IsWalking"; // Animator parameter name.

    [SerializeField] private Player player; // Reference to the Player script to check movement state.

    private Animator animator; // Reference to the Animator component.

    private void Awake()
    {
        animator = GetComponent<Animator>(); // Get the Animator component attached to this GameObject.
    }

    private void Update()
    {
        // Only update animation if this is the local player (owner)
        // This prevents unnecessary updates on other clients, reducing animation glitches.
        if (!IsOwner)
        {
            return;
        }

        // Set the walking animation state based on whether the player is currently walking.
        animator.SetBool(IS_WALKING, player.IsWalking());
    }
}
