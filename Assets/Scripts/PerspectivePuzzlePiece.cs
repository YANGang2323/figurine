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
        if (axis == RotateAxis.X)
        {
            transform.Rotate(-90f, 0f, 0f, Space.Self);
        }
        else
        {
            transform.Rotate(0f, -90f, 0f, Space.Self);
        }
    }
}