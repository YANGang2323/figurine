using UnityEngine;

// �÷��̾� ������ ���� ��ũ��Ʈ
// ������ ���� �̵� ������ ���� ����
//1,2��������
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CapsuleCollider standingCollider;
    [SerializeField] private CapsuleCollider crouchCollider;
    private bool isCrouching = false;
    public bool canMove = true;
        
    public float moveSpeed; // �̵� �ӵ�
    public Animator animator;
    float h = 0;
    float v = 0;
    private Vector3 moveDir = Vector3.zero;
    //public float smoothTime = 0.1f; // ��ȯ �ӵ� ����
    //private float currentMoveX = 0f; // ���� �ִϸ����Ϳ� ���� ���� ��

    // ���� ���� ����
    //public float jumpForce = 5f; // ���� ��
    private Rigidbody rb;
    private bool isGrounded = true;

    [Header("Jump Settings")]
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float groundCheckDistance = 0.2f;
    [SerializeField] private LayerMask jumpableLayers;

    // [�߰�] �޸��� �� ȸ�� ���� ����
    [Header("Run & Rotation Settings")]
    public bool runable;
    public float runSpeedMultiplier = 1.5f; // �޸��� �� �ӵ� ���� ����
    private float currentSpeed; // ���� ����Ǵ� ���� �ӵ�
    public float rotationOffsetA = 90f; // AŰ ���� �� �߰� ȸ�� ����
    public float rotationOffsetD = -90f; // DŰ ���� �� �߰� ȸ�� ����


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        Vector3 camDirection = Camera.main.transform.position - transform.position;
        camDirection.y = 0; // ���� ���� ���� �� �¿� ���⸸ ����
        if (camDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camDirection);
            Vector3 currentEuler = transform.eulerAngles;
            // ���� X, Z�� �����ϰ� Y�� ī�޶� �������� ����
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
        if (!canMove)
            return;

        moveDir = Vector3.zero;
        h = 0;
        v = 0;

        // [�߰�] �� ������ �⺻ �ӵ��� �ʱ�ȭ
        currentSpeed = moveSpeed;

        // ����1: ���� ����(�⺻) �� �¿�(A, D) �̵��� ���
        if (!CameraPivotRotator.Instance.isSideView)
        {
            if (InputManager.instance.GetKey(KeyCode.A))
            { // ���� ������ �ݴ� ����
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
        // ����2: ������ ����(����) �� ����Ű(������, ����) �̵��� ���
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
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z); // ���� ���� �����ӵ� �ʱ�ȭ
            rb.AddForce(Vector3.up * jumpForce, ForceMode.VelocityChange);
        }
        //����Ű �ݶ��̴� ����
        if (Input.GetKey(KeyCode.LeftControl))
        {
            EnterCrouch();
        }
        else
        {
            ExitCrouch();
        }

        //�ִϸ��̼� MoveState ���� ����
        int currentState = 0; // �⺻ ä���� 0�� (���)
        
        if (moveDir != Vector3.zero) // ����Ű�� ������ �̵� ���� ��
        {
            currentState = 1; // �ϴ� 1�� (�ȱ�)

            // ���� ���� Shift Ű�� ���� ���·� �̵� ���̶��?
            if (Input.GetKey(KeyCode.LeftShift) & runable == true)
            {
                currentState = 2; // 2�� (�޸���)
                currentSpeed = moveSpeed * runSpeedMultiplier; // [�߰�] �޸��� ���� �� �ӵ� ���� ����
            }
            // ���� Ctrl Ű�� ���� ���� (���帮��)+ ������
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                currentState = 4;
                currentSpeed = moveSpeed * 0.5f;
            }
        }
        else // ���帮�� ����Ű�� ������, �������� ��
        {
            // ���帲(3��) 
            if (Input.GetKey(KeyCode.LeftControl))
            {
                currentState = 3;
            }
            else
            {
                currentState = 0; // �ƹ��͵� �� ������ 0�� (���)
            }
        }
        animator.SetInteger("MoveState", currentState);

        // ���� �̵�
        //transform.Translate(moveDir * moveSpeed * Time.deltaTime, Space.World);

        ////���� �Է� ó��
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
            // [����] moveSpeed ��� currentSpeed(�޸��� ���� �ݿ��� �ӵ�) ���
            Vector3 newPos = rb.position + moveDir * currentSpeed * Time.fixedDeltaTime;
            rb.MovePosition(newPos);
        }
    }

    void LateUpdate()
    {
        //transform.LookAt(Camera.main.transform);
        Vector3 lookDirection = Camera.main.transform.position - transform.position;
        lookDirection.y = 0; // y�� ���� �����ؼ� ���� ���⸸ �ٶ󺸰�

        if (lookDirection.sqrMagnitude > 0.001f) // ���� ���Ͱ� �ʹ� ���� ������ Ȯ��
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection);

            // [�߰�] A �Ǵ� D Ű�� ������ ���� �� �߰� ȸ���� ����
            float yOffset = 0f;
            if (InputManager.instance.GetKey(KeyCode.A))
            {
                yOffset = rotationOffsetA; // �⺻ 90��
            }
            else if (InputManager.instance.GetKey(KeyCode.D))
            {
                yOffset = rotationOffsetD; // �⺻ -90��
            }

            // [����] ī�޶� �ٶ󺸴� ȸ������ yOffset�� ���Ͽ� ���� ȸ�� ����
            transform.rotation = Quaternion.Euler(0f, targetRotation.eulerAngles.y + yOffset, 0f);
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