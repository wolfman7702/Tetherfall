using UnityEngine;

[RequireComponent(typeof(FPSController))]
public class PlayerAnimDriver : MonoBehaviour
{
    [SerializeField] Animator animator;

    // Match the params we’ve been using
    [SerializeField] string pSpeed = "Speed";
    [SerializeField] string pIsGrounded = "IsGrounded";
    [SerializeField] string pJump = "Jump";

    FPSController ctrl;

    void Reset() { animator = GetComponentInChildren<Animator>(); }
    void Awake()
    {
        ctrl = GetComponent<FPSController>();
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!animator || !ctrl) return;

        // Drive locomotion blend (0..1 where walk≈0.5, run=1)
        animator.SetFloat(pSpeed, ctrl.Speed01ForAnimator, 0.12f, Time.deltaTime);

        // Grounded flag (used by transition back to locomotion)
        animator.SetBool(pIsGrounded, ctrl.IsGrounded);

        // Fire the single jump clip when we actually jumped this frame
        if (ctrl.ConsumeJumpPressedThisFrame())
        {
            animator.ResetTrigger(pJump); // in case it’s lingering
            animator.SetTrigger(pJump);
        }
    }
}
