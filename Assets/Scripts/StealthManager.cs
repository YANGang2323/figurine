using System.Collections;
using UnityEngine;

public class StealthManager : MonoBehaviour
{
    public static StealthManager Instance;

    public Transform player;
    public Transform currentCheckpoint;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private float fadeTime = 1f;
    private bool isRespawning = false;
    public bool IsRespawning => isRespawning;

    void Awake()
    {
        Instance = this;
    }

    public void Respawn()
    {
        Debug.Log("Respawn 호출");
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        Debug.Log("RespawnRoutine 시작");
        isRespawning = true;

        InputLockManager.DialogueLock = true;

        // 암전
        yield return screenFader.FadeOut(fadeTime);
        Debug.Log("암전 완료");
        player.position = currentCheckpoint.position;
        Debug.Log("플레이어 이동");
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DetectionUI.Instance.UpdateUI(0);

        CameraPivotRotator.Instance.SnapToPlayer();

        yield return null;

        // 밝아짐
        yield return screenFader.FadeIn(fadeTime);
        Debug.Log("페이드인 완료");
        player.position = currentCheckpoint.position;
        InputLockManager.DialogueLock = false;
        isRespawning = false;

        foreach (var cctv in FindObjectsOfType<CCTVController>())
            cctv.ResetDetection();

        //연속된 게임오버 후 팁 표시
        if (CCTVController.pendingCaptionTip != null)
        {
            DialogueData tip = CCTVController.pendingCaptionTip;
            CCTVController.pendingCaptionTip = null;   // 먼저 비워서 중복 재생 방지
            while (DialogueManager.Instance.IsPlaying) yield return null;
            DialogueManager.Instance.StartDialogue(tip);
        }
    }
}