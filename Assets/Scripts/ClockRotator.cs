using UnityEngine;

public class ClockRotator : MonoBehaviour
{
    public float rotateAmount = 90f;
    public float rotateSpeed = 180f;

    private bool rotating = false;
    private Quaternion targetRot;

    void Start()
    {
        targetRot = transform.rotation;
    }

    void Update()
    {
        if (rotating)
        {
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRot,
                rotateSpeed * Time.deltaTime
            );

            if (Quaternion.Angle(transform.rotation, targetRot) < 0.1f)
            {
                transform.rotation = targetRot;
                rotating = false;
            }
        }
    }

    public void RotateClock()
    {
        if (rotating) return;

        targetRot *= Quaternion.Euler(0, rotateAmount, 0);
        rotating = true;
    }
}