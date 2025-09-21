using System.Collections.Generic;
using UnityEngine;

public class BakeNoiseCollider : MonoBehaviour
{
    [Header("Source of render meshes (with noise applied)")]
    public Transform sourceRoot;              // e.g., your VIS_Rocks parent
    [Header("Output")]
    public string colliderObjectName = "COL_Noise";
    public string groundLayerName = "Ground";
    public bool combineIntoSingleCollider = true;

    [Tooltip("Optional smoothing passes on the collider mesh to tame spiky triangles.")]
    [Range(0,10)] public int smoothIterations = 2;
    [Range(0f,1f)] public float smoothStrength = 0.35f;

    [ContextMenu("Bake Now")]
    public void BakeNow()
    {
        if (!sourceRoot) { Debug.LogError("Assign sourceRoot (your VIS_Rocks parent).", this); return; }

        // Collect all MeshFilters under sourceRoot
        var filters = sourceRoot.GetComponentsInChildren<MeshFilter>(true);
        var combine = new List<CombineInstance>();
        foreach (var f in filters)
        {
            var mesh = f.sharedMesh;
            var renderer = f.GetComponent<Renderer>();
            if (!mesh || !renderer) continue;

            if (combineIntoSingleCollider)
            {
                var ci = new CombineInstance {
                    mesh = mesh,
                    transform = f.transform.localToWorldMatrix
                };
                combine.Add(ci);
            }
            else
            {
                // Individual colliders per child (more expensive if many children)
                var go = f.gameObject;
                var mc = go.GetComponent<MeshCollider>() ?? go.AddComponent<MeshCollider>();
                mc.sharedMesh = null;
                mc.sharedMesh = mesh;
                mc.convex = false; // static, non-convex collider
                go.layer = LayerMask.NameToLayer(groundLayerName);
                GameObjectUtilityMarkStatic(go);
            }
        }

        if (combineIntoSingleCollider)
        {
            // Create or find output object
            var colGo = transform.Find(colliderObjectName)?.gameObject;
            if (!colGo)
            {
                colGo = new GameObject(colliderObjectName);
                colGo.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
                colGo.transform.localScale = Vector3.one;
                colGo.transform.SetParent(transform, true);
            }

            // Build combined mesh in world, then bring it into local space of output
            var combined = new Mesh { indexFormat = UnityEngine.Rendering.IndexFormat.UInt32 };
            combined.CombineMeshes(combine.ToArray(), true, true, true);

            // Optionally smooth a bit to remove tiny spikes
            if (smoothIterations > 0 && combined.vertexCount > 0)
                LaplacianSmoothInPlace(combined, smoothIterations, smoothStrength);

            var mc = colGo.GetComponent<MeshCollider>() ?? colGo.AddComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = combined;
            mc.convex = false; // must be false for large static terrain-like shapes
            colGo.layer = LayerMask.NameToLayer(groundLayerName);
            GameObjectUtilityMarkStatic(colGo);
        }

        Debug.Log("Baked noise collider.", this);
    }

    // --- helpers ---

    static void GameObjectUtilityMarkStatic(GameObject go)
    {
        #if UNITY_EDITOR
        UnityEditor.GameObjectUtility.SetStaticEditorFlags(
            go, UnityEditor.StaticEditorFlags.BatchingStatic |
                UnityEditor.StaticEditorFlags.NavigationStatic |
                UnityEditor.StaticEditorFlags.OccludeeStatic |
                UnityEditor.StaticEditorFlags.OccluderStatic);
        #endif
    }

    // very light Laplacian smoothing (no topology changes)
    static void LaplacianSmoothInPlace(Mesh m, int iters, float strength)
    {
        var v = m.vertices;
        var t = m.triangles;
        var adj = BuildAdjacency(v.Length, t);
        var tmp = new Vector3[v.Length];

        for (int k = 0; k < iters; k++)
        {
            for (int i = 0; i < v.Length; i++)
            {
                var list = adj[i];
                if (list == null || list.Count == 0) { tmp[i] = v[i]; continue; }
                Vector3 avg = Vector3.zero;
                for (int j = 0; j < list.Count; j++) avg += v[list[j]];
                avg /= list.Count;
                tmp[i] = Vector3.Lerp(v[i], avg, strength);
            }
            var swap = v; v = tmp; tmp = swap;
        }
        m.vertices = v;
        m.RecalculateNormals();
        m.RecalculateBounds();
    }

    static List<int>[] BuildAdjacency(int vertCount, int[] tris)
    {
        var adj = new List<int>[vertCount];
        for (int i = 0; i < tris.Length; i += 3)
        {
            int a = tris[i], b = tris[i+1], c = tris[i+2];
            AddPair(adj, a, b); AddPair(adj, b, a);
            AddPair(adj, b, c); AddPair(adj, c, b);
            AddPair(adj, c, a); AddPair(adj, a, c);
        }
        return adj;
    }
    static void AddPair(List<int>[] adj, int i, int j)
    {
        var list = adj[i];
        if (list == null) adj[i] = list = new List<int>(6);
        if (!list.Contains(j)) list.Add(j);
    }
}
