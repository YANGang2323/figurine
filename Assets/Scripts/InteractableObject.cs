using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public string objectName = "extrawoman";
    private DialogueTrigger dialogueTrigger;

    private void Awake()
    {
        dialogueTrigger = GetComponent<DialogueTrigger>();
    }

    public void Interact()
    {
        Debug.Log(objectName + " 와 상호작용!");
        if (dialogueTrigger != null)
        {
            dialogueTrigger.PlayDialogue();
        }
    }
}
