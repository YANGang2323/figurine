using UnityEngine;

public class RedStoneInteractable : TimingInteractable
{
    public ClockRotator clock;

    public override void Activate()
    {
        base.Activate();

        if (clock != null)
            clock.RotateClock();
    }
}