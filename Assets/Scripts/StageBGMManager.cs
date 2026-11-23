using System.Collections;
using UnityEngine;

public class StageBGMManager : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip mapBGM;
    [SerializeField] private float targetVolume = 1f;
    [SerializeField] private float fadeTime = 1f;
    private bool started;

    private void Update()
    {
        if (DialogueManager.Instance == null)
            return;
        if (audioSource == null)
            return;
        // 컷씬 BGM이 재생 중이면 대기
        if (DialogueManager.Instance.IsCutsceneBGMPlaying)
            return;
        if (mapBGM == null)
            return;
        // 이미 맵 BGM이 나오고 있으면 종료
        if (audioSource.isPlaying)
            return;

        if (!started)
        {
            StartCoroutine(FadeInMapBGM());
        }
        else return;
    }

    IEnumerator FadeInMapBGM()
    {
        started = true;

        if (audioSource == null || mapBGM == null)
            yield break;

        audioSource.clip = mapBGM;
        audioSource.volume = 0f;
        audioSource.Play();

        float t = 0f;

        while (t < fadeTime)
        {
            t += Time.deltaTime;
            audioSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            yield return null;
        }

        audioSource.volume = targetVolume;
    }
}