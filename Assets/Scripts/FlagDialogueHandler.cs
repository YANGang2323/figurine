using UnityEngine;

// 이 컴포넌트가 인터페이스(자격증)를 가집니다.
public class FlagDialogueHandler : MonoBehaviour, IInteractHandler
{
    [Header("진행 상황 체크 플래그")]
    public string requiredFlag;

    [Header("플래그가 있을 때 실행할 대화 (진행된 사항)")]
    public DialogueTrigger successDialogue;

    [Header("플래그가 없을 때 실행할 대화 (진행되지 않음)")]
    public DialogueTrigger failDialogue;

    public void Interact()
    {
        // 1. ProgressManager가 씬에 배치되지 않았거나 준비가 안 된 경우 체크
        if (ProgressManager.Instance == null)
        {
            Debug.LogError("🚨 ProgressManager 인스턴스를 찾을 수 없습니다! 하이어라키에 매니저 오브젝트가 있는지 확인하세요.");
            return;
        }
        // ProgressManager에 플래그가 있는지 검사합니다.
        if (ProgressManager.Instance.HasFlag(requiredFlag))
        {
            if (successDialogue != null)
            {
                successDialogue.PlayDialogue();
            }
        }
        else
        {
            if (failDialogue != null)
            {
                failDialogue.PlayDialogue();
            }
        }
    }
}