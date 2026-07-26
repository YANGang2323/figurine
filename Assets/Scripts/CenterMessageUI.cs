using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CenterMessageUI : MonoBehaviour
{
    public static CenterMessageUI Instance;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private GameObject root;
    [SerializeField] private Image background;

    [Header("Texts")]
    [SerializeField] private TMP_Text titleText;      // figurine
    [SerializeField] private TMP_Text bodyText1;       // 본문
    [SerializeField] private TMP_Text bodyText2;       // 본문
    [SerializeField] private TMP_Text playCodeText;   // #0000

    [Header("Others")]
    [SerializeField] private Image qrImage;
    [SerializeField] private GameObject menuButton;   

    public string webUrl = "https://google.com";

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
        SetTextAlpha(titleText, 0);
        SetTextAlpha(bodyText1, 0);
        SetTextAlpha(bodyText2, 0);
        SetTextAlpha(playCodeText, 0);

        SetImageAlpha(qrImage, 0);

        menuButton.SetActive(false);
    }

    public IEnumerator EndingRoutine(string playCode)
    {
        titleText.text = "figurine";

        bodyText1.text =
            "figurine의 데모 버전을 플레이해 주셔서 감사합니다.\n" +
            "이야기는 정식 버전에서 계속됩니다.\n\n" +
            "당신의 플레이 코드입니다.";

        playCodeText.text = playCode;

        bodyText2.text =
            "아래 QR 코드를 통해 설문조사에 참여해 주세요.\n\n" +
            "여러분의 소중한 의견은\n" +
            "정식 버전 개발에 큰 도움이 됩니다.";

        SetImageAlpha(background, 1);
        //yield return screenFader.FadeOut(1f);
        root.SetActive(true);
        yield return new WaitForSeconds(3f);

        yield return FadeText(titleText, 0.8f);

        yield return FadeText(bodyText1, 0.8f);
        yield return FadeText(playCodeText, 0.8f);
        yield return FadeText(bodyText2, 0.8f);
        yield return FadeImage(qrImage, 0.8f);

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

    IEnumerator FadeImage(Image img, float time)
    {
        float t = 0;

        while (t < time)
        {
            t += Time.deltaTime;
            SetImageAlpha(img, Mathf.Clamp01(t / time));
            yield return null;
        }

        SetImageAlpha(img, 1);
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

    public void ConnectLink()
    {
        Debug.Log("버튼 클릭됨");
        if (!string.IsNullOrEmpty(webUrl))
            Application.OpenURL(webUrl);
    }

    public void MainMenu()
    {
        SceneManager.LoadScene("MainMenu");
    }
}