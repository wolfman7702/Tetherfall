using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Speeds (m/s)")]
    [SerializeField] public float walkSpeed = 2.0f;
    [SerializeField] public float runSpeed  = 4.0f;

    [Header("Jump & Gravity")]
    [SerializeField] float jumpHeight = 1.2f;       // ~1.0–1.6 m
    [SerializeField] float gravity = -9.81f;
    [SerializeField] float fallMultiplier = 2.4f;   // 2.2–2.6 reduces hang-time
    [SerializeField] float lowJumpMultiplier = 2.1f;
    [SerializeField] float terminalVel = -55f;

    [Header("External Multipliers (kept for compatibility)")]
    [Tooltip("Writable by other systems (e.g., TetherLink). Not applied to speed here.")]
    public float pcMoveMultiplier = 1.0f; // kept so TetherLink compiles; NOT used for movement

    [Header("Reliable Ground Check")]
    [Tooltip("Empty child at feet; slightly above the soles.")]
    [SerializeField] Transform groundCheck;          // assign in Inspector
    [SerializeField] float groundCheckRadius = 0.25f;
    [SerializeField] LayerMask groundMask = ~0;      // set to your Ground/Terrain layers

    [Header("Jump QoL")]
    [Tooltip("Grace time after leaving ground during which a jump still counts.")]
    [SerializeField] float coyoteTime = 0.10f;       // seconds
    [Tooltip("Time window to buffer a jump pressed slightly before landing.")]
    [SerializeField] float jumpBuffer = 0.12f;       // seconds

    CharacterController cc;
    Vector3 velocity;            // vertical in y
    Vector3 lastMoveWorld;       // world-space planar move this frame

    // Timers/state for reliable jumping
    float coyoteTimer;
    float bufferTimer;
    bool grounded;               // our reliable grounded (cc OR sphere)
    bool jumpPressedThisFrame;   // consumed by PlayerAnimDriver if you use it

    // --- Exposed for Animator driver ---
    public bool IsGrounded => grounded;
    public float VerticalVelocity => velocity.y;
    public float CurrentPlanarSpeed { get; private set; } // m/s (X/Z only)
    public float LocalMoveX { get; private set; }         // -1..1 (left/right)
    public float LocalMoveY { get; private set; }         // -1..1 (forward/back)
    public float Speed01ForAnimator { get; private set; } // 0..1 based on chosen tier

    void OnValidate()
    {
        // Keep sensible relationships if values got tweaked in Inspector
        if (walkSpeed < 0.1f) walkSpeed = 0.1f;
        if (runSpeed < walkSpeed + 0.01f) runSpeed = walkSpeed + 0.01f;
        if (groundCheckRadius < 0.05f) groundCheckRadius = 0.05f;
        if (coyoteTime < 0f) coyoteTime = 0f;
        if (jumpBuffer < 0f) jumpBuffer = 0f;
    }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // ---- INPUT (character-relative) ----
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        const float deadzone = 0.12f;
        if (input.magnitude < deadzone) input = Vector2.zero;

        // ---- SPEED TIER: WALK by default, RUN with LeftShift ----
        bool wantRun = Input.GetKey(KeyCode.LeftShift);

        // Base speed is the *tier*
        float baseSpeed =
            input == Vector2.zero ? 0f :
            (wantRun ? runSpeed : walkSpeed);

        // This is what the Animator should use for its 1D blend (walk≈0.5, run=1.0)
        Speed01ForAnimator = (runSpeed > 0f) ? Mathf.Clamp01(baseSpeed / runSpeed) : 0f;

        // NOTE: Stabilized build — we do NOT apply pcMoveMultiplier to movement speed here.
        float targetSpeed = baseSpeed;

        // ---- HORIZONTAL MOVE ----
        Vector3 worldMove = (transform.right * input.x + transform.forward * input.y) * targetSpeed;
        lastMoveWorld = worldMove;
        cc.Move(worldMove * dt);

        // Localized inputs for 2D Freeform (normalize to -1..1 based on chosen tier speed)
        Vector3 local = transform.InverseTransformDirection(worldMove);
        float denom = Mathf.Max(0.0001f, targetSpeed);
        LocalMoveX = Mathf.Clamp(local.x / denom, -1f, 1f);
        LocalMoveY = Mathf.Clamp(local.z / denom, -1f, 1f);

        // ---- RELIABLE GROUND CHECK ----
        bool ccGrounded = cc.isGrounded;
        bool sphereGrounded = false;
        if (groundCheck != null)
            sphereGrounded = Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);

        grounded = ccGrounded || sphereGrounded;

        // ---- TIMERS: coyote + jump buffer ----
        if (grounded) coyoteTimer = coyoteTime;
        else          coyoteTimer = Mathf.Max(0f, coyoteTimer - dt);

        if (Input.GetButtonDown("Jump")) bufferTimer = jumpBuffer;
        else                             bufferTimer = Mathf.Max(0f, bufferTimer - dt);

        // ---- STICK TO GROUND ----
        if (grounded && velocity.y < 0f)
            velocity.y = -2f;

        // ---- PERFORM JUMP (buffered + coyote) ----
        jumpPressedThisFrame = false;
        if (bufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight); // initial v from height
            bufferTimer = 0f;
            coyoteTimer = 0f;
            jumpPressedThisFrame = true;

            // OPTIONAL: fire Animator "Jump" trigger if it exists (won't break if it doesn't)
            var anim = GetComponentInChildren<Animator>();
            if (anim != null && HasParam(anim, "Jump", AnimatorControllerParameterType.Trigger))
                anim.SetTrigger("Jump");
        }

        // ---- BETTER GRAVITY ----
        bool rising = velocity.y > 0f;
        bool jumpHeld = Input.GetButton("Jump");
        float g = gravity;

        if (!grounded)
        {
            if (velocity.y < 0f)          g *= fallMultiplier;     // faster fall
            else if (rising && !jumpHeld) g *= lowJumpMultiplier;  // cut short hop
        }

        velocity.y = Mathf.Max(velocity.y + g * dt, terminalVel);
        cc.Move(new Vector3(0f, velocity.y, 0f) * dt);

        // ---- METRICS ----
        CurrentPlanarSpeed = new Vector3(lastMoveWorld.x, 0f, lastMoveWorld.z).magnitude;
    }

    // Animator driver (if you’re using it) can poll this to fire a Jump trigger once
    public bool ConsumeJumpPressedThisFrame()
    {
        bool v = jumpPressedThisFrame;
        jumpPressedThisFrame = false;
        return v;
    }

    private static bool HasParam(Animator anim, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in anim.parameters)
            if (p.type == type && p.name == name)
                return true;
        return false;
    }
}
