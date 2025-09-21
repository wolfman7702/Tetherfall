using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class PerlinCliffArc : MonoBehaviour
{
    [Header("Shape")]
    public float height = 24f;
    public float innerRadius = 16f;     // cliff “base” radius
    public float thickness = 1.5f;      // rock thickness
    [Range(10f, 360f)] public float arcDegrees = 120f;
    [Range(8, 256)] public int radialSegments = 96;  // along the arc
    [Range(2, 128)] public int verticalSegments = 32;

    [Header("Noise")]
    public int seed = 12345;
    public float pushAmplitude = 1.2f;  // outward bumps
    public float freqAngle = 0.35f;     // noise along arc
    public float freqHeight = 0.22f;    // noise along height
    [Range(1, 5)] public int octaves = 3;
    [Range(0f, 1f)] public float persistence = 0.55f;
    public float lateralJitter = 0.25f; // tiny sideways wobble

    [Header("Collision")]
    public bool generateCollider = false;

    MeshFilter mf;
    MeshCollider mc;

    void OnEnable()
    {
        mf = GetComponent<MeshFilter>();
        if (generateCollider)
        {
            mc = gameObject.GetComponent<MeshCollider>();
            if (!mc) mc = gameObject.AddComponent<MeshCollider>();
        }
        Rebuild();
    }

    void OnValidate()
    {
        if (!isActiveAndEnabled) return;
        Rebuild();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        int rs = Mathf.Max(8, radialSegments);
        int vs = Mathf.Max(2, verticalSegments);

        // Two rings (front/back) → closed slab you can see from both sides.
        int ringStride = (rs + 1) * 2; // inner+outer per radial sample

        var verts = new Vector3[(vs + 1) * ringStride];
        var uvs   = new Vector2[verts.Length];
        var tris  = new System.Collections.Generic.List<int>(vs * rs * 6 * 3); // top+bottom+walls

        float arcRad = Mathf.Deg2Rad * arcDegrees;
        float rIn = innerRadius;
        float rOut = innerRadius + thickness;

        Vector2 off = SeedToOffset(seed);

        // Build vertical rings from bottom to top
        for (int y = 0; y <= vs; y++)
        {
            float ty = y / (float)vs;
            float yPos = ty * height;

            for (int i = 0; i <= rs; i++)
            {
                float t = i / (float)rs;           // 0..1 along arc
                float ang = -arcRad * 0.5f + t * arcRad;

                // multi-octave perlin for outward push
                float n = FBM(t * freqAngle + off.x, ty * freqHeight + off.y, octaves, persistence);
                float push = (n - 0.5f) * 2f * pushAmplitude;

                // tiny sideways jitter
                float j = (Mathf.PerlinNoise(t * freqAngle * 2f + 17.3f, ty * freqHeight * 2f - 9.1f) - 0.5f) * 2f * lateralJitter;

                // inner & outer vertices for this radial sample
                Vector3 dir = new Vector3(Mathf.Cos(ang), 0f, Mathf.Sin(ang));
                Vector3 lateral = new Vector3(-Mathf.Sin(ang), 0f, Mathf.Cos(ang)); // perpendicular along arc

                int baseIndex = y * ringStride + i * 2;
                verts[baseIndex + 0] = dir * (rIn + push)  + Vector3.up * yPos + lateral * j;   // inner
                verts[baseIndex + 1] = dir * (rOut + push) + Vector3.up * yPos + lateral * j;   // outer

                uvs[baseIndex + 0] = new Vector2(t, ty);
                uvs[baseIndex + 1] = new Vector2(t, ty);
            }
        }

        // build triangles: top/bottom surfaces + inner/outer walls
        for (int y = 0; y < vs; y++)
        {
            int rowA = y * ringStride;
            int rowB = (y + 1) * ringStride;

            for (int i = 0; i < rs; i++)
            {
                int a0 = rowA + i * 2;     // inner top left
                int a1 = a0 + 1;           // outer top left
                int b0 = rowA + (i + 1) * 2;
                int b1 = b0 + 1;

                int c0 = rowB + i * 2;     // inner bottom left
                int c1 = c0 + 1;
                int d0 = rowB + (i + 1) * 2;
                int d1 = d0 + 1;

                // top face (outer ring)
                tris.Add(a1); tris.Add(c1); tris.Add(b1);
                tris.Add(b1); tris.Add(c1); tris.Add(d1);

                // bottom face (inner ring) (flip to face down)
                tris.Add(d0); tris.Add(c0); tris.Add(b0);
                tris.Add(b0); tris.Add(c0); tris.Add(a0);

                // outer wall
                tris.Add(a1); tris.Add(b1); tris.Add(c1);
                tris.Add(b1); tris.Add(d1); tris.Add(c1);

                // inner wall
                tris.Add(c0); tris.Add(b0); tris.Add(a0);
                tris.Add(c0); tris.Add(d0); tris.Add(b0);
            }
        }

        var m = new Mesh();
        m.indexFormat = (verts.Length > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;
        m.vertices = verts;
        m.uv = uvs;
        m.triangles = tris.ToArray();
        m.RecalculateNormals();
        m.RecalculateBounds();

        if (mf == null) mf = GetComponent<MeshFilter>();
        mf.sharedMesh = m;

        if (generateCollider)
        {
            if (mc == null) mc = gameObject.GetComponent<MeshCollider>();
            if (!mc) mc = gameObject.AddComponent<MeshCollider>();
            mc.sharedMesh = null;
            mc.sharedMesh = m;
        }
    }

    static Vector2 SeedToOffset(int s)
    {
        unchecked {
            uint u = (uint)s;
            u ^= 2747636419u; u *= 2654435769u; u ^= u >> 16; u *= 2654435769u; u ^= u >> 16;
            float a = (u & 0xFFFF) / 65535f * 1000f;
            float b = ((u >> 16) & 0xFFFF) / 65535f * 1000f;
            return new Vector2(a, b);
        }
    }

    static float FBM(float x, float y, int oct, float p)
    {
        float amp = 1f, sum = 0f, norm = 0f, fx = x, fy = y;
        for (int i = 0; i < oct; i++)
        {
            sum += Mathf.PerlinNoise(fx, fy) * amp;
            norm += amp;
            amp *= p; fx *= 2f; fy *= 2f;
        }
        return (norm > 0f) ? sum / norm : 0f; // 0..1
    }
}
