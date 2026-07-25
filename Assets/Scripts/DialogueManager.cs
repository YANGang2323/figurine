using UnityEngine;
using UnityEngine.UI;
using System.Collections;
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
    public PlayerInteraction playerInteraction;
    public GameObject panel;

    public TMP_Text speakerText;
    public TMP_Text dialogueText;

    public Image backgroundImage;
    public Image leftPortraitImage;
    public Image rightPortraitImage;

    [Header("DetailImage")]
    public GameObject detailPanel;
    public Image detailImage;

    public Button leftButton;
    public Button rightButton;

    private Sprite[] detailSprites = new Sprite[3];
    private int currentDetailIndex;
    private int currentDetailCount;

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

    // ===== 타이핑 =====
    [Header("Typing")]
    public float typingSpeed = 0.03f;

    private DialogueLine currentLine;
    private Coroutine typingCoroutine;
    private bool isTyping;

    // ===== Dialogue 효과음 =====
    [Header("Audio")]
    public AudioClip nextClip;
    public AudioClip choiceSelectClip;
    //스페이스바 효과음
    public AudioSource DialogueAudioSource;
    
    [SerializeField] private AudioSource bgmSource; //브금
    [SerializeField] private float bgmFadeTime = 1f;
    private Coroutine bgmCoroutine;
    [SerializeField] private AudioSource sfxSource; //효과음



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
            // 타이핑 중이면 효과음 없이 즉시 출력
            if (isTyping)
            {
                FinishTyping();
                return;
            }

            // 선택지 대기
            if (waitingForChoice)
            {
                waitingForChoice = false;
                ShowChoiceUI(pendingChoices);
                return;
            }

            // 다음 대사로 넘어갈 때만 효과음
            if (nextClip != null)
                DialogueAudioSource.PlayOneShot(nextClip);

            ShowNextLine();
        }
    }

    public void StartDialogue(DialogueData dialogue)
    {
        InputLockManager.DialogueLock = true; // LockMove, LockInteraction, LockCamera 잠금

        playerInteraction.HideInteractionUI();
        currentDialogue = dialogue;

        // 한번만 재생되는 대화는 시작하는 순간 기록
        if (dialogue.playOnlyOnce &&
            !DataManager.Instance.nowGame.playedDialogues.Contains(dialogue.dialogueID))
        {
            DataManager.Instance.nowGame.playedDialogues.Add(dialogue.dialogueID);
        }

        panel.SetActive(true);

        lines.Clear();

        foreach (var line in dialogue.lines)
        {
            lines.Enqueue(line);
        }

        isPlaying = true;

        ShowNextLine();
    }
    //타이핑 코루틴
    IEnumerator TypeText(string text)
    {
        isTyping = true;

        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }
    //브금
    public bool IsCutsceneBGMPlaying
    {
        get
        {
            return bgmSource != null && bgmSource.isPlaying;
        }
    }
    IEnumerator ChangeBGM(AudioClip newClip, float targetVolume, float fadeTime)
    {
        // 현재 브금 Fade Out
        if (bgmSource.isPlaying)
        {
            float start = bgmSource.volume;

            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                bgmSource.volume = Mathf.Lerp(start, 0f, t / fadeTime);
                yield return null;
            }
        }

        // 교체
        bgmSource.Stop();

        if (newClip == null)
            yield break;

        bgmSource.clip = newClip;
        bgmSource.volume = 0f;
        bgmSource.Play();

        // Fade In
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(0f, targetVolume, t / fadeTime);
            yield return null;
        }

        bgmSource.volume = targetVolume;
    }

    IEnumerator StopBGM(float fadeTime)
    {
        if (!bgmSource.isPlaying)
            yield break;

        float start = bgmSource.volume;

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmSource.volume = Mathf.Lerp(start, 0f, t / fadeTime);
            yield return null;
        }

        bgmSource.Stop();
        bgmSource.volume = start;
    }

    public void ShowNextLine()
    {
        if (lines.Count == 0)
        {
            EndDialogue();
            return;
        }

        currentLine = lines.Dequeue();
        DialogueLine line = currentLine;

        if (string.IsNullOrEmpty(line.speakerName))
        {
            speakerText.gameObject.SetActive(false);
        }
        else
        {
            speakerText.gameObject.SetActive(true);
            speakerText.text = line.speakerName;
        }

        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(line.text));

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
        // 디테일 이미지
        if (line.detailImage1 != null || line.detailImage2 != null || line.detailImage3 != null)
        {
            detailPanel.SetActive(true);
            detailSprites[0] = line.detailImage1;
            detailSprites[1] = line.detailImage2;
            detailSprites[2] = line.detailImage3;

            ShowDetail();
        }
        else
        {
            detailPanel.SetActive(false);
        }
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
            DialogueAudioSource.PlayOneShot(line.soundEffect, line.soundVolume);
        }
        // ===== 컷씬 BGM =====
        if (line.changeBGM)
        {
            if (bgmCoroutine != null)
                StopCoroutine(bgmCoroutine);

            bgmCoroutine =
                StartCoroutine(ChangeBGM(
                    line.bgm,
                    line.bgmVolume,
                    bgmFadeTime));
        }

        if (line.stopBGM)
        {
            if (bgmCoroutine != null)
                StopCoroutine(bgmCoroutine);

            bgmCoroutine =
                StartCoroutine(StopBGM(bgmFadeTime));
        }

        // ===== 대사 효과음 =====
        if (line.soundEffect != null)
        {
            sfxSource.PlayOneShot(line.soundEffect, line.soundVolume);
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
        }
        if (bgmSource.isPlaying)
        {
            if (bgmCoroutine != null)
                StopCoroutine(bgmCoroutine);

            bgmCoroutine =
                StartCoroutine(StopBGM(bgmFadeTime));
        }
        backgroundImage.gameObject.SetActive(false);
        leftPortraitImage.gameObject.SetActive(false);
        rightPortraitImage.gameObject.SetActive(false);
        detailPanel.SetActive(false);

        choicePanel.SetActive(false);
        panel.SetActive(false);

        isPlaying = false;
        InputLockManager.DialogueLock = false;   // LockMove, LockInteraction, LockCamera 해제
    }

    void ShowDetail()
    {
        currentDetailCount = 0;

        for (int i = 0; i < 3; i++)
        {
            if (detailSprites[i] != null)
            {
                currentDetailIndex = i;
                currentDetailCount++;
                break;
            }
        }

        if (currentDetailCount == 0)
        {
            detailPanel.SetActive(false);
            return;
        }

        detailPanel.SetActive(true);
        RefreshDetail();
    }

    void RefreshDetail()
    {
        detailImage.sprite = detailSprites[currentDetailIndex];

        bool hasLeft = false;
        bool hasRight = false;

        for (int i = currentDetailIndex - 1; i >= 0; i--)
        {
            if (detailSprites[i] != null)
            {
                hasLeft = true;
                break;
            }
        }

        for (int i = currentDetailIndex + 1; i < 3; i++)
        {
            if (detailSprites[i] != null)
            {
                hasRight = true;
                break;
            }
        }

        leftButton.gameObject.SetActive(hasLeft);
        rightButton.gameObject.SetActive(hasRight);
    }
    //디테일왼쪽버튼
    public void PrevDetail()
    {
        for (int i = currentDetailIndex - 1; i >= 0; i--)
        {
            if (detailSprites[i] != null)
            {
                currentDetailIndex = i;
                RefreshDetail();
                return;
            }
        }
    }
    //디테일오른쪽버튼
    public void NextDetail()
    {
        for (int i = currentDetailIndex + 1; i < 3; i++)
        {
            if (detailSprites[i] != null)
            {
                currentDetailIndex = i;
                RefreshDetail();
                return;
            }
        }
    }

    void FinishTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = currentLine.text;

        isTyping = false;
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
        if (choiceSelectClip != null)
            DialogueAudioSource.PlayOneShot(choiceSelectClip);

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
                foreach (string id in dialogueEvent.targetIDs)
                {
                    DialogueTarget target = DialogueTarget.Find(id);

                    if (target != null)
                        target.Enable();
                    else
                        Debug.LogWarning($"DialogueTarget을 찾을 수 없습니다. ID : {id}");
                }

                break;

            case DialogueEventType.DisableObject:

                foreach (string id in dialogueEvent.targetIDs)
                {
                    DialogueTarget target = DialogueTarget.Find(id);

                    if (target != null)
                        target.Disable();
                    else
                        Debug.LogWarning($"DialogueTarget을 찾을 수 없습니다. ID : {id}");
                }

                break;

            case DialogueEventType.LoadScene:

                DataManager.Instance.nowGame.sceneName =
                    dialogueEvent.sceneName;

                DataManager.Instance.nowGame.playerPosition =
                    dialogueEvent.spawnPosition;

                SceneManager.LoadScene(dialogueEvent.sceneName);

                break;

            case DialogueEventType.DoorUnlock:

                // 추후 DoorTarget 등 구현

                break;

            case DialogueEventType.StartPuzzle:

                foreach (string id in dialogueEvent.targetIDs)
                {
                    DialogueTarget target = DialogueTarget.Find(id);

                    if (target == null)
                        continue;

                    PerspectiveEmblemPuzzle puzzle =
                        target.GetComponent<PerspectiveEmblemPuzzle>();

                    if (puzzle != null)
                        puzzle.InteractPuzzle();
                }
                break;

            case DialogueEventType.PlayCutscene:

                // 추후 Timeline 연결

                break;

            case DialogueEventType.ActivateCCTV:
                foreach (string id in dialogueEvent.targetIDs)
                {
                    DialogueTarget target = DialogueTarget.Find(id);

                    if (target == null)
                        continue;

                    CCTVController cctv =
                        target.GetComponent<CCTVController>();

                    if (cctv != null)
                        cctv.Activate();
                }

                break;

            case DialogueEventType.CctvRespawn:
                StealthManager.Instance.Respawn();
                break;

            case DialogueEventType.TwoStageDoorFirst:
                {
                    foreach (string id in dialogueEvent.targetIDs)
                    {
                        DialogueTarget target = DialogueTarget.Find(id);
                        if (target != null)
                        {
                            NumberSelectDoor door = target.GetComponent<NumberSelectDoor>();

                            if (door != null)
                                door.FirstInteraction();
                        }
                    }
                }
                break;

        }
    }
}