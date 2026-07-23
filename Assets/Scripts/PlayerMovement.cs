using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CapsuleCollider standingCollider;
    [SerializeField] private CapsuleCollider crouchCollider;
    private bool isCrouching = false;
    public bool canMove = true;
        
    public float moveSpeed;
    public Animator animator;
    float h = 0;
    float v = 0;
    private Vector3 moveDir = Vector3.zero;
    private Rigidbody rb;
    private bool isGrounded = true;

    [SerializeField] private float obstacleCheckDistance = 0.2f;
    [SerializeField] private LayerMask obstacleLayers;

    [Header("Jump Settings")]
    public bool canJump = true;
    

    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask jumpableLayers;

    [Header("Run & Rotation Settings")]
    public bool runable;
    public float runSpeedMultiplier = 1.5f; 
    private float currentSpeed; 
    public float rotationOffsetA = 90f;
    public float rotationOffsetD = -90f; 


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        InputLockManager.OnLock += StopMotion;
        Vector3 camDirection = Camera.main.transform.position - transform.position;
        camDirection.y = 0; 
        if (camDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camDirection);
            Vector3 currentEuler = transform.eulerAngles;
            transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y, 0f);
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        standingCollider.enabled = true;
        crouchCollider.enabled = false;
    }

    void Update()
    {
        if (InputLockManager.LockMove)
        {
            moveDir = Vector3.zero;
            currentSpeed = 0f;
            animator.SetInteger("MoveState", 0);
            return;
        }

        moveDir = Vector3.zero;
        h = 0;
        v = 0;

        currentSpeed = moveSpeed;

        if (!CameraPivotRotator.Instance.isSideView)
        {
            if (InputManager.instance.GetKey(KeyCode.A))
            { 
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

        if (canJump &&
        isGrounded &&
        InputManager.instance.GetKeyDown(KeyCode.Space))
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            EnterCrouch();
        }
        else
        {
            ExitCrouch();
        }

        int currentState = 0;
        
        if (moveDir != Vector3.zero)
        {
            currentState = 1;

            if (Input.GetKey(KeyCode.LeftShift) & runable == true)
            {
                currentState = 2;
                currentSpeed = moveSpeed * runSpeedMultiplier; 
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                currentState = 4;
                currentSpeed = moveSpeed * 0.5f;
            }
        }
        else
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                currentState = 3;
            }
            else
            {
                currentState = 0; 
            }
        }
        animator.SetInteger("MoveState", currentState);
    }

    void FixedUpdate()
    {
        if (InputLockManager.LockMove)
            return;
        if (moveDir == Vector3.zero)
            return;

        // 점프가 불가능한 플레이어만 벽 체크
        if (!canJump)
        {
            Vector3 origin1 = rb.position + Vector3.up * 0.2f;
            Vector3 origin2 = rb.position + Vector3.up * 0.8f;

            bool blocked =
                Physics.Raycast(origin1, moveDir.normalized, obstacleCheckDistance, obstacleLayers) ||
                Physics.Raycast(origin2, moveDir.normalized, obstacleCheckDistance, obstacleLayers);

            if (blocked)
                return;
        }

        Vector3 newPos = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
        rb.MovePosition(newPos);
    }

    void LateUpdate()
    {
        //transform.LookAt(Camera.main.transform);
        Vector3 lookDirection = Camera.main.transform.position - transform.position;
        lookDirection.y = 0; 

        if (lookDirection.sqrMagnitude > 0.001f) 
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            float yOffset = 0f;
            if (!InputLockManager.LockMove)
            {
                if (InputManager.instance.GetKey(KeyCode.A))
                {
                    yOffset = rotationOffsetA; // 90도
                }
                else if (InputManager.instance.GetKey(KeyCode.D))
                {
                    yOffset = rotationOffsetD; // -90도
                }
            }

            transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y + yOffset, 0f);
        }
    }

    void OnDestroy()
    {
        InputLockManager.OnLock -= StopMotion;
    }

    void StopMotion()
    {
        moveDir = Vector3.zero;

        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        Vector3 lookDirection = Camera.main.transform.position - transform.position;
        lookDirection.y = 0f;

        if (lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }

    void EnterCrouch(){
        if (isCrouching) return;
        isCrouching = true;

        standingCollider.enabled = false;
        crouchCollider.enabled = true;
    }
    void ExitCrouch(){
        if (!isCrouching) return;
        isCrouching = false;

        crouchCollider.enabled = false;
        standingCollider.enabled = true;
    }
    private void CheckGrounded()
    {
        Vector3 origin = transform.position + Vector3.up * 0.1f; // �ణ ������ ��
        isGrounded = Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f, jumpableLayers);
    }

    ////�ٴ� ���� ����
    //void OnCollisionEnter(Collision collision)
    //{
    //    if (collision.gameObject.CompareTag("Ground"))
    //    {
    //        isGrounded = true;
    //    }
    //}
}