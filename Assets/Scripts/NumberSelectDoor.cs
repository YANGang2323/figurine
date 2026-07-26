using UnityEngine;
using TMPro;
public class NumberSelectDoor : MonoBehaviour
{
    private int[] currentNumbers;

    [Header("UI")]
    [SerializeField] private GameObject puzzleUI;

    [Header("Numbers")]
    [SerializeField] private TMP_Text[] numberTexts;   // 4개, 나중에 6개도 가능
    [SerializeField] private int[] answerNumbers;      // 정답

    [Header("Unlock Objects (최초 상호작용 이후 활성)")]
    [SerializeField] private InteractableObject[] unlockObjects;
    [SerializeField] private GameObject[] activateObjects;

    [Header("Objects to disable (최초 상호작용 이후 비활성)")]
    [SerializeField] private GameObject[] deactivateObjects;


    private bool firstInteract = false;
    private bool solved = false;

    [Header("Dialogue")]
    [SerializeField] private DialogueData successDialogue;
    [SerializeField] private DialogueData failDialogue;
    [SerializeField] private DialogueData newRepeatDialogue;
    [SerializeField] private DialogueTrigger doorDialogue;

    void Awake()
    {
        currentNumbers = new int[numberTexts.Length];
        RefreshAll();
    }
    public void FirstInteraction() 
    {
        if (firstInteract)
            return;

        firstInteract = true;

        foreach (InteractableObject obj in unlockObjects)
        {
            if (obj != null)
                obj.canInteract = true;
        }
        // CCTV, SpotLight, Trigger 등 원하는 오브젝트 활성화
        foreach (GameObject obj in activateObjects)
        {
            if (obj != null)
                obj.SetActive(true);
        }
        foreach (GameObject obj in deactivateObjects)
        {
            if (obj != null)
                obj.SetActive(false);
        }
    }

    public void OpenPuzzle()
    {
        if (solved)
            return;
        puzzleUI.SetActive(true);
    }

    public void ClosePuzzle()
    {
        puzzleUI.SetActive(false);
    }

    public void Increase(int index)
    {
        currentNumbers[index] = (currentNumbers[index] + 1) % 10;
        Refresh(index);
    }

    public void Decrease(int index)
    {
        currentNumbers[index] = (currentNumbers[index] + 9) % 10;
        Refresh(index);
    }

    void Refresh(int index)
    {
        numberTexts[index].text = currentNumbers[index].ToString();
    }

    void RefreshAll()
    {
        for (int i = 0; i < numberTexts.Length; i++)
            Refresh(i);
    }
    public void Confirm()
    {
        if (currentNumbers.Length != answerNumbers.Length)
            return;

        for (int i = 0; i < currentNumbers.Length; i++)
        {
            if (currentNumbers[i] != answerNumbers[i])
            {
                Fail();
                return;
            }
        }

        Success();
    }

    void Success()
    {
        solved = true;

        puzzleUI.SetActive(false);

        doorDialogue.repeatDialogue = newRepeatDialogue;

        DialogueManager.Instance.StartDialogue(successDialogue);
    }

    void Fail()
    {
        puzzleUI.SetActive(false);
        DialogueManager.Instance.StartDialogue(failDialogue);
    }
}
