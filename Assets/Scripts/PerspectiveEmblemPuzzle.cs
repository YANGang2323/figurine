using UnityEngine;

public class PerspectiveEmblemPuzzle : MonoBehaviour
{
    public enum PuzzleState
    {
        Before,
        Playing,
        Cleared
    }

    [Header("State")]
    public PuzzleState state = PuzzleState.Before;
    private InteractableObject startInteractable;
    private InteractableObject rightInteractable;
    private InteractableObject leftInteractable;
    private Collider startCollider;

    [Header("Dialogue")]
    public DialogueTrigger dialogueTrigger;
    public DialogueData firstPieceDialogue;

    private bool firstPieceCompleted = false;

    [Header("Pieces")]
    public PerspectivePuzzlePiece pieceX;
    public PerspectivePuzzlePiece pieceY;

    private void Awake()
    {
        startInteractable = GetComponent<InteractableObject>();

        rightInteractable = pieceX.GetComponent<InteractableObject>();
        leftInteractable = pieceY.GetComponent<InteractableObject>();

        // 시작 상태
        startInteractable.canInteract = true;
        rightInteractable.canInteract = false;
        leftInteractable.canInteract = false;
    }
    private void Start()
    {
        // 게임이 시작되거나 씬이 로드될 때 저장된 데이터를 불러옵니다.
        LoadPuzzleData();
    }

    // 현재 데이터 정보를 DataManager의 변수에 덮음
    public void UpdatePuzzleDataToManager()
    {
        if (DataManager.instance == null || DataManager.instance.nowGame == null) return;

        var save = DataManager.instance.nowGame;
        save.EmblemPuzzleState = (int)state;
        save.EmblempieceXCurrentStep = pieceX.currentStep;
        save.EmblempieceYCurrentStep = pieceY.currentStep;
        save.EmblemfirstPieceCompleted = firstPieceCompleted;

    }

    // 세이브 데이터를 기반으로 퍼즐을 복구하는 함수
    private void LoadPuzzleData()
    {
        // 1. 데이터매니저가 없거나 세이브 슬롯이 지정되지 않은 상태(-1)라면 
        // 새 게임이거나 세이브를 불러온 게 아니므로 인스펙터 설정을 그대로 유지하고 나갑니다.
        if (DataManager.instance == null || DataManager.instance.nowGame == null || DataManager.instance.nowSlot == -1)
        {
            SetDefaultInspectorState();
            return;
        }

        var load = DataManager.instance.nowGame;

        // 2. 만약 세이브 슬롯은 지정되어 있지만, 퍼즐을 한 번도 건드리지 않은 완전 초기 상태(Before = 0)라면
        // 이때도 씬에 배치된 인스펙터 본연의 스텝과 각도를 지켜야 하므로 회전시키지 않고 종료합니다.
        if (load.EmblemPuzzleState == 0)
        {
            SetDefaultInspectorState();
            return;
        }

        // ====================================================================
        // 여기서부터는 실제로 '플레이 중(1)'이거나 '클리어(2)' 상태인 세이브 파일을 로드했을 때만 실행됩니다.
        // ====================================================================
        state = (PuzzleState)load.EmblemPuzzleState;
        firstPieceCompleted = load.EmblemfirstPieceCompleted;
        pieceX.currentStep = load.EmblempieceXCurrentStep;
        pieceY.currentStep = load.EmblempieceYCurrentStep;

        // 오직 이때만 저장된 데이터를 바탕으로 각도를 회전시켜 복구합니다!
        pieceX.ApplyLoadedRotation();
        pieceY.ApplyLoadedRotation();

        // 상태(Playing 또는 Cleared)에 따른 인터랙션 활성화 상태 복구
        if (state == PuzzleState.Playing)
        {
            startInteractable.canInteract = false;
            rightInteractable.canInteract = true;
            leftInteractable.canInteract = true;

            pieceX.canRotate = !firstPieceCompleted || !pieceX.IsCorrect();
            pieceY.canRotate = !firstPieceCompleted || !pieceY.IsCorrect();
        }
        else if (state == PuzzleState.Cleared)
        {
            startInteractable.canInteract = true;
            rightInteractable.canInteract = false;
            leftInteractable.canInteract = false;
            pieceX.canRotate = false;
            pieceY.canRotate = false;
        }
    }

