using UnityEngine;
using UnityEngine.UI;

public class AudioSettingsUI : MonoBehaviour
{
    [SerializeField] private Slider masterSlider;
    [SerializeField] private Slider bgmSlider;
    [SerializeField] private Slider sfxSlider;

    [SerializeField] private Image masterIcon;
    [SerializeField] private Image bgmIcon;
    [SerializeField] private Image sfxIcon;

    [SerializeField] private Sprite soundOn;
    [SerializeField] private Sprite soundOff;

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

    public void OnMasterMute()
    {
        AudioSettingsManager.Instance.ToggleMasterMute();
        masterSlider.SetValueWithoutNotify(
            AudioSettingsManager.Instance.masterVolume
        );
        UpdateIcons();
    }

    public void OnBGMMute()
    {
        AudioSettingsManager.Instance.ToggleBGMMute();
        bgmSlider.SetValueWithoutNotify(
            AudioSettingsManager.Instance.bgmVolume
        );
        UpdateIcons();
    }

    public void OnSFXMute()
    {
        AudioSettingsManager.Instance.ToggleSFXMute();
        sfxSlider.SetValueWithoutNotify(
            AudioSettingsManager.Instance.sfxVolume
        );
        UpdateIcons();
    }

    void UpdateIcons()
    {
        masterIcon.sprite =
            AudioSettingsManager.Instance.masterMuted
            ? soundOff
            : soundOn;

        bgmIcon.sprite =
            AudioSettingsManager.Instance.bgmMuted
            ? soundOff
            : soundOn;

        sfxIcon.sprite =
            AudioSettingsManager.Instance.sfxMuted
            ? soundOff
            : soundOn;
    }

    public void OnMasterVolumeChanged(float value)
    {
        AudioSettingsManager.Instance.SetMasterVolume(value);
        AudioSettingsManager.Instance.masterMuted =
            value <= 0.001f;

        UpdateIcons();
    }

    public void OnBGMVolumeChanged(float value)
    {
        AudioSettingsManager.Instance.SetBGMVolume(value);
        AudioSettingsManager.Instance.bgmMuted =
            value <= 0.001f;

        UpdateIcons();
    }

    public void OnSFXVolumeChanged(float value)
    {
        AudioSettingsManager.Instance.SetSFXVolume(value);
        AudioSettingsManager.Instance.sfxMuted =
            value <= 0.001f;

        UpdateIcons();
    }
}