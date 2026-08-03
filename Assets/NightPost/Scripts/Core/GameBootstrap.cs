using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [Header("기본 저장 데이터")]
    [SerializeField] private PlayerSaveData defaultSaveDataTemplate;

    [Header("데이터 및 서비스")]
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LetterService letterService;
    [SerializeField] private DeliveryService deliveryService;
    [SerializeField] private ReplyService replyService;
    [SerializeField] private ProgressionService progressionService;
    [SerializeField] private FacilityService facilityService;
    [SerializeField] private SaveService saveService;

    [Header("게임 흐름")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private OfflineProgressService offlineProgressService;

    // 실제 게임 실행 중 사용하는 저장 데이터
    private PlayerSaveData runtimeSaveData;
    private SortingService sortingService;
    private MorningReportService morningReportService;

    // 기존 저장 데이터가 없는 신규 게임인지 여부
    private bool isNewGame;

    // 편지와 답장의 수집 상태를 도감 항목으로 구성하는 서비스임
    private CollectionService collectionService;

    // 전체 초기화 완료 여부
    private bool isInitialized;

    /// <summary>
    /// 게임 오브젝트가 생성될 때
    /// 런타임 저장 데이터와 각 서비스를 순서대로 초기화한다.
    /// </summary>
    private void Awake()
    {
        // 기본 저장 데이터 템플릿을 복제하고 PlayerDataManager에 전달
        bool isInit = InitializePlayerData();
        if(!isInit)
        {
            Debug.LogError("[GameBootStrap] 플레이어 데이터 초기화 실패");
            return;
        }

        Debug.Log("[GameBootStrap] 플레이어 데이터 초기화 성공");
        // 게임에서 사용하는 각 서비스에 필요한 의존성을 전달하고 초기화
        isInit = InitializeServices();
        if (!isInit)
        {
            Debug.LogError("[GameBootStrap] 서비스 초기화 실패");
            return;
        }
        // 새 게임 시작 시 기본 해금 대상으로 설정된 배달부와 노선을 저장 데이터에 등록
        if(isNewGame) progressionService.ApplyDefaultUnlocks();

        // 불러온 저장 데이터의 누적 배달 완료 수를 기준으로 추가로 해금될 배달부와 노선을 검사
        progressionService.EvaluateProgressUnlocks();

        // 신규 게임이라면 초기 데이터를 SQLite에 최초 저장함
        if (isNewGame)
        {
            if (!saveService.SaveAll())
            {
                Debug.LogError("[GameBootStrap] 신규 게임 데이터 최초 저장 실패");
                return;
            }
        }

        // 모든 초기화 과정이 정상적으로 끝났음을 기록
        isInitialized = true;

        Debug.Log("[GameBootStrap] 서비스 초기화 성공");
    }

    /// <summary>
    /// Awake에서 전체 초기화가 끝난 뒤
    /// 오프라인 진행을 반영하고 변경된 데이터를 저장함
    /// </summary>
    private void Start()
    {
        // 전체 초기화가 실패한 상태라면 오프라인 진행을 처리하지 않음
        if (!isInitialized) return;

        // 마지막 저장 시점 이후 완료된 배달을 처리하고 주기적인 완료 검사를 시작함
        offlineProgressService.CheckOffline();

        // 오프라인 진행이 반영된 현재 데이터를 SQLite에 저장함
        // 저장에 실패했다면 오류 로그를 출력함
        if (!saveService.SaveAll())
        {
            Debug.LogError("[GameBootStrap] 오프라인 진행 반영 후 저장 실패");
        }
    }

    /// <summary>
    /// 게임에서 사용하는 각 서비스에
    /// 필요한 의존성을 전달하고 초기화한다.
    /// </summary>
    /// <returns>
    /// 모든 서비스 초기화에 성공하면 true,
    /// 하나라도 실패하면 false
    /// </returns> 
    private bool InitializeServices()
    {
        // 필수 컴포넌트가 하나라도 연결되지 않았다면 서비스 초기화 실패
        if (staticDataCatalog == null || letterService == null || deliveryService == null || playerDataManager == null ||
            replyService == null || gameFlowController == null || offlineProgressService == null || progressionService == null ||
            facilityService == null) return false;

        bool isInit = facilityService.Initialize(staticDataCatalog, playerDataManager);
        if (!isInit) return false;
        isInit = letterService.Initialize(playerDataManager, staticDataCatalog, facilityService);
        if (!isInit) return false;
        isInit = progressionService.Initialize(staticDataCatalog, playerDataManager);
        if (!isInit) return false;
        morningReportService = new MorningReportService();
        isInit = morningReportService.Initialize(playerDataManager, staticDataCatalog, letterService, progressionService);
        if (!isInit) return false;
        collectionService = new CollectionService();
        isInit = collectionService.Initialize(staticDataCatalog, playerDataManager);
        if (!isInit) return false;
        sortingService = new SortingService();
        isInit = sortingService.Initialize(staticDataCatalog, letterService);
        if (!isInit) return false;
        isInit = deliveryService.Initialize(playerDataManager, staticDataCatalog, progressionService, facilityService);
        if (!isInit) return false;
        isInit = replyService.Initialize(playerDataManager, staticDataCatalog);
        if (!isInit) return false;
        isInit = gameFlowController.Initialize(letterService, sortingService, deliveryService, replyService, facilityService, progressionService, morningReportService, collectionService, playerDataManager);
        if (!isInit) return false;
        isInit = offlineProgressService.Initialize(deliveryService);
        if(!isInit) return false;

        return true;
    }

    /// <summary>
    /// 기본 PlayerSaveData 템플릿을 복제하여
    /// 실제 게임 실행 중 사용할 런타임 저장 데이터를 생성한다.
    /// </summary>
    /// <returns>
    /// 생성된 런타임 PlayerSaveData.
    /// 생성에 실패하면 null
    /// </returns>
    private PlayerSaveData CreateRuntimeData()
    {
        // 기본 PlayerSaveData 템플릿이 연결되어 있는지 확인한
        if(defaultSaveDataTemplate == null) return null;

        // 기본 템플릿을 복제하여 실제 게임 실행 중 사용할 PlayerSaveData를 생성
        runtimeSaveData = Instantiate(defaultSaveDataTemplate);

        // 원본 에셋과 구분할 수 있도록 생성한 런타임 데이터의 이름을 변경
        runtimeSaveData.name = $"{defaultSaveDataTemplate.name}_Runtime";
        // 생성한 런타임 데이터를 반환
        return runtimeSaveData;
    }
    /// <summary>
    /// 런타임 PlayerSaveData를 생성하고
    /// PlayerDataManager에 전달하여 초기화한다.
    /// </summary>
    /// <returns>
    /// 플레이어 데이터 초기화에 성공하면 true,
    /// 실패하면 false
    /// </returns>
    private bool InitializePlayerData()
    {
        // PlayerDataManager 또는 SaveService가 연결되지 않았다면 초기화에 실패함
        if(playerDataManager == null || saveService == null) return false;

        // 기본 템플릿을 복제하여 실제 게임에서 사용할 런타임 데이터를 생성함
        PlayerSaveData playerSaveData = CreateRuntimeData();

        // 런타임 데이터 생성에 실패했다면 초기화에 실패함
        if(playerSaveData == null) return false;

        // 생성한 런타임 데이터를 SaveService에 전달함
        // SaveService 초기화에 실패했다면 초기화에 실패함
        if(!saveService.Initialize(playerSaveData)) return false;

        // SQLite 데이터베이스 파일을 생성하거나 기존 파일에 연결함
        // 데이터베이스 연결에 실패했다면 초기화에 실패함
        if(!saveService.InitializeDatabase()) return false;

        // 게임 저장에 필요한 전체 SQLite 테이블을 생성함
        // 테이블 생성에 실패했다면 초기화에 실패함
        if(!saveService.CreateTables()) return false;

        // 기존 플레이어 저장 데이터가 존재하는지 조회함
        bool isChecked = saveService.TryHasSaveData(out bool hasSaveData);

        // 저장 데이터 존재 여부 조회에 실패했다면 초기화에 실패함
        if(!isChecked) return false;

        // 기존 저장 데이터가 있는지에 따라 신규 게임 여부를 기록함
        isNewGame = !hasSaveData;

        // 기존 저장 데이터가 있다면 전체 데이터를 런타임 데이터에 복원함
        // 기존 저장 데이터 복원에 실패했다면 초기화에 실패함
        if(hasSaveData)
        {
            if (!saveService.LoadAll()) return false;
        }

        // 최종 런타임 데이터를 PlayerDataManager에 전달하여 초기화함
        playerDataManager.Initialize(playerSaveData);

        // 전체 플레이어 데이터 초기화가 완료되었음을 반환함
        return true;
    }
    /// <summary>
    /// 애플리케이션이 백그라운드로 전환될 때
    /// 현재 플레이어 진행 데이터를 SQLite에 저장함
    /// </summary>
    private void OnApplicationPause(bool pauseStatus)
    {
        // 백그라운드로 전환되는 상황이 아니라면 저장하지 않음
        if (!pauseStatus) return;

        // 전체 게임 초기화가 완료되지 않았다면 저장하지 않음
        if (!isInitialized) return;

        // 현재 플레이어 진행 데이터를 SQLite에 저장함
        if(!saveService.SaveAll())
        {
            // 저장에 실패했다면 오류 로그를 출력함
            Debug.LogError("[GameBootStrap] 백그라운드 전환 시 저장 실패");
        }
    }

    /// <summary>
    /// 애플리케이션이 종료될 때
    /// 현재 플레이어 진행 데이터를 SQLite에 저장함
    /// </summary>
    private void OnApplicationQuit()
    {
        // 전체 게임 초기화가 완료되지 않았다면 저장하지 않음
        if (!isInitialized) return;
        // 현재 플레이어 진행 데이터를 SQLite에 저장함
        if (!saveService.SaveAll())
        {
            // 저장에 실패했다면 오류 로그를 출력함
            Debug.LogError("[GameBootStrap] 어플리케이션 종료 시 저장 실패");
        }
    }

    /// <summary>
    /// 현재 게임 실행 중 사용 중인
    /// 런타임 저장 데이터를 반환한다.
    /// </summary>
    public PlayerSaveData RuntimeSaveData => runtimeSaveData;
    /// <summary>
    /// GameBootstrap의 전체 초기화 완료 여부를 반환한다.
    /// </summary>
    public bool IsInitialized => isInitialized;

    /// <summary>
    /// GameBootstrap이 파괴될 때
    /// 현재 열려 있는 SQLite 연결을 종료함
    /// </summary>
    private void OnDestroy()
    {
        // SaveService가 연결되지 않았다면 종료 처리를 진행하지 않음
        if (saveService == null) return;
        // 현재 열려 있는 SQLite 연결을 종료함
        saveService.CloseDatabase();
    }

}
