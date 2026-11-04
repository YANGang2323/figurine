using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
[DefaultExecutionOrder(50)] // Process Tab in CameraPivotRotator before judging a hit this frame.
public class FallingLightHazard : MonoBehaviour
{
    public FallingLightDirector director;
    public Transform lightObject;
    public Transform checkpoint;
    public bool teachTab;
    [Header("Timing and projected danger zone")]
    public float shakeDuration = 1.2f;
    public float shakeAngle = 8f;
    public float alignmentWidth = 0.75f;
    public float gravity = 9.8f;
    public float tutorialPauseAfter = 0.12f;
    public float floorY = -0.35f;
    [Min(0)] public float hitHeightBelowHead = 1f;
    [Min(0)] public float minimumReactionTime = 0.55f;
    public enum State { Ready, Shaking, WaitingBelow, Falling, Tutorial, Landed, Failed }
    public State CurrentState { get; private set; }
    public bool EntrySideView { get; private set; }
    Vector3 homePosition;
    Quaternion homeRotation;
    Vector3 fallRight;
    bool fallSideView;
    float elapsed;
    bool headChecked;
    bool frameRequested;
    bool hasDodged;
    CameraPivotRotator subscribedPivot;
    Renderer[] renderers;

    void Awake()
    {
        homePosition = lightObject.position;
        homeRotation = lightObject.rotation;
        renderers = lightObject.GetComponentsInChildren<Renderer>();
    }

    public Bounds LightBounds
    {
        get
        {
            Bounds bounds = new Bounds(lightObject.position, Vector3.zero);
            if (renderers == null) renderers = lightObject.GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0) bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }
    }

    void OnTriggerEnter(Collider other) => TryArm(other);
    void OnTriggerStay(Collider other) => TryArm(other);

    public void TryArm(Collider other)
    {
        Transform player = other.attachedRigidbody != null ? other.attachedRigidbody.transform : other.transform;
        if (CurrentState != State.Ready || director.Busy ||
            InputLockManager.DialogueLock || !player.CompareTag("Player")) return;
        if (!director.TryClaim(this, player)) return;
        EntrySideView = director.Pivot.isSideView;
        fallSideView = EntrySideView;
        fallRight = director.ViewCamera.transform.right;
        subscribedPivot = director.Pivot;
        subscribedPivot.ViewTurnStarted += OnViewTurnStarted;
        CurrentState = State.Shaking;
        elapsed = 0;
    }

    void Update()
    {
        if (director.Busy || InputLockManager.ESCMenuLock) return;
        if (CurrentState == State.Shaking)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shakeDuration);
            lightObject.rotation = homeRotation * Quaternion.Euler(0, 0,
                Mathf.Sin(t * Mathf.PI * 4) * shakeAngle * Mathf.Sin(t * Mathf.PI));
            if (t >= 1) { lightObject.rotation = homeRotation; CurrentState = State.WaitingBelow; }
        }
        if (CurrentState == State.WaitingBelow)
        {
            // A view change cannot postpone a warning that already belongs to this approach.
            if (IsPlayerBelow(fallRight))
            {
                CurrentState = State.Falling;
                elapsed = 0;
            }
        }
        if (CurrentState == State.Tutorial)
        {
            if (hasDodged || director.Pivot.isSideView != fallSideView)
            {
                director.HideTutorial(true);
                hasDodged = true;
                CurrentState = State.Falling;
            }
            else return;
        }
        if (CurrentState != State.Falling) return;

        if (director.Pivot.isSideView != fallSideView) hasDodged = true;
        if (!frameRequested && hasDodged)
        {
            frameRequested = true;
            director.FrameLight(this);
        }
        elapsed += Time.deltaTime;
        lightObject.position = homePosition + Vector3.down * (0.5f * gravity * elapsed * elapsed);
        // A low fixture can reach the head before the normal pause delay.
        // Always show the first tutorial BEFORE evaluating a hit.
        if (teachTab && !director.TutorialSeen &&
            (elapsed >= tutorialPauseAfter || LightBounds.min.y <= director.PlayerHeadHeight + 0.1f))
        {
            director.ShowTutorial();
            CurrentState = State.Tutorial;
            return;
        }
        if (teachTab && !director.TutorialSeen) return;

        float hitHeight = director.PlayerHeadHeight - hitHeightBelowHead;
        if (!headChecked && elapsed >= minimumReactionTime && LightBounds.min.y <= hitHeight)
        {
            headChecked = true;
            // Starting a Tab turn is enough; do not wait for a rotation angle.
            // frameRequested latches the successful dodge for the rest of this fall.
            bool changedView = hasDodged;
            bool stillBelow = IsPlayerBelow(fallRight);
            if (!changedView && stillBelow)
            {
                CurrentState = State.Failed;
                UnsubscribeTurn();
                director.Hit(this);
                return;
            }
        }
        var bounds = LightBounds;
        if (bounds.min.y <= floorY && (headChecked || elapsed >= minimumReactionTime))
        {
            lightObject.position += Vector3.up * (floorY - bounds.min.y);
            CurrentState = State.Landed;
            UnsubscribeTurn();
            director.Finished(this);
        }
    }

    void OnViewTurnStarted()
    {
        if (CurrentState == State.Ready || CurrentState == State.Failed || CurrentState == State.Landed) return;
        hasDodged = true;
        if (CurrentState == State.Tutorial)
        {
            director.HideTutorial(true);
            CurrentState = State.Falling;
        }
        // Only framing changes on Tab; never reset or stop the falling clock.
        if (CurrentState == State.Falling && !frameRequested)
        {
            frameRequested = true;
            director.FrameLight(this);
        }
    }

    void UnsubscribeTurn()
    {
        if (subscribedPivot != null) subscribedPivot.ViewTurnStarted -= OnViewTurnStarted;
        subscribedPivot = null;
    }

    void OnDisable() => UnsubscribeTurn();

    bool IsPlayerBelow(Vector3 right)
    {
        // Use the visible widths, not just the two pivot points. Nearby scenery
        // can keep the player's center slightly off the light's center line.
        Vector3 axis = new Vector3(Mathf.Abs(right.x), Mathf.Abs(right.y), Mathf.Abs(right.z));
        Bounds lightBounds = LightBounds;
        float radius = Vector3.Dot(lightBounds.extents, axis);
        float playerRadius = 0;
        foreach (var collider in director.Player.GetComponents<Collider>())
            if (collider.enabled && !collider.isTrigger)
                playerRadius = Mathf.Max(playerRadius, Vector3.Dot(collider.bounds.extents, axis));
        return Mathf.Abs(Vector3.Dot(lightBounds.center - director.Player.position, right)) <=
            radius + playerRadius + alignmentWidth;
    }

    public void ResetForRetry()
    {
        UnsubscribeTurn();
        lightObject.SetPositionAndRotation(homePosition, homeRotation);
        CurrentState = State.Ready;
        elapsed = 0;
        headChecked = false;
        frameRequested = false;
        hasDodged = false;
    }
}
