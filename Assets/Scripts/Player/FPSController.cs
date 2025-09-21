using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class FPSController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.5f;
    public float sprintMultiplier = 1.35f;

    [Header("Jump/Gravity")]
    public float jumpHeight = 1.6f;
    public float gravity = -30f;          // was too light; increase magnitude for faster fall
    public float terminalVelocity = -55f; // cap fall speed

    [Header("Grounding")]
    public Transform groundCheck;
    public float groundRadius = 0.25f;
    public LayerMask groundMask = ~0;

    [Header("Jump Assist")]
    public float coyoteTime = 0.12f;      // grace after leaving ground
    public float jumpBuffer = 0.12f;      // buffer before landing

    [HideInInspector] public float pcMoveMultiplier = 1f; // external slowdowns (tether)

    CharacterController cc;
    Vector3 velocity;
    bool isGrounded, jumpedSinceAir;
    float lastGroundedTime, lastJumpPressedTime;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!groundCheck)
        {
            groundCheck = new GameObject("GroundCheck").transform;
            groundCheck.SetParent(transform);
            groundCheck.localPosition = new Vector3(0f, 0.1f, 0f);
        }
    }

    void Update()
    {
        // --- input
        if (Input.GetButtonDown("Jump"))
            lastJumpPressedTime = Time.time;

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right   = Vector3.ProjectOnPlane(transform.right,   Vector3.up).normalized;
        Vector3 move    = (forward * v + right * h).normalized;

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        cc.Move(move * (speed * pcMoveMultiplier) * Time.deltaTime);

        // --- grounding
        bool wasGrounded = isGrounded;
        isGrounded = Physics.CheckSphere(groundCheck.position, groundRadius, groundMask, QueryTriggerInteraction.Ignore);
        if (isGrounded)
        {
            lastGroundedTime = Time.time;
            jumpedSinceAir = false;
            if (velocity.y < 0f) velocity.y = -2f; // stick to ground
        }

        // --- buffered / coyote jump (no double jump)
        bool canCoyote = Time.time - lastGroundedTime <= coyoteTime;
        bool buffered  = Time.time - lastJumpPressedTime <= jumpBuffer;

        if (!jumpedSinceAir && buffered && (isGrounded || canCoyote))
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            lastJumpPressedTime = -999f;
            jumpedSinceAir = true;
        }

        // --- gravity + terminal velocity
        velocity.y += gravity * Time.deltaTime;
        if (velocity.y < terminalVelocity) velocity.y = terminalVelocity;

        cc.Move(velocity * Time.deltaTime);
    }

    // Helpers used elsewhere
    public bool IsGrounded() => isGrounded;
    public float VerticalVelocity() => velocity.y;
}
