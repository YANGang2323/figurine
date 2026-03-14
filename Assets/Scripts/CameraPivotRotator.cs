using UnityEngine;

// 카메라 시점을 회전시키는 스크립트 (싱글톤 적용)
// Tab 키를 눌러 왼쪽 시점(기본) ↔ 오른쪽 시점(측면) 전환
public class CameraPivotRotator : MonoBehaviour
{
    // 싱글톤 인스턴스
    public static CameraPivotRotator Instance { get; private set; }

    [Header("Rotation Settings")]
    public float rotationSpeed = 5f;          // 회전 속도 조절
    public bool isSideView;           // 현재 측면 시점 여부 (true면 시점2)
    private bool isRotating = false;
    public bool IsRotating => isRotating; // 외부 접근용

    [Header("Timing Puzzle")]
    public bool timingMode = false;
    public float normalSpeed = 5f;
    public float timingSpeed = 10.0f;


    //추가
    [Header("Player Following Settings")]
    public Transform player; // 플레이어 Transform
    public float followSpeed = 3f; // 카메라가 플레이어를 따라가는 속도
    public Vector3 sideViewOffset = new Vector3(5f, 0f, 0f); // 사이드뷰에서의 오프셋
    private PlayerMovement3 playerMovement;
    public GameObject playerObj;

    [Header("Movement Constraints")]
    public bool useMovementConstraints = true; // 움직임 제한 사용 여부
    public Vector3 minPosition = new Vector3(-10f, 0f, -10f); // 최소 위치
    public Vector3 maxPosition = new Vector3(10f, 0f, 10f); // 최대 위치
    //

    public enum CameraMovingType { Fix, Moving, Follow } 
    public CameraMovingType cameraType = CameraMovingType.Fix;

    private Quaternion defaultRotation;       // 왼쪽 시점(기본)
    private Quaternion sideRotation;          // 오른쪽 시점
    private Quaternion targetRotation;        // 목표 회전값
    private Vector3 defaultPosition; // 기본 위치
    private Vector3 targetPosition; // 목표 위치
    private string nowstage;

    public GameObject PipeManager;
    public bool PipeRun = false;

    public GameObject pipe;
    void Awake()
    {
        // 싱글톤 인스턴스 초기화
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
    }

    void Start()
    {
        if (player != null)
            playerMovement = player.GetComponent<PlayerMovement3>();

        DecideCameraType();

        /* GameData data = DataManager.instance.nowGame;     
        nowstage = data.stage.ToString();
        PipeRun = PipeManager.GetComponent<BoardManager>().PipeRun;*/ //잠시주석처리0314

        //if (nowstage == "1")
        //{
        //    cameraType = CameraMovingType.Fix;
        //    defaultRotation = transform.rotation;
        //    sideRotation = Quaternion.Euler(0, 270, 0);
        //    targetRotation = defaultRotation;
        //}
        //else if (nowstage == "2")
        //{
        //    if (!PipeRun)
        //    {
        //        if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //        else if (isSideView) { cameraType = CameraMovingType.Moving; }

        //    }
        //    else
        //    {
        //        cameraType = CameraMovingType.Fix;
        //        Debug.Log("Fix완료!");
        //    }


        //}
        //else if (nowstage == "3") //3스테이지 카메라 조건 정하기
        //{
        //    //if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //    cameraType = CameraMovingType.Follow;
        //    //else if (isSideView) { cameraType = CameraMovingType.Follow; }
        //}
    }

