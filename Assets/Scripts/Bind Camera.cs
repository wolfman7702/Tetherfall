using Mirror;
using UnityEngine;

public class LocalTPCameraBinder : NetworkBehaviour {
    public override void OnStartLocalPlayer() {
        var cam = Camera.main;                       // scene camera tagged MainCamera
        var follow = cam ? cam.GetComponent<SimpleFollowCam>() : null;
        if (follow) follow.target = transform;       // follow THIS local player
    }
}
