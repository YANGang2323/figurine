using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NoiseJitter : MonoBehaviour
{
    RawImage image;

    void Start()
    {
        image = GetComponent<RawImage>();
        StartCoroutine(Jitter());
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