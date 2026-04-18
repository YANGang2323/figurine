using UnityEngine;

public class TimingPuzzleTrigger : MonoBehaviour
{
    public TimingInteractionManager manager;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            CameraPivotRotator.Instance.timingMode = true;
            manager.enabled = true;
        }
    }
}