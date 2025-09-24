using UnityEngine;

public enum PlayerMovement { Idle, Run, Sprint, Jump, Fall, Strafe }

public class PlayerState : MonoBehaviour
{
    [field: SerializeField] public PlayerMovement Current { get; private set; } = PlayerMovement.Idle;
    public void Set(PlayerMovement s) => Current = s;
}