    // 인스펙터에 배치한 순수한 원본 상태를 그대로 유지시키는 헬퍼 함수
    private void SetDefaultInspectorState()
    {
        state = PuzzleState.Before;
        startInteractable.canInteract = true;
        rightInteractable.canInteract = false;
        leftInteractable.canInteract = false;
        pieceX.canRotate = false;
        pieceY.canRotate = false;
        // 핵심: 여기서는 ApplyLoadedRotation()을 절대 호출하지 않으므로 
        // 씬에 놓인 최초의 이쁜 각도가 단 0.01도도 돌아가지 않고 완벽히 보존됩니다.
    }
    /// 퍼즐 시작 오브젝트와 상호작용 시 호출
    public void InteractPuzzle()
    {
        switch (state)
        {
            case PuzzleState.Before:

                dialogueTrigger.PlayDialogue();

                state = PuzzleState.Playing;

                pieceX.canRotate = true;
                pieceY.canRotate = true;

                startInteractable.canInteract = false;

                rightInteractable.canInteract = true;
                leftInteractable.canInteract = true;

                Debug.Log("퍼즐 시작");
                Debug.Log(pieceX.canRotate);
                Debug.Log(pieceY.canRotate);
                UpdatePuzzleDataToManager(); // 게임데이터(데이터매니저) 갱신
                break;

            case PuzzleState.Playing:

                // 진행중에는 아무것도 안 함
                break;

            case PuzzleState.Cleared:
                dialogueTrigger.PlayPuzzleCompleteDialogue();
                break;
        }
    }

    public void CheckPuzzle()
    {
        if (state != PuzzleState.Playing)
            return;

        bool rightCorrect = pieceX.IsCorrect();
        bool leftCorrect = pieceY.IsCorrect();

        // 첫 번째 조각 완성
        if (!firstPieceCompleted)
        {
            if (rightCorrect)
            {
                firstPieceCompleted = true;
                pieceX.canRotate = false;   // 오른쪽 조각 잠금

                if (firstPieceDialogue != null)
                    DialogueManager.Instance.StartDialogue(firstPieceDialogue);
            }
            else if (leftCorrect)
            {
                firstPieceCompleted = true;
                pieceY.canRotate = false;   // 왼쪽 조각 잠금

                if (firstPieceDialogue != null)
                    DialogueManager.Instance.StartDialogue(firstPieceDialogue);
            }
        }

        UpdatePuzzleDataToManager(); // 조각이 회전하여 단계가 바뀔 때마다 매니저 데이터 갱신

        if (rightCorrect && leftCorrect)
        {
            ClearPuzzle();
        }
    }
    //public void CheckPuzzle()
    //{
    //    if (state != PuzzleState.Playing)
    //        return;

    //    bool xCorrect =
    //        Mathf.Abs(
    //            Mathf.DeltaAngle(
    //                pieceX.transform.localEulerAngles.x,
    //                answerX))
    //        < 1f;

    //    bool yCorrect =
    //        Mathf.Abs(
    //            Mathf.DeltaAngle(
    //                pieceY.transform.localEulerAngles.y,
    //                answerY))
    //        < 1f;

    //    if (xCorrect && yCorrect)
    //    {
    //        ClearPuzzle();
    //    }
    //}

    void ClearPuzzle()
    {
        state = PuzzleState.Cleared;
        pieceX.canRotate = false;
        pieceY.canRotate = false;

        startInteractable.canInteract = true;

        rightInteractable.canInteract = false;
        leftInteractable.canInteract = false;

        UpdatePuzzleDataToManager(); // 클리어 상태 매니저 데이터 갱신

        dialogueTrigger.PlayPuzzleCompleteDialogue();
    }
}