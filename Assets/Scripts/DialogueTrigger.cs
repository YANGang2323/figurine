using UnityEngine;

public enum DialogueTriggerType
{
    AutoOnStart,      // 씬 시작 시 자동 실행
    Interaction,      // E키 상호작용
    Area,             // 닿을시
    EventTriggered    // 퍼즐, 조건 달성 등
}

public class DialogueTrigger : MonoBehaviour
{
    public DialogueTriggerType triggerType;

    [Header("처음 대화")]
    public DialogueData firstDialogue;

    [Header("반복 대화")]
    public DialogueData repeatDialogue;

    [Header("퍼즐 완료 후 대화 (선택사항)")]
    public DialogueData puzzleCompleteDialogue;

    private void Start()
    {
        if (triggerType == DialogueTriggerType.AutoOnStart)
        {
            PlayDialogue();
        }

    }
    private void OnTriggerEnter(Collider other)
    {
        if (triggerType != DialogueTriggerType.Area)
            return;

        if (!other.CompareTag("Player"))
            return;

        PlayDialogue();
    }
    //public void PlayDialogue()
    //{
    //    if (firstDialogue == null)
    //        return;

    //    bool played =
    //        DataManager.Instance.nowGame.playedDialogues
    //        .Contains(firstDialogue.dialogueID);
    //    Debug.Log($"played = {played}");

    //    if (!played)
    //    {
    //        DialogueManager.Instance.StartDialogue(firstDialogue);
    //    }
    //    else
    //    {
    //        if (repeatDialogue != null)
    //            DialogueManager.Instance.StartDialogue(repeatDialogue);
    //    }
    //}
    public void PlayDialogue()
    {
        // first가 없는 경우
        if (firstDialogue == null)
        {
            if (repeatDialogue != null)
                DialogueManager.Instance.StartDialogue(repeatDialogue);

            return;
        }

        // 반복 재생 대사
        if (!firstDialogue.playOnlyOnce)
        {
            DialogueManager.Instance.StartDialogue(firstDialogue);
            return;
        }

        bool played = DataManager.Instance.nowGame.playedDialogues
            .Contains(firstDialogue.dialogueID);

        if (!played)
        {
            DialogueManager.Instance.StartDialogue(firstDialogue);
        }
        else
        {
            if (repeatDialogue != null)
                DialogueManager.Instance.StartDialogue(repeatDialogue);
        }
    }

    public void PlayPuzzleCompleteDialogue()
    {
        if (puzzleCompleteDialogue == null)
        {
            if (repeatDialogue != null)
                DialogueManager.Instance.StartDialogue(repeatDialogue);

            return;
        }

        if (!puzzleCompleteDialogue.playOnlyOnce)
        {
            DialogueManager.Instance.StartDialogue(puzzleCompleteDialogue);
            return;
        }

        bool played = DataManager.Instance.nowGame.playedDialogues
            .Contains(puzzleCompleteDialogue.dialogueID);

        if (!played)
        {
            DialogueManager.Instance.StartDialogue(puzzleCompleteDialogue);
        }
        else if (repeatDialogue != null)
        {
            DialogueManager.Instance.StartDialogue(repeatDialogue);
        }
    }
}