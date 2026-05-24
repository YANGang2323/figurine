using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StageChanger : MonoBehaviour
{
    [SerializeField] private string nextSceneName; // 이동할 다음 씬 이름
    private bool isTransitioning = false; // 중복 실행 방지용 플래그

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어와 충돌했고, 이미 전환 중이 아니라면 실행
        if (other.CompareTag("Player") && !isTransitioning)
        {
            StartCoroutine(TransitionAfterDelay(1f));
        }
    }

    private IEnumerator TransitionAfterDelay(float delay)
    {
        isTransitioning = true; // 전환 시작 표시

        yield return new WaitForSeconds(delay); // 지정된 시간 동안 대기

        SceneManager.LoadScene(nextSceneName); // 씬 로드
    }
}