    void Update()
    {
        //Debug.Log("현재 cameraType: " + cameraType);
        //Debug.Log("현재 스테이지: " + nowstage);
        



        // Tab 키 입력 시 시점 전환
        //if (InputManager.instance.GetKeyDown(KeyCode.Tab) && !isRotating && !pipe.activeSelf && (cameraType == CameraMovingType.Fix || cameraType == CameraMovingType.Moving))
        //{
        if (InputManager.instance.GetKeyDown(KeyCode.Tab) && !isRotating && (cameraType == CameraMovingType.Fix || cameraType == CameraMovingType.Moving))
        {
            isSideView = !isSideView;
            targetRotation = isSideView ? sideRotation : defaultRotation;
            isRotating = true;

            //if (nowstage == "2")
            //{
            //    cameraType = isSideView ? CameraMovingType.Moving : CameraMovingType.Fix;
            //}*/ //잠시주석0314
        }


        // 부드러운 회전 처리
        if (isRotating)
        {
            float rotationSpeed = timingMode ? timingSpeed : 5f;
            if (cameraType == CameraMovingType.Moving)
            {
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            }
            else { transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed); 
            }

            // 회전 완료 판정
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f)
            {
                transform.rotation = targetRotation;
                isRotating = false;
            }
        }

        // Moving 타입이고 사이드뷰일 때만 플레이어 따라가기
        if (cameraType == CameraMovingType.Moving && isSideView && player != null && !isRotating)
        {
            FollowPlayer();
        }
        else if ( !isRotating  && !isSideView && cameraType == CameraMovingType.Fix)
        {
            //if (nowstage == "1" && nowstage == "2")
            //{
                transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
            //}*/ //잠시주석0314
        }
        else if (cameraType == CameraMovingType.Follow && player != null ) //&& !isRotating
        {
            //SetFollowSettings(3f, sideViewOffset);

            // Follow 타입일 때는 자유롭게 플레이어 따라 부드럽게 이동
            FreeCircleFollowPlayer();
        }
    }
    void DecideCameraType() {
        //GameData data = DataManager.instance.nowGame;
        //nowstage = data.stage.ToString(); */ //잠시주석0314
        nowstage = "1";

        defaultPosition = transform.position;
        targetPosition = defaultPosition;

        if (nowstage == "1")
        {
            cameraType = CameraMovingType.Fix;

            defaultRotation = Quaternion.Euler(0, 180, 0);
            sideRotation = Quaternion.Euler(0, 270, 0);
            isSideView = true;
            defaultPosition = transform.position;
            targetPosition = defaultPosition;
        }
        else if (nowstage == "2") 
        {
            if (!PipeRun)
            {
                if (!isSideView) { cameraType = CameraMovingType.Fix; }
                else if (isSideView) { cameraType = CameraMovingType.Moving; }

                defaultRotation = Quaternion.Euler(0, 180, 0);
                sideRotation = Quaternion.Euler(0, 270, 0);
                isSideView = false;

                defaultPosition = transform.position;
                targetPosition = defaultPosition;
            }
            else
            {
                cameraType = CameraMovingType.Fix;
                Debug.Log("Fix완료!");
            }
            
            
        }
        else if (nowstage == "3") //3스테이지 카메라 조건 정하기
        {
            var playerscript1 = playerObj.GetComponent<PlayerMovement>();
            var playerscript3 = playerObj.GetComponent<PlayerMovement3>();
            playerscript1.enabled = true;
            playerscript3.enabled = false;
            //if (!isSideView) { cameraType = CameraMovingType.Fix; }
            cameraType = CameraMovingType.Fix;
            isSideView = false;
            defaultRotation = Quaternion.Euler(0, 180, 0);
            sideRotation = Quaternion.Euler(0, 270, 0);

            
            defaultPosition = transform.position;
            targetPosition = defaultPosition;
            //else if (isSideView) { cameraType = CameraMovingType.Follow; }
        }
        targetRotation = isSideView ? sideRotation : defaultRotation;
    }

    void FollowPlayer()
    {
        // 플레이어 위치에 오프셋을 더한 목표 위치 계산
        Vector3 desiredPosition = player.position + sideViewOffset;

        // 움직임 제한 적용
        if (useMovementConstraints)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minPosition.z, maxPosition.z);
        }

        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);
    }
    // 외부에서 움직임 범위를 설정할 수 있는 메서드
    public void SetMovementConstraints(Vector3 min, Vector3 max)
    {
        minPosition = min;
        maxPosition = max;
    }


    public Vector3 cameraOffset;      // 카메라와 플레이어 사이 거리
    
    // 외부에서 플레이어 따라가기 설정을 변경할 수 있는 메서드
    public void SetFollowSettings(float speed, Vector3 offset)
    {
        followSpeed = speed;
        sideViewOffset = offset;
    }
    //void FreeFollowPlayer()
    //{
    //    if (player == null) return;

    //    // 플레이어 → 중심 방향 계산
    //    Vector3 playerToCenter = player.position - playerMovement.center.position;
    //    Vector3 offsetDirection = Quaternion.Euler(0f, 90f, 0f) * playerToCenter.normalized; // 90도 회전해서 옆에서 보기

    //    float distance = 5f; // 플레이어와 카메라 거리
    //    float height = 0.1f;   // 카메라 높이

    //    // 최종 위치 계산 (원 궤도 따라 회전)
    //    Vector3 desiredPosition = player.position + offsetDirection * distance + Vector3.up * height;

    //    transform.position = Vector3.Lerp(transform.position, desiredPosition, followSpeed * Time.deltaTime);
    //    //transform.LookAt(player.position + Vector3.up * 1f); // 약간 위를 바라보게
    //    Debug.Log("FreeFollow target position: " + desiredPosition);
    //    Debug.Log("Camera position before move: " + transform.position);
    //}
    void FreeCircleFollowPlayer()
    {
        if (player == null || playerMovement == null || playerMovement.center == null)
            return;

        // 중심 기준 벡터
        Vector3 center = playerMovement.center.position;
        Vector3 toPlayer = player.position - center;

        float playerRadius = toPlayer.magnitude;
        float cameraRadius = playerRadius + 3f; // 카메라가 플레이어보다 더 멀리 떨어지도록

        // 플레이어가 있는 각도 계산 (XZ 평면)
        float angleRad = Mathf.Atan2(toPlayer.z, toPlayer.x);

        // 카메라 위치 = 같은 각도, 더 큰 반지름
        float x = center.x + cameraRadius * Mathf.Cos(angleRad);
        float z = center.z + cameraRadius * Mathf.Sin(angleRad);
        float y = player.position.y + 1f; 

        Vector3 desiredPosition = new Vector3(x, y, z);

        float t = 1f - Mathf.Exp(-10f * Time.deltaTime); // 빠른 지수 보간
        transform.position = Vector3.Lerp(transform.position, desiredPosition, t);

        Vector3 lookAtPos = player.position;
        lookAtPos.y = transform.position.y; // 현재 카메라 높이로 고정 (X, Z축 회전 방지)
        transform.LookAt(lookAtPos);
    }
}
