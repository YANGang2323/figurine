using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Services.Analytics;

[Serializable]
public class PlayExitSnapshot
{
    public string runID = "", sceneName = "";
    public int stage;
    public float stageStaySeconds, activeStageSeconds, zoneStaySeconds, puzzleStaySeconds, noProgressSeconds, idleSeconds;
    public string zoneID = "", zoneName = "", cameraView = "", currentActivity = "exploration";
    public Vector3 position;
    public bool positionKnown, focused = true;
    public int zoneChanges, errorCount, cctvDetectedCount, lightFailureCount;
    public string lastObjectID = "", lastTutorialID = "", lastTutorialName = "", lastDialogueID = "", lastDialogueName = "";
    public int dialogueLine, dialogueTotalLines;
    public bool dialogueActive;
    public string puzzleID = "", puzzleType = "", stepID = "", puzzleState = "";
    public int completedSteps, totalSteps, attempts;
    public bool puzzleActive, endingReached;
    public string currentLockInput = "", currentLockPuzzleID = "";
    public string lastSubmittedAnswer = "", lastSubmittedPuzzleID = "";
    public bool hasSubmittedAnswer, lastSubmissionSucceeded;
    public string checkpointID = "", checkpointName = "";
    public float progressPercent, highestProgressPercent;
    public string progressVersion = "";
    public List<string> completedMilestones = new List<string>();
}
[Serializable]
public class PlayObservation
{
    public string eventName, observationID, runID, attemptSessionID, observedAtUtc, playCode, trigger, progressVersion;
    public int sequence;
    public bool recovered;
    public string exitReason = "not_applicable";
    public PlayExitSnapshot state;
}

