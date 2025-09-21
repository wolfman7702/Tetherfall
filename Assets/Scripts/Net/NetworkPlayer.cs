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
        // Enable input for the local player
        fps.enabled = true;

        // Lock the main camera to this player's CameraPivot
        var cam = Camera.main;
        if (cam != null)
        {
            var look = cam.GetComponent<FPSLook>();
            if (look != null)
            {
                look.playerBody = transform;

                var pivot = transform.Find("CameraPivot");
                if (pivot != null)
                {
                    look.cameraPivot = pivot;
                }
            }
        }

        // Hook into Vivox (if available)
        if (VoiceManagerUGS.Instance != null)
        {
            VoiceManagerUGS.Instance.AttachLocalPlayer(transform);
        }

        // Tell the mountain streamer to follow THIS player
        var faceStacker = FindObjectOfType<MountainFaceStacker>();
        if (faceStacker != null && faceStacker.trackPlayer == null)
        {
            faceStacker.trackPlayer = transform;
        }
    }

    public override void OnStopAuthority()
    {
        fps.enabled = false;
    }

    void Start()
    {
        // Disable input for players we don’t own
        if (!isOwned)
        {
            fps.enabled = false;
        }
    }
}
