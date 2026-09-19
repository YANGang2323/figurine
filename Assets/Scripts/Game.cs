using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System;
#if UNITY_EDITOR
using UnityEditor.Build.Content;
using static UnityEditor.Progress;
#endif
using UnityEngine.EventSystems;

public class Game : MonoBehaviour
{
    public GameObject ESCMenuUI;
    public GameObject OptionUI;
    public GameObject SaveUI;
    public GameObject Save;

    //public Text name;
    public Button[] slotButtons; // 버튼들을 인스펙터에서 연결
    public TMP_Text[] slotText;		// 슬롯버튼 아래에 존재하는 Text들
    public int slotNum;

    bool[] savefile = new bool[5];	// 세이브파일 존재유무 저장
    //private bool isFirstTab = true;
    //bool isCorrectCameraAngle = true; //카메라 각도 사이드뷰에서 시작

    private string savePath;
    private string nowstage;
    private string nowpiece;
    private string nowinfo;
    private string nowcctvAlarm;

    //[SerializeField] private DialogManager dialogManager;

    void Start()
    {
        GameData data = DataManager.instance.nowGame;
        SlotText();

        nowstage = data.stage.ToString();
        nowpiece = data.piece.ToString();
        nowinfo = data.info.ToString();
    

        Debug.Log($"스테이지: {nowstage}, 조각: {nowpiece}, 정보: {nowinfo}");
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) {
            if (!ESCMenuUI.activeSelf)
            {
                ESCMenuUI.SetActive(true);
                UISoundManager.Instance.PlayOpen();

                InputLockManager.ESCMenuLock = true; // LockMove, LockInteraction, LockCamera 잠금
                InputLockManager.Refresh();
            }
            else
            {
                ESCMenuUI.SetActive(false);
                UISoundManager.Instance.PlayClose();

                InputLockManager.ESCMenuLock = false; // LockMove, LockInteraction, LockCamera 잠금 해제
            }
            //ESCMenuUI.SetActive(true);
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
        UISoundManager.Instance.PlayClick();
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
    public void SaveCreat()	// Save슬롯 버튼 클릭 시
    {
        UISoundManager.Instance.PlayClick();
        Save.gameObject.SetActive(true);
    }
    public void SaveDestr()	// Save 팝업창 no선택시
    {
        UISoundManager.Instance.PlayClick();
        Save.gameObject.SetActive(false);
    }
   
    public void OnClickBackToGame()
    {
        ESCMenuUI.SetActive(false);
        UISoundManager.Instance.PlayClose();

        InputLockManager.ESCMenuLock = false; // LockMove, LockInteraction, LockCamera 잠금 해제
        InputLockManager.Refresh();
    }
    public void OnClickOpenOptionUI()
    {
        OptionUI.SetActive(true);
        UISoundManager.Instance.PlayClick();
    }
    public void OnClickCloseOptionUI()
    {
        OptionUI.SetActive(false);
        UISoundManager.Instance.PlayClick();
    }
    public void OnClickOpenSaveUI()
    {
        SaveUI.SetActive(true);
        UISoundManager.Instance.PlayClick();
    }
    public void OnClickCloseSaveUI()
    {
        SaveUI.SetActive(false);
        UISoundManager.Instance.PlayClick();
    }
    
    public void OnClickBackToMenu()
    {
        ESCMenuUI.SetActive(false);
        UISoundManager.Instance.PlayClose();
        InputLockManager.ESCMenuLock = false; // LockMove, LockInteraction, LockCamera 잠금 해제
        InputLockManager.DialogueLock = false;   // LockMove, LockInteraction, LockCamera 해제
        InputLockManager.CameraRotateLock = false; // LockMove, LockCamera 해제
        InputLockManager.Refresh();
        SceneManager.LoadScene("MainMenu");
    }
    private void SlotText()
    {
        savePath = Application.persistentDataPath; // 저장 경로
        int totalSlots = slotButtons.Length;

        for (int i = 0; i < totalSlots; i++)
        {
            slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>();
            if (slotButtons[i] == null) continue;
            string latestFile = DataManager.instance.FindLatestSaveFile(i);
            if (slotText[i] == null) continue;

            if (latestFile != null)
            {
                // 2. savefile 배열 범위 초과 크래시 방지
                if (i < savefile.Length)
                {
                    savefile[i] = true;
                }
                string fileName = Path.GetFileName(latestFile);

                // 메인메뉴와 동일하게 1번부터 표기되도록 i+1로 매치합니다.
                slotText[i].text = $"Slot {i + 1}: {fileName}";
            }
            else
            {
                if (i < savefile.Length)
                {
                    savefile[i] = false;
                }
                // 파일이 없어도 무조건 Slot 1, Slot 2 형태로 텍스트가 바뀝니다.
                slotText[i].text = $"Slot {i + 1}";
            }
        }
    }

    

}