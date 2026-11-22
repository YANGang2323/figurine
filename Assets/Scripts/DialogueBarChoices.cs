using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class DialogueBarChoices : MonoBehaviour
{
    [SerializeField] private Button[] buttons;
    [SerializeField] private TMP_Text[] labels;
    [SerializeField] private GameObject[] hiddenWhileChoosing;
    [SerializeField] private string prefix = "> ";
    private bool[] previousVisibility;
    private Action<int> selected;
    private bool open;
    private int openedFrame;

    public Button[] Buttons => buttons;
    public bool IsOpen => open;

    public void Show(string[] options, TMP_FontAsset font, float fontSize, Color color, Action<int> onSelected)
    {
        if (options == null || options.Length == 0 || options.Length > buttons.Length)
            throw new ArgumentException("Author enough choice rows in the Dialog bar prefab for every option.");

        Hide();
        previousVisibility = new bool[hiddenWhileChoosing.Length];
        for (int i = 0; i < hiddenWhileChoosing.Length; i++)
        {
            previousVisibility[i] = hiddenWhileChoosing[i].activeSelf;
            hiddenWhileChoosing[i].SetActive(false);
        }

        selected = onSelected;
        open = true;
        openedFrame = Time.frameCount;
        for (int i = 0; i < buttons.Length; i++)
        {
            bool visible = i < options.Length;
            buttons[i].gameObject.SetActive(visible);
            buttons[i].onClick.RemoveAllListeners();
            if (!visible) continue;

            labels[i].text = prefix + options[i];
            labels[i].font = font;
            labels[i].fontSize = fontSize;
            labels[i].fontSizeMax = fontSize;
            labels[i].fontSizeMin = Mathf.Min(26f, fontSize);
            labels[i].color = color;
            // Size the hit area to this option's text, not the full dialogue bar.
            float maxWidth = ((RectTransform)transform).rect.width;
            Vector2 preferred = labels[i].GetPreferredValues(labels[i].text, maxWidth, Mathf.Infinity);
            var row = buttons[i].GetComponent<LayoutElement>();
            row.preferredWidth = Mathf.Min(maxWidth, Mathf.Ceil(preferred.x) + 8f);
            row.preferredHeight = Mathf.Max(fontSize * 1.35f, Mathf.Ceil(preferred.y));
            row.flexibleWidth = row.flexibleHeight = 0f;
            int index = i;
            buttons[i].onClick.AddListener(() => Choose(index));
            buttons[i].navigation = new Navigation
            {
                mode = Navigation.Mode.Explicit,
                selectOnUp = buttons[(i + options.Length - 1) % options.Length],
                selectOnDown = buttons[(i + 1) % options.Length]
            };
        }
        gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
        EventSystem.current?.SetSelectedGameObject(null);
        foreach (var label in labels)
            label.CrossFadeColor(Color.white, 0f, true, true);
    }

    private void Update()
    {
        // Keyboard focus starts only after an intentional arrow-key press.
        if (!open || InputLockManager.ESCMenuLock || EventSystem.current == null ||
            EventSystem.current.currentSelectedGameObject != null) return;
        if (Input.GetKeyDown(KeyCode.DownArrow))
            EventSystem.current.SetSelectedGameObject(buttons[0].gameObject);
        else if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            for (int i = buttons.Length - 1; i >= 0; i--)
                if (buttons[i].gameObject.activeSelf)
                {
                    EventSystem.current.SetSelectedGameObject(buttons[i].gameObject);
                    break;
                }
        }
    }

    private void Choose(int index)
    {
        if (!open || Time.frameCount == openedFrame || InputLockManager.ESCMenuLock) return;
        Action<int> callback = selected;
        Hide();
        callback?.Invoke(index);
    }

    public void Hide()
    {
        if (open && previousVisibility != null)
        {
            for (int i = 0; i < hiddenWhileChoosing.Length; i++)
                hiddenWhileChoosing[i].SetActive(previousVisibility[i]);
        }
        if (EventSystem.current != null && EventSystem.current.currentSelectedGameObject != null &&
            EventSystem.current.currentSelectedGameObject.transform.IsChildOf(transform))
            EventSystem.current.SetSelectedGameObject(null);
        selected = null;
        open = false;
        gameObject.SetActive(false);
    }
}
