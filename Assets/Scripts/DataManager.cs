using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using UnityEngine.Playables;
using Unity.VisualScripting;
using Unity.Services.Analytics;

//����: �����͸� ���̽����� ��ȯ->�ܺο� ����
//�ҷ�����: �ܺο��� ���̽� ������-> �����ͷ� ��ȯ
//DataManager.instance.�޼����̸�() ���� ȣ��

public enum SavedCameraView { Unspecified, Front, Side }

public class GameData
{
    // ��������, ������ȣ, ��������
    //public string name;
    public const string DefaultPlayCode = "#000000";
    public string playCode = DefaultPlayCode;
    public int stage = 1;
    public int piece = 0;
    public int info = 0;

    public SavedCameraView cameraView;
    public string sceneName;       // ����� �� �̸�
    public Vector3 playerPosition = Vector3.zero; // �÷��̾� ��ġ
    public List<string> playedDialogues = new List<string>();    // ����� ��ȭ ID ���
    public List<string> progressFlags = new();  // ���� ��Ȳ �÷��� ���

    public int EmblemPuzzleState;          // 0: Before, 1: Playing, 2: Cleared
    public int EmblempieceXCurrentStep;    // pieceX�� ���� ȸ�� �ܰ� (0~3)
    public int EmblempieceYCurrentStep;    // pieceY�� ���� ȸ�� �ܰ� (0~3)
    public bool EmblemfirstPieceCompleted; // ù ��° ���� �ϼ� ����

    public string startTimeStamp; // �������� ���� �ð� ��Ͽ�
    public string totalGameStartTime;   // ������ ���� �ð� ��Ͽ�

    public int totalSaveCount = 0;       // ���� Ƚ��
    public int totalLoadCount = 0;       // �ε� Ƚ��
    public int totalCctvDetected = 0;    // CCTV �߰� Ƚ��
    public int numSelectFailCount = 0;   // �ѹ� ������ ���� Ƚ��
    public int dialogueSkipCount = 0;    // Ÿ���� ��ŵ Ƚ��

    [System.NonSerialized] public float numSelectStartTime; // �ѹ������� ���� �ð� (�������)
}
[DefaultExecutionOrder(-1000)]
public class DataManager : MonoBehaviour
{
    //�̱���
    public static DataManager instance;
    public string path;
    //private int nowSlot; // ���� ���Թ�ȣ
    public int nowSlot;

    public GameData nowGame = new GameData();
    public string LatestSavedPlayCode { get; private set; }

    private void Awake()
    {
        #region �̱��� (���� ����)
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
            return; // �ߺ� ���� ����
        }
        #endregion

