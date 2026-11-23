using UnityEngine;

// �÷��̾� ������ ���� ��ũ��Ʈ
// ������ ���� �̵� ������ ���� ����
//3��������
public class PlayerMovement3 : MonoBehaviour
{
    public float moveSpeed; // �̵� �ӵ�
    public Animator animator;
    float h = 0;
    float v = 0;
    //public float smoothTime = 0.1f; // ��ȯ �ӵ� ����
    //private float currentMoveX = 0f; // ���� �ִϸ����Ϳ� ���� ���� ��
    private string CameraType;

    // ���� ���� ����
    private Rigidbody rb;


    private Vector3 moveDir = Vector3.zero;

    void Awake() { 
        rb = GetComponent<Rigidbody>();
        Vector3 camDirection = Camera.main.transform.position - transform.position;
        camDirection.y = 0; // ���� ���� ���� �� �¿� ���⸸ ����

        if (camDirection != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(camDirection);
            Vector3 currentEuler = transform.eulerAngles;
            // ���� X, Z�� �����ϰ� Y�� ī�޶� �������� ����
            transform.rotation = Quaternion.Euler(currentEuler.x, targetRotation.eulerAngles.y, currentEuler.z);
        }
    }

    void Start()
    {
        animator = GetComponent<Animator>();
        // �ʱ� ��ġ ���� (��� ���� radius �Ÿ��� ������ ��ġ)
        Vector3 startPos = new Vector3(-11.5f, 0f, 6f);
        transform.position = startPos;

        // Rigidbody ����
        rb.freezeRotation = true;

        // currentAngle�� currentHeight �ʱ�ȭ
        Vector3 dir = startPos - center.position;
        currentAngle = Mathf.Atan2(dir.z, dir.x) * Mathf.Rad2Deg;
        currentHeight = startPos.y;
    }

    void Update()
    {
        
        h = 0;
        v = 0;


        // ���� �̵�
        if (CameraPivotRotator.Instance.cameraType == CameraPivotRotator.CameraMovingType.Fix)
        {
            Debug.Log("�Ƚ�ī�޶� ������");
            FixMove();
            rb.MovePosition(rb.position + moveDir * moveSpeed * Time.fixedDeltaTime);
        }
        //else if (CameraPivotRotator.Instance.cameraType == CameraPivotRotator.CameraMovingType.Follow)
        //{
        //    Debug.Log("����������");
        //    UpCircular();
        //}
        // �ε巴�� ��ȯ
        //currentMoveX = Mathf.Lerp(currentMoveX, moveX, Time.deltaTime / smoothTime);
        // �ִϸ����Ϳ� ����
        //animator.SetFloat("MoveX", h);
        //animator.SetFloat("MoveY", v);
        //animator.SetFloat("MoveX", currentMoveX);
        

    }
    void FixedUpdate()
    {
        if (CameraPivotRotator.Instance.cameraType == CameraPivotRotator.CameraMovingType.Follow)
        {
            //Debug.Log("����������");
            UpCircular();
            animator.SetFloat("MoveX", h);
            animator.SetFloat("MoveY", v);
        }
        CheckForwardCollision();
        //CheckSideCollision();
    }

    public Transform center;       // �߽��� (����� �߽�)
    public float radius;      // ȸ�� ������
    public float angularSpeed;   // ȸ�� �ӵ� (��/��)
    public float heightSpeed;     // �ö󰡴� �ӵ� (����/��)

    private float currentAngle = 0f;
    private float currentHeight = 0f;

    [SerializeField] private float gravity; // �߷� ���ϰ� ����
    private float verticalSpeed = 0f;

    void UpCircular()
    {
        moveDir = Vector3.zero;

        float horizontalInput = 0f;

        if (InputManager.instance.GetKey(KeyCode.A))
            horizontalInput = 1f;
        else if (InputManager.instance.GetKey(KeyCode.D))
            horizontalInput = -1f;

        // �� �� �������� �浹 üũ
        Vector3 centerToPlayer = transform.position - center.position;
        Vector3 tangentDir = Vector3.Cross(Vector3.up, centerToPlayer).normalized;
        Vector3 rayOrigin = transform.position + Vector3.up * 0.5f;
        float rayDistance = 0.3f;

        // ȸ�� ���⿡ ���� ���� ���� ����
        Vector3 rayDir = (horizontalInput > 0) ? tangentDir : -tangentDir;

        if (horizontalInput != 0 && Physics.Raycast(rayOrigin, rayDir, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            
            //horizontalInput = 0f;  // �浹 �� ȸ�� ����
        }

        // ?? �� ���Ŀ� ���� �����ؾ� ��!! 
        currentAngle -= horizontalInput * angularSpeed * Time.deltaTime;

        // ���� ó��
        // ��ġ ���
        float rad = currentAngle * Mathf.Deg2Rad;
        float x = center.position.x + radius * Mathf.Cos(rad);
        float z = center.position.z + radius * Mathf.Sin(rad);
        float y = transform.position.y;
        Vector3 newXZPosition = new Vector3(x, y, z);
        Vector3 moveVector = newXZPosition - transform.position;
        rb.MovePosition(transform.position + new Vector3(moveVector.x, 0, moveVector.z));

        // ���� ����
        if (horizontalInput > 0)
            transform.forward = tangentDir;
        else if (horizontalInput < 0)
            transform.forward = -tangentDir;

        h = 0;
        v = (horizontalInput != 0) ? 1 : 0;
    }

    void FixMove()
    {
        // �ʱ�ȭ
        moveDir = Vector3.zero;

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

        // �ٶ󺸴� ���� ����
        if (moveDir != Vector3.zero)
            transform.forward = moveDir;

        // �ִϸ����� ������
        h = Input.GetAxis("Horizontal");
        v = Input.GetAxis("Vertical");

    }

        void LateUpdate()
    {
        
    }
    bool frontBlocked = false;  

    private void CheckForwardCollision() //���� ����ĳ��Ʈ
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f; // �÷��̾� ������ ����
        Vector3 direction = transform.forward;
        float rayDistance = 0.2f;

        frontBlocked = Physics.Raycast(origin, direction, rayDistance);
        if (Physics.Raycast(origin, transform.forward, out RaycastHit frontHit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("���� �浹: " + frontHit.collider.gameObject.name);
        }
        Debug.DrawRay(origin, direction * rayDistance, Color.red); // Scene���� �ð�ȭ
    }
    private void CheckSideCollision() //�¿� ����ĳ��Ʈ
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;
        float rayDistance = 0.2f;

        // ����
        if (Physics.Raycast(origin, -transform.right, out RaycastHit leftHit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("���� �浹: " + leftHit.collider.gameObject.name);
            
        }

        // ����
        if (Physics.Raycast(origin, transform.right, out RaycastHit rightHit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
        {
            Debug.Log("���� �浹: " + rightHit.collider.gameObject.name);
        }

        Debug.DrawRay(origin, -transform.right * rayDistance, Color.green);
        Debug.DrawRay(origin, transform.right * rayDistance, Color.blue);
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log("�浹 �߻�: " + collision.gameObject.name);
    }
    void OnTriggerEnter(Collider other)
    {
        Debug.Log("Ʈ���� �浹: " + other.name);
    }

}
