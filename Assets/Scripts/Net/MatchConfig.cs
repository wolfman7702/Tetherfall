using Mirror;
using UnityEngine;

public enum WinMode { FirstToTop, LastAlive, Either }
public enum DamageMode { None, FallDamage, FallDamageAndDeath }
public enum CheckpointMode { None, Easy }

public class MatchConfig : NetworkBehaviour
{
    public static MatchConfig I { get; private set; }

    [Header("Teams")]
    [SyncVar] public int maxTeams = 1;      // start simple: coop
    [SyncVar] public int maxTeamSize = 8;   // up to 8 total

    [Header("Win")]
    [SyncVar] public WinMode winMode = WinMode.FirstToTop;

    [Header("Falls & Damage")]
    [SyncVar] public DamageMode damageMode = DamageMode.None;
    [SyncVar] public float killY = -20f;

    [Header("Checkpoints")]
    [SyncVar] public CheckpointMode checkpointMode = CheckpointMode.None;

    [Header("Level")]
    [SyncVar] public int levelSeed = 0; // server sets once

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this; DontDestroyOnLoad(gameObject);
    }

    public override void OnStartServer()
    {
        if (levelSeed == 0) levelSeed = Random.Range(1, int.MaxValue);
    }
}
