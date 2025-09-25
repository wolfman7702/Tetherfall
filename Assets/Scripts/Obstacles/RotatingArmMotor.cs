using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody))]
public class RotatingArmMotor : MonoBehaviour
{
    [Tooltip("Degrees per second. Positive = CCW looking along axis.")]
    public float degreesPerSecond = 100f;

    [Tooltip("Local axis to rotate around (usually Y).")]
    public Vector3 localAxis = Vector3.up;

    [Tooltip("Start angle in degrees.")]
    public float startAngle = 0f;

    Rigidbody rb;
    Vector3 worldAxis;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        worldAxis = transform.TransformDirection(localAxis.normalized);

        var start = transform.rotation * Quaternion.AngleAxis(startAngle, worldAxis);
        rb.MoveRotation(start);
    }

    void FixedUpdate()
    {
        float step = degreesPerSecond * Time.fixedDeltaTime;
        var delta = Quaternion.AngleAxis(step, worldAxis);
        rb.MoveRotation(rb.rotation * delta);
    }
}
