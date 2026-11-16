using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class OptionPanel : MonoBehaviour
{
    [SerializeField] private GameObject soundWindow;
    [SerializeField] private GameObject keysetWindow;
    [SerializeField] private GameObject languageWindow;
    [SerializeField] private Button soundTab;
    [SerializeField] private Button keysetTab;
    [SerializeField] private Button languageTab;
    [SerializeField] private Button[] closeButtons;

    public void Bind(UnityAction close)
    {
        soundTab.onClick.AddListener(() => Select(soundWindow));
        keysetTab.onClick.AddListener(() => Select(keysetWindow));
        languageTab.onClick.AddListener(() => Select(languageWindow));

        foreach (Button button in closeButtons)
            button.onClick.AddListener(close);
    }

    private void OnEnable()
    {
        Show(soundWindow);
    }

    private void Select(GameObject window)
    {
        UISoundManager.Instance.PlayClick();
        Show(window);
    }

    private void Show(GameObject window)
    {
        soundWindow.SetActive(window == soundWindow);
        keysetWindow.SetActive(window == keysetWindow);
        languageWindow.SetActive(window == languageWindow);
    }
}
