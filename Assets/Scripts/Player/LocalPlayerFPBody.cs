using UnityEngine;

public class LocalViewSetup : MonoBehaviour
{
    public GameObject fullBody; // Bear Hero
    public GameObject fpBody;   // Bear Hero FP
    public Camera playerCamera;

    // Replace this with your actual local-player check (Mirror/NGO/etc.)
    bool IsLocalPlayer() => true; 

    void Start()
    {
        bool isLocal = IsLocalPlayer();
        // Full body is always active so others can see you,
        // but your camera won't render its layer anyway.
        fullBody.SetActive(true);

        // FP body only needed for local player
        fpBody.SetActive(isLocal);

        if (isLocal && playerCamera)
        {
            // Make sure culling mask includes FirstPersonFP and excludes ThirdPerson
            // (already set in editor, this is just safety)
        }
    }
}
