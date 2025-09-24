using UnityEngine;

public class ResetAllBoneScales : MonoBehaviour
{
    void Start()
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            t.localScale = Vector3.one;
        Debug.Log("All bone scales reset to 1 on " + name);
        enabled = false; // run once
    }
}
