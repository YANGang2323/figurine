using UnityEngine;

public static class InputLockManager
{
    public static bool DialogueLock;
    public static bool ESCMenuLock;
    public static bool CameraRotateLock;

    public static System.Action OnLock;

    public static bool LockMove =>
        DialogueLock || ESCMenuLock || CameraRotateLock;

    public static bool LockInteraction =>
        DialogueLock || ESCMenuLock;

    public static bool LockCamera =>
        DialogueLock || ESCMenuLock || CameraRotateLock;

    public static void Refresh()
    {
        if (LockMove)
            OnLock?.Invoke();
    }
}