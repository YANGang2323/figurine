using UnityEngine;

public enum DialogueTriggerType
{
    AutoOnStart,      // 씬 시작 시 자동 실행
    Interaction,      // E키 상호작용
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

    public void PlayDialogue()
    {
        if (firstDialogue == null)
            return;

        bool played =
            DataManager.Instance.nowGame.playedDialogues
            .Contains(firstDialogue.dialogueID);
        Debug.Log($"played = {played}");

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
}