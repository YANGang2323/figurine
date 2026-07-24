using System.Collections.Generic;
using UnityEngine;

public class DialogueTarget : MonoBehaviour
{
    public string targetID;

    private static Dictionary<string, DialogueTarget> targets
        = new Dictionary<string, DialogueTarget>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]

    static void RegisterAll()
    {
        targets.Clear();

        DialogueTarget[] allTargets =
            Resources.FindObjectsOfTypeAll<DialogueTarget>();

        foreach (DialogueTarget target in allTargets)
        {
            if (string.IsNullOrEmpty(target.targetID))
                continue;

            targets[target.targetID] = target;
        }
    }

    private void Awake()
    {
        if (!string.IsNullOrEmpty(targetID))
        {
            targets[targetID] = this;
        }
    }

    private void OnDestroy()
    {
        if (targets.ContainsKey(targetID))
        {
            targets.Remove(targetID);
        }
    }

    public static DialogueTarget Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        targets.TryGetValue(id, out DialogueTarget target);
        return target;
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        gameObject.SetActive(false);
    }
}