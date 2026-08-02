using System.Collections.Generic;
using UnityEngine;

// 아침 배달 보고서의 집계 결과가 현재 플레이어 데이터와 일치하는지 검증함
public class MorningReportTester : MonoBehaviour
{
    [Header("필수 참조")]
    [SerializeField] private GameFlowController gameFlowController;
    [SerializeField] private PlayerDataManager playerDataManager;
    [SerializeField] private StaticDataCatalog staticDataCatalog;
    [SerializeField] private LetterService letterService;
    [SerializeField] private ProgressionService progressionService;

    private int passCount;
    private int failCount;

    /// <summary>
    /// 현재 진행 상태와 아침 배달 보고서의 집계 결과를 비교함
    /// </summary>
    [ContextMenu("Run Morning Report Test")]
    private void RunMorningReportTest()
    {
        passCount = 0;
        failCount = 0;

        Debug.Log("========== Morning Report Test 시작 ==========");

        // 플레이 모드가 아니라면 테스트하지 않음
        if (!Application.isPlaying)
        {
            LogResult(
                "플레이 모드 확인",
                false,
                "플레이 모드에서 실행해야 함");

            PrintResult();
            return;
        }

        // 테스트에 필요한 참조를 확인함
        if (!ValidateReferences())
        {
            PrintResult();
            return;
        }

        // GameFlowController를 통해 아침 배달 보고서를 조회함
        MorningReportData reportData =
            gameFlowController.GetMorningReport();

        // 보고서 데이터 생성 여부를 확인함
        LogResult(
            "1. 아침 배달 보고서 생성",
            reportData != null,
            reportData != null
                ? "MorningReportData 생성됨"
                : "MorningReportData가 null임");

        // 보고서가 없다면 이후 비교를 진행하지 않음
        if (reportData == null)
        {
            PrintResult();
            return;
        }

        // 현재 미확인 배달 결과를 기준으로 예상값을 계산함
        CalculateExpectedDeliverySummary(
            out int expectedUncheckedDeliveryCount,
            out int expectedClaimableRewardAmount);

        // 미확인 배달 결과 수가 일치하는지 확인함
        LogResult(
            "2. 미확인 배달 결과 수 일치",
            reportData.UncheckedDeliveryCount ==
            expectedUncheckedDeliveryCount,
            $"기대: {expectedUncheckedDeliveryCount}, " +
            $"실제: {reportData.UncheckedDeliveryCount}");

        // 수령 가능한 보상 합계가 일치하는지 확인함
        LogResult(
            "3. 수령 가능한 보상 합계 일치",
            reportData.ClaimableRewardAmount ==
            expectedClaimableRewardAmount,
            $"기대: {expectedClaimableRewardAmount}, " +
            $"실제: {reportData.ClaimableRewardAmount}");

        // 현재 읽지 않은 답장 수를 조회함
        int expectedUnreadReplyCount =
            playerDataManager.GetUnreadReplyCount();

        // 읽지 않은 답장 수가 일치하는지 확인함
        LogResult(
            "4. 읽지 않은 답장 수 일치",
            reportData.UnreadReplyCount ==
            expectedUnreadReplyCount,
            $"기대: {expectedUnreadReplyCount}, " +
            $"실제: {reportData.UnreadReplyCount}");

        // 현재 해금 가능한 노선 수를 계산함
        int expectedUnlockableRouteCount =
            CalculateExpectedUnlockableRouteCount();

        // 해금 가능한 노선 수가 일치하는지 확인함
        LogResult(
            "5. 해금 가능한 노선 수 일치",
            reportData.UnlockableRouteCount ==
            expectedUnlockableRouteCount,
            $"기대: {expectedUnlockableRouteCount}, " +
            $"실제: {reportData.UnlockableRouteCount}");

        // 현재 보관 중인 편지 수를 조회함
        int expectedCurrentLetterCount =
            letterService.GetCurrentLetterCount();

        // 현재 편지 수가 일치하는지 확인함
        LogResult(
            "6. 현재 편지 수 일치",
            reportData.CurrentLetterCount ==
            expectedCurrentLetterCount,
            $"기대: {expectedCurrentLetterCount}, " +
            $"실제: {reportData.CurrentLetterCount}");

        // 현재 최대 편지 보관 수를 조회함
        int expectedMaxLetterCapacity =
            letterService.GetMaxLetterCapacity();

        // 최대 편지 보관 수가 일치하는지 확인함
        LogResult(
            "7. 최대 편지 보관 수 일치",
            reportData.MaxLetterCapacity ==
            expectedMaxLetterCapacity,
            $"기대: {expectedMaxLetterCapacity}, " +
            $"실제: {reportData.MaxLetterCapacity}");

        // 편지 수가 최대 수용량을 초과하지 않는지 확인함
        LogResult(
            "8. 편지 수용량 범위 확인",
            reportData.CurrentLetterCount <=
            reportData.MaxLetterCapacity,
            $"현재: {reportData.CurrentLetterCount}, " +
            $"최대: {reportData.MaxLetterCapacity}");

        // 집계값에 음수가 포함되지 않았는지 확인함
        bool hasValidValues =
            reportData.UncheckedDeliveryCount >= 0 &&
            reportData.ClaimableRewardAmount >= 0 &&
            reportData.UnreadReplyCount >= 0 &&
            reportData.UnlockableRouteCount >= 0 &&
            reportData.CurrentLetterCount >= 0 &&
            reportData.MaxLetterCapacity >= 1;

        LogResult(
            "9. 보고서 집계값 유효성 확인",
            hasValidValues,
            hasValidValues
                ? "모든 집계값이 유효함"
                : "음수 또는 잘못된 수용량 값이 존재함");

        PrintResult();
    }

