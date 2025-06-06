using System;
using System.Collections.Generic;
using UnityEngine;

public class StageManager : SingletonBase<StageManager>
{
    [Header("UI")]

    [SerializeField] private GameObject _optionPanel;
    [SerializeField] private GameObject _stageInfo;
    [SerializeField] private StageInfoController _stageInfoController;

    public DataTable.Stage_Data StageData { get; private set; }

    public int TotalWave { get; private set; } // 전체 웨이브 수
    public int CurrWave { get; private set; } // 현재 웨이브
    public int CurrHealth { get; private set; } // 현재 체력
    public float CurrGold { get; private set; } // 현재 골드
    public int StarsCount { get; private set; }

    [Header("Stage")]

    [SerializeField] private GameObject _stage;
    [SerializeField] private StartBattleButtonController _startBattleBtnController;
    private int _stageIdx;
    public List<Transform> SpawnPointList { get; private set; }
    public List<Transform> StartPointList { get; private set; }
    public Transform EndPoint { get; private set; }

    [Header("Sfx Pools")]
    [SerializeField] private PoolManager.PoolConfig[] _poolConfigs;

    private MonsterSpawner _monsterSpawner;
    private MonsterEvolutionUI _monsterEvolutionUI;

    public Action OnChangeGold;

    protected override void Awake()
    {
        base.Awake();
        PoolManager.Instance.AddPools<SfxSoundSource>(_poolConfigs);
        SoundManager.Instance.PlayBGM(BgmType.Stage);
        SetStageInfo();
        SetStageObject();
        SetPointInfo();
    }

    private void Start()
    {
        _stageInfoController = _stageInfo.GetComponent<StageInfoController>();
        _stageInfoController.ChangeUI();
    }

    private void OnEnable()
    {
        Time.timeScale = 1;
        GameManager.Instance.isPlaying = true;
    }

    // stage에 대한 정보 초기화
    private void SetStageInfo()
    {
        // 현재 Stage의 Stat 설정
        _stageIdx = DataManager.Instance.selectedStageIdx;
        StageData = DataManager.Instance.GetStageByIndex(_stageIdx);
        TotalWave = StageData.wave;
        CurrWave = 0;
        CurrHealth = StageData.health;
        CurrGold = StageData.gold;
        StarsCount = 0;
    }

    // Stage 및 하위 오브젝트 캐싱
    private void SetStageObject()
    {
        // 스테이지 동적 로드
        _stage = Instantiate<GameObject>(Resources.Load<GameObject>($"Prefabs/Stage/Stage{_stageIdx + 1}"));
        _stage.name = $"Stage{_stageIdx + 1}";
        SpawnPointList = _stage.GetComponentInChildren<SpawnPoint>().SpawnPointList;
        
        // startBattleBtn에 interWaveDelay필드에 값 저장하기 위해 StageData 세팅 후에 캐싱
        _startBattleBtnController = _stage.GetComponentInChildren<StartBattleButtonController>();
    }

    public void SetMonsterUI(MonsterSpawner spawner, MonsterEvolutionUI evolutionUI)
    {
        if (spawner != null) _monsterSpawner = spawner;
        if (evolutionUI != null) _monsterEvolutionUI = evolutionUI;
    }

    // 스테이지의 시작점과 종료지점 캐싱
    private void SetPointInfo()
    {
        StartPointList = new List<Transform>();
        GameObject[] spawnPoints = GameObject.FindGameObjectsWithTag("HumanSpawnPoint");
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogAssertion("Startpoints not found");
            return;
        }
        foreach (GameObject point in spawnPoints)
        {
            StartPointList.Add(point.transform);
        }
        
        EndPoint = GameObject.FindWithTag("DestinationPoint").transform;
        if (EndPoint == null)
        {
            Debug.LogAssertion("Endpoint not found");
        }
    }

    // Wave 업데이트
    public void UpdateWave()
    {
        // 전체 웨이브가 끝나면 return
        if (CurrWave >= TotalWave) return;
        CurrWave++;
        _stageInfoController.ChangeUI();
    }

    // 현재 스테이지의 모든 웨이브가 끝났는지 확인
    public bool CheckLastWave()
    {
        return (CurrWave == TotalWave);
    }

    // health 변경
    public void ChangeHealth(int health)
    {
        CurrHealth = Mathf.Max(CurrHealth + health, 0);
        _stageInfoController.ChangeUI();
        if (CurrHealth <= 0)
        {
            CurrHealth = 0;            
            GameManager.Instance.GameOver();
        }
    }

    public void ChangeGold(int gold)
    {
        CurrGold += gold;
        _stageInfoController.ChangeUI();

        OnChangeGold?.Invoke();
    }

    // StopPanel 활성화
    public void ShowOptionPanel()
    {
        _optionPanel.SetActive(true);
        Time.timeScale = 0f;
    }

    // 테스트 버튼 클릭
    public void ClickEndWaveBtn()
    {
        _startBattleBtnController.ClickEndWave();
    }

    public void CaculateStars()
    {
        if (CurrHealth >= 20)
        {
            StarsCount = 3;
        }
        else if (CurrHealth > 10 && CurrHealth < 20)
        {
            StarsCount = 2;
        }
        else if (CurrHealth >= 1 && CurrHealth <= 10)
        {
            StarsCount = 1;
        }
    }
    
    public void SavePlayData()
    {
        SaveManager.Instance.UpdatePlayInfo(_stageIdx, StarsCount, true);
    }
}
