using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.IO;
using Unity.VisualScripting;
using TMPro;
using Unity.Services.Analytics;

public class MainMenuManager : MonoBehaviour
{
    public GameObject optionUI;
    public GameObject soundOptionUI;
    public GameObject keysetOptionUI;
    public GameObject languageOptionUI;
    public GameObject LoadUI;
    [SerializeField] private Button firstMenuButton;
    public Button[] slotButtons; // ��ư���� �ν����Ϳ��� ����
    public TMP_Text[] slotText;		// ���Թ�ư �Ʒ��� �����ϴ� Text��

    bool[] savefile = new bool[4];	// ���̺����� �������� ����

    private string savePath;
    public string NewGameScene = "Stage1";
    void Start()
    {
        optionUI?.GetComponent<OptionPanel>()?.Bind(OnClickBackToMenu);
        LoadUI?.GetComponent<SaveLoadPanel>()?.Bind(Slot, OnClickBackToMenu);
        savePath = Application.persistentDataPath; // ���� ���?        //SlotTextEdit();
        //for (int i = 0; i < 5; i++)
        //{
        //    slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>();
        //    string latestFile = DataManager.instance.FindLatestSaveFile(i);
        //    if (savefile[i] != false)// ���Ժ��� �����?�����Ͱ� �����ϴ��� �Ǵ�.
        //    {
        //        savefile[i] = true;			// �ش� ���� ��ȣ�� bool�迭 true�� ��ȯ
        //        string fileName = Path.GetFileName(latestFile);
        //        DataManager.instance.nowSlot = i;	// ������ ���� ��ȣ ����
        //        DataManager.instance.LoadData();	// �ش� ���� ������ �ҷ���

        //        slotText[i].text = $"Slot {i}: {fileName}";
        //    }
        //    else	// �����Ͱ� ���� ���?        //    {
        //        slotText[i].text = $"Slot {i}";
        //    }
        //}
    }

