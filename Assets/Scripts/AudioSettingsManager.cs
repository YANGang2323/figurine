using UnityEngine;

public class AudioSettingsManager : MonoBehaviour
{
    public static AudioSettingsManager Instance;

    [Range(0f, 1f)]
    public float masterVolume = 1f;

    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    [Range(0f, 1f)]
    public float sfxVolume = 1f;

    public bool masterMuted;
    public bool bgmMuted;
    public bool sfxMuted;

    private float prevMasterVolume = 1f;
    private float prevBGMVolume = 1f;
    private float prevSFXVolume = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
    }

    public void SetMasterVolume(float value)
    {
        masterVolume = value;
        SaveSettings();
    }

    public void SetBGMVolume(float value)
    {
        bgmVolume = value;
        SaveSettings();
    }

    public void SetSFXVolume(float value)
    {
        sfxVolume = value;
        SaveSettings();
    }

    public void ToggleMasterMute()
    {
        if (!masterMuted){
            prevMasterVolume = masterVolume;

            masterVolume = 0f;
            masterMuted = true;
        }else {
            masterVolume = prevMasterVolume;
            masterMuted = false;
        }
        SaveSettings();
    }

    public void ToggleBGMMute()
    {
        if (!bgmMuted) {
            prevBGMVolume = bgmVolume;

            bgmVolume = 0f;
            bgmMuted = true;
        } else {
            bgmVolume = prevBGMVolume;
            bgmMuted = false;
        }
        SaveSettings();
    }

    public void ToggleSFXMute()
    {
        if (!sfxMuted) {
            prevSFXVolume = sfxVolume;

            sfxVolume = 0f;
            sfxMuted = true;
        } else{
            sfxVolume = prevSFXVolume;
            sfxMuted = false;
        }
        SaveSettings();
    }

    void SaveSettings()
    {
        PlayerPrefs.SetInt("MasterMuted", masterMuted ? 1 : 0);
        PlayerPrefs.SetInt("BGMMuted", bgmMuted ? 1 : 0);
        PlayerPrefs.SetInt("SFXMuted", sfxMuted ? 1 : 0);
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("BGMVolume", bgmVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        
        PlayerPrefs.Save();
    }

    void LoadSettings()
    {
        masterMuted = PlayerPrefs.GetInt("MasterMuted", 0) == 1;
        bgmMuted = PlayerPrefs.GetInt("BGMMuted", 0) == 1;
        sfxMuted = PlayerPrefs.GetInt("SFXMuted", 0) == 1;
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 0.8f);
        bgmVolume = PlayerPrefs.GetFloat("BGMVolume", 0.8f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
        
    }
}