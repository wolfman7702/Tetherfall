using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FirstPersonAnimDriver : MonoBehaviour
{
    [Header("Animator")]
    [SerializeField] private Animator animator;          // auto-fills if left empty
    [SerializeField] private string speedParam = "Speed";
    [SerializeField] private string groundedParam = "IsGrounded";
    [SerializeField] private string jumpParam = "Jump";

    [Header("Tuning")]
    [Tooltip("Smoothing for Speed param")]
    [SerializeField] private float dampTime = 0.12f;
    [Tooltip("Scales raw m/s into your blend tree's thresholds")]
    [SerializeField] private float speedScale = 1.0f;
    [Tooltip("Ignore tiny jitter below this speed")]
    [SerializeField] private float deadzone = 0.05f;
    [Tooltip("Clamp Speed sent to Animator")]
    [SerializeField] private float maxSpeed = 7.0f;

    [Header("Ground Check (fallback if no CharacterController)")]
    [SerializeField] private float groundRayUp = 0.2f;
    [SerializeField] private float groundRayDown = 0.6f;
    [SerializeField] private LayerMask groundMask = ~0;

    CharacterController cc;
    Rigidbody rb;
    Vector3 lastPos;
    int speedHash, groundedHash, jumpHash;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        rb = GetComponent<Rigidbody>();

        if (!animator) animator = GetComponentInChildren<Animator>(true);

        speedHash = Animator.StringToHash(speedParam);
        groundedHash = Animator.StringToHash(groundedParam);
        jumpHash = Animator.StringToHash(jumpParam);

        lastPos = transform.position;

        // Helpful defaults if someone forgot to set Animator flags
        if (animator)
        {
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.applyRootMotion = false; // we drive motion via controller
        }
    }

    void Update()
    {
        if (!animator) return;

        // --- 1) Compute horizontal speed (m/s) ---
        float horizSpeed = 0f;

        if (cc)
        {
            Vector3 v = cc.velocity;
            horizSpeed = new Vector2(v.x, v.z).magnitude;
        }
        else if (rb)
        {
            Vector3 v = rb.velocity;
            horizSpeed = new Vector2(v.x, v.z).magnitude;
        }
        else
        {
            // Fallback for transform-based movement
            Vector3 now = transform.position;
            Vector3 delta = now - lastPos; delta.y = 0f;
            horizSpeed = delta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            lastPos = now;
        }

        // Scale, deadzone, clamp
        horizSpeed *= Mathf.Max(0.0001f, speedScale);
        if (horizSpeed < deadzone) horizSpeed = 0f;
        horizSpeed = Mathf.Min(horizSpeed, maxSpeed);

        // --- 2) Grounded ---
        bool grounded = cc ? cc.isGrounded : RayGroundedFallback();

        // --- 3) Feed the Animator (damped Speed) ---
        animator.SetFloat(speedHash, horizSpeed, dampTime, Time.deltaTime);
        animator.SetBool(groundedHash, grounded);

        // --- 4) Optional Jump trigger (only if your controller uses it) ---
        if (grounded && (Input.GetButtonDown("Jump") || Input.GetKeyDown(KeyCode.Space)))
        {
            // Only trigger if the param exists (avoids console spam)
            if (HasParam(animator, jumpHash, AnimatorControllerParameterType.Trigger))
                animator.SetTrigger(jumpHash);
        }
    }

    bool RayGroundedFallback()
    {
        // Cast a short ray downward from just above the feet.
        Vector3 origin = transform.position + Vector3.up * groundRayUp;
        float dist = groundRayUp + groundRayDown;
        return Physics.Raycast(origin, Vector3.down, dist, groundMask, QueryTriggerInteraction.Ignore);
    }

    static bool HasParam(Animator anim, int nameHash, AnimatorControllerParameterType type)
    {
        if (!anim) return false;
        foreach (var p in anim.parameters)
            if (p.nameHash == nameHash && p.type == type) return true;
        return false;
    }
}
