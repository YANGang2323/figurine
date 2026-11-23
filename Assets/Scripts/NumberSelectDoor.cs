using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Unity.Services.Analytics;

public class NumberSelectDoor : MonoBehaviour
{
    private int[] currentNumbers;
    private string submittedAnswer = string.Empty;
    public string CurrentInput => currentNumbers == null ? string.Empty : string.Join("", currentNumbers);

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
    //Progress 플래그
    [SerializeField] private string firstInteractFlag;
    [SerializeField] private Stage2CCTVLighting cctvLightingOnSuccess;

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

    void Start()
    {
        // ProgressManager와 세이브 데이터에 플래그가 들어있는지 확인
        if (!string.IsNullOrEmpty(firstInteractFlag) && ProgressManager.Instance != null)
        {
            if (ProgressManager.Instance.HasFlag(firstInteractFlag))
            {
                // 이미 플래그가 있다면, 대화나 이벤트를 스킵하고 오브젝트들의 상태만 복구합니다.
                ApplyFirstInteractionWorldState();
            }
        }
    }

    public void FirstInteraction() 
    {
        if (firstInteract)
            return;

        firstInteract = true;

        ApplyFirstInteractionWorldState();

        // 시간 측정 시작점 기록 (유니티 실행 시간 사용)
        DataManager.instance.nowGame.numSelectStartTime = Time.time;

        if (!string.IsNullOrEmpty(firstInteractFlag))
        {
            if (ProgressManager.Instance != null)
            {
                ProgressManager.Instance.AddFlag(firstInteractFlag);
                Debug.Log($"[NumberSelectDoor] 최초 상호작용 플래그 등록 완료: {firstInteractFlag}");
            }
            else
            {
                Debug.LogError("🚨 씬에 ProgressManager 인스턴스가 존재하지 않습니다!");
            }
        }
    }

    private void ApplyFirstInteractionWorldState()
    {
        firstInteract = true;

        foreach (InteractableObject obj in unlockObjects)
        {
            if (obj != null)
                obj.canInteract = true;
        }

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

        Debug.Log($"상호작용 후 변화 상태 완료");
    }

    public void OpenPuzzle()
    {
        if (solved)
            return;
        puzzleUI.SetActive(true);
        TrackExitPuzzle("opened");
    }

    public void ClosePuzzle()
    {
        TrackExitPuzzle("closed");
        puzzleUI.SetActive(false);
    }

    public void Increase(int index)
    {
        currentNumbers[index] = (currentNumbers[index] + 1) % 10;
        Refresh(index);
        TrackExitPuzzle("editing");
    }

    public void Decrease(int index)
    {
        currentNumbers[index] = (currentNumbers[index] + 9) % 10;
        Refresh(index);
        TrackExitPuzzle("editing");
    }

    private void TrackExitPuzzle(string step, bool attempt = false)
    {
        PlayExitAnalytics.Puzzle(this, "number_lock", step, solved ? 1 : 0, 1,
            attempt ? submittedAnswer : CurrentInput, attempt);
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

        submittedAnswer = CurrentInput;
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
        TrackExitPuzzle("completed", true);
        if (cctvLightingOnSuccess != null)
            cctvLightingOnSuccess.Complete();

        puzzleUI.SetActive(false);

        // 해결 시간 계산 (현재 유니티 시간 - 최초 상호작용 시간)
        float solveTime = Time.time - DataManager.instance.nowGame.numSelectStartTime;

        var eventData = new Dictionary<string, object> {
            { "PlayCode", DataManager.instance.nowGame.playCode },
            { "DoorID", gameObject.name },
            { "SubmittedAnswer", submittedAnswer },
            { "SolveTimeSeconds", solveTime },
            { "TotalFailCount", DataManager.instance.nowGame.numSelectFailCount }
        };
        CustomEvent successEvent = new CustomEvent("number_puzzle_success");
        foreach (var param in eventData) successEvent.Add(param.Key, param.Value);
        AnalyticsService.Instance.RecordEvent(successEvent);

        doorDialogue.repeatDialogue = newRepeatDialogue;

        DialogueManager.Instance.StartDialogue(successDialogue);
    }

    void Fail()
    {
        TrackExitPuzzle("failed", true);
        puzzleUI.SetActive(false);

        // 실패 횟수 증가 및 데이터 송신
        DataManager.instance.nowGame.numSelectFailCount++;
        var eventData = new Dictionary<string, object> {
            { "PlayCode", DataManager.instance.nowGame.playCode },
            { "DoorID", gameObject.name },
            { "SubmittedAnswer", submittedAnswer },
            { "FailAccumulated", DataManager.instance.nowGame.numSelectFailCount }
        };
        CustomEvent failEvent = new CustomEvent("number_puzzle_fail");
        foreach (var param in eventData) failEvent.Add(param.Key, param.Value);
        AnalyticsService.Instance.RecordEvent(failEvent);

        DialogueManager.Instance.StartDialogue(failDialogue);
    }
}
