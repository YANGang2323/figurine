using UnityEngine;

public enum DialogueEventType
{
    None,

    DoorUnlock,     // 문 열기
    LoadScene,      // 씬 이동
    PlayCutscene,   // 컷신 시작
    StartPuzzle,    // 퍼즐 시작
    ActivateCCTV,   // CCTV 활성화
    CctvRespawn,        // 리스폰

    EnableObject,   // 오브젝트 활성화
    DisableObject   // 오브젝트 비활성화
}