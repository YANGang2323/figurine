using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class FallingLightDirector : MonoBehaviour
{
    public DialogueData hitDialogue;
    public DialogueData reminderDialogue;
    public ScreenFader fader;
    public CanvasGroup tutorialOverlay;
    public float fadeDuration = 0.45f;
    public float returnDuration = 0.8f;
    [Min(0f)] public float tutorialLandingHold = 0.9f;
    public Transform Player { get; private set; }
    public CameraPivotRotator Pivot { get; private set; }
    public Camera ViewCamera { get; private set; }
    public bool Busy { get; private set; }
    private float retryGraceUntil;
    public bool TutorialSeen => HasFlag("light_tab_tutorial_completed");
    public int DeathCount { get; private set; }
    FallingLightHazard active;
    FallingLightHazard shot;
    bool returning;
    float returnTime;
    float landingHoldRemaining;
    Vector3 shotPosition;
    float shotHeight;
    const string ReminderFlag = "light_hit_reminder_seen";

    void Start()
    {
        tutorialOverlay.alpha = 0;
        tutorialOverlay.gameObject.SetActive(false);
        DeathCount = HasFlag("light_death_2") ? 2 : HasFlag("light_death_1") ? 1 : 0;
    }

    public bool TryClaim(FallingLightHazard hazard, Transform player)
    {
        if (Busy || Time.time < retryGraceUntil || (active != null && active != hazard)) return false;
        active = hazard;
        Player = player;
        Pivot = CameraPivotRotator.Instance;
        ViewCamera = Pivot.GetComponentInChildren<Camera>();
        return true;
    }

    public float PlayerHeadHeight
    {
        get
        {
            float height = Player.position.y + 1.6f;
            bool found = false;
            foreach (var collider in Player.GetComponents<Collider>())
            {
                if (!collider.enabled || collider.isTrigger) continue;
                height = found ? Mathf.Max(height, collider.bounds.max.y) : collider.bounds.max.y;
                found = true;
            }
            return height;
        }
    }

    public void ShowTutorial()
    {
        InputLockManager.TutorialMoveLock = true;
        InputLockManager.Refresh();
        tutorialOverlay.gameObject.SetActive(true);
        tutorialOverlay.alpha = 1;
    }

    public void CompleteTutorial()
    {
        AddFlag("light_tab_tutorial_completed");
        CancelTutorial();
    }

    public void CancelTutorial()
    {
        tutorialOverlay.alpha = 0;
        tutorialOverlay.gameObject.SetActive(false);
        InputLockManager.TutorialMoveLock = false;
    }

    public void FrameLight(FallingLightHazard hazard)
    {
        if (shot == null)
        {
            shotHeight = Pivot.transform.position.y;
        }
        shot = hazard;
        landingHoldRemaining = 0f;
        returning = false;
    }

    public void Finished(FallingLightHazard hazard)
    {
        if (active == hazard) active = null;
        if (hazard.teachTab && shot != hazard)
            FrameLight(hazard);
        if (shot == hazard)
        {
            returning = true;
            returnTime = 0f;
            landingHoldRemaining = hazard.teachTab ? tutorialLandingHold : 0.4f;
            shotPosition = Pivot.transform.position;
        }
    }

    void LateUpdate()
    {
        if (shot == null || Pivot == null || !Pivot.gameObject.activeInHierarchy || Busy) return;
        if (returning && landingHoldRemaining <= 0f)
        {
            returnTime += Time.deltaTime;
            float t = Mathf.SmoothStep(0, 1, Mathf.Clamp01(returnTime / returnDuration));
            Vector3 position = Vector3.Lerp(shotPosition, Pivot.transform.position, t);
            position.y = shotHeight;
            if (shot.teachTab && Pivot.UsesMapRotation)
                Pivot.transform.position = position;
            else
                ApplyShotPosition(position);
            if (t >= 1) shot = null;
            return;
        }
        bool holdingLanding = returning && landingHoldRemaining > 0f;
        Bounds bounds = shot.LightBounds;
        bool showLandedLight = holdingLanding && shot.teachTab;
        if (!showLandedLight)
        {
            bounds.Encapsulate(Player.position);
            bounds.Encapsulate(new Vector3(Player.position.x, PlayerHeadHeight + 0.25f, Player.position.z));
        }
        // Reframe horizontally only; falling geometry must never pull the camera down.
        Vector3 center = bounds.center;
        center.y = shotHeight;
        if (Pivot.UsesMapRotation)
        {
            Vector3 baseline = Pivot.transform.position;
            center = baseline + Vector3.Project(center - baseline, ViewCamera.transform.right);
            center.y = shotHeight;
            if (showLandedLight)
                Pivot.transform.position = Vector3.Lerp(shotPosition, center, Time.deltaTime * 10f);
            else
                ApplyShotPosition(center);
        }
        else
            ApplyShotPosition(center);

        if (holdingLanding)
        {
            shotPosition = Pivot.transform.position;
            landingHoldRemaining -= Time.deltaTime;
        }
    }

    private void ApplyShotPosition(Vector3 position)
    {
        if (Pivot.UsesMapRotation)
            Pivot.FrameMapPuzzle(position);
        else
            Pivot.transform.position = position;
    }

    public void Hit(FallingLightHazard hazard)
    {
        if (Busy) return;
        Busy = true;
        Debug.Log($"[FallingLight] Hit {hazard.name}; returning {Player.name} to {hazard.checkpoint.name} at {hazard.checkpoint.position}");
        StartCoroutine(HitRoutine(hazard));
    }

    IEnumerator HitRoutine(FallingLightHazard hazard)
    {
        // Snapshot the victim and destination before any dialogue/trigger can change state.
        Transform hitPlayer = Player;
        CameraPivotRotator hitPivot = Pivot;
        Vector3 respawnPosition;
        Rigidbody body = hitPlayer.GetComponent<Rigidbody>();
        bool wasKinematic = body != null && body.isKinematic;
        InputLockManager.TransitionLock = true;
        InputLockManager.Refresh();
        DeathCount++;
        AddFlag(DeathCount >= 2 ? "light_death_2" : "light_death_1");
        var manager = DialogueManager.Instance;
        var dialogCanvas = manager.panel.GetComponentInParent<Canvas>();
        int originalOrder = dialogCanvas.sortingOrder;
        bool originalOverride = dialogCanvas.overrideSorting;
        dialogCanvas.overrideSorting = true;
        dialogCanvas.sortingOrder = fader.GetComponentInParent<Canvas>().sortingOrder + 2;
        try
        {
            yield return fader.FadeOut(fadeDuration);
            shot = null;
            if (body != null)
            {
                if (!body.isKinematic)
                {
                    body.linearVelocity = Vector3.zero;
                    body.angularVelocity = Vector3.zero;
                }
                body.isKinematic = true;
            }
            hitPivot.RestoreView(hazard.EntrySideView);
            respawnPosition = FindClearRespawn(hitPlayer, hazard.checkpoint.position, hitPivot.transform.right);
            hitPlayer.position = respawnPosition;
            if (body != null) body.position = respawnPosition;
            hitPivot.SnapToPlayer();
            hazard.ResetForRetry();
            active = null;
            Physics.SyncTransforms();
            yield return fader.FadeIn(fadeDuration);
            yield return PlayAndWait(hitDialogue);
            if ((hazard.teachTab || DeathCount >= 2) && !HasFlag(ReminderFlag))
            {
                AddFlag(ReminderFlag);
                yield return PlayAndWait(reminderDialogue);
            }
        }
        finally
        {
            if (body != null) body.isKinematic = wasKinematic;
            dialogCanvas.sortingOrder = originalOrder;
            dialogCanvas.overrideSorting = originalOverride;
            InputLockManager.TransitionLock = false;
            retryGraceUntil = Time.time + 0.75f;
            Busy = false;
        }
    }

    private static Vector3 FindClearRespawn(Transform player, Vector3 checkpoint, Vector3 right)
    {
        foreach (CapsuleCollider capsule in player.GetComponents<CapsuleCollider>())
        {
            if (!capsule.enabled || capsule.isTrigger)
                continue;

            Vector3 centerOffset = capsule.transform.TransformPoint(capsule.center) - player.position;
            float reach = capsule.bounds.extents.magnitude;
            for (int step = 0; step <= 8; step++)
            {
                for (int direction = -1; direction <= 1; direction += 2)
                {
                    Vector3 candidate = checkpoint + right * (direction * step * 0.125f);
                    bool blocked = false;
                    foreach (Collider other in Physics.OverlapSphere(candidate + centerOffset, reach,
                                 Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                    {
                        if (other.transform.IsChildOf(player))
                            continue;

                        if (Physics.ComputePenetration(capsule, candidate, player.rotation,
                                other, other.transform.position, other.transform.rotation,
                                out Vector3 separation, out float depth) &&
                            depth > 0.005f && separation.y < 0.9f)
                        {
                            blocked = true;
                            break;
                        }
                    }

                    if (!blocked)
                        return candidate;
                }
            }
        }

        return checkpoint;
    }

    IEnumerator PlayAndWait(DialogueData dialogue)
    {
        if (dialogue == null || dialogue.lines.Count == 0) yield break;
        var manager = DialogueManager.Instance;
        bool complete = false;
        System.Action<DialogueData> handler = data => { if (data == dialogue) complete = true; };
        manager.DialogueCompleted += handler;
        try
        {
            manager.StartDialogue(dialogue);
            while (!complete && manager != null && manager.IsPlaying) yield return null;
        }
        finally { if (manager != null) manager.DialogueCompleted -= handler; }
    }

    static bool HasFlag(string flag) => DataManager.Instance.nowGame.progressFlags.Contains(flag);
    static void AddFlag(string flag)
    {
        var flags = DataManager.Instance.nowGame.progressFlags;
        if (!flags.Contains(flag)) flags.Add(flag);
    }

    void OnDestroy()
    {
        InputLockManager.TutorialMoveLock = false;
        if (Busy) InputLockManager.TransitionLock = false;
    }
}
