using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class ProceduralMountainFace : MonoBehaviour
{
    [Header("Dimensions")]
    public float height = 10f;         // height of this chunk
    public float baseRadius = 18f;
    public float topRadius  = 16.5f;

    [Header("Surface Noise")]
    public int radialSegments = 96;
    public int verticalSegments = 12;
    public float radialNoiseAmp = 1.8f;
    public float radialNoiseFreq = 0.45f;
    public float verticalNoiseFreq = 0.22f;

    [Header("Materials")]
    public Material cliffMaterial;
    public Material ledgeMaterial;

    [Header("Terrace / Ledges")]
    public int terraces = 2;                  // how many ledge rings inside this chunk
    public float terraceThickness = 0.25f;    // ledge "slab" thickness
    public float terraceWidth = 2.2f;         // how far it sticks out
    public int terraceArcSegments = 24;       // mesh resolution along an arc
    public Vector2 terraceArcDegrees = new Vector2(60f, 160f); // min/max arc length in degrees

    System.Random rng;

    public void Build(int seed)
    {
        rng = new System.Random(seed);
        BuildCliff();
        BuildTerraces();
    }

    void BuildCliff()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();
        var mesh = new Mesh();

        var verts = new List<Vector3>();
        var tris  = new List<int>();
        var uvs   = new List<Vector2>();

        int rs = Mathf.Max(12, radialSegments);
        int vs = Mathf.Max(2, verticalSegments);

        for (int y = 0; y <= vs; y++)
        {
            float ty = y / (float)vs;
            float yPos = ty * height;
            float rBase = Mathf.Lerp(baseRadius, topRadius, ty);

            for (int i = 0; i <= rs; i++)
            {
                float t = i / (float)rs;
                float th = t * Mathf.PI * 2f;

                float n = Mathf.PerlinNoise(t * radialNoiseFreq * 10f + 31f,
                                            ty * verticalNoiseFreq * 10f + 97f);
                float rr = rBase + (n - 0.5f) * radialNoiseAmp;
                verts.Add(new Vector3(Mathf.Cos(th) * rr, yPos, Mathf.Sin(th) * rr));
                uvs.Add(new Vector2(t, ty));
            }
        }

        int stride = rs + 1;
        for (int y = 0; y < vs; y++)
        {
            for (int i = 0; i < rs; i++)
            {
                int a = y * stride + i;
                int b = a + 1;
                int c = a + stride;
                int d = c + 1;
                tris.Add(a); tris.Add(c); tris.Add(b);
                tris.Add(b); tris.Add(c); tris.Add(d);
            }
        }

        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();

        mf.sharedMesh = mesh;
        if (mr && cliffMaterial) mr.sharedMaterial = cliffMaterial;
    }

    void BuildTerraces()
    {
        if (terraces <= 0 || ledgeMaterial == null) return;

        for (int k = 0; k < terraces; k++)
        {
            float ty = (k + 1) / (float)(terraces + 1);      // spread within this chunk
            float y  = ty * height;
            float r  = Mathf.Lerp(baseRadius, topRadius, ty);

            // choose a random arc on this ring
            float arcLenDeg = Mathf.Lerp(terraceArcDegrees.x, terraceArcDegrees.y, (float)rng.NextDouble());
            float startDeg  = (float)rng.NextDouble() * 360f;
            float endDeg    = startDeg + arcLenDeg;

            BuildTerraceArc(y, r, startDeg, endDeg);
        }
    }

    void BuildTerraceArc(float y, float radiusCenter, float startDeg, float endDeg)
    {
        // Create a little “balcony” arc: inner radius = radiusCenter - small inset, outer = + terraceWidth
        float rIn  = radiusCenter - 0.2f;
        float rOut = radiusCenter + terraceWidth;

        int steps = Mathf.Max(8, terraceArcSegments);
        var verts = new List<Vector3>();
        var tris  = new List<int>();

        // top ring then bottom ring (for thickness)
        for (int ring = 0; ring < 2; ring++)
        {
            float yRing = y + (ring == 0 ? 0f : -terraceThickness);
            for (int i = 0; i <= steps; i++)
            {
                float t = i / (float)steps;
                float deg = Mathf.Lerp(startDeg, endDeg, t) * Mathf.Deg2Rad;

                // two verts per step: inner then outer
                verts.Add(new Vector3(Mathf.Cos(deg) * rIn,  yRing, Mathf.Sin(deg) * rIn));
                verts.Add(new Vector3(Mathf.Cos(deg) * rOut, yRing, Mathf.Sin(deg) * rOut));
            }
        }

        int ringStride = (steps + 1) * 2;

        // top surface
        for (int i = 0; i < (steps); i++)
        {
            int a = i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            tris.Add(a); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(d);
        }

        // bottom surface
        int o = ringStride;
        for (int i = 0; i < (steps); i++)
        {
            int a = o + i * 2;
            int b = a + 1;
            int c = a + 2;
            int d = a + 3;
            tris.Add(d); tris.Add(c); tris.Add(b);
            tris.Add(b); tris.Add(c); tris.Add(a);
        }

        // outer wall
        for (int i = 0; i < (steps); i++)
        {
            int topOuterA = i * 2 + 1;
            int topOuterB = topOuterA + 2;
            int botOuterA = o + i * 2 + 1;
            int botOuterB = botOuterA + 2;

            tris.Add(topOuterA); tris.Add(botOuterB); tris.Add(botOuterA);
            tris.Add(topOuterA); tris.Add(topOuterB); tris.Add(botOuterB);
        }

        // inner wall
        for (int i = 0; i < (steps); i++)
        {
            int topInnerA = i * 2 + 0;
            int topInnerB = topInnerA + 2;
            int botInnerA = o + i * 2 + 0;
            int botInnerB = botInnerA + 2;

            tris.Add(botInnerA); tris.Add(botInnerB); tris.Add(topInnerA);
            tris.Add(topInnerB); tris.Add(topInnerA); tris.Add(botInnerB);
        }

        var mesh = new Mesh();
        mesh.SetVertices(verts);
        mesh.SetTriangles(tris, 0);
        mesh.RecalculateNormals();

        var arcGO = new GameObject("TerraceArc");
        arcGO.transform.SetParent(transform, false);
        var mf = arcGO.AddComponent<MeshFilter>();
        var mr = arcGO.AddComponent<MeshRenderer>();
        var mc = arcGO.AddComponent<MeshCollider>();
        mf.sharedMesh = mesh;
        mr.sharedMaterial = ledgeMaterial;
        mc.sharedMesh = mesh;
        mc.convex = false; // static ledge collider
    }
}
