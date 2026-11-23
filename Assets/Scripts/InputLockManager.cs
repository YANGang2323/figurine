using UnityEngine;

public static class InputLockManager
{
    public static bool DialogueLock;
    public static bool ESCMenuLock;
    public static bool CameraRotateLock;
    public static bool TransitionLock;
    public static bool TutorialMoveLock;

    public static System.Action OnLock;

    public static bool LockMove =>
        DialogueLock || ESCMenuLock || CameraRotateLock || TransitionLock || TutorialMoveLock;

    public static bool LockInteraction =>
        DialogueLock || ESCMenuLock || TransitionLock || TutorialMoveLock;

    public static bool LockCamera =>
        DialogueLock || ESCMenuLock || CameraRotateLock || TransitionLock;

    public static void Refresh()
    {
        if (LockMove)
            OnLock?.Invoke();
    }
}
