using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerAnimDriver : MonoBehaviour
{
    [SerializeField] Animator animator;            // drag Bear_Hero’s Animator
    [SerializeField] float runMagnitudeSpeed = 5f; // your top run m/s maps to 1.0
    [SerializeField] float damp = 0.12f;

    CharacterController cc;
    Vector3 lastPos;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!animator) animator = GetComponentInChildren<Animator>(); // self-wire safety
        lastPos = transform.position;
    }

    void Update()
    {
        if (!animator) return;
        float dt = Time.deltaTime; if (dt <= 0f) return;

        // world-space velocity from displacement
        Vector3 worldVel = (transform.position - lastPos) / dt;
        lastPos = transform.position;

        Vector3 flat = new Vector3(worldVel.x, 0f, worldVel.z);
        float speed = flat.magnitude;
        float mag01 = runMagnitudeSpeed > 0 ? Mathf.Clamp01(speed / runMagnitudeSpeed) : 0f;

        Vector3 local = transform.InverseTransformDirection(flat);
        float x = runMagnitudeSpeed > 0 ? Mathf.Clamp(local.x / runMagnitudeSpeed, -1f, 1f) : 0f;
        float y = runMagnitudeSpeed > 0 ? Mathf.Clamp(local.z / runMagnitudeSpeed, -1f, 1f) : 0f;

        SetFloatIfExists("InputMagnitude", mag01, damp, dt);
        SetFloatIfExists("InputX", x, damp, dt);
        SetFloatIfExists("InputY", y, damp, dt);
        SetBoolIfExists("IsGrounded", cc ? cc.isGrounded : true);
    }

    void SetFloatIfExists(string n, float v, float d, float dt)
    {
        foreach (var p in animator.parameters)
            if (p.name == n && p.type == AnimatorControllerParameterType.Float)
            { animator.SetFloat(n, v, d, dt); return; }
    }
    void SetBoolIfExists(string n, bool v)
    {
        foreach (var p in animator.parameters)
            if (p.name == n && p.type == AnimatorControllerParameterType.Bool)
            { animator.SetBool(n, v); return; }
    }
}