    // Update is called once per frame
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HandleEscape();
            return;
        }

        var eventSystem = EventSystem.current;
        if (eventSystem != null && eventSystem.currentSelectedGameObject == null &&
            firstMenuButton != null && !optionUI.activeSelf && !LoadUI.activeSelf &&
            (!Mathf.Approximately(Input.GetAxisRaw("Horizontal"), 0f) ||
             !Mathf.Approximately(Input.GetAxisRaw("Vertical"), 0f)))
        {
            firstMenuButton.Select();
        }
    }
    public void OnClickNewGame()
    {
        UISoundManager.Instance.PlayClick();

        Debug.Log("�� ����");
        // 1. �ߺ����� �ʴ� ���� �빮��+���� 4�ڸ� ���� ����
        string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        char[] stringChars = new char[4];
        System.Random random = new System.Random();
        for (int i = 0; i < stringChars.Length; i++)
        {
            stringChars[i] = chars[random.Next(chars.Length)];
        }

        // 2. �� ���� ������ ����
        DataManager.instance.nowGame = new GameData();
        DataManager.instance.nowGame.playCode = "#" + new string(stringChars);
        DataManager.instance.nowGame.startTimeStamp = System.DateTime.Now.ToString("o"); // ���� �ð� ���?        DataManager.instance.nowGame.totalGameStartTime = System.DateTime.Now.ToString("o"); // ������ ���� �ð� ���?   

        // 3. �ֳθ�ƽ�� ����: ���� ���� ���� �� �÷����ڵ� �ο�
        var eventData = new Dictionary<string, object> {
            { "PlayCode", DataManager.instance.nowGame.playCode }
        };
        // �տ� �ּ�(Unity.Services.Analytics.)�� ��Ȯ�ϰ� �ٿ��� �浹�� ���մϴ�.
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("game_start");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        AnalyticsService.Instance.RecordEvent(myEvent);

        SceneManager.LoadScene(NewGameScene);
    }

    public void OnClickLoad()
    {
        UISoundManager.Instance.PlayClick();

        Debug.Log("�ҷ�����");
        SlotTextEdit();
        LoadUI.SetActive(true);
    }
    public void Slot(int number)	// ������ ���?����
    {
        DataManager.instance.nowSlot = number;	// ������ ��ȣ�� ���Թ�ȣ�� �Է���.

        if (number < savefile.Length && savefile[number])	// bool �迭���� ���� ���Թ�ȣ�� true���?= ������ �����Ѵٴ� ��
        {
            DataManager.instance.LoadData();	// �����͸� �ε��ϰ�
            GoGame();	// ���Ӿ����� �̵�
        }
        else	// bool �迭���� ���� ���Թ�ȣ�� false���?�����Ͱ� ���ٴ� ��
        {
            Debug.Log("�����Ͱ� �������?�ʾҽ��ϴ�. ���� ������ �̵��� �� �����ϴ�.");
            //SaveCreat();	// ���� UI Ȱ��ȭ
        }
    }
    public void GoGame()	// ���Ӿ����� �̵�
    {
        if (!savefile[DataManager.instance.nowSlot])	// ���� ���Թ�ȣ�� �����Ͱ� ���ٸ�
        {
            Debug.Log("�����Ͱ� �������?�ʾҽ��ϴ�. ���� ������ �̵��� �� �����ϴ�.");
            return;
        }
        string targetScene = DataManager.instance.nowGame.sceneName;
        SceneManager.sceneLoaded += OnSceneLoaded; // �� �ε� �Ϸ� �� ó�� ����
        SceneManager.LoadScene(targetScene);
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            GameData save = DataManager.instance.nowGame;
            player.transform.position = save.playerPosition;
            if (player.TryGetComponent(out Rigidbody body))
                body.position = save.playerPosition;

            CameraPivotRotator pivot = CameraPivotRotator.Instance;
            if (pivot != null && pivot.gameObject.scene == scene)
                pivot.ViewOnLoad = save.cameraView;
        }
    }
    public void OnClickDemo2()
    {
        UISoundManager.Instance.PlayClick();

        Debug.Log("����2");
        NewGameScene = "Demo2";
        SceneManager.LoadScene(NewGameScene);
    }
    public void OnClickPipe()
    {
        UISoundManager.Instance.PlayClick();

        Debug.Log("��������");
        NewGameScene = "Pipe";
        SceneManager.LoadScene(NewGameScene);
    }
    public void OnClickOption()
    {
        UISoundManager.Instance.PlayClick();

        Debug.Log("�ɼ�");
        optionUI.SetActive(true);
        soundOptionUI.SetActive(true);
        keysetOptionUI.SetActive(false);
        languageOptionUI.SetActive(false);
    }

    public void OnClickSoundOption()
    {
        UISoundManager.Instance.PlayClick();
        Debug.Log("���� �ɼ�");
        soundOptionUI.SetActive(true);
        keysetOptionUI.SetActive(false);
        languageOptionUI.SetActive(false);
    }
    public void OnClickKeysetOption()
    {
        UISoundManager.Instance.PlayClick();
        Debug.Log("Ű ���� �ɼ�");
        soundOptionUI.SetActive(false);
        keysetOptionUI.SetActive(true);
        languageOptionUI.SetActive(false);
    }
    public void OnClickLanguageOption()
    {
        UISoundManager.Instance.PlayClick();
        Debug.Log("���?�ɼ�");
        soundOptionUI.SetActive(false);
        keysetOptionUI.SetActive(false);
        languageOptionUI.SetActive(true);
    }

    public void OnClickQuit()
    {
        QuitApplication();
    }

    private bool quitRequested;
    private void QuitApplication()
    {
        if (quitRequested) return;
        quitRequested = true;
        StartCoroutine(QuitAfterAnalytics());
    }
    private IEnumerator QuitAfterAnalytics()
    {
        PlayExitAnalytics.SendExitSummary();
        yield return new WaitForSecondsRealtime(0.5f);
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
    public void OnClickBackToMenu()
    {
        UISoundManager.Instance.PlayClose();
        optionUI.SetActive(false);
        LoadUI.SetActive(false);
    }
    private void HandleEscape() { 
        if (optionUI.activeSelf)
        {   //���� �ɼ� ������ �� �ݱ�
            UISoundManager.Instance.PlayClose();
            soundOptionUI.SetActive(true);
            keysetOptionUI.SetActive(false);
            languageOptionUI.SetActive(false);
            optionUI.SetActive(false);
            return;
        }
        // Load â�� ���� ������ �ݱ�
        if (LoadUI.activeSelf)
        {
            UISoundManager.Instance.PlayClose();
            LoadUI.SetActive(false);
            return;
        }
    }

    private void SlotTextEdit()
    {
        savePath = Application.persistentDataPath;
        int totalSlots = slotButtons.Length;

        for (int i = 0; i < totalSlots; i++)
        {
            if (slotButtons[i] == null) continue;
            slotText[i] = slotButtons[i].GetComponentInChildren<TMP_Text>(true);
            if (slotText[i] == null) continue;
            string latestFile = DataManager.instance.FindLatestSaveFile(i);

            if (latestFile != null)
            {
                if (i < savefile.Length)
                {
                    savefile[i] = true;
                }
                string fileName = Path.GetFileName(latestFile);

                // �ؽ�Ʈ�� ������Ʈ (���� ���´� �ǵ帮�� ����)
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