        path = Application.persistentDataPath + "/save";
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        LatestSavedPlayCode = ReadLatestSavedPlayCode();
    }

    // ����ƽ �Ӽ��� �߰��Ͽ� �ڵ� ��𼭵� �����ϰ� �����ϰ� ����ϴ�.
    public static DataManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindFirstObjectByType<DataManager>();
                if (instance == null)
                    Debug.LogError("Place DataManager in the scene hierarchy before playing.");
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
    // GameData �� ����
    public void IncrementStage() => nowGame.stage++;
    public void IncrementPiece() => nowGame.piece++;
    public void IncrementInfo() => nowGame.info++;

    // GameData �� ����
    public void SetStage(int value) => nowGame.stage = value;
    public void SetPiece(int value) => nowGame.piece = value;
    public void SetInfo(int value) => nowGame.info = value;

    // ���� �� �������� (���� ����)
    public int GetStage() => nowGame.stage;
    public int GetPiece() => nowGame.piece;
    public int GetInfo() => nowGame.info;

    public void SaveData()
    {
        Debug.Log($"���� ���� ���� ��ȣ: {nowSlot}");
        nowGame.totalSaveCount++;   // ���� Ƚ�� ����
        string PlayCode = nowGame.playCode;
        // �ֳθ�ƽ�� ����
        var eventData = new Dictionary<string, object> {
            { "PlayCode", PlayCode },
            { "SaveCount", nowGame.totalSaveCount },
            { "CurrentSlot", nowSlot }
        };
        // �տ� �ּ�(Unity.Services.Analytics.)�� ��Ȯ�ϰ� �ٿ��� �浹�� ���մϴ�.
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("player_save");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        try
        {
            AnalyticsService.Instance.RecordEvent(myEvent);
            AnalyticsService.Instance.Flush();
        }
        catch (System.Exception error)
        {
            Debug.LogWarning($"Save analytics failed: {error.Message}");
        }

        //�÷��̾� ��ġ����
        GameObject player = GameObject.FindWithTag("Player");
        CameraPivotRotator pivot = CameraPivotRotator.Instance;
        nowGame.cameraView = SavedCameraView.Unspecified;
        if (player != null)
        {
            if (pivot != null)
            {
                nowGame.playerPosition = pivot.ToSavePosition(player.transform.position);
                nowGame.cameraView = pivot.SaveSideView ? SavedCameraView.Side : SavedCameraView.Front;
            }
            else
            {
                nowGame.playerPosition = player.transform.position;
            }
        }

        nowGame.sceneName = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
        int slot = nowSlot + 1; //  ���� ��ȣ�� 1���� �����ϵ��� ����
        string datetime = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string filename = $"{slot}_{PlayCode}_{datetime}.json";
        string data = JsonUtility.ToJson(nowGame); //string��
        Debug.Log(datetime); Debug.Log(filename); Debug.Log(path);

        File.WriteAllText(Path.Combine(path, filename), data);
        if (HasPlayCode(PlayCode))
            LatestSavedPlayCode = PlayCode;
    }

    public static bool HasPlayCode(string playCode)
    {
        return !string.IsNullOrWhiteSpace(playCode) && playCode != GameData.DefaultPlayCode;
    }

    private string ReadLatestSavedPlayCode()
    {
        var saves = new DirectoryInfo(path).GetFiles("*_*.json")
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .ThenByDescending(file => file.Name, System.StringComparer.Ordinal);

        foreach (FileInfo save in saves)
        {
            try
            {
                var game = JsonUtility.FromJson<GameData>(File.ReadAllText(save.FullName));
                if (HasPlayCode(game?.playCode))
                    return game.playCode;
            }
            catch (System.Exception exception) when (exception is IOException ||
                exception is System.UnauthorizedAccessException || exception is System.ArgumentException)
            {
                Debug.LogWarning($"Could not read play code from {save.Name}: {exception.Message}");
            }
        }

        return null;
    }

    public void LoadData()
    {
        string file = FindLatestSaveFile(nowSlot);
        if (file != null)
        {
            string data = File.ReadAllText(file);
            nowGame = JsonUtility.FromJson<GameData>(data);
            nowGame.totalLoadCount++; // �ε� Ƚ�� ����

            var eventData = new Dictionary<string, object> {
                { "PlayCode", nowGame.playCode },
                { "LoadCount", nowGame.totalLoadCount }
            };
            // �տ� �ּ�(Unity.Services.Analytics.)�� ��Ȯ�ϰ� �ٿ��� �浹�� ���մϴ�.
            Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("player_load");
            foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
            AnalyticsService.Instance.RecordEvent(myEvent);
        }
        else
        {
            nowGame = new GameData();
        }

        //string data = File.ReadAllText(path + nowSlot.ToString());

        //nowGame = JsonUtility.FromJson<GameData>(data); //string��
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
        // ������ġ 1: ���θ޴��� Ÿ��Ʋ�� ���ư��� �ε�� Ȥ�� �����ִ� ���� ���� �ֳθ�ƽ�� ���� �� ��
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

        // �ֽ� RecordEvent ������� �̺�Ʈ ����
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("stage_clear");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(myEvent);

        // ���� ������ �Ѿ�� �� ���ۿ� �ִ� �����͸� ��� ���� ����!
        Unity.Services.Analytics.AnalyticsService.Instance.Flush();

        Debug.Log($"[Analytics] {game.stage}�������� Ŭ���� ������ ���� Flush �Ϸ�: {elapsedSeconds}��");

        // ���� ���������� ��Ȯ�� �ð� ������ ���� Ÿ�ӽ������� ���� �ð����� �ֽ�ȭ
        game.startTimeStamp = System.DateTime.Now.ToString("o");
    }

    public void EndingClear()
    {
        if (nowGame == null) return;

        // ������ġ: ��ü ���� �ð��� ���� ����ִٸ� �� �������� ���� �ð��̶� �����ͼ� ���
        string targetStartTime = string.IsNullOrEmpty(nowGame.totalGameStartTime)
            ? nowGame.startTimeStamp
            : nowGame.totalGameStartTime;

        System.DateTime startTime = System.DateTime.Parse(targetStartTime);
        float totalPlayTimeSeconds = (float)(System.DateTime.Now - startTime).TotalSeconds;

        // ���� ������ ���� �����͸� �����մϴ�. (Stage ��� "Ending" ����)
        var eventData = new Dictionary<string, object> {
            { "PlayCode", nowGame.playCode },
            { "Stage", nowGame.stage -1 },              // Ŭ���� ����� �������� ��ȣ
            { "ClearType", "Demo_Total_Ending_Clear" },
            { "TotalPlayTimeSeconds", totalPlayTimeSeconds } // �����Ӻ��� ���������� �� ���� �ð�!
        };

        IncrementStage();
        Unity.Services.Analytics.CustomEvent myEvent = new Unity.Services.Analytics.CustomEvent("game_ending_clear");
        foreach (var param in eventData) myEvent.Add(param.Key, param.Value);
        Unity.Services.Analytics.AnalyticsService.Instance.RecordEvent(myEvent);

        // ���� �����̹Ƿ� ������ ��� ���� ����!
        Unity.Services.Analytics.AnalyticsService.Instance.Flush();

        Debug.Log($"[Analytics] ���� ���� Ŭ���� ������ ���� Flush �Ϸ�: {totalPlayTimeSeconds}��");
        // ���� ���Ŀ� Ȥ�� �� ���� �ൿ�̳� ������ ����� Ÿ�ӽ����� �ֽ�ȭ
        nowGame.startTimeStamp = System.DateTime.Now.ToString("o");
    }
}
