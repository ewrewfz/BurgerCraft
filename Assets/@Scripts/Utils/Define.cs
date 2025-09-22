using UnityEngine;

public static class Define
{
    public enum EState
    {
        None,
        Idle,
        Move
    }

    public static int IDLE = Animator.StringToHash("Idle");
    public static int MOVE = Animator.StringToHash("Move");
    public static int SERVING_IDLE = Animator.StringToHash("ServingIdle");
    public static int SERVING_MOVE = Animator.StringToHash("ServingMove");
}
