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
    public GameObject soundOptionUI;
    public GameObject keysetOptionUI;
    public GameObject languageOptionUI;
    public GameObject SaveUI;
    public GameObject Save;

    //public Text name;
    public Button[] slotButtons; // 버튼들을 인스펙터에서 연결
    public TMP_Text[] slotText;		// 슬롯버튼 아래에 존재하는 Text들
    public int slotNum;

    bool[] savefile = new bool[5];	// 세이브파일 존재유무 저장
    //bool isCorrectCameraAngle = true; //카메라 각도 사이드뷰에서 시작

    private string savePath;
    private string nowstage;
    private string nowpiece;
    private string nowinfo;
    private string nowcctvAlarm;

    //[SerializeField] private DialogManager dialogManager;

    void Start()
    {
        OptionUI?.GetComponent<OptionPanel>()?.Bind(OnClickCloseOptionUI);
        SaveUI?.GetComponent<SaveLoadPanel>()?.Bind(inGameSlot, OnClickCloseSaveUI, SaveBtn, SaveDestr);
        GameData data = DataManager.instance.nowGame;
        SlotText();

        nowstage = data.stage.ToString();
        nowpiece = data.piece.ToString();
        nowinfo = data.info.ToString();
    

        Debug.Log($"스테이지: {nowstage}, 조각: {nowpiece}, 정보: {nowinfo}");
    }
    private void Update()
    {
        if ((!InputLockManager.TransitionLock || transitionSaved != null) && Input.GetKeyDown(KeyCode.Escape)) {
            HandleEscape();
        }
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

    private Action transitionSaved;
    private Action transitionSaveCancelled;
    private bool transitionOpenedMenu;

    public void OpenTransitionSave(Action saved, Action cancelled)
    {
        transitionSaved = saved;
        transitionSaveCancelled = cancelled;
        transitionOpenedMenu = !ESCMenuUI.activeSelf;
        ESCMenuUI.SetActive(true);
        Save.SetActive(false);
        SlotText();
        OnClickOpenSaveUI();
    }

    public void SaveBtn()
    {
        try
        {
            DataManager.instance.SaveData();
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            return;
        }
        SlotText();
        UISoundManager.Instance.PlayClick();
        Save.SetActive(false);
        if (transitionSaved != null)
        {
            Action saved = transitionSaved;
            transitionSaved = null;
            transitionSaveCancelled = null;
            SaveUI.SetActive(false);
            saved();
        }

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
        if (transitionSaved != null)
            return;

        ESCMenuUI.SetActive(false);
        UISoundManager.Instance.PlayClose();

        InputLockManager.ESCMenuLock = false; // LockMove, LockInteraction, LockCamera 잠금 해제
        InputLockManager.Refresh();
    }
    // 옵션 UI
    public void OnClickOpenOptionUI()
    {
        if (transitionSaved != null)
            return;

        UISoundManager.Instance.PlayClick();

        Debug.Log("옵션");
        OptionUI.SetActive(true);
        soundOptionUI.SetActive(true);
        keysetOptionUI.SetActive(false);
        languageOptionUI.SetActive(false);
    }

    public void OnClickSoundOption()
    {
        UISoundManager.Instance.PlayClick();
        Debug.Log("사운드 옵션");
        soundOptionUI.SetActive(true);
        keysetOptionUI.SetActive(false);
        languageOptionUI.SetActive(false);
    }
    public void OnClickKeysetOption()
    {
        UISoundManager.Instance.PlayClick();
        Debug.Log("키 설정 옵션");
        soundOptionUI.SetActive(false);
        keysetOptionUI.SetActive(true);
        languageOptionUI.SetActive(false);
    }
    public void OnClickLanguageOption()
    {
        UISoundManager.Instance.PlayClick();
        Debug.Log("언어 옵션");
        soundOptionUI.SetActive(false);
        keysetOptionUI.SetActive(false);
        languageOptionUI.SetActive(true);
    }
    public void OnClickCloseOptionUI()
    {
        OptionUI.SetActive(false);
        UISoundManager.Instance.PlayClick();
    }
    // 세이브 UI
    public void OnClickOpenSaveUI()
    {
        SaveUI.SetActive(true);
        UISoundManager.Instance.PlayClick();
    }
    public void OnClickCloseSaveUI()
    {
        Action cancelled = transitionSaveCancelled;
        transitionSaved = null;
        transitionSaveCancelled = null;
        Save.SetActive(false);
        SaveUI.SetActive(false);
        if (cancelled != null && transitionOpenedMenu)
            ESCMenuUI.SetActive(false);
        transitionOpenedMenu = false;
        cancelled?.Invoke();
        UISoundManager.Instance.PlayClick();
    }
    
    public void OnClickBackToMenu()
    {
        if (transitionSaved != null)
            return;

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
            if (slotButtons[i] == null) continue;
            slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>(true);
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

    private void HandleEscape()
    {
        if (transitionSaved != null)
        {
            if (Save.activeSelf)
                SaveDestr();
            else
                OnClickCloseSaveUI();
            return;
        }

        // 1. 옵션창이 열려 있으면 옵션창부터 닫음
        if (OptionUI.activeSelf)
        {
            //세부 옵션 원상태 후 닫기
            UISoundManager.Instance.PlayClose();
            soundOptionUI.SetActive(true);
            keysetOptionUI.SetActive(false);
            languageOptionUI.SetActive(false);
            OptionUI.SetActive(false);
            return;
        }

        // 2. Save 팝업이 열려 있으면 팝업부터 닫음
        if (Save.activeSelf)
        {
            Save.SetActive(false);
            UISoundManager.Instance.PlayClose();
            return;
        }

        // 3. 세이브 UI가 열려 있으면 닫음
        if (SaveUI.activeSelf)
        {
            OnClickCloseSaveUI();
            return;
        }

        // 4. ESC 메뉴가 열려 있으면 닫음
        if (ESCMenuUI.activeSelf)
        {
            ESCMenuUI.SetActive(false);
            UISoundManager.Instance.PlayClose();

            InputLockManager.ESCMenuLock = false;
            InputLockManager.Refresh();

            return;
        }

        // 5. 아무것도 안 열려 있으면 ESC 메뉴 열기
        ESCMenuUI.SetActive(true);
        UISoundManager.Instance.PlayOpen();

        InputLockManager.ESCMenuLock = true;
        InputLockManager.Refresh();
    }

}
