using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Speeds (m/s)")]
    [SerializeField] public float walkSpeed = 4.5f;
    [SerializeField] public float runSpeed = 7.0f;

    [Header("Jump & Gravity")]
    [SerializeField] float jumpHeight = 3.5f;
    [SerializeField] float gravity = -4f;
    [SerializeField] float fallMultiplier = 3.0f;
    [SerializeField] float lowJumpMultiplier = 2.1f;
    [SerializeField] float terminalVel = -55f;

    [Header("External Multipliers (compatibility)")]
    [Tooltip("Kept for compatibility with other scripts (e.g., TetherLink). 1 = normal speed.")]
    public float pcMoveMultiplier = 1.0f;

    [Header("Ground Check")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.25f;
    [SerializeField] LayerMask groundMask = ~0;

    [Header("Jump QoL")]
    [SerializeField] float coyoteTime = 0.10f;
    [SerializeField] float jumpBuffer = 0.12f;

    CharacterController cc;
    Vector3 velocity;
    Vector3 lastMoveWorld;

    float coyoteTimer;
    float bufferTimer;
    bool grounded;
    bool jumpPressedThisFrame;

    ExternalForceReceiver extr;   // << NEW

    public bool IsGrounded => grounded;
    public float VerticalVelocity => velocity.y;
    public float CurrentPlanarSpeed { get; private set; }
    public float LocalMoveX { get; private set; }
    public float LocalMoveY { get; private set; }
    public float Speed01ForAnimator { get; private set; }

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        extr = GetComponent<ExternalForceReceiver>();
    }

    void Update()
    {
        float dt = Time.deltaTime;

        // --- INPUT ---
        Vector2 input = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
        input = Vector2.ClampMagnitude(input, 1f);
        if (input.magnitude < 0.12f) input = Vector2.zero;

        bool wantRun = Input.GetKey(KeyCode.LeftShift);

        // Base tier speed (walk/run), then apply multiplier
        float baseSpeed = (input == Vector2.zero) ? 0f : (wantRun ? runSpeed : walkSpeed);
        float targetSpeed = baseSpeed * Mathf.Clamp(pcMoveMultiplier, 0f, 3f);

        // Animator helper (walk≈0.5, run=1)
        Speed01ForAnimator = (runSpeed > 0f) ? Mathf.Clamp01(baseSpeed / runSpeed) : 0f;

        // --- HORIZONTAL MOVE ---
        Vector3 ext = (extr != null) ? new Vector3(extr.velocity.x, 0f, extr.velocity.z) : Vector3.zero;
        Vector3 worldMove = (transform.right * input.x + transform.forward * input.y) * targetSpeed + ext;
        lastMoveWorld = worldMove;
        cc.Move(worldMove * dt);

        // Animator-localized values
        Vector3 local = transform.InverseTransformDirection(worldMove);
        float denom = Mathf.Max(0.0001f, Mathf.Max(targetSpeed, 1f));
        LocalMoveX = Mathf.Clamp(local.x / denom, -1f, 1f);
        LocalMoveY = Mathf.Clamp(local.z / denom, -1f, 1f);

        // --- GROUND CHECK ---
        bool ccGrounded = cc.isGrounded;
        bool sphereGrounded = groundCheck && Physics.CheckSphere(groundCheck.position, groundCheckRadius, groundMask, QueryTriggerInteraction.Ignore);
        grounded = ccGrounded || sphereGrounded;

        // --- TIMERS ---
        coyoteTimer = grounded ? coyoteTime : Mathf.Max(0f, coyoteTimer - dt);
        if (Input.GetButtonDown("Jump")) bufferTimer = jumpBuffer;
        else bufferTimer = Mathf.Max(0f, bufferTimer - dt);

        // --- STICK TO GROUND ---
        if (grounded && velocity.y < 0f) velocity.y = -2f;

        // --- JUMP ---
        jumpPressedThisFrame = false;
        if (bufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = Mathf.Sqrt(2f * Mathf.Abs(gravity) * jumpHeight);
            bufferTimer = 0f;
            coyoteTimer = 0f;
            jumpPressedThisFrame = true;
        }

        // --- GRAVITY ---
        bool rising = velocity.y > 0f;
        bool jumpHeld = Input.GetButton("Jump");
        float g = gravity;
        if (!grounded)
        {
            if (velocity.y < 0f) g *= fallMultiplier;
            else if (rising && !jumpHeld) g *= lowJumpMultiplier;
        }

        velocity.y = Mathf.Max(velocity.y + g * dt, terminalVel);
        cc.Move(new Vector3(0f, velocity.y, 0f) * dt);

        CurrentPlanarSpeed = new Vector3(lastMoveWorld.x, 0f, lastMoveWorld.z).magnitude;
    }

    public bool ConsumeJumpPressedThisFrame()
    {
        bool v = jumpPressedThisFrame;
        jumpPressedThisFrame = false;
        return v;
    }
}
