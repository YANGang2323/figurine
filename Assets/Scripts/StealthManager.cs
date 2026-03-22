using UnityEngine;

public class StealthManager : MonoBehaviour
{
    public static StealthManager Instance;

    public Transform player;
    public Transform currentCheckpoint;

    void Awake()
    {
        Instance = this;
    }

    public void Respawn()
    {
        player.position = currentCheckpoint.position;

        // 감지 초기화
        DetectionUI.Instance.UpdateUI(0);
    }
}