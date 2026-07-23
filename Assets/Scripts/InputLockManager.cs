using UnityEngine;

public static class InputLockManager
{
    private static int lockCount = 0;

    public static bool IsLocked => lockCount > 0;

    public static void Lock()
    {
        lockCount++;
    }

    public static void Unlock()
    {
        lockCount = Mathf.Max(0, lockCount - 1);
    }
}