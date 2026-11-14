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
        Debug.Log("Respawn ȣ��");
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        Debug.Log("RespawnRoutine ����");
        isRespawning = true;

        InputLockManager.DialogueLock = true;

        // ����
        yield return screenFader.FadeOut(fadeTime);
        Debug.Log("���� �Ϸ�");
        player.position = currentCheckpoint.position;
        Debug.Log("�÷��̾� �̵�");
        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DetectionUI.Instance.UpdateUI(0);

        CameraPivotRotator.Instance.SnapToPlayer();

        yield return null;

        // �����
        yield return screenFader.FadeIn(fadeTime);
        Debug.Log("���̵��� �Ϸ�");
        InputLockManager.DialogueLock = false;
        isRespawning = false;

        foreach (var cctv in FindObjectsOfType<CCTVController>())
            cctv.ResetDetection();

        //���ӵ� ���ӿ��� �� �� ǥ��
        if (CCTVController.pendingCaptionTip != null)
        {
            DialogueData tip = CCTVController.pendingCaptionTip;
            CCTVController.pendingCaptionTip = null;   // ���� ����� �ߺ� ��� ����
            while (DialogueManager.Instance.IsPlaying) yield return null;
            DialogueManager.Instance.StartDialogue(tip);
        }
    }
}