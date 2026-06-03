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

    public Image portraitImage;

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

        if (line.portrait != null)
        {
            portraitImage.gameObject.SetActive(true);
            portraitImage.sprite = line.portrait;
        }
        else
        {
            portraitImage.gameObject.SetActive(false);
        }
    }

    void EndDialogue()
    {
        if (currentDialogue != null &&
           currentDialogue.playOnlyOnce)
        {
            if (!DataManager.Instance.nowGame.playedDialogues
                .Contains(currentDialogue.dialogueID))
            {
                DataManager.Instance.nowGame.playedDialogues
                    .Add(currentDialogue.dialogueID);
            }
        }

        panel.SetActive(false);
        isPlaying = false;
    }
}