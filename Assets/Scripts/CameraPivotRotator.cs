using System.Runtime.InteropServices;
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

    [Header("Player Following Settings")]
    public PlayerMovement playerMovement;
    public Transform player; // 플레이어 Transform
    public float followSpeed = 3f; // 카메라가 플레이어를 따라가는 속도
    public Vector3 sideViewOffset = new Vector3(5f, 0f, 0f); // 사이드뷰에서의 오프셋
    private PlayerMovement3 playerMovement3;
    public GameObject playerObj;

    [Header("Stage 3 Camera")]
    public bool stage3CameraMode = false;
    public float stage3TiltAngle = 20f;
    public Vector3 stage3FollowOffset = new Vector3(0f, 1.8f, 0f);

    [Header("Movement Constraints - Current")]
    public bool useMovementConstraints = true;
    private Vector3 minPosition; // 현재 적용 중인 최소 범위
    private Vector3 maxPosition; // 현재 적용 중인 최대 범위
    
    [Header("Movement Constraints - Default View")]
    public Vector3 minPosDefault = new Vector3(-10f, 0f, -10f);
    public Vector3 maxPosDefault = new Vector3(10f, 0f, 10f);

    [Header("Movement Constraints - Side View")]
    public Vector3 minPosSide = new Vector3(-20f, 0f, -5f);
    public Vector3 maxPosSide = new Vector3(20f, 0f, 5f);
    

    public enum CameraMovingType { Fix, Moving, Follow } 
    public CameraMovingType cameraType = CameraMovingType.Fix;

    private Quaternion defaultRotation;       // 왼쪽 시점(기본)
    private Quaternion sideRotation;          // 오른쪽 시점
    private Quaternion targetRotation;        // 목표 회전값
    private Vector3 defaultPosition; // 기본 위치
    private Vector3 targetPosition; // 목표 위치
    private string nowstage;

    private float stage3Distance = 0; //3스테이지 카메라와 플레이어 거리

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
        {
            playerMovement3 = player.GetComponent<PlayerMovement3>();
            //sideViewOffset = transform.position - player.position;
        }
            
        DecideCameraType();
        if (stage3CameraMode) { 
            isSideView = false;
            targetRotation = defaultRotation;
            transform.rotation = defaultRotation;

            SetConstraintsByView(); //위치제한설정
            if (stage3CameraMode && player != null) {   // 기본 시점(yaw 180°)에서 카메라와 플레이어의 Z 거리
            stage3Distance = Mathf.Abs(transform.position.z - player.position.z);
            }
            SnapToPlayer();//플레이어위치로
        
        }
        else
        {
            float currentY = transform.eulerAngles.y;
            if (Mathf.Abs(currentY - 270f) < 45f || Mathf.Abs(currentY + 90f) < 45f)
            {
                isSideView = true;
            }
            else
            {
                isSideView = false;
            }
            targetRotation = isSideView ? sideRotation : defaultRotation;

            SetConstraintsByView();
        }

        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * followSpeed);
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
        // 이미 회전중이 아니고 잠겨있을 때만 입력 차단
        if (!isRotating && InputLockManager.LockCamera)
            return;
        // Tab 키 입력 시 시점 전환
        if (!isRotating &&
        InputManager.instance.GetKeyDown(KeyCode.Tab) &&
        (cameraType == CameraMovingType.Fix ||
         cameraType == CameraMovingType.Moving))
        {
            InputLockManager.CameraRotateLock = true;   //LockMove, LockCamera 잠금

            isSideView = !isSideView;
            targetRotation = isSideView ? sideRotation : defaultRotation;
            isRotating = true;

            // 시점에 따라 제한 범위를 즉시 교체
            SetConstraintsByView();
        }

        // 회전은 Lock이어도 계속 수행
        if (isRotating)
        {
            //// 타이밍 모드면 즉시 회전
            //if (timingMode)
            //{
            //    transform.rotation = targetRotation;
            //    isRotating = false;
            //    if (playerMovement != null)
            //        playerMovement.canMove = true;
            //}
            //else
            //{
            float rotationSpeed = 5f;

            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

            // 회전 완료 판정
            if (Quaternion.Angle(transform.rotation, targetRotation) < 0.1f){
                transform.rotation = targetRotation;
                isRotating = false;
                if (playerMovement != null)
                     playerMovement.canMove = true;
                InputLockManager.CameraRotateLock = false;
            }
            //}
        }

        FollowPlayer();
    }
    void DecideCameraType() {
        //GameData data = DataManager.instance.nowGame;
        //nowstage = data.stage.ToString();  //잠시주석0314
        nowstage = "1";

        defaultPosition = transform.position;
        targetPosition = defaultPosition;

        if (nowstage == "1")
        {
            cameraType = CameraMovingType.Moving;

            defaultRotation = Quaternion.Euler(0, 180, 0);
            sideRotation = Quaternion.Euler(0, 270, 0);
            isSideView = true;
            defaultPosition = transform.position;
            targetPosition = defaultPosition;
        }
        if (stage3CameraMode)
        {
            if (player != null)
            {
                playerMovement = player.GetComponent<PlayerMovement>();

                if (playerMovement != null)
                    playerMovement.enabled = true;
            }

            // Tab 시점 전환 사용
            cameraType = CameraMovingType.Moving;
            isSideView = false;

            // 기본 시점: 위에서 약 20도 내려다보는 시점
            defaultRotation = Quaternion.Euler(20f, 180f, 0f);

            // 측면 시점: Y축으로 90도 전환
            sideRotation = Quaternion.Euler(20f, 270f, 0f);

            defaultPosition = transform.position;
            targetPosition = defaultPosition;
        }
        //else if (nowstage == "2") 
        //{
        //    if (!PipeRun)
        //    {
        //        if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //        else if (isSideView) { cameraType = CameraMovingType.Moving; }

        //        defaultRotation = Quaternion.Euler(0, 180, 0);
        //        sideRotation = Quaternion.Euler(0, 270, 0);
        //        isSideView = false;

        //        defaultPosition = transform.position;
        //        targetPosition = defaultPosition;
        //    }
        //    else
        //    {
        //        cameraType = CameraMovingType.Fix;
        //        Debug.Log("Fix완료!");
        //    }
        //}
        //else if (nowstage == "3") //3스테이지 카메라 조건 정하기
        //{
        //    var playerscript1 = playerObj.GetComponent<PlayerMovement>();
        //    var playerscript3 = playerObj.GetComponent<PlayerMovement3>();
        //    playerscript1.enabled = true;
        //    playerscript3.enabled = false;
        //    //if (!isSideView) { cameraType = CameraMovingType.Fix; }
        //    cameraType = CameraMovingType.Fix;
        //    isSideView = false;
        //    defaultRotation = Quaternion.Euler(0, 180, 0);
        //    sideRotation = Quaternion.Euler(0, 270, 0);


        //    defaultPosition = transform.position;
        //    targetPosition = defaultPosition;
        //    //else if (isSideView) { cameraType = CameraMovingType.Follow; }
        //}
        //targetRotation = isSideView ? sideRotation : defaultRotation;
    }
    public void SnapToPlayer()
    {
        if (player == null)
            return;
        Quaternion yawOnly = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 desiredPosition = player.position + yawOnly * sideViewOffset;

        if (useMovementConstraints)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
            desiredPosition.y = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minPosition.z, maxPosition.z);
        }

        transform.position = desiredPosition;
    }
    void FollowPlayer()
    {
        if (player == null)
            return;
        // 기울기(pitch)를 제외하고 Y축 회전만 사용
        Quaternion yawOnly = Quaternion.Euler(0f, transform.eulerAngles.y, 0f);
        Vector3 rotatedOffset = yawOnly * sideViewOffset;
        Vector3 desiredPosition = player.position + rotatedOffset;
        
        // 움직임 제한 적용
        if (useMovementConstraints)
        {
            desiredPosition.x = Mathf.Clamp(desiredPosition.x, minPosition.x, maxPosition.x);
            desiredPosition.z = Mathf.Clamp(desiredPosition.z, minPosition.z, maxPosition.z);
            if (!stage3CameraMode)
                desiredPosition.y = Mathf.Clamp(desiredPosition.y, minPosition.y, maxPosition.y);
        }
        if (stage3CameraMode)// 3스테이지: X, Y, Z 모두 플레이어를 따라감
        {
            desiredPosition.y = player.position.y + stage3FollowOffset.y; // Clamp 안 함
            if (isSideView)
                desiredPosition.x = player.position.x + stage3Distance;  // 측면: X 거리 유지, Z 따라감
            else
                desiredPosition.z = player.position.z + stage3Distance;  // 기본: Z 거리 유지, X 따라감
        }
        // 부드럽게 이동
        transform.position = Vector3.Lerp(transform.position, desiredPosition, Time.deltaTime * followSpeed);

        // [보정] 끝까지 안 따라가는 문제 해결
        if (Vector3.Distance(transform.position, desiredPosition) < 0.01f)
            transform.position = desiredPosition;
        //Debug.Log($"side={isSideView} player=({player.position.x:F1},{player.position.y:F1},{player.position.z:F1}) desired=({desiredPosition.x:F1},{desiredPosition.y:F1},{desiredPosition.z:F1}) cam=({transform.position.x:F1},{transform.position.y:F1},{transform.position.z:F1})");
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
    // [추가] 시점에 따라 범위를 갈아끼워주는 전용 함수
    void SetConstraintsByView()
    {
        if (isSideView)
        {
            minPosition = minPosSide;
            maxPosition = maxPosSide;
        }
        else
        {
            minPosition = minPosDefault;
            maxPosition = maxPosDefault;
        }
    }
    
    void FreeCircleFollowPlayer()
    {
        if (player == null || playerMovement3 == null || playerMovement3.center == null)
            return;

        // 중심 기준 벡터
        Vector3 center = playerMovement3.center.position;
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
}
