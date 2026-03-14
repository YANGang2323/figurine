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
            transform.rotation = Quaternion.Euler(-90f, targetRotation.eulerAngles.y, 0f);
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
        animator.SetFloat("MoveX", h);
        animator.SetFloat("MoveY", v);
        //animator.SetFloat("MoveX", currentMoveX);
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
            Vector3 newPos = rb.position + moveDir * moveSpeed * Time.fixedDeltaTime;
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
            Quaternion  targetRotation = Quaternion.LookRotation(lookDirection);
            transform.rotation = Quaternion.Euler(-90f, targetRotation.eulerAngles.y, 0f);
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
