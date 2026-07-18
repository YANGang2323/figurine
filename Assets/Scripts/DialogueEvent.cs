using UnityEngine;

[System.Serializable]
public class DialogueEvent
{
    public DialogueEventType type = DialogueEventType.None;

    [Header("대상 오브젝트")]
    public GameObject target;

    [Header("씬 이동")]
    public string sceneName;
    public Vector3 spawnPosition;
}