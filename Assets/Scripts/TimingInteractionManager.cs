using UnityEngine;
using System.Collections.Generic;

public class TimingInteractionManager : MonoBehaviour
{
    [Header("Camera")]
    public Camera cam;

    [Header("Targets")]
    public List<TimingInteractable> targets = new List<TimingInteractable>();

    [Header("Timing Settings")]
    public float lineTolerance = 120f;   // 중앙선 판정 두께
    public float timingWindow = 0.3f;  // 입력 허용 시간

    private TimingInteractable currentTarget;

    private bool inTimingZone = false;
    private float currentTimer = 0f;
    float highlightDuration = 1.0f; // 노란색 유지 시간
    float highlightTimer = 0f;


    Dictionary<TimingInteractable, float> prevX = new Dictionary<TimingInteractable, float>();
    void Update()
    {
        // 카메라 회전 중이 아니면 작동 X
        if (!CameraPivotRotator.Instance.IsRotating)
        {
            ClearTarget();
            return;
        }

        DetectTarget();
        UpdateTimingWindow();

        if (Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.F) ||
            Input.GetMouseButtonDown(0))
        {
            TryInteract();
        }
        UpdateHighlight();
    }

    // 🎯 가장 중앙선에 가까운 오브젝트 찾기
    void DetectTarget()
    {
        TimingInteractable bestTarget = null;
        float bestDist = float.MaxValue;

        float centerX = Screen.width / 2f;

        foreach (var t in targets)
        {
            if (t == null || t.isActivated) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(t.transform.position);
            if (screenPos.z < 0) continue;

            float currentX = screenPos.x;

            if (!prevX.ContainsKey(t))
                prevX[t] = currentX;

            float previousX = prevX[t];

            float dx = Mathf.Abs(currentX - centerX);

            if (dx < lineTolerance && dx < bestDist)
            {
                bestTarget = t;
                bestDist = dx;
            }
            prevX[t] = currentX;
        }

        // 타겟 변경 처리
        if (currentTarget != bestTarget)
        {
            if (currentTarget != null)
                currentTarget.SetTarget(false);

            currentTarget = bestTarget;

            if (currentTarget != null)
            {
                currentTarget.SetTarget(true);

                // ⭐ 타이밍 시작
                inTimingZone = true;
                currentTimer = timingWindow;

                highlightTimer = highlightDuration;
            }
        }
    }

    // ⏱ 타이밍 윈도우 업데이트
    void UpdateTimingWindow()
    {
        if (!inTimingZone) return;

        currentTimer -= Time.deltaTime;

        if (currentTimer <= 0f)
        {
            inTimingZone = false;

            // 타이밍 끝나면 타겟 표시 해제
            if (currentTarget != null)
                currentTarget.SetTarget(false);
        }
    }

    // 🎮 입력 처리
    void TryInteract()
    {
        if (currentTarget == null) return;
        if (!inTimingZone) return;

        currentTarget.Activate();

        // 한 번 성공하면 바로 초기화
        inTimingZone = false;
        currentTarget = null;

        CheckPuzzleClear();
    }

    // 🧹 타겟 초기화
    void ClearTarget()
    {
        if (currentTarget != null)
        {
            currentTarget.SetTarget(false);
            currentTarget = null;
        }

        inTimingZone = false;
    }

    // ✅ 퍼즐 완료 체크
    void CheckPuzzleClear()
    {
        foreach (var t in targets)
        {
            if (t == null) continue;

            if (!t.isActivated)
                return;
        }

        Debug.Log("퍼즐 완료!");
    }
    void UpdateHighlight()
    {
        if (currentTarget == null) return;

        highlightTimer -= Time.deltaTime;

        if (highlightTimer <= 0f && !inTimingZone)
        {
            currentTarget.SetTarget(false);
            currentTarget = null;
        }
    }

    // 🎨 중앙선 시각화 (디버그용)
    //void OnGUI()
    //{
    //    if (!CameraPivotRotator.Instance.timingMode) return;

    //    float x = Screen.width / 2f;

    //    GUI.color = Color.red;
    //    GUI.DrawTexture(
    //        new Rect(x - 2, 0, 4, Screen.height),
    //        Texture2D.whiteTexture
    //    );
    //}
}