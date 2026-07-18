using System;

public enum ChoiceTag
{
    None,

    // 세계관 가치관
    Authority,     // 질서·권위
    Doubt,         // 의심·자기판단

    // 나중에 추가 가능
    Trust,
    Suspicion,
    Freedom,
    Obedience
}

[System.Serializable]
public class DialogueChoice
{
    public string text;

    // 선택 시 이어질 대사
    public DialogueData nextDialogue;

    // 선택 직후 실행할 이벤트
    public DialogueEvent eventData;

    // 엔딩 분기용
    public ChoiceTag tag;

    public int value = 1;   // 추가

    // 몇 점 줄지
    public int score = 1;

    
}