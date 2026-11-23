using UnityEngine;

public class Stage2ViewTutorialTrigger : MonoBehaviour
{
    [SerializeField] private Stage2IntroController controller;

    private void OnTriggerEnter(Collider other) => controller.TryShowViewTutorial(other);

    private void OnTriggerStay(Collider other) => controller.TryShowViewTutorial(other);
}
