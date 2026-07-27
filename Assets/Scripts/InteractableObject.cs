using UnityEngine;
using System.Collections.Generic; 
using Unity.Services.Analytics;
public class InteractableObject : MonoBehaviour
{
    public Transform interactionPoint;
    public string objectName = "extrawoman";
    public bool canInteract = true;
    public bool requireSideView;
    public bool useViewCondition;

    private DialogueTrigger dialogueTrigger;
    public PerspectiveEmblemPuzzle puzzle;
    public PerspectivePuzzlePiece puzzlePiece;


    private void Awake()
    {
        dialogueTrigger = GetComponent<DialogueTrigger>();
    }

    public bool IsAvailable()
    {
        if (!canInteract)
            return false;

        if (useViewCondition)
        {
            if (CameraPivotRotator.Instance.isSideView != requireSideView)
                return false;
        }

        return true;
    }

    public void Interact()
    {
        Debug.Log(objectName + " 와 상호작용!");
        
        // 애널리틱스 전송: 오브젝트 ID와 플레이코드를 묶어서 송신
        var eventData = new Dictionary<string, object> {
            { "PlayCode", DataManager.instance.nowGame.playCode },
            { "ObjectID", objectName } // 인스펙터에 적어두신 extrawoman 같은 고유 이름 활용
        };
        CustomEvent myEvent = new CustomEvent("object_interact");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        AnalyticsService.Instance.RecordEvent(myEvent);

        if (puzzle != null)
        {
            puzzle.InteractPuzzle();
            return;
        }

        if (puzzlePiece != null)
        {
            puzzlePiece.Interact();
            return;
        }

        if (DialogueManager.Instance != null &&
        DialogueManager.Instance.IsPlaying)
        {return;}
        //플래그처리
        IInteractHandler handler = GetComponent<IInteractHandler>();
        if (handler != null)
        {
            handler.Interact();
            return; // 분기 처리를 했으므로 아래 기본 dialogueTrigger는 실행 안 함
        }

        if (dialogueTrigger != null)
        {
            dialogueTrigger.PlayDialogue();
        }
    }
}
