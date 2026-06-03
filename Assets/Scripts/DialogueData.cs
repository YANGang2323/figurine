using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dialogue/Dialogue")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;

    public bool playOnlyOnce;

    public List<DialogueLine> lines;
}