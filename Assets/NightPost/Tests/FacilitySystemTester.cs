using System;
using System.Collections.Generic;
using UnityEngine;

public class FacilitySystemTester : MonoBehaviour
{
    [Header("공통 테스트 데이터")]
    [SerializeField] private GameBootstrap gameBootstrap;

    [Header("서비스")]
    [SerializeField] private FacilityService facilityService;
    [SerializeField] private LetterService letterService;
    [SerializeField] private DeliveryService deliveryService;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private StaticDataCatalog staticDataCatalog;

    [Header("시설 업그레이드 테스트")]
    [SerializeField] private int facilityID = 4001;

    [Header("배달 시간 감소 테스트")]
    [SerializeField] private int letterID = 1001;
    [SerializeField] private int courierID = 2001;
    [SerializeField] private int routeID = 3001;

    private bool isTestRunning;

    private int facilityUpgradedEventCount;
    private int currencyChangedEventCount;

    private int lastUpgradedFacilityID;
    private int lastUpgradedFacilityLevel;
    private int lastCurrency;

    private void OnEnable()
    {
        GameEvents.FacilityUpgraded += OnFacilityUpgraded;
        GameEvents.CurrencyChanged += OnCurrencyChanged;
    }

    private void OnDisable()
    {
        GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
        GameEvents.CurrencyChanged -= OnCurrencyChanged;

        isTestRunning = false;
    }

