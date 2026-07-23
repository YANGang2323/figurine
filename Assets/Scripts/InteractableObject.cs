using UnityEngine;

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

        if (dialogueTrigger != null)
        {
            dialogueTrigger.PlayDialogue();
        }
    }
}
