using UnityEngine;

public static class InputLockManager
{
    public static bool LockMove;
    public static bool LockInteract;
    public static bool LockCamera;

    public static void UnlockAll()
    {
        LockMove = false;
        LockInteract = false;
        LockCamera = false;
    }
}