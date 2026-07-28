using UnityEngine;

public class GameBootstrap : MonoBehaviour
{
    [SerializeField] private PlayerSaveData defaultSaveDataTemplate;

    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LetterService letterService;
    [SerializeField] private DeliveryService deliveryService;
    [SerializeField] private ReplyService replyService;

    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private OfflineProgressService offlineProgressService;

    private PlayerSaveData runtimeSaveData;
    private bool isInitialized;
    private void Awake()
    {
        bool isInit = InitializePlayerData();
        if(!isInit)
        {
            Debug.LogError("[GameBootStrap] 플레이어 데이터 초기화 실패");
            return;
        }

        Debug.Log("[GameBootStrap] 플레이어 데이터 초기화 성공");

        isInit = InitializeServices();
        if (!isInit)
        {
            Debug.LogError("[GameBootStrap] 서비스 초기화 실패");
            return;
        }

        Debug.Log("[GameBootStrap] 서비스 초기화 성공");
        isInitialized = true;
    }
    private void Start()
    {
        if (!isInitialized) return;

        offlineProgressService.CheckOffline();
    }
    private bool InitializeServices()
    {
        if (staticDataCatalog == null || letterService == null || deliveryService == null || playerDataManager == null ||
            replyService == null || gameFlowController == null || offlineProgressService == null) return false;

        bool isInit = letterService.Initialize(playerDataManager, staticDataCatalog);
        if (!isInit) return false;
        isInit = deliveryService.Initialize(playerDataManager, staticDataCatalog);
        if (!isInit) return false;
        isInit = replyService.Initialize(playerDataManager, staticDataCatalog);
        if (!isInit) return false;
        isInit = gameFlowController.Initialize(letterService, deliveryService, replyService, playerDataManager);
        if (!isInit) return false;
        isInit = offlineProgressService.Initialize(deliveryService);
        if (!isInit) return false;
        return true;
    }
    private PlayerSaveData CreateRuntimeData()
    {
        // 기본 PlayerSaveData 템플릿이 연결되어 있는지 확인한다.
        if(defaultSaveDataTemplate == null) return null;

        // 기본 템플릿을 복제하여
        // 실제 게임 실행 중 사용할 PlayerSaveData를 생성
        runtimeSaveData = Instantiate(defaultSaveDataTemplate);

        // 원본 에셋과 구분할 수 있도록
        // 생성한 런타임 데이터의 이름을 변경
        runtimeSaveData.name = $"{defaultSaveDataTemplate.name}_Runtime";
        // 생성한 런타임 데이터를 반환
        return runtimeSaveData;
    }

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

    public PlayerSaveData RuntimeSaveData => runtimeSaveData;
    public bool IsInitialized => isInitialized;
}
