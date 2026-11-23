using UnityEngine;
using System.Collections.Generic;
[System.Serializable]
public class DialogueEvent
{
    public DialogueEventType type = DialogueEventType.None;

    [Header("대상 오브젝트 ID")]
    public List<string> targetIDs = new();
    //public string targetID;
    //public GameObject target;

    [Header("씬 이동")]
    public string sceneName;
    public Vector3 spawnPosition;
}