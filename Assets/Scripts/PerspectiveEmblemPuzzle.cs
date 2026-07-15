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
        if (!firstPieceCompleted && (rightCorrect || leftCorrect))
        {
            firstPieceCompleted = true;

            if (firstPieceDialogue != null)
            {
                DialogueManager.Instance.StartDialogue(firstPieceDialogue);
            }
        }

        

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

        dialogueTrigger.PlayPuzzleCompleteDialogue();
    }
}