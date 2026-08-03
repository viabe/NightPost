using System.Collections.Generic;

public class MorningReportService
{
    // 배달 결과와 답장 상태를 조회하는 플레이어 데이터 관리자임
    private PlayerDataManager playerDataManager;

    // 전체 노선 정적 데이터를 조회하는 카탈로그임
    private StaticDataCatalog staticDataCatalog;

    // 현재 편지 수와 최대 편지 수용량을 조회하는 편지 서비스임
    private LetterService letterService;

    // 조건을 달성한 노선의 해금 가능 여부를 확인하는 진행 서비스임
    private ProgressionService progressionService;

    /// <summary>
    /// 아침 배달 보고서 생성에 필요한 의존성을 연결함
    /// </summary>
    public bool Initialize(PlayerDataManager dataManager, StaticDataCatalog catalog, LetterService letter, ProgressionService progression)
    {
        // 플레이어 데이터 관리자가 없다면 초기화하지 않음
        if(dataManager == null) return false;

        // 정적 데이터 카탈로그가 없다면 초기화하지 않음
        if(catalog == null) return false;

        // 편지 서비스가 없다면 초기화하지 않음
        if(letter == null) return false;

        // 진행 서비스가 없다면 초기화하지 않음
        if (progression == null) return false;

        // 전달받은 플레이어 데이터 관리자를 저장함
        playerDataManager = dataManager;

        // 전달받은 정적 데이터 카탈로그를 저장함
        staticDataCatalog = catalog;

        // 전달받은 편지 서비스를 저장함
        letterService = letter;

        // 전달받은 진행 서비스를 저장함
        progressionService = progression;

        // 모든 의존성 연결이 완료되면 초기화 성공을 반환함
        return true;
    }

    /// <summary>
    /// 아직 확인하지 않은 배달 결과 수와 수령 가능한 보상 총합을 계산함
    /// </summary>
    private void CalculateUncheckedDeliverySummary(out int uncheckedDeliveryCount, out int claimableRewardAmount)
    {
        // 반환할 미확인 배달 결과 수를 0으로 초기화함
        uncheckedDeliveryCount = 0;

        // 반환할 수령 가능 보상 총합을 0으로 초기화함
        claimableRewardAmount = 0;

        // 플레이어 데이터 관리자가 없다면 기본값을 유지하고 종료함
        if(playerDataManager == null) return;

        // 플레이어 데이터 관리자에서 아직 확인하지 않은 배달 결과 목록을 조회함
        IReadOnlyList<DeliveryResultData> uncheckedResults = playerDataManager.GetUncheckedDeliveryResults();

        // 미확인 배달 결과 목록이 없거나 비어 있다면 기본값을 유지하고 종료함
        if(uncheckedResults == null || uncheckedResults.Count == 0) return;

        // 미확인 배달 결과 목록을 순회함
        foreach(DeliveryResultData result in uncheckedResults)
        {
            // 유효하지 않은 배달 결과 데이터는 건너뜀
            if(result == null) continue;

            // 유효한 미확인 배달 결과 수를 1 증가시킴
            uncheckedDeliveryCount++;
            // 보상 금액이 0보다 클 때만 수령 가능 보상 총합에 더함
            if(result.RewardAmount > 0) claimableRewardAmount += result.RewardAmount;
        }
    }
    /// <summary>
    /// 조건을 달성해 현재 사용자가 직접 해금할 수 있는 노선 수를 반환함
    /// </summary>
    private int CalculateUnlockableRouteCount()
    {
        // 정적 데이터 카탈로그 또는 진행 서비스가 없다면 0을 반환함
        if (staticDataCatalog == null || progressionService == null) return 0;

        // 정적 데이터 카탈로그에서 전체 노선 목록을 조회함
        IReadOnlyList<RouteStaticData> routes = staticDataCatalog.Routes();

        // 전체 노선 목록이 없거나 비어 있다면 0을 반환함
        if(routes == null || routes.Count == 0) return 0;

        // 해금 가능한 노선 수를 저장할 변수를 0으로 초기화함
        int unlockableRouteCount = 0;

        // 전체 노선 정적 데이터를 순회함
        foreach(RouteStaticData route in routes)
        {
            // 유효하지 않은 노선 데이터는 건너뜀
            if(route == null) continue;

            // 진행 서비스에서 해당 노선의 현재 해금 가능 여부를 확인함
            if(!progressionService.CanUnlockRoute(route.RouteID)) continue;

            // 현재 해금할 수 있는 노선이라면 개수를 1 증가시킴
            unlockableRouteCount++;
        }

        // 계산한 해금 가능 노선 수를 반환함
        return unlockableRouteCount;
    }

    /// <summary>
    /// 현재 플레이어 진행 상태를 집계해 아침 배달 보고서를 생성함
    /// </summary>
    public MorningReportData CreateMorningReport()
    {
        // 플레이어 데이터 관리자, 편지 서비스, 정적 데이터 카탈로그,
        // 진행 서비스 중 하나라도 없다면 보고서를 생성하지 않음
        if (playerDataManager == null || staticDataCatalog == null || letterService == null || progressionService == null) return null;

        // 아직 확인하지 않은 배달 결과 수와 수령 가능한 보상 총합을 계산함
        CalculateUncheckedDeliverySummary(out int uncheckedDeliveryCount, out int claimableRewardAmount);

        // 현재 읽지 않은 답장 수를 조회함
        int unreadReplyCount = playerDataManager.GetUnreadReplyCount();

        // 조건을 달성해 현재 직접 해금할 수 있는 노선 수를 계산함
        int unlockableRouteCount = CalculateUnlockableRouteCount();

        // 현재 우체국에서 보관 중인 편지 수를 조회함
        int currentLetterCount = letterService.GetCurrentLetterCount();

        // 시설 효과가 적용된 최대 편지 보관 수를 조회함
        int maxLetterCapacity = letterService.GetMaxLetterCapacity();

        // 모든 집계 결과를 사용해 아침 배달 보고서 데이터를 생성함
        // 생성한 아침 배달 보고서 데이터를 반환함
        return new MorningReportData(uncheckedDeliveryCount, claimableRewardAmount, unreadReplyCount, unlockableRouteCount, currentLetterCount, maxLetterCapacity);
    }

    /// <summary>
    /// 아직 확인하지 않은 배달 결과 목록을 아침 보고 화면에 제공함
    /// </summary>
    public IReadOnlyList<DeliveryResultData> GetUncheckedDeliveryResults()
    {
        // 반환할 미확인 배달 결과 목록을 생성함
        List<DeliveryResultData> reportResults = new();

        // 플레이어 데이터 관리자가 없다면 빈 목록을 반환함
        if(playerDataManager == null) return reportResults;

        // 플레이어 데이터 관리자에서 미확인 배달 결과 목록을 조회함
        IReadOnlyList<DeliveryResultData> uncheckedResults = playerDataManager.GetUncheckedDeliveryResults();

        // 조회한 목록이 없다면 빈 목록을 반환함
        if(uncheckedResults == null) return reportResults;

        // 조회한 전체 미확인 배달 결과를 순회함
        foreach (DeliveryResultData deliveryResult in uncheckedResults)
        {
            // 유효하지 않은 배달 결과는 제외함
            if(deliveryResult == null || deliveryResult.LetterID <= 0) continue;

            // 방어적으로 이미 확인된 배달 결과는 제외함
            if (deliveryResult.IsChecked) continue;

            // 아침 보고 화면에 제공할 결과 목록에 추가함
            reportResults.Add(deliveryResult);
        }

        // 미확인 배달 결과 목록을 반환함
        return reportResults;
    }

}
