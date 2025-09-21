using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class NoiseDisplacerWS_Jagged : MonoBehaviour
{
    public enum DisplaceMode { AlongNormal, GlobalDirection }

    [Header("World-space field")]
    public Transform anchor;                 // set to your Section root so all children share space
    public int   seed        = 12345;
    public float worldScale  = 6.0f;         // meters per big feature (bigger = broader shapes)

    [Header("Displacement")]
    public DisplaceMode displaceMode = DisplaceMode.GlobalDirection;
    public Vector3 globalDirection   = Vector3.forward; // outward axis when using GlobalDirection
    public float amplitude           = 0.22f;           // max push distance (meters)
    public float baseOutset          = 0.05f;           // always push at least this much outward
    public bool  outwardOnly         = true;            // never pull inward toward play space

    [Header("Jaggedness (ridged + warp)")]
    [Range(0f,1f)] public float ridgedAmount = 0.65f;   // 0 = soft perlin, 1 = ridged
    public float ridgeSharpness = 1.4f;                 // >1 sharpens ridges
    public float warpStrength   = 0.20f;                // small domain warp to kill blobby look
    public float warpScale      = 0.5f;                 // warp frequency (relative to worldScale)

    [Header("FBM")]
    [Range(1,5)]  public int octaves = 3;
    [Range(0f,1f)] public float persistence = 0.55f;

    [Header("Keep tops flatter")]
    [Range(0f,4f)] public float upMaskHardness = 3.2f;  // higher = flatter upward faces

    [Header("Safety")]
    public bool makeMeshInstance = true;

    MeshFilter mf;
    Mesh original, instance;
    Vector3[] baseVerts;
    Vector3[] baseNormals;
    int cachedVertCount = -1;

    void OnEnable(){ Init(); Rebuild(); }
    void OnValidate(){ Init(); Rebuild(); }
    void OnDisable(){ if (instance) DestroyImmediate(instance); instance = null; }

    void Init()
    {
        if (!mf) mf = GetComponent<MeshFilter>();
        if (!mf || !mf.sharedMesh) return;

        if (makeMeshInstance)
        {
            if (original == null || mf.sharedMesh != instance)
            {
                original = mf.sharedMesh;
                instance = Instantiate(original);
                instance.name = original.name + " (WSJagged)";
                mf.sharedMesh = instance;
                CaptureBase(instance);
            }
        }
        else
        {
            instance = mf.sharedMesh;
            if (baseVerts == null || cachedVertCount != instance.vertexCount)
                CaptureBase(instance);
        }
    }

    void CaptureBase(Mesh m)
    {
        cachedVertCount = m.vertexCount;
        baseVerts   = m.vertices;
        baseNormals = (m.normals != null && m.normals.Length == m.vertexCount) ? m.normals : null;
    }

    [ContextMenu("Capture Base (re-cache)")]
    public void CaptureBaseNow() { if (mf && mf.sharedMesh) CaptureBase(mf.sharedMesh); Rebuild(); }

    [ContextMenu("Reset Mesh To Base")]
    public void ResetToBase()
    {
        if (!mf || !mf.sharedMesh || baseVerts == null) return;
        var m = mf.sharedMesh;
        m.vertices = (Vector3[])baseVerts.Clone();
        m.RecalculateNormals();
        m.RecalculateBounds();
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (!isActiveAndEnabled || instance == null || baseVerts == null) return;
        if (instance.vertexCount != cachedVertCount) CaptureBase(instance);

        var outVerts = new Vector3[baseVerts.Length];
        Transform ax = anchor ? anchor : null;

        // global outward dir (for GlobalDirection mode)
        Vector3 worldOut = (ax ? ax.TransformDirection(globalDirection) : globalDirection).normalized;

        Vector2 off = SeedToOffset(seed);
        float big = Mathf.Max(0.0001f, worldScale);
        float warpBig = Mathf.Max(0.0001f, worldScale * warpScale);

        for (int i = 0; i < baseVerts.Length; i++)
        {
            Vector3 baseLocal = baseVerts[i];
            Vector3 worldPos  = transform.TransformPoint(baseLocal);
            Vector3 sp        = ax ? ax.InverseTransformPoint(worldPos) : worldPos; // sample in shared space

            // domain warp: tiny position offset to break up round perlin blobbiness
            if (warpStrength > 0f)
            {
                float wx = (FBM3((sp.y+off.x)/warpBig, (sp.z+off.y)/warpBig, (sp.x+off.x)/warpBig, octaves, persistence) - 0.5f);
                float wy = (FBM3((sp.z+off.x)/warpBig, (sp.x+off.y)/warpBig, (sp.y+off.x)/warpBig, octaves, persistence) - 0.5f);
                sp += new Vector3(wx, wy, -wx) * (warpStrength * big * 0.25f); // small, anisotropic warp
            }

            // base fbm (0..1)
            float nx = (sp.x + off.x) / big;
            float ny = (sp.y + off.y) / big;
            float nz = (sp.z + off.x * 0.5f) / big;
            float p  = FBM3(nx, ny, nz, octaves, persistence);

            // ridged variant: 1 - |2p-1|   (peaks + valleys sharpened)
            float ridge = 1f - Mathf.Abs(2f*p - 1f);
            ridge = Mathf.Pow(Mathf.Clamp01(ridge), ridgeSharpness);

            // blend soft/ridged
            float n = Mathf.Lerp(p, ridge, ridgedAmount);

            // mask tops using base normals (keep ledges clean)
            float mask = 1f;
            if (baseNormals != null && baseNormals.Length == baseVerts.Length)
            {
                float up = Mathf.Clamp01(baseNormals[i].y);
                mask = 1f - Mathf.Pow(up, upMaskHardness);
            }

            // signed push in [-ampl..+ampl]
            float signed = (n - 0.5f) * 2f * amplitude * mask;

            // outward-only clamp + base outset
            if (outwardOnly) signed = Mathf.Max(0f, signed);
            float push = baseOutset + signed;

            Vector3 dir = (displaceMode == DisplaceMode.AlongNormal)
                ? ((baseNormals != null ? baseNormals[i] : Vector3.up).normalized)
                : worldOut;

            outVerts[i] = baseLocal + dir * push;
        }

        instance.vertices = outVerts;
        instance.RecalculateNormals();
        instance.RecalculateBounds();
    }

    static Vector2 SeedToOffset(int s)
    {
        unchecked {
            uint u = (uint)s;
            u ^= 2747636419u; u *= 2654435769u; u ^= (u >> 16); u *= 2654435769u; u ^= (u >> 16);
            float a = (u & 0xFFFF) / 65535f * 1000f;
            float b = ((u >> 16) & 0xFFFF) / 65535f * 1000f;
            return new Vector2(a, b);
        }
    }

    // cheap 3D-ish fbm built from 2D perlin mixes
    static float FBM3(float x, float y, float z, int oct, float p)
    {
        float amp = 1f, sum = 0f, norm = 0f, fx = x, fy = y, fz = z;
        for (int i = 0; i < oct; i++)
        {
            float n = Mathf.PerlinNoise(fx + Mathf.PerlinNoise(fz, fy), fy);
            sum += n * amp; norm += amp;
            amp *= p; fx *= 2f; fy *= 2f; fz *= 2f;
        }
        return (norm > 0f) ? sum / norm : 0f;
    }
}
