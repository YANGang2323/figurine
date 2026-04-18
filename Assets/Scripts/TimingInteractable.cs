using UnityEngine;

public class TimingInteractable : MonoBehaviour
{
    public bool isActivated = false;

    public Renderer rend;
    public Color normalColor = Color.white;
    public Color targetColor = Color.yellow;
    public Color activatedColor = Color.green;

    void Start()
    {
        rend = GetComponent<Renderer>();
        rend.material.color = normalColor;
    }

    public void SetTarget(bool active)
    {
        if (isActivated) return;

        if (active)
            rend.material.color = targetColor;
        else
            rend.material.color = normalColor;
    }

    public void Activate()
    {
        if (isActivated) return;

        isActivated = true;
        rend.material.color = activatedColor;
    }
}