using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Analytics;

public class UnityAnalyticsInitializer : MonoBehaviour
{
    async void Awake()
    {
        try
        {
            // UGS 애널리틱스 서비스 초기화
            await UnityServices.InitializeAsync();
            AnalyticsService.Instance.StartDataCollection();
            PlayExitAnalytics.MarkServiceReady();
            Debug.Log("유니티 애널리틱스 초기화 성공!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"애널리틱스 초기화 실패: {e.Message}");
        }
    }
}
