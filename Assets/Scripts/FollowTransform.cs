using UnityEngine;

// FollowTransform is a utility script that makes a GameObject follow the position and rotation of a target transform.
// This is used instead of direct parenting in multiplayer scenarios to ensure correct network synchronization.
public class FollowTransform : MonoBehaviour
{
    // The transform that this object should follow.
    private Transform targetTransform;

    // Sets the target transform to follow.
    public void SetTargetTransform(Transform targetTransform)
    {
        this.targetTransform = targetTransform;
    }

    // LateUpdate is used to ensure that the target has moved before updating this object's position.
    private void LateUpdate()
    {
        if (targetTransform == null)
        {
            return;
        }

        // Update both position and rotation to match the target.
        transform.position = targetTransform.position;
        transform.rotation = targetTransform.rotation;
    }
}
