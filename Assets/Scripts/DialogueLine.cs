using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    public string speakerName;

    [TextArea(3, 5)]
    public string text;

    [Header("Background")]
    public bool hideBackground;
    public Sprite background;

    [Header("Portrait")]
    public bool hideLeftPortrait;
    public Sprite leftPortrait;

    public bool hideRightPortrait;
    public Sprite rightPortrait;
}