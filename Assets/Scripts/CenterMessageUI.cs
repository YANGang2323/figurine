using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class CenterMessageUI : MonoBehaviour
{
    public static CenterMessageUI Instance;
    [SerializeField] private GameObject root;
    [SerializeField] private Image background;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;      // figurine
    [SerializeField] private TMP_Text bodyText1;       // 본문

    [Header("Others")]
    [SerializeField] private GameObject menuButton;   

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
        SetTextAlpha(titleText, 0);
        SetTextAlpha(bodyText1, 0);

        menuButton.SetActive(false);
    }

    public IEnumerator EndingRoutine()
    {
        titleText.text = "figurine";

        bodyText1.text =
            "데모 버전을 끝까지 플레이해 주셔서 감사합니다.\n" +
            "figurine의 이야기는 계속됩니다.\n\n" +
            "나가실 때 만족도 조사에 참여해 주시면 두 배로 도움이 됩니다!";

        SetImageAlpha(background, 1);
        root.SetActive(true);
        yield return new WaitForSeconds(3f);

        yield return FadeText(titleText, 0.8f);

        yield return FadeText(bodyText1, 0.8f);

        menuButton.SetActive(true);
    }

    IEnumerator FadeText(TMP_Text text, float time)
    {
        float t = 0;

        while (t < time)
        {
            t += Time.deltaTime;
            SetTextAlpha(text, Mathf.Clamp01(t / time));
            yield return null;
        }

        SetTextAlpha(text, 1);
    }

    void SetTextAlpha(TMP_Text text, float alpha)
    {
        Color c = text.color;
        c.a = alpha;
        text.color = c;
    }

    void SetImageAlpha(Image img, float alpha)
    {
        Color c = img.color;
        c.a = alpha;
        img.color = c;
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}
