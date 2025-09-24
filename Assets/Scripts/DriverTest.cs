using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class MotionProbe : MonoBehaviour
{
    Vector3 last;
    void Start() { last = transform.position; }
    void Update()
    {
        Vector3 delta = transform.position - last;
        last = transform.position;
        // shows world-space movement per second (m/s) ignoring Y
        Vector3 flat = new Vector3(delta.x, 0f, delta.z) / Mathf.Max(Time.deltaTime, 0.0001f);
        Debug.Log("[MotionProbe] speed=" + flat.magnitude.ToString("0.00") +
                  "  localX=" + transform.InverseTransformDirection(flat).x.ToString("0.00") +
                  "  localZ=" + transform.InverseTransformDirection(flat).z.ToString("0.00"));
    }
}
