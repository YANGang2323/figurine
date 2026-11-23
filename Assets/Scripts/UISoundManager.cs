using UnityEngine;

public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance;

    public AudioSource audioSource;

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
    }

    public void PlayClick()
    {
        if (clickClip != null)
        {
            float volume = clickVolume * GetSFXVolume();
            audioSource.PlayOneShot(clickClip, volume);
        }
    }

    public void PlayOpen()
    {
        if (openClip != null)
        {
            audioSource.pitch = 0.9f;
            float volume = openVolume * GetSFXVolume();
            audioSource.PlayOneShot(openClip, volume);
            audioSource.pitch = 1f;
        }
    }

    public void PlayClose()
    {
        if (closeClip != null)
        {
            audioSource.pitch = 0.3f;
            float volume = closeVolume * GetSFXVolume();
            audioSource.PlayOneShot(closeClip, volume);
            audioSource.pitch = 1f;
        }
    }

    float GetSFXVolume()
    {
        if (AudioSettingsManager.Instance == null)
            return 1f;

        return AudioSettingsManager.Instance.masterVolume *
               AudioSettingsManager.Instance.sfxVolume;
    }
}