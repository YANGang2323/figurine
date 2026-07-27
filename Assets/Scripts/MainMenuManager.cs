using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.VisualScripting;
using TMPro;
using Unity.Services.Analytics;

public class MainMenuManager : MonoBehaviour
{
    public GameObject optionUI;
    public GameObject LoadUI;
    public Button[] slotButtons; // 버튼들을 인스펙터에서 연결
    public TMP_Text[] slotText;		// 슬롯버튼 아래에 존재하는 Text들

    bool[] savefile = new bool[4];	// 세이브파일 존재유무 저장

    private string savePath;
    public string NewGameScene = "Stage1";
    void Start()
    {
        savePath = Application.persistentDataPath; // 저장 경로
        //SlotTextEdit();
        //for (int i = 0; i < 5; i++)
        //{
        //    slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>();
        //    string latestFile = DataManager.instance.FindLatestSaveFile(i);
        //    if (savefile[i] != false)// 슬롯별로 저장된 데이터가 존재하는지 판단.
        //    {
        //        savefile[i] = true;			// 해당 슬롯 번호의 bool배열 true로 변환
        //        string fileName = Path.GetFileName(latestFile);
        //        DataManager.instance.nowSlot = i;	// 선택한 슬롯 번호 저장
        //        DataManager.instance.LoadData();	// 해당 슬롯 데이터 불러옴

        //        slotText[i].text = $"Slot {i}: {fileName}";
        //    }
        //    else	// 데이터가 없는 경우
        //    {
        //        slotText[i].text = $"Slot {i}";
        //    }
        //}
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void OnClickNewGame()
    {
        Debug.Log("새 게임");
        // 1. 중복되지 않는 영어 대문자+숫자 4자리 조합 생성
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[4];
        System.Random random = new System.Random();
        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        // 2. 새 게임 데이터 세팅
        DataManager.instance.nowGame = new GameData();
        DataManager.instance.nowGame.playCode = "#" + new string(stringChars);
        DataManager.instance.nowGame.startTimeStamp = System.DateTime.Now.ToString("o"); // 현재 시간 기록
        DataManager.instance.nowGame.totalGameStartTime = System.DateTime.Now.ToString("o"); // 새게임 시작 시간 기록    

        // 3. 애널리틱스 전송: 게임 최초 실행 및 플레이코드 부여
        var eventData = new Dictionary<string, object> {
            { "PlayCode", DataManager.instance.nowGame.playCode }
        };
        // 앞에 주소(Unity.Services.Analytics.)를 명확하게 붙여서 충돌을 피합니다.
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("game_start");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        AnalyticsService.Instance.RecordEvent(myEvent);

        SceneManager.LoadScene(NewGameScene);
    }

    public void OnClickLoad()
    {
        Debug.Log("불러오기");
        SlotTextEdit();
        LoadUI.SetActive(true);
    }
    public void Slot(int number)	// 슬롯의 기능 구현
    {
        DataManager.instance.nowSlot = number;	// 슬롯의 번호를 슬롯번호로 입력함.

        if (number < savefile.Length && savefile[number])	// bool 배열에서 현재 슬롯번호가 true라면 = 데이터 존재한다는 뜻
        {
            DataManager.instance.LoadData();	// 데이터를 로드하고
            GoGame();	// 게임씬으로 이동
        }
        else	// bool 배열에서 현재 슬롯번호가 false라면 데이터가 없다는 뜻
        {
            Debug.Log("데이터가 저장되지 않았습니다. 게임 씬으로 이동할 수 없습니다.");
            //SaveCreat();	// 저장 UI 활성화
        }
    }
    public void GoGame()	// 게임씬으로 이동
    {
        if (!savefile[DataManager.instance.nowSlot])	// 현재 슬롯번호의 데이터가 없다면
        {
            Debug.Log("데이터가 저장되지 않았습니다. 게임 씬으로 이동할 수 없습니다.");
            return;
        }
        string targetScene = DataManager.instance.nowGame.sceneName;
        SceneManager.sceneLoaded += OnSceneLoaded; // 씬 로드 완료 후 처리 연결
        SceneManager.LoadScene(targetScene);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = DataManager.instance.nowGame.playerPosition;
        }
    }
    public void OnClickDemo2()
    {
        Debug.Log("데모2");
        NewGameScene = "Demo2";
        SceneManager.LoadScene(NewGameScene);
    }
    public void OnClickPipe()
    {
        Debug.Log("파이프씬");
        NewGameScene = "Pipe";
        SceneManager.LoadScene(NewGameScene);
    }
    public void OnClickOption()
    {
        Debug.Log("옵션");
        optionUI.SetActive(true);
    }

    public void OnClickQuit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnClickBackToMenu()
    {
        optionUI.SetActive(false);
        LoadUI.SetActive(false);
    }
    private void SlotTextEdit()
    {
        savePath = Application.persistentDataPath; // 저장 경로
        int totalSlots = slotButtons.Length;

        for (int i = 0; i < totalSlots; i++)
        {
            if (slotButtons[i] == null) continue;
            slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>();
            if (slotText[i] == null) continue;
            string latestFile = DataManager.instance.FindLatestSaveFile(i);

            if (latestFile != null)
            {
                if (i < savefile.Length)
                {
                    savefile[i] = true;
                }
                string fileName = Path.GetFileName(latestFile);

                // 텍스트만 업데이트 (슬롯 상태는 건드리지 않음)
                slotText[i].text = $"Slot {i+1}: {fileName}";
            }
            else
            {
                if (i < savefile.Length)
                {
                    savefile[i] = false;
                }
                slotText[i].text = $"Slot {i+1}";
            }
        }
    }
}
