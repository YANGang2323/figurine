using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public Image fadeImage;

    public IEnumerator FadeOut(float time)
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < time)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(0f, 1f, t / time);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 1f;
        fadeImage.color = c;
    }

    public IEnumerator FadeIn(float time)
    {
        float t = 0f;
        Color c = fadeImage.color;

        while (t < time)
        {
            t += Time.deltaTime;
            c.a = Mathf.Lerp(1f, 0f, t / time);
            fadeImage.color = c;
            yield return null;
        }

        c.a = 0f;
        fadeImage.color = c;
    }
}