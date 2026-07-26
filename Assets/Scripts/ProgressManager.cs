using UnityEngine;

public class ProgressManager : MonoBehaviour
{
    public static ProgressManager Instance;

    void Awake()
    {
        Instance = this;
    }

    public bool HasFlag(string flag)
    {
        return DataManager.Instance.nowGame.progressFlags.Contains(flag);
    }

    public void AddFlag(string flag)
    {
        if (!HasFlag(flag))
            DataManager.Instance.nowGame.progressFlags.Add(flag);
    }

    public void RemoveFlag(string flag)
    {
        DataManager.Instance.nowGame.progressFlags.Remove(flag);
    }
}