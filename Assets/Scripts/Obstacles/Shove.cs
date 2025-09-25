using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PushOnTrigger : MonoBehaviour
{
    [Tooltip("Shove magnitude in m/s added to player on contact.")]
    public float pushSpeed = 8f;

    void Reset()
    {
        var c = GetComponent<Collider>();
        if (c) c.isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        var recv = other.GetComponentInParent<ExternalForceReceiver>();
        if (!recv) return;

        // Push outward from the arm, horizontally
        Vector3 dir = other.transform.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 1e-6f) dir = transform.right; // fallback
        recv.AddImpact(dir, pushSpeed);
    }
}
