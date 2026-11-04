using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class Stage2IntroTrigger : MonoBehaviour
{
    public Stage2IntroController controller;

    void OnTriggerEnter(Collider other) => controller.TryStartBackstageCutscene(other);

    // Also accept a player who changes to side view while already inside the zone.
    void OnTriggerStay(Collider other) => controller.TryStartBackstageCutscene(other);
}
