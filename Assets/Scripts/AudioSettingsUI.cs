using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        LoadSliderValues();
    }

    public void LoadSliderValues()
    {
        if (AudioSettingsManager.Instance == null)
            return;

        masterSlider.SetValueWithoutNotify(
            AudioSettingsManager.Instance.masterVolume
        );

        bgmSlider.SetValueWithoutNotify(
            AudioSettingsManager.Instance.bgmVolume
        );

        sfxSlider.SetValueWithoutNotify(
            AudioSettingsManager.Instance.sfxVolume
        );
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioSettingsManager.Instance.SetMasterVolume(value);
    }

    public void OnBGMVolumeChanged(float value)
    {
        AudioSettingsManager.Instance.SetBGMVolume(value);
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioSettingsManager.Instance.SetSFXVolume(value);
    }
}