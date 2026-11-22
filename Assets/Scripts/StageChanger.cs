using System;
using System.Collections;
using UnityEngine;

public class StageChanger : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    private bool isTransitioning;
    private static bool promptOpen;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTransitioning && !promptOpen)
            StartCoroutine(TransitionAfterDelay());
    }

    private IEnumerator TransitionAfterDelay()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(1f);
        RequestTransition(nextSceneName);
        isTransitioning = false;
    }

    public static void RequestTransition(string destination, Action beforeLoad = null)
    {
        if (promptOpen)
            return;

        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogWarning($"Cannot load stage {destination}.");
            return;
        }

        var prompt = FindFirstObjectByType<StageSavePrompt>(FindObjectsInactive.Include);
        if (prompt == null)
        {
            Debug.LogError("Stage Save Prompt is missing from the scene hierarchy.");
            return;
        }
        promptOpen = true;
        InputLockManager.TransitionLock = true;
        InputLockManager.Refresh();
        prompt.Show(destination, beforeLoad, () => promptOpen = false);
    }
}
