using UnityEngine;
public class InputManager : MonoBehaviour
{
    public static InputManager instance;
    // 나중에 다시 활성화할 UI 관련 변수들
    // public GameObject ESCMenuUI;
    // public bool UIon = false;
    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject); // 새로운 것을 파괴 (기존 유지)
        }
    }
    // 입력 차단 조건 (필요 시 활성화)
    private bool ShouldBlockInput(KeyCode key)
    {
        // 현재는 차단 없음. 나중에 대화/메뉴 시스템 추가 시:
        // if (DialogManager.instance?.isDialogue == true) return true;
        // if (ESCMenuUI?.activeSelf == true) return true;
        return false;
    }
    public bool GetKey(KeyCode key)
    {
        if (ShouldBlockInput(key))
            return false;

        return Input.GetKey(key);  // 실제 Unity 입력 확인
    }
    public bool GetKeyDown(KeyCode key)
    {
        if (ShouldBlockInput(key))
            return false;

        return Input.GetKeyDown(key);  // 실제 Unity 입력 확인
    }
    public bool GetKeyUp(KeyCode key)
    {
        if (ShouldBlockInput(key))
            return false;

        return Input.GetKeyUp(key);  // 실제 Unity 입력 확인
    }
}