using UnityEngine;
using System.Collections.Generic;

public class FirstPersonBodyTrimSafe : MonoBehaviour
{
    // EDIT these to match your rig exactly (case-insensitive)
    // Don’t use generic words like "ear" that appear inside "Bear_*"
    public List<string> exactBoneNames = new List<string> {
        "Head", "Head_end",
        "Neck", "Neck_01", "Neck_02",
        "Jaw", "Tongue",
        "Eye_L", "Eye_R", "Eyelid_L", "Eyelid_R",
        "Ear_L", "Ear_R" // only if your bones are exactly named this way
    };

    void Start()
    {
        var set = new HashSet<string>();
        foreach (var n in exactBoneNames) set.Add(n.ToLower());

        int hidden = 0;
        foreach (var t in GetComponentsInChildren<Transform>(true))
        {
            if (set.Contains(t.name.ToLower()))
            {
                t.localScale = Vector3.zero;
                hidden++;
            }
        }
        Debug.Log($"Trimmed {hidden} FP head/neck bones on {name}");
    }
}
