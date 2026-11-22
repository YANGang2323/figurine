using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageSavePrompt : MonoBehaviour
{
    [SerializeField] private DialogueManager dialogueManager;
    [Tooltip("Uses the same background, portrait, font, color, detail image and audio options as a dialogue line.")]
    [SerializeField] private DialogueLine presentation = new DialogueLine
    {
        hideBackground = true,
        hideLeftPortrait = true,
        hideRightPortrait = true
    };
    [SerializeField] private string saveChoiceText = "저장하고 다음 스테이지로 이동";
    [SerializeField] private string continueChoiceText = "저장하지 않고 다음 스테이지로 이동";
    private string destination;
    private Action beforeLoad;
    private Action onContinue;
    private bool continuing;

    public void Show(string sceneName, Action prepare, Action complete)
    {
        destination = sceneName;
        beforeLoad = prepare;
        onContinue = complete;
        continuing = false;
        gameObject.SetActive(true);
        StartCoroutine(ShowAfterDialogueEnds());
    }

    private IEnumerator ShowAfterDialogueEnds()
    {
        // LoadScene events can run inside EndDialogue. Wait for its UI cleanup first.
        yield return null;
        DisplayChoices();
    }

    private void DisplayChoices()
    {
        gameObject.SetActive(true);
        dialogueManager.ShowPromptChoices(presentation,
            new[] { saveChoiceText, continueChoiceText }, index =>
            {
                if (index == 0) Save();
                else Continue();
            });
    }

    private void Save()
    {
        Game game = FindFirstObjectByType<Game>();
        if (game == null || game.SaveUI == null)
        {
            Debug.LogWarning("저장 창을 열 수 없습니다.", this);
            DisplayChoices();
            return;
        }
        dialogueManager.HidePromptChoices();
        gameObject.SetActive(false);
        game.OpenTransitionSave(Continue, DisplayChoices);
    }

    private void Continue()
    {
        if (continuing) return;
        continuing = true;
        dialogueManager.HidePromptChoices();
        gameObject.SetActive(false);
        try { beforeLoad?.Invoke(); }
        catch (Exception error) { Debug.LogException(error); }
        onContinue?.Invoke();
        InputLockManager.TransitionLock = false;
        SceneManager.LoadScene(destination);
    }
}
