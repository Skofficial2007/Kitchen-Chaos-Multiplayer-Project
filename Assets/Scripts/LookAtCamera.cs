using UnityEngine;

public class LookAtCamera : MonoBehaviour
{
    // Enum to define different orientation modes
    private enum Mode
    {
        LookAt,                 // Object faces the camera
        LookAtInverted,         // Object faces away from the camera
        CameraForward,          // Object aligns with the camera's forward direction
        CameraForwardInverted   // Object aligns opposite to the camera's forward direction
    }

    [SerializeField] private Mode mode;

    private void LateUpdate()
    {
        switch (mode)
        {
            case Mode.LookAt:
                // Makes the object look directly at the camera
                transform.LookAt(Camera.main.transform);
                break;

            case Mode.LookAtInverted:
                // Calculates a direction away from the camera and applies it
                Vector3 dirFromCamera = transform.position - Camera.main.transform.forward;
                transform.LookAt(transform.position + dirFromCamera);
                break;

            case Mode.CameraForward:
                // Aligns the object's forward direction with the camera's forward direction
                transform.forward = Camera.main.transform.forward;
                break;

            case Mode.CameraForwardInverted:
                // Aligns the object's forward direction opposite to the camera's forward direction
                transform.forward = -Camera.main.transform.forward;
                break;
        }
    }
}
