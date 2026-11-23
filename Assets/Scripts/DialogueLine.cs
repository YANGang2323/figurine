using TMPro;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [Header("Text")]
    public string speakerName;

    [TextArea(3, 5)]
    public string text;

    [Header("Tutorial Analytics (optional)")]
    public string tutorialID;
    public string tutorialName;

    [Header("Background")]
    public bool hideBackground;
    public Sprite background;

    [Header("Portrait")]
    public bool hideLeftPortrait;
    public Sprite leftPortrait;

    public bool hideRightPortrait;
    public Sprite rightPortrait;

    [Header("DetailImage")]
    public Sprite detailImage1;
    public Sprite detailImage2;
    public Sprite detailImage3;

    [Header("텍스트 스타일")]
    public bool useCustomDialogueFont;
    public TMP_FontAsset dialogueFont;

    public bool useCustomSpeakerFont;
    public TMP_FontAsset speakerFont;

    public bool useCustomDialogueSize;
    public float dialogueFontSize;

    public bool useCustomSpeakerSize;
    public float speakerFontSize;

    public bool useCustomDialogueColor;
    public Color dialogueColor = Color.white;

    public bool useCustomSpeakerColor;
    public Color speakerColor = Color.white;


    [Header("SFX")]
    public AudioClip soundEffect;

    [Range(0f, 1f)]
    public float soundVolume = 1f;

    [Header("BGM")]
    public bool changeBGM;
    public AudioClip bgm;

    [Range(0f, 1f)]
    public float bgmVolume = 1f;

    public bool stopBGM;


    [Header("Choice")]
    public bool hasChoice;

    public DialogueChoice[] choices;
}
