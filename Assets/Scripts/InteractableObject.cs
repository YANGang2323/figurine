using UnityEngine;

public class InteractableObject : MonoBehaviour
{
    public string objectName = "extrawoman";

    public void Interact()
    {
        Debug.Log(objectName + " 와 상호작용!");
    }
}
