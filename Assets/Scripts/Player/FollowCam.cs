using UnityEngine;
public class SimpleFollowCam : MonoBehaviour {
    public Transform target;
    public Vector3 offset = new Vector3(0,3,-6);
    public float lerp = 8f;
    void LateUpdate(){
        if(!target) return;
        var desired = target.position + target.TransformVector(offset);
        transform.position = Vector3.Lerp(transform.position, desired, Time.deltaTime*lerp);
        transform.LookAt(target.position + Vector3.up*1.5f);
    }
}
