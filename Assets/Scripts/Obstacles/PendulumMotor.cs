using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PendulumMotor : MonoBehaviour
{
    [Header("Motion")]
    [Tooltip("Peak swing each side, in degrees.")]
    public float maxAngle = 45f;

    [Tooltip("Cycles per second. 0.5 = one full left↔right swing every 2 seconds.")]
    public float frequency = 0.5f;

    [Tooltip("Starting phase offset (degrees along the sine wave).")]
    public float startPhaseDeg = 0f;

    [Header("Axis")]
    [Tooltip("Local axis to rotate around (X, Y, or Z). Example: use Z to swing forward/back if your arm points down Y.")]
    public Vector3 localAxis = Vector3.forward;

    Rigidbody rb;
    Quaternion restRotation;     // rotation at Awake (the 'hanging straight down' pose)
    Vector3 worldAxis;           // localAxis expressed in world space
    float phaseRad;

    void OnValidate()
    {
        // keep sane values
        if (frequency < 0f) frequency = 0f;
        if (localAxis.sqrMagnitude < 1e-6f) localAxis = Vector3.forward;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.isKinematic = true;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        restRotation = transform.rotation;
        worldAxis    = transform.TransformDirection(localAxis.normalized);
        phaseRad     = startPhaseDeg * Mathf.Deg2Rad;
    }

    void FixedUpdate()
    {
        // angle(t) = sin(2π f t + phase) * max
        float ang = Mathf.Sin((Time.time * frequency * Mathf.PI * 2f) + phaseRad) * maxAngle;

        // rotate around the ORIGINAL restRotation & axis (no accumulation drift)
        Quaternion target = restRotation * Quaternion.AngleAxis(ang, worldAxis);
        rb.MoveRotation(target);
    }

    /// <summary>Call this if you re-orient the pivot at edit-time to make the new pose the 'rest' pose.</summary>
    public void RecalibrateRestPose()
    {
        restRotation = transform.rotation;
        worldAxis    = transform.TransformDirection(localAxis.normalized);
    }
}
