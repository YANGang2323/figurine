using UnityEngine;
using UnityEngine.UI;

public class DetectionUI : MonoBehaviour
{
    public static DetectionUI Instance;

    public Image detectionBar;
    public Image damageOverlay;
    public AudioSource warningSound;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateUI(float ratio)
    {
        // 게이지
        detectionBar.fillAmount = ratio;

        // 화면 빨개짐
        Color c = damageOverlay.color;
        c.a = ratio * 0.6f;
        damageOverlay.color = c;

        // 경고음
        if (ratio > 0)
        {
            if (!warningSound.isPlaying)
                warningSound.Play();
        }
        else
        {
            warningSound.Stop();
        }
    }
}