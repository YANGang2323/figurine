using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Dialogue/Dialogue")]
public class DialogueData : ScriptableObject
{
    public string dialogueID;

    public bool playOnlyOnce;

    public List<DialogueLine> lines;

    [Header("대화 종료 이벤트")]
    public DialogueEvent endEvent;
}