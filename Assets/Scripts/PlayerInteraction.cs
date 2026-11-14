using System.Collections.Generic;
using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    // ��ȣ�ۿ� UI
    public GameObject interactionUI;

    // UI �̵� �ӵ�
    public float uiFollowSpeed = 10f;

    // ��ó ��ȣ�ۿ� ������ ��ü��
    private List<InteractableObject> nearbyObjects =
        new List<InteractableObject>();

    // ���� ���� ����� ��ü
    private InteractableObject closestObject;

    // ������ ���õ� ��ü
    private InteractableObject previousObject;

    void Start()
    {
        // ���� �� UI �����
        interactionUI.SetActive(false);
    }

    void LateUpdate()
    {
        if (DialogueManager.Instance != null &&DialogueManager.Instance.IsPlaying)
        {
            interactionUI.SetActive(false);
            return;
        }
        // ȸ�� ���ϰ� ui�߰�
        if (CameraPivotRotator.Instance != null &&
            CameraPivotRotator.Instance.IsRotating)
        {
            interactionUI.SetActive(false);
            previousObject = null;
            return;
        }
        // ���� ����� ��ü ã��
        FindClosestObject();

        // ����� ��ü�� ���� ���
        if (closestObject != null)
        {
            // UI ǥ��
            interactionUI.SetActive(true);

            // interactionPoint ��ġ ��������
            Vector3 worldPosition;

            if (closestObject.interactionPoint != null)
            {
                worldPosition = closestObject.interactionPoint.position;
            }
            else
            {
                worldPosition = closestObject.transform.position;
            }

            // ���� ��ǥ �� ȭ�� ��ǥ ��ȯ
            Vector3 screenPosition =
                Camera.main.WorldToScreenPoint(worldPosition);

            // ī�޶� �տ� ���� ���� ǥ��
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

                // �ٸ� ��ü�� ����Ǿ����� ��� �̵�
                if (closestObject != previousObject)
                {
                    interactionUI.transform.position = screenPosition;
                    previousObject = closestObject;
                }
                else
                {
                    // ���� ��ü�� �ε巴�� �̵�
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

            // E Ű �Է�
            if (Input.GetKeyDown(KeyCode.E)
            && !InputLockManager.LockInteraction
            && !DialogueManager.Instance.IsPlaying)
            {
                closestObject.Interact();
            }
        }
        else
        {
            // ����� ��ü�� ������ UI �����
            interactionUI.SetActive(false);

            // ���� ��ü �ʱ�ȭ
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
        for (int i = nearbyObjects.Count - 1; i >= 0; i--)
        {
            InteractableObject obj = nearbyObjects[i];

            // �����Ǿ��ų� ��Ȱ��ȭ�Ǿ����� ����Ʈ���� ����
            if (obj == null || !obj.gameObject.activeInHierarchy)
            {
                nearbyObjects.RemoveAt(i);
                continue;
            }
            // ��ȣ�ۿ� �Ұ��� ������Ʈ ���� ó�� ���ͷ��ǿ�����Ʈ�� IsAvailable()
            if (!obj.IsAvailable())
                continue;
            // �Ÿ� ���
            float distance = Vector3.Distance(
                transform.position,
                obj.transform.position);
            // �� ����� ��ü �߰� �� ��ü
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestObject = obj;
            }
        }
    }

    private static bool HasInteractionTrigger(Collider other)
    {
        foreach (Collider collider in other.GetComponents<Collider>())
        {
            if (collider.enabled && collider.isTrigger)
                return true;
        }

        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.isTrigger && HasInteractionTrigger(other))
            return;

        InteractableObject interactable =
            other.GetComponent<InteractableObject>();

        if (interactable != null)
        {
            // �ߺ� �߰� ����
            if (!nearbyObjects.Contains(interactable))
            {
                nearbyObjects.Add(interactable);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.isTrigger && HasInteractionTrigger(other))
            return;

        InteractableObject interactable =
            other.GetComponent<InteractableObject>();

        if (interactable != null)
        {
            nearbyObjects.Remove(interactable);
        }
    }
}