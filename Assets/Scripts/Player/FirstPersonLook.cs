using UnityEngine;

public class FirstPersonLook : MonoBehaviour
{
    [SerializeField] Transform cameraPivot; // assign CameraPivot in Inspector
    [SerializeField] float sensitivity = 2.2f;
    [SerializeField] float minPitch = -70f, maxPitch = 85f;

    float pitch;

    void Start(){
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update(){
        float mx = Input.GetAxis("Mouse X") * sensitivity;
        float my = Input.GetAxis("Mouse Y") * sensitivity;

        // yaw: rotate the player root (turn left/right)
        transform.Rotate(0f, mx, 0f, Space.Self);

        // pitch: rotate the camera pivot (look up/down)
        pitch = Mathf.Clamp(pitch - my, minPitch, maxPitch);
        cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }
}
