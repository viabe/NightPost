using System;
using UnityEngine;

// GameFlowController를 통한 편지 분류 제출 흐름을 검증함
public class SortingSystemTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameBootstrap gameBootstrap;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private LetterService letterService;
    [SerializeField] private GameFlowController gameFlowController;

    [Header("테스트 편지")]
    [SerializeField] private int testLetterID = 1;

    private int passCount;
    private int failCount;

    /// <summary>
    /// 오답 제출과 정답 제출을 포함한 전체 분류 흐름을 검증함
    /// </summary>
    [ContextMenu("Run Sorting System Test")]
    private void RunSortingSystemTest()
    {
        passCount = 0;
        failCount = 0;

        Debug.Log("========== Sorting System Test 시작 ==========");

        // 플레이 모드가 아니라면 런타임 테스트를 진행하지 않음
        if (!Application.isPlaying)
        {
            LogResult("플레이 모드 확인", false, "플레이 모드에서 실행해야 함");
            PrintResult();
            return;
        }

        // 테스트에 필요한 참조와 초기화 상태를 확인함
        if (!ValidateReferences())
        {
            PrintResult();
            return;
        }

        // 테스트에 사용할 편지 정적 데이터를 조회함
        LetterStaticData letterStaticData =
            staticDataCatalog.GetLetter(testLetterID);

        LogResult(
            "1. 테스트 편지 정적 데이터 조회",
            letterStaticData != null,
            $"LetterID: {testLetterID}");

        if (letterStaticData == null)
        {
            PrintResult();
            return;
        }

        // 테스트 편지의 진행 데이터를 준비함
        LetterProgressData progressData =
            PrepareLetterProgress();

        LogResult(
            "2. 테스트 편지 진행 데이터 준비",
            progressData != null,
            $"LetterID: {testLetterID}");

        if (progressData == null)
        {
            PrintResult();
            return;
        }

        // 분류 전 편지가 New 상태인지 확인함
        LogResult(
            "3. 분류 전 편지 상태가 New",
            progressData.State == ELetterProgressState.New,
            $"현재 상태: {progressData.State}");

        if (progressData.State != ELetterProgressState.New)
        {
            Debug.LogError(
                "[SortingSystemTest] 테스트 편지가 이미 처리된 상태임. " +
                "플레이 모드를 다시 시작하거나 다른 테스트 편지 ID를 사용해야 함.");

            PrintResult();
            return;
        }

        // 편지를 열어 현재 선택 편지로 저장함
        bool selectResult =
            gameFlowController.SelectLetter(testLetterID);

        LogResult(
            "4. 테스트 편지 선택",
            selectResult,
            $"LetterID: {testLetterID}");

        if (!selectResult)
        {
            PrintResult();
            return;
        }

        // 정답과 다른 지역값을 준비함
        bool hasWrongRegion =
            TryGetDifferentRegion(
                letterStaticData.DestinationRegion,
                out ERegionType wrongRegion);

        LogResult(
            "5. 오답 지역값 준비",
            hasWrongRegion,
            hasWrongRegion
                ? $"정답: {letterStaticData.DestinationRegion}, 오답: {wrongRegion}"
                : "ERegionType에 다른 값이 없음");

        if (!hasWrongRegion)
        {
            PrintResult();
            return;
        }

        // 지역만 틀리고 긴급도와 무게는 맞는 값을 제출함
        SortingResultData wrongResult =
            gameFlowController.SubmitSelectedLetterSorting(
                wrongRegion,
                letterStaticData.Urgency,
                letterStaticData.Weight);

        LogResult(
            "6. 오답 제출 결과 생성",
            wrongResult != null,
            "지역만 틀린 값을 제출함");

        if (wrongResult == null)
        {
            PrintResult();
            return;
        }

        // 개별 항목 판정 결과를 확인함
        LogResult(
            "7. 지역 항목 오답 판정",
            !wrongResult.IsRegionCorrect,
            $"IsRegionCorrect: {wrongResult.IsRegionCorrect}");

        LogResult(
            "8. 긴급도와 무게 정답 판정",
            wrongResult.IsUrgencyCorrect &&
            wrongResult.IsWeightCorrect,
            $"Urgency: {wrongResult.IsUrgencyCorrect}, " +
            $"Weight: {wrongResult.IsWeightCorrect}");

        LogResult(
            "9. 오답 제출의 전체 결과가 실패",
            !wrongResult.IsSuccess,
            $"IsSuccess: {wrongResult.IsSuccess}");

        // 오답 제출 후 편지 상태가 유지되는지 확인함
        LogResult(
            "10. 오답 제출 후 New 상태 유지",
            progressData.State == ELetterProgressState.New,
            $"현재 상태: {progressData.State}");

        // 세 항목을 모두 정답으로 제출함
        SortingResultData correctResult =
            gameFlowController.SubmitSelectedLetterSorting(
                letterStaticData.DestinationRegion,
                letterStaticData.Urgency,
                letterStaticData.Weight);

        LogResult(
            "11. 정답 제출 결과 생성",
            correctResult != null,
            "세 항목을 모두 정답으로 제출함");

        if (correctResult == null)
        {
            PrintResult();
            return;
        }

        LogResult(
            "12. 정답 제출의 전체 결과가 성공",
            correctResult.IsSuccess,
            $"IsSuccess: {correctResult.IsSuccess}");

        // 정답 제출 후 Waiting 상태로 전환되는지 확인함
        LogResult(
            "13. 정답 제출 후 Waiting 상태 전환",
            progressData.State == ELetterProgressState.Waiting,
            $"현재 상태: {progressData.State}");

        // 이미 분류 완료된 편지를 다시 제출함
        SortingResultData duplicateResult =
            gameFlowController.SubmitSelectedLetterSorting(
                letterStaticData.DestinationRegion,
                letterStaticData.Urgency,
                letterStaticData.Weight);

        LogResult(
            "14. 분류 완료 편지의 중복 제출 차단",
            duplicateResult == null,
            duplicateResult == null
                ? "중복 제출이 차단됨"
                : "중복 제출 결과가 반환됨");

        PrintResult();
    }

    /// <summary>
    /// 테스트에 필요한 참조와 초기화 상태를 확인함
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
    /// 테스트 편지의 진행 데이터를 조회하거나 신규 수신 상태로 생성함
    /// </summary>
    private LetterProgressData PrepareLetterProgress()
    {
        // 기존 진행 데이터가 있다면 그대로 반환함
        LetterProgressData progressData =
            playerDataManager.GetLetterProgress(testLetterID);

        if (progressData != null)
        {
            return progressData;
        }

        // 진행 데이터가 없다면 신규 편지로 수신함
        bool receiveResult =
            letterService.ReceiveLetter(testLetterID);

        if (!receiveResult)
        {
            return null;
        }

        // 신규 수신 후 생성된 진행 데이터를 반환함
        return playerDataManager.GetLetterProgress(testLetterID);
    }

    /// <summary>
    /// 정답 지역과 다른 ERegionType 값을 하나 조회함
    /// </summary>
    private bool TryGetDifferentRegion(
        ERegionType correctRegion,
        out ERegionType differentRegion)
    {
        Array regionValues =
            Enum.GetValues(typeof(ERegionType));

        foreach (ERegionType region in regionValues)
        {
            if (region == correctRegion)
            {
                continue;
            }

            differentRegion = region;
            return true;
        }

        differentRegion = correctRegion;
        return false;
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
                $"[SortingSystemTest][PASS] {testName} | {detail}");
        }
        else
        {
            failCount++;
            Debug.LogError(
                $"[SortingSystemTest][FAIL] {testName} | {detail}");
        }
    }

    /// <summary>
    /// 전체 테스트 결과를 콘솔에 출력함
    /// </summary>
    private void PrintResult()
    {
        Debug.Log(
            "========== Sorting System Test 종료 ==========\n" +
            $"PASS: {passCount}, FAIL: {failCount}");
    }
}
