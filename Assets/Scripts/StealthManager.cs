using System.Collections;
using UnityEngine;

public class StealthManager : MonoBehaviour
{
    public static StealthManager Instance;

    public Transform player;
    public Transform currentCheckpoint;
    [SerializeField] private ScreenFader screenFader;
    [SerializeField] private float fadeTime = 1f;
    private bool isRespawning;

    void Awake()
    {
        Instance = this;
    }

    public void Respawn()
    {
        if (isRespawning)
            return;

        StartCoroutine(RespawnRoutine());
    }

    IEnumerator RespawnRoutine()
    {
        isRespawning = true;

        InputLockManager.DialogueLock = true;

        // ¾ÏÀü
        yield return screenFader.FadeOut(fadeTime);

        player.position = currentCheckpoint.position;

        Rigidbody rb = player.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        DetectionUI.Instance.UpdateUI(0);

        CameraPivotRotator.Instance.SnapToPlayer();

        yield return null;

        // ¹à¾ÆÁü
        yield return screenFader.FadeIn(fadeTime);

        player.position = currentCheckpoint.position;

        InputLockManager.DialogueLock = false;

        isRespawning = false;
    }
}