public class MorningReportData
{
    // 완료됐지만 아직 결과를 확인하지 않은 배달 수임
    public int UncheckedDeliveryCount { get; private set; }

    // 미확인 배달 결과에서 수령할 수 있는 보상의 총합임
    public int ClaimableRewardAmount { get; private set; }

    // 현재 수신한 답장 중 아직 읽지 않은 답장 수임
    public int UnreadReplyCount { get; private set; }

    // 조건을 달성해 현재 사용자가 직접 해금할 수 있는 노선 수임
    public int UnlockableRouteCount { get; private set; }

    // 현재 우체국에서 보관 중인 편지 수임
    public int CurrentLetterCount { get; private set; }

    // 시설 효과가 적용된 현재 최대 편지 보관 수임
    public int MaxLetterCapacity { get; private set; }

    /// <summary>
    /// 아침 배달 보고서에 표시할 진행 현황을 생성함
    /// </summary>
    public MorningReportData(int uncheckedDeliveryCount, int claimableRewardAmount, int unreadReplyCount, int unlockableRouteCount, int currentLetterCount, int maxLetterCapacity)
    {
        // 아직 확인하지 않은 배달 결과 수를 저장함
        UncheckedDeliveryCount = uncheckedDeliveryCount;

        // 수령 가능한 보상의 총합을 저장함
        ClaimableRewardAmount = claimableRewardAmount;

        // 읽지 않은 답장 수를 저장함
        UnreadReplyCount = unreadReplyCount;

        // 현재 직접 해금 가능한 노선 수를 저장함
        UnlockableRouteCount = unlockableRouteCount;

        // 현재 보관 중인 편지 수를 저장함
        CurrentLetterCount = currentLetterCount;

        // 현재 최대 편지 보관 수를 저장함
        MaxLetterCapacity = maxLetterCapacity;
    }
}
