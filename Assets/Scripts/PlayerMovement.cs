using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private CapsuleCollider standingCollider;
    [SerializeField] private CapsuleCollider crouchCollider;
    public bool canMove = true;

    public float moveSpeed;
    public Animator animator;
    private Vector3 moveDir = Vector3.zero;
    private Rigidbody rb;
    private bool jumpRequested;

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
        animator = GetComponent<Animator>();
        animator.applyRootMotion = false;
        rb.freezeRotation = true;
        if (!rb.isKinematic)
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        if (standingCollider != null)
            standingCollider.enabled = true;
        if (crouchCollider != null)
            crouchCollider.enabled = false;

        InputLockManager.OnLock += StopMotion;
    }

    void Update()
    {
        if (InputLockManager.LockMove || !canMove)
        {
            moveDir = Vector3.zero;
            jumpRequested = false;
            animator.SetInteger("MoveState", 0);
            return;
        }

        bool sideView = CameraPivotRotator.Instance != null && CameraPivotRotator.Instance.isSideView;
        bool fixedView = CameraPivotRotator.Instance != null && CameraPivotRotator.Instance.UsesMapRotation;
        Vector3 left = sideView && !fixedView ? Vector3.back : Vector3.right;
        moveDir = Vector3.zero;
        if (InputManager.instance.GetKey(KeyCode.A))
            moveDir = left;
        else if (InputManager.instance.GetKey(KeyCode.D))
            moveDir = -left;

        if (canJump && InputManager.instance.GetKeyDown(KeyCode.Space))
            jumpRequested = true;

        bool isMoving = moveDir != Vector3.zero;
        bool isRunning = isMoving && runable && Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
        animator.SetInteger("MoveState", isRunning ? 2 : isMoving ? 1 : 0);
    }

    void FixedUpdate()
    {
        if (rb.isKinematic)
            return;

        bool movementLocked = InputLockManager.LockMove || !canMove;
        Vector3 horizontalVelocity = movementLocked ? Vector3.zero : moveDir * currentSpeed;
        float verticalVelocity = rb.linearVelocity.y;

        if (!movementLocked && canJump && jumpRequested && IsGrounded())
            verticalVelocity = jumpForce;

        jumpRequested = false;

        // Stop before pushing a rounded capsule up a wall or prop.
        if (horizontalVelocity != Vector3.zero &&
            rb.SweepTest(horizontalVelocity.normalized, out RaycastHit hit,
                horizontalVelocity.magnitude * Time.fixedDeltaTime, QueryTriggerInteraction.Ignore) &&
            hit.normal.y < 0.65f)
        {
            float safeDistance = Mathf.Max(0f, hit.distance - Physics.defaultContactOffset);
            horizontalVelocity = horizontalVelocity.normalized * (safeDistance / Time.fixedDeltaTime);
        }

        Vector3 velocity = new Vector3(horizontalVelocity.x, verticalVelocity, horizontalVelocity.z);
        if (rb.linearVelocity != velocity)
            rb.linearVelocity = velocity;
    }

    void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        Vector3 lookDirection = camera.transform.position - transform.position;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude <= 0.001f)
            return;

        float offset = 0f;
        if (!InputLockManager.LockMove && canMove)
        {
            if (InputManager.instance.GetKey(KeyCode.A))
                offset = rotationOffsetA;
            else if (InputManager.instance.GetKey(KeyCode.D))
                offset = rotationOffsetD;
        }

        float yaw = Quaternion.LookRotation(lookDirection).eulerAngles.y;
        transform.rotation = Quaternion.Euler(0f, yaw + offset, 0f);
    }

    void OnDestroy()
    {
        InputLockManager.OnLock -= StopMotion;
    }

    void StopMotion()
    {
        moveDir = Vector3.zero;
        jumpRequested = false;

        if (!rb.isKinematic)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }

    private bool IsGrounded()
    {
        Vector3 origin = rb.position + Vector3.up * 0.1f;
        return Physics.Raycast(origin, Vector3.down, groundCheckDistance + 0.1f,
            jumpableLayers, QueryTriggerInteraction.Ignore);
    }
}
