using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using Unity.Services.Analytics;

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

    private float currentBGMLineVolume = 1f;
    private float bgmFadeMultiplier = 1f;



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
        // 실시간 볼륨 적용
        RefreshAudioVolume();
        // ESC 메뉴가 열려있으면 대화 입력 무시
        if (InputLockManager.ESCMenuLock)
            return;

        //대화 입력 처리
        if (!isPlaying)
            return;

        if (choicePanel.activeSelf)
            return;

        if (Input.GetKeyDown(KeyCode.Space))
        {
            // 타이핑 중이면 효과음 없이 즉시 출력
            if (isTyping)
            {
                var game = DataManager.instance.nowGame;
                game.dialogueSkipCount++;
                FinishTyping();
                var eventData = new Dictionary<string, object> {
                    { "PlayCode", game.playCode },
                    { "DialogueID", currentDialogue.dialogueID }, // 현재 출력 중인 대화 데이터의 고유 ID
                    { "TotalSkips", game.dialogueSkipCount }
                };
                CustomEvent myEvent = new CustomEvent("dialogue_skip");
                foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
                AnalyticsService.Instance.RecordEvent(myEvent);
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
            if (nextClip != null){
                DialogueAudioSource.PlayOneShot(nextClip);
            }

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
    //볼륨 불러오기
    float GetMasterVolume()
    {
        if (AudioSettingsManager.Instance == null)
            return 1f;

        return AudioSettingsManager.Instance.masterVolume;
    }

    float GetSFXVolume()
    {
        if (AudioSettingsManager.Instance == null)
            return 1f;

        return AudioSettingsManager.Instance.sfxVolume;
    }

    float GetBGMVolume()
    {
        if (AudioSettingsManager.Instance == null)
            return 1f;

        return AudioSettingsManager.Instance.bgmVolume;
    }

    //볼륨 적용
    void RefreshAudioVolume()
    {
        float master = GetMasterVolume();
        float sfx = GetSFXVolume();
        float bgm = GetBGMVolume();

        if (bgmSource != null)
            bgmSource.volume = currentBGMLineVolume * master * bgm * bgmFadeMultiplier;

        if (sfxSource != null)
            sfxSource.volume = master * sfx;

        if (DialogueAudioSource != null)
            DialogueAudioSource.volume = master * sfx;
    }

    //브금
    public bool IsCutsceneBGMPlaying
    {
        get
        {
            return bgmSource != null && bgmSource.isPlaying;
        }
    }
    
    IEnumerator ChangeBGM(AudioClip newClip, float targetVolume, float fadeTime) {
        // 현재 브금 볼륨을 기억하는 변수
        currentBGMLineVolume = targetVolume;
        // 현재 브금 Fade Out
        if (bgmSource.isPlaying)
        {
            for (float t = 0; t < fadeTime; t += Time.deltaTime)
            {
                bgmFadeMultiplier = Mathf.Lerp(1f, 0f, t / fadeTime);
                RefreshAudioVolume();
                yield return null;
            }
        }

        // 교체
        bgmSource.Stop();
        if (newClip == null)
        {
            bgmFadeMultiplier = 1f;
            RefreshAudioVolume();
            yield break;
        }

        bgmSource.clip = newClip;
        bgmSource.Play();

        // Fade In
        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmFadeMultiplier = Mathf.Lerp(0f, 1f, t / fadeTime);
            RefreshAudioVolume();
            yield return null;
        }

        bgmFadeMultiplier = 1f;
        RefreshAudioVolume();
    }

    IEnumerator StopBGM(float fadeTime)
    {
        if (!bgmSource.isPlaying)
            yield break;

        for (float t = 0; t < fadeTime; t += Time.deltaTime)
        {
            bgmFadeMultiplier = Mathf.Lerp(1f, 0f, t / fadeTime);
            RefreshAudioVolume();
            yield return null;
        }

        bgmSource.Stop();
        bgmFadeMultiplier = 1f;
        RefreshAudioVolume();
    }

    public void ShowNextLine()
    {
        if (lines.Count == 0){
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
        // ===== 컷씬 BGM =====
        if (line.changeBGM)
        {
            if (bgmCoroutine != null)
                StopCoroutine(bgmCoroutine);

            bgmCoroutine = StartCoroutine(ChangeBGM(line.bgm, line.bgmVolume, bgmFadeTime));
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
    //디테일샷 왼쪽버튼
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
    //디테일샷 오른쪽버튼
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
        //선택지 효과음
        if (choiceSelectClip != null){
            DialogueAudioSource.PlayOneShot(choiceSelectClip);
        }
            // ----------------------------------------------------------------
            // [애널리틱스 수집용 코드 추가] 선택지를 누른 순간 데이터 수집
            // ----------------------------------------------------------------
            if (DataManager.instance != null && DataManager.instance.nowGame != null)
        {
            // 1. 혹시 매개변수(choice) 객체 자체가 널일 경우를 완벽하게 차단
            string playCode = DataManager.instance.nowGame.playCode;
            string tagString = "Null_Tag";
            string valueString = "Null_Value";
            bool hasNext = false;

            if (choice != null)
            {
                // 2. ChoiceTag가 enum이거나 null일 수 있으므로 안전하게 ToString()으로 형변환 처리합니다.
                tagString = choice.tag != null ? choice.tag.ToString() : "Empty_Tag";
                valueString = choice.value != null ? choice.value.ToString() : "Empty_Value";
                hasNext = choice.nextDialogue != null;
            }
            // 1. 수집할 데이터 상자(Dictionary) 만들기
            var eventData = new Dictionary<string, object> {
            { "PlayCode", playCode  },
            { "ChoiceTag", tagString  },                   // 선택지의 고유 태그 정보
            { "ChoiceValue", valueString  },               // 선택지의 값 (데이터 내용)
            { "HasNextDialogue", hasNext  } // 다음 대화 연결 여부
        };

            try
            {
                Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("dialogue_choice");
                foreach (var param in eventData)
                {
                    // 파라미터가 null 인 데이터가 있어도 무시하거나 안전하게 전송하기 위한 방어벽
                    if (param.Value == null)
                    {
                        myEvent.Add(param.Key, "Null_Param");
                    }
                    else
                    {
                        myEvent.Add(param.Key, param.Value);
                    }
                }
                Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(myEvent);
                Debug.Log($"[Analytics] 선택지 로그 정상 기록 완료 - Tag: {tagString}, Value: {valueString}");
            }
            catch (System.Exception ex)
            {
                // 애널리틱스가 설령 실패하더라도 인게임 시스템은 절대 튕기지 않도록 로그만 띄웁니다.
                Debug.LogWarning($"[Analytics] 데이터 전송이 예외 처리되었습니다: {ex.Message}");
            }
        }
        // ----------------------------------------------------------------
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
                //로드씬 함수를 실행하기 직전에 애널리틱스 호출 (가려는 씬 이름을 던져줌)
                DataManager.Instance.StageClear(dialogueEvent.sceneName);

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

            case DialogueEventType.EndingUI:
                DataManager.Instance.StageClear("STAGE3");
                DataManager.Instance.EndingClear();
                StartCoroutine(
                    CenterMessageUI.Instance.EndingRoutine(
                        DataManager.Instance.nowGame.playCode));
                break;
        }
    }
}