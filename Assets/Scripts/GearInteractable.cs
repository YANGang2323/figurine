using UnityEngine;

public class GearInteractable : TimingInteractable
{
    public GameObject gearObject;

    public override void Activate()
    {
        base.Activate();

        if (gearObject != null)
        {
            Destroy(gearObject);
        }
    }
}