// Shared state for checkpoints, heartbeat events, saved games and exit summaries.
public class PlayExitAnalytics : MonoBehaviour
{
    public static bool ServiceReady { get; private set; }
    DataManager owner;
    GameData observedGame;
    string attemptSessionID;
    int sequence;
    bool sent, applicationClosing;
    float heartbeat, sampleTimer, retryTimer;
    Vector3 previousMouse;
    CameraPivotRotator pivot;
    PlayerMovement player;
    AnalyticsZone[] zones = new AnalyticsZone[0];
    AnalyticsProgressConfig config;
    readonly Dictionary<string, float> highWater = new Dictionary<string, float>();
    readonly List<PlayObservation> pending = new List<PlayObservation>();
    PlayObservation recovery;
    string JournalPath => Path.Combine(Application.persistentDataPath, "analytics_last_observation.json");
    public string ProgressVersion => config != null ? config.progressVersion : "demo_v1";
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() { ServiceReady = false; }
    public static void MarkServiceReady() { ServiceReady = true; }
    void Awake()
    {
        owner = GetComponent<DataManager>();
        config = Resources.Load<AnalyticsProgressConfig>("AnalyticsProgressConfig");
        zones = FindObjectsByType<AnalyticsZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        if (!Application.isPlaying) return;
        try
        {
            if (File.Exists(JournalPath)) recovery = JsonUtility.FromJson<PlayObservation>(File.ReadAllText(JournalPath));
            if (recovery?.state != null && !string.IsNullOrEmpty(recovery.runID)) highWater[recovery.runID + "|" + recovery.progressVersion] = recovery.state.highestProgressPercent;
        }
        catch (Exception e) { Debug.LogWarning("Could not read analytics recovery: " + e.Message); }
    }
    void OnEnable() { SceneManager.sceneLoaded += OnSceneLoaded; Application.logMessageReceived += OnLog; }
    void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; Application.logMessageReceived -= OnLog; }
    void OnLog(string message, string stack, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)
            if (owner?.nowGame?.exitSnapshot != null) owner.nowGame.exitSnapshot.errorCount++;
    }
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        pivot = null; player = null;
        zones = FindObjectsByType<AnalyticsZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        EnterScene(scene);
        if (IsGameplay(scene.name)) Emit("play_progress", "stage_enter", true);
    }
    public static bool IsGameplay(string name) => !string.IsNullOrEmpty(name) && name != "MainMenu" && name != "Title";
    void EnsureRun()
    {
        if (owner?.nowGame == null) return;
        var game = owner.nowGame;
        if (game.exitSnapshot == null) game.exitSnapshot = new PlayExitSnapshot();
        var state = game.exitSnapshot;
        if (string.IsNullOrEmpty(state.runID))
        {
            if (!string.IsNullOrEmpty(game.totalGameStartTime))
            {
                using (var hash = System.Security.Cryptography.MD5.Create())
                    state.runID = new Guid(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(game.playCode + "|" + game.totalGameStartTime))).ToString("N");
            }
            else state.runID = Guid.NewGuid().ToString("N");
        }
        if (state.completedMilestones == null) state.completedMilestones = new List<string>();
        if (state.progressVersion != ProgressVersion)
        {
            state.progressVersion = ProgressVersion;
            state.progressPercent = 0; state.highestProgressPercent = 0;
            state.checkpointID = ""; state.checkpointName = "";
            foreach (var milestone in config != null ? config.milestones : AnalyticsProgressConfig.Defaults())
                if (state.completedMilestones.Contains(milestone.id) && milestone.percent >= state.progressPercent)
                { state.progressPercent = milestone.percent; state.checkpointID = milestone.id; state.checkpointName = milestone.label; }
            state.highestProgressPercent = state.progressPercent;
        }
        if (observedGame != game)
        {
            observedGame = game; attemptSessionID = Guid.NewGuid().ToString("N");
            sequence = 0; heartbeat = 0; sent = false;
        }
        if (highWater.TryGetValue(state.runID + "|" + ProgressVersion, out float highest)) state.highestProgressPercent = Mathf.Max(highest, state.highestProgressPercent);
        highWater[state.runID + "|" + ProgressVersion] = Mathf.Max(state.highestProgressPercent, state.progressPercent);
    }
    void EnterScene(Scene scene)
    {
        EnsureRun();
        if (owner?.nowGame == null || !IsGameplay(scene.name)) return;
        var game = owner.nowGame;
        var old = game.exitSnapshot;
        if (old.sceneName != scene.name)
        {
            game.exitSnapshot = new PlayExitSnapshot
            {
                runID = old.runID, sceneName = scene.name, stage = game.stage,
                lastTutorialID = old.lastTutorialID, lastTutorialName = old.lastTutorialName,
                lastDialogueID = old.lastDialogueID, lastDialogueName = old.lastDialogueName,
                progressPercent = old.progressPercent, highestProgressPercent = old.highestProgressPercent,
                checkpointID = old.checkpointID, checkpointName = old.checkpointName,
                progressVersion = old.progressVersion,
                completedMilestones = old.completedMilestones, endingReached = old.endingReached,
                currentLockInput = old.currentLockInput, currentLockPuzzleID = old.currentLockPuzzleID,
                lastSubmittedAnswer = old.lastSubmittedAnswer, lastSubmittedPuzzleID = old.lastSubmittedPuzzleID,
                hasSubmittedAnswer = old.hasSubmittedAnswer, lastSubmissionSucceeded = old.lastSubmissionSucceeded
            };
            game.exitSnapshot.zoneID = scene.name + "/exploration";
            game.exitSnapshot.zoneName = scene.name + " 탐색 구역";
        }
        if (scene.name == "Stage1") ApplyMilestone("stage1_enter");
        if (scene.name == "Stage2") ApplyMilestone("stage2_enter");
    }
    void Update()
    {
        if (owner?.nowGame == null || applicationClosing) return;
        EnsureRun();
        retryTimer += Time.unscaledDeltaTime;
        if (retryTimer >= 5) { retryTimer = 0; Drain(); }
        var scene = SceneManager.GetActiveScene();
        var state = owner.nowGame.exitSnapshot;
        float dt = Time.unscaledDeltaTime;
        if (IsGameplay(scene.name))
        {
            EnterScene(scene); state = owner.nowGame.exitSnapshot;
            state.stageStaySeconds += dt; state.zoneStaySeconds += dt; state.focused = Application.isFocused;
            if (Application.isFocused)
            {
                state.activeStageSeconds += dt;
                if (state.puzzleActive && !state.dialogueActive && !InputLockManager.ESCMenuLock) state.puzzleStaySeconds += dt;
                if (!InputLockManager.ESCMenuLock) state.noProgressSeconds += dt;
                bool input = Input.anyKeyDown || (Input.mousePosition - previousMouse).sqrMagnitude > 4;
                state.idleSeconds = input ? 0 : state.idleSeconds + dt;
                previousMouse = Input.mousePosition;
            }
            RefreshSavedMilestones();
        }
        else state.currentActivity = "main_menu";
        sampleTimer += dt;
        if (sampleTimer >= .5f) { sampleTimer = 0; SampleLocation(true); }
        if (string.IsNullOrEmpty(state.sceneName)) return;
        heartbeat += dt;
        float interval = config != null ? Mathf.Max(10, config.snapshotIntervalSeconds) : 30;
        if (heartbeat >= interval) { heartbeat = 0; Emit("play_snapshot", "heartbeat", true); }
    }
    void RefreshSavedMilestones()
    {
        var game = owner.nowGame;
        if (game.exitSnapshot.sceneName == "Stage1")
        {
            if (game.EmblemPuzzleState > 0) ApplyMilestone("emblem_started");
            if (game.EmblemfirstPieceCompleted) ApplyMilestone("emblem_first_piece");
            if (game.EmblemPuzzleState == 2) ApplyMilestone("emblem_complete");
        }
        if (game.exitSnapshot.sceneName == "Stage2" && game.progressFlags != null)
        {
            if (game.progressFlags.Contains("stage2_intro_cutscene")) Milestone("corridor_complete");
            if (game.progressFlags.Contains("stage2_intro_complete")) Milestone("backstage_enter");
            if (game.progressFlags.Contains("light_tab_tutorial_completed")) Milestone("light_dodge_complete");
        }
    }
    void SampleLocation(bool reportZoneChange)
    {
        var s = owner?.nowGame?.exitSnapshot;
        var scene = SceneManager.GetActiveScene();
        if (s == null) return;
        if (!IsGameplay(scene.name)) { s.currentActivity = "main_menu"; return; }
        if (pivot == null || !pivot.isActiveAndEnabled || pivot.gameObject.scene != scene)
        {
            pivot = null;
            foreach (var c in FindObjectsByType<CameraPivotRotator>(FindObjectsSortMode.None))
                if (c.isActiveAndEnabled && c.gameObject.scene == scene && c.player != null && c.player.gameObject.activeInHierarchy) { pivot = c; break; }
        }
        Transform subject = pivot != null ? pivot.player : null;
        if (subject == null)
        {
            if (player == null || !player.isActiveAndEnabled) player = FindFirstObjectByType<PlayerMovement>();
            if (player != null && player.gameObject.scene == scene) subject = player.transform;
        }
        if (subject != null)
        {
            s.positionKnown = true; s.position = pivot != null ? pivot.ToSavePosition(subject.position) : subject.position;
            s.cameraView = pivot != null ? pivot.SaveView.ToString() : "unknown";
            AnalyticsZone best = null;
            foreach (var zone in zones)
                if (zone != null && zone.gameObject.scene == scene && zone.Contains(subject.position) &&
                    (best == null || zone.priority > best.priority ||
                    (zone.priority == best.priority && (subject.position-zone.transform.position).sqrMagnitude < (subject.position-best.transform.position).sqrMagnitude))) best = zone;
            string id = best != null ? best.zoneID : scene.name + "/exploration";
            if (id != s.zoneID)
            {
                s.zoneID = id; s.zoneName = best != null ? best.zoneName : scene.name + " 탐색 구역";
                s.zoneStaySeconds = 0; s.zoneChanges++;
                if (reportZoneChange) Emit("play_progress", "zone_enter", false);
            }
        }
        s.currentActivity = InputLockManager.ESCMenuLock ? "esc_menu" : s.dialogueActive ? "dialogue" : s.puzzleActive ? "puzzle" : "exploration";
    }
    bool ApplyMilestone(string id)
    {
        var s = owner?.nowGame?.exitSnapshot;
        if (s == null || s.completedMilestones.Contains(id)) return false;
        AnalyticsMilestone milestone = null;
        foreach (var item in config != null ? config.milestones : AnalyticsProgressConfig.Defaults()) if (item.id == id) { milestone = item; break; }
        if (milestone == null) return false;
        s.completedMilestones.Add(id);
        if (milestone.percent >= s.progressPercent)
        {
            s.checkpointID = id; s.checkpointName = milestone.label; s.progressPercent = milestone.percent; s.noProgressSeconds = 0;
        }
        s.highestProgressPercent = Mathf.Max(s.highestProgressPercent, s.progressPercent);
        highWater[s.runID + "|" + ProgressVersion] = s.highestProgressPercent;
        return true;
    }
    static PlayExitAnalytics Tracker()
    {
        var manager = DataManager.instance;
        if (manager?.nowGame == null) return null;
        var tracker = manager.GetComponent<PlayExitAnalytics>();
        if (tracker == null) tracker = manager.gameObject.AddComponent<PlayExitAnalytics>();
        if (tracker.owner == null) tracker.owner = manager;
        tracker.EnterScene(SceneManager.GetActiveScene());
        return tracker;
    }
    static PlayExitSnapshot Current() => Tracker()?.owner.nowGame.exitSnapshot;
    public static void Milestone(string id)
    {
        var tracker = Tracker();
        if (tracker != null && tracker.ApplyMilestone(id)) tracker.Emit("play_progress", "checkpoint", true);
    }
    public static void Interaction(string id)
    {
        var s = Current(); if (s == null) return;
        s.lastObjectID = id ?? ""; s.idleSeconds = 0;
        Tracker().Emit("play_progress", "object_interact", false);
    }
    public static void Tutorial(string id, string name)
    {
        var s = Current(); if (s == null) return;
        s.lastTutorialID = SceneManager.GetActiveScene().name + "/" + id; s.lastTutorialName = name ?? "";
        Tracker().Emit("play_progress", "tutorial_shown", true);
    }
    public static void Dialogue(DialogueData dialogue)
    {
        if (dialogue == null) return;
        var s = Current(); if (s == null) return;
        s.lastDialogueID = dialogue.dialogueID ?? ""; s.lastDialogueName = dialogue.name;
        s.dialogueActive = true; s.dialogueLine = 0; s.dialogueTotalLines = dialogue.lines?.Count ?? 0;
        if (dialogue.dialogueID == "Demo_Finale") Milestone("finale_started");
        Tracker().Emit("play_progress", "dialogue_start", false);
    }
    public static void DialogueLineShown(DialogueData dialogue, DialogueLine line)
    {
        var s = Current(); if (s != null && dialogue?.lines != null) s.dialogueLine = dialogue.lines.IndexOf(line) + 1;
    }
    public static void DialogueEnded(DialogueData dialogue)
    {
        var s = Current(); if (s == null) return;
        s.dialogueActive = false;
        if (dialogue != null && dialogue.dialogueID == "STAGE1 Scene1") Milestone("intro_complete");
        Tracker().Emit("play_progress", "dialogue_end", true);
    }
    public static void DialogueTutorial(DialogueLine line)
    {
        if (line != null && !string.IsNullOrWhiteSpace(line.tutorialID)) Tutorial(line.tutorialID, string.IsNullOrWhiteSpace(line.tutorialName) ? line.text : line.tutorialName);
    }
    public static void Puzzle(Component puzzle, string type, string step, int completed, int total, string detail, bool attempt = false, bool restore = false)
    {
        var target = puzzle.GetComponent<DialogueTarget>();
        if (target == null || string.IsNullOrWhiteSpace(target.targetID)) return;
        var s = Current();
        if (s == null || (restore && !string.IsNullOrEmpty(s.puzzleID) && s.puzzleID != target.targetID)) return;
        bool different = s.puzzleID != target.targetID;
        int oldCompleted = different ? 0 : s.completedSteps;
        string oldStep = different ? "" : s.stepID;
        if (different) { s.attempts = 0; s.puzzleStaySeconds = 0; }
        s.puzzleID = target.targetID; s.puzzleType = type; s.stepID = step;
        s.completedSteps = completed; s.totalSteps = total; s.puzzleState = detail;
        s.puzzleActive = step != "completed" && step != "closed" && step != "failed";
        if (attempt) s.attempts++;
        if (completed > oldCompleted) s.noProgressSeconds = 0;
        if (type == "emblem")
        {
            Milestone("emblem_started");
            if (completed >= 1) Milestone("emblem_first_piece");
            if (completed >= 2) Milestone("emblem_complete");
        }
        if (type == "number_lock")
        {
            s.currentLockInput = detail ?? "";
            s.currentLockPuzzleID = target.targetID;
            if (attempt)
            {
                s.hasSubmittedAnswer = true;
                s.lastSubmittedAnswer = detail ?? "";
                s.lastSubmittedPuzzleID = target.targetID;
                s.lastSubmissionSucceeded = total > 0 && completed >= total;
            }
            Milestone("number_lock_started");
            if (completed >= 1) Milestone("number_lock_complete");
        }
        if (!restore && (different || attempt || completed != oldCompleted || (step != oldStep && step != "editing")))
            Tracker().Emit("play_progress", attempt ? "puzzle_attempt" : "puzzle_progress", true);
    }
    public static void Failure(string kind)
    {
        var s = Current(); if (s == null) return;
        if (kind == "cctv") s.cctvDetectedCount++;
        if (kind == "falling_light") s.lightFailureCount++;
        Tracker().Emit("play_progress", kind + "_failure", true);
    }
    public static void Ending() { var s = Current(); if (s != null) { s.endingReached = true; Milestone("ending"); } }
    public static void BeforeMenu() { Tracker()?.Emit("play_snapshot", "return_to_menu", true); }
    public static void BeforeLoad() { Tracker()?.Emit("play_snapshot", "before_load", true); }
    void OnApplicationFocus(bool focus)
    {
        if (owner?.nowGame?.exitSnapshot == null) return;
        owner.nowGame.exitSnapshot.focused = focus;
        if (!focus) Emit("play_snapshot", "focus_lost", true);
    }
    void OnApplicationQuit() { if (!sent) SendExitSummary("application_quit"); applicationClosing = true; }
    public static void SendExitSummary(string reason = "quit_button")
    {
        var tracker = Tracker(); if (tracker == null || tracker.sent) return;
        tracker.sent = true; tracker.Emit("play_exit_summary", "exit", true, reason);
    }
    public static string ReasonHint(PlayExitSnapshot s)
    {
        if (s.endingReached) return "completed";
        if (s.errorCount > 0) return "error_observed";
        if (s.idleSeconds >= 120) return "inactive";
        if (s.totalSteps > 0 && s.completedSteps < s.totalSteps && s.attempts >= 5 && s.noProgressSeconds >= 120) return "possible_puzzle_difficulty";
        if (s.zoneChanges >= 4 && s.noProgressSeconds >= 180) return "possible_navigation_difficulty";
        return "unknown";
    }
    void Emit(string eventName, string trigger, bool flush, string exitReason = "not_applicable")
    {
        if (!Application.isPlaying || owner?.nowGame?.exitSnapshot == null || string.IsNullOrEmpty(owner.nowGame.exitSnapshot.sceneName)) return;
        EnsureRun(); SampleLocation(false);
        var item = new PlayObservation
        {
            eventName=eventName, observationID=Guid.NewGuid().ToString("N"), runID=owner.nowGame.exitSnapshot.runID,
            attemptSessionID=attemptSessionID, sequence=++sequence, observedAtUtc=DateTime.UtcNow.ToString("o"),
            playCode=owner.nowGame.playCode, trigger=trigger, exitReason=exitReason, progressVersion=ProgressVersion,
            state=JsonUtility.FromJson<PlayExitSnapshot>(JsonUtility.ToJson(owner.nowGame.exitSnapshot))
        };
        try
        {
            string temporary = JournalPath + ".tmp";
            File.WriteAllText(temporary, JsonUtility.ToJson(item));
            if (File.Exists(JournalPath)) File.Replace(temporary, JournalPath, null);
            else File.Move(temporary, JournalPath);
        }
        catch (Exception e) { Debug.LogWarning("Could not journal analytics: " + e.Message); }
        pending.Add(item); Drain();
        if (flush && ServiceReady) { try { AnalyticsService.Instance.Flush(); } catch (Exception e) { Debug.LogWarning(e.Message); } }
    }
    void Drain()
    {
        if (!Application.isPlaying || !ServiceReady) return;
        if (recovery != null) { recovery.recovered = true; pending.Insert(0, recovery); recovery = null; }
        while (pending.Count > 0)
        {
            try
            {
                var item = pending[0]; var record = new CustomEvent(item.eventName);
                foreach (var pair in Parameters(item)) record.Add(pair.Key, pair.Value);
                AnalyticsService.Instance.RecordEvent(record); pending.RemoveAt(0);
            }
            catch (Exception e) { Debug.LogWarning("Progress analytics pending: " + e.Message); break; }
        }
    }
    public Dictionary<string, object> Parameters(PlayObservation o)
    {
        var s = o.state;
        return new Dictionary<string, object>
        {
            {"PlayCode",o.playCode??""},{"RunID",o.runID??""},{"AttemptSessionID",o.attemptSessionID??""},
            {"ObservationID",o.observationID??""},{"EventSequence",o.sequence},{"ObservedAtUtc",o.observedAtUtc??""},{"Recovered",o.recovered},
            {"ProgressVersion",o.progressVersion??ProgressVersion},{"ProgressTrigger",o.trigger??""},
            {"LastStage",s.stage},{"LastSceneName",s.sceneName??""},{"StageStaySeconds",s.stageStaySeconds},
            {"ActiveStageSeconds",s.activeStageSeconds},{"ZoneStaySeconds",s.zoneStaySeconds},{"PuzzleStaySeconds",s.puzzleStaySeconds},
            {"ZoneID",s.zoneID??""},{"ZoneName",s.zoneName??""},{"PositionX",s.position.x},{"PositionY",s.position.y},{"PositionZ",s.position.z},{"PositionKnown",s.positionKnown},
            {"CameraView",s.cameraView??""},{"CurrentActivity",s.currentActivity??""},{"IsFocused",s.focused},
            {"CheckpointID",s.checkpointID??""},{"CheckpointName",s.checkpointName??""},{"ProgressPercent",s.progressPercent},{"HighestProgressPercent",s.highestProgressPercent},
            {"NoProgressSeconds",s.noProgressSeconds},{"IdleSeconds",s.idleSeconds},{"ZoneChanges",s.zoneChanges},
            {"LastObjectID",s.lastObjectID??""},{"LastTutorialID",s.lastTutorialID??""},{"LastTutorialName",s.lastTutorialName??""},
            {"LastDialogueID",s.lastDialogueID??""},{"LastDialogueName",s.lastDialogueName??""},{"DialogueLine",s.dialogueLine},{"DialogueTotalLines",s.dialogueTotalLines},{"DialogueActive",s.dialogueActive},
            {"LastPuzzleID",s.puzzleID??""},{"LastPuzzleType",s.puzzleType??""},{"LastPuzzleStepID",s.stepID??""},{"PuzzleCompletedSteps",s.completedSteps},{"PuzzleTotalSteps",s.totalSteps},
            {"PuzzleProgressPercent",s.totalSteps>0?100f*s.completedSteps/s.totalSteps:0f},{"PuzzleAttempts",s.attempts},{"PuzzleState",s.puzzleState??""},{"EndingReached",s.endingReached},
            {"CurrentLockInput",s.currentLockInput??""},{"CurrentLockPuzzleID",s.currentLockPuzzleID??""},
            {"HasSubmittedAnswer",s.hasSubmittedAnswer},{"LastSubmittedAnswer",s.lastSubmittedAnswer??""},
            {"LastSubmittedPuzzleID",s.lastSubmittedPuzzleID??""},{"LastSubmissionSucceeded",s.lastSubmissionSucceeded},
            {"ErrorCount",s.errorCount},{"CctvDetectedCount",s.cctvDetectedCount},{"LightFailureCount",s.lightFailureCount},
            {"ExitReason",o.exitReason??"unknown"},{"ReasonHint",ReasonHint(s)},{"UserReportedReason","not_collected"}
        };
    }
}
