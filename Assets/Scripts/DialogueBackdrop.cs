using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public sealed class DialogueBackdrop : MonoBehaviour
{
    [SerializeField] private Image storyBackground;

    private static readonly HashSet<DialogueBackdrop> ActiveBackdrops = new();

    private void OnEnable() => ActiveBackdrops.Add(this);
    private void OnDisable() => ActiveBackdrops.Remove(this);

    internal static bool AppliesTo(Camera camera)
    {
        if (ActiveBackdrops.Count == 0 || camera.cameraType != CameraType.Game || camera != Camera.main)
            return false;

        foreach (DialogueBackdrop backdrop in ActiveBackdrops)
        {
            if (!backdrop.storyBackground.isActiveAndEnabled)
                return true;
        }

        return false;
    }
}