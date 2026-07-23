using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    // 상호작용 UI
    public GameObject interactionUI;

    // UI 이동 속도
    public float uiFollowSpeed = 10f;

    // 근처 상호작용 가능한 물체들
    private List<InteractableObject> nearbyObjects =
        new List<InteractableObject>();

    // 현재 가장 가까운 물체
    private InteractableObject closestObject;

    // 이전에 선택된 물체
    private InteractableObject previousObject;

    void Start()
    {
        // 시작 시 UI 숨기기
        interactionUI.SetActive(false);
    }

    void LateUpdate()
    {
        if (DialogueManager.Instance != null &&DialogueManager.Instance.IsPlaying)
        {
            interactionUI.SetActive(false);
            return;
        }
        // 가장 가까운 물체 찾기
        FindClosestObject();

        // 가까운 물체가 있을 경우
        if (closestObject != null)
        {
            // UI 표시
            interactionUI.SetActive(true);

            // interactionPoint 위치 가져오기
            Vector3 worldPosition;

            if (closestObject.interactionPoint != null)
            {
                worldPosition = closestObject.interactionPoint.position;
            }
            else
            {
                worldPosition = closestObject.transform.position;
            }

            // 월드 좌표 → 화면 좌표 변환
            Vector3 screenPosition =
                Camera.main.WorldToScreenPoint(worldPosition);

            // 카메라 앞에 있을 때만 표시
            if (screenPosition.z > 0)
            {
                float margin = 80f;
                screenPosition.x = Mathf.Clamp(
                    screenPosition.x,
                    margin,
                    Screen.width - margin);

                screenPosition.y = Mathf.Clamp(
                    screenPosition.y,
                    margin,
                    Screen.height - margin);

                // 다른 물체로 변경되었으면 즉시 이동
                if (closestObject != previousObject)
                {
                    interactionUI.transform.position = screenPosition;
                    previousObject = closestObject;
                }
                else
                {
                    // 같은 물체면 부드럽게 이동
                    interactionUI.transform.position =
                        Vector3.Lerp(
                            interactionUI.transform.position,
                            screenPosition,
                            Time.deltaTime * uiFollowSpeed
                        );
                }
            }
            else
            {
                interactionUI.SetActive(false);
            }

            // E 키 입력
            if (Input.GetKeyDown(KeyCode.E)
            && !InputLockManager.LockInteraction
            && !DialogueManager.Instance.IsPlaying)
            {
                closestObject.Interact();
            }
        }
        else
        {
            // 가까운 물체가 없으면 UI 숨기기
            interactionUI.SetActive(false);

            // 이전 물체 초기화
            previousObject = null;
        }
    }
    public void HideInteractionUI()
    {
        interactionUI.SetActive(false);
        previousObject = null;
    }
    void FindClosestObject()
    {
        float closestDistance = Mathf.Infinity;
        closestObject = null;

        foreach (InteractableObject obj in nearbyObjects)
        {
            // 삭제된 오브젝트 예외 처리
            if (obj == null)
                continue;

            // 상호작용 불가한 오브젝트 예외 처리 인터랙션오브젝트의 IsAvailable()
            if (!obj.IsAvailable())
                continue;

            // 거리 계산
            float distance =
                Vector3.Distance(
                    transform.position,
                    obj.transform.position
                );

            // 더 가까운 물체 발견 시 교체
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = obj;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        InteractableObject interactable =
            other.GetComponent<InteractableObject>();

        if (interactable != null)
        {
            // 중복 추가 방지
            if (!nearbyObjects.Contains(interactable))
            {
                nearbyObjects.Add(interactable);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        InteractableObject interactable =
            other.GetComponent<InteractableObject>();

        if (interactable != null)
        {
            nearbyObjects.Remove(interactable);
        }
    }
}