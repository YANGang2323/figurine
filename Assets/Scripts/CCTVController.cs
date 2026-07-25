using UnityEngine;
using UnityEngine.UI; // 게임오버 UI용 (필요 시)

public class CCTVController : MonoBehaviour
{
    [Header("활성 여부")]
    [SerializeField] private bool isActive = false;
    [Header("회전 설정")]
    public bool isRotating = true;
    public float rotationAngle = 45f;
    public float rotationSpeed = 2f;
    private Quaternion initialRotation;

    [Header("감지 및 경고 설정")]
    public Light cctvLight;            // CCTV의 스포트라이트
    public float detectionThreshold = 2.0f; // 감지까지 걸리는 시간 (초)
    private float detectionTimer = 0f;      // 현재 쌓인 감지 시간

    [Header("플레이어 설정")]
    public Transform spawnPoint;       // 리스폰 지점
    public Transform rayOrigin;         // 레이 시작점 (CCTV 렌즈)

    private Color normalColor = Color.white; // 평상시 불빛 색상
    [Header("오버시 스크립트")]
    public DialogueData stage2_cctv_Respawn;
    private bool isCaught = false;
    void Start()
    {
        initialRotation = transform.rotation;
        if (cctvLight != null) normalColor = cctvLight.color;
    }

    void Update()
    {
        if (!isActive)
            return;
        if (StealthManager.Instance.IsRespawning)
            return;
        // 1. 회전 로직
        if (isRotating)
        {
            float angle = Mathf.Sin(Time.time * rotationSpeed) * rotationAngle;
            transform.rotation = initialRotation * Quaternion.Euler(0, angle, 0);
        }

        // 2. 감지 안 될 때 색상 및 타이머 서서히 복구 (선택 사항)
        if (detectionTimer > 0)
        {
            // 시야에서 벗어나면 경고 수치가 서서히 줄어들게 함
            detectionTimer -= Time.deltaTime * 0.5f;
            detectionTimer = Mathf.Max(0f, detectionTimer);
            UpdateLightColor();
        }
    }
    public void Activate()
    {
        isActive = true;
        isCaught = false;
        detectionTimer = 0f;

        if (cctvLight != null)
            cctvLight.color = normalColor;
    }
    private void OnTriggerStay(Collider other)
    {
        if (!isActive)
            return;
        if (isCaught)
            return;
        if (StealthManager.Instance != null &&
            StealthManager.Instance.IsRespawning)
            return;
        if (other.CompareTag("Player"))
        {
            // 플레이어 중심 높이 사용
            Vector3 targetPos = other.bounds.center;

            Vector3 direction = targetPos - rayOrigin.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(rayOrigin.position, direction.normalized, out RaycastHit hit, distance))
            {
                Debug.Log(hit.transform.name);
                Debug.DrawRay(rayOrigin.position, direction, Color.red);

                if (hit.transform.CompareTag("Player"))
                {
                    detectionTimer += Time.deltaTime * 1.5f;
                    UpdateLightColor();

                    if (detectionTimer >= detectionThreshold)
                    {
                        GameOver(other.transform);
                    }
                }
            }
        }
    }
    public void OnPlayerInSight(Collider playerCollider)
    {
        if (!isActive)
            return;
        if (isCaught)
            return;
        // 1. 레이 방향 설정 (CCTV 렌즈 -> 플레이어 중심)
        Vector3 targetPos = playerCollider.bounds.center;
        Vector3 direction = targetPos - rayOrigin.position;
        float distance = Vector3.Distance(rayOrigin.position, targetPos);

        // 2. Scene 뷰에서 확인용 레이 그리기 (에디터 전용)
        Debug.DrawRay(rayOrigin.position, direction, Color.red);

        // 3. 레이캐스트 실행 (벽 레이어만 체크하고 싶다면 세번째 인자에 레이어마스크 추가 가능)
        if (Physics.Raycast(rayOrigin.position, direction, out RaycastHit hit, distance))
        {
            Debug.Log(hit.transform.name);
            if (hit.transform.CompareTag("Player"))
            {
                // [감지 성공] 벽에 가려지지 않음
                detectionTimer += Time.deltaTime * 1.5f;
                UpdateLightColor();

                if (detectionTimer >= detectionThreshold)
                {
                    GameOver(playerCollider.transform);
                }
            }
            else
            {
                // [감지 실패] 레이가 벽이나 다른 물체에 먼저 맞음
                // 필요하다면 여기서 타이머를 서서히 줄이는 로직을 넣으세요.
            }
        }
    }
    void UpdateLightColor()
    {
        if (cctvLight != null)
        {
            // 감지 시간에 따라 흰색(평상시) -> 붉은색으로 보간(Lerp)
            float t = detectionTimer / detectionThreshold;
            cctvLight.color = Color.Lerp(normalColor, Color.red, t);
        }
    }

    void GameOver(Transform player)
    {
        if (isCaught)
            return;
        
        Debug.Log("게임 오버! 플레이어 검거!");
        isCaught = true;
        detectionTimer = 0; // 타이머 초기화
        UpdateLightColor();

        // 실패 다이얼로그 재생
        DialogueManager.Instance.StartDialogue(stage2_cctv_Respawn);

        // (참고) 만약 CharacterController를 사용 중이라면 이동 전후로 
        // .enabled = false/true를 해줘야 위치 값이 즉시 반영됩니다.
    }
    public void ResetDetection()
    {
        isCaught = false;
        detectionTimer = 0f;

        if (cctvLight != null)
            cctvLight.color = normalColor;
    }
}




//public class CCTVController : MonoBehaviour
//{
//    public float viewAngle = 60f;
//    public float viewDistance = 8f;

//    public float rotateSpeed = 30f;
//    public float rotateLimit = 45f;

//    public float detectTime = 2f;

//    public float height = 3f; // 감지 높이

//    float detectTimer;
//    float currentAngle;
//    bool right = true;

//    Transform player;

//    public LayerMask obstacleMask; // 벽
//    public LayerMask playerMask;   // 플레이어

//    void Start()
//    {
//        player = GameObject.FindGameObjectWithTag("Player").transform;
//    }

//    void Update()
//    {
//        if (DetectPlayer())
//        {
//            detectTimer += Time.deltaTime;
//        }
//        else
//        {
//            detectTimer -= Time.deltaTime;
//        }

//        detectTimer = Mathf.Clamp(detectTimer, 0, detectTime);

//        // UI 업데이트
//        DetectionUI.Instance.UpdateUI(detectTimer / detectTime);

//        if (detectTimer >= detectTime)
//        {
//            GameOver();
//        }
//    }



//    bool DetectPlayer()
//    {
//        if (player == null) return false;

//        Vector3 origin = transform.position + Vector3.up * 1.5f;
//        Vector3 dir = player.position - origin;

//        if (dir.magnitude > viewDistance)
//            return false;

//        // 평면 기준 각도
//        Vector3 flatDir = new Vector3(dir.x, 0, dir.z);
//        float angle = Vector3.Angle(transform.forward, flatDir);

//        if (angle < viewAngle * 0.5f)
//        {
//            RaycastHit hit;

//            // LayerMask 아예 안 씀 (핵심)
//            if (Physics.Raycast(origin, dir.normalized, out hit, viewDistance))
//            {
//                Debug.Log("Hit: " + hit.collider.name + " / Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

//                if (hit.collider.CompareTag("Player"))
//                    return true;
//            }
//        }

//        return false;
//    }

//    void GameOver()
//    {
//        Debug.Log("Game Over");
//    }
//}