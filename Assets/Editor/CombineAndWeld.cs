#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class CombineAndWeldSelected
{
    // Tweak this if you still see hairline seams after combining.
    // 0.01 = 1cm tolerance. Try 0.015-0.02 if your pieces aren’t perfectly snapped.
    const float WELD_EPS = 0.015f;

    [MenuItem("Tools/Combine & Weld Selected (Simple)")]
    public static void CombineSelected()
    {
        var parent = Selection.activeTransform;
        if (!parent) { Debug.LogError("Select your VIS_Rocks parent in the Hierarchy, then run this."); return; }

        var filters = parent.GetComponentsInChildren<MeshFilter>(true);
        if (filters.Length == 0) { Debug.LogWarning("No MeshFilters under selection."); return; }

        // Build in parent's local space so all children share one frame.
        var verts = new List<Vector3>();
        var norms = new List<Vector3>();
        var uvs   = new List<Vector2>();
        var tris  = new List<int>();

        int baseIndex = 0;
        foreach (var f in filters)
        {
            if (!f.sharedMesh) continue;
            var r = f.GetComponent<Renderer>();
            if (!r) continue;

            var m        = f.sharedMesh;
            var toLocal  = parent.worldToLocalMatrix * f.transform.localToWorldMatrix;
            var mv       = m.vertices;
            var mn       = (m.normals != null && m.normals.Length == mv.Length) ? m.normals : null;
            var mu       = (m.uv != null && m.uv.Length == mv.Length) ? m.uv : null;
            var indices  = m.triangles;

            for (int i = 0; i < mv.Length; i++)
            {
                verts.Add(toLocal.MultiplyPoint3x4(mv[i]));
                var n = mn != null ? mn[i] : Vector3.up;
                norms.Add((toLocal.MultiplyVector(n)).normalized);
                uvs.Add(mu != null ? mu[i] : Vector2.zero);
            }

            for (int i = 0; i < indices.Length; i++)
                tris.Add(baseIndex + indices[i]);

            baseIndex += mv.Length;
        }

        // Weld: snap nearby vertices to the same point (tolerance = WELD_EPS).
        var map      = new int[verts.Count];
        var newV     = new List<Vector3>();
        var newN     = new List<Vector3>();
        var newUV    = new List<Vector2>();
        var hash     = new Dictionary<Vector3Int, int>(); // integer grid hash for speed

        Vector3Int Key(Vector3 v)
        {
            int x = Mathf.RoundToInt(v.x / WELD_EPS);
            int y = Mathf.RoundToInt(v.y / WELD_EPS);
            int z = Mathf.RoundToInt(v.z / WELD_EPS);
            return new Vector3Int(x, y, z);
        }

        for (int i = 0; i < verts.Count; i++)
        {
            var k = Key(verts[i]);
            if (!hash.TryGetValue(k, out int idx))
            {
                idx = newV.Count;
                hash[k] = idx;
                newV.Add(verts[i]);
                newN.Add(norms[i]);
                newUV.Add(uvs[i]);
            }
            else
            {
                // Average normals for a smooth seam (prevents lighting cracks).
                newN[idx] = (newN[idx] + norms[i]).normalized;
                // Keep first UV; different islands are fine for a cliff.
            }
            map[i] = idx;
        }

        // Remap triangles to welded indices and drop degenerates
        var newTris = new List<int>(tris.Count);
        for (int t = 0; t < tris.Count; t += 3)
        {
            int a = map[tris[t]], b = map[tris[t+1]], c = map[tris[t+2]];
            if (a == b || b == c || c == a) continue;                 // drop 0-area
            // Also drop near-zero area triangles
            var ab = newV[b] - newV[a];
            var ac = newV[c] - newV[a];
            if (Vector3.Cross(ab, ac).sqrMagnitude < 1e-10f) continue; // ultra-thin
            newTris.Add(a); newTris.Add(b); newTris.Add(c);
        }

        // Make / reuse VIS_Combined child
        var combinedGO = parent.Find("VIS_Combined") ? parent.Find("VIS_Combined").gameObject : new GameObject("VIS_Combined");
        combinedGO.transform.SetParent(parent, false);
        combinedGO.transform.localPosition = Vector3.zero;
        combinedGO.transform.localRotation = Quaternion.identity;
        combinedGO.transform.localScale    = Vector3.one;

        var mf = combinedGO.GetComponent<MeshFilter>()   ?? combinedGO.AddComponent<MeshFilter>();
        var mr = combinedGO.GetComponent<MeshRenderer>() ?? combinedGO.AddComponent<MeshRenderer>();

        var mesh = new Mesh();
        mesh.indexFormat = (newV.Count > 65000) ? UnityEngine.Rendering.IndexFormat.UInt32 : UnityEngine.Rendering.IndexFormat.UInt16;
        mesh.SetVertices(newV);
        mesh.SetNormals(newN);
        mesh.SetUVs(0, newUV);
        mesh.SetTriangles(newTris, 0, true);
        mesh.RecalculateBounds();
        // (Normals already smoothed; add RecalculateTangents if you use normal maps)

        mf.sharedMesh = mesh;

        // Try to copy a material from a child (so you see something)
        if (mr.sharedMaterial == null)
        {
            foreach (var f in filters)
            {
                var r = f.GetComponent<Renderer>();
                if (r && r.sharedMaterial) { mr.sharedMaterial = r.sharedMaterial; break; }
            }
        }

        // Disable originals so you only render the welded mesh
        foreach (var f in filters) if (f && f.gameObject != combinedGO) f.gameObject.SetActive(false);

        Debug.Log($"[Combine&Weld] Children: {filters.Length}  →  Welded Verts: {newV.Count}  Tris: {newTris.Count/3}", combinedGO);
        Selection.activeGameObject = combinedGO;
        EditorGUIUtility.PingObject(combinedGO);
    }
}
#endif
