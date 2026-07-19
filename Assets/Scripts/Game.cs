using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor.Build.Content;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UnityEditor.Progress;
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{
    public GameObject ESCMenuUI;
    public GameObject OptionUI;
    public GameObject SaveUI;
    public GameObject Save;
    //public GameObject pipeManager;

    //public Text name;
    //public Text level;
    //public Text coin;
    //public GameObject[] item;
    public Button[] slotButtons; // 버튼들을 인스펙터에서 연결
    public TMP_Text[] slotText;		// 슬롯버튼 아래에 존재하는 Text들
    public int slotNum;

    bool[] savefile = new bool[5];	// 세이브파일 존재유무 저장
    private bool isFirstTab = true;
    //bool isCorrectCameraAngle = true; //카메라 각도 사이드뷰에서 시작

    private string savePath;
    private string nowstage;
    private string nowpiece;
    private string nowinfo;

    //[SerializeField] private DialogManager dialogManager;
    
    void Start()
    {
        GameData data = DataManager.instance.nowGame;
        SlotText();

        nowstage = data.stage.ToString();
        nowpiece = data.piece.ToString();
        nowinfo = data.info.ToString();
        //pipeManager.SetActive(false);
        //if(nowstage == "1")
        //{
        //    Camera.main.orthographicSize = 2.5f;
        //}
        //else if(nowstage == "2")
        //{
        //    Camera.main.orthographicSize = 2.5f;
        //}
        //else if (nowstage == "3")
        //{
        //    Camera.main.orthographicSize = 4f;
        //    if (CameraPivotRotator.Instance != null)
        //    {
        //        CameraPivotRotator.Instance.cameraType = CameraPivotRotator.CameraMovingType.Follow;
        //        CameraPivotRotator.Instance.isSideView = false; // 필요하면 시점도 설정
        //    }
        //}

        Debug.Log($"스테이지: {nowstage}, 조각: {nowpiece}, 정보: {nowinfo}");

        //if (DialogueDataBase.instance != null && DialogueDataBase.instance.isFinish)
        //{
        //    Dialogue[] startDialogues = DialogueDataBase.instance.GetDialogues(1, 2);
        //    dialogManager.ShowDialogue(startDialogues);
        //}
    }
    private void Update()
    {
        if (Input.GetKey(KeyCode.Escape)) {
            ESCMenuUI.SetActive(true);
        }
        //if (nowstage == "1")
        //{
        //    if (isFirstTab && InputManager.instance.GetKeyDown(KeyCode.Tab))
        //    {
        //        if (DialogueDataBase.instance != null && DialogueDataBase.instance.isFinish)
        //        {
        //            Dialogue[] startDialogues = DialogueDataBase.instance.GetDialogues(3, 3);
        //            dialogManager.ShowDialogue(startDialogues);
        //            isFirstTab = false; // 두 번 다시 안 뜨게
        //        }
        //    }
        //}
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current.IsPointerOverGameObject())
            {
                Debug.Log("⚠️ UI 위 클릭됨 - 클릭 무시됨");
            }
            else
            {
                Debug.Log("✅ UI가 아님 - 클릭 정상 처리 가능");
            }
        }
    }

    public void StageUp()
    {
        DataManager.instance.nowGame.stage++;
        //level.text = "레벨 : " + DataManager.instance.nowGame.level.ToString();
    }

    public void PieceUp()
    {
        DataManager.instance.nowGame.piece++;
        //coin.text = "코인 : " + DataManager.instance.nowGame.coin.ToString();
    }
    public void InfoUp()
    {
        DataManager.instance.nowGame.info++;
        //info.text = "코인 : " + DataManager.instance.nowGame.coin.ToString();
    }

    public void SaveBtn()
    {
        DataManager.instance.SaveData();
        SlotText();
        Save.SetActive(false);

    }
    public void inGameSlot(int number)	// 슬롯의 기능 구현
    {
        //slotNum = number;
        DataManager.instance.nowSlot = number;	// 슬롯의 번호를 슬롯번호로 입력함.
        Debug.Log($"슬롯 버튼 클릭됨. 선택된 슬롯 번호: {number}");
        Debug.Log($"DataManager.nowSlot 설정 완료: {DataManager.instance.nowSlot}");
        SaveCreat();	// 플레이어 닉네임 입력 UI 활성화
        
    }
    public void SaveCreat()	// Save UI를 활성화하는 메소드
    {
        Save.gameObject.SetActive(true);
    }
    public void SaveDestr()	// Save UI를 비활성화하는 메소드
    {
        Save.gameObject.SetActive(false);
    }
   
    public void OnClickBackToGame()
    {
        ESCMenuUI.SetActive(false);
    }
    public void OnClickOpenOptionUI()
    {
        OptionUI.SetActive(true);
    }
    public void OnClickCloseOptionUI()
    {
        OptionUI.SetActive(false);
    }
    public void OnClickOpenSaveUI()
    {
        SaveUI.SetActive(true);
    }
    public void OnClickCloseSaveUI()
    {
        SaveUI.SetActive(false);
    }
    
    public void OnClickBackToMenu()
    {
        ESCMenuUI.SetActive(false);
        SceneManager.LoadScene("MainMenu");
    }
    private void SlotText()
    {
        savePath = Application.persistentDataPath; // 저장 경로
        for (int i = 0; i < 5; i++)
        {
            slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>();
            string latestFile = DataManager.instance.FindLatestSaveFile(i);

            if (latestFile != null)
            {
                savefile[i] = true;
                string fileName = Path.GetFileName(latestFile);

                // 텍스트만 업데이트 (슬롯 상태는 건드리지 않음)
                slotText[i].text = $"Slot {i}: {fileName}";
            }
            else
            {
                savefile[i] = false;
                slotText[i].text = $"Slot {i}";
            }
        }
    }
}