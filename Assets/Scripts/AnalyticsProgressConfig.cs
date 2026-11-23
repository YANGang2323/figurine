using System;
using UnityEngine;

[Serializable]
public class AnalyticsMilestone
{
    public string id, label;
    [Range(0, 100)] public float percent;
}
[CreateAssetMenu(menuName = "Figurine/Analytics Progress Config")]
public class AnalyticsProgressConfig : ScriptableObject
{
    public string progressVersion = "demo_v1";
    public float snapshotIntervalSeconds = 30;
    public AnalyticsMilestone[] milestones = Defaults();
    public static AnalyticsMilestone[] Defaults() => new[]
    {
        Item("stage1_enter", "1스테이지 진입", 0),
        Item("intro_complete", "도입 대화·이동 안내 완료", 5),
        Item("emblem_started", "엠블럼 시작", 15),
        Item("emblem_first_piece", "엠블럼 첫 조각 완성", 25),
        Item("emblem_complete", "엠블럼 해결", 35),
        Item("stage2_enter", "1스테이지 완료·2스테이지 진입", 40),
        Item("light_dodge_complete", "복도 낙하 조명 회피 튜토리얼 완료", 45),
        Item("corridor_complete", "복도 통과", 50),
        Item("backstage_enter", "무대 뒤 도입 완료", 60),
        Item("number_lock_started", "숫자 자물쇠 진입", 65),
        Item("number_lock_complete", "숫자 자물쇠 해결", 85),
        Item("finale_started", "마지막 대화 진입", 90),
        Item("ending", "엔딩 도달", 100)
    };
    static AnalyticsMilestone Item(string id, string label, float percent) => new AnalyticsMilestone { id=id, label=label, percent=percent };
}
