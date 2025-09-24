using UnityEngine;

public class ThirdPersonFollow : MonoBehaviour
{
    [SerializeField] Transform target;  // assign CameraPivot here
    [SerializeField] Vector3 offset = new Vector3(0, 2f, -4f);
    [SerializeField] float followSpeed = 10f;
    [SerializeField] float rotateSpeed = 120f;

    float yaw, pitch;
    [SerializeField] float minPitch = -30f, maxPitch = 60f;

    void LateUpdate()
    {
        if (!target) return;

        // Mouse look (hold RMB for free look, optional)
        if (Cursor.lockState == CursorLockMode.Locked || Input.GetMouseButton(1))
        {
            yaw += Input.GetAxis("Mouse X") * rotateSpeed * Time.deltaTime;
            pitch -= Input.GetAxis("Mouse Y") * rotateSpeed * Time.deltaTime;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
        }

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0);
        Vector3 desiredPos = target.position + rot * offset;

        transform.position = Vector3.Lerp(transform.position, desiredPos, followSpeed * Time.deltaTime);
        transform.LookAt(target.position + Vector3.up * 1.2f);
    }
}
