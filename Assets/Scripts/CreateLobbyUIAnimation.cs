using System.Collections;
using UnityEngine;

public class CreateLobbyUIAnimation : MonoBehaviour
{
    // This parameter controls which animation plays:
    // When false, the PopOut (show) animation plays.
    // When true, the PopIn (hide) animation plays.
    private const string POP_ANIMATION_TRIGGER = "PopAnimationTrigger";

    // Reference to the Animator component
    [SerializeField] private Animator animator;

    // Duration of the PopIn animation (in seconds). Adjust to match your animation.
    [SerializeField] private float popInDuration = 0.5f;

    /// <summary>
    /// Call this when the UI is activated (Show). It will play the PopOut animation.
    /// </summary>
    public void PlayPopOutAnimation()
    {
        // Reset the parameter so the PopOut animation plays.
        animator.SetBool(POP_ANIMATION_TRIGGER, false);
    }

    /// <summary>
    /// Call this when the UI should be hidden (Hide). It will play the PopIn animation,
    /// then disable the UI after the animation completes.
    /// </summary>
    public void PlayPopInAnimation()
    {
        // Trigger the PopIn animation by setting the bool to true.
        animator.SetBool(POP_ANIMATION_TRIGGER, true);
        StartCoroutine(DisableAfterAnimation());
    }

    private IEnumerator DisableAfterAnimation()
    {
        // Wait for the pop in animation to finish before deactivating.
        yield return new WaitForSeconds(popInDuration);
        gameObject.SetActive(false);
    }
}
