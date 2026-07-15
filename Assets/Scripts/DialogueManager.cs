using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    public GameObject panel;

    public TMP_Text speakerText;
    public TMP_Text dialogueText;

    public Image backgroundImage;
    public Image leftPortraitImage;
    public Image rightPortraitImage;

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
    }

    void Update()
    {
        if (!isPlaying)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            ShowNextLine();
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

        //speakerText.text = line.speakerName;
        dialogueText.text = line.text;
        //portraitImage.sprite = line.portrait;

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
    }

    void EndDialogue()
    {
        if (currentDialogue != null)
        {
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

        panel.SetActive(false);
        isPlaying = false;
    }
}