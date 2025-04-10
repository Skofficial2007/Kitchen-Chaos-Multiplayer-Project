using UnityEngine;

public class PlayerSounds : MonoBehaviour
{
    private Player player;
    private float footstepTimer; // Timer to control footstep sound playback
    private float footstepTimerMax = 0.1f; // Time interval between footstep sounds

    private void Awake()
    {
        // Get reference to Player script attached to this GameObject
        player = GetComponent<Player>();
    }

    private void Update()
    {
        // Decrease the footstep timer over time
        footstepTimer -= Time.deltaTime;

        // Check if it's time to play a footstep sound
        if (footstepTimer < 0f)
        {
            footstepTimer = footstepTimerMax; // Reset timer

            // Play footstep sound only if the player is walking
            if (player.IsWalking())
            {
                float volume = 1f;
                SoundManager.Instance.PlayFootstepSound(player.transform.position, volume);
            }
        }
    }
}
