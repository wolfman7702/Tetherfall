using UnityEngine;

public class FPSLook : MonoBehaviour
{
    [Header("Assign")]
    public Transform playerBody;     // the Player (capsule)
    public Transform cameraPivot;    // the CameraPivot (eye height)

    [Header("Sensitivity")]
    public float mouseSensitivity = 6f; // raise if too slow

    [Header("Pitch Clamp")]
    public float minPitch = -70f;
    public float maxPitch = 85f;

    float yaw;   // rotates the body (Y)
    float pitch; // rotates the camera (X)

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Initialize yaw/pitch from current transforms
        yaw = playerBody.eulerAngles.y;
        pitch = cameraPivot.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f; // convert to -180..180
    }

    void Update()
    {
        float mx = Input.GetAxis("Mouse X");
        float my = Input.GetAxis("Mouse Y");

        yaw   += mx * mouseSensitivity * 100f * Time.deltaTime;
        pitch -= my * mouseSensitivity * 100f * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        playerBody.rotation        = Quaternion.Euler(0f, yaw, 0f);
        cameraPivot.localRotation  = Quaternion.Euler(pitch, 0f, 0f);
    }
}
