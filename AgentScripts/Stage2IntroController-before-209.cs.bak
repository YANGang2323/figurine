using System.Collections;
using UnityEngine;

// Select the active rig before either player's Awake and the save-load scene callback.
[DefaultExecutionOrder(-200)]
public class Stage2IntroController : MonoBehaviour
{
    public DialogueData openingDialogue;
    public DialogueData afterChoiceDialogue;
    public DialogueData stage2Dialogue;
    public DialogueData stage2SceneChoiceDialogue;
    public DialogueData teodorDialogue;
    public DialogueData[] teodorChoiceDialogues;
    public Transform doorTarget;
    public float doorPanDuration = 1.25f;
    public float doorHoldDuration = 1.5f;
    public GameObject corridorPlayer;
    public GameObject backstagePlayer;
    public CameraPivotRotator corridorCamera;
    public CameraPivotRotator backstageCamera;
    public ScreenFader fader;
    public float fadeDuration = 0.4f;

    const string CorridorFlag = "stage2_intro_corridor";
    const string CutsceneFlag = "stage2_intro_cutscene";
    const string CompleteFlag = "stage2_intro_complete";
    public enum Phase { Opening, Corridor, Cutscene, Transition, Backstage }
    public Phase CurrentPhase { get; private set; }
    DialogueManager manager;

    void Awake()
    {
        var flags = DataManager.Instance.nowGame.progressFlags;
        CurrentPhase = flags.Contains(CompleteFlag) ? Phase.Backstage :
            flags.Contains(CutsceneFlag) ? Phase.Cutscene :
            flags.Contains(CorridorFlag) ? Phase.Corridor : Phase.Opening;
        SelectRig(CurrentPhase == Phase.Backstage || CurrentPhase == Phase.Cutscene);
    }

    void Start()
    {
        manager = DialogueManager.Instance;
        manager.DialogueCompleted += OnDialogueCompleted;
        BindInteraction(CurrentPhase == Phase.Backstage || CurrentPhase == Phase.Cutscene);
        if (CurrentPhase == Phase.Opening)
            manager.StartDialogue(openingDialogue);
        else if (CurrentPhase == Phase.Cutscene)
            manager.StartDialogue(stage2Dialogue);
    }

    void SelectRig(bool backstage)
    {
        // Activate the camera before the player: PlayerMovement.Awake uses Camera.main.
        var oldCamera = backstage ? corridorCamera : backstageCamera;
        var newCamera = backstage ? backstageCamera : corridorCamera;
        var oldPlayer = backstage ? corridorPlayer : backstagePlayer;
        var newPlayer = backstage ? backstagePlayer : corridorPlayer;
        oldPlayer.SetActive(false);
        oldCamera.gameObject.SetActive(false);
        newCamera.gameObject.SetActive(true);
        newPlayer.SetActive(true);
        BindInteraction(backstage);
    }

    void BindInteraction(bool backstage)
    {
        var dialogueManager = DialogueManager.Instance;
        if (dialogueManager != null)
            dialogueManager.playerInteraction =
                (backstage ? backstagePlayer : corridorPlayer).GetComponent<PlayerInteraction>();
    }

    void OnDialogueCompleted(DialogueData dialogue)
    {
        if (CurrentPhase == Phase.Opening && dialogue == afterChoiceDialogue)
        {
            CurrentPhase = Phase.Corridor;
            AddFlag(CorridorFlag);
        }
        else if (CurrentPhase == Phase.Cutscene && dialogue == stage2Dialogue)
        {
            manager.StartDialogue(stage2SceneChoiceDialogue);
        }
        else if (CurrentPhase == Phase.Cutscene && dialogue == stage2SceneChoiceDialogue)
        {
            manager.StartDialogue(teodorDialogue);
        }
        else if (CurrentPhase == Phase.Cutscene &&
            System.Array.IndexOf(teodorChoiceDialogues, dialogue) >= 0)
        {
            CurrentPhase = Phase.Transition;
            StartCoroutine(ShowDoorAndReturn());
        }
    }

    IEnumerator ShowDoorAndReturn()
    {
        InputLockManager.TransitionLock = true;
        InputLockManager.Refresh();
        Transform rig = backstageCamera.transform;
        Vector3 origin = rig.position;
        if (doorTarget != null)
        {
            Camera lens = backstageCamera.GetComponentInChildren<Camera>();
            Vector3 focus = doorTarget.position;
            // Translate along the image plane, preserving the authored view and depth.
            Vector3 offset = focus - lens.transform.position;
            Vector3 destination = origin + Vector3.ProjectOnPlane(offset, lens.transform.forward);
            yield return PanCamera(rig, origin, destination);
            float held = 0f;
            while (held < doorHoldDuration)
            {
                if (!InputLockManager.ESCMenuLock) held += Time.deltaTime;
                yield return null;
            }
            yield return PanCamera(rig, destination, origin);
        }
        rig.position = origin;
        AddFlag(CompleteFlag);
        CurrentPhase = Phase.Backstage;
        InputLockManager.TransitionLock = false;
    }

    IEnumerator PanCamera(Transform rig, Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < doorPanDuration)
        {
            if (!InputLockManager.ESCMenuLock) elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / Mathf.Max(0.01f, doorPanDuration));
            rig.position = Vector3.Lerp(from, to, t);
            yield return null;
        }
        rig.position = to;
    }

    public void TryShowViewTutorial(Collider other)
    {
        if (CurrentPhase == Phase.Corridor &&
            other.transform.IsChildOf(corridorPlayer.transform))
            corridorCamera.ShowViewTutorialHint("Q와 E를 눌러보며 화면을 전환해 보세요.", 5f);
    }

    public void TryStartBackstageCutscene(Collider other)
    {
        if (CurrentPhase != Phase.Corridor || manager == null || manager.IsPlaying ||
            InputLockManager.TransitionLock || InputLockManager.TutorialMoveLock ||
            !corridorCamera.isSideView || corridorCamera.IsRotating ||
            (other.gameObject != corridorPlayer && !other.transform.IsChildOf(corridorPlayer.transform)))
            return;

        CurrentPhase = Phase.Transition;
        StartCoroutine(EnterBackstageCutscene());
    }

    IEnumerator EnterBackstageCutscene()
    {
        InputLockManager.TransitionLock = true;
        InputLockManager.Refresh();
        if (fader != null) yield return fader.FadeOut(fadeDuration);
        SelectRig(true);
        // Allow the newly activated camera's Start to initialize its view first.
        yield return null;
        CurrentPhase = Phase.Cutscene;
        AddFlag(CutsceneFlag);
        manager.StartDialogue(stage2Dialogue);
        if (fader != null) yield return fader.FadeIn(fadeDuration);
        InputLockManager.TransitionLock = false;
    }

    static void AddFlag(string flag)
    {
        var flags = DataManager.Instance.nowGame.progressFlags;
        if (!flags.Contains(flag)) flags.Add(flag);
    }

    void OnDestroy()
    {
        if (manager != null) manager.DialogueCompleted -= OnDialogueCompleted;
        InputLockManager.TransitionLock = false;
    }
}
