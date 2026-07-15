using UnityEngine;

public class PerspectivePuzzlePiece : MonoBehaviour
{
    public enum RotateAxis
    {
        X,
        Y
    }

    [Header("Setting")]
    public RotateAxis axis;

    [Header("Camera")]
    public bool requireSideView;

    [HideInInspector]
    public bool canRotate = false;

    public int currentStep = 0;

    [Tooltip("정답 회전 횟수")]
    public int answerStep = 0;

    public PerspectiveEmblemPuzzle puzzle;

    public void Interact()
    {
        Debug.Log("조각 Interact");

        if (!canRotate){
            Debug.Log("회전 불가");
            return;
        }
        Debug.Log("회전 가능");
        if (CameraPivotRotator.Instance.isSideView != requireSideView){
            Debug.Log("시점 불일치");
            return;
        }
        RotatePiece();

        puzzle.CheckPuzzle();
    }

    void RotatePiece()
    {
        currentStep = (currentStep + 1) % 4;

        if (axis == RotateAxis.X)
            transform.Rotate(-90f, 0, 0, Space.Self);
        else
            transform.Rotate(0, -90f, 0, Space.Self);

    }

    public bool IsCorrect()
    {
        return currentStep == answerStep;
    }
}