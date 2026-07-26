using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Playables;
using Unity.VisualScripting;
//저장: 데이터를 제이슨으로 변환->외부에 저장
//불러오기: 외부에서 제이슨 가져옴-> 데이터로 변환
//DataManager.instance.메서드이름() 으로 호출

public class GameData
{
    // 스테이지, 쪽지번호, 정보개수
    //public string name;
    public string playCode = "#000000";
    public int stage = 1;
    public int piece = 0;
    public int info = 0;

    public string sceneName;       // 저장된 씬 이름
    public Vector3 playerPosition = Vector3.zero; // 플레이어 위치
    public List<string> playedDialogues = new List<string>();    // 재생된 대화 ID 목록
    public List<string> progressFlags = new();  // 진행 상황 플래그 목록
}
public class DataManager : MonoBehaviour
{
    //싱글톤
    public static DataManager instance;
    public string path;
    //private int nowSlot; // 현재 슬롯번호
    public int nowSlot;

    public GameData nowGame = new GameData();

    private void Awake()
    {
        #region 싱글톤 (개선 버전)
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // 중복 생성 방지
        }
        #endregion

        path = Application.persistentDataPath + "/save";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
    }

    // 스태틱 속성을 추가하여 코드 어디서든 안전하게 접근하게 만듭니다.
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                // 씬에 없으면 새로 만들어서라도 반환
                GameObject go = new GameObject("DataManager");
                instance = go.AddComponent<DataManager>();
            }
            return instance;
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    // GameData 값 증가
    public void IncrementStage() => nowGame.stage++;
    public void IncrementPiece() => nowGame.piece++;
    public void IncrementInfo() => nowGame.info++;

    // GameData 값 설정
    public void SetStage(int value) => nowGame.stage = value;
    public void SetPiece(int value) => nowGame.piece = value;
    public void SetInfo(int value) => nowGame.info = value;

    // 현재 값 가져오기 (선택 사항)
    public int GetStage() => nowGame.stage;
    public int GetPiece() => nowGame.piece;
    public int GetInfo() => nowGame.info;

    public void SaveData()
    {
        Debug.Log($"현재 저장 슬롯 번호: {nowSlot}");
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            nowGame.playerPosition = player.transform.position;
        }

        nowGame.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

        string datetime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"save_{nowSlot}_{datetime}.json";
        string data = JsonUtility.ToJson(nowGame); //string값
        Debug.Log(datetime); Debug.Log(filename); Debug.Log(path);

        File.WriteAllText(Path.Combine(path, filename), data);
    }
    public void LoadData()
    {
        string file = FindLatestSaveFile(nowSlot);
        if (file != null)
        {
            string data = File.ReadAllText(file);
            nowGame = JsonUtility.FromJson<GameData>(data);
        }
        else
        {
            nowGame = new GameData();
        }

        //string data = File.ReadAllText(path + nowSlot.ToString());

        //nowGame = JsonUtility.FromJson<GameData>(data); //string값
    }
    public string FindLatestSaveFile(int slot)
    {
        string[] files = Directory.GetFiles(path, $"save_{slot}_*.json");
        if (files.Length == 0) return null;

        System.Array.Sort(files);
        return files[files.Length - 1]; // 가장 최근 파일
    }
    public void DataClear()
    {
        nowSlot = -1;
        nowGame = new GameData();
    }
}
