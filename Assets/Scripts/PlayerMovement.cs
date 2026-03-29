using UnityEngine;

// 플레이어 움직임 제어 스크립트
// 시점에 따라 이동 가능한 방향 제한
//1,2스테이지
public class PlayerMovement : MonoBehaviour
{
    public float moveSpeed; // 이동 속도
    public Animator animator;
    float h = 0;
    float v = 0;
    private Vector3 moveDir = Vector3.zero;
    //public float smoothTime = 0.1f; // 전환 속도 조절
    //private float currentMoveX = 0f; // 현재 애니메이터에 적용 중인 값

    // 점프 관련 변수
    //public float jumpForce = 5f; // 점프 힘
    private Rigidbody rb;
    private bool isGrounded = true;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask jumpableLayers;

    // [추가] 달리기 및 회전 설정 변수
    [Header("Run & Rotation Settings")]
    public float runSpeedMultiplier = 1.5f; // 달리기 시 속도 증가 배율
    private float currentSpeed; // 현재 적용되는 실제 속도
    public float rotationOffsetA = 90f; // A키 누를 때 추가 회전 각도
    public float rotationOffsetD = -90f; // D키 누를 때 추가 회전 각도


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Vector3 camDirection = Camera.main.transform.position - transform.position;
        camDirection.y = 0; // 수직 방향 제거 → 좌우 방향만 고려
        if (camDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camDirection);
            Vector3 currentEuler = transform.eulerAngles;
            // 현재 X, Z는 유지하고 Y만 카메라 방향으로 설정
            transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
    }

    void Update()
    {
        moveDir = Vector3.zero;
        h = 0;
        v = 0;

        // [추가] 매 프레임 기본 속도로 초기화
        currentSpeed = moveSpeed;

        // 시점1: 왼쪽 시점(기본) → 좌우(A, D) 이동만 허용
        if (!CameraPivotRotator.Instance.isSideView)
        {
            if (InputManager.instance.GetKey(KeyCode.A))
            { // 시점 때문에 반대 상태
                moveDir = Vector3.right;
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
            else if (InputManager.instance.GetKey(KeyCode.D))
            {
                moveDir = Vector3.left;
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
        }
        // 시점2: 오른쪽 시점(측면) → 방향키(오른쪽, 왼쪽) 이동만 허용
        else
        {
            if (InputManager.instance.GetKey(KeyCode.A))
            {
                moveDir = Vector3.back;
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
            else if (InputManager.instance.GetKey(KeyCode.D))
            {
                moveDir = Vector3.forward;
                h = Input.GetAxis("Horizontal");
                v = Input.GetAxis("Vertical");
            }
        }
        CheckGrounded();

        if (isGrounded && InputManager.instance.GetKeyDown(KeyCode.Space))
        {
            rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z); // 점프 전에 수직속도 초기화
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        // 부드럽게 전환
        //currentMoveX = Mathf.Lerp(currentMoveX, moveX, Time.deltaTime / smoothTime);
        // 애니메이터에 적용
        //animator.SetFloat("MoveX", h);
        //animator.SetFloat("MoveY", v);
        //animator.SetFloat("MoveX", currentMoveX);

        //애니메이션 MoveState 상태 변경
        int currentState = 0; // 기본 채널은 0번 (대기)

        if (moveDir != Vector3.zero) // 방향키를 눌러서 이동 중일 때
        {
            currentState = 1; // 일단 1번 (걷기)

            // 만약 왼쪽 Shift 키를 누른 상태로 이동 중이라면?
            if (Input.GetKey(KeyCode.LeftShift))
            {
                currentState = 2; // 2번 (달리기)
                currentSpeed = moveSpeed * runSpeedMultiplier; // [추가] 달리기 중일 때 속도 배율 적용
            }
            // 만약 Ctrl 키를 누른 상태로 이동 중이라면?
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                currentState = 3; // 3번 (기어가기)
            }
        }
        else // 방향키를 안 누르고 멈춰있을 때
        {
            // 제자리에 멈춰서 Ctrl키를 누르면 제자리 엎드리기(3번) 유지
            if (Input.GetKey(KeyCode.LeftControl))
            {
                currentState = 3;
            }
            else
            {
                currentState = 0; // 아무것도 안 누르면 0번 (대기)
            }
        }
        animator.SetInteger("MoveState", currentState);

        // 실제 이동
        //transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        ////점프 입력 처리
        //if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        //{
        //    rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        //    isGrounded = false;
        //}
    }

    void FixedUpdate()
    {
        if (moveDir != Vector3.zero)
        {
            // [수정] moveSpeed 대신 currentSpeed(달리기 상태 반영된 속도) 사용
            Vector3 newPos = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    void LateUpdate()
    {
        //transform.LookAt(Camera.main.transform);
        Vector3 lookDirection = Camera.main.transform.position - transform.position;
        lookDirection.y = 0; // y축 방향 제거해서 수평 방향만 바라보게

        if (lookDirection.sqrMagnitude > 0.001f) // 방향 벡터가 너무 작지 않은지 확인
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // [추가] A 또는 D 키를 누르고 있을 때 추가 회전값 설정
            float yOffset = 0f;
            if (InputManager.instance.GetKey(KeyCode.A))
            {
                yOffset = rotationOffsetA; // 기본 90도
            }
            else if (InputManager.instance.GetKey(KeyCode.D))
            {
                yOffset = rotationOffsetD; // 기본 -90도
            }

            // [수정] 카메라를 바라보는 회전값에 yOffset을 더하여 최종 회전 적용
            transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y + yOffset, 0f);
        }
    }

    private void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f; // 약간 위에서 쏨
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, jumpableLayers);
    }
    ////바닥 착지 감지
    //void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Ground"))
    //    {
    //        isGrounded = true;
    //    }
    //}
}