public class DeliveryPreviewData
{
    // 예상 정보를 계산한 편지 ID임
    public int LetterID { get; private set; }

    // 예상 정보를 계산한 배달부 ID임
    public int CourierID { get; private set; }

    // 예상 정보를 계산한 노선 ID임
    public int RouteID { get; private set; }

    // 노선에 설정된 기본 배달 시간임
    public float RouteBaseDurationSeconds { get; private set; }

    // 배달부 속도가 적용된 배달 시간임
    public float CourierAdjustedDurationSeconds { get; private set; }

    // 시설에서 제공하는 배달 시간 감소 비율임
    public float FacilityReductionRate { get; private set; }

    // 배달부와 시설 효과가 모두 적용된 최종 예상 시간임
    public float EstimatedDurationSeconds { get; private set; }

    // 배달 완료 후 받을 것으로 예상되는 보상임
    public int ExpectedReward { get; private set; }

    // 편지의 목적 지역과 선택 노선의 지역이 일치하는지 나타냄
    public bool IsRegionMatched { get; private set; }

    // 현재 선택 조합으로 실제 배달을 시작할 수 있는지 나타냄
    public bool CanStartDelivery { get; private set; }

    /// <summary>
    /// 편지·배달부·노선 조합의 배달 예상 정보를 생성함
    /// </summary>
    public DeliveryPreviewData(int letterID, int courierID, int routeID, float routeBaseDurationSeconds, float courierAdjustedDurationSeconds, float facilityReductionRate, float estimatedDurationSeconds, int expectedReward, bool isRegionMatched, bool canStartDelivery)
    {
        // 전달받은 편지 ID를 저장함
        LetterID = letterID;

        // 전달받은 배달부 ID를 저장함
        CourierID = courierID;

        // 전달받은 노선 ID를 저장함
        RouteID = routeID;

        // 노선 기본 배달 시간을 저장함
        RouteBaseDurationSeconds = routeBaseDurationSeconds;

        // 배달부 속도가 적용된 시간을 저장함
        CourierAdjustedDurationSeconds = courierAdjustedDurationSeconds;

        // 시설 배달 시간 감소 비율을 저장함
        FacilityReductionRate = facilityReductionRate;

        // 최종 예상 배달 시간을 저장함
        EstimatedDurationSeconds = estimatedDurationSeconds;

        // 예상 보상을 저장함
        ExpectedReward = expectedReward;

        // 목적 지역과 노선 지역의 일치 여부를 저장함
        IsRegionMatched = isRegionMatched;

        // 실제 배달 시작 가능 여부를 저장함
        CanStartDelivery = canStartDelivery;
    }
}
