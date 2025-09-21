using UnityEngine;
using Mirror;

public class TetherLink : MonoBehaviour
{
    [Header("Ends")]
    public Transform a;
    public Transform b;

    [Header("Tuning")]
    public float slack = 4.0f;             // free length before tension
    public float stiffness = 2.0f;         // gentle pull strength
    public float maxStretch = 7.0f;        // hard cap used to normalize tension
    public float speedPenaltyAtMax = 0.5f; // 0..1 (50% speed at max tension)

    [Header("Visual")]
    public LineRenderer line;

    [Range(0f,1f)] public float tension01; // for UI if you want

    void LateUpdate()
    {
        if (!a || !b) return;

        Vector3 pa = a.position;
        Vector3 pb = b.position;

        float dist = Vector3.Distance(pa, pb);
        float over = Mathf.Max(0f, dist - slack);
        tension01  = Mathf.Clamp01(over / Mathf.Max(0.0001f, (maxStretch - slack)));

        // Draw rope
        if (line)
        {
            if (line.positionCount != 2) line.positionCount = 2;
            line.SetPosition(0, pa);
            line.SetPosition(1, pb);
        }

        // Slow and gently nudge the LOCAL player's character only
        if (over > 0f)
        {
            float pull = stiffness * over * Time.deltaTime;

            ApplyPenaltyIfLocal(a, 1f - tension01 * speedPenaltyAtMax);
            ApplyPenaltyIfLocal(b, 1f - tension01 * speedPenaltyAtMax);

            NudgeIfLocal(a, (pa - pb).normalized * pull);
            NudgeIfLocal(b, (pb - pa).normalized * pull);
        }
        else
        {
            ApplyPenaltyIfLocal(a, 1f);
            ApplyPenaltyIfLocal(b, 1f);
        }
    }

    void ApplyPenaltyIfLocal(Transform t, float mult)
    {
        if (!t) return;
        var np = t.GetComponent<NetworkPlayer>();
        var pc = t.GetComponent<FPSController>();
        if (np && pc && np.isOwned) pc.pcMoveMultiplier = mult;
    }

    void NudgeIfLocal(Transform t, Vector3 delta)
    {
        if (!t) return;
        var np = t.GetComponent<NetworkPlayer>();
        var cc = t.GetComponent<CharacterController>();
        if (np && np.isOwned && cc && cc.enabled)
            cc.Move(delta);
    }
}
