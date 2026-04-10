using UnityEngine;
using UnityEngine.UI;

public class DetectionUI : MonoBehaviour
{
    public static DetectionUI Instance;

    public AudioSource warningSound;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateUI(float ratio)
    {
        
        // °æ°íÀ½
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