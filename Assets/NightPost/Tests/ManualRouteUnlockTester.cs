using UnityEngine;

// 조건부 노선이 자동 해금되지 않고 UI 요청으로만 해금되는 흐름을 검증함
public class ManualRouteUnlockTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameBootstrap gameBootstrap;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private ProgressionService progressionService;
    [SerializeField] private GameFlowController gameFlowController;

    private int passCount;
    private int failCount;

    private bool isTestRunning;
    private int routeUnlockedEventCount;
    private int lastUnlockedRouteID;

    private void OnEnable()
    {
        GameEvents.RouteUnlocked += OnRouteUnlocked;
    }

    private void OnDisable()
    {
        GameEvents.RouteUnlocked -= OnRouteUnlocked;
        isTestRunning = false;
    }

    /// <summary>
    /// 조건부 노선의 수동 해금 흐름을 검증함
    /// </summary>
    [ContextMenu("Run Manual Route Unlock Test")]
    private void RunManualRouteUnlockTest()
    {
        passCount = 0;
        failCount = 0;
        routeUnlockedEventCount = 0;
        lastUnlockedRouteID = 0;

        Debug.Log("========== Manual Route Unlock Test 시작 ==========");

        // 플레이 모드가 아니라면 런타임 테스트를 진행하지 않음
        if (!Application.isPlaying)
        {
            LogResult(
                "플레이 모드 확인",
                false,
                "플레이 모드에서 실행해야 함");

            PrintResult();
            return;
        }

        // 테스트에 필요한 참조와 초기화 상태를 확인함
        if (!ValidateReferences())
        {
            PrintResult();
            return;
        }

        // 기본 해금이 아니며 아직 잠긴 조건부 노선을 하나 조회함
        RouteStaticData routeStaticData =
            FindLockedConditionalRoute();

        LogResult(
            "1. 테스트할 조건부 잠금 노선 조회",
            routeStaticData != null,
            routeStaticData != null
                ? $"RouteID: {routeStaticData.RouteID}"
                : "사용 가능한 조건부 잠금 노선이 없음");

        if (routeStaticData == null)
        {
            Debug.LogError(
                "[ManualRouteUnlockTest] 플레이 모드를 다시 시작하거나 " +
                "기본 해금이 아닌 잠긴 노선 데이터를 확인해야 함.");

            PrintResult();
            return;
        }

        int routeID =
            routeStaticData.RouteID;

        int requiredDeliveryCount =
            routeStaticData.UnlockCondition.RequiredCompletedDeliveryCount;

        // 조건 달성 전 상태를 확인함
        LogResult(
            "2. 테스트 시작 시 노선이 잠금 상태",
            !playerDataManager.IsRouteUnlocked(routeID),
            $"RouteID: {routeID}");

        // 요구 배달 완료 횟수까지 진행도를 증가시킴
        IncreaseDeliveryCountTo(requiredDeliveryCount);

        int completedDeliveryCount =
            playerDataManager.GetCompletedDeliveryCount();

        LogResult(
            "3. 노선 해금 조건 달성",
            completedDeliveryCount >= requiredDeliveryCount,
            $"현재: {completedDeliveryCount}, 요구: {requiredDeliveryCount}");

        // 진행도 자동 해금 검사를 실행해도 노선은 자동 해금되지 않아야 함
        progressionService.EvaluateProgressUnlocks();

        LogResult(
            "4. 조건 달성 후에도 노선 자동 해금되지 않음",
            !playerDataManager.IsRouteUnlocked(routeID),
            $"RouteID: {routeID}");

        // 조건 달성 후 수동 해금 가능 상태인지 확인함
        bool canUnlock =
            progressionService.CanUnlockRoute(routeID);

        LogResult(
            "5. 조건 달성 노선의 수동 해금 가능",
            canUnlock,
            $"CanUnlockRoute: {canUnlock}");

        if (!canUnlock)
        {
            PrintResult();
            return;
        }

        // GameFlowController를 통한 UI 해금 요청 흐름을 검증함
        isTestRunning = true;

        bool unlockResult =
            gameFlowController.UnlockRoute(routeID);

        isTestRunning = false;

        LogResult(
            "6. GameFlowController를 통한 노선 해금 성공",
            unlockResult,
            $"UnlockRoute 결과: {unlockResult}");

        // 플레이어 데이터에 실제 해금 상태가 저장됐는지 확인함
        LogResult(
            "7. 플레이어 데이터에 노선 해금 반영",
            playerDataManager.IsRouteUnlocked(routeID),
            $"RouteID: {routeID}");

        // 노선 해금 이벤트가 정확히 한 번 발생했는지 확인함
        LogResult(
            "8. RouteUnlocked 이벤트 1회 발생",
            routeUnlockedEventCount == 1,
            $"발생 횟수: {routeUnlockedEventCount}");

        // 이벤트로 전달된 노선 ID가 일치하는지 확인함
        LogResult(
            "9. RouteUnlocked 이벤트 노선 ID 일치",
            lastUnlockedRouteID == routeID,
            $"기대: {routeID}, 실제: {lastUnlockedRouteID}");

        // 이미 해금된 노선은 다시 해금할 수 없어야 함
        bool duplicateUnlockResult =
            gameFlowController.UnlockRoute(routeID);

        LogResult(
            "10. 이미 해금된 노선의 중복 해금 차단",
            !duplicateUnlockResult,
            $"두 번째 UnlockRoute 결과: {duplicateUnlockResult}");

        // 이미 해금된 노선은 수동 해금 가능 상태도 아니어야 함
        LogResult(
            "11. 해금 완료 노선의 CanUnlockRoute 차단",
            !progressionService.CanUnlockRoute(routeID),
            $"RouteID: {routeID}");

        PrintResult();
    }

    /// <summary>
    /// 테스트에 필요한 참조와 GameBootstrap 초기화 상태를 확인함
    /// </summary>
    private bool ValidateReferences()
    {
        bool hasReferences =
            gameBootstrap != null &&
            staticDataCatalog != null &&
            playerDataManager != null &&
            progressionService != null &&
            gameFlowController != null;

        LogResult(
            "필수 참조 연결",
            hasReferences,
            hasReferences
                ? "모든 참조가 연결됨"
                : "Inspector의 필수 참조를 확인해야 함");

        if (!hasReferences)
        {
            return false;
        }

        LogResult(
            "GameBootstrap 초기화 완료",
            gameBootstrap.IsInitialized,
            $"IsInitialized: {gameBootstrap.IsInitialized}");

        return gameBootstrap.IsInitialized;
    }

    /// <summary>
    /// 기본 해금이 아니며 아직 잠긴 조건부 노선을 하나 반환함
    /// </summary>
    private RouteStaticData FindLockedConditionalRoute()
    {
        var routes =
            staticDataCatalog.Routes();

        if (routes == null)
        {
            return null;
        }

        foreach (RouteStaticData routeStaticData in routes)
        {
            if (routeStaticData == null)
            {
                continue;
            }

            if (routeStaticData.UnlockCondition == null)
            {
                continue;
            }

            if (routeStaticData.UnlockCondition.IsUnlockedByDefault)
            {
                continue;
            }

            if (playerDataManager.IsRouteUnlocked(
                routeStaticData.RouteID))
            {
                continue;
            }

            return routeStaticData;
        }

        return null;
    }

    /// <summary>
    /// 현재 배달 완료 횟수를 지정한 요구 횟수까지 증가시킴
    /// </summary>
    private void IncreaseDeliveryCountTo(
        int requiredDeliveryCount)
    {
        int currentDeliveryCount =
            playerDataManager.GetCompletedDeliveryCount();

        while (currentDeliveryCount <
               requiredDeliveryCount)
        {
            playerDataManager
                .IncreaseCompletedDeliveryCount();

            currentDeliveryCount =
                playerDataManager.GetCompletedDeliveryCount();
        }
    }

    /// <summary>
    /// 테스트 중 발생한 노선 해금 이벤트 내용을 기록함
    /// </summary>
    private void OnRouteUnlocked(int routeID)
    {
        if (!isTestRunning)
        {
            return;
        }

        routeUnlockedEventCount++;
        lastUnlockedRouteID = routeID;
    }

    /// <summary>
    /// 개별 테스트 결과를 출력하고 성공·실패 횟수를 기록함
    /// </summary>
    private void LogResult(
        string testName,
        bool success,
        string detail)
    {
        if (success)
        {
            passCount++;

            Debug.Log(
                $"[ManualRouteUnlockTest][PASS] {testName} | {detail}");
        }
        else
        {
            failCount++;

            Debug.LogError(
                $"[ManualRouteUnlockTest][FAIL] {testName} | {detail}");
        }
    }

    /// <summary>
    /// 전체 테스트 결과를 콘솔에 출력함
    /// </summary>
    private void PrintResult()
    {
        Debug.Log(
            "========== Manual Route Unlock Test 종료 ==========\n" +
            $"PASS: {passCount}, FAIL: {failCount}");
    }
}
