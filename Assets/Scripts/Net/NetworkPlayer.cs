using Mirror;
using UnityEngine;

[RequireComponent(typeof(FPSController))]
public class NetworkPlayer : NetworkBehaviour
{
    private FPSController fps;

    void Awake()
    {
        fps = GetComponent<FPSController>();
    }

    public override void OnStartAuthority()
    {
        // Enable local input
        fps.enabled = true;

        // Lock main camera to this player
        var cam = Camera.main;
        if (cam != null)
        {
            var look = cam.GetComponent<FPSLook>();
            if (look != null)
            {
                look.playerBody = transform;
                var pivot = transform.Find("CameraPivot");
                if (pivot != null)
                    look.cameraPivot = pivot;
            }
        }

        // Vivox (optional)
        if (VoiceManagerUGS.Instance != null)
            VoiceManagerUGS.Instance.AttachLocalPlayer(transform);

        // Note: Removed MountainFaceStacker/MountainStacker reference so this compiles
        // even if those scripts are gone.
    }

    public override void OnStopAuthority()
    {
        fps.enabled = false;
    }

    void Start()
    {
        // Disable input for non-owned players
        if (!isOwned)
            fps.enabled = false;
    }
}
