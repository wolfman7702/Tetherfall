using UnityEngine;

public class AnimatorPumper : MonoBehaviour
{
    [SerializeField] Animator animator;

    void Awake()
    {
        if (!animator) animator = GetComponentInChildren<Animator>();
    }

    void Update()
    {
        if (!animator) return;

        // Hold keys to simulate movement
        float x = (Input.GetKey(KeyCode.D) ? 1f : 0f) + (Input.GetKey(KeyCode.A) ? -1f : 0f);
        float y = (Input.GetKey(KeyCode.W) ? 1f : 0f) + (Input.GetKey(KeyCode.S) ? -1f : 0f);
        float mag = Mathf.Clamp01(new Vector2(x, y).magnitude);

        SafeSetFloat("InputX", x);
        SafeSetFloat("InputY", y);
        SafeSetFloat("InputMagnitude", mag);
        SafeSetBool("IsGrounded", true); // just force true for the test
    }

    void SafeSetFloat(string n, float v)
    {
        foreach (var p in animator.parameters)
            if (p.name == n && p.type == AnimatorControllerParameterType.Float)
                { animator.SetFloat(n, v, 0.1f, Time.deltaTime); return; }
    }
    void SafeSetBool(string n, bool v)
    {
        foreach (var p in animator.parameters)
            if (p.name == n && p.type == AnimatorControllerParameterType.Bool)
                { animator.SetBool(n, v); return; }
    }
}
