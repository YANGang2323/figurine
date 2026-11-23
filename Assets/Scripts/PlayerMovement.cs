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
    private bool facingInitialized;

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
            animator.SetInteger("MoveState", 0);
            return;
        }

        bool fixedView = CameraPivotRotator.Instance != null && CameraPivotRotator.Instance.UsesMapRotation;
        Vector3 left = !fixedView && CameraPivotRotator.Instance != null
            ? Quaternion.Euler(0f, 90f * (int)CameraPivotRotator.Instance.CurrentViewDirection, 0f) * Vector3.right
            : Vector3.right;
        moveDir = Vector3.zero;
        if (InputManager.instance.GetKey(KeyCode.A))
            moveDir = left;
        else if (InputManager.instance.GetKey(KeyCode.D))
            moveDir = -left;

        bool isMoving = moveDir != Vector3.zero;
        bool isRunning = isMoving && runable && Input.GetKey(KeyCode.LeftShift);
        currentSpeed = isRunning ? moveSpeed * runSpeedMultiplier : moveSpeed;
    }

    void FixedUpdate()
    {
        if (rb.isKinematic)
            return;

        bool movementLocked = InputLockManager.LockMove || !canMove;
        Vector3 horizontalVelocity = movementLocked ? Vector3.zero : moveDir * currentSpeed;
        float verticalVelocity = rb.linearVelocity.y;

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
        bool actuallyMoving = horizontalVelocity.sqrMagnitude > 0.0001f;
        animator.SetInteger("MoveState", actuallyMoving ? (currentSpeed > moveSpeed ? 2 : 1) : 0);
    }

    void LateUpdate()
    {
        Camera camera = Camera.main;
        if (camera == null)
            return;

        // Orthographic screen direction is independent of the lens position.
        // Keep the last facing direction when idle instead of snapping at the lens.
        if (!facingInitialized)
        {
            Vector3 initialFacing = Vector3.ProjectOnPlane(-camera.transform.forward, Vector3.up);
            if (initialFacing.sqrMagnitude > 0.001f)
                transform.rotation = Quaternion.LookRotation(initialFacing, Vector3.up);
            facingInitialized = true;
        }
        if (!InputLockManager.LockMove && canMove && moveDir.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(moveDir, Vector3.up);
    }

    void OnDestroy()
    {
        InputLockManager.OnLock -= StopMotion;
    }

    void StopMotion()
    {
        moveDir = Vector3.zero;

        if (!rb.isKinematic)
            rb.linearVelocity = new Vector3(0f, rb.linearVelocity.y, 0f);
    }
}
