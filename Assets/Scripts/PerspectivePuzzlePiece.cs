using System.Collections;
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
    private Quaternion initialSceneRotation;
    private int initialSceneStep;

    [Header("Camera")]
    public bool requireSideView;

    [HideInInspector]
    public bool canRotate = false;

    public int currentStep = 0;

    [Tooltip("정답 회전 횟수")]
    public int answerStep = 0;

    [Header("Rotate speed")]
    public float rotateSpeed = 180f;

    public PerspectiveEmblemPuzzle puzzle;

    private void Awake()
    {
        initialSceneRotation = transform.localRotation;
        initialSceneStep = currentStep;
    }

    public void Interact()
    {
        Debug.Log("조각 Interact");

        if (!canRotate)
        {
            Debug.Log("회전 불가");
            return;
        }
        Debug.Log("회전 가능");
        if (CameraPivotRotator.Instance.isSideView != requireSideView)
        {
            Debug.Log("시점 불일치");
            return;
        }
        RotatePiece();
    }

    private bool isRotating = false;

    void RotatePiece()
    {
        if (isRotating)
            return;

        currentStep = (currentStep + 1) % 4;

        Vector3 axisVector =
            (axis == RotateAxis.X) ? Vector3.right : Vector3.up;

        StartCoroutine(RotateSmooth(axisVector));
    }
    IEnumerator RotateSmooth(Vector3 axisVector)
    {
        isRotating = true;

        float rotated = 0f;
        float speed = rotateSpeed;   // 초당 회전 각도

        while (rotated < 90f)
        {
            float angle = speed * Time.deltaTime;

            if (rotated + angle > 90f)
                angle = 90f - rotated;

            transform.Rotate(axisVector, -angle, Space.Self);

            rotated += angle;

            yield return null;
        }

        isRotating = false;

        puzzle.CheckPuzzle();
    }

    public bool IsCorrect()
    {
        return currentStep == answerStep;
    }

    public void ApplyLoadedRotation()
    {
        // 처음 배치된 스텝과 현재 세이브 데이터 스텝의 차이를 구합니다.
        int stepDifference = currentStep - initialSceneStep;

        // 1스텝당 -90도씩 회전하므로 상대적인 회전량 계산
        float targetAngle = stepDifference * -90f;

        Vector3 axisVector = (axis == RotateAxis.X) ? Vector3.right : Vector3.up;

        // 처음에 배치했던 원본 각도(initialSceneRotation) 뒤에 스텝 차이만큼만 회전을 덧붙입니다.
        // 만약 새 게임이라 스텝 차이가 0이면 원본 각도가 그대로 복원됩니다.
        transform.localRotation = initialSceneRotation * Quaternion.AngleAxis(targetAngle, axisVector);
    }
}