using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    public AudioSource audioSource;

    [Range(0f, 1f)]
    public float masterVolume = 1f; // 나중에 설정창에서 변경

    public AudioClip openClip;
    [Range(0f, 1f)]
    public float openVolume = 0.5f;

    public AudioClip closeClip;
    [Range(0f, 1f)]
    public float closeVolume = 0.7f;

    public AudioClip clickClip;
    [Range(0f, 1f)]
    public float clickVolume = 1f;

    private void Awake()
    {
        Instance = this;
        audioSource.volume = 0.5f;
    }

    public void PlayClick()
    {
        if (clickClip != null)
        {

            audioSource.PlayOneShot(clickClip, clickVolume * masterVolume);
        }
    }

    public void PlayOpen()
    {
        if (openClip != null)
        {
            audioSource.pitch = 0.9f;
            audioSource.PlayOneShot(openClip, openVolume * masterVolume);
            audioSource.pitch = 1f;
        }
    }

    public void PlayClose()
    {
        if (closeClip != null)
        {
            audioSource.pitch = 0.3f;
            audioSource.PlayOneShot(closeClip, closeVolume * masterVolume);
            audioSource.pitch = 1f;
        }
    }
}