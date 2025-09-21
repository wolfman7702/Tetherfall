using System.Collections;
using UnityEngine;
using Mirror;

public class TetherAutoPair : MonoBehaviour
{
    public float startDelay = 1.0f;
    public float ropeWidth = 0.05f;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(startDelay);

        var players = FindObjectsOfType<NetworkPlayer>();
        NetworkPlayer local = null;
        foreach (var p in players) if (p.isOwned) { local = p; break; }
        if (local == null || players.Length < 2) yield break;

        // Find nearest other
        NetworkPlayer nearest = null;
        float best = float.MaxValue;
        foreach (var p in players)
        {
            if (p == local) continue;
            float d = Vector3.Distance(local.transform.position, p.transform.position);
            if (d < best) { best = d; nearest = p; }
        }
        if (nearest == null) yield break;

        // Make a rope object locally
        var ropeGO = new GameObject($"Tether_{local.name}_{nearest.name}");
        var link = ropeGO.AddComponent<TetherLink>();
        var lr = ropeGO.AddComponent<LineRenderer>();

        // Simple default material
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.widthMultiplier = ropeWidth;
        lr.textureMode = LineTextureMode.Stretch;

        link.line = lr;
        link.a = local.transform;
        link.b = nearest.transform;
        // tweak if you want:
        link.slack = 4.0f;
        link.maxStretch = 7.0f;
        link.stiffness = 2.0f;
        link.speedPenaltyAtMax = 0.5f;
    }
}
