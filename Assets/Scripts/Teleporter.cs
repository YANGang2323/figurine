using UnityEngine;

public class Teleporter : MonoBehaviour
{
    [Header("텔레포트 설정")]
    [Tooltip("플레이어가 이동할 목적지 텔레포터 오브젝트를 연결하세요.")]
    public Transform destination;

    [Tooltip("이 포털이 활성화되어 이동 가능한지 여부")]
    public bool isLinkActive = true;

    private bool isPlayerInside = false;
    private GameObject playerObject;

    private void Update()
    {
        // 플레이어가 콜라이더 안에 있고, Tab 키를 눌렀을 때
        if (isPlayerInside && Input.GetKeyDown(KeyCode.Tab))
        {
            Teleport();
        }
    }

    private void Teleport()
    {
        // 목적지가 없거나, 링크가 비활성화된 경우(일방향 출구용) 작동 안 함
        if (destination == null || !isLinkActive)
        {
            Debug.LogWarning($"{gameObject.name}: 목적지가 지정되지 않았거나 비활성화된 링크입니다.");
            return;
        }

        if (playerObject != null)
        {
            // CharacterController가 있으면 텔레포트 시 오작동하므로 잠시 껐다 켭니다
            CharacterController cc = playerObject.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;

            // 플레이어 위치를 목적지 텔레포터의 위치로 이동
            playerObject.transform.position = destination.position;

            if (cc != null) cc.enabled = true;

            Debug.Log($"{playerObject.name}이(가) {destination.name}(으)로 순간이동했습니다.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 충돌한 오브젝트가 플레이어인지 확인 (태그가 "Player"여야 합니다)
        if (other.CompareTag("Player"))
        {
            isPlayerInside = true;
            playerObject = other.gameObject;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerInside = false;
            playerObject = null;
        }
    }
}
