using UnityEngine;
using UnityEngine.Rendering;

// Scene-local lighting cue; saved progress also restores the CCTV look on load.
public sealed class Stage2CCTVLighting : MonoBehaviour
{
    [SerializeField] private string activationFlag = "door2_firstinteract";
    [SerializeField] private string completionFlag = "door2_cctv_completed";
    [SerializeField] private CCTVController[] controllers;
    [SerializeField] private Light directionalLight;
    [SerializeField] private Light[] spotLights;
    [SerializeField] private float cctvDirectionalIntensity = 0.12f;
    [SerializeField] private Color cctvAmbientColor = new Color(0.055f, 0.065f, 0.09f);
    [SerializeField] private float cctvReflectionIntensity = 0.18f;

    private bool activated;
    private bool[] originalEnabled;
    private bool completed;
    private float originalDirectionalIntensity;
    private AmbientMode originalAmbientMode;
    private Color originalSky, originalEquator, originalGround;
    private float originalAmbientIntensity, originalReflectionIntensity;

    private void Awake()
    {
        originalDirectionalIntensity = directionalLight != null ? directionalLight.intensity : 1f;
        originalAmbientMode = RenderSettings.ambientMode;
        originalSky = RenderSettings.ambientSkyColor;
        originalEquator = RenderSettings.ambientEquatorColor;
        originalGround = RenderSettings.ambientGroundColor;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
        originalEnabled = new bool[spotLights.Length];
        for (int i = 0; i < spotLights.Length; i++)
        {
            if (spotLights[i] == null) continue;
            originalEnabled[i] = spotLights[i].enabled;
            spotLights[i].enabled = false;
        }
    }

    private void Start() => CheckProgress();

    private void Update() => CheckProgress();

    private void CheckProgress()
    {
        if (completed || ProgressManager.Instance == null ||
            DataManager.Instance == null || DataManager.Instance.nowGame == null)
            return;

        if (ProgressManager.Instance.HasFlag(completionFlag))
        {
            Complete();
            return;
        }
        if (activated || !ProgressManager.Instance.HasFlag(activationFlag)) return;

        activated = true;
        if (directionalLight != null)
            directionalLight.intensity = cctvDirectionalIntensity;
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = cctvAmbientColor;
        RenderSettings.reflectionIntensity = cctvReflectionIntensity;
        for (int i = 0; i < spotLights.Length; i++)
        {
            if (spotLights[i] != null)
                spotLights[i].enabled = originalEnabled[i];
        }
    }

    public void Complete()
    {
        if (completed) return;
        completed = true;
        if (ProgressManager.Instance != null && DataManager.Instance != null &&
            DataManager.Instance.nowGame != null)
            ProgressManager.Instance.AddFlag(completionFlag);

        if (directionalLight != null) directionalLight.intensity = originalDirectionalIntensity;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientSkyColor = originalSky;
        RenderSettings.ambientEquatorColor = originalEquator;
        RenderSettings.ambientGroundColor = originalGround;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
        foreach (var controller in controllers)
            if (controller != null) controller.ShutDown();
        foreach (var light in spotLights)
            if (light != null) light.enabled = false;
        enabled = false;
    }
}
