using UnityEngine;

public class HideHead : MonoBehaviour
{
    public Transform headBone; // drag your head bone here in Inspector

    void Start()
    {
        if (headBone != null)
        {
            foreach (Transform t in headBone.GetComponentsInChildren<Transform>())
                t.localScale = Vector3.zero; // shrink head + children
        }
    }
}
