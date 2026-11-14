using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StageChanger : MonoBehaviour
{
    [SerializeField] private string nextSceneName;
    private bool isTransitioning;
    private static bool promptOpen;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isTransitioning && !promptOpen)
            StartCoroutine(TransitionAfterDelay());
    }

    private IEnumerator TransitionAfterDelay()
    {
        isTransitioning = true;
        yield return new WaitForSeconds(1f);
        RequestTransition(nextSceneName);
        isTransitioning = false;
    }

    public static void RequestTransition(string destination, Action beforeLoad = null)
    {
        if (promptOpen)
            return;

        if (!Application.CanStreamedLevelBeLoaded(destination))
        {
            Debug.LogWarning($"Cannot load stage {destination}.");
            return;
        }

        promptOpen = true;
        InputLockManager.TransitionLock = true;
        InputLockManager.Refresh();
        var canvas = new GameObject("Stage Save Prompt", typeof(Canvas), typeof(CanvasScaler),
            typeof(GraphicRaycaster));
        canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.GetComponent<Canvas>().sortingOrder = 400;
        var scaler = canvas.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var shade = new GameObject("Shade", typeof(Image)).GetComponent<Image>();
        shade.transform.SetParent(canvas.transform, false);
        shade.rectTransform.anchorMin = Vector2.zero;
        shade.rectTransform.anchorMax = Vector2.one;
        shade.rectTransform.offsetMin = shade.rectTransform.offsetMax = Vector2.zero;
        shade.color = new Color(0f, 0f, 0f, 0.88f);
        TMP_Text title = AddLabel(shade.transform, "다음 스테이지로 넘어가기 전에 저장하시겠습니까?",
            new Vector2(0f, 90f), new Vector2(1050f, 90f));

        var actions = new GameObject("Actions", typeof(RectTransform));
        actions.transform.SetParent(shade.transform, false);

        void Continue()
        {
            if (!promptOpen)
                return;

            canvas.SetActive(false);
            try { beforeLoad?.Invoke(); }
            catch (Exception error) { Debug.LogException(error); }

            promptOpen = false;
            InputLockManager.TransitionLock = false;
            Destroy(canvas);
            SceneManager.LoadScene(destination);
        }

        AddButton(actions.transform, "저장", new Vector2(-155f, -40f), () =>
        {
            Game game = FindFirstObjectByType<Game>();
            if (game == null || game.SaveUI == null)
            {
                title.text = "저장 창을 열 수 없습니다.";
                return;
            }

            canvas.SetActive(false);
            game.OpenTransitionSave(Continue, () => canvas.SetActive(true));
        });
        AddButton(actions.transform, "저장 없이 계속", new Vector2(155f, -40f), Continue);
    }

    internal static TMP_Text AddLabel(Transform parent, string text, Vector2 position, Vector2 size)
    {
        var label = new GameObject("Label", typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        label.transform.SetParent(parent, false);
        label.rectTransform.anchoredPosition = position;
        label.rectTransform.sizeDelta = size;
        if (DialogueManager.Instance != null && DialogueManager.Instance.defaultDialogueFont != null)
            label.font = DialogueManager.Instance.defaultDialogueFont;
        label.text = text;
        label.fontSize = 34f;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        return label;
    }

    private static void AddButton(Transform parent, string text, Vector2 position, UnityEngine.Events.UnityAction action)
    {
        var button = new GameObject(text, typeof(Image), typeof(Button)).GetComponent<Button>();
        var rect = (RectTransform)button.transform;
        rect.SetParent(parent, false);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(240f, 70f);
        button.image.color = new Color(0.24f, 0.19f, 0.16f);
        AddLabel(rect, text, Vector2.zero, rect.sizeDelta);
        button.onClick.AddListener(action);
    }
}
