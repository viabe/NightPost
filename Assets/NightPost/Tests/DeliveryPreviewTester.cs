using UnityEngine;

// 편지·배달부·노선 조합의 배달 예상 정보 계산 흐름을 검증함
public class DeliveryPreviewTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameBootstrap gameBootstrap;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LetterService letterService;
    [SerializeField] private GameFlowController gameFlowController;

    private int passCount;
    private int failCount;

    /// <summary>
    /// 배달 가능한 편지·배달부·노선 조합을 찾아 예상 정보 계산 결과를 검증함
    /// </summary>
    [ContextMenu("Run Delivery Preview Test")]
    private void RunDeliveryPreviewTest()
    {
        passCount = 0;
        failCount = 0;

        Debug.Log("========== Delivery Preview Test 시작 ==========");

        if (!Application.isPlaying)
        {
            LogResult("플레이 모드 확인", false, "플레이 모드에서 실행해야 함");
            PrintResult();
            return;
        }

        if (!ValidateReferences())
        {
            PrintResult();
            return;
        }

        bool hasCombination =
            TryPrepareDeliveryCombination(
                out LetterStaticData letterStaticData,
                out CourierStaticData courierStaticData,
                out RouteStaticData routeStaticData);

        LogResult(
            "1. 배달 가능한 테스트 조합 준비",
            hasCombination,
            hasCombination
                ? $"Letter: {letterStaticData.LetterID}, Courier: {courierStaticData.CourierID}, Route: {routeStaticData.RouteID}"
                : "배달 가능한 편지·배달부·노선 조합이 없음");

        if (!hasCombination)
        {
            PrintResult();
            return;
        }

        DeliveryPreviewData previewData =
            gameFlowController.GetSelectedLetterDeliveryPreview(
                courierStaticData.CourierID,
                routeStaticData.RouteID);

        LogResult(
            "2. 배달 예상 정보 생성",
            previewData != null,
            previewData != null
                ? "DeliveryPreviewData 생성됨"
                : "DeliveryPreviewData가 null임");

        if (previewData == null)
        {
            PrintResult();
            return;
        }

        LogResult(
            "3. 편지·배달부·노선 ID 일치",
            previewData.LetterID == letterStaticData.LetterID &&
            previewData.CourierID == courierStaticData.CourierID &&
            previewData.RouteID == routeStaticData.RouteID,
            $"Letter: {previewData.LetterID}, Courier: {previewData.CourierID}, Route: {previewData.RouteID}");

        LogResult(
            "4. 노선 기본 배달 시간 일치",
            Mathf.Approximately(
                previewData.RouteBaseDurationSeconds,
                routeStaticData.BaseDeliveryTimeSeconds),
            $"기대: {routeStaticData.BaseDeliveryTimeSeconds}, 실제: {previewData.RouteBaseDurationSeconds}");

        float expectedCourierAdjustedDuration =
            routeStaticData.BaseDeliveryTimeSeconds /
            courierStaticData.Speed;

        LogResult(
            "5. 배달부 속도 적용 시간 일치",
            Mathf.Approximately(
                previewData.CourierAdjustedDurationSeconds,
                expectedCourierAdjustedDuration),
            $"기대: {expectedCourierAdjustedDuration}, 실제: {previewData.CourierAdjustedDurationSeconds}");

        LogResult(
            "6. 시설 감소율 범위 확인",
            previewData.FacilityReductionRate >= 0.0f &&
            previewData.FacilityReductionRate <= 1.0f,
            $"감소율: {previewData.FacilityReductionRate}");

        float expectedDuration =
            expectedCourierAdjustedDuration *
            (1.0f - previewData.FacilityReductionRate);

        if (expectedDuration <= 1.0f)
        {
            expectedDuration = 1.0f;
        }

        LogResult(
            "7. 최종 예상 배달 시간 일치",
            Mathf.Approximately(
                previewData.EstimatedDurationSeconds,
                expectedDuration),
            $"기대: {expectedDuration}, 실제: {previewData.EstimatedDurationSeconds}");

        LogResult(
            "8. 예상 보상 일치",
            previewData.ExpectedReward ==
            letterStaticData.LetterReward,
            $"기대: {letterStaticData.LetterReward}, 실제: {previewData.ExpectedReward}");

        LogResult(
            "9. 편지 목적 지역과 노선 지역 일치",
            previewData.IsRegionMatched,
            $"Letter: {letterStaticData.DestinationRegion}, Route: {routeStaticData.RegionType}");

        LogResult(
            "10. 실제 배달 시작 가능 상태",
            previewData.CanStartDelivery,
            $"CanStartDelivery: {previewData.CanStartDelivery}");

        DeliveryPreviewData invalidPreview =
            gameFlowController.GetSelectedLetterDeliveryPreview(
                0,
                routeStaticData.RouteID);

        LogResult(
            "11. 유효하지 않은 ID의 예상 정보 생성 차단",
            invalidPreview == null,
            invalidPreview == null
                ? "잘못된 요청이 차단됨"
                : "잘못된 요청에서 데이터가 반환됨");

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
            letterService != null &&
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
    /// 실제 배달을 시작할 수 있는 편지·배달부·노선 조합을 준비함
    /// </summary>
    private bool TryPrepareDeliveryCombination(
        out LetterStaticData selectedLetter,
        out CourierStaticData selectedCourier,
        out RouteStaticData selectedRoute)
    {
        selectedLetter = null;
        selectedCourier = FindAvailableCourier();
        selectedRoute = null;

        if (selectedCourier == null)
        {
            return false;
        }

        var letters = staticDataCatalog.Letters();
        var routes = staticDataCatalog.Routes();

        if (letters == null || routes == null)
        {
            return false;
        }

        foreach (LetterStaticData letterStaticData in letters)
        {
            if (letterStaticData == null ||
                letterStaticData.LetterID <= 0)
            {
                continue;
            }

            RouteStaticData matchingRoute =
                FindUnlockedMatchingRoute(
                    routes,
                    letterStaticData.DestinationRegion);

            if (matchingRoute == null)
            {
                continue;
            }

            if (!PrepareLetterWaitingState(letterStaticData))
            {
                continue;
            }

            if (!gameFlowController.SelectLetter(
                letterStaticData.LetterID))
            {
                continue;
            }

            DeliveryPreviewData previewData =
                gameFlowController.GetSelectedLetterDeliveryPreview(
                    selectedCourier.CourierID,
                    matchingRoute.RouteID);

            if (previewData == null ||
                !previewData.CanStartDelivery)
            {
                continue;
            }

            selectedLetter = letterStaticData;
            selectedRoute = matchingRoute;
            return true;
        }

        selectedCourier = null;
        return false;
    }

    /// <summary>
    /// 보유 중이며 현재 다른 배달을 진행하지 않는 배달부를 반환함
    /// </summary>
    private CourierStaticData FindAvailableCourier()
    {
        var couriers = staticDataCatalog.Couriers();

        if (couriers == null)
        {
            return null;
        }

        foreach (CourierStaticData courierStaticData in couriers)
        {
            if (courierStaticData == null ||
                courierStaticData.CourierID <= 0)
            {
                continue;
            }

            if (!playerDataManager.IsCourierOwned(
                courierStaticData.CourierID))
            {
                continue;
            }

            if (playerDataManager.IsCourierDelivering(
                courierStaticData.CourierID))
            {
                continue;
            }

            if (courierStaticData.Speed <= 0.0f)
            {
                continue;
            }

            return courierStaticData;
        }

        return null;
    }

    /// <summary>
    /// 편지 목적 지역과 일치하며 해금된 노선을 반환함
    /// </summary>
    private RouteStaticData FindUnlockedMatchingRoute(
        System.Collections.Generic.IReadOnlyList<RouteStaticData> routes,
        ERegionType destinationRegion)
    {
        foreach (RouteStaticData routeStaticData in routes)
        {
            if (routeStaticData == null ||
                routeStaticData.RouteID <= 0)
            {
                continue;
            }

            if (!playerDataManager.IsRouteUnlocked(
                routeStaticData.RouteID))
            {
                continue;
            }

            if (routeStaticData.RegionType !=
                destinationRegion)
            {
                continue;
            }

            if (routeStaticData.BaseDeliveryTimeSeconds <= 0.0f)
            {
                continue;
            }

            return routeStaticData;
        }

        return null;
    }

    /// <summary>
    /// 테스트 편지를 Waiting 상태로 준비함
    /// </summary>
    private bool PrepareLetterWaitingState(
        LetterStaticData letterStaticData)
    {
        LetterProgressData progressData =
            playerDataManager.GetLetterProgress(
                letterStaticData.LetterID);

        if (progressData == null)
        {
            if (!letterService.ReceiveLetter(
                letterStaticData.LetterID))
            {
                return false;
            }

            progressData =
                playerDataManager.GetLetterProgress(
                    letterStaticData.LetterID);
        }

        if (progressData == null)
        {
            return false;
        }

        if (progressData.State ==
                ELetterProgressState.Delivering ||
            progressData.State ==
                ELetterProgressState.Completed)
        {
            return false;
        }

        if (progressData.State ==
            ELetterProgressState.Waiting)
        {
            return true;
        }

        if (!gameFlowController.SelectLetter(
            letterStaticData.LetterID))
        {
            return false;
        }

        SortingResultData sortingResult =
            gameFlowController.SubmitSelectedLetterSorting(
                letterStaticData.DestinationRegion,
                letterStaticData.Urgency,
                letterStaticData.Weight);

        return sortingResult != null &&
               sortingResult.IsSuccess;
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
                $"[DeliveryPreviewTest][PASS] {testName} | {detail}");
        }
        else
        {
            failCount++;
            Debug.LogError(
                $"[DeliveryPreviewTest][FAIL] {testName} | {detail}");
        }
    }

    /// <summary>
    /// 전체 테스트 결과를 콘솔에 출력함
    /// </summary>
    private void PrintResult()
    {
        Debug.Log(
            "========== Delivery Preview Test 종료 ==========\n" +
            $"PASS: {passCount}, FAIL: {failCount}");
    }
}
