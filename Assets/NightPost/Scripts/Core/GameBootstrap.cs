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


    [Header("게임 흐름")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private OfflineProgressService offlineProgressService;

    // 실제 게임 실행 중 사용하는 저장 데이터
    private PlayerSaveData runtimeSaveData;
    private SortingService sortingService;
    private MorningReportService morningReportService;
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
        progressionService.ApplyDefaultUnlocks();

        // 불러온 저장 데이터의 누적 배달 완료 수를 기준으로 추가로 해금될 배달부와 노선을 검사
        progressionService.EvaluateProgressUnlocks();
        // 모든 초기화 과정이 정상적으로 끝났음을 기록
        isInitialized = true;

        Debug.Log("[GameBootStrap] 서비스 초기화 성공");
    }

    /// <summary>
    /// Awake에서 모든 초기화가 끝난 뒤
    /// 오프라인 동안 완료된 배달을 확인한다.
    /// </summary>
    private void Start()
    {
        // 초기화가 실패한 상태라면 오프라인 실행 X
        if (!isInitialized) return;
        // 마지막 저장 시점 이후 완료된 배달을 처리하고 주기적인 완료 검사 시작
        offlineProgressService.CheckOffline();
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
        // PlayerDataManager가 인스펙터에 연결되어 있는지 확인
        if(playerDataManager == null) return false;

        // CreateRuntimeData()를 호출하여
        // 실제 게임에서 사용할 PlayerSaveData를 생성
        PlayerSaveData createdData = CreateRuntimeData();

        if (createdData == null) return false;


        // 생성한 런타임 데이터를 전달하여
        // PlayerDataManager를 초기화
        playerDataManager.Initialize(createdData);

        // 모든 초기화가 정상적으로 끝났다면 true를 반환
        return true;
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
}
