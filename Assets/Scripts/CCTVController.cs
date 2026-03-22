using UnityEngine;

public class CCTVController : MonoBehaviour
{
    public float viewAngle = 60f;
    public float viewDistance = 8f;

    public float rotateSpeed = 30f;
    public float rotateLimit = 45f;

    public float detectTime = 2f;

    public float height = 3f; // 감지 높이

    float detectTimer;
    float currentAngle;
    bool right = true;

    Transform player;

    public LayerMask obstacleMask; // 벽
    public LayerMask playerMask;   // 플레이어

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (DetectPlayer())
        {
            detectTimer += Time.deltaTime;
        }
        else
        {
            detectTimer -= Time.deltaTime;
        }

        detectTimer = Mathf.Clamp(detectTimer, 0, detectTime);

        // UI 업데이트
        DetectionUI.Instance.UpdateUI(detectTimer / detectTime);

        if (detectTimer >= detectTime)
        {
            GameOver();
        }
    }



    bool DetectPlayer()
    {
        if (player == null) return false;

        Vector3 origin = transform.position + Vector3.up * 1.5f;
        Vector3 dir = player.position - origin;

        if (dir.magnitude > viewDistance)
            return false;

        // 평면 기준 각도
        Vector3 flatDir = new Vector3(dir.x, 0, dir.z);
        float angle = Vector3.Angle(transform.forward, flatDir);

        if (angle < viewAngle * 0.5f)
        {
            RaycastHit hit;

            // LayerMask 아예 안 씀 (핵심)
            if (Physics.Raycast(origin, dir.normalized, out hit, viewDistance))
            {
                Debug.Log("Hit: " + hit.collider.name + " / Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

                if (hit.collider.CompareTag("Player"))
                    return true;
            }
        }

        return false;
    }

    void GameOver()
    {
        Debug.Log("Game Over");
    }
}