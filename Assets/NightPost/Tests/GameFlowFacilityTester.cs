using UnityEngine;

public class GameFlowFacilityTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameBootstrap gameBootstrap;
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private FacilityService facilityService;
    [SerializeField] private PlayerDataManager playerDataManager;

    [Header("테스트 시설")]
    [SerializeField] private int facilityID = 4002;

    private bool isTestRunning;
    private int facilityUpgradedEventCount;
    private int lastFacilityID;
    private int lastFacilityLevel;

    private void OnEnable()
    {
        GameEvents.FacilityUpgraded += OnFacilityUpgraded;
    }

    private void OnDisable()
    {
        GameEvents.FacilityUpgraded -= OnFacilityUpgraded;
        isTestRunning = false;
    }

    /// <summary>
    /// GameFlowController를 통한 시설 선택 및 업그레이드 흐름을 검증함
    /// </summary>
    [ContextMenu("Run Game Flow Facility Test")]
    private void RunGameFlowFacilityTest()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError(
                "[GameFlowFacilityTest] 플레이 모드에서 실행해야 합니다.");
            return;
        }

        if (!EnsureReady())
        {
            return;
        }

        PlayerSaveData saveData = gameBootstrap.RuntimeSaveData;

        if (saveData.FacilityProgressesList == null)
        {
            Debug.LogError(
                "[GameFlowFacilityTest] FacilityProgressesList가 없습니다.");
            return;
        }

        // 다른 테스트 결과의 영향을 받지 않도록 시설 진행 상태를 초기화함
        saveData.FacilityProgressesList.Clear();

        FacilityLevelData nextLevelData =
            facilityService.GetNextLevelData(facilityID);

        if (nextLevelData == null)
        {
            Debug.LogError(
                "[GameFlowFacilityTest] 1레벨 시설 데이터를 찾을 수 없습니다.");
            return;
        }

        if (!EnsureUpgradeCurrency(nextLevelData.UpgradeCost))
        {
            Debug.LogError(
                "[GameFlowFacilityTest] 업그레이드 비용을 준비하지 못했습니다.");
            return;
        }

        facilityUpgradedEventCount = 0;
        lastFacilityID = 0;
        lastFacilityLevel = 0;
        isTestRunning = true;

        int currencyBefore = playerDataManager.GetCurrency();

        bool invalidSelectResult =
            gameFlowController.SelectFacility(-1);

        LogResult(
            "1. 유효하지 않은 시설 선택 차단",
            !invalidSelectResult);

        bool selectResult =
            gameFlowController.SelectFacility(facilityID);

        LogResult(
            "2. 시설 선택 성공",
            selectResult);

        if (!selectResult)
        {
            isTestRunning = false;
            return;
        }

        bool upgradeResult =
            gameFlowController.UpgradeSelectedFacility();

        LogResult(
            "3. 선택한 시설 업그레이드 성공",
            upgradeResult);

        FacilityProgressData progressData =
            playerDataManager.GetFacilityProgress(facilityID);

        LogResult(
            "4. 시설 진행 데이터 생성",
            progressData != null);

        if (progressData == null)
        {
            isTestRunning = false;
            return;
        }

        LogResult(
            "5. 시설 현재 레벨이 1",
            progressData.CurrentLevel == 1);

        LogResult(
            "6. 업그레이드 비용만큼 재화 차감",
            playerDataManager.GetCurrency() ==
            currencyBefore - nextLevelData.UpgradeCost);

        LogResult(
            "7. FacilityUpgraded 이벤트 1회 발생",
            facilityUpgradedEventCount == 1);

        LogResult(
            "8. 이벤트로 전달된 시설 ID와 레벨 일치",
            lastFacilityID == facilityID &&
            lastFacilityLevel == 1);

        isTestRunning = false;

        Debug.Log(
            "========== Game Flow Facility Test 종료 ==========");
    }

    /// <summary>
    /// 테스트에 필요한 참조와 런타임 데이터를 확인함
    /// </summary>
    private bool EnsureReady()
    {
        if (gameBootstrap == null ||
            gameFlowController == null ||
            facilityService == null ||
            playerDataManager == null)
        {
            Debug.LogError(
                "[GameFlowFacilityTest] 필요한 참조가 연결되지 않았습니다.");
            return false;
        }

        if (gameBootstrap.RuntimeSaveData == null)
        {
            Debug.LogError(
                "[GameFlowFacilityTest] RuntimeSaveData가 초기화되지 않았습니다.");
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

        return playerDataManager.AddCurrency(
            upgradeCost - currentCurrency);
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
        lastFacilityID = upgradedFacilityID;
        lastFacilityLevel = currentLevel;
    }

    /// <summary>
    /// 테스트 항목의 성공 여부를 콘솔에 출력함
    /// </summary>
    private void LogResult(string testName, bool success)
    {
        if (success)
        {
            Debug.Log($"[GameFlowFacilityTest][PASS] {testName}");
        }
        else
        {
            Debug.LogError($"[GameFlowFacilityTest][FAIL] {testName}");
        }
    }
}