    /// <summary>
    /// 시설 업그레이드와 배달 시간 감소 효과를 함께 검증함
    /// </summary>
    [ContextMenu("Run Facility System Test")]
    private void RunFacilitySystemTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[FacilitySystemTest] 플레이 모드에서 실행해야 합니다.");
            return;
        }

        if (!EnsureReady())
        {
            return;
        }

        PlayerSaveData saveData = gameBootstrap.RuntimeSaveData;

        if (!PrepareCleanTestState(saveData))
        {
            return;
        }

        if (!ValidateStaticData(
            out FacilityStaticData facilityStaticData,
            out FacilityLevelData firstLevelData,
            out LetterStaticData letterStaticData,
            out CourierStaticData courierStaticData,
            out RouteStaticData routeStaticData))
        {
            return;
        }

        if (!EnsureUpgradeCurrency(firstLevelData.UpgradeCost))
        {
            return;
        }

        ResetEventTracking();
        isTestRunning = true;

        int currencyBeforeUpgrade = saveData.Currency;

        float totalEffectBeforeUpgrade =
            facilityService.GetTotalFacilityEffectValue(
                EFacilityEffectType.DeliveryTimeReduction);

        LogResult(
            "1. 테스트 시작 전 시설 진행 데이터 없음",
            playerDataManager.GetFacilityProgress(facilityID) == null);

        LogResult(
            "2. 테스트 시작 전 현재 레벨 데이터 없음",
            facilityService.GetCurrentLevelData(facilityID) == null);

        LogResult(
            "3. 다음 업그레이드 데이터가 1레벨 데이터",
            facilityService.GetNextLevelData(facilityID) ==
            firstLevelData);

        LogResult(
            "4. 업그레이드 전 배달 시간 감소 효과 0",
            Mathf.Approximately(
                totalEffectBeforeUpgrade,
                0f));

        LogResult(
            "5. 시설 업그레이드 가능",
            facilityService.CanUpgradeFacility(facilityID));

        bool upgradeResult =
            facilityService.UpgradeFacility(facilityID);

        LogResult(
            "6. 시설 1레벨 업그레이드 성공",
            upgradeResult);

        if (!upgradeResult)
        {
            isTestRunning = false;
            return;
        }

        FacilityProgressData facilityProgressData =
            playerDataManager.GetFacilityProgress(facilityID);

        LogResult(
            "7. 시설 진행 데이터 생성",
            facilityProgressData != null);

        if (facilityProgressData == null)
        {
            isTestRunning = false;
            return;
        }

        LogResult(
            "8. 시설 현재 레벨이 1",
            facilityProgressData.CurrentLevel == 1);

        LogResult(
            "9. 업그레이드 비용만큼 재화 차감",
            saveData.Currency ==
            currencyBeforeUpgrade - firstLevelData.UpgradeCost);

        LogResult(
            "10. FacilityUpgraded 이벤트 1회 발생",
            facilityUpgradedEventCount == 1);

        LogResult(
            "11. FacilityUpgraded 이벤트 값 일치",
            lastUpgradedFacilityID == facilityID &&
            lastUpgradedFacilityLevel == 1);

        int expectedCurrencyEventCount =
            firstLevelData.UpgradeCost > 0 ? 1 : 0;

        LogResult(
            "12. CurrencyChanged 이벤트 횟수 정상",
            currencyChangedEventCount ==
            expectedCurrencyEventCount);

        if (firstLevelData.UpgradeCost > 0)
        {
            LogResult(
                "13. CurrencyChanged 최종 재화 값 일치",
                lastCurrency == saveData.Currency);
        }
        else
        {
            LogResult(
                "13. 무료 업그레이드 시 CurrencyChanged 미발생",
                currencyChangedEventCount == 0);
        }

        FacilityLevelData currentLevelData =
            facilityService.GetCurrentLevelData(facilityID);

        LogResult(
            "14. 현재 레벨 데이터가 1레벨 데이터",
            currentLevelData == firstLevelData);

        float currentFacilityEffect =
            facilityService.GetFacilityEffectValue(
                facilityID,
                EFacilityEffectType.DeliveryTimeReduction);

        LogResult(
            "15. 현재 시설 효과값이 1레벨 값과 일치",
            Mathf.Approximately(
                currentFacilityEffect,
                firstLevelData.EffectValue));

        float totalEffectAfterUpgrade =
            facilityService.GetTotalFacilityEffectValue(
                EFacilityEffectType.DeliveryTimeReduction);

        LogResult(
            "16. 전체 배달 시간 감소 효과값 일치",
            Mathf.Approximately(
                totalEffectAfterUpgrade,
                firstLevelData.EffectValue));

        bool letterPrepared = PrepareLetter(letterID);

        LogResult(
            "17. 배달 시간 테스트용 편지 Waiting 준비",
            letterPrepared);

        if (!letterPrepared)
        {
            isTestRunning = false;
            return;
        }

        bool deliveryStarted =
            deliveryService.StartDelivery(
                courierID,
                letterID,
                routeID);

        LogResult(
            "18. 시설 효과가 적용된 배달 시작 성공",
            deliveryStarted);

        if (!deliveryStarted)
        {
            isTestRunning = false;
            return;
        }

        ActiveDeliveryData activeDeliveryData =
            FindActiveDelivery(letterID);

        LogResult(
            "19. ActiveDeliveryData 생성",
            activeDeliveryData != null);

        if (activeDeliveryData == null)
        {
            isTestRunning = false;
            return;
        }

        float baseDeliveryDuration =
            routeStaticData.BaseDeliveryTimeSeconds /
            courierStaticData.Speed;

        float clampedReduction =
            Mathf.Clamp01(totalEffectAfterUpgrade);

        float reducedDeliveryDuration =
            baseDeliveryDuration *
            (1f - clampedReduction);

        if (reducedDeliveryDuration < 1f)
        {
            reducedDeliveryDuration = 1f;
        }

        long expectedDurationSeconds =
            (long)Math.Ceiling(reducedDeliveryDuration);

        long actualDurationSeconds =
            activeDeliveryData.CompleteAtUnixTime -
            activeDeliveryData.StartedAtUnixTime;

        LogResult(
            $"20. 배달 시간 감소 결과 일치 " +
            $"Expected={expectedDurationSeconds}, " +
            $"Actual={actualDurationSeconds}",
            actualDurationSeconds ==
            expectedDurationSeconds);

        LogResult(
            "21. 편지가 Delivering 상태로 변경",
            IsLetterState(
                letterID,
                ELetterProgressState.Delivering));

        PrintFinalState(
            saveData,
            facilityStaticData,
            firstLevelData,
            baseDeliveryDuration,
            reducedDeliveryDuration,
            expectedDurationSeconds,
            actualDurationSeconds);

        isTestRunning = false;

        Debug.Log(
            "========== Facility System Test 종료 ==========");
    }

    /// <summary>
    /// 테스트 실행에 필요한 참조와 런타임 데이터를 확인함
    /// </summary>
    private bool EnsureReady()
    {
        if (gameBootstrap == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] GameBootstrap이 연결되지 않았습니다.");
            return false;
        }

        if (gameBootstrap.RuntimeSaveData == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] RuntimeSaveData가 초기화되지 않았습니다.");
            return false;
        }

        if (facilityService == null ||
            letterService == null ||
            deliveryService == null ||
            playerDataManager == null ||
            staticDataCatalog == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] 필요한 서비스가 연결되지 않았습니다.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 시설 테스트를 위한 런타임 저장 상태를 정리함
    /// </summary>
    private bool PrepareCleanTestState(PlayerSaveData saveData)
    {
        if (saveData == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] RuntimeSaveData가 없습니다.");
            return false;
        }

        if (saveData.FacilityProgressesList == null ||
            saveData.LetterProgressesList == null ||
            saveData.ActiveDeliveryList == null ||
            saveData.DeliveryResultsList == null ||
            saveData.OwnedCourierIDs == null ||
            saveData.UnlockedRouteIDs == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] PlayerSaveData 목록 중 " +
                "초기화되지 않은 목록이 있습니다.");
            return false;
        }

        saveData.FacilityProgressesList.Clear();
        saveData.LetterProgressesList.Clear();
        saveData.ActiveDeliveryList.Clear();
        saveData.DeliveryResultsList.Clear();

        AddUniqueID(
            saveData.OwnedCourierIDs,
            courierID);

        AddUniqueID(
            saveData.UnlockedRouteIDs,
            routeID);

        return true;
    }

    /// <summary>
    /// 시설과 배달 시간 테스트에 사용할 정적 데이터를 확인함
    /// </summary>
    private bool ValidateStaticData(
        out FacilityStaticData facilityStaticData,
        out FacilityLevelData firstLevelData,
        out LetterStaticData letterStaticData,
        out CourierStaticData courierStaticData,
        out RouteStaticData routeStaticData)
    {
        facilityStaticData = null;
        firstLevelData = null;
        letterStaticData = null;
        courierStaticData = null;
        routeStaticData = null;

        if (facilityID <= 0 ||
            letterID <= 0 ||
            courierID <= 0 ||
            routeID <= 0)
        {
            Debug.LogError(
                "[FacilitySystemTest] 모든 테스트 ID는 1 이상이어야 합니다.");
            return false;
        }

        facilityStaticData =
            staticDataCatalog.GetFacility(facilityID);

        letterStaticData =
            staticDataCatalog.GetLetter(letterID);

        courierStaticData =
            staticDataCatalog.GetCourier(courierID);

        routeStaticData =
            staticDataCatalog.GetRoute(routeID);

        if (facilityStaticData == null ||
            letterStaticData == null ||
            courierStaticData == null ||
            routeStaticData == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] 필요한 정적 데이터가 없습니다.");
            return false;
        }

        if (facilityStaticData.LevelData == null ||
            facilityStaticData.LevelData.Length == 0)
        {
            Debug.LogError(
                "[FacilitySystemTest] 시설 레벨 데이터가 없습니다.");
            return false;
        }

        foreach (FacilityLevelData levelData
                 in facilityStaticData.LevelData)
        {
            if (levelData == null)
            {
                continue;
            }

            if (levelData.Level == 1)
            {
                firstLevelData = levelData;
                break;
            }
        }

        if (firstLevelData == null)
        {
            Debug.LogError(
                "[FacilitySystemTest] Level이 1인 시설 데이터가 없습니다.");
            return false;
        }

        if (firstLevelData.UpgradeCost < 0)
        {
            Debug.LogError(
                "[FacilitySystemTest] 1레벨 업그레이드 비용이 음수입니다.");
            return false;
        }

        if (firstLevelData.EffectType !=
            EFacilityEffectType.DeliveryTimeReduction)
        {
            Debug.LogError(
                "[FacilitySystemTest] 테스트 시설의 1레벨 EffectType을 " +
                "DeliveryTimeReduction으로 설정해야 합니다.");
            return false;
        }

        if (firstLevelData.EffectValue <= 0f)
        {
            Debug.LogError(
                "[FacilitySystemTest] 테스트 시설의 1레벨 EffectValue는 " +
                "0보다 커야 합니다.");
            return false;
        }

        if (letterStaticData.DestinationRegion !=
            routeStaticData.RegionType)
        {
            Debug.LogError(
                "[FacilitySystemTest] 편지 목적지와 노선 지역이 다릅니다.");
            return false;
        }

        if (courierStaticData.Speed <= 0f ||
            routeStaticData.BaseDeliveryTimeSeconds <= 0f)
        {
            Debug.LogError(
                "[FacilitySystemTest] 배달부 속도와 노선 시간은 " +
                "0보다 커야 합니다.");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 시설 업그레이드 비용을 지불할 수 있도록 재화를 확보함
    /// </summary>
    private bool EnsureUpgradeCurrency(int upgradeCost)
    {
        if (upgradeCost < 0)
        {
            return false;
        }

        int currentCurrency =
            playerDataManager.GetCurrency();

        if (currentCurrency >= upgradeCost)
        {
            return true;
        }

        int requiredAmount =
            upgradeCost - currentCurrency;

        return playerDataManager.AddCurrency(requiredAmount);
    }

    /// <summary>
    /// 배달 시간 테스트에 사용할 편지를 Waiting 상태로 준비함
    /// </summary>
    private bool PrepareLetter(int targetLetterID)
    {
        if (!letterService.ReceiveLetter(targetLetterID))
        {
            return false;
        }

        if (letterService.OpenLetter(targetLetterID) == null)
        {
            return false;
        }

        if (!letterService.CompleteSorting(targetLetterID))
        {
            return false;
        }

        return IsLetterState(
            targetLetterID,
            ELetterProgressState.Waiting);
    }

    /// <summary>
    /// 지정한 편지의 현재 진행 상태가 예상 상태인지 확인함
    /// </summary>
    private bool IsLetterState(
        int targetLetterID,
        ELetterProgressState expectedState)
    {
        LetterProgressData progressData =
            playerDataManager.GetLetterProgress(targetLetterID);

        return progressData != null &&
               progressData.State == expectedState;
    }

    /// <summary>
    /// 지정한 편지에 해당하는 진행 중 배달 데이터를 조회함
    /// </summary>
    private ActiveDeliveryData FindActiveDelivery(
        int targetLetterID)
    {
        IReadOnlyList<ActiveDeliveryData> activeDeliveries =
            playerDataManager.GetActiveDeliveries();

        if (activeDeliveries == null)
        {
            return null;
        }

        foreach (ActiveDeliveryData activeDeliveryData
                 in activeDeliveries)
        {
            if (activeDeliveryData == null)
            {
                continue;
            }

            if (activeDeliveryData.LetterID ==
                targetLetterID)
            {
                return activeDeliveryData;
            }
        }

        return null;
    }

    /// <summary>
    /// 테스트 이벤트 기록 값을 초기화함
    /// </summary>
    private void ResetEventTracking()
    {
        facilityUpgradedEventCount = 0;
        currencyChangedEventCount = 0;

        lastUpgradedFacilityID = 0;
        lastUpgradedFacilityLevel = 0;
        lastCurrency = -1;
    }

    /// <summary>
    /// 시설 업그레이드 이벤트 발생 내용을 기록함
    /// </summary>
    private void OnFacilityUpgraded(
        int upgradedFacilityID,
        int currentLevel)
    {
        if (!isTestRunning)
        {
            return;
        }

        facilityUpgradedEventCount++;
        lastUpgradedFacilityID = upgradedFacilityID;
        lastUpgradedFacilityLevel = currentLevel;
    }

    /// <summary>
    /// 재화 변경 이벤트 발생 내용을 기록함
    /// </summary>
    private void OnCurrencyChanged(int currentCurrency)
    {
        if (!isTestRunning)
        {
            return;
        }

        currencyChangedEventCount++;
        lastCurrency = currentCurrency;
    }

    /// <summary>
    /// 목록에 ID가 없을 때만 추가함
    /// </summary>
    private void AddUniqueID(
        List<int> idList,
        int id)
    {
        if (!idList.Contains(id))
        {
            idList.Add(id);
        }
    }

    /// <summary>
    /// 테스트 항목의 성공 여부를 콘솔에 출력함
    /// </summary>
    private void LogResult(
        string testName,
        bool success)
    {
        if (success)
        {
            Debug.Log(
                $"[FacilitySystemTest][PASS] {testName}");
        }
        else
        {
            Debug.LogError(
                $"[FacilitySystemTest][FAIL] {testName}");
        }
    }

    /// <summary>
    /// 시설 업그레이드와 배달 시간 계산의 최종 상태를 출력함
    /// </summary>
    private void PrintFinalState(
        PlayerSaveData saveData,
        FacilityStaticData facilityStaticData,
        FacilityLevelData firstLevelData,
        float baseDeliveryDuration,
        float reducedDeliveryDuration,
        long expectedDurationSeconds,
        long actualDurationSeconds)
    {
        FacilityProgressData progressData =
            playerDataManager.GetFacilityProgress(facilityID);

        Debug.Log(
            "[FacilitySystemTest] 최종 상태\n" +
            $"Facility ID: {facilityStaticData.FacilityID}\n" +
            $"Facility Level: {progressData?.CurrentLevel ?? 0}\n" +
            $"Upgrade Cost: {firstLevelData.UpgradeCost}\n" +
            $"Effect Type: {firstLevelData.EffectType}\n" +
            $"Effect Value: {firstLevelData.EffectValue}\n" +
            $"Currency: {saveData.Currency}\n" +
            $"FacilityUpgraded Event Count: " +
            $"{facilityUpgradedEventCount}\n" +
            $"CurrencyChanged Event Count: " +
            $"{currencyChangedEventCount}\n" +
            $"Base Delivery Duration: " +
            $"{baseDeliveryDuration}\n" +
            $"Reduced Delivery Duration: " +
            $"{reducedDeliveryDuration}\n" +
            $"Expected Duration Seconds: " +
            $"{expectedDurationSeconds}\n" +
            $"Actual Duration Seconds: " +
            $"{actualDurationSeconds}");
    }
}
