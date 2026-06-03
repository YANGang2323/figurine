using UnityEngine;

public enum DialogueTriggerType
{
    AutoOnStart,      // 씬 시작 시 자동 실행
    Interaction,      // E키 상호작용
    EventTriggered    // 퍼즐, 조건 달성 등
}

public class DialogueTrigger : MonoBehaviour
{
    public DialogueData dialogue;
    public DialogueTriggerType triggerType;

    private void Start()
    {
        if (triggerType == DialogueTriggerType.AutoOnStart)
        {
            PlayDialogue();
        }
    }

    public void PlayDialogue()
    {
        if (dialogue == null)
            return;

        if (dialogue.playOnlyOnce &&
            DataManager.Instance.nowGame.playedDialogues.Contains(dialogue.dialogueID))
        {
            return;
        }

        DialogueManager.Instance.StartDialogue(dialogue);
    }
}