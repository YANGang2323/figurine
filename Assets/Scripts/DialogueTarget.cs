using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DialogueTarget : MonoBehaviour
{
    public string targetID;

    private static Dictionary<string, DialogueTarget> targets
        = new Dictionary<string, DialogueTarget>();
    [SerializeField] private string progressFlag;
    public string ProgressFlag => progressFlag;

    //게임이 처음 켜질 때 씬 로드 감지 시스템을 등록
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitializeSceneEvent()
    {
        // 씬이 로드될 때마다 온체인지 메서드를 실행하라고 유니티에게 예약함
        SceneManager.sceneLoaded += OnSceneLoaded;

        // 첫 번째 씬의 오브젝트들도 수집하기 위해 수동으로 1회 실행
        RefreshAllTargets();
    }
    //새로운 씬이 열릴 때마다 유니티가 자동으로 이 함수를 호출
    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshAllTargets();
    }
    //실제 씬 안의 모든 타겟(꺼진 것 포함)을 긁어모으는 핵심 함수
    private static void RefreshAllTargets()
    {
        targets.Clear();

        // 현재 로드된 씬에 존재하는 꺼진 오브젝트까지 전부 수집
        DialogueTarget[] allTargets =
            Object.FindObjectsByType<DialogueTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (DialogueTarget target in allTargets)
        {
            if (string.IsNullOrEmpty(target.targetID))
                continue;

            targets[target.targetID] = target;
        }

        Debug.Log($"[DialogueTarget] 씬 갱신됨! 총 {targets.Count}개의 타겟 등록 완료. (bag01 등록 여부: {targets.ContainsKey("bag01")})");
    }

    //static void RegisterAll()
    //{
    //    targets.Clear();

    //    DialogueTarget[] allTargets =
    //        Object.FindObjectsByType<DialogueTarget>(FindObjectsInactive.Include, FindObjectsSortMode.None);

    //    foreach (DialogueTarget target in allTargets)
    //    {
    //        if (string.IsNullOrEmpty(target.targetID))
    //            continue;

    //        targets[target.targetID] = target;
    //    }
    //    Debug.Log($"[DialogueTarget] 총 {targets.Count}개의 타겟 등록 완료! (bag01 등록 여부: {targets.ContainsKey("bag01")})");
    //}

    private void Awake()
    {
        if (!string.IsNullOrEmpty(targetID))
        {
            targets[targetID] = this;
        }
    }

    private void OnDestroy()
    {
        if (!string.IsNullOrEmpty(targetID) && targets.ContainsKey(targetID))
        {
            if (targets[targetID] == this)
            {
                targets.Remove(targetID);
            }
        }
    }

    public static DialogueTarget Find(string id)
    {
        if (string.IsNullOrEmpty(id))
            return null;

        targets.TryGetValue(id, out DialogueTarget target);
        return target;
    }

    public void Enable()
    {
        gameObject.SetActive(true);
    }

    public void Disable()
    {
        if (!string.IsNullOrEmpty(ProgressFlag))
        {
            ProgressManager.Instance.AddFlag(ProgressFlag);
        }
        gameObject.SetActive(false);
    }
}