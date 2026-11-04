using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Playables;
using Unity.VisualScripting;
using Unity.Services.Analytics;

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

    public int EmblemPuzzleState;          // 0: Before, 1: Playing, 2: Cleared
    public int EmblempieceXCurrentStep;    // pieceX의 현재 회전 단계 (0~3)
    public int EmblempieceYCurrentStep;    // pieceY의 현재 회전 단계 (0~3)
    public bool EmblemfirstPieceCompleted; // 첫 번째 조각 완성 여부

    public string startTimeStamp; // 스테이지 시작 시간 기록용
    public string totalGameStartTime;   // 새게임 시작 시간 기록용

    public int totalSaveCount = 0;       // 저장 횟수
    public int totalLoadCount = 0;       // 로드 횟수
    public int totalCctvDetected = 0;    // CCTV 발각 횟수
    public int numSelectFailCount = 0;   // 넘버 셀렉션 실패 횟수
    public int dialogueSkipCount = 0;    // 타이핑 스킵 횟수

    [System.NonSerialized] public float numSelectStartTime; // 넘버셀렉션 진입 시간 (저장안함)
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
        nowGame.totalSaveCount++;   // 저장 횟수 증가
        string PlayCode = nowGame.playCode;
        // 애널리틱스 전송
        var eventData = new Dictionary<string, object> {
            { "PlayCode", PlayCode },
            { "SaveCount", nowGame.totalSaveCount },
            { "CurrentSlot", nowSlot }
        };
        // 앞에 주소(Unity.Services.Analytics.)를 명확하게 붙여서 충돌을 피합니다.
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("player_save");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        AnalyticsService.Instance.RecordEvent(myEvent);
        AnalyticsService.Instance.Flush(); // 즉시 강제 전송

        //플레이어 위치저장
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            nowGame.playerPosition = player.transform.position;
        }

        nowGame.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int slot = nowSlot + 1; //  슬롯 번호를 1부터 시작하도록 조정
        string datetime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{slot}_{PlayCode}_{datetime}.json";
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
            nowGame.totalLoadCount++; // 로드 횟수 증가

            var eventData = new Dictionary<string, object> {
                { "PlayCode", nowGame.playCode },
                { "LoadCount", nowGame.totalLoadCount }
            };
            // 앞에 주소(Unity.Services.Analytics.)를 명확하게 붙여서 충돌을 피합니다.
            Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("player_load");
            foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
            AnalyticsService.Instance.RecordEvent(myEvent);
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
        if (slot < 0 || string.IsNullOrEmpty(path) || !Directory.Exists(path))
            return null;

        // Play codes precede dates in file names; alphabetical order is not save order.
        string latestFile = null;
        System.DateTime latestWriteTime = System.DateTime.MinValue;
        foreach (string file in Directory.GetFiles(path, $"{slot + 1}_*.json"))
        {
            System.DateTime writeTime = File.GetLastWriteTimeUtc(file);
            if (latestFile == null || writeTime > latestWriteTime ||
                (writeTime == latestWriteTime && string.CompareOrdinal(file, latestFile) > 0))
            {
                latestFile = file;
                latestWriteTime = writeTime;
            }
        }
        return latestFile;
    }
    public void DataClear()
    {
        nowSlot = -1;
        nowGame = new GameData();
    }
    public void StageClear(string nextSceneName)
    {
        string currentSceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        // 안전장치 1: 메인메뉴나 타이틀로 돌아가는 로드씬 혹은 지금있는 씬인 경우는 애널리틱스 수집 안 함
        if (currentSceneName == "MainMenu" || nextSceneName == "MainMenu" || nextSceneName == "Title")
        {
            return;
        }

        if (DataManager.instance == null || DataManager.instance.nowGame == null) return;
        var game = DataManager.instance.nowGame;

        if (string.IsNullOrEmpty(game.startTimeStamp))
            game.startTimeStamp = System.DateTime.Now.ToString("o");

        System.DateTime startTime = System.DateTime.Parse(game.startTimeStamp);
        float elapsedSeconds = (float)(System.DateTime.Now - startTime).TotalSeconds;

        var eventData = new Dictionary<string, object> {
        { "PlayCode", game.playCode },
        { "Stage", game.stage },
        { "ClearTimeSeconds", elapsedSeconds }
    };
        IncrementStage();

        // 최신 RecordEvent 방식으로 이벤트 저장
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("stage_clear");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(myEvent);

        // 다음 씬으로 넘어가기 전 버퍼에 있는 데이터를 즉시 강제 전송!
        Unity.Services.Analytics.AnalyticsService.Instance.Flush();

        Debug.Log($"[Analytics] {game.stage}스테이지 클리어 데이터 강제 Flush 완료: {elapsedSeconds}초");

        // 다음 스테이지의 정확한 시간 측정을 위해 타임스탬프를 현재 시간으로 최신화
        game.startTimeStamp = System.DateTime.Now.ToString("o");
    }

    public void EndingClear()
    {
        if (nowGame == null) return;

        // 안전장치: 전체 시작 시간이 만약 비어있다면 매 스테이지 시작 시간이라도 가져와서 계산
        string targetStartTime = string.IsNullOrEmpty(nowGame.totalGameStartTime)
            ? nowGame.startTimeStamp
            : nowGame.totalGameStartTime;

        System.DateTime startTime = System.DateTime.Parse(targetStartTime);
        float totalPlayTimeSeconds = (float)(System.DateTime.Now - startTime).TotalSeconds;

        // 엔딩 시점의 최종 데이터를 구성합니다. (Stage 대신 "Ending" 명시)
        var eventData = new Dictionary<string, object> {
            { "PlayCode", nowGame.playCode },
            { "Stage", nowGame.stage -1 },              // 클리어 당시의 스테이지 번호
            { "ClearType", "Demo_Total_Ending_Clear" },
            { "TotalPlayTimeSeconds", totalPlayTimeSeconds } // 뉴게임부터 엔딩까지의 총 누적 시간!
        };

        IncrementStage();
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("game_ending_clear");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(myEvent);

        // 최종 엔딩이므로 무조건 즉시 강제 전송!
        Unity.Services.Analytics.AnalyticsService.Instance.Flush();

        Debug.Log($"[Analytics] 최종 엔딩 클리어 데이터 강제 Flush 완료: {totalPlayTimeSeconds}초");
        // 엔딩 이후에 혹시 모를 자유 행동이나 연출을 대비해 타임스탬프 최신화
        nowGame.startTimeStamp = System.DateTime.Now.ToString("o");
    }
}
