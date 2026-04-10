using UnityEngine;

public class CCTVTriggerRelay : MonoBehaviour
{
    private CCTVController parentController;

    void Start()
    {
        // 부모에 있는 메인 컨트롤러를 찾아옵니다.
        parentController = GetComponentInParent<CCTVController>();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            // 부모 스크립트의 감지 함수를 호출합니다.
            parentController.OnPlayerInSight(other);
        }
    }
}