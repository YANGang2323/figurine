using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class NoiseJitter : MonoBehaviour
{
    private RawImage image;
    private Coroutine jitterCoroutine;

    private void Awake()
    {
        image = GetComponent<RawImage>();
    }

    private void OnEnable()
    {
        // 혹시 남아있을 수도 있는 코루틴 정리
        if (jitterCoroutine != null)
            StopCoroutine(jitterCoroutine);

        jitterCoroutine = StartCoroutine(Jitter());
    }

    private void OnDisable()
    {
        if (jitterCoroutine != null)
        {
            StopCoroutine(jitterCoroutine);
            jitterCoroutine = null;
        }
    }

    IEnumerator Jitter()
    {
        while (true)
        {
            Rect rect = image.uvRect;
            rect.x = Random.Range(0f, 1f);
            rect.y = Random.Range(0f, 1f);
            image.uvRect = rect;

            Color c = image.color;
            c.a = Random.Range(0.03f, 0.08f);
            image.color = c;

            yield return new WaitForSecondsRealtime(0.05f);
        }
    }
}