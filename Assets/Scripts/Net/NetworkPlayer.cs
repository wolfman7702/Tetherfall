using UnityEngine;
#if MIRROR
using Mirror;
#endif

// Remove hard dependency on FPSController.
// [RequireComponent(typeof(FPSController))]  // <-- delete this
[DisallowMultipleComponent]
public class NetworkPlayer : 
#if MIRROR
    NetworkBehaviour
#else
    MonoBehaviour
#endif
{
    // Optional components
    private MonoBehaviour fpsController;  // could be your FPSController script
    private CharacterController cc;

    void Awake()
    {
        // Try to find your FPSController by name (optional)
        // If you have a concrete type, replace "FPSController" with the type and remove reflection
        var maybeFps = GetComponent("FPSController") as MonoBehaviour;
        if (maybeFps != null) fpsController = maybeFps;

        cc = GetComponent<CharacterController>();
    }

#if MIRROR
    public override void OnStartAuthority()
    {
        EnableLocalInput(true);
        BindCameraLook();
        AttachVoiceIfAvailable();
    }

    public override void OnStopAuthority()
    {
        EnableLocalInput(false);
    }
#endif

    void Start()
    {
        // If Mirror is active and we don’t own this player, disable input. 
        // Otherwise (offline / editor), ENABLE input so single-player testing works.
        bool networkActive =
#if MIRROR
            Mirror.NetworkClient.active;
#else
            false;
#endif

        bool owned =
#if MIRROR
            isOwned;
#else
            true;  // assume owned offline
#endif

        if (networkActive && !owned)
        {
            EnableLocalInput(false);
        }
        else
        {
            EnableLocalInput(true);
            // In offline/editor tests, also bind look once.
            BindCameraLook();
            AttachVoiceIfAvailable();
        }
    }

    private void EnableLocalInput(bool enable)
    {
        if (fpsController) fpsController.enabled = enable;
        // You may also enable/disable other local-only scripts here (e.g., input maps)
        // Example: var input = GetComponent<PlayerInput>(); if (input) input.enabled = enable;
    }

    private void BindCameraLook()
    {
        // Lock main camera to this player (optional and safe)
        var cam = Camera.main;
        if (!cam) return;

        // Support either your custom FPSLook or the safe look I gave you earlier
        var look = cam.GetComponent<MonoBehaviour>(); // placeholder
        // Prefer specific types if present:
        var fpsLook = cam.GetComponent("FPSLook") as MonoBehaviour;
        var simpleLook = cam.GetComponent("SimpleFPSLook") as MonoBehaviour;

        Transform pivot = transform.Find("CameraPivot"); // create this child if it doesn’t exist
        if (pivot == null)
        {
            // Create a pivot if missing (safer than null)
            var go = new GameObject("CameraPivot");
            go.transform.SetParent(transform, false);
            pivot = go.transform;
        }

        // If you have a concrete FPSLook type with fields 'playerBody' and 'cameraPivot', set them reflectively:
        if (fpsLook)
        {
            var t = fpsLook.GetType();
            var bodyField = t.GetField("playerBody");
            var pivotField = t.GetField("cameraPivot");
            if (bodyField != null) bodyField.SetValue(fpsLook, transform);
            if (pivotField != null) pivotField.SetValue(fpsLook, pivot);
        }
        else if (simpleLook) // from earlier snippet: fields yawRoot & pitchPivot
        {
            var t = simpleLook.GetType();
            var yawField = t.GetField("yawRoot");
            var pitchField = t.GetField("pitchPivot");
            if (yawField != null) yawField.SetValue(simpleLook, transform);
            if (pitchField != null) pitchField.SetValue(simpleLook, pivot);
        }
        // If neither look script exists, do nothing (safe).
    }

    private void AttachVoiceIfAvailable()
    {
        // Optional; safe guard
        var voiceMgr = GameObject.FindObjectOfType(System.Type.GetType("VoiceManagerUGS"));
        if (voiceMgr != null)
        {
            // Call AttachLocalPlayer if it exists
            var m = voiceMgr.GetType().GetMethod("AttachLocalPlayer");
            if (m != null) m.Invoke(voiceMgr, new object[] { transform });
        }
    }
}
