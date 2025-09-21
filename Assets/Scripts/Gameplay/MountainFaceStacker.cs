using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class MountainFaceStacker : NetworkBehaviour
{
    public ProceduralMountainFace facePrefab;

    [Header("Streaming")]
    public int initialFaces = 14;       // 14 * 10m ≈ 140m
    public float keepAheadHeight = 80f;
    public float cullBelow = 60f;

    [SyncVar] int seed;
    System.Random rng;
    float builtTopY;
    readonly List<(GameObject go, float y, float h)> spawned = new();

    public Transform trackPlayer;

    public override void OnStartServer()
    {
        seed = (MatchConfig.I && MatchConfig.I.levelSeed != 0) ? MatchConfig.I.levelSeed : UnityEngine.Random.Range(1, int.MaxValue);
        rng = new System.Random(seed);
        Rebuild();
    }

    public override void OnStartClient()
    {
        rng = new System.Random(seed);
        Rebuild();
    }

    void Update()
    {
        if (!trackPlayer) return;

        while (builtTopY < trackPlayer.position.y + keepAheadHeight)
            SpawnNext();

        for (int i = spawned.Count - 1; i >= 0; i--)
        {
            if (spawned[i].y + spawned[i].h < trackPlayer.position.y - cullBelow)
            {
                if (spawned[i].go) Destroy(spawned[i].go);
                spawned.RemoveAt(i);
            }
        }
    }

    void Rebuild()
    {
        foreach (var s in spawned) if (s.go) Destroy(s.go);
        spawned.Clear();
        builtTopY = 0f;
        for (int i = 0; i < initialFaces; i++) SpawnNext();
    }

    void SpawnNext()
    {
        var face = Instantiate(facePrefab, new Vector3(0f, builtTopY, 0f), Quaternion.identity);
        int faceSeed = rng.Next();
        face.Build(faceSeed);

        float h = face.height;
        spawned.Add((face.gameObject, builtTopY, h));
        builtTopY += h;
    }
}
