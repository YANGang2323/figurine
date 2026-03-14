using UnityEngine;

// 플레이어 움직임 제어 스크립트
// 시점에 따라 이동 가능한 방향 제한
//3스테이지
public class PlayerMovement3 : MonoBehaviour
{
    public float moveSpeed; // 이동 속도
    public Animator animator;
    float h = 0;
    float v = 0;
    //public float smoothTime = 0.1f; // 전환 속도 조절
    //private float currentMoveX = 0f; // 현재 애니메이터에 적용 중인 값
    private string CameraType;

    // 점프 관련 변수
    //public float jumpForce = 5f; // 점프 힘
    private Rigidbody rb;
    private bool isGrounded = true;


    [Header("Jump Settings")]
    [SerializeField] private float jumpForce;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask jumpableLayers;
    private Vector3 moveDir = Vector3.zero;

    void Awake() { 
        rb = GetComponent<Rigidbody>();
        Vector3 camDirection = Camera.main.transform.position - transform.position;
        camDirection.y = 0; // 수직 방향 제거 → 좌우 방향만 고려

        if (camDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camDirection);
            Vector3 currentEuler = transform.eulerAngles;
            // 현재 X, Z는 유지하고 Y만 카메라 방향으로 설정
            transform.rotation = Quaternion.Euler(currentEuler.x, targetRotation.eulerAngles.y, currentEuler.z);
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        // 초기 위치 설정 (등대 기준 radius 거리의 적절한 위치)
        Vector3 startPos = new Vector3(-11.5f, 0f, 6f);
        transform.position = startPos;

        // Rigidbody 설정
        rb.freezeRotation = true;

        // currentAngle과 currentHeight 초기화
        Vector3 dir = startPos - center.position;
        currentAngle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        currentHeight = startPos.y;
    }

    void Update()
    {
        
        h = 0;
        v = 0;


        // 실제 이동
        if (CameraPivotRotator.Instance.cameraType == CameraPivotRotator.CameraMovingType.Fix)
        {
            Debug.Log("픽스카메라 움직임");
            FixMove();
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
        //else if (CameraPivotRotator.Instance.cameraType == CameraPivotRotator.CameraMovingType.Follow)
        //{
        //    Debug.Log("원형움직임");
        //    UpCircular();
        //}
        // 부드럽게 전환
        //currentMoveX = Mathf.Lerp(currentMoveX, moveX, Time.deltaTime / smoothTime);
        // 애니메이터에 적용
        //animator.SetFloat("MoveX", h);
        //animator.SetFloat("MoveY", v);
        //animator.SetFloat("MoveX", currentMoveX);
        

    }
    void FixedUpdate()
    {
        if (CameraPivotRotator.Instance.cameraType == CameraPivotRotator.CameraMovingType.Follow)
        {
            //Debug.Log("원형움직임");
            UpCircular();
            animator.SetFloat("MoveX", h);
            animator.SetFloat("MoveY", v);
        }
        CheckForwardCollision();
        //CheckSideCollision();
    }

    public Transform center;       // 중심축 (원기둥 중심)
    public float radius;      // 회전 반지름
    public float angularSpeed;   // 회전 속도 (도/초)
    public float heightSpeed;     // 올라가는 속도 (유닛/초)

    private float currentAngle = 0f;
    private float currentHeight = 0f;

    [SerializeField] private float gravity; // 중력 강하게 설정
    private float verticalSpeed = 0f;

    void UpCircular()
    {
        moveDir = Vector3.zero;

        float horizontalInput = 0f;

        if (InputManager.instance.GetKey(KeyCode.A))
            horizontalInput = 1f;
        else if (InputManager.instance.GetKey(KeyCode.D))
            horizontalInput = -1f;

        // → 이 시점에서 충돌 체크
        Vector3 centerToPlayer = transform.position - center.position;
        Vector3 tangentDir = Vector3.Cross(Vector3.up, centerToPlayer).normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float rayDistance = 0.3f;

        // 회전 방향에 따라 레이 방향 결정
        Vector3 rayDir = (horizontalInput > 0) ? tangentDir : -tangentDir;

        if (horizontalInput != 0 && Physics.Raycast(rayOrigin, rayDir, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            
            //horizontalInput = 0f;  // 충돌 시 회전 차단
        }

        // ?? 이 이후에 각도 갱신해야 함!! 
        currentAngle -= horizontalInput * angularSpeed * Time.deltaTime;

        // 점프 처리
        CheckGrounded();
        if (isGrounded && InputManager.instance.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        // 위치 계산
        float rad = currentAngle * Mathf.Deg2Rad;
        float x = center.position.x + radius * Mathf.Cos(rad);
        float z = center.position.z + radius * Mathf.Sin(rad);
        float y = transform.position.y;
        Vector3 newXZPosition = new Vector3(x, y, z);
        Vector3 moveVector = newXZPosition - transform.position;
        rb.MovePosition(transform.position + new Vector3(moveVector.x, 0, moveVector.z));

        // 방향 고정
        if (horizontalInput > 0)
            transform.forward = tangentDir;
        else if (horizontalInput < 0)
            transform.forward = -tangentDir;

        h = 0;
        v = (horizontalInput != 0) ? 1 : 0;
    }

    void FixMove()
    {
        // 초기화
        moveDir = Vector3.zero;

        CheckGrounded();

        if (isGrounded && InputManager.instance.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // 점프 전에 수직속도 초기화
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }

        if (!CameraPivotRotator.Instance.isSideView)
        {
            if (InputManager.instance.GetKey(KeyCode.A))
            {
                moveDir = Vector3.right;
            }
            else if (InputManager.instance.GetKey(KeyCode.D))
            {
                moveDir = Vector3.left;
            }
        }
        else
        {
            if (InputManager.instance.GetKey(KeyCode.A))
            {
                moveDir = Vector3.back;
            }
            else if (InputManager.instance.GetKey(KeyCode.D))
            {
                moveDir = Vector3.forward;
            }
        }

        // 바라보는 방향 적용
        if (moveDir != Vector3.zero)
            transform.forward = moveDir;

        // 애니메이터 연동용
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

    }

        void LateUpdate()
    {
        
    }
    private void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f; // 약간 위에서 쏨
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, jumpableLayers);
    }

    bool frontBlocked = false;  

    private void CheckForwardCollision() //전방 레이캐스트
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f; // 플레이어 몸통쯤 높이
        Vector3 direction = transform.forward;
        float rayDistance = 0.2f;

        frontBlocked = Physics.Raycast(origin, direction, rayDistance);
        if (Physics.Raycast(origin, transform.forward, out RaycastHit frontHit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("전방 충돌: " + frontHit.collider.gameObject.name);
        }
        Debug.DrawRay(origin, direction * rayDistance, Color.red); // Scene에서 시각화
    }
    private void CheckSideCollision() //좌우 레이캐스트
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        float rayDistance = 0.2f;

        // 좌측
        if (Physics.Raycast(origin, -transform.right, out RaycastHit leftHit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("좌측 충돌: " + leftHit.collider.gameObject.name);
            
        }

        // 우측
        if (Physics.Raycast(origin, transform.right, out RaycastHit rightHit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("우측 충돌: " + rightHit.collider.gameObject.name);
        }

        Debug.DrawRay(origin, -transform.right * rayDistance, Color.green);
        Debug.DrawRay(origin, transform.right * rayDistance, Color.blue);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("충돌 발생: " + collision.gameObject.name);
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("트리거 충돌: " + other.name);
    }

}
