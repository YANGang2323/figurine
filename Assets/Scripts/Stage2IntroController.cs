using System.Collections;
using UnityEngine;

// Select the active rig before either player's Awake and the save-load scene callback.
[DefaultExecutionOrder(-200)]
public class Stage2IntroController : MonoBehaviour
{
    public DialogueData openingDialogue;
    public DialogueData afterChoiceDialogue;
    public DialogueData stage2Dialogue;
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
        else if (CurrentPhase == Phase.Corridor)
            corridorCamera.ArmTabTutorialHint(10f);
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
            corridorCamera.ArmTabTutorialHint(10f);
        }
        else if (CurrentPhase == Phase.Cutscene && dialogue == stage2Dialogue)
        {
            CurrentPhase = Phase.Backstage;
            AddFlag(CompleteFlag);
        }
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
