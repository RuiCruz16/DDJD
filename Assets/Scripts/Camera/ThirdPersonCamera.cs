using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    [Header("Target")]
    public Transform target; 

    [Header("Orbital Positioning")]
    public float distance = 8f;       
    public float heightOffset = 2f;   

    [Header("Mouse Sensitivity")]
    public float lookSpeed = 3f;
    public float minPitch = 0f;  // Minimum angle (looking up)
    public float maxPitch = 70f; // Maximum angle (looking down)

    private float currentYaw;
    private float currentPitch = 25f; // Initial downward tilt

    void Start()
    {
        // Lock and hide the cursor for continuous camera movement
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void LateUpdate()
    {
        if (target == null) return;

        if (Inventory.isInventoryOpen || Time.timeScale == 0f)
            return;

        // Read mouse input
        currentYaw += Input.GetAxis("Mouse X") * lookSpeed;
        currentPitch -= Input.GetAxis("Mouse Y") * lookSpeed;
        
        // Clamp pitch to prevent the camera from flipping over
        currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);

        // Calculate rotation
        Quaternion rotation = Quaternion.Euler(currentPitch, currentYaw, 0);

        // Calculate position
        Vector3 focusPoint = target.position + (Vector3.up * heightOffset);
        Vector3 desiredPosition = focusPoint - (rotation * Vector3.forward * distance);

        transform.position = desiredPosition;
        transform.rotation = rotation;
    }
}
