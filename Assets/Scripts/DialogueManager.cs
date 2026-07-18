using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
[System.Serializable]
public class ChoiceRecord
{
    public ChoiceTag tag;
    public int value;
}
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public GameObject panel;

    public TMP_Text speakerText;
    public TMP_Text dialogueText;

    public Image backgroundImage;
    public Image leftPortraitImage;
    public Image rightPortraitImage;

    public List<ChoiceRecord> choiceHistory = new(); //선택지 기록
    /*나중에 사용시
    if (Authority >= 5)
        EndingA();

    else if (Doubt >= 5)
        EndingB();
     */
    public GameObject choicePanel;
    public Button[] choiceButtons;
    private DialogueChoice[] pendingChoices;
    private bool waitingForChoice = false;
    private DialogueEvent currentChoiceEvent;

    //스타일
    [Header("기본 텍스트 스타일")]
    public TMP_FontAsset defaultDialogueFont;
    public TMP_FontAsset defaultSpeakerFont;

    public float defaultDialogueFontSize = 36;
    public float defaultSpeakerFontSize = 42;

    public Color defaultDialogueColor = Color.white;
    public Color defaultSpeakerColor = Color.white;
    
    //효과음
    public AudioSource audioSource;

    private Queue<DialogueLine> lines
    = new Queue<DialogueLine>();
    private DialogueData currentDialogue;   //현재 대화 기억

    bool isPlaying;

    public bool IsPlaying => isPlaying;


    void Awake()
    {
        Debug.Log("DialogueManager Awake");
        Instance = this;
        panel.SetActive(false);
        choicePanel.SetActive(false);

        dialogueText.font = defaultDialogueFont;
        speakerText.font = defaultSpeakerFont;

        dialogueText.fontSize = defaultDialogueFontSize;
        speakerText.fontSize = defaultSpeakerFontSize;

        dialogueText.color = defaultDialogueColor;
        speakerText.color = defaultSpeakerColor;
    }

    void Update()
    {
        if (!isPlaying)
            return;

        if (choicePanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (waitingForChoice)
            {
                waitingForChoice = false;
                ShowChoiceUI(pendingChoices);
            }
            else
            {
                ShowNextLine();
            }
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;

        panel.SetActive(true);

        lines.Clear();

        foreach (var line in dialogue.lines)
        {
            lines.Enqueue(line);
        }

        isPlaying = true;

        ShowNextLine();
    }

    public void ShowNextLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        DialogueLine line = lines.Dequeue();

        if (string.IsNullOrEmpty(line.speakerName))
        {
            speakerText.gameObject.SetActive(false);
        }
        else
        {
            speakerText.gameObject.SetActive(true);
            speakerText.text = line.speakerName;
        }

        dialogueText.text = line.text;

        // 배경
        if (line.hideBackground)
        {
            backgroundImage.gameObject.SetActive(false);
        }
        else if (line.background != null)
        {
            backgroundImage.gameObject.SetActive(true);
            backgroundImage.sprite = line.background;
        }
        // background == null이면 아무것도 안 함(이전 배경 유지)

        // 왼쪽 초상화
        if (line.hideLeftPortrait)
        {
            leftPortraitImage.gameObject.SetActive(false);
        }
        else if (line.leftPortrait != null)
        {
            leftPortraitImage.gameObject.SetActive(true);
            leftPortraitImage.sprite = line.leftPortrait;
        }
        //null이면 아무것도 안 함(이전 유지)

        // 오른쪽 초상화
        if (line.hideRightPortrait)
        {
            rightPortraitImage.gameObject.SetActive(false);
        }
        else if (line.rightPortrait != null)
        {
            rightPortraitImage.gameObject.SetActive(true);
            rightPortraitImage.sprite = line.rightPortrait;
        }
        //null이면 아무것도 안 함(이전 유지)

        //텍스트 스타일
        // Dialogue Font
        dialogueText.font =
            (line.useCustomDialogueFont && line.dialogueFont != null)
            ? line.dialogueFont
            : defaultDialogueFont;

        // Speaker Font
        speakerText.font =
            (line.useCustomSpeakerFont && line.speakerFont != null)
            ? line.speakerFont
            : defaultSpeakerFont;
        //글자색
        dialogueText.color =
     line.useCustomDialogueColor
     ? line.dialogueColor
     : defaultDialogueColor;

        speakerText.color =
            line.useCustomSpeakerColor
            ? line.speakerColor
            : defaultSpeakerColor;

        //크기
        dialogueText.fontSize =line.useCustomDialogueSize
    ? line.dialogueFontSize
    : defaultDialogueFontSize;

        speakerText.fontSize =
            line.useCustomSpeakerSize
            ? line.speakerFontSize
            : defaultSpeakerFontSize;

        //사운드
        if (line.soundEffect != null)
        {
            audioSource.PlayOneShot(line.soundEffect, line.soundVolume);
        }

        //선택지 저장만
        if (line.hasChoice)
        {
            pendingChoices = line.choices;
            waitingForChoice = true;
        }

    }

    void EndDialogue()
    {
        if (currentChoiceEvent != null)
        {
            ExecuteDialogueEvent(currentChoiceEvent);
            currentChoiceEvent = null;
        }
        if (currentDialogue != null)
        {
            ExecuteDialogueEvent(currentDialogue.endEvent);

            if (!DataManager.Instance.nowGame.playedDialogues
                .Contains(currentDialogue.dialogueID))
            {
                DataManager.Instance.nowGame.playedDialogues
                    .Add(currentDialogue.dialogueID);
            }
        }
        backgroundImage.gameObject.SetActive(false);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);

        choicePanel.SetActive(false);
        panel.SetActive(false);

        isPlaying = false;
    }

    void ShowChoiceUI(DialogueChoice[] choices)
    {
        choicePanel.SetActive(true);

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);

                choiceButtons[i]
                    .GetComponentInChildren<TMP_Text>().text
                    = choices[i].text;

                choiceButtons[i].onClick.RemoveAllListeners();

                DialogueChoice choice = choices[i];

                choiceButtons[i].onClick.RemoveAllListeners();

                choiceButtons[i].onClick.AddListener(() =>
                {
                    SelectChoice(choice);
                });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }
    void SelectChoice(DialogueChoice choice)
    {
        choicePanel.SetActive(false);

        pendingChoices = null;
        waitingForChoice = false;

        choiceHistory.Add(new ChoiceRecord()
        {
            tag = choice.tag,
            value = choice.value
        });

        currentChoiceEvent = choice.eventData;

        if (choice.nextDialogue != null)
        {
            StartDialogue(choice.nextDialogue);
        }
        else
        {
            EndDialogue();
        }
    }
    void ExecuteDialogueEvent(DialogueEvent dialogueEvent)
    {
        if (dialogueEvent == null)
            return;

        switch (dialogueEvent.type)
        {
            case DialogueEventType.None:
                break;

            case DialogueEventType.EnableObject:
                if (dialogueEvent.target != null)
                    dialogueEvent.target.SetActive(true);
                break;

            case DialogueEventType.DisableObject:
                if (dialogueEvent.target != null)
                    dialogueEvent.target.SetActive(false);
                break;

            case DialogueEventType.LoadScene:

                DataManager.Instance.nowGame.sceneName =
                    dialogueEvent.sceneName;

                DataManager.Instance.nowGame.playerPosition =
                    dialogueEvent.spawnPosition;

                SceneManager.LoadScene(dialogueEvent.sceneName);

                break;

            case DialogueEventType.DoorUnlock:
                // TODO : Door 스크립트 구현 후 추가
                /*if (dialogueEvent.target != null)
                {
                    Door door =
                        dialogueEvent.target.GetComponent<Door>();

                    if (door != null)
                        door.Unlock();
                }*/

                break;

            case DialogueEventType.StartPuzzle:

                if (dialogueEvent.target != null)
                {
                    PerspectiveEmblemPuzzle puzzle =
                        dialogueEvent.target.GetComponent<PerspectiveEmblemPuzzle>();

                    if (puzzle != null)
                        puzzle.InteractPuzzle();
                }

                break;

            case DialogueEventType.PlayCutscene:

                // 추후 Timeline이나 컷씬 매니저 연결

                break;
        }
    }
}