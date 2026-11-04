using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ExitFeedbackPopup : MonoBehaviour
{
    private const string SurveyUrl = "https://forms.gle/WUxtE1vZf894KVTPA";
    private const string CopyInstruction = "플레이 코드 박스를 클릭하면 코드가 복사됩니다.";
    private const string SurveyInstructions = "\n위 QR 코드를 스캔하거나 클릭해 만족도 조사에 참여해 주세요.\n" +
        "여러분의 의견은 정식 버전 개발에 큰 도움이 됩니다.";

    private TMP_Text codeLabel;
    private TMP_Text instructionsLabel;
    private Button cancelButton;
    private string playCode;

    public static ExitFeedbackPopup Create(TMP_FontAsset font, Sprite textBox,
        Sprite qr, Action confirm, Action cancel)
    {
        var root = new GameObject("Exit Feedback", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ExitFeedbackPopup));
        var canvas = root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue;
        var scaler = root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;

        var popup = root.GetComponent<ExitFeedbackPopup>();
        var backdrop = AddElement<Image>("Backdrop", root.transform, 0, 0, 0, 0);
        backdrop.rectTransform.anchorMin = Vector2.zero;
        backdrop.rectTransform.anchorMax = Vector2.one;
        backdrop.color = new Color(0, 0, 0, 0.8f);
        var panel = AddElement<Image>("Panel", root.transform, 0, 0, 940, 760);
        panel.color = new Color(0.075f, 0.065f, 0.052f);

        AddLabel("당신의 플레이 코드", panel.transform, font, 0, 313, 860, 56, 42);
        var codeButton = AddButton("Copy Play Code", panel.transform, textBox, 0, 239, 560, 80);
        popup.codeLabel = AddLabel("", codeButton.transform, font, 0, 0, 478, 50, 38);
        codeButton.onClick.AddListener(popup.CopyCode);

        var qrButton = AddButton("Open Survey", panel.transform, qr, 0, 50, 256, 256);
        qrButton.image.preserveAspect = true;
        qrButton.onClick.AddListener(() => Application.OpenURL(SurveyUrl));

        popup.instructionsLabel = AddLabel(CopyInstruction + SurveyInstructions,
            panel.transform, font, 0, -162, 866, 118, 27);

        var cancelButton = AddButton("Cancel", panel.transform, textBox, -160, -303, 280, 88);
        popup.cancelButton = cancelButton;
        AddLabel("GO TO MENU", cancelButton.transform, font, 0, 0, 224, 50, 29);
        cancelButton.onClick.AddListener(() => cancel());
        var confirmButton = AddButton("Confirm", panel.transform, textBox, 160, -303, 280, 88);
        AddLabel("QUIT", confirmButton.transform, font, 0, 0, 224, 50, 29);
        confirmButton.onClick.AddListener(() => confirm());

        SetNavigation(codeButton, confirmButton, qrButton, null, null);
        SetNavigation(qrButton, codeButton, cancelButton, null, null);
        SetNavigation(cancelButton, qrButton, null, null, confirmButton);
        SetNavigation(confirmButton, qrButton, codeButton, cancelButton, null);

        return popup;
    }

    public void Show(string code)
    {
        playCode = code;
        codeLabel.text = playCode;
        instructionsLabel.text = CopyInstruction + SurveyInstructions;
        gameObject.SetActive(true);
        EventSystem.current?.SetSelectedGameObject(cancelButton.gameObject);
    }

    private static void SetNavigation(Button button, Button up, Button down, Button left, Button right)
    {
        button.navigation = new Navigation
        {
            mode = Navigation.Mode.Explicit,
            selectOnUp = up,
            selectOnDown = down,
            selectOnLeft = left,
            selectOnRight = right
        };
    }

    private void CopyCode()
    {
        if (string.IsNullOrEmpty(playCode))
            return;

        GUIUtility.systemCopyBuffer = playCode;
        instructionsLabel.text = CopyInstruction + " (복사 완료)" + SurveyInstructions;
    }

    private static T AddElement<T>(string name, Transform parent, float x, float y,
        float width, float height) where T : Component
    {
        var item = new GameObject(name, typeof(RectTransform));
        var rect = (RectTransform)item.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = new Vector2(x, y);
        rect.sizeDelta = new Vector2(width, height);
        return item.AddComponent<T>();
    }

    private static TMP_Text AddLabel(string text, Transform parent, TMP_FontAsset font,
        float x, float y, float width, float height, float fontSize)
    {
        var label = AddElement<TextMeshProUGUI>("Label", parent, x, y, width, height);
        label.font = font;
        label.text = text;
        label.alignment = TextAlignmentOptions.Center;
        label.fontSize = fontSize;
        label.enableAutoSizing = true;
        label.fontSizeMin = fontSize * 0.7f;
        label.fontSizeMax = fontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.raycastTarget = false;
        return label;
    }

    private static Button AddButton(string name, Transform parent, Sprite sprite,
        float x, float y, float width, float height)
    {
        var image = AddElement<Image>(name, parent, x, y, width, height);
        image.sprite = sprite;
        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        return button;
    }
}
