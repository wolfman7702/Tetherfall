using UnityEngine;

public class ExternalForceReceiver : MonoBehaviour
{
    [Tooltip("Current externally applied velocity (world space).")]
    public Vector3 velocity;

    [Range(0f, 20f)] public float decay = 6f; // m/s^2 toward zero

    void Update()
    {
        if (velocity.sqrMagnitude > 0f)
            velocity = Vector3.MoveTowards(velocity, Vector3.zero, decay * Time.deltaTime);
    }

    public void AddImpact(Vector3 worldDir, float speed)
    {
        worldDir.y = 0f;
        if (worldDir.sqrMagnitude < 1e-6f) return;
        velocity += worldDir.normalized * speed;
    }
}