    /// <summary>
    /// 테스트에 필요한 참조가 모두 연결되어 있는지 확인함
    /// </summary>
    private bool ValidateReferences()
    {
        // 필수 참조 연결 여부를 확인함
        bool hasReferences =
            gameFlowController != null &&
            playerDataManager != null &&
            staticDataCatalog != null &&
            letterService != null &&
            progressionService != null;

        // 참조 검사 결과를 출력함
        LogResult(
            "필수 참조 연결",
            hasReferences,
            hasReferences
                ? "모든 참조가 연결됨"
                : "Inspector의 필수 참조를 확인해야 함");

        // 참조 연결 결과를 반환함
        return hasReferences;
    }

    /// <summary>
    /// 현재 미확인 배달 결과 수와 수령 가능한 보상 합계를 계산함
    /// </summary>
    private void CalculateExpectedDeliverySummary(
        out int uncheckedDeliveryCount,
        out int claimableRewardAmount)
    {
        // 예상 미확인 배달 결과 수를 0으로 초기화함
        uncheckedDeliveryCount = 0;

        // 예상 보상 합계를 0으로 초기화함
        claimableRewardAmount = 0;

        // 현재 미확인 배달 결과 목록을 조회함
        IReadOnlyList<DeliveryResultData> uncheckedResults =
            playerDataManager.GetUncheckedDeliveryResults();

        // 결과 목록이 없다면 기본값을 유지함
        if (uncheckedResults == null) return;

        // 현재 미확인 배달 결과를 순회함
        foreach (DeliveryResultData deliveryResultData in uncheckedResults)
        {
            // 유효하지 않은 결과 데이터는 건너뜀
            if (deliveryResultData == null) continue;

            // 유효한 미확인 결과 수를 증가시킴
            uncheckedDeliveryCount++;

            // 양수 보상만 예상 보상 합계에 더함
            if (deliveryResultData.RewardAmount > 0)
            {
                claimableRewardAmount +=
                    deliveryResultData.RewardAmount;
            }
        }
    }

    /// <summary>
    /// 현재 조건을 충족해 직접 해금할 수 있는 노선 수를 계산함
    /// </summary>
    private int CalculateExpectedUnlockableRouteCount()
    {
        // 전체 노선 정적 데이터를 조회함
        IReadOnlyList<RouteStaticData> routes =
            staticDataCatalog.Routes();

        // 노선 목록이 없다면 0을 반환함
        if (routes == null) return 0;

        // 해금 가능한 노선 수를 저장함
        int unlockableRouteCount = 0;

        // 전체 노선을 순회함
        foreach (RouteStaticData routeStaticData in routes)
        {
            // 유효하지 않은 노선 데이터는 건너뜀
            if (routeStaticData == null) continue;

            // 유효하지 않은 노선 ID는 건너뜀
            if (routeStaticData.RouteID <= 0) continue;

            // 현재 직접 해금할 수 있는 노선만 집계함
            if (progressionService.CanUnlockRoute(
                routeStaticData.RouteID))
            {
                unlockableRouteCount++;
            }
        }

        // 계산한 해금 가능 노선 수를 반환함
        return unlockableRouteCount;
    }

    /// <summary>
    /// 개별 테스트 결과를 출력하고 성공·실패 횟수를 기록함
    /// </summary>
    private void LogResult(
        string testName,
        bool success,
        string detail)
    {
        // 테스트에 성공한 경우 성공 횟수를 증가시킴
        if (success)
        {
            passCount++;

            Debug.Log(
                $"[MorningReportTest][PASS] {testName} | {detail}");

            return;
        }

        // 테스트에 실패한 경우 실패 횟수를 증가시킴
        failCount++;

        Debug.LogError(
            $"[MorningReportTest][FAIL] {testName} | {detail}");
    }

    /// <summary>
    /// 전체 테스트 결과를 콘솔에 출력함
    /// </summary>
    private void PrintResult()
    {
        // 성공 및 실패 개수를 출력함
        Debug.Log(
            "========== Morning Report Test 종료 ==========\n" +
            $"PASS: {passCount}, FAIL: {failCount}");
    }
}
