using System;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;
using Unity.Services.Vivox;

// Drop this on a GameObject in your scene (e.g., "Voice").
// It logs into Unity Services + Vivox, then joins a voice channel.
// Start with usePositional = false to verify voice works, then turn it on.
public class VoiceManagerUGS : MonoBehaviour
{
    public static VoiceManagerUGS Instance { get; private set; }

    [Header("Channel")]
    [Tooltip("Any unique name. All players join the same channel.")]
    public string channelName = "proximity_channel";

    [Header("Mode")]
    [Tooltip("Off = 2D party chat. On = 3D positional (proximity) audio.")]
    public bool usePositional = false;

    [Header("Positional Settings (used when usePositional = true)")]
    [Tooltip("Distance (meters) at which the voice becomes inaudible.")]
    public int audibleDistance = 22;
    [Tooltip("Distance (meters) within which voice is at full volume.")]
    public int conversationalDistance = 3;
    [Tooltip("Higher = faster fade with distance (1.0 is default).")]
    public float fadeIntensity = 1.0f;
    [Tooltip("Fade model for distance rolloff.")]
    public AudioFadeModel fadeModel = AudioFadeModel.InverseByDistance;

    [Header("Local Player Hook (set automatically)")]
    public Transform localPlayer; // assigned by NetworkPlayer.OnStartAuthority

    bool initialized;
    bool loggedInVivox;
    bool joined;
    string joinedChannel;

    void Awake()
    {
        if (Instance && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    async void Start()
    {
        await InitializeAndLoginAsync();
        if (loggedInVivox) await JoinVoiceAsync();
    }

    async Task InitializeAndLoginAsync()
    {
        try
        {
            if (UnityServices.State == ServicesInitializationState.Uninitialized)
                await UnityServices.InitializeAsync();

            // Anonymous UGS auth
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();

            // Vivox login (required before joining channels)
            await VivoxService.Instance.LoginAsync();

            initialized = true;
            loggedInVivox = true;
            Debug.Log("[Vivox] Initialized and logged in.");
        }
        catch (Exception e)
        {
            Debug.LogError("[Vivox] Init/Login failed: " + e);
            initialized = false;
            loggedInVivox = false;
        }
    }

    async Task JoinVoiceAsync()
    {
        if (!loggedInVivox) return;

        try
        {
            if (!usePositional)
            {
                // 2D non-positional (everyone hears everyone; no distance fade)
                await VivoxService.Instance.JoinGroupChannelAsync(
                    channelName,
                    ChatCapability.AudioOnly
                );
                joined = true;
                joinedChannel = channelName;
                Debug.Log("[Vivox] Joined 2D group channel: " + channelName);
            }
            else
            {
                // 3D positional (proximity) channel
                var props = new Channel3DProperties(
                    audibleDistance,
                    conversationalDistance,
                    fadeIntensity,
                    fadeModel
                );

                await VivoxService.Instance.JoinPositionalChannelAsync(
                    channelName,
                    ChatCapability.AudioOnly,
                    props
                );
                joined = true;
                joinedChannel = channelName;
                Debug.Log("[Vivox] Joined 3D positional channel: " + channelName);
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[Vivox] Join channel failed: " + e);
            joined = false;
            joinedChannel = null;
        }
    }

    void Update()
    {
        // Only needed for positional mode: feed Vivox the local player's transform every frame
        if (joined && usePositional && localPlayer != null && !string.IsNullOrEmpty(joinedChannel))
        {
            // v16 overload: (GameObject participantObject, string channelName, bool allowPanning)
            VivoxService.Instance.Set3DPosition(localPlayer.gameObject, joinedChannel, allowPanning: true);
        }
    }

    public void AttachLocalPlayer(Transform t)
    {
        localPlayer = t;
        // If we switch to positional at runtime, ensure we start updating positions
    }

    // Optional helpers if you want to toggle modes at runtime:
    public async Task RejoinAs2DAsync()
    {
        usePositional = false;
        if (joined) await VivoxService.Instance.LeaveChannelAsync(channelName);
        await JoinVoiceAsync();
    }

    public async Task RejoinAs3DAsync()
    {
        usePositional = true;
        if (joined) await VivoxService.Instance.LeaveChannelAsync(channelName);
        await JoinVoiceAsync();
    }
}
