using UnityEngine;

public class SpotlightColor : MonoBehaviour
{
    [Header("Spot Lights")]
    [SerializeField] private Light[] sideLights;   // 사이드뷰에서 켜질 조명
    [SerializeField] private Light[] frontLights;  // 정면뷰에서 켜질 조명

    [Header("Colors")]
    [SerializeField] private Color activeColor = Color.red;
    [SerializeField] private Color inactiveColor = Color.white;

    private bool lastSideView;

    void Start()
    {
        lastSideView = CameraPivotRotator.Instance.isSideView;
        ApplyColor(lastSideView);
    }

    void Update()
    {
        bool current = CameraPivotRotator.Instance.isSideView;

        if (current != lastSideView)
        {
            lastSideView = current;
            ApplyColor(current);
        }
    }

    void ApplyColor(bool sideView)
    {
        foreach (Light l in sideLights)
        {
            if (l == null)
                continue;

            Color target = sideView ? activeColor : inactiveColor;
            l.color = target;

            CCTVController cctv = l.GetComponentInParent<CCTVController>();
            if (cctv != null)
                cctv.SetNormalColor(target);
        }

        foreach (Light l in frontLights)
        {
            if (l == null)
                continue;

            Color target = sideView ? inactiveColor : activeColor;
            l.color = target;

            CCTVController cctv = l.GetComponentInParent<CCTVController>();
            if (cctv != null)
                cctv.SetNormalColor(target);
        }
    }
}